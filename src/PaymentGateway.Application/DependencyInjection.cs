using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Application.Behaviors;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Application.Payments.Rules;
using PaymentGateway.Application.Settings;

using System.Reflection;

namespace PaymentGateway.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.Configure<PaymentRulesSettings>(configuration.GetSection(PaymentRulesSettings.SectionName));
        services.AddTransient<IPaymentBusinessRule, BlockedBinRule>();

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
            config.AddOpenBehavior(typeof(IdempotencyBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
