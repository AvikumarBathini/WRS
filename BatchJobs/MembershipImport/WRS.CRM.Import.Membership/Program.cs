using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WRS.CRM.Import.Membership
{
    class Program
    {
        public static IOrganizationService CrmService = CrmServiceClient();
        static int batchSize = int.Parse(ConfigurationManager.AppSettings["BatchSize"]);
        static string onetimeImport = ConfigurationManager.AppSettings["OneTimeImport"];
        private const string NCId = "9999";

        static void Main(string[] args)
        {

            UpsertMembershipPasses();

            UpsertMembershipBooking();
        }

        private static void UpsertMembershipPassCoMember(DataTable table)
        {
            Program p = new Program();
            if (table != null && table.Rows.Count > 0)
            {
                int requests = (table.Rows.Count / batchSize) + 1;
                string primaryKeyLogicalName = "wrs_id";
                for (int i = 0; i < requests; i++)
                {
                    EntityCollection _lisEntityCollection = GetEntityCollection("PassesCoMember", i, table, PrepareMembershipPassCoMemObject);
                    if (_lisEntityCollection != null && _lisEntityCollection.Entities.Count > 0)
                    {
                        try
                        {
                            CrmExecuteMultiple(_lisEntityCollection, "PassesCoMember");
                        }
                        catch (Exception e)
                        {
                            string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','" + primaryKeyLogicalName + "','Genneric'";
                            WriteLogsToDB(errorLog);
                            continue;
                        }
                    }
                }
            }
        }

        private static DataTable RetrieveRecordsFromDB(string storeProc, DataTable dt = null)
        {
            DataTable DBTable = new DataTable();
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQL"].ConnectionString))
            using (var command = new SqlCommand(storeProc, conn))
            using (var da = new SqlDataAdapter(command))
            {
                try
                {

                    command.CommandType = CommandType.StoredProcedure;
                    if (dt != null)
                    {
                        SqlParameter tvparam = command.Parameters.AddWithValue("@Passes", dt);
                        tvparam.SqlDbType = SqlDbType.Structured;
                    }
                    da.Fill(DBTable);
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
            }
            return DBTable;
        }

        private static void UpsertMembershipBooking()
        {
            DataTable table = RetrieveRecordsFromDB("CrmGetMembershipsBooking");
            if (table != null && table.Rows.Count > 0)
            {
                int requests = (table.Rows.Count / batchSize) + 1; //Maximum parallel requests
                string primaryKeyLogicalName = "wrs_id";
                for (int i = 0; i < requests; i++)
                {
                    EntityCollection _lisEntityCollection = GetEntityCollection("MembershipBooking", i, table, PrepareMembershipBookingObject);
                    if (_lisEntityCollection != null && _lisEntityCollection.Entities.Count > 0)
                    {
                        try
                        {
                            CrmExecuteMultiple(_lisEntityCollection, "MembershipBooking");
                        }
                        catch (Exception e)
                        {
                            string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','" + primaryKeyLogicalName + "','Genneric'";
                            WriteLogsToDB(errorLog);
                            continue;
                        }
                    }
                }
            }
        }

        private static void UpsertMembershipPasses()
        {
            DataTable table = RetrieveRecordsFromDB("CrmGetPasses");
            if (table != null && table.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "wrs_id";
                int n = 0;
                int requests = (table.Rows.Count / 100) + 1; //Maximum parallel requests
                List<DataTable> _listTables = new List<DataTable>();
                for (int i = 0; i < requests; i++)
                {
                    EntityCollection _lisEntityCollection = GetEntityCollection("Passes", i, table, PrepareMembershipPassObject);
                    if (_lisEntityCollection != null && _lisEntityCollection.Entities.Count > 0)
                    {
                        try
                        {
                            CrmExecuteMultiple(_lisEntityCollection, "Passes");
                        }
                        catch (Exception e)
                        {
                            string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','" + primaryKeyLogicalName + "','Genneric'";
                            WriteLogsToDB(errorLog);
                            continue;
                        }
                    }
                }
            }
        }

        private static EntityCollection GetEntityCollection(string tableName, int sequence, DataTable table, Func<DataRow, Entity> myMethod)
        {
            EntityCollection _lisEntityCollection = new EntityCollection();
            int start = 100 * sequence;
            int end = (100 * (sequence + 1)) + 1;

            for (int j = start; j < end; j++)
            {
                if (j < table.Rows.Count)
                {
                    DataRow dr = table.Rows[j];
                    try
                    {
                        Entity _record = myMethod(dr);
                        if (_record != null)
                            _lisEntityCollection.Entities.Add(_record);
                    }
                    catch (Exception e)
                    {
                        string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','','" + tableName + "','" + dr["Id"].ToString() + "'";
                        WriteLogsToDB(errorLog);
                        continue;
                    }
                }
                else
                {
                    break;
                }
            }
            return _lisEntityCollection;
        }

        private static Entity PrepareMembershipBookingObject(DataRow dr)
        {

            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {
                Entity entity = new Entity("wrs_membershipbooking", "wrs_id", int.Parse(dr["Id"].ToString()));

                entity["wrs_id"] = int.Parse(dr["Id"].ToString());

                if (dr["CustomerId"] != DBNull.Value)
                    entity["wrs_customerid"] = new EntityReference("contact", "wrs_id", int.Parse(dr["CustomerId"].ToString()));

                if (dr["TicketType"] != DBNull.Value)
                    entity["wrs_tickettype"] = dr["TicketType"].ToString();

                if (dr["VisitDate"] != DBNull.Value)
                    entity["wrs_visitdate"] = DateTime.Parse(dr["VisitDate"].ToString());

                entity["wrs_migratedon"] = DateTime.Now;

                if (dr["SendDate"] != DBNull.Value)
                    entity["wrs_senddate"] = DateTime.Parse(dr["SendDate"].ToString());

                if (dr["RequestDate"] != DBNull.Value)
                    entity["wrs_requestdate"] = DateTime.Parse(dr["RequestDate"].ToString());

                if (dr["Timing"] != DBNull.Value)
                    entity["wrs_timing"] = dr["Timing"].ToString();

                if (dr["AdultQty"] != DBNull.Value)
                    entity["wrs_adultqty"] = int.Parse(dr["AdultQty"].ToString());

                if (dr["ChildQty"] != DBNull.Value)
                    entity["wrs_childqty"] = int.Parse(dr["ChildQty"].ToString());

                if (dr["Deleted"] != DBNull.Value)
                    entity["wrs_deleted"] = false;

                if (dr["Redeemed"] != DBNull.Value)
                    entity["wrs_redeemed"] = false;

                if (dr["ProductId"] != DBNull.Value)
                    entity["wrs_productid"] = new EntityReference("product", "wrs_id", int.Parse(dr["ProductId"].ToString()));

                if (dr["EventId"] != DBNull.Value)
                    entity["wrs_eventid"] = int.Parse(dr["EventId"].ToString());

                return entity;
            }
            return null;
        }

        private static Entity PrepareMembershipPassObject(DataRow dr)
        {

            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {
                Entity entity = new Entity("wrs_membership", "wrs_id", int.Parse(dr["Id"].ToString()));
                try
                {
                    entity["wrs_id"] = int.Parse(dr["Id"].ToString());
                    entity["wrs_migratedon"] = DateTime.Now;

                    if (dr["PassNo"] != DBNull.Value)
                        entity["wrs_name"] = dr["PassNo"].ToString();


                    #region Contact
                    if (dr["Email"] != DBNull.Value)
                    {
                        Guid contactId = Guid.Empty;
                        QueryExpression qe = new QueryExpression("contact");
                        FilterExpression filter = new FilterExpression(LogicalOperator.Or);
                        filter.AddCondition("emailaddress1", ConditionOperator.Equal, dr["Email"].ToString());
                        filter.AddCondition("wrs_id", ConditionOperator.Equal, int.Parse(dr["CustomerId"].ToString()));
                        qe.Criteria.AddFilter(filter);
                        qe.ColumnSet = new ColumnSet(new string[] { "wrs_id", "emailaddress1" });
                        EntityCollection Contcoll = CrmService.RetrieveMultiple(qe);
                        Entity accountDetail = null;
                        Entity memberDetail = null;
                        if (Contcoll != null && Contcoll.Entities.Count > 0)
                        {
                            accountDetail = Contcoll.Entities.Where(a => a.Contains("wrs_id") && a["wrs_id"].ToString() == int.Parse(dr["CustomerId"].ToString()).ToString()).FirstOrDefault();
                            memberDetail = Contcoll.Entities.Where(a => a.Contains("emailaddress1") && a["emailaddress1"].ToString() == dr["Email"].ToString()).FirstOrDefault();
                        }
                        if (accountDetail != null)
                        {
                            entity["wrs_loggedincustomerid"] = new EntityReference("contact", accountDetail.Id);
                        }
                        else
                        {
                            return null;
                        }
                        if (memberDetail != null)
                        {
                            entity["wrs_customerid"] = new EntityReference("contact", memberDetail.Id);
                        }
                        else
                        {
                            Entity newContact = new Entity("contact");
                            if (dr["Firstname"] != DBNull.Value)
                                newContact.Attributes.Add("firstname", dr["Firstname"].ToString());

                            if (dr["Lastname"] != DBNull.Value)
                                newContact.Attributes.Add("lastname", dr["Lastname"].ToString());

                            if (dr["Email"] != DBNull.Value)
                            {
                                newContact.Attributes.Add("emailaddress1", dr["Email"].ToString());
                                newContact.Attributes.Add("wrs_username", dr["Email"].ToString());
                            }

                            if (dr["Street1"] != DBNull.Value)
                                newContact.Attributes.Add("address1_line1", dr["Street1"].ToString());

                            if (dr["Street2"] != DBNull.Value)
                                newContact.Attributes.Add("address1_line2", dr["Street2"].ToString());

                            if (dr["City"] != DBNull.Value)
                                newContact.Attributes.Add("address1_city", dr["City"].ToString());

                            if (dr["State"] != DBNull.Value)
                                newContact.Attributes.Add("address1_stateorprovince", dr["State"].ToString());

                            if (dr["Zip"] != DBNull.Value)
                                newContact.Attributes.Add("address1_postalcode", dr["Zip"].ToString());

                            if (dr["Phone"] != DBNull.Value)
                                newContact.Attributes.Add("telephone1", dr["Phone"].ToString());

                            if (dr["DateOfBirth"] != DBNull.Value)
                                newContact["birthdate"] = DateTime.Parse(dr["DateOfBirth"].ToString());

                            //newContact["wrs_sourcefrom"] = "NC";
                            contactId = CrmService.Create(newContact);

                            entity["wrs_customerid"] = new EntityReference("contact", contactId);
                        }
                    }

                    #endregion

                    #region MemberShip Pass
                    if (dr["Firstname"] != DBNull.Value)
                        entity.Attributes.Add("wrs_firstname", dr["Firstname"].ToString());

                    if (dr["Email"] != DBNull.Value)
                        entity.Attributes.Add("wrs_mainmemberemail", dr["Email"].ToString());

                    if (dr["Lastname"] != DBNull.Value)
                        entity.Attributes.Add("wrs_lastname", dr["Lastname"].ToString());

                    if (dr["Street1"] != DBNull.Value)
                        entity.Attributes.Add("wrs_street1", dr["Street1"].ToString());

                    if (dr["Street2"] != DBNull.Value)
                        entity.Attributes.Add("wrs_street2", dr["Street2"].ToString());

                    if (dr["City"] != DBNull.Value)
                        entity.Attributes.Add("wrs_city", dr["City"].ToString());

                    if (dr["State"] != DBNull.Value)
                        entity.Attributes.Add("wrs_state", dr["State"].ToString());

                    if (dr["Zip"] != DBNull.Value)
                        entity.Attributes.Add("wrs_postalcode", dr["Zip"].ToString());

                    if (dr["Phone"] != DBNull.Value)
                        entity.Attributes.Add("wrs_phone", dr["Phone"].ToString());

                    if (dr["DateOfBirth"] != DBNull.Value)
                        entity["wrs_birthdate"] = DateTime.Parse(dr["DateOfBirth"].ToString());

                    if (dr["Kind"] != DBNull.Value)
                        entity["wrs_kind"] = int.Parse(dr["Kind"].ToString());

                    if (dr["Parking"] != DBNull.Value)
                        entity["wrs_parking"] = dr["Parking"].ToString();

                    if (dr["CarPlate"] != DBNull.Value)
                        entity["wrs_carplate"] = dr["CarPlate"].ToString();

                    if (dr["IU"] != DBNull.Value)
                        entity["wrs_iu"] = dr["IU"].ToString();

                    if (dr["ValidFrom"] != DBNull.Value)
                        entity["wrs_validfrom"] = DateTime.Parse(dr["ValidFrom"].ToString());

                    if (dr["ValidUntil"] != DBNull.Value)
                        entity["wrs_validuntil"] = DateTime.Parse(dr["ValidUntil"].ToString());

                    if (dr["VisualId"] != DBNull.Value)
                        entity["wrs_visualid"] = dr["VisualId"].ToString();

                    if (dr["PLU"] != DBNull.Value)
                        entity["wrs_plu"] = dr["PLU"].ToString();

                    if (dr["ContactId"] != DBNull.Value)
                        entity["wrs_contactid"] = int.Parse(dr["ContactId"].ToString());

                    if (dr["CategoryType"] != DBNull.Value)
                        entity["wrs_categorytype"] = dr["CategoryType"].ToString();

                    if (dr["GalaxyLastUpdate"] != DBNull.Value)
                        entity["wrs_galaxylastupdate"] = DateTime.Parse(dr["GalaxyLastUpdate"].ToString());

                    if (dr["ParkingPushedDate"] != DBNull.Value)
                        entity["wrs_parkingpusheddate"] = DateTime.Parse(dr["ParkingPushedDate"].ToString());

                    if (dr["AdultQty"] != DBNull.Value)
                        entity["wrs_adultqty"] = int.Parse(dr["AdultQty"].ToString());

                    if (dr["ChildQty"] != DBNull.Value)
                        entity["wrs_childqty"] = int.Parse(dr["ChildQty"].ToString());

                    //if (dr["PictureId"] != DBNull.Value)
                    //    entity["wrs_id"] = int.Parse(dr["PictureId"].ToString());

                    if (dr["ItemName"] != DBNull.Value)
                        entity["wrs_itemname"] = dr["ItemName"].ToString();

                    if (dr["GalaxyOrderNo"] != DBNull.Value)
                        entity["wrs_galaxyorderno"] = int.Parse(dr["GalaxyOrderNo"].ToString());

                    //if (dr["Status"] != DBNull.Value)
                    //    entity["wrs_id"] = int.Parse(dr["Status"].ToString());

                    if (dr["GalaxyInfoPushedOnUtc"] != DBNull.Value)
                    {
                        DateTime dt = (DateTime)dr["GalaxyInfoPushedOnUtc"];
                        DateTime convertedDate = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        entity["wrs_galaxyinfopushedonutc"] = convertedDate.ToLocalTime();
                    }

                    if (dr["GalaxyPicPushedOnUtc"] != DBNull.Value)
                    {
                        DateTime dt = (DateTime)dr["GalaxyPicPushedOnUtc"];
                        DateTime convertedDate = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        entity["wrs_galaxypicpushedonutc"] = convertedDate.ToLocalTime();
                    }

                    if (dr["RenewalDateOnUtc"] != DBNull.Value)
                    {
                        DateTime dt = (DateTime)dr["RenewalDateOnUtc"];
                        DateTime convertedDate = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        entity["wrs_renewaldateonutc"] = convertedDate.ToLocalTime();
                    }

                    if (dr["IdentificationNo"] != DBNull.Value)
                        entity["wrs_identificationno"] = dr["IdentificationNo"].ToString();


                    if (dr["MemberSubmittedProfileOnUtc"] != DBNull.Value)
                    {
                        DateTime dt = (DateTime)dr["MemberSubmittedProfileOnUtc"];
                        DateTime convertedDate = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        entity["wrs_membersubmittedprofileonutc"] = convertedDate.ToLocalTime();
                    }

                    if (dr["MemberSubmittedPictureOnUtc"] != DBNull.Value)
                    {
                        DateTime dt = (DateTime)dr["MemberSubmittedPictureOnUtc"];
                        DateTime convertedDate = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        entity["wrs_membersubmittedpiconutc"] = convertedDate.ToLocalTime();
                    }

                    if (dr["DeliveryType"] != DBNull.Value)
                        entity["wrs_deliverytype"] = int.Parse(dr["DeliveryType"].ToString());

                    //if (dr["Master"] != DBNull.Value)
                    //    entity["wrs_id"] = int.Parse(dr["Master"].ToString());

                    if (dr["CreatedOnUtc"] != DBNull.Value)
                    {
                        DateTime dt = (DateTime)dr["CreatedOnUtc"];
                        DateTime convertedDate = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        entity["overriddencreatedon"] = convertedDate.ToLocalTime();
                    }

                    try
                    {
                        if (dr["PictureBinary"] != DBNull.Value)
                            entity["entityimage"] = dr["PictureBinary"];
                    }
                    catch (Exception e)
                    {
                        if (e.Message.Contains("System.OutOfMemoryException"))
                        {
                            return entity;
                        }
                    }
                    return entity;
                    #endregion

                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("System.OutOfMemoryException"))
                    {
                        return entity;
                    }
                    return null;
                }
            }
            return null;
        }

        private static Entity PrepareMembershipPassCoMemObject(DataRow dr)
        {
            if (dr["PassesId"] != DBNull.Value && int.Parse(dr["PassesId"].ToString()) > 0 &&
                dr["wrs_id"] != DBNull.Value && int.Parse(dr["wrs_id"].ToString()) > 0)
            {

                Entity comember = new Entity("wrs_membershipcomember", "wrs_id", int.Parse(dr["Id"].ToString()));

                comember["wrs_id"] = int.Parse(dr["Id"].ToString());

                if (dr["FirstName"] != DBNull.Value)
                    comember.Attributes.Add("wrs_name", dr["Firstname"].ToString());

                if (dr["LastName"] != DBNull.Value)
                    comember.Attributes.Add("wrs_lastname", dr["LastName"].ToString());

                if (dr["DateOfBirth"] != DBNull.Value)
                {
                    DateTime Dob = DateTime.Parse(dr["DateOfBirth"].ToString());
                    int Years = new DateTime(DateTime.Now.Subtract(Dob).Ticks).Year - 1;
                    comember.Attributes.Add("wrs_dateofbirth", Dob);
                    comember.Attributes.Add("wrs_age", Years.ToString());
                }

                comember.Attributes.Add("wrs_migratedon", DateTime.Now);


                if (dr["AgeGroup"] != DBNull.Value)
                    comember.Attributes.Add("wrs_agegroup", dr["AgeGroup"].ToString());

                if (dr["Email"] != DBNull.Value)
                    comember.Attributes.Add("wrs_email", dr["Email"].ToString());

                if (dr["Gender"] != DBNull.Value)
                {
                    int g = -1;
                    string gender = dr["Gender"].ToString();
                    switch (gender)
                    {
                        case "0": g = 1; break;
                        case "1": g = 2; break;
                        default: break;
                    }
                    if (g > -1)
                    {
                        comember.Attributes.Add("wrs_gender", new OptionSetValue(g));
                    }
                }

                comember["wrs_membershippassid"] = new EntityReference("wrs_membership", "wrs_id", int.Parse(dr["PassesId"].ToString()));

                return comember;
            }
            return null;
        }

        private static void UpdateSyncStatus(string entityName, string wrs_id)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQL"].ConnectionString))
            using (var update_item = new SqlCommand("CrmUpdateAuditLogSyncStatus", conn))
            {
                update_item.CommandType = CommandType.StoredProcedure;
                update_item.Parameters.Add("@EntityId", SqlDbType.NVarChar).Value = wrs_id.ToString();
                update_item.Parameters.Add("@EntityName", SqlDbType.NVarChar).SqlValue = entityName;
                conn.Open();
                update_item.ExecuteNonQuery();
                conn.Close();
            }
        }

        public static IOrganizationService CrmServiceClient()
        {
            try
            {
                CrmServiceClient crmConnD = null;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                crmConnD = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRMD"].ConnectionString);
                crmConnD.OrganizationServiceProxy.Timeout = new TimeSpan(0, 20, 0);
                IOrganizationService crmServiceD = crmConnD.OrganizationServiceProxy;
                return crmServiceD;
            }
            catch (Exception ex)
            {

                throw;
            }
            return null;
        }

        private static UpsertRequest CreateUpsertRequest(Entity target)
        {
            return new UpsertRequest()
            {
                Target = target
            };

        }

        private static DataTable RetrieveRecordsFromDB(string storeProc)
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
                    string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','SP','SP_ID'";
                    WriteLogsToDB(errorLog);
                }
            }
            return DBTable;
        }

        private static void CrmExecuteMultiple(EntityCollection ecoll, string table)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("PassId");

            string mainSuccessLog = string.Empty;
            List<string> passIds = new List<string>();
            int i = 0;
            try
            {
                foreach (Entity e in ecoll.Entities)
                {
                    i++;
                    string wrs_id = e["wrs_id"].ToString();
                    try
                    {
                        var resp = CrmService.Execute(CreateUpsertRequest(e));
                        dt.Rows.Add(int.Parse(wrs_id));
                        UpdateSyncStatus(table, wrs_id);
                    }
                    catch (Exception ex)
                    {
                        string errorLog = "('" + DateTime.Now + "','" + ex.Message.Replace("'", "''") + "','" + (ex.StackTrace != null ? ex.StackTrace.Replace("'", "''") : " ") + "','" + table + "','" + wrs_id + "'";
                        WriteLogsToDB(errorLog);
                        continue;
                    }
                }
                if (table == "Passes")
                {
                    DataTable OrderItemTable = RetrieveRecordsFromDB("CrmGetPassCoMember", dt);
                    UpsertMembershipPassCoMember(OrderItemTable);
                }
            }
            catch (Exception e)
            {
                string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','" + table + "','" + NCId + "'";
                WriteLogsToDB(errorLog);
            }
        }

        private static void WriteLogsToDB(string insertQuery)
        {
            if (insertQuery != string.Empty)
            {
                string sql = String.Empty;
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

    }
}
