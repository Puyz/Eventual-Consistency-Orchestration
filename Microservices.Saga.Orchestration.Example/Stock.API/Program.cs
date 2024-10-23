using MassTransit;
using MongoDB.Driver;
using Stock.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MongoDBService>();

builder.Services.AddMassTransit(configurator =>
{
    configurator.UsingRabbitMq((context, _configure) =>
    {
        _configure.Host(builder.Configuration.GetConnectionString("RabbitMQ"));
    });
});


var app = builder.Build();

using var scope = builder.Services.BuildServiceProvider().CreateScope();
var mongoDbService = scope.ServiceProvider.GetRequiredService<MongoDBService>();
var stockCollection = mongoDbService.GetCollection<Stock.API.Models.Stock>();

if (!stockCollection.FindSync(session => true).Any())
{
    await stockCollection.InsertManyAsync(new List<Stock.API.Models.Stock>()
    {
         new() { ProductId = 1, Count = 200 },
         new() { ProductId = 2, Count = 100 },
         new() { ProductId = 3, Count = 500 },
         new() { ProductId = 4, Count = 250 },
         new() { ProductId = 5, Count = 50 },
    });
}

app.Run();