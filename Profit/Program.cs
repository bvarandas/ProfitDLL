using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using ProfitDLL.Config;
using System;
using System.IO;

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

IHost host = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(builder =>
        {
            builder.Sources.Clear();
            builder.AddConfiguration(config);

        })
        .ConfigureLogging(logging =>
        {
            logging.AddOpenTelemetry(log =>
            {
                log.AddOtlpExporter();
            });
        })
        .ConfigureServices(services =>
        {

            NativeInjectorBoostrapper.RegisterServices(services, config);

        }).Build();
await host
.RunAsync();