using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WRS.CRMInterface.WebAPI.Helper
{
    public class ApiDBSql
    {
        public const string insertLog = @"INSERT INTO WRS_CRMAPILog (LogType,APIName,CRMRequestInfo,NCRequestInfo,ResponseInfo,ExceptionLog,StartTime,EndTime) 
                                          Values('@LogType','@APIName','@CRMRequestInfo','@NCRequestInfo','@ResponseInfo','@ExceptionLog','@startTime','@EndTime')";

        public const string insertExceptionLog = @"INSERT INTO WRS_CRMAPILog (LogType,APIName,CRMRequestInfo,ExceptionLog,StartTime,EndTime) 
                                          Values('exception','@APIName','@CRMRequestInfo','@ExceptionLog','@startTime','@EndTime')";
    }
}