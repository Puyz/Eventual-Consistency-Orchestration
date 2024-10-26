using MassTransit;
using Order.API.Context;
using Shared.OrderEvents;

namespace Order.API.Consumers
{
    public class OrderCompletedEventConsumer : IConsumer<OrderCompletedEvent>
    {
        private readonly OrderDbContext _context;

        public OrderCompletedEventConsumer(OrderDbContext context)
        {
            _context = context;
        }

        public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
        {
            Order.API.Models.Order order = await _context.Orders.FindAsync(context.Message.OrderId);
            if (order != null)
            {
                order.OrderStatus = Enums.OrderStatus.Completed;
                await _context.SaveChangesAsync();
            }
        }
    }
}
