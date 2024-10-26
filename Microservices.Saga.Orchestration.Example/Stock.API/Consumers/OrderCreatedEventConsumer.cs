using MassTransit;
using MongoDB.Driver;
using Shared.OrderEvents;
using Shared.Settings;
using Shared.StockEvents;
using Stock.API.Services;
namespace Stock.API.Consumers
{
    public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
    {
        private readonly MongoDBService mongoDBService;
        private readonly ISendEndpointProvider sendEndpointProvider;
        public OrderCreatedEventConsumer(MongoDBService mongoDBService, ISendEndpointProvider sendEndpointProvider)
        {
            this.mongoDBService = mongoDBService;
            this.sendEndpointProvider = sendEndpointProvider;
        }

        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            List<bool> stockResult = new();
            var stockCollection = mongoDBService.GetCollection<Stock.API.Models.Stock>();

            foreach (var orderItem in context.Message.OrderItems)
            {
                stockResult.Add(await (await stockCollection.FindAsync(s => s.ProductId == orderItem.ProductId && s.Count >= orderItem.Count)).AnyAsync());
            }

            var sendEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMQSettings.StateMachineQueue}"));

            if (stockResult.TrueForAll(s => s.Equals(true)))
            {
                foreach (var orderItem in context.Message.OrderItems)
                {
                    var stock = await (await stockCollection.FindAsync(s => s.ProductId == orderItem.ProductId)).FirstOrDefaultAsync();
                    stock.Count -= orderItem.Count;

                    await stockCollection.FindOneAndReplaceAsync(x => x.ProductId == orderItem.ProductId, stock);
                }

                StockReservedEvent stockReservedEvent = new(context.Message.CorrelationId)
                {
                    OrderItems = context.Message.OrderItems
                };
                await sendEndpoint.Send(stockReservedEvent);
            }
            else
            {
                StockNotReservedEvent stockNotReservedEvent = new(context.Message.CorrelationId)
                {
                    Message = "Stok yetersiz"
                };
                sendEndpoint.Send(stockNotReservedEvent);
            }
        }
    }
}
