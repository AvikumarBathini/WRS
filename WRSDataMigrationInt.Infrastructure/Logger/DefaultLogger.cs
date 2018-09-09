using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Practices.EnterpriseLibrary.Logging;

namespace WRSDataMigrationInt.Infrastructure.Logger
{
    public class DefaultLogger
    {
        private static DefaultLogger _logger;
        public static DefaultLogger Current => _logger ?? (_logger = new DefaultLogger(TraceEventType.Information));

        public TraceEventType TraceEventType { get; private set; }

        private DefaultLogger(TraceEventType traceEventType)
        {
            TraceEventType = traceEventType;
        }

        public void WriteLog(LogEntry logger)
        {
            if (Microsoft.Practices.EnterpriseLibrary.Logging.Logger.IsLoggingEnabled())
            {
                ////////////////////////////////////////////////////
                // http://blogs.msdn.com/b/mark_bi/archive/2011/02/18/enterprise-library-5-logging-using-msmq.aspx
                // http://blogs.southworks.net/mwoloski/2005/03/24/entlib-async-logging-how-to/
                // http://msdn.microsoft.com/en-us/library/ff664561(PandP.50).aspx
                ////////////////////////////////////////////////////
                foreach (var category in logger.Categories)
                {
                    //Have to split categories. Otherwise it will become duplidated. 
                    //For example, when web called msmq trace listener to store logs (and assume there are two categories)
                    //then, at this time, one log entry will become to two entries in msmq storage.
                    //continually msmq service calls db trace listener. at this time, each log entries will become to double.
                    //at last, there are 4 log entries in db. (every category has two entries data)
                    var cloned = logger.Clone() as LogEntry;
                    if (cloned != null)
                    {
                        var categoryList = new List<string> { category };

                        cloned.Categories = categoryList;
                        Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(cloned);
                    }
                }
            }
            else
            {
                throw new LoggingException("Logging is disabled in the configuration.");
            }
        }

        //public void WriteLog(string message, string logtype = "")
        //{
        //    var logger = logtype == "exception" ? GenerateExceptionLogEntry() : GenerateDefaultLogEntry();
        //    logger.Message = message;
        //    WriteLog(logger);
        //}

        internal static LogEntry GenerateDefaultLogEntry()
        {
            var category = new List<string> { LogCategories.Information };
            var logger = new LogEntry
                {
                    ActivityId = Guid.Empty,
                    Categories = category,
                    TimeStamp = DateTime.Now,
                    Severity = TraceEventType.Information
                };

            return logger;
        }

        internal static LogEntry GenerateExceptionLogEntry()
        {
            var category = new List<string> { LogCategories.Exception };
            var logger = new LogEntry
            {
                ActivityId = Guid.Empty,
                Categories = category,
                TimeStamp = DateTime.Now,
                Severity = TraceEventType.Error
            };

            return logger;
        }
    }
}
