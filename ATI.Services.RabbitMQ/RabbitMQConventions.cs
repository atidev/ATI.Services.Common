using EasyNetQ;

namespace ATI.Services.RabbitMQ
{
    public class RabbitMqConventions : Conventions
    {
        public RabbitMqConventions(ITypeNameSerializer typeNameSerializer, EventbusOptions options) :
            base(typeNameSerializer)
        {
            ErrorExchangeNamingConvention = _ => "Services_Default_Error_Exchange";
            ErrorQueueNamingConvention = _ => !string.IsNullOrEmpty(options.ErrorQueueName)
                ? options.ErrorQueueName
                : "Services_Default_Error_Queue";
        }
    }
}