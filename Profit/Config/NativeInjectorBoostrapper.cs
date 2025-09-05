using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProfitDLL.Services;
using System;

namespace ProfitDLL.Config;

internal class NativeInjectorBoostrapper
{
    public static void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("ProfitDLL"))
            .WithMetrics(metrics =>
            {
                metrics.AddConsoleExporter()
                       .AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation();

                metrics.AddOtlpExporter(options => options.Endpoint = new Uri("http://profitdll.dashboard:18889"));
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                tracing.AddOtlpExporter(options => options.Endpoint = new Uri("http://profitdll.dashboard:18889"));
            });


        services.Configure<ProfitSettings>(config.GetSection(nameof(ProfitSettings)));

        services.AddHostedService<DllConectorFacadeService>();
    }
}
