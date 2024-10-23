namespace Shared.OrderEvents
{
    public class OrderFailedEvent
    {
        public long OrderId { get; set; }
        public string Message { get; set; }
    }
}
