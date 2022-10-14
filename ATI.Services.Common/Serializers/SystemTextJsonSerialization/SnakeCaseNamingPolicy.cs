using System;
using System.Buffers;
using System.Text.Json;

namespace ATI.Services.Common.Serializers.SystemTextJsonSerialization;

public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        return string.IsNullOrEmpty(name)
            ? name
            : ToSnakeCase(name);
    }

    private static string ToSnakeCase(string name)
    {
        var delimitersCount = DelimitersCount(name);
        var bufferSize = name.Length + delimitersCount;

        var buffer = ArrayPool<char>.Shared.Rent(bufferSize);
        var bufferPosition = 1;
            
        buffer[0] = char.ToLowerInvariant(name[0]);

        for (var namePosition = 1; namePosition < name.Length; ++namePosition)
        {
            var character = name[namePosition];
                
            if (char.IsUpper(character))
            {
                buffer[bufferPosition] = '_';
                bufferPosition++;
                buffer[bufferPosition] = char.ToLowerInvariant(character);
                bufferPosition++;
            }
            else
            {
                buffer[bufferPosition] = character;
                bufferPosition++;
            }
        }

        var slice = buffer.AsSpan(0, bufferSize);
            
        var result = new string(slice);
        ArrayPool<char>.Shared.Return(buffer);

        return result;
    }

    private static int DelimitersCount(string input)
    {
        var count = 0;

        for (var i = 1; i < input.Length; ++i)
        {
            if (char.IsUpper(input[i]))
                count++;
        }

        return count;
    }
}
