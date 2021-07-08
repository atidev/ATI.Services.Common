using JetBrains.Annotations;

namespace ATI.Services.RabbitMQ
{
    [PublicAPI]
    public static class Operation
    {
        public const string Change = "changed";
        public const string Update = "u";
        public const string Insert = "i";
        public const string Delete = "d";
        public const string BeforeUpdate = "beforeUpdate";
        public const string Any = "*";
    }
}
