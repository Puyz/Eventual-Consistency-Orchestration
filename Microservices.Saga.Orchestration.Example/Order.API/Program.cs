using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.API.Context;
using Order.API.DTOs;
using Shared.OrderEvents;
using Shared.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<OrderDbContext>(config =>
{
    config.UseSqlServer(builder.Configuration.GetConnectionString("SQLServer"));
});

builder.Services.AddMassTransit(configurator =>
{
    configurator.UsingRabbitMq((context, _configure) =>
    {
        _configure.Host(builder.Configuration.GetConnectionString("RabbitMQ"));
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.MapPost("/create-order", async (CreateOrderDTO createOrder, OrderDbContext context, ISendEndpointProvider sendEndpointProvider) =>
{
    Order.API.Models.Order order = new()
    {
        BuyerId = createOrder.BuyerId,
        CreatedDate = DateTime.UtcNow,
        OrderStatus = Order.API.Enums.OrderStatus.Suspend,
        TotalPrice = createOrder.OrderItems.Sum(oi => oi.Count * oi.Price),
        OrderItems = createOrder.OrderItems.Select(oi => new Order.API.Models.OrderItem
        {
            ProductId = oi.ProductId,
            Price = oi.Price,
            Count = oi.Count,
        }).ToList(),
    };

    await context.Orders.AddAsync(order);
    await context.SaveChangesAsync();

    OrderStartedEvent orderStartedEvent = new()
    {
        OrderId = order.Id,
        BuyerId = createOrder.BuyerId,
        TotalPrice = createOrder.OrderItems.Sum(oi => oi.Price * oi.Count),
        OrderItems = createOrder.OrderItems.Select(item => new Shared.Messages.OrderItemMessage
        {
            ProductId = item.ProductId,
            Price = item.Price,
            Count = item.Count,
        }).ToList()
    };

    var sendEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMQSettings.StateMachineQueue}"));
    await sendEndpoint.Send(orderStartedEvent);
});

app.Run();
