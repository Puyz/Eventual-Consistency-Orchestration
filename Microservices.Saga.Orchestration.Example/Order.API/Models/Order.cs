using Order.API.Enums;

namespace Order.API.Models
{
    public class Order
    {
        public long Id { get; set; }
        public long BuyerId { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
