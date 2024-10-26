using MassTransit;
using Shared.PaymentEvents;
using Shared.Settings;

namespace Payment.API.Consumers
{
    public class PaymentStartedEventConsumer : IConsumer<PaymentStartedEvent>
    {
        private readonly ISendEndpointProvider _sendEndpointProvider;

        public PaymentStartedEventConsumer(ISendEndpointProvider sendEndpointProvider)
        {
            _sendEndpointProvider = sendEndpointProvider;
        }

        public async Task Consume(ConsumeContext<PaymentStartedEvent> context)
        {
            var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMQSettings.StateMachineQueue}"));
            bool isCompleted = true;

            if (isCompleted)
            {
                PaymentCompletedEvent paymentCompletedEvent = new(context.Message.CorrelationId) { };
                await sendEndpoint.Send(paymentCompletedEvent);
            }
            else
            {
                PaymentFailedEvent paymentFailedEvent = new(context.Message.CorrelationId)
                {
                    OrderItems = context.Message.OrderItems,
                    Message = "Yetersiz bakiye"
                };
                await sendEndpoint.Send(paymentFailedEvent);
            }
        }
    }
}
