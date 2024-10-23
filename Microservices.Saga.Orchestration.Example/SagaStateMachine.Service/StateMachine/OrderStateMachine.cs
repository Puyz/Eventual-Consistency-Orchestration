using MassTransit;
using SagaStateMachine.Service.StateInstances;
using Shared.OrderEvents;
using Shared.PaymentEvents;
using Shared.StockEvents;

namespace SagaStateMachine.Service.StateMachine
{
    public class OrderStateMachine : MassTransitStateMachine<OrderStateInstance>
    {
        public Event<OrderStartedEvent> OrderStartedEvent { get; set; }
        public Event<StockReservedEvent> StockReservedEvent { get; set; }
        public Event<StockNotReservedEvent> StockNotReservedEvent { get; set; }
        public Event<PaymentCompletedEvent> PaymentCompletedEvent { get; set; }
        public Event<PaymentFailedEvent> PaymentFailedEvent { get; set; }

        public State OrderCreated { get; set; }
        public State StockReserved { get; set; }
        public State StockNotReserved { get; set; }
        public State PaymentCompleted { get; set; }
        public State PaymentFailed { get; set; }

        public OrderStateMachine()
        {
            InstanceState(instance => instance.CurrentState);

            // veritabanındaki order id ile eventden gelen order id eşleşmesine göre;
            // Eğer eşleşme(instance) varsa o zaman gelenin yeni bir sipariş olmadığını anlıyoruz ve kaydetmiyoruz. Eğer yoksa yeni correlationId oluştur.
            Event(() => OrderStartedEvent, 
                orderStateInstance => orderStateInstance.CorrelateBy<long>(database => database.OrderId, @event => @event.Message.OrderId)
                .SelectId(e => Guid.NewGuid()));

            Event(() => StockReservedEvent, orderStateInstance => orderStateInstance.CorrelateById(@event => @event.Message.CorrelationId));
            Event(() => StockNotReservedEvent, orderStateInstance => orderStateInstance.CorrelateById(@event => @event.Message.CorrelationId));
            Event(() => PaymentCompletedEvent, orderStateInstance => orderStateInstance.CorrelateById(@event => @event.Message.CorrelationId));
            Event(() => PaymentFailedEvent, orderStateInstance => orderStateInstance.CorrelateById(@event => @event.Message.CorrelationId));
        }
    }
}
