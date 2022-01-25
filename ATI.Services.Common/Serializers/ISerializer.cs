using System;
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
    }
}