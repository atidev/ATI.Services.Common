using System;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace ATI.Services.Common.Serializers
{
    [PublicAPI]
    public interface ISerializer
    {
        void SetSerializeSettings(object settings);
        
        string Serialize<T>(T value);
        string Serialize(object value, Type type);
        
        T Deserialize<T>(string value);
        object Deserialize(string value, Type type);
        Task<T> DeserializeAsync<T>(Stream stream);
    }
}