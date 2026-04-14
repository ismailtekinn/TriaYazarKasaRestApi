using System.Text;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Business.manager;
using TriaYazarKasaRestApi.Business.services;

var builder = WebApplication.CreateBuilder(args);

Directory.SetCurrentDirectory(AppContext.BaseDirectory);
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSingleton<IBekoConnectionManager, BekoConnectionManager>();
builder.Services.AddSingleton<IBekoBasketOperationStore, BekoBasketOperationStore>();
builder.Services.AddSingleton<IAutoConnectionStore, AutoConnectionStore>();
builder.Services.AddScoped<IBekoDeviceService, BekoDeviceService>();

var app = builder.Build();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
