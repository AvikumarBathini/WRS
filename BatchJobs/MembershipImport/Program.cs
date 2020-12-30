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

namespace MembershipImport
{
    class Program
    {
        public static IOrganizationService CrmService = CrmServiceClient();
        static int batchSize = int.Parse(ConfigurationManager.AppSettings["BatchSize"]);
        static string onetimeImport = ConfigurationManager.AppSettings["OneTimeImport"];
        private string NCId;

        static void Main(string[] args)
        {
            string alternateKeyLogicalName = "wrs_id";

            string MembershipBookingStoreProcName = onetimeImport.ToLower() == "no" ? "CrmGetMembershipsBooking" : "CrmGetMembershipsBooking_OneTime";
            string MembershipPassesStoreProcName = onetimeImport.ToLower() == "no" ? "CrmGetPasses" : "CrmGetPasses_OneTime";
            string MembershipPassesCoMemberStoreProcName = onetimeImport.ToLower() == "no" ? "CrmGetPassCoMember" : "CrmGetPassCoMember_OneTime";

            string MembershipBooking = "MembershipBooking";
            string MembershipPasses = "Passes";
            string MembershipPassCoMem = "PassesCoMember";

            Program p = new Program();

            p.UpsertMembershipPasses(MembershipPasses, p, MembershipPassesStoreProcName, alternateKeyLogicalName);

            p.UpsertMembershipPassCoMember(MembershipPassCoMem, p, MembershipPassesCoMemberStoreProcName, "");

            p.UpsertMembershipBooking(MembershipBooking, p, MembershipBookingStoreProcName, alternateKeyLogicalName);

        }

        private void UpsertMembershipPassCoMember(string Table, Program p, string membershipPassesCoMemberStoreProcName, string alternateKeyLogicalName)
        {
            DataTable table = RetrieveRecordsFromDB(membershipPassesCoMemberStoreProcName);
            if (table != null && table.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "wrs_id";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(table, PrepareMembershipPassCoMemObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        try
                        {
                            p.CrmExecuteMultiple(ec, p, Table, "wrs_membershipcomember", alternateKeyLogicalName, primaryKeyLogicalName);
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

        public DataTable RetrieveRecordsFromDB(string storeProc, DataTable dt = null)
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
                        SqlParameter tvparam = command.Parameters.AddWithValue("@List", dt);
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

        private void UpsertMembershipBooking(string Table, Program p, string MembershipBookingStoreProcName, string alternateKeyLogicalName)
        {
            DataTable table = RetrieveRecordsFromDB(MembershipBookingStoreProcName);
            if (table != null && table.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "wrs_id";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(table, PrepareMembershipBookingObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        try
                        {
                            p.CrmExecuteMultiple(ec, p, Table, "wrs_membershipbooking", alternateKeyLogicalName, primaryKeyLogicalName);
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

        private void UpsertMembershipPasses(string Table, Program p, string MembershipPassesStoreProcName, string alternateKeyLogicalName)
        {
            DataTable table = RetrieveRecordsFromDB(MembershipPassesStoreProcName);
            if (table != null && table.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "wrs_id";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(table, PrepareMembershipPassObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        try
                        {
                            if (ec.Entities.Count > 0)
                                p.CrmExecuteMultiple(ec, p, Table, "wrs_membership", alternateKeyLogicalName, primaryKeyLogicalName);
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
                catch (Exception e)
                {
                    string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','" + table + "',''";
                    WriteLogsToDB(errorLog);
                    continue;
                }
            }
            return _lisEntityCollection;
        }

        private Entity PrepareMembershipBookingObject(DataRow dr)
        {

            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {
                Entity entity = new Entity("wrs_membershipbooking", "wrs_id", int.Parse(dr["Id"].ToString()));

                if (dr["CustomerId"] != DBNull.Value)
                    entity["wrs_customerid"] = new EntityReference("contact", "wrs_id", int.Parse(dr["CustomerId"].ToString()));

                if (dr["TicketType"] != DBNull.Value)
                    entity["wrs_tickettype"] = dr["TicketType"].ToString();

                if (dr["VisitDate"] != DBNull.Value)
                    entity["wrs_visitdate"] = DateTime.Parse(dr["VisitDate"].ToString());

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
                //          M.[Id]
                //,M.[CustomerId]
                //,M.[TicketType]
                //,M.[VisitDate]
                //,M.[SendDate]
                //,M.[RequestDate]
                //,M.[Timing]
                //,M.[AdultQty]
                //,M.[ChildQty]
                //,M.[Deleted]
                //,M.[Redeemed]
                //,M.[ProductId]
                //,M.[EventId]

            }
            return null;
        }

        private Entity PrepareMembershipPassObject(DataRow dr)
        {
            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {
                Entity entity = new Entity("wrs_membership", "wrs_id", int.Parse(dr["Id"].ToString()));
                if (dr["PassNo"] != DBNull.Value)
                    entity["wrs_name"] = dr["PassNo"].ToString();

                #region Contact
                if (dr["Email"] != DBNull.Value)
                {
                    Guid contactId = Guid.Empty;
                    QueryExpression qe = new QueryExpression("contact");
                    qe.Criteria.Conditions.Add(new ConditionExpression("emailaddress1", ConditionOperator.Equal, dr["Email"].ToString()));
                    qe.ColumnSet = new ColumnSet(new string[] { "wrs_id" });
                    EntityCollection Contcoll = CrmService.RetrieveMultiple(qe);
                    if (Contcoll.Entities.Count == 0)
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
                        //if (dr["Source"] != DBNull.Value)
                        //    newContact.Attributes.Add("wrs_sourcefrom", "MEMBERSHIP");

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

                        contactId = CrmService.Create(newContact);
                    }
                    else { contactId = Contcoll.Entities.FirstOrDefault().Id; }
                    if (contactId != Guid.Empty)
                        entity["wrs_customerid"] = new EntityReference("contact", contactId);
                }
                if (dr["CustomerId"] != DBNull.Value)
                {
                    RetrieveRequest req = new RetrieveRequest()
                    {
                        ColumnSet = new ColumnSet("wrs_id"),
                        Target = new EntityReference("contact", "wrs_id", int.Parse(dr["CustomerId"].ToString()))
                    };
                    RetrieveResponse en = (RetrieveResponse)CrmService.Execute(req);
                    if (en != null && en.Results.Count > 0)
                    {
                        Guid id = (Guid)((WRS.Xrm.Contact)(en.Results.Values.FirstOrDefault())).ContactId;
                        if (dr["Email"] == DBNull.Value)
                            entity["wrs_customerid"] = new EntityReference("contact", id);
                        entity["wrs_loggedincustomerid"] = new EntityReference("contact", id);
                    }
                }

                #endregion
                //PictureBinary
                System.Drawing.Image imageIn = System.Drawing.Image.FromFile(@"D:\NCS\Deployments\SyncJobs\InitialImport\InitialImport\MembershipImport\my.jpg");
                using (var ms = new System.IO.MemoryStream())
                {
                    imageIn.Save(ms, imageIn.RawFormat);
                    entity["entityimage"] = ms.ToArray();
                }

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

                if (dr["PictureBinary"] != DBNull.Value)
                    entity["entityimage"] = dr["PictureBinary"];

                return entity;
            }
            return null;
        }

        private Entity PrepareMembershipPassCoMemObject(DataRow dr)
        {
            //      [Id]
            //,[PassesId]
            //,[FirstName]
            //,[LastName]
            //,[DateOfBirth]
            //,[AgeGroup]
            //,[CustContactId]
            //,[CreatedOnUtc]
            //,[UpdatedOnUtc]
            //,[IdentificationNo]
            //,[Gender]
            //,[Email]
            //,[RelationshipTypeId]
            //,[CustomerId]

            if (dr["PassesId"] != DBNull.Value && int.Parse(dr["PassesId"].ToString()) > 0)
            {

                Entity comember = new Entity("wrs_membershipcomember");
                if (dr["Id"] != DBNull.Value)
                    comember.Attributes.Add("wrs_id", int.Parse(dr["Id"].ToString()));

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

        public UpsertRequest CreateUpsertRequest(Entity target)
        {
            return new UpsertRequest()
            {
                Target = target
            };

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
                    string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','SP','SP_ID'";
                    WriteLogsToDB(errorLog);
                }
            }
            return DBTable;
        }

        public DataTable RetrieveRecordsFromDB(string storeProc, string passid)
        {
            SqlParameter p = new SqlParameter("PassID", passid);
            DataTable DBTable = new DataTable();
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQL"].ConnectionString))
            using (var command = new SqlCommand(storeProc, conn))
            using (var da = new SqlDataAdapter(command))
            {
                try
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(p);
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
                ExecuteMultipleResponse emrResponse = (ExecuteMultipleResponse)CrmService.Execute(multipleReq);
                List<string> existingRecords = new List<string>();

                foreach (ExecuteMultipleResponseItem e in emrResponse.Responses)
                {
                    if (e.Fault == null)
                    {
                        string alternateKey = "";
                        Guid ID = ((UpsertResponse)e.Response).Target.Id;
                        switch (table.ToLower())
                        {
                            case "PassesCoMember":
                                alternateKey = (((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target).Attributes["wrs_id"].ToString();
                                break;
                            default:
                                Entity target = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target;
                                alternateKey = target.KeyAttributes.Contains("wrs_id") ? target.KeyAttributes["wrs_id"].ToString() : target.Attributes["wrs_id"].ToString();
                                break;
                        }
                        if (int.Parse(alternateKey) != 0 && alternateKey != "")
                        {
                            //ErrorLog(string.Format("{0} with Guid {1} is created for NC DB ID {2}", table, ID, alternateKey));
                            existingRecords.Add(alternateKey);

                            if (mainSuccessLog != string.Empty)
                            {
                                mainSuccessLog = mainSuccessLog + ",";
                            }

                            mainSuccessLog = mainSuccessLog + "('" + DateTime.Now + "','Success','" + table + "','" + alternateKey + "')";
                        }
                    }
                    else if (e.Fault != null)
                    {
                        NCId = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes["wrs_id"].ToString();
                        if (e.Fault.Message.Contains("A record that has the attribute values NC ID already exists") ||
                            e.Fault.Message.Contains("A record with the specified key values does not exist in wrs_membership entity"))
                        {
                            existingRecords.Add(NCId);
                            mainSuccessLog = mainSuccessLog + "('" + DateTime.Now + "','Success','" + table + "','" + NCId + "')";
                        }
                        else
                        {
                            string errorLog = "('" + DateTime.Now + "','" + e.Fault.Message.Replace("'", "''") + "','" + (e.Fault.TraceText != null ? e.Fault.TraceText.Replace("'", "''") : " ") + "','" + table + "','" + NCId + "'";
                            WriteLogsToDB(errorLog);
                        }
                    }
                }
                if (existingRecords.Count > 0)
                {
                    foreach (string l in existingRecords)
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
