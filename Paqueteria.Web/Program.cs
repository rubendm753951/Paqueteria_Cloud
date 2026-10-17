using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Paqueteria.Web.Data;
using Paqueteria.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Aurora/RDS uses a CA that may not be installed in the local Windows
// certificate store. Prefer an explicit PGSSLROOTCERT value and, for local
// development, fall back to the AWS bundle shipped beside the project.
var rootCertificate = builder.Configuration["Database:RootCertificate"]
    ?? Environment.GetEnvironmentVariable("PGSSLROOTCERT");
if (string.IsNullOrWhiteSpace(rootCertificate))
{
    var localRootCertificate = Path.Combine(builder.Environment.ContentRootPath, "global-bundle.pem");
    if (File.Exists(localRootCertificate))
    {
        rootCertificate = localRootCertificate;
    }
}

if (!string.IsNullOrWhiteSpace(rootCertificate))
{
    var npgsqlConnection = new NpgsqlConnectionStringBuilder(connectionString)
    {
        RootCertificate = rootCertificate,
        SslMode = SslMode.VerifyFull
    };
    connectionString = npgsqlConnection.ConnectionString;
}

// Tablas de negocio:
// c_agencias, c_corporativos, c_paises, etc.
builder.Services.AddDbContext<PaqueteriaDbContext>(options =>
    options.UseNpgsql(connectionString));

// Tablas de usuarios y seguridad:
// AspNetUsers, AspNetRoles, AspNetUserRoles, etc.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<AgencyAccessService>();
builder.Services.AddScoped<ShipmentPricingService>();
builder.Services.AddSingleton<PendingShipmentQuoteStore>();
builder.Services.Configure<ResendOptions>(builder.Configuration.GetSection("Resend"));
builder.Services.AddHttpClient<IEmailNotificationService, ResendEmailNotificationService>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        // Durante el desarrollo local no exigiremos confirmación por correo.
        // En producción se puede cambiar a true.
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Index");
    options.Conventions.AuthorizeFolder("/Agencias");
    options.Conventions.AuthorizeFolder("/Ordenes");
    options.Conventions.AuthorizeFolder("/Envios");
    options.Conventions.AuthorizeFolder("/Conductores");
    options.Conventions.AuthorizeFolder("/Productos");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

app.UseRouting();

// El orden es importante:
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();
