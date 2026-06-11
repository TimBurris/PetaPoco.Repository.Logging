using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using PetaPoco.Repository.Abstractions;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PetaPoco.Repository.Logging;

public class SqlLoggerHostedService : Microsoft.Extensions.Hosting.IHostedService
{
    private readonly ILogger<SqlLoggerHostedService> _logger;
    private readonly IDatabaseFactory _databaseFactory;
    private readonly PetaPocoLoggingConfiguration _petaPocoLoggingConfiguration;

    //PetaPoco has no duration on its CommandExecuted event, so we time from CommandExecuting to
    //CommandExecuted ourselves. Keyed by the command instance (the same IDbCommand is passed to both
    //events). ConditionalWeakTable means a command that never completes (e.g. it threw) is collected
    //with its key and can never leak.
    private readonly ConditionalWeakTable<IDbCommand, Stopwatch> _commandTimers = new ConditionalWeakTable<IDbCommand, Stopwatch>();

    public SqlLoggerHostedService(ILogger<SqlLoggerHostedService> logger, Abstractions.IDatabaseFactory databaseFactory, PetaPocoLoggingConfiguration petaPocoLoggingConfiguration)
    {
        _logger = logger;
        _databaseFactory = databaseFactory;
        _petaPocoLoggingConfiguration = petaPocoLoggingConfiguration;
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        //IMPORTANT: subscription is a one-time startup action, so we deliberately do NOT gate it on
        //LogSqlStatements or on the logger level here. Previously this method returned early when logging
        //was disabled at startup, which meant the hooks were never attached and logging could never be
        //turned on later. We always attach the hooks (cheap) and decide per-command whether to actually
        //emit (see ShouldLog), so logging can be toggled at runtime via LogSqlStatements.
        _databaseFactory.DatabaseInstantiated += this._databaseFactory_DatabaseInstantiated;

        return Task.CompletedTask;
    }

    //Single place that decides whether a statement is logged right now. LogSqlStatements is the live
    //runtime switch; the IsEnabled check ensures we still skip work when the logger's level is disabled.
    private bool ShouldLog()
        => _petaPocoLoggingConfiguration.LogSqlStatements
           && _logger?.IsEnabled(_petaPocoLoggingConfiguration.SqlLogLevel) == true;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _databaseFactory.DatabaseInstantiated -= this._databaseFactory_DatabaseInstantiated;
        return Task.CompletedTask;
    }

    private void _databaseFactory_DatabaseInstantiated(object sender, DatabaseInstantiatedEventArgs e)
    {
        var db = e.Database;
        db.CommandExecuting += this.Db_CommandExecuting;
        db.CommandExecuted += this.Db_CommandExecuted;//know way to know when the db dies, so we can't unregister :(
    }

    private void Db_CommandExecuting(object sender, DbCommandEventArgs e)
    {
        //enable check because we don't want to waste time timing if we aren't even going to log
        if (!ShouldLog())
            return;

        var cmd = e.Command;
        if (cmd == null)
            return;

        //start the timer for this command; it's read back in Db_CommandExecuted
        _commandTimers.Remove(cmd);//defensive: a reused command instance shouldn't carry a stale timer
        _commandTimers.Add(cmd, Stopwatch.StartNew());
    }

    private void Db_CommandExecuted(object sender, DbCommandEventArgs e)
    {
        var cmd = e.Command;

        //stop/remove the timer regardless of whether we end up logging, so nothing lingers
        long? elapsedMs = null;
        if (cmd != null && _commandTimers.TryGetValue(cmd, out var stopwatch))
        {
            stopwatch.Stop();
            elapsedMs = stopwatch.ElapsedMilliseconds;
            _commandTimers.Remove(cmd);
        }

        //enable check becaue we don't want to waste time getting arguments together if we aren't even going to log
        if (ShouldLog())
        {
            string sql = cmd.CommandText;
            try
            {
                var parameters = cmd.Parameters.Cast<IDataParameter>().ToList();
                object[] sqlArgValues = parameters.Select(x => x?.Value).ToArray();

                //this try catch is because i've seen some weird instances where the sqlArgValues fail to log, so if that happens, we'll re-log without the values
                try
                {
                    _logger.Log(_petaPocoLoggingConfiguration.SqlLogLevel, "Executed Sql in {elapsedMs}ms:\r\n{sql}\r\n with args:\r\n{sqlArg}", elapsedMs, sql, sqlArgValues);
                }
                catch
                {
                    _logger.Log(_petaPocoLoggingConfiguration.SqlLogLevel, "Executed Sql in {elapsedMs}ms:\r\n{sql}\r\n with args:\r\n-args failed to log-", elapsedMs, sql);
                }
            }
            catch { }//i really don't think this will happen, but i don't want issues with logging to break things
        }
    }

}