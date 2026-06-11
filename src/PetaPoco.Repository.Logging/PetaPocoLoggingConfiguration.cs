namespace PetaPoco.Repository
{
    public class PetaPocoLoggingConfiguration
    {

        private volatile bool _logSqlStatements = true;

        /// <summary>
        /// Live, thread-safe toggle. When true, every SQL statement executed by Databases of the configured
        /// <see cref="Abstractions.IDatabaseFactory"/> is logged (at <see cref="SqlLogLevel"/>). Can be
        /// flipped at any time (e.g. from an admin UI) to turn logging on/off at runtime with no restart.
        /// Default true.
        /// </summary>
        public bool LogSqlStatements { get => _logSqlStatements; set => _logSqlStatements = value; }

        public Microsoft.Extensions.Logging.LogLevel SqlLogLevel { get; set; } = Microsoft.Extensions.Logging.LogLevel.Debug;
    }
}
