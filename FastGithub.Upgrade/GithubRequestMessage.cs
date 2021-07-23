using System.Net.Http;
using System.Net.Http.Headers;

namespace FastGithub.Upgrade
{
    /// <summary>
    /// github请求消息
    /// </summary>
    class GithubRequestMessage : HttpRequestMessage
    {
        public GithubRequestMessage()
        {
            this.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            this.Headers.UserAgent.Add(new ProductInfoHeaderValue(nameof(FastGithub), "1.0"));
        }
    }
}
