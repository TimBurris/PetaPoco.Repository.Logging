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

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _databaseFactory.DatabaseInstantiated -= this._databaseFactory_DatabaseInstantiated;
        return Task.CompletedTask;
    }

    private void _databaseFactory_DatabaseInstantiated(object sender, DatabaseInstantiatedEventArgs e)
    {
        var db = e.Database;
        db.CommandExecuted += this.Db_CommandExecuted;//know way to know when the db dies, so we can't unregister :(
    }

    private void Db_CommandExecuted(object sender, DbCommandEventArgs e)
    {
        var cmd = e.Command;

        //enable check becaue we don't want to waste time getting arguments together if we aren't even going to log 
        if (_logger?.IsEnabled(_petaPocoLoggingConfiguration.SqlLogLevel) == true)
        {
            string sql = cmd.CommandText;
            try
            {
                var parameters = cmd.Parameters.Cast<IDataParameter>().ToList();
                object[] sqlArgValues = parameters.Select(x => x?.Value).ToArray();

                //this try catch is because i've seen some weird instances where the sqlArgValues fail to log, so if that happens, we'll re-log without the values
                try
                {
                    _logger.Log(_petaPocoLoggingConfiguration.SqlLogLevel, "Executing Sql:\r\n{sql}\r\n with args:\r\n{sqlArg}", sql, sqlArgValues);
                }
                catch
                {
                    _logger.Log(_petaPocoLoggingConfiguration.SqlLogLevel, "Executing Sql:\r\n{sql}\r\n with args:\r\n-args failed to log-", sql);
                }
            }
            catch { }//i really don't think this will happen, but i don't want issues with logging to break things
        }
    }

}