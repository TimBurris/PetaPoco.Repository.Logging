namespace PetaPoco.Repository
{
    public class PetaPocoLoggingConfiguration
    {

        /// <summary>
        /// when true, every SQL Statement executed by Databases of the configured <see cref="Abstractions.IDatabaseFactory"/> will be logged
        /// </summary>
        public bool LogSqlStatements { get; set; } = true;
        public Microsoft.Extensions.Logging.LogLevel SqlLogLevel { get; set; } = Microsoft.Extensions.Logging.LogLevel.Debug;
    }
}
