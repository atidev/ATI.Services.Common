using System;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace ATI.Services.Common.Serializers;

[PublicAPI]
public interface ISerializer : ISerializer<string>
{ }

[PublicAPI]
public interface IBinarySerializer : ISerializer<byte[]>
{ }

[PublicAPI]
public interface ISerializer<TOut>
{
    void SetSerializeSettings(object settings);
        
    TOut Serialize<TIn>(TIn value);
    TOut Serialize(object value, Type type);
        
    TIn Deserialize<TIn>(TOut value);
    object Deserialize(TOut value, Type type);
    Task<T> DeserializeAsync<T>(Stream stream);

    bool CanSerialize(string serializerType);
}