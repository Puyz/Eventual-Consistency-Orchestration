namespace Order.API.Models
{
    public class OrderItem
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public int Count { get; set; }
        public decimal Price { get; set; }
    }
}
