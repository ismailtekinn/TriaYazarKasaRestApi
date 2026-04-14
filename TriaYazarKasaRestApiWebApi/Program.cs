using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Business.manager;
using TriaYazarKasaRestApi.Business.services;
using TriaYazarKasaRestApi.Data.Acces.Data;
using TriaYazarKasaRestApiWebApi.HostedServices;
using TriaYazarKasaRestApiWebApi.Services;
var builder = WebApplication.CreateBuilder(args);

// Hugin DLL'leri sertifika ve benzeri dosyalari calisma dizinine gore ariyor.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

// Hugin DLL'leri .NET Framework davranisina benzer sekilde legacy code pages bekliyor.
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseInMemoryDatabase("HuginDb"));


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IHuginConnectionManager, HuginConnectionManager>();
builder.Services.AddScoped<IHuginDeviceService, HuginDeviceService>();
builder.Services.AddHttpClient<IBekoDeviceService, BekoWorkerProxyService>(client =>
{
    client.BaseAddress = new Uri(BekoWorkerHostedService.WorkerUrl);
    client.Timeout = TimeSpan.FromSeconds(35);
});
builder.Services.AddHttpClient("BekoWorker", client =>
{
    client.BaseAddress = new Uri(BekoWorkerHostedService.WorkerUrl);
    client.Timeout = TimeSpan.FromSeconds(35);
});
builder.Services.AddScoped<IUnifiedSaleService, UnifiedSaleService>();

builder.Services.AddSingleton<IAutoConnectionStore, AutoConnectionStore>();
builder.Services.AddHostedService<BekoWorkerHostedService>();
builder.Services.AddHostedService<PosAutoConnectHostedService>();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
