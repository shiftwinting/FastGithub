using Microsoft.Extensions.DependencyInjection;
using System;

namespace FastGithub
{
    /// <summary>
    /// 表示服务特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ServiceAttribute : Attribute
    {
        /// <summary>
        /// 获取服务的生命周期
        /// </summary>
        public ServiceLifetime Lifetime { get; }

        /// <summary>
        /// 获取或设置注册的服务类型
        /// 为null直接使得当前类型
        /// </summary>
        public Type? ServiceType { get; set; }

        /// <summary>
        /// 将当前实现类型注册为服务的特性
        /// </summary>
        /// <param name="lifetime">生命周期</param>
        public ServiceAttribute(ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
        }
    }
}
