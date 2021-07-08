using System; 
using System.Net;
using System.Net.Http.Headers;

namespace ATI.Services.Common.Tracing
{
    public class HttpResponseMessage<T>
    {
        public HttpStatusCode StatusCode { get; set; }
        public T Content { get; set; }
        public string RawContent { get; set; }
        public string ReasonPhrase { get; set; }
        public HttpResponseHeaders Headers { get; set; }
        public HttpResponseHeaders TrailingHeaders { get; set; }
        public Version Version { get; set; }
    }
}