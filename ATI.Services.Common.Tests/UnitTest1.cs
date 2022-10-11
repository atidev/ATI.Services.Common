using System;
using ATI.Services.Common.Serializers;
using ATI.Services.Common.Serializers.Newtonsoft;
using ATI.Services.Common.Serializers.SystemTextJsonSerialization;
using ATI.Services.Common.Tracing;
using Xunit;

namespace ATI.Services.Common.Tests;

public class UnitTest1
{
    private class User
    {
        public int Age { get; set; }
        public string Info { get; set; }
        public DateTime BirthDate { get; set; }
        public bool IsMale { get; set; }
    }
    
    [Fact]
    public void Test1()
    {
        // Arrange
        var kek = new TracedHttpClientConfig("test", TimeSpan.FromSeconds(5),
            SerializerType.Newtonsoft, true);
        var user = new User
        {
            Age = 15,
            BirthDate = DateTime.Now,
            Info = "kek",
            IsMale = true
        };
        var systemTextJsonSerializer = new SystemTextJsonSerializer(false);
        var newtonsoftSerializer = new NewtonsoftSerializer();
        
        // Act
        var textJson = systemTextJsonSerializer.Serialize(user);
        var newtonsoftJson = newtonsoftSerializer.Serialize(user);

        // Assert
        Assert.Equal(textJson, newtonsoftJson);
    }

    public void Test2()
    {
        // Arrange
        var json = "{\"Age\":15,\"Info\":\"kek\",\"BirthDate\":\"2022-10-11T12:43:31.446317+03:00\",\"IsMale\":true}";
        var systemTextJsonSerializer = new SystemTextJsonSerializer(false);
        var newtonsoftSerializer = new NewtonsoftSerializer();
        
        // Act
        var textData = systemTextJsonSerializer.Deserialize<User>(json);
        var newtonsoftData = newtonsoftSerializer.Deserialize<User>(json);
    }
}