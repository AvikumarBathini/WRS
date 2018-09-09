using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WRSDataMigrationInt.Infrastructure.DBHelper;

namespace WRS.CRMInterface.WebAPI.Helper
{
    public class DBDataAccess : DataAccessBase
    {

        //public static int InsertOrUpdateLog(string querySqlString)
        //{
        //    return CreateOrUpdateBySql(querySqlString, "WRSDB.ConnectionString");
        //}

        //public static int InsertOrUpdateLog(string LogType, string APIName, string CRMRequestInfo, string NCRequestInfo, string ResponseInfo, 
        //    string ExceptionLog, string StartTime, string EndTime)
        //{
        //    SqlCommand sqlCommand = new SqlCommand(@"INSERT INTO WRS_CRMAPILog (LogType,APIName,CRMRequestInfo,NCRequestInfo,ResponseInfo,ExceptionLog,StartTime,EndTime) 
        //                                  Values('@LogType','@APIName','@CRMRequestInfo','@NCRequestInfo','@ResponseInfo','@ExceptionLog','@startTime','@EndTime')");
        //    sqlCommand.Parameters.AddWithValue("@LogType", LogType);
        //    sqlCommand.Parameters.AddWithValue("@APIName", APIName);
        //    sqlCommand.Parameters.AddWithValue("@CRMRequestInfo", CRMRequestInfo);
        //    sqlCommand.Parameters.AddWithValue("@NCRequestInfo", NCRequestInfo);
        //    sqlCommand.Parameters.AddWithValue("@ResponseInfo", ResponseInfo);
        //    sqlCommand.Parameters.AddWithValue("@ExceptionLog", ExceptionLog);
        //    sqlCommand.Parameters.AddWithValue("@startTime", StartTime);
        //    sqlCommand.Parameters.AddWithValue("@EndTime", EndTime);

        //    return CreateOrUpdateBySql(sqlCommand, "WRSDB.ConnectionString");
        //}

        public static DataTable getAccountData(string querySqlString)
        {
            return GetDataBySql(querySqlString);
        }

        public static int InsertOrUpdateLog(string querySqlString)
        {
            return CreateOrUpdateBySql(querySqlString, "WRSDB.ConnectionString");
        }
    }
}
