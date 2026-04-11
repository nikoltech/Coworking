using Coworking.Application.Common.Behaviors;
using Coworking.Application.Common.Behaviors.Performance;
using Coworking.Application.Features.Bookings.Commands.Create;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Coworking.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper();
            services.AddMediatR();

            return services;
        }

        private static void AddAutoMapper(this IServiceCollection services)
        {
            services.AddAutoMapper(_ => { }, typeof(DependencyInjection).Assembly);
        }

        private static void AddMediatR(this IServiceCollection services)
        {
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

                AddBehaviorsPipeline(cfg);
            });

            services.AddFluentValidators();
        }

        private static void AddBehaviorsPipeline(MediatRServiceConfiguration cfg)
        {
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));

            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionConflictRetryBehavior<,>));
        }

        private static void AddFluentValidators(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblyContaining<CreateBookingCommand>();
        }
    }
}
