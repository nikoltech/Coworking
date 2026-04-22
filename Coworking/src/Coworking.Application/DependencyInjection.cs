// Application/DependencyInjection.cs
using Coworking.Application.Behaviors;
using Coworking.Application.Behaviors.Performance;
using Coworking.Application.Features.Bookings.Commands.Create;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Coworking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services
            .AddAutoMapper()
            .AddMediatR();

        return services;
    }

    private static IServiceCollection AddAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(_ => { }, typeof(DependencyInjection).Assembly);
        return services;
    }

    private static IServiceCollection AddMediatR(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddBehaviorsPipeline();
        });

        services.AddValidatorsFromAssemblyContaining<CreateBookingCommand>();

        return services;
    }

    private static void AddBehaviorsPipeline(this MediatRServiceConfiguration cfg)
    {
        cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        cfg.AddOpenBehavior(typeof(DomainExceptionBehavior<,>));
        cfg.AddOpenBehavior(typeof(TransactionConflictRetryBehavior<,>));
    }
}