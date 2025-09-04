using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProfitDLL.Services;

namespace ProfitDLL.Config;

internal class NativeInjectorBoostrapper
{
    public static void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        services.Configure<ProfitConfig>(config.GetSection(nameof(ProfitConfig)));

        services.AddHostedService<DllConectorFacadeService>();
    }
}
