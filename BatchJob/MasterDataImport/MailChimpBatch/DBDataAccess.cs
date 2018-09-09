using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WRSDataMigrationInt.Infrastructure.DBHelper;

namespace MailChimpBatch
{
    public class DBDataAccess : DataAccessBase
    {
        public static int InsertLog(string LogType, string logInfo, string emailAddress, string createOn)
        {
            //SqlCommand sqlCommand = new SqlCommand(@"INSERT INTO {0} ([LogType],[LogInfo],[EmailAddress],[CreateOn]) 
            //                                        VALUES ('@logType','@logInfo','@emailAddress','@createOn')");
            //sqlCommand.Parameters.AddWithValue("@logType", LogType);
            //sqlCommand.Parameters.AddWithValue("@logInfo", logInfo);
            //sqlCommand.Parameters.AddWithValue("@emailAddress", emailAddress);
            //sqlCommand.Parameters.AddWithValue("@createOn", createOn);

            string insertLog = @"INSERT INTO ([LogType],[LogInfo],[EmailAddress],[CreateOn]) 
                                                    VALUES ('@logType','@logInfo','@emailAddress','@createOn')";

            var commonSql = insertLog.Replace("@logType", LogType).Replace("@logInfo", logInfo)
                               .Replace("@emailAddress", emailAddress).Replace("@createOn", createOn);

            return CreateOrUpdateBySql(commonSql, "WRSDB.ConnectionString");
        }
    }
}
