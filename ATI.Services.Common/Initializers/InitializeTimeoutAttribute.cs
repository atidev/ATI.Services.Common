using System;

namespace ATI.Services.Common.Initializers;

[AttributeUsage(AttributeTargets.Class)]
public class InitializeTimeoutAttribute : Attribute
{
    public TimeSpan InitTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool Required { get; set; } = false;
}