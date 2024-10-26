using MassTransit;
using MongoDB.Driver;
using Shared.Messages;
using Stock.API.Services;

namespace Stock.API.Consumers
{
    public class StockRollbackMessageConsumer : IConsumer<StockRollbackMessage>
    {
        private readonly MongoDBService mongoDBService;
        private readonly ISendEndpointProvider sendEndpointProvider;

        public StockRollbackMessageConsumer(MongoDBService mongoDBService, ISendEndpointProvider sendEndpointProvider)
        {
            this.mongoDBService = mongoDBService;
            this.sendEndpointProvider = sendEndpointProvider;
        }

        public async Task Consume(ConsumeContext<StockRollbackMessage> context)
        {
            var stockCollection = mongoDBService.GetCollection<Stock.API.Models.Stock>();

            foreach (var orderItem in context.Message.OrderItems)
            {
                var stock = await (await stockCollection.FindAsync(x => x.ProductId == orderItem.ProductId)).FirstOrDefaultAsync();
                stock.Count += orderItem.Count;

                await stockCollection.FindOneAndReplaceAsync(x => x.ProductId == orderItem.ProductId, stock);
            }
        }
    }
}
