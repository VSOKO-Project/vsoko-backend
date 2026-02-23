using System.Reflection;
using Application.Common.Behaviors;
using Application.Common.Specification.WorkloadSpecification;
using Application.Common.Specification.FeedbackSpecification;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection service)
    {
        service.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        });

        service.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        service.AddScoped<IWorkloadAccessService, WorkloadAccessService>();
        service.AddScoped<IFeedbackAccessService, FeedbackAccessService>();

        return service;
    }
}
