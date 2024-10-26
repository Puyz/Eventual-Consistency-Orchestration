using MassTransit;
using Order.API.Context;
using Shared.OrderEvents;

namespace Order.API.Consumers
{
    public class OrderFailedEventConsumer : IConsumer<OrderFailedEvent>
    {
        private readonly OrderDbContext _context;

        public OrderFailedEventConsumer(OrderDbContext context)
        {
            _context = context;
        }
        public async Task Consume(ConsumeContext<OrderFailedEvent> context)
        {
            Order.API.Models.Order order = await _context.Orders.FindAsync(context.Message.OrderId);
            if (order != null)
            {
                order.OrderStatus = Enums.OrderStatus.Fail;
                await _context.SaveChangesAsync();
            }
        }
    }
}
