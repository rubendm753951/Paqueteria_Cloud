using Paqueteria.Web.Models;

namespace Paqueteria.Web.Data;

public sealed class PendingShipmentQuoteStore
{
    private static readonly TimeSpan QuoteLifetime = TimeSpan.FromMinutes(10);
    private readonly object _sync = new();
    private readonly Dictionary<Guid, Entry> _quotes = [];

    public PendingShipmentQuote Add(PendingShipmentQuote quote)
    {
        lock (_sync)
        {
            RemoveExpiredQuotes();
            _quotes[quote.Token] = new Entry(quote);
        }

        return quote;
    }

    public static DateTimeOffset GetExpiration() => DateTimeOffset.UtcNow.Add(QuoteLifetime);

    public bool TryClaim(Guid token, string userId, out PendingShipmentQuote quote)
    {
        lock (_sync)
        {
            quote = null!;
            if (!_quotes.TryGetValue(token, out var entry) || entry.Quote.UserId != userId || entry.IsClaimed)
            {
                return false;
            }

            if (entry.Quote.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                _quotes.Remove(token);
                return false;
            }

            entry.IsClaimed = true;
            quote = entry.Quote;
            return true;
        }
    }

    public void Release(Guid token)
    {
        lock (_sync)
        {
            if (_quotes.TryGetValue(token, out var entry))
            {
                entry.IsClaimed = false;
            }
        }
    }

    public bool TryTake(Guid token, string userId, out PendingShipmentQuote quote)
    {
        lock (_sync)
        {
            quote = null!;
            if (!_quotes.TryGetValue(token, out var entry) || entry.Quote.UserId != userId || entry.IsClaimed)
            {
                return false;
            }

            if (entry.Quote.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                _quotes.Remove(token);
                return false;
            }

            _quotes.Remove(token);
            quote = entry.Quote;
            return true;
        }
    }

    public void Consume(Guid token)
    {
        lock (_sync)
        {
            _quotes.Remove(token);
        }
    }

    private void RemoveExpiredQuotes()
    {
        var expiredTokens = _quotes
            .Where(pair => pair.Value.Quote.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var token in expiredTokens)
        {
            _quotes.Remove(token);
        }
    }

    private sealed class Entry(PendingShipmentQuote quote)
    {
        public PendingShipmentQuote Quote { get; } = quote;
        public bool IsClaimed { get; set; }
    }
}