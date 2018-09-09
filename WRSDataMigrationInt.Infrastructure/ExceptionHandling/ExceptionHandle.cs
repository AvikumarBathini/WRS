using System;
using Microsoft.Xrm.Sdk;

namespace WRSDataMigrationInt.Infrastructure.LoggerExceptionHandling
{
    public static class ExceptionHandle
    {
        public static void HandleExceptionForRethrow(ITracingService tracingService, Exception exception)
        {
            Exception currentException;

            for (currentException = exception; currentException != null; currentException = currentException.InnerException)
            {
                tracingService.Trace("Message : {0}", currentException.Message);
                tracingService.Trace("StactTrace : {0}", currentException.StackTrace); 
            }

            //Rethrow exception
            throw new InvalidPluginExecutionException(exception.Message);
        }
    }
}
