using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Paqueteria.Web.Data;
using Paqueteria.Web.Models;

namespace Paqueteria.Web.Services;

public sealed class ResendOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = "http://localhost:5090";
}

public interface IEmailNotificationService
{
    bool IsConfigured { get; }
    Task<bool> SendSubscriptionConfirmationAsync(TrackingSubscription subscription, CancellationToken cancellationToken = default);
    Task SendTrackingUpdateAsync(string trackingNumber, string status, string description, CancellationToken cancellationToken = default);
}

public sealed class ResendEmailNotificationService(
    HttpClient httpClient,
    IOptions<ResendOptions> options,
    PaqueteriaDbContext context,
    ILogger<ResendEmailNotificationService> logger) : IEmailNotificationService
{
    private readonly ResendOptions settings = options.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(settings.ApiKey) &&
        !string.IsNullOrWhiteSpace(settings.FromEmail);

    public async Task<bool> SendSubscriptionConfirmationAsync(
        TrackingSubscription subscription,
        CancellationToken cancellationToken = default)
    {
        var confirmationUrl = BuildUrl("/Rastreo/Confirmar", subscription.ConfirmationToken);
        var unsubscribeUrl = BuildUrl("/Rastreo/Cancelar", subscription.ConfirmationToken);
        var html = $"""
            <h2>Confirma las actualizaciones de tu envío</h2>
            <p>Solicitaste recibir novedades del envío <strong>{Encode(subscription.TrackingNumber)}</strong>.</p>
            <p><a href="{Encode(confirmationUrl)}">Confirmar suscripción</a></p>
            <p>Si no solicitaste esta suscripción, puedes ignorar este mensaje.</p>
            <p><a href="{Encode(unsubscribeUrl)}">Cancelar suscripción</a></p>
            """;

        return await SendAsync(
            subscription.Email,
            $"Confirma el seguimiento {subscription.TrackingNumber}",
            html,
            $"subscription-confirmation-{subscription.Id}",
            cancellationToken);
    }

    public async Task SendTrackingUpdateAsync(
        string trackingNumber,
        string status,
        string description,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await context.TrackingSubscriptions.AsNoTracking()
            .Where(item => item.TrackingNumber == trackingNumber && item.Status == "confirmed")
            .ToListAsync(cancellationToken);

        foreach (var subscription in subscriptions)
        {
            var trackingUrl = BuildUrl("/Rastreo", trackingNumber);
            var unsubscribeUrl = BuildUrl("/Rastreo/Cancelar", subscription.ConfirmationToken);
            var html = $"""
                <h2>Actualización de tu envío</h2>
                <p>El envío <strong>{Encode(trackingNumber)}</strong> tiene una nueva actualización:</p>
                <p><strong>{Encode(status)}</strong></p>
                <p>{Encode(description)}</p>
                <p><a href="{Encode(trackingUrl)}">Consultar seguimiento</a></p>
                <p><a href="{Encode(unsubscribeUrl)}">Cancelar suscripción</a></p>
                """;

            await SendAsync(
                subscription.Email,
                $"Actualización de envío {trackingNumber}: {status}",
                html,
                $"tracking-update-{trackingNumber}-{status}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
                cancellationToken);
        }
    }

    private async Task<bool> SendAsync(
        string recipient,
        string subject,
        string html,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("Resend no está configurado. Configure Resend:ApiKey y Resend:FromEmail.");
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            request.Headers.Add("Idempotency-Key", idempotencyKey);
            request.Content = JsonContent.Create(new ResendEmailRequest(settings.FromEmail, [recipient], subject, html));

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode) return true;

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Resend rechazó el correo para {Recipient}. HTTP {StatusCode}: {Body}", recipient, response.StatusCode, body);
            return false;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(exception, "No se pudo enviar el correo mediante Resend a {Recipient}.", recipient);
            return false;
        }
    }

    private string BuildUrl(string path, string? value)
    {
        var baseUrl = settings.PublicBaseUrl.TrimEnd('/');
        var encodedValue = Uri.EscapeDataString(value ?? string.Empty);
        return path == "/Rastreo"
            ? $"{baseUrl}{path}?tracking={encodedValue}"
            : $"{baseUrl}{path}?token={encodedValue}";
    }

    private static string Encode(string value) => HtmlEncoder.Default.Encode(value);

    private sealed record ResendEmailRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string[] To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html);
}
