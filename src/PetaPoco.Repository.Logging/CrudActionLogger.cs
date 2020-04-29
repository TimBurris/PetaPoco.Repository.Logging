using Microsoft.Extensions.Logging;
using PetaPoco.Repository.Abstractions;
using PetaPoco.Repository.Logging.Options;
using System;

namespace PetaPoco.Repository.Logging
{

    public class CrudActionLogger : ICrudRepositoryService
    {
        private readonly CrudActionLoggerOptions _options;
        private readonly ILogger<CrudActionLogger> _logger;

        public CrudActionLogger(CrudActionLoggerOptions options, ILogger<CrudActionLogger> logger)
        {
            _options = options;
            _logger = logger;
        }

        #region ICrudRepositoryService

        public void AfterAdd<T, TPrimaryKeyType>(ICrudRepository<T, TPrimaryKeyType> repository, T entity)
        {
        }

        public void AfterRemove<T, TPrimaryKeyType>(ICrudRepository<T, TPrimaryKeyType> repository, TPrimaryKeyType entityId)
        {
        }

        public void AfterUpdate<T, TPrimaryKeyType>(ICrudRepository<T, TPrimaryKeyType> repository, T entity)
        {
        }

        public void BeforeAdd<T, TPrimaryKeyType>(ICrudRepository<T, TPrimaryKeyType> repository, T entity)
        {
            this.LogEntityAction(action: "Add", entity: entity);
        }

        public void BeforeRemove<T, TPrimaryKeyType>(ICrudRepository<T, TPrimaryKeyType> repository, TPrimaryKeyType entityId)
        { //LogEntityAction will check whether logging is enabled, 
            //      but we don't want to waste time/resources going and getting the record we are about to delete if it's not going to be logged
            if (_options.EntityLoggingEnabled)
            {
                var entity = repository.FindById(entityId);

                this.LogEntityAction(action: "Remove", entity: entity);
            }
        }

        public void BeforeUpdate<T, TPrimaryKeyType>(ICrudRepository<T, TPrimaryKeyType> repository, T entity)
        {
            this.LogEntityAction(action: "Update", entity: entity);
        }

        #endregion

        private void LogEntityAction<T>(string action, T entity)
        {
            if (_options.EntityLoggingEnabled && _logger != null && _logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Perform {action} on {entityType} with values {entityJson}", action, typeof(T).ToString(), SerializeForLogging(entity));
            }
        }

        private string SerializeForLogging(object value)
        {
            if (value == null)
                return string.Empty;
            else if (_options.EntityLoggingEnabled)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(value, formatting: Newtonsoft.Json.Formatting.Indented);
            }
            else
                return " this type of entity is not logged ";
        }

    }
}

