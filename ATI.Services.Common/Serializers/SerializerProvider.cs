using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace ATI.Services.Common.Serializers;

[PublicAPI]
public class SerializerProvider
{
    private readonly IReadOnlyCollection<ISerializer> _serializers;
    private readonly IReadOnlyCollection<IBinarySerializer> _binarySerializers;

    public SerializerProvider(
        IEnumerable<ISerializer> serializers,
        IEnumerable<IBinarySerializer> binarySerializers)
    {
        _binarySerializers = new List<IBinarySerializer>(binarySerializers);
        _serializers = new List<ISerializer>(serializers);
    }

    [CanBeNull]
    public ISerializer GetSerializerByType(string serializerType)
    {
        var required = _serializers.SingleOrDefault(v => v.CanSerialize(serializerType));

       return required;
    }

    [CanBeNull]
    public IBinarySerializer GetBinarySerializerByType(string serializerType)
    {
        var required = _binarySerializers.SingleOrDefault(v => v.CanSerialize(serializerType));

        return required;
    }
}