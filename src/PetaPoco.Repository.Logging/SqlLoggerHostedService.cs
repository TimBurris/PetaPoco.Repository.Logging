using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using PetaPoco.Repository.Abstractions;
using System.Data;
using System.Linq;

namespace PetaPoco.Repository.Logging;

public class SqlLoggerHostedService : Microsoft.Extensions.Hosting.IHostedService
{
    private readonly ILogger<SqlLoggerHostedService> _logger;
    private readonly IDatabaseFactory _databaseFactory;
    private readonly PetaPocoLoggingConfiguration _petaPocoLoggingConfiguration;

    public SqlLoggerHostedService(ILogger<SqlLoggerHostedService> logger, Abstractions.IDatabaseFactory databaseFactory, PetaPocoLoggingConfiguration petaPocoLoggingConfiguration)
    {
        _logger = logger;
        _databaseFactory = databaseFactory;
        _petaPocoLoggingConfiguration = petaPocoLoggingConfiguration;
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_petaPocoLoggingConfiguration.LogSqlStatements)
            return Task.CompletedTask;

        if (_logger == null || !_logger.IsEnabled(_petaPocoLoggingConfiguration.SqlLogLevel))
            return Task.CompletedTask;

        _databaseFactory.DatabaseInstantiated += this._databaseFactory_DatabaseInstantiated;

        return Task.CompletedTask;
    }

    private void _databaseFactory_DatabaseInstantiated(object sender, DatabaseInstantiatedEventArgs e)
    {
        var db = e.Database;
        db.CommandExecuted += this.Db_CommandExecuted;
    }

    private void Db_CommandExecuted(object sender, DbCommandEventArgs e)
    {
        var cmd = e.Command;

        if (_logger?.IsEnabled(LogLevel.Debug) == true)
        {
            string sql = cmd.CommandText;
            try
            {
                var parameters = cmd.Parameters.Cast<IDataParameter>().ToList();
                string sqlArgNames = string.Join(",", parameters.Select(x => "{" + x.ParameterName + "}").ToArray());
                //arg values always fail to log, throwing an exception, so i'm excluding them
                //object[] sqlArgValues = paramerters.Select(x => x?.Value).ToArray();
                //System.Diagnostics.Debug.WriteLine($"Executing Sql:\r\n{sql}\r\n with args:\r\n{sqlArgNames}", sqlArgValues);
                //_logger?.LogDebug($"Executing Sql:\r\n{sql}\r\n with args:\r\n{sqlArgNames}", sqlArgValues);
                _logger.Log(_petaPocoLoggingConfiguration.SqlLogLevel, "Executing Sql:\r\n{sql}\r\n with args:\r\n{sqlArgNames}", sql, sqlArgNames);
            }
            catch
            {

            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _databaseFactory.DatabaseInstantiated -= this._databaseFactory_DatabaseInstantiated;
        return Task.CompletedTask;
    }
}