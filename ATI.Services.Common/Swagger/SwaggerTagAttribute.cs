using System;

namespace ATI.Services.Common.Swagger
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class SwaggerTagAttribute : Attribute
    {
        public SwaggerTagAttribute(SwaggerTag tag)
        {
            Tag = tag;
        }
        public SwaggerTag Tag { get; }
    }
}
