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

        //Always register the SQL logger. It subscribes to command events at startup and decides per-command
        //whether to emit based on the live PetaPocoLoggingConfiguration.LogSqlStatements toggle, so logging
        //can be turned on/off at runtime. When off, the per-command cost is a single volatile bool check.
        services.AddHostedService<SqlLoggerHostedService>();

        return services;
    }

    public static Abstractions.ICrudRepositoryServiceCollection AddEntityCrudLogging(this Abstractions.ICrudRepositoryServiceCollection services, Logging.Options.CrudActionLoggerOptions options)
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
