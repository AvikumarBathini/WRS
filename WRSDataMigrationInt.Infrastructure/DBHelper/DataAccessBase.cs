#region Copyright(C) 2013 NCS Pte. Ltd. All rights reserved.
// ==================================================================================================
// Copyright(C) 2013 NCS Pte. Ltd. All rights reserved.
//
// SYSTEM NAME	:   NCS eService System
// COMPONENT ID	:   eService.UI.Web.DataAccess.DataAccessBase
// COMPONENT DESC:  		
//
// CREATED DATE/BY:	26 Mar 2013 / Yao Shang Dong
//
// REVISION HISTORY:	DATE/BY			SR#/CS/PM#/OTHERS		DESCRIPTION OF CHANGE                  
// ==================================================================================================
#endregion
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Practices.EnterpriseLibrary.Data;

namespace WRSDataMigrationInt.Infrastructure.DBHelper
{
    public class DataAccessBase
    {

        //#region Protected methods

        //protected static int CreateOrUpdateBySql(SqlCommand sqlCommand, string connectionStringName)
        //{
        //    var db = CreateDatabaseConnection(connectionStringName);
        //    return db.ExecuteNonQuery(sqlCommand);
        //}
        //#endregion


        //private static Database CreateDatabaseConnection(string connstringName = "")
        //{
        //    Database result = null;
        //    if (!string.IsNullOrEmpty(connstringName))
        //    {
        //        result = DatabaseFactory.CreateDatabase(connstringName);
        //    }
        //    else
        //    {
        //        result = DatabaseFactory.CreateDatabase();
        //    }
        //    if (result == null) throw new Exception("Create Database failed.");

        //    return result;
        //}

        private static readonly Dictionary<string, object> RowMappers = new Dictionary<string, object>();
        //private static readonly string NCDbConnectionName = "";
        //private static readonly string ExceptionDbConnectionName = "";

        #region Protected methods

        protected static DataTable GetDataBySql(string commandText)
        {
            Database db = CreateDatabase();
            var dataTable = new DataTable();
            var command = db.GetSqlStringCommand(commandText);
            using (var dataSet = db.ExecuteDataSet(command))
            {
                dataTable = dataSet.Tables[0];
            }
            return dataTable;
        }

        protected static int CreateBySql(string sqlCommandText)
        {
            var db = CreateDatabase();

            var command = db.GetSqlStringCommand(sqlCommandText);

            return db.ExecuteNonQuery(command);
        }

        protected static int CreateOrUpdateBySql(string sqlCommandText, string connectionStringName)
        {
            var db = CreateDatabase(connectionStringName);

            var command = db.GetSqlStringCommand(sqlCommandText);

            return db.ExecuteNonQuery(command);
        }

        protected IList<T> ExecuteSprocAccessor<T>(string procedureName, params object[] parameterValues) where T : new()
        {
            Database db = CreateDatabase();
            IRowMapper<T> rowMapper = GetRowMapper<T>(procedureName);
            IEnumerable<T> result = db.ExecuteSprocAccessor(procedureName, rowMapper, parameterValues);
            return result.ToList();
        }

        /// <summary>
        /// Overloaded to accept IResultSetMapper
        /// </summary>
        protected static IList<T> ExecuteSprocAccessor<T>(string procedureName, IResultSetMapper<T> resultSetMapper, params object[] parameterValues) where T : new()
        {
            Database db = CreateDatabase();
            IEnumerable<T> result = db.ExecuteSprocAccessor
                (procedureName, resultSetMapper, parameterValues);
            return result.ToList();
        }

        protected static DataSet ExecuteSprocWithTableValueParameterAccessor(string procedureName, string tableValueParameterName, DataTable tableValueParameter)
        {
            var database = CreateDatabase();
            var sqlCommand = database.GetStoredProcCommand(procedureName);
            sqlCommand.Parameters
                .Add(new SqlParameter(tableValueParameterName, tableValueParameter) { SqlDbType = SqlDbType.Structured });
            DataSet dataset = database.ExecuteDataSet(sqlCommand);
            return dataset;
        }

        protected static void AddRowMapper<T>(string key, Expression<Func<IRowMapper<T>>> command)
        {
            if (!RowMappers.ContainsKey(key))
            {
                var e = (MethodCallExpression)command.Body;
                var o = Expression.Lambda(e).Compile().DynamicInvoke();
                lock (RowMappers)
                {
                    if (!RowMappers.ContainsKey(key))
                    {
                        RowMappers.Add(key, o);
                    }
                }
            }
        }

        protected static IRowMapper<T> GetRowMapper<T>(string key)
        {
            if (!RowMappers.ContainsKey(key))
            {
                throw new InvalidConstraintException("key does not exists.");
            }
            return (IRowMapper<T>)RowMappers[key];
        }

        protected static int Update(string procedureName, params object[] parameterValues)
        {
            var db = CreateDatabase();

            var result = db.ExecuteNonQuery(procedureName, parameterValues);
            return result;
        }

        protected static int Delete(string procedureName, params object[] parameterValues)
        {
            return Update(procedureName, parameterValues);
        }

        protected int UpdateWithRetries(string procedureName, int retries, params object[] parameterValues)
        {
            Func<string, object[], int> function = (proc, param) => Update(proc, param);
            return ExecuteFunction(procedureName, retries, function, parameterValues);
        }

        protected static void CreateBatch(string procedureName, IEnumerable<object[]> parameterValuesList)
        {
            var db = CreateDatabase();

            foreach (var parameterValues in parameterValuesList)
            {
                var command = db.GetStoredProcCommand(procedureName);

                db.AssignParameters(command, parameterValues);

                db.ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// In the Store procedure, the last parameter must be output parameter p_id
        /// </summary>
        /// <param name="procedureName"></param>
        /// <param name="parameterValues"></param>
        /// <returns></returns>
        protected static int Create(string procedureName, params object[] parameterValues)
        {
            var db = CreateDatabase();

            var command = db.GetStoredProcCommand(procedureName);

            db.AssignParameters(command, parameterValues);

            db.ExecuteNonQuery(command);

            return (int)db.GetParameterValue(command, "p_id");
        }

        /// <summary>
        /// In the Store procedure, allow return more than one output parameter
        /// </summary>
        /// <param name="procedureName">The SP name</param>
        /// <param name="outputValues">The list of output parameters Key/Value pair</param>
        /// <param name="inputValues">The list of input parameters</param>
        /// <returns>the output parameters</returns>
        protected static Dictionary<string, object> CreateWithMultipleReturn(string procedureName, Dictionary<string, object> outputValues, params object[] inputValues)
        {
            var db = CreateDatabase();
            var command = db.GetStoredProcCommand(procedureName);
            db.AssignParameters(command, inputValues);
            db.ExecuteNonQuery(command);
            return outputValues.ToDictionary(item => item.Key, item => db.GetParameterValue(command, item.Key));
        }

        protected static void CreateWithoutReturn(string procedureName, params object[] parameterValues)
        {
            var db = CreateDatabase();

            var command = db.GetStoredProcCommand(procedureName);

            db.AssignParameters(command, parameterValues);

            db.ExecuteNonQuery(command);
        }

        protected int CreateWithRetries(string procedureName, int retries, params object[] parameterValues)
        {
            Func<string, object[], int> function = (proc, param) => Create(proc, param);
            return ExecuteFunction(procedureName, retries, function, parameterValues);
        }

        protected static IDataReader ExecuteReader(string procedureName, params object[] parameterValues)
        {
            var db = CreateDatabase();
            var command = db.GetStoredProcCommand(procedureName);
            db.AssignParameters(command, parameterValues);
            IDataReader reader = db.ExecuteReader(command);
            return reader;
        }

        protected static object ExecuteScalar(string procedureName, params object[] parameterValues)
        {
            var db = CreateDatabase();
            var command = db.GetStoredProcCommand(procedureName);
            db.AssignParameters(command, parameterValues);
            var result = db.ExecuteScalar(command);
            return result;
        }
        #endregion

        private static Database CreateDatabase(string connstringName = "")
        {
            Database result = null;
            if (!string.IsNullOrEmpty(connstringName))
            {
                result = DatabaseFactory.CreateDatabase(connstringName);
            }
            else
            {
                result = DatabaseFactory.CreateDatabase();
            }
            if (result == null) throw new Exception("Create Database failed.");

            return result;
        }

        private static int ExecuteFunction(string procedureName, int retries, Func<string, object[], int> function, params object[] parameterValues)
        {
            int result = 0;
            bool isSuccessful = false;
            int attempt = 0;

            while (!isSuccessful && attempt < retries)
            {
                try
                {
                    result = function.Invoke(procedureName, parameterValues);
                    isSuccessful = true;
                }
                catch
                {
                    attempt++;

                    if (attempt >= retries)
                    {
                        throw;
                    }
                }
            }
            return result;
        }


    }
}