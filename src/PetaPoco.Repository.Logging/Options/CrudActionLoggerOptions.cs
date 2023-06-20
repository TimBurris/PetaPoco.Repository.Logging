namespace PetaPoco.Repository.Logging.Options
{
    public class CrudActionLoggerOptions
    {
        public bool EntityLoggingEnabled { get; set; } = true;
        internal static System.IServiceProvider ServiceProvider { get; set; }
    }
}

