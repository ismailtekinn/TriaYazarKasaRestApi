using Microsoft.EntityFrameworkCore;
using System.Text;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Business.manager;
using TriaYazarKasaRestApi.Business.services;
using TriaYazarKasaRestApi.Data.Acces.Data;

var builder = WebApplication.CreateBuilder(args);

Directory.SetCurrentDirectory(AppContext.BaseDirectory);
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=C:\\TriaYazarKasaDataBase\\TriaPos.db";

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddSingleton<IBekoConnectionManager, BekoConnectionManager>();
builder.Services.AddSingleton<IBekoBasketOperationStore, BekoBasketOperationStore>();
builder.Services.AddSingleton<IAutoConnectionStore, AutoConnectionStore>();
builder.Services.AddScoped<IBekoDeviceService, BekoDeviceService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    using var dbContext = dbContextFactory.CreateDbContext();
    dbContext.Database.Migrate();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
