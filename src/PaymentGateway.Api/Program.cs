using Asp.Versioning;
using AspNetCore.Authentication.ApiKey;
using Scalar.AspNetCore;
using Serilog;
using PaymentGateway.Application;
using PaymentGateway.Infrastructure;
using PaymentGateway.Api.Authentication;
using PaymentGateway.Api.Extensions;
using PaymentGateway.Api.Middleware;

namespace PaymentGateway.Api;

public class Program
{
    public static void Main(string[] args)
    {
        ConfigureBootstrapLogger();

        try
        {
            Log.Information("Starting PaymentGateway API");

            var builder = WebApplication.CreateBuilder(args);

            ConfigureLogging(builder);
            ConfigureServices(builder);

            var app = builder.Build();

            ConfigurePipeline(app);

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "PaymentGateway API terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

        builder.Services.AddOpenApi("v1");

        builder.Services.AddApplication(builder.Configuration);
        builder.Services.AddInfrastructure(builder.Configuration);

        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        builder.Services.Configure<MerchantSettings>(
            builder.Configuration.GetSection(MerchantSettings.SectionName));

        builder.Services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
            .AddApiKeyInHeader<MerchantApiKeyProvider>(options =>
            {
                options.Realm = "PaymentGateway";
                options.KeyName = "X-API-Key";
            });

        builder.Services.AddAuthorization();
        builder.Services.AddApiRateLimiting(builder.Configuration);

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi("/openapi/{documentName}.json");

            app.MapScalarApiReference(options =>
            {
                options.WithTitle("PaymentGateway API")
                       .WithOpenApiRoutePattern("/openapi/{documentName}.json");
            });
        }

        app.UseExceptionHandler();

        app.UseCorrelationId();

        app.UseRequestLogging();

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseRateLimiter();

        app.MapControllers();

        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });

        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });
    }

    private static void ConfigureBootstrapLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, config) =>
            config.ReadFrom.Configuration(context.Configuration)
                  .ReadFrom.Services(services)
                  .Enrich.FromLogContext()
                  .Enrich.WithProperty("Application", "PaymentGateway.Api"));
    }
}


