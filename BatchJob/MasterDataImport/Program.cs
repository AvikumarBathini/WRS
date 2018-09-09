using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using WRS.Xrm;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;

namespace MasterDataImport
{
    class Program
    {
        static int batchSize = int.Parse(ConfigurationManager.AppSettings["BatchSize"]);

        private string NCId;
        Dictionary<string, int> currIds;

        static void Main(string[] args)
        {
            string alternatekeyLogicalName = "wrs_id";

            const string StoreStoreProcName = "CrmGetStores";
            const string CountryStoreProcName = "CrmGetCountries";
            const string StateStoreProcName = "CrmGetStateProvinces";
            const string CurrencyStoreProcName = "CrmGetCurrencies";

            const string Store = "Store";
            const string Country = "Country";
            const string State = "StateProvince";
            const string Currency = "Currency";
            Program p = new Program();

            p.UpsertStores(Store, p, StoreStoreProcName, alternatekeyLogicalName);

            p.UpsertCountries(Country, p, CountryStoreProcName, alternatekeyLogicalName);

            p.UpsertStateProvinces(State, p, StateStoreProcName, alternatekeyLogicalName);

            p.UpsertCurrency(Currency, p, CurrencyStoreProcName, alternatekeyLogicalName);
        }

        private void UpsertCurrency(string currency, Program p, string storeProc, string alternateKeyLogicalName)
        {
            string primaryKeyLogicalName = "transactioncurrencyid";
            alternateKeyLogicalName = "currencyname";
            DataTable CurrencyTable = RetrieveRecordsFromDB(storeProc);
            if (CurrencyTable != null && CurrencyTable.Rows.Count > 0)
            {
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(CurrencyTable, PrepareCurrencyObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, currency, TransactionCurrency.EntityLogicalName, alternateKeyLogicalName, primaryKeyLogicalName);
                    }
            }
        }

        private List<EntityCollection> GetEntityCollection(DataTable table, Func<DataRow, Entity> myMethod)
        {
            int totalCount = table.Rows.Count;
            if (totalCount == 0)
                return null;
            int requests = (totalCount / batchSize) + 1; //Maximum parallel requests
            List<EntityCollection> _lisEntityCollection = new List<EntityCollection>();
            for (int i = 0; i < requests; i++)
            {
                //Add (requests) set of EntityCollection in _lisEntityCollection
                _lisEntityCollection.Add(new EntityCollection());
            }

            //Add maximum of (batchCount) number of records in each _lisEntityCollection -> EntityCollection object
            for (int j = 0; j < totalCount; j++)
            {
                try
                {
                    DataRow dr = table.Rows[j];
                    Entity _record = myMethod(dr);
                    if (_record != null)
                        _lisEntityCollection[j % requests].Entities.Add(_record);
                }
                catch (Exception ex)
                {
                    continue;
                }
            }
            return _lisEntityCollection;
        }

        private void UpsertStores(string Store, Program p, string storeProc, string alternatekeyLogicalName)
        {
            string primaryKeyLogicalName = "wrs_storeid";
            DataTable StoreTable = RetrieveRecordsFromDB(storeProc);
            if (StoreTable != null && StoreTable.Rows.Count > 0)
            {
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(StoreTable, PrepareStoreObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, Store, wrs_store.EntityLogicalName, alternatekeyLogicalName, primaryKeyLogicalName);
                    }
            }
        }

        private void UpsertCountries(string Country, Program p, string storeProc, string alternatekeyLogicalName)
        {
            string primaryKeyLogicalName = "wrs_countryid";
            DataTable CountryTable = RetrieveRecordsFromDB(storeProc);
            if (CountryTable != null && CountryTable.Rows.Count > 0)
            {
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(CountryTable, PrepareCountryObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, Country, wrs_country.EntityLogicalName, alternatekeyLogicalName, primaryKeyLogicalName);
                    }
            }
        }

        private void UpsertStateProvinces(string State, Program p, string storeProc, string alternatekeyLogicalName)
        {
            string primaryKeyLogicalName = "wrs_stateregionid";
            DataTable StateTable = RetrieveRecordsFromDB(storeProc);
            if (StateTable != null && StateTable.Rows.Count > 0)
            {
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(StateTable, PrepareStateObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, State, wrs_stateregion.EntityLogicalName, alternatekeyLogicalName, primaryKeyLogicalName);
                    }
            }
        }

        public void CrmExecuteMultiple(EntityCollection ecoll, Program p, string table, string entityLogicalName, string alternate, string primary)
        {
            string mainSuccessLog = string.Empty;
            try
            {
                ExecuteMultipleRequest multipleReq = new ExecuteMultipleRequest()
                {
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = true,
                        ReturnResponses = true
                    },
                    Requests = new OrganizationRequestCollection()
                };
                foreach (Entity e in ecoll.Entities)
                {
                    multipleReq.Requests.Add(CreateUpsertRequest(e));
                }
                ExecuteMultipleResponse emrResponse = (ExecuteMultipleResponse)Program.serviceClient().Execute(multipleReq);
                List<string> existingRecords = new List<string>();
                foreach (ExecuteMultipleResponseItem e in emrResponse.Responses)
                {
                    if (e.Fault == null)
                    {
                        string alternateKey = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString();
                        if ((!entityLogicalName.Equals(TransactionCurrency.EntityLogicalName) && int.Parse(alternateKey) != 0) ||
                            (entityLogicalName.Equals(TransactionCurrency.EntityLogicalName) && alternateKey != ""))
                        {
                            existingRecords.Add(alternateKey);
                            if (mainSuccessLog != string.Empty)
                                mainSuccessLog = mainSuccessLog + ",";
                            mainSuccessLog = mainSuccessLog + "('" + DateTime.Now + "','Success','" + table + "','" + alternateKey + "')";
                        }
                    }
                    else if (e.Fault != null)
                    {
                        NCId = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString();
                        string errorLog = "('" + DateTime.Now + "','" + e.Fault.Message.Replace("'", "''") + "','" + (e.Fault.TraceText != null ? e.Fault.TraceText.Replace("'", "''") : " ") + "','" + table + "','" + NCId + "'";
                        WriteLogsToDB(errorLog);
                    }
                }
                foreach (string l in existingRecords)
                {
                    if (table == "Currency")
                    {
                        bool SGCur = true;
                        p.UpdateSyncStatus(table, currIds[l].ToString());
                        if (SGCur)
                        {
                            SGCur = false;
                            p.UpdateSyncStatus(table, currIds["Singapore Dollar"].ToString());
                        }
                    }
                    {
                        p.UpdateSyncStatus(table, l);
                    }
                }
                WriteMigrateStatusLogs(mainSuccessLog);
            }
            catch (Exception e)
            {
                string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','" + table + "','" + NCId + "'";
                WriteLogsToDB(errorLog);
            }
        }

        public void UpdateSyncStatus(string entityName, string l)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQL"].ConnectionString))
            using (var update_item = new SqlCommand("CrmUpdateAuditLogSyncStatus", conn))
            {
                update_item.CommandType = CommandType.StoredProcedure;
                update_item.Parameters.Add("@EntityId", SqlDbType.NVarChar).Value = l.ToString();
                update_item.Parameters.Add("@EntityName", SqlDbType.NVarChar).SqlValue = entityName;
                conn.Open();
                update_item.ExecuteNonQuery();
                conn.Close();
            }
        }

        public int RetreveAlternateKey(Guid accountID, string entityLogicalName, string alternate, string primary)
        {
            QueryExpression accountbyID = new QueryExpression(entityLogicalName);
            accountbyID.Criteria.AddCondition(new ConditionExpression(primary, ConditionOperator.Equal, accountID));
            accountbyID.ColumnSet.Columns.Add(alternate);
            Entity e = serviceClient().Retrieve(entityLogicalName, accountID, accountbyID.ColumnSet);
            if (e != null && e.Attributes[alternate] != null)
                return e.GetAttributeValue<int>(alternate);
            return 0;
        }

        public static void ErrorLog(string sErrMsg)
        {
            string sYear = DateTime.Now.Year.ToString();
            string sMonth = DateTime.Now.Month.ToString();
            string sDay = DateTime.Now.Day.ToString();
            string sErrorTime = sYear + sMonth + sDay;

            string sLogFormat = DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString() + " ==> ";
            StreamWriter sw = new StreamWriter("C:/" + sErrorTime, true);
            sw.WriteLine(sLogFormat + sErrMsg);
            sw.Flush();
            sw.Close();
        }

        public static IOrganizationService serviceClient()
        {
            CrmServiceClient crmConnD = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRMD"].ConnectionString);
            crmConnD.OrganizationServiceProxy.Timeout = new TimeSpan(0, 20, 0);
            IOrganizationService crmServiceD = crmConnD.OrganizationServiceProxy;
            return crmServiceD;
        }

        public DataTable RetrieveRecordsFromDB(string storeProc)
        {
            DataTable DBTable = new DataTable();
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQL"].ConnectionString))
            using (var command = new SqlCommand(storeProc, conn))
            using (var da = new SqlDataAdapter(command))
            {
                try
                {
                    command.CommandType = CommandType.StoredProcedure;
                    da.Fill(DBTable);
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
            }
            return DBTable;
        }

        public UpsertRequest CreateUpsertRequest(Entity target)
        {
            return new UpsertRequest()
            {
                Target = target
            };
        }

        public Entity PrepareCurrencyObject(DataRow dr)
        {
            currIds.Add(dr["Name"].ToString(), int.Parse(dr["Id"].ToString()));
            if (dr["Name"] != DBNull.Value && dr["Name"].ToString() != "Singapore Dollar")
            {
                Entity currencyToBeCreated = new Entity(TransactionCurrency.EntityLogicalName, "currencyname", dr["Name"].ToString());
                TransactionCurrency currencyToBeCreatedFromDB = currencyToBeCreated.ToEntity<TransactionCurrency>();

                currencyToBeCreatedFromDB.CurrencyName = dr["Name"] != DBNull.Value ? dr["Name"].ToString() : "";
                currencyToBeCreatedFromDB.ISOCurrencyCode = dr["CurrencyCode"] != DBNull.Value ? dr["CurrencyCode"].ToString() : "";
                currencyToBeCreatedFromDB.CurrencySymbol = dr["CurrencyCode"] != DBNull.Value ? dr["CurrencyCode"].ToString() : "";
                if (dr["Rate"] != DBNull.Value && decimal.Parse(dr["Rate"].ToString()) != 0)
                    currencyToBeCreatedFromDB.ExchangeRate = decimal.Parse(dr["Rate"].ToString());
                if (dr["CreatedOnUtc"] != DBNull.Value)
                    currencyToBeCreatedFromDB.OverriddenCreatedOn = (DateTime?)(dr["CreatedOnUtc"]);
                return currencyToBeCreatedFromDB;
            }
            return null;
        }
        public wrs_store PrepareStoreObject(DataRow dr)
        {
            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {
                Entity store = new Entity(wrs_store.EntityLogicalName, "wrs_id", int.Parse(dr["Id"].ToString()));
                wrs_store StoreTobeCreatedFromDB = store.ToEntity<wrs_store>();
                StoreTobeCreatedFromDB.wrs_id = int.Parse(dr["Id"].ToString());
                StoreTobeCreatedFromDB.wrs_name = dr["Name"] != DBNull.Value ? dr["Name"].ToString() : "";
                StoreTobeCreatedFromDB.wrs_url = dr["Url"] != DBNull.Value ? dr["Url"].ToString() : "";
                StoreTobeCreatedFromDB.wrs_sslenabled = dr["SslEnabled"] != DBNull.Value ? (bool)dr["SslEnabled"] : false;
                StoreTobeCreatedFromDB.wrs_secureurl = dr["SecureUrl"] != DBNull.Value ? dr["SecureUrl"].ToString() : "";
                StoreTobeCreatedFromDB.wrs_hosts = dr["Hosts"] != DBNull.Value ? dr["Hosts"].ToString() : "";
                if (dr["DefaultLanguageId"] != DBNull.Value && int.Parse(dr["DefaultLanguageId"].ToString()) != 0)
                    StoreTobeCreatedFromDB.wrs_defaultlanguage = new OptionSetValue(int.Parse(dr["DefaultLanguageId"].ToString()));
                if (dr["DisplayOrder"] != DBNull.Value)
                    StoreTobeCreatedFromDB.wrs_displayorder = int.Parse(dr["DisplayOrder"].ToString());
                StoreTobeCreatedFromDB.wrs_companyname = dr["CompanyName"] != DBNull.Value ? dr["CompanyName"].ToString() : "";
                StoreTobeCreatedFromDB.wrs_companyaddress = dr["CompanyAddress"] != DBNull.Value ? dr["CompanyAddress"].ToString() : "";
                StoreTobeCreatedFromDB.wrs_companyphonenumber = dr["CompanyPhoneNumber"] != DBNull.Value ? dr["CompanyPhoneNumber"].ToString() : "";
                StoreTobeCreatedFromDB.wrs_companyvat = dr["CompanyVat"] != DBNull.Value ? dr["CompanyVat"].ToString() : "";

                return StoreTobeCreatedFromDB;
            }
            return null;
        }

        public wrs_country PrepareCountryObject(DataRow dr)
        {
            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {
                Entity country = new Entity(wrs_country.EntityLogicalName, "wrs_id", int.Parse(dr["Id"].ToString()));
                wrs_country CountryTobeCreatedFromDB = country.ToEntity<wrs_country>();
                CountryTobeCreatedFromDB.wrs_id = int.Parse(dr["Id"].ToString());
                CountryTobeCreatedFromDB.wrs_name = dr["Name"] != DBNull.Value ? dr["Name"].ToString() : "";
                CountryTobeCreatedFromDB.wrs_countrycode = dr["TwoLetterIsoCode"] != DBNull.Value ? dr["TwoLetterIsoCode"].ToString() : "";
                CountryTobeCreatedFromDB.wrs_alpha3code = dr["ThreeLetterIsoCode"] != DBNull.Value ? dr["ThreeLetterIsoCode"].ToString() : "";
                CountryTobeCreatedFromDB.wrs_numeric3code = dr["NumericIsoCode"] != DBNull.Value ? dr["NumericIsoCode"].ToString() : "";
                if (dr["DisplayOrder"] != DBNull.Value)
                    CountryTobeCreatedFromDB.wrs_displayorder = int.Parse(dr["DisplayOrder"].ToString());
                CountryTobeCreatedFromDB.wrs_published = dr["Published"] != DBNull.Value ? (bool)dr["Published"] : false;
                return CountryTobeCreatedFromDB;
            }
            return null;
        }

        public wrs_stateregion PrepareStateObject(DataRow dr)
        {
            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {
                Entity state = new Entity(wrs_stateregion.EntityLogicalName, "wrs_id", int.Parse(dr["Id"].ToString()));
                wrs_stateregion StateTobeCreatedFromDB = state.ToEntity<wrs_stateregion>();
                StateTobeCreatedFromDB.wrs_id = int.Parse(dr["Id"].ToString());
                StateTobeCreatedFromDB.wrs_name = dr["Name"] != DBNull.Value ? dr["Name"].ToString() : "";
                StateTobeCreatedFromDB.wrs_abbriviation = dr["Abbreviation"] != DBNull.Value ? dr["Abbreviation"].ToString() : "";
                if (dr["CountryId"] != DBNull.Value && dr["CountryId"].ToString() != string.Empty)
                    StateTobeCreatedFromDB.wrs_countryid = new EntityReference(wrs_country.EntityLogicalName, "wrs_id", int.Parse(dr["CountryId"].ToString()));

                return StateTobeCreatedFromDB;
            }
            return null;
        }

        private static void WriteLogsToDB(string insertQuery)
        {
            if (insertQuery != string.Empty)
            {
                string sql = string.Empty;
                sql = "INSERT INTO [dbo].[CrmImportErrorLog] ([CreatedOnUtc],[Message],[StackTrace],[Table],[EntityId]) " +
                       "VALUES " + insertQuery + ")";

                SqlConnection cnn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQLErrorLog"].ToString());
                SqlCommand command;
                cnn.Open();
                command = new SqlCommand(sql, cnn);
                SqlDataReader dr = command.ExecuteReader();
                command.Dispose();
                cnn.Close();
            }
        }

        private static void WriteMigrateStatusLogs(string insertQuery)
        {
            if (insertQuery != string.Empty)
            {
                string sql = string.Empty;
                sql = "INSERT INTO [dbo].[CrmImportStatusLog] ([CreatedOnUtc],[Message],[Table],[EntityId]) " +
                       "VALUES " + insertQuery;

                SqlConnection cnn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQLErrorLog"].ToString());
                SqlCommand command;
                cnn.Open();
                command = new SqlCommand(sql, cnn);
                SqlDataReader dr = command.ExecuteReader();
                command.Dispose();
                cnn.Close();
            }
        }
    }
}
