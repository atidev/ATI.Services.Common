using System.Collections.Generic;
using System.Linq;
using ATI.Services.Common.Logging.Configuration;
using NLog.Layouts;

namespace ATI.Services.Common.Logging;

public static class JsonAttributeHelper
{
    public static JsonAttribute CreateWithoutUnicodeEscaping(string name, Layout layout) =>
        new(name, layout) {EscapeUnicode = false};

    public static JsonAttribute CreateWithoutUnicodeEscaping(string name, Layout layout, bool jsonEncode) =>
        new(name, layout, jsonEncode) {EscapeUnicode = false};

    public static JsonAttribute ToJsonAttribute(this ConfigJsonAttribute configJsonAttribute) =>
        new()
        {
            Name = configJsonAttribute.Name,
            Layout = configJsonAttribute.Layout,
            Encode = configJsonAttribute.EncodeJson,
            EscapeUnicode = configJsonAttribute.EscapeUnicode,
            IncludeEmptyValue = configJsonAttribute.IncludeEmptyValue
        };

    public static IEnumerable<JsonAttribute> OverrideBy(this IEnumerable<JsonAttribute> baseAttributes,
        IEnumerable<JsonAttribute> attributes)
    {
            var jsonAttributes = attributes.ToList();
            var overridenNames = jsonAttributes.Select(x => x.Name).ToHashSet();
            return baseAttributes.Where(attribute => !overridenNames.Contains(attribute.Name)).Concat(jsonAttributes);
        }
        
}