using JetBrains.Annotations;

namespace ATI.Services.RabbitMQ
{
    [PublicAPI]
    public static class RoutingKeys
    {
        public const string Created = "created";
        public const string Inserted = "inserted";
        public const string Updated = "updated";
        public const string Deleted = "deleted";
        public const string Restored = "restored";
    }
}