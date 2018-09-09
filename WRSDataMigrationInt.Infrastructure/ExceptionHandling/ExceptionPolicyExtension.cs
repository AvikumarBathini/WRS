using System;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling;

namespace WRSDataMigrationInt.Infrastructure.LoggerExceptionHandling
{
    public static class ExceptionPolicyExtension
    {
        /// <summary>
        /// The main entry point into the Exception Handling Application Block.
        /// Handles the specified <see cref="Exception"/>
        /// object according to the given <paramref name="policyName"></paramref>.
        /// </summary>
        /// <param name="exceptionToHandle">An <see cref="Exception"/> object.</param>
        /// <param name="policyName">The name of the policy to handle.</param>        
        /// <returns>
        /// Whether or not a rethrow is recommended.
        /// </returns>
        /// <example>
        /// The following code shows the usage of the 
        /// exception handling framework.
        /// <code>
        /// try
        ///	{
        ///		DoWork();
        ///	}
        ///	catch (Exception e)
        ///	{
        ///		if (ExceptionPolicy.HandleException(e, name)) throw;
        ///	}
        /// </code>
        /// </example>
        public static bool HandleException(Exception exceptionToHandle, string policyName)
        {
            return ExceptionPolicy.HandleException(exceptionToHandle, policyName);
        }

        /// <summary>
        /// Handles the specified <see cref="Exception"/>
        /// object according to the rules configured for <paramref name="policyName"/>.
        /// </summary>
        /// <param name="exceptionToHandle">An <see cref="Exception"/> object.</param>
        /// <param name="policyName">The name of the policy to handle.</param>
        /// <param name="exceptionToThrow">The new <see cref="Exception"/> to throw, if any.</param>
        /// <remarks>
        /// If a rethrow is recommended and <paramref name="exceptionToThrow"/> is <see langword="null"/>,
        /// then the original exception <paramref name="exceptionToHandle"/> should be rethrown; otherwise,
        /// the exception returned in <paramref name="exceptionToThrow"/> should be thrown.
        /// </remarks>
        /// <returns>
        /// Whether or not a rethrow is recommended. 
        /// </returns>
        /// <example>
        /// The following code shows the usage of the 
        /// exception handling framework.
        /// <code>
        /// try
        ///	{
        ///		DoWork();
        ///	}
        ///	catch (Exception e)
        ///	{
        ///	    Exception exceptionToThrow;
        ///		if (ExceptionPolicy.HandleException(e, name, out exceptionToThrow))
        ///		{
        ///		  if(exceptionToThrow == null)
        ///		    throw;
        ///		  else
        ///		    throw exceptionToThrow;
        ///		}
        ///	}
        /// </code>
        /// </example>
        /// <seealso cref="ExceptionManagerImpl.HandleException(Exception, string)"/>
        public static bool HandleException(Exception exceptionToHandle, string policyName, out Exception exceptionToThrow)
        {
            return ExceptionPolicy.HandleException(exceptionToHandle, policyName, out exceptionToThrow);
        }
         
        public static bool HandleException(Exception exceptionToHandle)
        {
            return HandleException(exceptionToHandle, ExceptionPolicyNames.UI);
        }

        public static bool HandleException(Exception exceptionToHandle, out Exception exceptionToThrow)
        {
            return HandleException(exceptionToHandle, ExceptionPolicyNames.UI, out exceptionToThrow);
        }

        public static void HandleExceptionForLogOnly(Exception exceptionToHandle)
        {
            HandleException(exceptionToHandle, ExceptionPolicyNames.LOG_ONLY);
        }
    }
}
