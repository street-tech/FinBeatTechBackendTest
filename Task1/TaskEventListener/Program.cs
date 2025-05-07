using TaskEventListener.Configuration;
using TaskEventListener.Services;
using Serilog;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

namespace TaskEventListener;

public static class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var configuration = BuildConfiguration(args);
            ConfigureLogging(configuration);
            
            Log.Information("Starting TaskEventListenerService host");
            
            var host = CreateHostBuilder(args, configuration).Build();
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "TaskEventListenerService host terminated unexpectedly");
            throw;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static IConfiguration BuildConfiguration(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
        
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
    }

    private static void ConfigureLogging(IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateBootstrapLogger();
    }

    private static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(builder =>
            {
                builder.Sources.Clear();
                builder.AddConfiguration(configuration);
            })
            .UseSerilog((context, services, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext();
            })
            .ConfigureServices(ConfigureServices);

    private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
    {
        services.Configure<RabbitMqSettings>(
            hostContext.Configuration.GetSection(RabbitMqSettings.SectionName));
        services.AddHostedService<RabbitMqListenerService>();
        ConfigureOpenTelemetry(hostContext, services);
    }

    private static void ConfigureOpenTelemetry(HostBuilderContext hostContext, IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => 
                resource.AddService(serviceName: hostContext.HostingEnvironment.ApplicationName))
            .WithTracing(tracing => 
                tracing
                    .AddSource("TaskEventListenerService")
                    .AddConsoleExporter());
    }
}