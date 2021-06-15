using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace FastGithub
{
    /// <summary>
    /// 服务注册扩展
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册程序集下所有服务下选项
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration">配置</param>  
        /// <returns></returns>
        public static IServiceCollection AddServiceAndOptions(this IServiceCollection services, Assembly assembly, IConfiguration configuration)
        { 
            services.AddAttributeServices(assembly);
            services.AddAttributeOptions(assembly, configuration); 

            return services;
        }

        /// <summary>
        /// 添加程序集下ServiceAttribute标记的服务
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assembly"></param> 
        /// <returns></returns>
        private static IServiceCollection AddAttributeServices(this IServiceCollection services, Assembly assembly)
        {
            var implTypes = assembly
                .GetTypes()
                .Where(item => item.IsClass && item.IsAbstract == false)
                .ToArray();

            foreach (var implType in implTypes)
            {
                var attributes = implType.GetCustomAttributes<ServiceAttribute>(false);
                foreach (var attr in attributes)
                {
                    var serviceType = attr.ServiceType ?? implType;
                    if (services.Any(item => item.ServiceType == serviceType && item.ImplementationType == implType) == false)
                    {
                        var descriptor = ServiceDescriptor.Describe(serviceType, implType, attr.Lifetime);
                        services.Add(descriptor);
                    }
                }
            }
            return services;
        }


        /// <summary>
        /// 添加程序集下OptionsAttribute标记的服务 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assembly"></param> 
        /// <param name="configuration"></param>
        private static IServiceCollection AddAttributeOptions(this IServiceCollection services, Assembly assembly, IConfiguration configuration)
        {
            foreach (var optionsType in assembly.GetTypes())
            {
                var optionsAttribute = optionsType.GetCustomAttribute<OptionsAttribute>();
                if (optionsAttribute != null)
                {
                    var key = optionsAttribute.SessionKey ?? optionsType.Name;
                    var section = configuration.GetSection(key);
                    OptionsBinder.Create(services, optionsType).Bind(section);
                }
            }
            return services;
        }

        /// <summary>
        /// options绑定器
        /// </summary>
        private abstract class OptionsBinder
        {
            public abstract void Bind(IConfiguration configuration);

            /// <summary>
            /// 创建OptionsBinder实例
            /// </summary>
            /// <param name="services"></param>
            /// <param name="optionsType"></param>
            /// <returns></returns>
            public static OptionsBinder Create(IServiceCollection services, Type optionsType)
            {
                var binderType = typeof(OptionsBinderImpl<>).MakeGenericType(optionsType);
                var binder = Activator.CreateInstance(binderType, new object[] { services });

                return binder is OptionsBinder optionsBinder
                    ? optionsBinder
                    : throw new TypeInitializationException(binderType.FullName, null);
            }

            private class OptionsBinderImpl<TOptions> : OptionsBinder where TOptions : class
            {
                private readonly IServiceCollection services;

                public OptionsBinderImpl(IServiceCollection services)
                {
                    this.services = services;
                }

                public override void Bind(IConfiguration configuration)
                {
                    this.services.AddOptions<TOptions>().Bind(configuration);
                }
            }
        }
    }
}
