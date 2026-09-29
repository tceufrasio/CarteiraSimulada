using Carteira.Infrastructure.Messaging;
using Carteira.Infrastructure.Persistence;
using Carteira.Worker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

var databaseConnection = builder.Configuration.GetConnectionString("Carteira")
    ?? throw new InvalidOperationException(
        "Configure a connection string 'Carteira'.");

builder.Services.AddDbContext<CarteiraDbContext>(options =>
    options.UseNpgsql(databaseConnection));

builder.Services.AddScoped<ProcessOrderNotification>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
