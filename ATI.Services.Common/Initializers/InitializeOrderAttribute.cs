using System;

namespace ATI.Services.Common.Initializers
{
    [AttributeUsage(AttributeTargets.Class)]
    public class InitializeOrderAttribute : Attribute
    {
        public InitializeOrder Order { get; set; }
    }
}