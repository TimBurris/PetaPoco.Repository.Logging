using Microsoft.Extensions.DependencyInjection;
using PetaPoco.Repository.Logging.Options;
using System;

namespace PetaPoco.Repository.Logging
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddPetaPocoRepositoryLogging(this IServiceCollection services, Action<PetaPocoLoggingConfiguration> configAction)
        {
            var cfg = new PetaPocoLoggingConfiguration();
            configAction.Invoke(cfg);
            services.AddSingleton<PetaPocoLoggingConfiguration>(cfg);

            if (cfg.LogFullEntityCrud)
            {
                cfg.CrudRepositoryServiceCollection.Add(new Logging.CrudActionLogger(new CrudActionLoggerOptions() { EntityLoggingEnabled = true }));
            }

            if (cfg.LogSqlStatements)
            {
                services.AddHostedService<SqlLoggerHostedService>();
            }

            return services;
        }

        public static void UsePetaPocoRepositoryLogging(this IServiceProvider serviceProvider)
        {
            var cfg = serviceProvider.GetRequiredService<PetaPocoLoggingConfiguration>();

            if (cfg.LogFullEntityCrud)
            {
                Logging.Options.CrudActionLoggerOptions.ServiceProvider = serviceProvider;
            }

            if (cfg.LogSqlStatements)
            {
                var factory = serviceProvider.GetRequiredService<Abstractions.IDatabaseFactory>();
                factory.DatabaseInstantiated += Factory_DatabaseInstantiated;
            }

        }

        private static void Factory_DatabaseInstantiated(object sender, Abstractions.DatabaseInstantiatedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
