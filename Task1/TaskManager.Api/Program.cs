using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Interfaces;
using TaskManager.Infrastructure.Services;
using TaskManager.Infrastructure.Data.Repositories;
using Microsoft.OpenApi.Models;
using System.Reflection;
using TaskManager.Infrastructure.Messaging;
using TaskManager.Infrastructure.Configuration;
using Serilog;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using TaskManager.Infrastructure.Data;

namespace TaskManager.Api;

internal abstract class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);
            ConfigureServices(builder);
            
            var app = builder.Build();
            ConfigureMiddleware(app);
            
            ApplyDatabaseMigrations(app);
            
            app.Run();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "TaskManagementService.Api host terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        ConfigureSerilog(builder);
        builder.Services.Configure<RabbitMqSettings>(
            builder.Configuration.GetSection(RabbitMqSettings.SectionName));
        ConfigureDatabase(builder);
        RegisterServices(builder.Services);
        ConfigureSwagger(builder.Services);
        ConfigureOpenTelemetry(builder);
    }

    private static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .CreateBootstrapLogger();

        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

        Log.Information("Starting TaskManagementService.Api host");
    }

    private static void ConfigureDatabase(WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<TaskDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                sqlServerOptionsAction: sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                }));
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<ITaskRepository, SqlTaskRepository>();
        services.AddSingleton<IMessageProducer, RabbitMqProducer>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddControllers();
        services.AddEndpointsApiExplorer();
    }

    private static void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Task Management API",
                Description = "API for managing user tasks"
            });

            ConfigureSwaggerXmlComments(options);
        });
    }

    private static void ConfigureSwaggerXmlComments(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
    {
        var xmlFiles = new[]
        {
            $"{Assembly.GetExecutingAssembly().GetName().Name}.xml",
            $"{typeof(ITaskService).Assembly.GetName().Name}.xml",
            $"{typeof(TaskManager.Domain.Entities.TaskItem).Assembly.GetName().Name}.xml"
        };

        foreach (var xmlFile in xmlFiles)
        {
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        }
    }

    private static void ConfigureOpenTelemetry(WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: builder.Environment.ApplicationName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                })
                .AddSource("TaskManager.Application")
                .AddConsoleExporter());
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            ConfigureSwaggerUi(app);
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
    }

    private static void ConfigureSwaggerUi(WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Management API V1");
            c.RoutePrefix = string.Empty;
        });
    }

    private static void ApplyDatabaseMigrations(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
            dbContext.Database.Migrate();
            Log.Information("Database migration applied successfully.");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An error occurred while migrating the database.");
            throw;
        }
    }
}