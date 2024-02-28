using FastGithub.Configuration;

namespace FastGithub.Http
{
    /// <summary>
    /// httpClient工厂
    /// </summary>
    public interface IHttpClientFactory
    {
        /// <summary>
        /// 创建httpClient
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="domainConfig"></param>
        /// <returns></returns>
        HttpClient CreateHttpClient(string domain, DomainConfig domainConfig);
    }
}
