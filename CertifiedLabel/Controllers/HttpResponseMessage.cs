using System.Net;

namespace CertifiedLabel.Controllers
{
    internal class HttpResponseMessage<T>
    {
        private HttpStatusCode badRequest;
        private string v;

        public HttpResponseMessage(string v, HttpStatusCode badRequest)
        {
            this.v = v;
            this.badRequest = badRequest;
        }
    }
}