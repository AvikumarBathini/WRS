using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling;
using Microsoft.Practices.EnterpriseLibrary.Logging;

namespace WRSDataMigrationInt.Infrastructure.ExceptionHandling
{
    [ConfigurationElementType(typeof(CustomLoggingExceptionHandlerData))]
    public class CustomLoggingExceptionHandler : IExceptionHandler
    {
        private readonly string _logCategory;
        private readonly int _eventId;
        private readonly TraceEventType _severity;
        private readonly string _defaultTitle;
        private readonly int _minimumPriority;

        public CustomLoggingExceptionHandler(string logCategory, int eventId,
            TraceEventType severity, string title, int priority)
        {
            _logCategory = logCategory;
            _eventId = eventId;
            _severity = severity;
            _defaultTitle = title;
            _minimumPriority = priority;
        }

        public Exception HandleException(Exception exception, Guid handlingInstanceId)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            var cateries = new List<string> { _logCategory };
            var log = new LogEntry
                {
                    Categories = cateries,
                    Message = exception.ToString(),
                    EventId = _eventId,
                    Severity = _severity,
                    Title = _defaultTitle,
                    TimeStamp = DateTime.Now,
                    Priority = _minimumPriority,
                };
            Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(log);

            return exception;
        }
    }
}
