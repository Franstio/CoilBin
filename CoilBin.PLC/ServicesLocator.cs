using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoilBin.PLC
{
    public class ServicesLocator
    {

        public static IServiceCollection ServiceCollection { get; private set; } = new ServiceCollection();
        public static IServiceProvider Services { get; private set; } = null!;

        public static void BuildServices()
        {
            Services = ServiceCollection.BuildServiceProvider();    
        }

        public class ExternalServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
        {
            public readonly IServiceProvider _serviceProvider;
            public ExternalServiceProviderFactory(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public IServiceCollection CreateBuilder(IServiceCollection services)
            {
                return services;
            }

            public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
            {
                return _serviceProvider ;
            }
        }
    }
}
