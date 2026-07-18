using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Paqueteria.Web.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("PaqueteriaConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'PaqueteriaConnection' not found.");

// Tablas de negocio:
// c_agencias, c_corporativos, c_paises, etc.
builder.Services.AddDbContext<PaqueteriaDbContext>(options =>
    options.UseNpgsql(connectionString));

// Tablas de usuarios y seguridad:
// AspNetUsers, AspNetRoles, AspNetUserRoles, etc.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        // Durante el desarrollo local no exigiremos confirmación por correo.
        // En producción se puede cambiar a true.
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();

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

app.UseHttpsRedirection();

app.UseRouting();

// El orden es importante:
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();