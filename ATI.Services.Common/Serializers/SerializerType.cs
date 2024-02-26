namespace ATI.Services.Common.Serializers;

public enum SerializerType
{
    Newtonsoft = 0,
    SystemTextJson = 1,
        
    /// <summary>
    /// System.Text.Json with disabled converters. Since net6.0  
    /// </summary>
    SystemTextJsonClassic = 2
}