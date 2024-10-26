using MassTransit;
using SagaStateMachine.Service.StateInstances;
using Shared.Messages;
using Shared.OrderEvents;
using Shared.PaymentEvents;
using Shared.Settings;
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


            /*
             context.instance ile context.Data arasındaki fark! context.instance, veritabanındaki İlgili siparişe karşılık gelen instance satırını temsil ederken,
                context.Data ise o anki ilgili eventten gelen datayı temsil eder.
             */
            Initially(When(OrderStartedEvent)
                .Then(context =>
                {
                    context.Instance.OrderId = context.Data.OrderId;
                    context.Instance.BuyerId = context.Data.BuyerId;
                    context.Instance.TotalPrice = context.Data.TotalPrice;
                    context.Instance.CreatedDate = DateTime.UtcNow;
                })
                .TransitionTo(OrderCreated)
                .Send(new Uri($"queue:{RabbitMQSettings.Stock_OrderCreatedEventQueue}"),
                context => new OrderCreatedEvent(context.Instance.CorrelationId)
                {
                    OrderItems = context.Data.OrderItems,
                }));



            // During, ilk parametre = o anki state'i ifade ediyor.
            // When ile Eventi kontrol ediyoruz
            // TransitionTo ile state'i değiştiriyoruz.
            // Send ile kuyruğa mesaj atıyoruz.
            During(OrderCreated,
                When(StockReservedEvent)
                .TransitionTo(StockReserved)
                .Send(new Uri($"queue:{RabbitMQSettings.Payment_StartedEventQueue}"), context => new PaymentStartedEvent(context.Instance.CorrelationId)
                {
                    OrderItems = context.Data.OrderItems,
                    TotalPrice = context.Data.OrderItems.Sum(x => x.Count * x.Price)
                }),

                When(StockNotReservedEvent)
                .TransitionTo(StockNotReserved)
                .Send(new Uri($"queue:{RabbitMQSettings.Order_OrderFailedEventQueue}"), context => new OrderFailedEvent
                {
                    OrderId = context.Instance.OrderId,
                    Message = context.Data.Message
                }));


            // Finalize ile süreç tamamlandığını belirtiyoruz ve SetCompletedWhenFinalized ile instance'ı veritabanından siliyoruz.
            During(StockReserved,
                When(PaymentCompletedEvent)
                .TransitionTo(PaymentCompleted)
                .Send(new Uri($"queue:{RabbitMQSettings.Order_OrderCompletedEventQueue}"), context => new OrderCompletedEvent
                {
                    OrderId = context.Instance.OrderId
                })
                .Finalize(),

                When(PaymentFailedEvent)
                .TransitionTo(PaymentFailed)
                .Send(new Uri($"queue:{RabbitMQSettings.Order_OrderFailedEventQueue}"), context => new OrderFailedEvent
                {
                    OrderId = context.Instance.OrderId,
                    Message = context.Data.Message
                })
                .Send(new Uri($"queue:{RabbitMQSettings.Stock_RollbackMessageQueue}"), context => new StockRollbackMessage
                {
                    OrderItems = context.Data.OrderItems
                }));

            SetCompletedWhenFinalized();
        }
    }
}
