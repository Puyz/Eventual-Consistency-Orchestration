using Shared.Messages;

namespace Shared.OrderEvents
{
    public class OrderStartedEvent
    {
        public long OrderId { get; set; }
        public long BuyerId { get; set; }
        public decimal TotalPrice { get; set; }
        public List<OrderItemMessage> OrderItems { get; set; }
    }
}
