﻿using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Xml.Serialization;
using WRS.Xrm;

namespace ContactImport
{
    public class Program
    {
        public static IOrganizationService CrmService = serviceClient();
        static int batchSize = int.Parse(ConfigurationManager.AppSettings["BatchSize"]);
        static string GetMemberListURL = ConfigurationManager.AppSettings["GetMemberListURL"];
        static string PUTMemberListURL = ConfigurationManager.AppSettings["PUTMemberListURL"];
        static string POSTMergeFieldsURL = ConfigurationManager.AppSettings["POSTMergeFieldsURL"];
        static string MailChimpApikey = ConfigurationManager.AppSettings["MailChimpApikey"];
        static string MailChimpListId = ConfigurationManager.AppSettings["MailChimpListId"];
        private string NCId;
        public static Dictionary<string, string> CategoryCodes = GetCategoryCodes();
        public static Dictionary<string, Guid> SubscriptionEmails = new Dictionary<string, Guid>();

        static void Main(string[] args)
        {

            string alternatekeyLogicalName = "wrs_id";
            Program p = new Program();
            const string CustomerProcName = "CrmGetCustomerDetails";
            const string GenericAttributeStoreProcNAme = "CrmGetCustomerGenericAttribute";

            const string Customer = "Customer";
            const string GenericAttr = "GenericAttribute";

            const string NewsLetterSubscriptionStoreProcNAme = "CrmGetNewsLetterSubscriptions";
            const string NewsLetterSubscriptions = "NewsLetterSubscription";

            p.UpsertAccounts(Customer, p, CustomerProcName, alternatekeyLogicalName);

            p.UpdateGenericAttribute(GenericAttr, p, GenericAttributeStoreProcNAme, alternatekeyLogicalName);

            p.UpsertNewsletterSubscription(NewsLetterSubscriptions, p, NewsLetterSubscriptionStoreProcNAme, alternatekeyLogicalName);

            p.UpdateLuminaVisitDate();

        }

        private void UpdateLuminaVisitDate()
        {
            string fetch =
                "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>" +
                      "<entity name='salesorderdetail'>" +
                        "<attribute name='salesorderdetailid' />" +
                        "<attribute name='wrs_attributesxml' />" +
                        "<attribute name='salesorderid' />" +
                        "<order attribute='salesorderid' descending='false' />" +
                        "<link-entity name='product' from='productid' to='productid' alias='PRODUCT'>" +
                          "<filter type='and'>" +
                            "<condition attribute='name' operator='like' value='%Lumina%' />" +
                          "</filter>" +
                        "</link-entity>" +
                        "<link-entity name='contact' from='contactid' to='wrs_customer' visible='false' link-type='outer' alias='CONTACT'>" +
                          "<attribute name='emailaddress1' />" +
                          "<attribute name='lastname' />" +
                          "<attribute name='firstname' />" +
                        "</link-entity>" +
                        "<link-entity name='salesorder' from='salesorderid' to='salesorderid' alias='ORDER'>" +
                          "<attribute name='wrs_customeremail' />" +
                          "<attribute name='createdon' />" +
                          "<attribute name='wrs_id' />" +
                          "<filter type='and'>" +
                            "<condition attribute='createdon' operator='last-x-days' value='2' />" +
                            //"<condition attribute='createdon' operator='last-x-hours' value='1' />" +
                            "<condition attribute='statuscode' operator='in'>" +
                              "<value>690970000</value>" +
                              "<value>100001</value>" +
                              "<value>3</value>" +
                              "<value>1</value>" +
                            "</condition>" +
                          "</filter>" +
                        "</link-entity>" +
                      "</entity>" +
                    "</fetch>";

            var results = CrmService.RetrieveMultiple(new FetchExpression(fetch));
            List<int> orders = new List<int>();
            if (results != null && results is EntityCollection && results.Entities.Count > 0)
            {
                foreach (Entity e in results.Entities)
                {
                    int orderNum = e.Attributes.Contains("ORDER.wrs_id") ? Convert.ToInt32((e.GetAttributeValue<AliasedValue>("ORDER.wrs_id")).Value) : -1;
                    if (!orders.Contains(orderNum))
                    {
                        string email = e.Attributes.Contains("CONTACT.emailaddress1") ? e.GetAttributeValue<AliasedValue>("CONTACT.emailaddress1").Value.ToString() : string.Empty;
                        string firstname = e.Attributes.Contains("CONTACT.firstname") ? e.GetAttributeValue<AliasedValue>("CONTACT.firstname").Value.ToString() : string.Empty;
                        string lastname = e.Attributes.Contains("CONTACT.lastname") ? e.GetAttributeValue<AliasedValue>("CONTACT.lastname").Value.ToString() : string.Empty;
                        DateTime pDate = Convert.ToDateTime(e.GetAttributeValue<AliasedValue>("ORDER.createdon").Value);
                        string purchasedate = e.Attributes.Contains("ORDER.createdon") ? Convert.ToDateTime(e.GetAttributeValue<AliasedValue>("ORDER.createdon").Value).ToShortDateString() : string.Empty;
                        string attributeXml = e.GetAttributeValue<string>("wrs_attributesxml");
                        DateTime _ticketdate = GetVistDate(attributeXml);
                        string ticketdate = _ticketdate.ToShortDateString();
                        QueryExpression exp = new QueryExpression()
                        {
                            EntityName = "wrs_subscription",
                            ColumnSet = new ColumnSet("wrs_subscriptionid", "wrs_id"),
                            Criteria = new FilterExpression()
                        };
                        exp.Criteria.AddCondition("wrs_email", ConditionOperator.Equal, email);
                        exp.Criteria.AddCondition("wrs_campaign", ConditionOperator.Equal, "Lumina");
                        var lSubscription = CrmService.RetrieveMultiple(exp);
                        //if (DateTime.UtcNow.Subtract(pDate) < TimeSpan.FromMinutes(5))
                        if (lSubscription != null && lSubscription is EntityCollection && lSubscription.Entities.Count > 0)
                        {
                            Entity _sub = lSubscription.Entities.FirstOrDefault();
                            int NCID = _sub.Contains("wrs_id") ? _sub.GetAttributeValue<int>("wrs_id") : 0;
                            UpsertMailChimp(NCID, firstname, lastname, email, "", "", "", purchasedate, ticketdate);
                        }
                        orders.Add(orderNum);
                    }
                }
            }
        }

        private static DateTime GetVistDate(string attrXml)
        {
            Attributes attrs = DeserializeXML(attrXml);
            if (attrs != null && attrs.ProductAttribute.Count > 0)
            {
                return Convert.ToDateTime(attrs.ProductAttribute[0].ProductAttributeValue.Value);
            }
            return DateTime.Today;
        }

        public static Attributes DeserializeXML(string attributeXML)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Attributes));
            StringReader rdr = new StringReader(attributeXML);
            Attributes resultingMessage = (Attributes)serializer.Deserialize(rdr);
            return resultingMessage;
        }

        private void UpsertNewsletterSubscription(string newsLetterSubscriptions, Program p, string newsLetterSubscriptionStoreProcNAme, string alternateKeyLogicalName)
        {
            DataTable NewsLetterTable = RetrieveRecordsFromDB(newsLetterSubscriptionStoreProcNAme);
            if (NewsLetterTable != null && NewsLetterTable.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "contactid";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(NewsLetterTable, PrepareNewsLetterObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, newsLetterSubscriptions, "contact", alternateKeyLogicalName, primaryKeyLogicalName, true);
                    }
                if (SubscriptionEmails.Count > 0)
                {
                    foreach (string email in SubscriptionEmails.Keys)
                    {
                        QueryExpression exp = new QueryExpression()
                        {
                            EntityName = "wrs_subscription",
                            ColumnSet = new ColumnSet("wrs_categorycode", "wrs_email", "wrs_contactid"),
                            Criteria = new FilterExpression()
                        };
                        exp.Criteria.AddCondition("wrs_email", ConditionOperator.Equal, email);
                        exp.Criteria.AddCondition("wrs_categorycode", ConditionOperator.NotNull);
                        exp.Criteria.AddCondition("wrs_contactid", ConditionOperator.NotNull);
                        exp.Criteria.AddCondition("wrs_subscriptionstatus", ConditionOperator.Equal, true);
                        var subscriptions = CrmService.RetrieveMultiple(exp);
                        if (subscriptions != null && subscriptions.Entities.Count > 0)
                        {
                            string subscriptionText = string.Empty;
                            foreach (Entity subscription in subscriptions.Entities)
                            {
                                subscriptionText = subscriptionText + subscription.GetAttributeValue<string>("wrs_categorycode") + ",";
                            }
                            subscriptionText = subscriptionText.Substring(0, subscriptionText.Length - 1);
                            Entity updateContact = new Entity("contact", SubscriptionEmails[email]);
                            updateContact.Attributes.Add("wrs_subscribedcategories", subscriptionText);
                            CrmService.Update(updateContact);
                        }
                    }
                }
            }
        }

        private void UpsertAccounts(string Customer, Program p, string storeProc, string alternatekeyLogicalName)
        {
            DataTable CustomerTable = RetrieveRecordsFromDB(storeProc);
            if (CustomerTable != null && CustomerTable.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "contactid";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(CustomerTable, PrepareCustomerObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, Customer, Contact.EntityLogicalName, alternatekeyLogicalName, primaryKeyLogicalName);
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

        private void UpdateGenericAttribute(string GenericAttr, Program p, string storeProc, string alternatekeyLogicalName)
        {
            DataTable CustomerTable = RetrieveRecordsFromDB(storeProc);
            if (CustomerTable != null && CustomerTable.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "contactid";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(CustomerTable, PrepareCustomerObjectGenericAttr);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, GenericAttr, Contact.EntityLogicalName, alternatekeyLogicalName, primaryKeyLogicalName);
                    }
            }
        }

        public void CrmExecuteMultiple(EntityCollection ecoll, Program p, string table, string entityLogicalName, string alternate, string primary, bool isNewsLetter = false)
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
                //ErrorLog(string.Format("Starting Upsert of {0} {1} records", ecoll.Entities.Count, table));


                foreach (Entity e in ecoll.Entities)
                {
                    if (table == "GenericAttribute")
                    {
                        multipleReq.Requests.Add(CreateUpdateRequest(e));
                    }
                    else
                    {
                        multipleReq.Requests.Add(CreateUpsertRequest(e));
                    }
                }
                ExecuteMultipleResponse emrResponse = (ExecuteMultipleResponse)Program.serviceClient().Execute(multipleReq);
                List<int> existingRecords = new List<int>();

                foreach (ExecuteMultipleResponseItem e in emrResponse.Responses)
                {
                    if (e.Fault == null)
                    {
                        int alternateKey;

                        if (table == "GenericAttribute")
                        {
                            alternateKey = (int)((UpdateRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate];
                        }

                        else
                        {
                            alternateKey = (int)((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate];
                        }

                        if (alternateKey != 0)
                        {
                            //ErrorLog(string.Format("{0} with Guid {1} is created for NC DB ID {2}", table, ID, alternateKey));
                            existingRecords.Add(alternateKey);
                            if (mainSuccessLog != string.Empty)
                                mainSuccessLog = mainSuccessLog + ",";
                            mainSuccessLog = mainSuccessLog + "('" + DateTime.Now + "','Success','" + table + "','" + alternateKey + "')";
                        }
                    }

                    else if (e.Fault != null)
                    {
                        //ErrorLog(string.Format(("{0} with NC DB ID {1} is failed for upsert with message {2}"), table, ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString(), e.Fault.Message));
                        if (table == "GenericAttribute")
                        {
                            NCId = ((UpdateRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString();
                        }
                        //else if (table == "NewsLetterSubscription")
                        //{
                        //    NCId = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.KeyAttributes[alternate].ToString();
                        //    NewsLetterSubscriptionList.Remove(NewsLetterSubscriptionList.Find(r => r.Key == int.Parse(NCId)));
                        //}
                        else
                        {
                            NCId = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString();
                        }
                        string errorLog = "('" + DateTime.Now + "','" + e.Fault.Message.Replace("'", "''") + "','" + (e.Fault.TraceText != null ? e.Fault.TraceText.Replace("'", "''") : " ") + "','" + table + "','" + NCId + "'";
                        WriteLogsToDB(errorLog);
                    }
                }
                //if (table == "NewsLetterSubscription" && NewsLetterSubscriptionList.Count > 0)
                //{
                //    foreach (var item in NewsLetterSubscriptionList)
                //    {
                //        p.UpdateSyncStatus(table, item.Value);
                //    }
                //}
                //else
                foreach (int l in existingRecords)
                {
                    if (table == "GenericAttribute")
                    {
                        p.UpdateSyncStatusGenericAttr(table, l);
                    }
                    else
                    {
                        p.UpdateSyncStatus(table, l);
                    }
                    //ErrorLog(string.Format("Sync Status updated in NC DB for {0} with ID - {1}", table, l));
                }
                WriteMigrateStatusLogs(mainSuccessLog);
            }
            catch (Exception e)
            {
                string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','" + table + "','" + NCId + "'";
                WriteLogsToDB(errorLog);
            }
        }

        public void UpdateSyncStatus(string entityName, int l)
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

        public void UpdateSyncStatusGenericAttr(string entityName, int l)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQL"].ConnectionString))
            using (var update_item = new SqlCommand("CrmUpdateAuditLogSyncGenAttr", conn))
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

        public static Dictionary<string, string> GetCategoryCodes()
        {
            try
            {
                Dictionary<string, string> Categories = new Dictionary<string, string>();
                QueryExpression exp = new QueryExpression("wrs_subscriptioncategory")
                {
                    ColumnSet = new ColumnSet("wrs_categorycode", "wrs_mailchimpcolumntag"),
                    Criteria = new FilterExpression()
                };
                EntityCollection coll = CrmService.RetrieveMultiple(exp);
                if (coll != null && coll.Entities.Count > 0)
                {
                    foreach (Entity category in coll.Entities)
                    {
                        string categoryCode = category.Contains("wrs_categorycode") ? category.GetAttributeValue<string>("wrs_categorycode").ToLower() : string.Empty;
                        string mailchimpcolumntag = category.Contains("wrs_mailchimpcolumntag") ? category.GetAttributeValue<string>("wrs_mailchimpcolumntag") : string.Empty;
                        Categories.Add(categoryCode, mailchimpcolumntag);
                    }
                }
                return Categories;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static void CreateMailChimpColumn(string columnName, out string tag)
        {
            Options options = new Options();
            options.choices = new List<string>(new string[] { "Yes", "No" });

            MergeColumn column = new MergeColumn();
            column.default_value = "Yes";
            column.name = columnName;
            column.@public = true;
            column.tag = "";
            column.type = "radio";
            column.options = options;
            string[] apikeys = MailChimpApikey.ToString().Split('-');
            string url = string.Format(POSTMergeFieldsURL, apikeys[1], MailChimpListId);
            var client = new RestClient(url);
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddHeader("authorization", "Basic " + MailChimpApikey);

            request.AddParameter("application/json", JsonConvert.SerializeObject(column), ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(response.Content)))
            {
                // Deserialization from JSON  
                DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(RootObject));
                RootObject bsObj2 = (RootObject)deserializer.ReadObject(ms);
                tag = bsObj2.tag;
            }
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

        public UpdateRequest CreateUpdateRequest(Entity target)
        {
            return new UpdateRequest()
            {
                Target = target
            };

        }

        public Contact PrepareCustomerObject(DataRow dr)
        {
            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {
                Entity accountToBeCreated = new Entity(Contact.EntityLogicalName, "wrs_id", int.Parse(dr["Id"].ToString()));
                Contact accountToBeCreatedFromDB = accountToBeCreated.ToEntity<Contact>();
                if (dr["FirstName"] != DBNull.Value)
                    accountToBeCreatedFromDB.FirstName = dr["FirstName"].ToString();

                accountToBeCreatedFromDB.wrs_id = int.Parse(dr["Id"].ToString());
                if (dr["AccountNumber"] != DBNull.Value)
                    accountToBeCreatedFromDB.wrs_accountnumber = dr["AccountNumber"].ToString();

                if (dr["LastName"] != DBNull.Value)
                    accountToBeCreatedFromDB.LastName = dr["LastName"].ToString();

                if (dr["Gender"] != DBNull.Value && dr["Gender"].ToString() != "")
                {
                    if (dr["Gender"].ToString() == "M" || dr["Gender"].ToString() == "Male")
                        accountToBeCreatedFromDB.GenderCode = new OptionSetValue(1);
                    else if (dr["Gender"].ToString() == "F" || dr["Gender"].ToString() == "Female")
                        accountToBeCreatedFromDB.GenderCode = new OptionSetValue(2);
                }
                if (dr["DateOfBirth"] != DBNull.Value)
                    accountToBeCreatedFromDB.BirthDate = (DateTime.Parse(dr["DateOfBirth"].ToString()));
                accountToBeCreatedFromDB.wrs_sourcefrom = "NC";
                if (dr["Username"] != DBNull.Value)
                    accountToBeCreatedFromDB.wrs_username = dr["Username"].ToString();

                if (dr["Email"] != DBNull.Value)
                    accountToBeCreatedFromDB.EMailAddress1 = dr["Email"].ToString();

                if (dr["EmailToRevalidate"] != DBNull.Value)
                    accountToBeCreatedFromDB.wrs_emailtorevalidate = dr["EmailToRevalidate"].ToString();

                if (dr["AdminComment"] != DBNull.Value)
                    accountToBeCreatedFromDB.wrs_admincomment = dr["AdminComment"].ToString();

                if (dr["IsTaxExempt"] != DBNull.Value)
                    accountToBeCreatedFromDB.wrs_istaxexempt = (bool)dr["IsTaxExempt"];

                if (dr["AffiliateId"] != DBNull.Value)
                    accountToBeCreatedFromDB.wrs_affiliatedid = int.Parse(dr["AffiliateId"].ToString());

                if (dr["VendorId"] != DBNull.Value)
                    accountToBeCreatedFromDB.wrs_vendorid = int.Parse(dr["VendorId"].ToString());

                if (dr["HasShoppingCartItems"] != DBNull.Value)
                    accountToBeCreatedFromDB.wrs_hasshoppingcartitems = (bool)dr["HasShoppingCartItems"];

                if (dr["RequireReLogin"] != DBNull.Value)
                    accountToBeCreatedFromDB.wrs_requirerelogin = (bool)dr["RequireReLogin"];

                if (dr["FailedLoginAttempts"] != DBNull.Value)
                    accountToBeCreatedFromDB.wrs_failedloginattempts = int.Parse(dr["FailedLoginAttempts"].ToString());

                if (dr["CannotLoginUntilDateUtc"].ToString() != string.Empty)
                    accountToBeCreatedFromDB.wrs_cannotloginuntildate = (DateTime?)dr["CannotLoginUntilDateUtc"];

                accountToBeCreatedFromDB.StateCode = (((dr["Active"]) != DBNull.Value) && (dr["Active"]).Equals(true)) ? ContactState.Active : ContactState.Inactive;

                accountToBeCreatedFromDB.wrs_deleted = dr["Deleted"] != DBNull.Value ? (bool)dr["Deleted"] : false;

                if (dr["IsSystemAccount"] != DBNull.Value)
                    accountToBeCreatedFromDB.wrs_issystemaccount = (bool)dr["IsSystemAccount"];

                if (dr["SystemName"] != DBNull.Value)
                    accountToBeCreatedFromDB.wrs_systemname = dr["SystemName"].ToString();

                if (dr["LastIpAddress"] != DBNull.Value)
                    accountToBeCreatedFromDB.wrs_lastipaddress = dr["LastIpAddress"].ToString();

                //if (dr["CreatedOnUtc"] != DBNull.Value)
                //    accountToBeCreatedFromDB.OverriddenCreatedOn = (DateTime?)dr["CreatedOnUtc"];

                if (dr["LastLoginDateUtc"] != DBNull.Value)
                    accountToBeCreatedFromDB.wrs_lastlogindate = (DateTime?)dr["LastLoginDateUtc"];

                if (dr["LastActivityDateUtc"] != DBNull.Value)
                    accountToBeCreatedFromDB.wrs_lastactivitydate = (DateTime?)dr["LastActivityDateUtc"];

                if (dr["RegisteredInStoreId"] != DBNull.Value && dr["RegisteredInStoreId"].ToString() != string.Empty)
                    accountToBeCreatedFromDB.wrs_registeredinstoreid = new EntityReference(wrs_store.EntityLogicalName, "wrs_id", int.Parse(dr["RegisteredInStoreId"].ToString()));

                //Billing Address
                if (dr["CityBill"] != DBNull.Value)
                    accountToBeCreatedFromDB.Address1_City = dr["CityBill"].ToString();

                if (dr["ZipPostalCodeBill"] != DBNull.Value)
                    accountToBeCreatedFromDB.Address1_PostalCode = dr["ZipPostalCodeBill"].ToString();

                if (dr["FaxNumberBill"] != DBNull.Value)
                    accountToBeCreatedFromDB.Address1_Fax = dr["FaxNumberBill"].ToString();

                if (dr["PhoneNumberBill"] != DBNull.Value)
                    accountToBeCreatedFromDB.Address1_Telephone1 = dr["PhoneNumberBill"].ToString();

                if (dr["Address1Bill"] != DBNull.Value)
                    accountToBeCreatedFromDB.Address1_Line1 = dr["Address1Bill"].ToString();

                if (dr["Address2Bill"] != DBNull.Value)
                    accountToBeCreatedFromDB.Address1_Line2 = dr["Address2Bill"].ToString();

                if (dr["CountryIdBill"] != DBNull.Value && dr["CountryIdBill"].ToString() != string.Empty)
                    accountToBeCreatedFromDB.wrs_address1country = new EntityReference(wrs_country.EntityLogicalName, "wrs_id", int.Parse(dr["CountryIdBill"].ToString()));

                if (dr["StateProvinceIdBill"] != DBNull.Value && dr["StateProvinceIdBill"].ToString() != string.Empty)
                    accountToBeCreatedFromDB.wrs_address1stateprovince = new EntityReference(wrs_stateregion.EntityLogicalName, "wrs_id", int.Parse(dr["StateProvinceIdBill"].ToString()));

                if (dr["BillingAddress_Id"] != DBNull.Value)
                    accountToBeCreatedFromDB.Address1_AddressTypeCode = new OptionSetValue((int)Contact.OptionSetEnums.address1_addresstypecode.Bill_To);

                //Shipping Address
                if (dr["CityShip"] != DBNull.Value)
                    accountToBeCreatedFromDB.Address2_City = dr["CityShip"].ToString();

                if (dr["ZipPostalCodeShip"] != DBNull.Value)
                    accountToBeCreatedFromDB.Address2_PostalCode = dr["ZipPostalCodeShip"].ToString();

                if (dr["FaxNumberShip"] != DBNull.Value)
                    accountToBeCreatedFromDB.Address2_Fax = dr["FaxNumberShip"].ToString();

                if (dr["PhoneNumberShip"] != DBNull.Value)
                    accountToBeCreatedFromDB.Address2_Telephone1 = dr["PhoneNumberShip"].ToString();

                if (dr["Address1Ship"] != DBNull.Value)
                    accountToBeCreatedFromDB.Address2_Line1 = dr["Address1Ship"].ToString();

                if (dr["Address2Ship"] != DBNull.Value)
                    accountToBeCreatedFromDB.Address2_Line2 = dr["Address2Ship"].ToString();

                if (dr["CountryIdShip"] != DBNull.Value && dr["CountryIdShip"].ToString() != string.Empty)
                    accountToBeCreatedFromDB.wrs_address2country = new EntityReference(wrs_country.EntityLogicalName, "wrs_id", int.Parse(dr["CountryIdShip"].ToString()));

                if (dr["StateProvinceIdShip"] != DBNull.Value && dr["StateProvinceIdShip"].ToString() != string.Empty)
                    accountToBeCreatedFromDB.wrs_address2stateprovince = new EntityReference(wrs_stateregion.EntityLogicalName, "wrs_id", int.Parse(dr["StateProvinceIdShip"].ToString()));

                if (dr["ShippingAddress_Id"] != DBNull.Value)
                    accountToBeCreatedFromDB.Address2_AddressTypeCode = new OptionSetValue((int)Contact.OptionSetEnums.address1_addresstypecode.Ship_To);

                return accountToBeCreatedFromDB;
            }
            return null;
        }

        public Contact PrepareCustomerObjectGenericAttr(DataRow dr)
        {
            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {
                Entity accountToBeCreated = new Entity(Contact.EntityLogicalName, "wrs_id", int.Parse(dr["Id"].ToString()));
                Contact accountToBeCreatedFromDB = accountToBeCreated.ToEntity<Contact>();
                accountToBeCreatedFromDB.wrs_id = int.Parse(dr["Id"].ToString());
                if (dr["FirstName"] != DBNull.Value)
                    accountToBeCreatedFromDB.FirstName = dr["FirstName"].ToString();

                if (dr["LastName"] != DBNull.Value)
                    accountToBeCreatedFromDB.LastName = dr["LastName"].ToString();

                if (dr["Gender"] != DBNull.Value && dr["Gender"].ToString() != "")
                {
                    if (dr["Gender"].ToString() == "M" || dr["Gender"].ToString() == "Male")
                        accountToBeCreatedFromDB.GenderCode = new OptionSetValue(1);
                    else if (dr["Gender"].ToString() == "F" || dr["Gender"].ToString() == "Female")
                        accountToBeCreatedFromDB.GenderCode = new OptionSetValue(2);
                }
                if (dr["DateOfBirth"] != DBNull.Value)
                    accountToBeCreatedFromDB.BirthDate = (DateTime.Parse(dr["DateOfBirth"].ToString()));
                accountToBeCreatedFromDB.wrs_sourcefrom = "NC";


                return accountToBeCreatedFromDB;
            }
            return null;
        }

        private Entity PrepareNewsLetterObject(DataRow dr)
        {
            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {
                if (dr["Email"] != DBNull.Value && dr["Email"].ToString() != "")
                {
                    string firstName = string.Empty;
                    string lastName = string.Empty;
                    string email = string.Empty;
                    string fieldTag = string.Empty;
                    string categoryCode = string.Empty;
                    string SourceFrom = string.Empty;
                    string Campaign = string.Empty;
                    Entity NewsLetterToBeCreated = new Entity(wrs_subscription.EntityLogicalName, "wrs_id", dr["Id"].ToString());
                    wrs_subscription NewsLetterToBeCreatedFromDB = NewsLetterToBeCreated.ToEntity<wrs_subscription>();
                    NewsLetterToBeCreated.Attributes.Add("wrs_id", int.Parse(dr["Id"].ToString()));
                    NewsLetterToBeCreated.Attributes.Add("wrs_name", "News Letter #" + dr["Id"].ToString());
                    if (dr["Firstname"] != DBNull.Value)
                    {
                        firstName = dr["Firstname"].ToString();
                        NewsLetterToBeCreated.Attributes.Add("wrs_firstname", dr["Firstname"].ToString());
                    }
                    if (dr["Lastname"] != DBNull.Value)
                    {
                        lastName = dr["Lastname"].ToString();
                        NewsLetterToBeCreated.Attributes.Add("wrs_lastname", dr["Lastname"].ToString());
                    }
                    if (dr["Email"] != DBNull.Value)
                    {
                        email = dr["Email"].ToString();
                        NewsLetterToBeCreated.Attributes.Add("wrs_email", dr["Email"].ToString());
                    }
                    if (dr["Source"] != DBNull.Value)
                    {
                        NewsLetterToBeCreated.Attributes.Add("wrs_sourcefrom", dr["Source"].ToString());
                        SourceFrom = dr["Source"].ToString();
                    }

                    if (dr["Category"] != DBNull.Value)
                    {
                        categoryCode = dr["Category"].ToString().ToLower();
                        NewsLetterToBeCreated.Attributes.Add("wrs_categorycode", dr["Category"].ToString());
                        if (!CategoryCodes.ContainsKey(dr["Category"].ToString().ToLower()))
                        {
                            string description = string.Empty;
                            description = description + (dr["Source"] != DBNull.Value ? dr["Source"].ToString() : "");
                            description = description + "-" + (dr["Campaign"] != DBNull.Value ? dr["Campaign"].ToString() : " ");
                            description = description + "-" + (dr["StoreId"] != DBNull.Value ? dr["StoreId"].ToString() : " ");
                            Entity _category = new Entity("wrs_subscriptioncategory");
                            _category.Attributes.Add("wrs_categorycode", dr["Category"].ToString());
                            _category.Attributes.Add("wrs_categorydescription", description);
                            //_category.Attributes.Add("wrs_id", CategoryCodes.Values.Max() + 1);
                            Guid categoryGuid = CrmService.Create(_category);

                            string tag = string.Empty;
                            CreateMailChimpColumn(dr["Category"].ToString(), out tag);
                            CategoryCodes.Add(dr["Category"].ToString().ToLower(), tag);
                            Entity _update = new Entity(_category.LogicalName, categoryGuid);
                            _update.Attributes.Add("wrs_mailchimpcolumntag", tag.ToUpper());
                            CrmService.Update(_update);
                            fieldTag = tag;
                        }
                        else
                        {
                            fieldTag = CategoryCodes[categoryCode];
                        }
                    }
                    if (dr["Campaign"] != DBNull.Value)
                    {
                        NewsLetterToBeCreated.Attributes.Add("wrs_campaign", dr["Campaign"].ToString());
                        Campaign = dr["Campaign"].ToString();
                    }
                    if (dr["Active"] != DBNull.Value && (bool)dr["Active"])
                        NewsLetterToBeCreated.Attributes.Add("wrs_subscriptionstatus", true);
                    if (dr["StoreId"] != DBNull.Value)
                        NewsLetterToBeCreated.Attributes.Add("wrs_storeid", new EntityReference("wrs_store", "wrs_id", dr["StoreId"].ToString()));

                    if (SubscriptionEmails.ContainsKey(dr["Email"].ToString()))
                        NewsLetterToBeCreated.Attributes.Add("wrs_contactid", new EntityReference("contact", SubscriptionEmails[dr["Email"].ToString()]));
                    else
                    {
                        QueryExpression qe = new QueryExpression("contact");
                        qe.Criteria.Conditions.Add(new ConditionExpression("emailaddress1", ConditionOperator.Equal, dr["Email"].ToString()));
                        //qe.Criteria.Conditions.Add(new ConditionExpression("wrs_accountnumber", ConditionOperator.NotNull));
                        //qe.Criteria.Conditions.Add(new ConditionExpression("wrs_id", ConditionOperator.NotNull));
                        qe.ColumnSet = new ColumnSet(new string[] { "wrs_id" });
                        EntityCollection Contcoll = serviceClient().RetrieveMultiple(qe);
                        if (Contcoll.Entities.Count > 0)
                        {
                            NewsLetterToBeCreated.Attributes.Add("wrs_contactid", new EntityReference("contact", Contcoll.Entities[0].Id));
                            SubscriptionEmails.Add(dr["Email"].ToString(), Contcoll.Entities[0].Id);
                        }
                        else
                        {
                            if (dr["Email"] != DBNull.Value)
                            {
                                Entity newContact = new Entity("contact");
                                newContact.Attributes.Add("wrs_username", dr["Email"].ToString());
                                if (dr["Firstname"] != DBNull.Value)
                                    newContact.Attributes.Add("firstname", dr["Firstname"].ToString());
                                if (dr["Lastname"] != DBNull.Value)
                                    newContact.Attributes.Add("lastname", dr["Lastname"].ToString());
                                if (dr["Email"] != DBNull.Value)
                                    newContact.Attributes.Add("emailaddress1", dr["Email"].ToString());

                                if (dr["Source"] != DBNull.Value)
                                    newContact.Attributes.Add("wrs_sourcefrom", dr["Source"].ToString());
                                Guid contactId = CrmService.Create(newContact);
                                NewsLetterToBeCreated.Attributes.Add("wrs_contactid", new EntityReference("contact", contactId));
                                SubscriptionEmails.Add(dr["Email"].ToString(), contactId);
                            }
                        } /**/
                    }

                    if (!string.IsNullOrEmpty(fieldTag) && !string.IsNullOrEmpty(email))
                        UpsertMailChimp(int.Parse(dr["Id"].ToString()), firstName, lastName, email, fieldTag, Campaign, SourceFrom, string.Empty, string.Empty);

                    return NewsLetterToBeCreatedFromDB;
                }
                return null;
            }
            return null;
        }

        private void UpsertMailChimp(int NCID, string firstName, string lastName, string email, string fieldTag, string campaign, string sourcefrom, string purchaseDate, string ticketDate)
        {
            string hashEMail = CalculateMD5Hash(email.ToLower());
            dynamic memberItem = new ExpandoObject();
            memberItem.email_address = email;
            memberItem.merge_fields = new ExpandoObject();
            if (!string.IsNullOrEmpty(fieldTag))
                ((IDictionary<String, Object>)memberItem.merge_fields)[fieldTag.ToUpper()] = "Yes";
            if (!string.IsNullOrEmpty(firstName))
                memberItem.merge_fields.FNAME = firstName;
            if (!string.IsNullOrEmpty(lastName))
                memberItem.merge_fields.LNAME = lastName;
            if (!string.IsNullOrEmpty(sourcefrom))
                memberItem.merge_fields.SOURCE3 = sourcefrom;
            if (!string.IsNullOrEmpty(campaign))
                memberItem.merge_fields.CAMPAIGN = campaign;
            if (!string.IsNullOrEmpty(purchaseDate))
                memberItem.merge_fields.LMPDATE = Convert.ToDateTime(purchaseDate);
            if (!string.IsNullOrEmpty(ticketDate))
                memberItem.merge_fields.LMTDATE = Convert.ToDateTime(ticketDate);
            //if (NCID > 0)
            //    memberItem.merge_fields.NCID = NCID;
            memberItem.status = "subscribed";

            string[] apikeys = MailChimpApikey.ToString().Split('-');
            string url = string.Format(PUTMemberListURL, apikeys[1], MailChimpListId, hashEMail.ToLower());

            var client = new RestClient(url);
            var request = new RestRequest(Method.PUT);
            request.AddHeader("content-type", "application/json");
            request.AddHeader("authorization", "Basic " + MailChimpApikey);
            request.AddParameter("application/json", JsonConvert.SerializeObject(memberItem), ParameterType.RequestBody);
            try
            {
                IRestResponse response = client.Execute(request);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public string CalculateMD5Hash(string input)

        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
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

    public enum SubscriptionFrom
    {
        MailChimp = 167320000,
        CRM = 167320001,
        NC = 167320002
    }

    public class Options
    {
        public List<string> choices { get; set; }
    }

    public class MergeColumn
    {
        public string tag { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string default_value { get; set; }
        public bool @public { get; set; }
        public Options options { get; set; }
    }

    public class Link
    {
        public string rel { get; set; }
        public string href { get; set; }
        public string method { get; set; }
        public string targetSchema { get; set; }
        public string schema { get; set; }
    }

    public class RootObject
    {
        public int merge_id { get; set; }
        public string tag { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public bool required { get; set; }
        public string default_value { get; set; }
        public bool @public { get; set; }
        public int display_order { get; set; }
        public Options options { get; set; }
        public string help_text { get; set; }
        public string list_id { get; set; }
        public List<Link> _links { get; set; }
    }

    [XmlRoot(ElementName = "ProductAttributeValue")]
    public class ProductAttributeValue
    {
        [XmlElement(ElementName = "Value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "ProductAttribute")]
    public class ProductAttribute
    {
        [XmlElement(ElementName = "ProductAttributeValue")]
        public ProductAttributeValue ProductAttributeValue { get; set; }
        [XmlAttribute(AttributeName = "ID")]
        public string ID { get; set; }
    }

    [XmlRoot(ElementName = "Attributes")]
    public class Attributes
    {
        [XmlElement(ElementName = "ProductAttribute")]
        public List<ProductAttribute> ProductAttribute { get; set; }
    }
}