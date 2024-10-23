namespace Order.API.DTOs
{
    public class CreateOrderDTO
    {
        public long BuyerId { get; set; }
        public ICollection<OrderItemDTO> OrderItems { get; set; }
    }
}
