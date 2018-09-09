using System.Collections.Generic;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration.ContainerModel;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling.Logging.Configuration;

namespace WRSDataMigrationInt.Infrastructure.ExceptionHandling
{ 
    public class CustomLoggingExceptionHandlerData : LoggingExceptionHandlerData
    {
        public CustomLoggingExceptionHandlerData():base()
        { }
         
        /// <summary>
        /// Retrieves a container configuration model for a <see cref="WrapToBaseExceptionHandler"/> based on the data in <see cref="WrapToBaseExceptionHandlerData"/>
        /// </summary>
        /// <param name="namePrefix">The name to use when building references to child items.</param>
        /// <returns>A <see cref="WrapToBaseExceptionHandler"/> to register a <see cref="WrapToBaseExceptionHandlerData"/> in the container</returns>
        public override IEnumerable<TypeRegistration> GetRegistrations(string namePrefix)
        {

            yield return
                new TypeRegistration<IExceptionHandler>(
                    () => new CustomLoggingExceptionHandler(LogCategory,EventId,Severity,Title,Priority))
                {
                    Name = BuildName(namePrefix),
                    Lifetime = TypeRegistrationLifetime.Transient
                };
        }
    }

}
