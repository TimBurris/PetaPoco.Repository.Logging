using Microsoft.Extensions.DependencyInjection;
using System;

namespace PetaPoco.Repository.Logging;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddPetaPocoRepositoryLogging(this IServiceCollection services, Action<PetaPocoLoggingConfiguration> configAction = null)
    {
        var cfg = new PetaPocoLoggingConfiguration();
        if (configAction != null)
        {
            configAction.Invoke(cfg);
        }

        services.AddSingleton(cfg);

        if (cfg.LogSqlStatements)
        {
            services.AddHostedService<SqlLoggerHostedService>();
        }

        return services;
    }

    public static Abstractions.ICrudRepositoryServiceCollection AddCreateUpdateDateStamper(this Abstractions.ICrudRepositoryServiceCollection services, Logging.Options.CrudActionLoggerOptions options)
    {
        services.Add(new Logging.CrudActionLogger(options));

        return services;
    }

    public static void UsePetaPocoRepositoryLogging(this IServiceProvider serviceProvider)
    {
        var cfg = serviceProvider.GetRequiredService<PetaPocoLoggingConfiguration>();

        Logging.Options.CrudActionLoggerOptions.ServiceProvider = serviceProvider;
    }

}
