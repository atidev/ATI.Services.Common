namespace ATI.Services.Common.Serializers;

/// <summary>
/// Описывает типы сериализаторов, встроенные в common.
/// Не описывает типы сериализаторов, добавленные внешними пакетами
/// </summary>
public enum SerializerType
{
    Newtonsoft = 0,
    SystemTextJson = 1,
        
    /// <summary>
    /// System.Text.Json with disabled converters. Since net6.0  
    /// </summary>
    SystemTextJsonClassic = 2
}