using System;
using JetBrains.Annotations;

namespace ATI.Services.Common.Swagger;

[Flags]
[PublicAPI]
public enum SwaggerTag
{
    All = 1,
    Open = 2,
    Public = 4,
    Internal = 8
}