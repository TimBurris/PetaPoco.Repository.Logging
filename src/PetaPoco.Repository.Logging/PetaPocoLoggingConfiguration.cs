namespace PetaPoco.Repository
{
    public class PetaPocoLoggingConfiguration
    {
        public Abstractions.ICrudRepositoryServiceCollection CrudRepositoryServiceCollection { get; set; }

        /// <summary>
        /// when true, the Json representation of Entities will be logged on Add, Update, and Remove
        /// </summary>
        /// <remarks>Requires assignment of <see cref="CrudRepositoryServiceCollection"/></remarks>
        public bool LogFullEntityCrud { get; set; } = true;
        public Microsoft.Extensions.Logging.LogLevel EntityCrudLogLevel { get; set; } = Microsoft.Extensions.Logging.LogLevel.Debug;

        /// <summary>
        /// when true, every SQL Statement executed by Databases of the configured <see cref="Abstractions.IDatabaseFactory"/> will be logged
        /// </summary>
        public bool LogSqlStatements { get; set; } = true;
        public Microsoft.Extensions.Logging.LogLevel SqlLogLevel { get; set; } = Microsoft.Extensions.Logging.LogLevel.Debug;
    }
}
