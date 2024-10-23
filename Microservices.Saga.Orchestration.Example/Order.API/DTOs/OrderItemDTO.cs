namespace Order.API.DTOs
{
    public class OrderItemDTO
    {
        public long ProductId { get; set; }
        public int Count { get; set; }
        public decimal Price { get; set; }
    }
}
