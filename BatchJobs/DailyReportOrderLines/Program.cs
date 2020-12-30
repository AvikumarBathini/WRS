using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace DailyReportOrderLines
{
    class Program
    {
        //SZ Chartered Buggy and JBP Chartered Buggy 
        public static IOrganizationService CrmService = serviceClient();
        public static Dictionary<string, Entity[]> ParkManagers = GetParkManagers();
        public static string FolderPath = ConfigurationManager.AppSettings["REPORT_FOLDER"];
        //public static LogHandler logger = new LogHandler("LOGS");

        public static string[] Parks = new string[] { "NS", "RS", "JBP", "SZ" };
        public static Dictionary<string, List<string>> Reports = new Dictionary<string, List<string>>() {
            { "FnB", new List<string> { "JBP - Lunch with Parrots", "NS - Ulu Asian Buffet", "NS - Ulu Indian Buffet", "SZ - Jungle Breakfast with Wildlife" } },
            {"Tour",new List<string> { "JBP - Bird's Eye Tour", "NS - Safari Adventure Tour", "RS - Amazing Amazonia", "RS - Manatee Mania", "SZ - Fragile Forest Tour", "SZ - Reptopia Tour", "SZ - Wild Discoverer Tour" ,"JBP - Chartered Buggy","SZ - Chartered Buggy"} },
            {"Rentals",new List<string> { "JBP - Stroller","JBP - Wagon","NS - Stroller","RS - Stroller","SZ - Stroller","SZ - Wagon" }}
        };

        public static Dictionary<string, List<string>> ChildReports = GetOrderLineProducts();
        static void Main(string[] args)
        {
            try
            {
                //logger.Log("Log Created");
                if (DateTime.Today.DayOfWeek.ToString() == "Monday" && DateTime.Now.ToString("tt", CultureInfo.InvariantCulture) == "AM")
                    WeekelyReport();
                DailyReport();
            }
            catch (Exception e)
            {
                //logger.TraceEx(e);
                List<Entity> arrTo = new List<Entity>();
                Guid contactId = GetContactGuid("avikumar.bathini@ncs.com.sg");
                Entity ap2 = new Entity("activityparty");
                ap2["partyid"] = new EntityReference("contact", contactId);
                arrTo.Add(ap2);
                CreateAndSendEmail("Error in DailyReportOrderLines Batch Job - " + DateTime.Now.ToString(), string.Format("Message - {0}</br>InnerException - {1}</br>StackTrace - {2}", e.Message, e.InnerException, e.StackTrace), arrTo.ToArray(), null);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static void DailyReport()
        {
            string filePath = string.Empty;
            #region Fetch Exp
            string fetch =
                    "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>" +
                      "<entity name='salesorderdetail'>" +
                        "<attribute name='productid' />" +
                        "<attribute name='quantity' />" +
                        "<attribute name='wrs_attributedescription' />" +
                        "<attribute name='wrs_attributesxml' />" +
                        "<attribute name='salesorderdetailid' />" +
                        "<order attribute='salesorderid' descending='false' />" +
                        "<filter type='and'>" +
                          "<condition attribute='productid' operator='in'>" +
                           "{0}" +
                          "</condition>" +
                          "<condition attribute='wrs_attributedescription' operator='like' value='{1}' />" +
                        "<condition attribute='wrs_attributedescription' operator='like' value='{2}' />" +
                        "</filter>" +
                        "<link-entity name='product' from='productid' to='productid' visible='false' link-type='outer' alias='PRODUCT'>" +
                          "<attribute name='parentproductid' />" +
                        "</link-entity>" +
                        "<link-entity name='salesorder' from='salesorderid' to='salesorderid' alias='ORDER'>" +
                            "<attribute name='createdon' />" +
                            "<attribute name='customerid' />" +
                            "<attribute name='wrs_id' />" +
                            "<attribute name='wrs_storeid' />" +
                            "<attribute name='billto_country' />" +
                          "<filter type='and'>" +
                            "<condition attribute='statuscode' operator='in'>" +
                              "<value>690970000</value>" +
                              "<value>100001</value>" +
                              "<value>1</value>" +
                            "</condition>" +
                            "<condition attribute='wrs_galaxyordernumber' operator='not-null' />" +
                          "</filter>" +
                        "</link-entity>" +
                        "<link-entity name='contact' from='contactid' to='wrs_customer' visible='false' link-type='outer' alias='CONTACT'>" +
                             "<attribute name='emailaddress1' />" +
                             "<attribute name='fullname' />" +
                             "<attribute name='address1_telephone1' />" +
                        "</link-entity>" +
                      "</entity>" +
                    "</fetch>";
            #endregion

            //if (DateTime.Now.Hour == 2)
            {
                List<string> filePaths1 = ToursAndRentalsReport(fetch, "Rentals", Reports["Rentals"], "Rentals");
                if (ParkManagers.ContainsKey("RENTALS_NS"))
                {
                    Entity[] reciepients = ParkManagers["RENTALS_NS"];
                    string subject = "Multi Park Rentals reports - " + DateTime.Today.ToShortDateString();
                    StringBuilder body = new StringBuilder();
                    body.Append("Hello Team,");
                    body.AppendLine("</br>");
                    body.AppendLine("</br>");
                    body.AppendLine("Please find the attached Multi Park Rentals report for the day " + DateTime.Today.ToShortDateString());
                    body.AppendLine("</br>");
                    body.AppendLine("</br>");
                    body.AppendLine("Cheers.");
                    CreateAndSendEmail(subject, body.ToString(), reciepients, filePaths1);
                }

                foreach (var park in Parks)
                {
                    foreach (var item in Reports)
                    {
                        List<string> filePaths = new List<string>();
                        if (item.Key == "Tour")
                            filePaths = ToursAndRentalsReport(fetch, park, item.Value, item.Key);
                        if (ParkManagers.Count > 0 && filePaths.Count > 0)
                        {
                            string key = item.Key.ToUpper() + "_" + park.ToUpper();
                            if (ParkManagers.ContainsKey(key))
                            {
                                Entity[] reciepients = ParkManagers[key];
                                string subject = park + " " + item.Key + " reports - " + DateTime.Today.ToShortDateString();
                                StringBuilder body = new StringBuilder();
                                body.Append("Hello Team,");
                                body.AppendLine("</br>");
                                body.AppendLine("</br>");
                                body.AppendLine("Please find the attached " + park + " " + item.Key + " report for the day " + DateTime.Today.ToShortDateString());
                                body.AppendLine("</br>");
                                body.AppendLine("</br>");
                                body.AppendLine("Cheers.");
                                CreateAndSendEmail(subject, body.ToString(), reciepients, filePaths);
                            }
                        }
                    }
                }
            }

            if (DateTime.Now.Hour == 15)
            {
                List<string> filePaths = new List<string>();
                filePaths = FnBReports(fetch, "Multi Park", Reports["FnB"]);
                if (ParkManagers.ContainsKey("FNB_DEPARTMENT"))
                {
                    Entity[] reciepients = ParkManagers["FNB_DEPARTMENT"];
                    string subject = "Multi Park FnB reports - " + DateTime.Today.ToShortDateString();
                    StringBuilder body = new StringBuilder();
                    body.Append("Hello Team,");
                    body.AppendLine("</br>");
                    body.AppendLine("</br>");
                    body.AppendLine("Please find the attached Multi Park FnB report for the day " + DateTime.Today.ToShortDateString());
                    body.AppendLine("</br>");
                    body.AppendLine("</br>");
                    body.AppendLine("Cheers.");
                    CreateAndSendEmail(subject, body.ToString(), reciepients, filePaths);
                }
            }
        }

        private static void CreateAndSendEmail(string subject, string description, Entity[] reciepients, List<string> attachmentPaths)
        {

            bool isUAT = Convert.ToBoolean(ConfigurationManager.AppSettings["IsUAT"]);
            if (isUAT)
                subject = subject + " - UAT";

            WhoAmIResponse resp = (WhoAmIResponse)(CrmService.Execute(new WhoAmIRequest()));
            Entity emailCreate = new Entity("email");
            emailCreate["subject"] = subject;
            emailCreate["description"] = description;

            Entity ap2 = new Entity("activityparty");
            ap2["partyid"] = new EntityReference("systemuser", resp.UserId);
            Entity[] aryFrom = { ap2 };
            emailCreate["from"] = aryFrom;
            emailCreate["to"] = reciepients;
            Guid emailguid = CrmService.Create(emailCreate);
            if (attachmentPaths != null && attachmentPaths.Count > 0)
            {
                foreach (string attachmentPath in attachmentPaths)
                {
                    if (!string.IsNullOrEmpty(attachmentPath))
                    {
                        string filename = Path.GetFileNameWithoutExtension(attachmentPath);
                        byte[] byteData = StreamFile(attachmentPath);
                        string encodedData = Convert.ToBase64String(byteData);
                        Entity Annotation = new Entity("activitymimeattachment");
                        Annotation.Attributes["subject"] = subject;
                        Annotation.Attributes["body"] = encodedData;
                        Annotation.Attributes["mimetype"] = @"application/xlsx";
                        Annotation.Attributes["filename"] = filename + ".csv";
                        Annotation["objectid"] = new EntityReference("email", emailguid);
                        Annotation["objecttypecode"] = "email";
                        CrmService.Create(Annotation);
                    }
                }
            }

            SendEmailRequest sendEmailreq = new SendEmailRequest
            {
                EmailId = emailguid,
                TrackingToken = "",
                IssueSend = true
            };
            SendEmailResponse sendEmailresp = (SendEmailResponse)CrmService.Execute(sendEmailreq);
        }

        private static Dictionary<string, Entity[]> GetParkManagers()
        {
            Dictionary<string, Entity[]> dictionary = new Dictionary<string, Entity[]>();
            QueryExpression exp = new QueryExpression()
            {
                EntityName = "wrs_configuration",
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression()
            };
            exp.Criteria.AddCondition("wrs_name", ConditionOperator.Equal, "WRS_REPORTS");
            exp.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            EntityCollection results = CrmService.RetrieveMultiple(exp);
            if (results != null && results.Entities.Count > 0)
            {
                foreach (Entity config in results.Entities)
                {
                    List<Entity> arrTo = new List<Entity>();
                    string type = config.Contains("wrs_key") ? config.GetAttributeValue<string>("wrs_key") : string.Empty;
                    string[] emails = config.Contains("wrs_value") ? config.GetAttributeValue<string>("wrs_value").Split(',') : null;
                    if (emails != null && emails.Length > 0)
                    {
                        foreach (string email in emails)
                        {
                            Guid contactId = GetContactGuid(email);
                            Entity ap2 = new Entity("activityparty");
                            ap2["partyid"] = new EntityReference("contact", contactId);
                            arrTo.Add(ap2);
                        }
                        dictionary.Add(type, arrTo.ToArray());
                    }
                }
            }
            return dictionary;
        }

        private static Dictionary<string, List<string>> GetOrderLineProducts()
        {
            Dictionary<string, List<string>> guidCollection = new Dictionary<string, List<string>>();
            QueryExpression personalViews = new QueryExpression("userquery");
            personalViews.ColumnSet = new ColumnSet(true);
            EntityCollection views = CrmService.RetrieveMultiple(personalViews);
            foreach (Entity view in views.Entities)
            {
                try
                {
                    string productGuid = string.Empty;
                    string orderLineReport = string.Empty;
                    string xml = view.Contains("fetchxml") ? view.GetAttributeValue<string>("fetchxml") : "";
                    XmlDocument fetchXml = new XmlDocument();
                    if (!string.IsNullOrEmpty(xml))
                        fetchXml.LoadXml(xml);
                    else continue;
                    var conditions = fetchXml.GetElementsByTagName("condition");
                    foreach (XmlElement condition in conditions)
                    {
                        var productId = condition.GetAttribute("attribute");
                        if (productId == "productid")
                        {
                            productGuid = string.IsNullOrEmpty(condition.InnerText) ? condition.GetAttribute("value") : condition.InnerText;
                            orderLineReport = view.GetAttributeValue<string>("name");

                            string[] guids = productGuid.Replace("{", "").Replace("}", ",").Split(',');
                            guids = guids.Take(guids.Count() - 1).ToArray();
                            guidCollection.Add(orderLineReport, guids.ToList());


                            break;
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
            return guidCollection;
        }

        private static Guid GetContactGuid(string email)
        {
            string fetch =
                "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>" +
                  "<entity name='contact'>" +
                    "<attribute name='fullname' />" +
                    "<attribute name='telephone1' />" +
                    "<attribute name='contactid' />" +
                    "<order attribute='fullname' descending='false' />" +
                    "<filter type='and'>" +
                      "<condition attribute='emailaddress1' operator='eq' value='{0}' />" +
                    "</filter>" +
                  "</entity>" +
                "</fetch>";
            var results = CrmService.RetrieveMultiple(new FetchExpression(string.Format(fetch, email)));
            if (results != null && results.Entities.Count > 0)
                return results.Entities.FirstOrDefault().Id;

            else
            {
                Entity contact = new Entity("contact");
                contact.Attributes.Add("firstname", email);
                contact.Attributes.Add("lastname", email);
                contact.Attributes.Add("emailaddress1", email);
                Guid id = CrmService.Create(contact);
                return id;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fetch"></param>
        /// <param name="toursOrWagons"></param>
        /// <param name="reportType"></param>
        private static List<string> ToursAndRentalsReport(string fetch, string park, List<string> toursOrWagons, string reportType)
        {
            List<string> paths = new List<string>();
            DateTime today = DateTime.Today;
            string year = DateTime.Now.Year.ToString();
            string month = DateTime.Now.ToString("MMMM");
            int sheetCount = 0;
            foreach (string tourOrWagon in toursOrWagons)
            {
                StringBuilder csvdata = new StringBuilder();
                if (!tourOrWagon.Contains(park) && reportType != "Rentals")
                    continue;

                List<string> productGuid = ChildReports[tourOrWagon];
                string valueCondition = string.Empty;
                foreach (string guid in productGuid)
                {
                    valueCondition += "<value>" + guid + "</value>";
                }
                //string fetchExp = string.Format(fetch, valueCondition, "%" + year + "%");
                string fetchExp = string.Format(fetch, valueCondition, "%" + year + "%", "%" + month + "%");
                EntityCollection orderItems = CrmService.RetrieveMultiple(new FetchExpression(fetchExp));
                if (orderItems is EntityCollection)
                {
                    sheetCount = sheetCount + 1;
                    List<OrderItem> tourVisitors = SerializeOrderItems(orderItems, tourOrWagon);

                    #region GENERATE EXCEL SHEET

                    int tourA = 0, tourC = 0, tourS = 0;
                    csvdata.Append(reportType + ",");
                    csvdata.Append("Add-ons,");
                    csvdata.Append("Full Name,");
                    csvdata.Append("Email,");
                    csvdata.Append("Phone,");
                    csvdata.Append("Visit Date,");
                    csvdata.Append("Order Date,");
                    csvdata.Append("Country,");
                    csvdata.Append("Tickets,");
                    csvdata.Append("OrderNo");
                    if (tourVisitors.Count > 0)
                    {
                        foreach (OrderItem visitor in tourVisitors)
                        {
                            if (DateTime.Today == visitor.VisitDate)
                            {
                                csvdata.AppendLine(string.Empty);
                                csvdata.Append(visitor.Park + ",");
                                csvdata.Append(visitor.Addons + ",");
                                csvdata.Append(visitor.CustomerName + ",");
                                csvdata.Append(visitor.CustomerEmail + ",");
                                csvdata.Append(visitor.CustomerPhone + ",");
                                csvdata.Append(visitor.VisitDate.ToString("yyyy-MM-dd") + ",");
                                csvdata.Append(visitor.OrderDate.ToString("yyyy-MM-dd") + ",");
                                csvdata.Append(visitor.CustomerCountry + ",");
                                csvdata.Append("Adults-" + visitor.Adult + " Child - " + visitor.Child + " Senior-" + visitor.Senior + ",");
                                csvdata.Append(visitor.OrderNumber);
                                tourA += visitor.Adult;
                                tourC += visitor.Child;
                                tourS += visitor.Senior;
                            }
                        }
                        csvdata.AppendLine(string.Empty);
                        if (tourA != 0)
                            csvdata.AppendLine("Total Adults - " + tourA);
                        if (tourC != 0)
                            csvdata.AppendLine("Total Child - " + tourC);
                        if (tourS != 0)
                            csvdata.AppendLine("Total Senior - " + tourS);
                        csvdata.AppendLine(string.Empty);
                        if (tourA == 0 && tourC == 0 && tourS == 0)
                        {
                            csvdata.AppendLine(string.Empty);
                            csvdata.AppendLine(string.Empty);
                            string _tempname = tourOrWagon.Replace("_", " ").Replace(reportType, "");
                            csvdata.AppendLine("No " + _tempname + " " + reportType + " booked today.");
                        }
                    }
                    else
                    {
                        string _tempname = tourOrWagon.Replace("_", " ").Replace(reportType, "");
                        csvdata.AppendLine(string.Empty);
                        csvdata.AppendLine(string.Empty);
                        csvdata.AppendLine("No " + _tempname + " " + reportType + " booked today.");
                    }
                    #endregion
                }
                string path = Path.Combine(FolderPath, tourOrWagon + "_Report_" + DateTime.Today.ToString("yyyy-MM-dd") + ".csv");
                File.WriteAllText(path, csvdata.ToString());
                paths.Add(path);
            }

            return paths;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fetch"></param>
        /// <param name="value"></param>
        private static List<string> FnBReports(string fetch, string park, List<string> fnbItems)
        {
            List<string> paths = new List<string>();
            string day = string.Empty;
            DateTime today = DateTime.Today;
            DateTime tomorrow = DateTime.Today.AddDays(1);
            string year = string.Empty;
            //DateTime.Now.Year.ToString();
            string month = string.Empty;
            //DateTime.Now.ToString("MMMM");
            int sheetCount = 0;
            foreach (string fnb in fnbItems)
            {
                StringBuilder csvdata = new StringBuilder();
                bool isUllu = false;
                if (fnb == "NS - Ulu Asian Buffet" || fnb == "NS - Ulu Indian Buffet")
                {
                    isUllu = true;
                    year = today.Year.ToString();
                    month = today.ToString("MMMM");
                    day = "today.";
                }
                else
                {
                    isUllu = false;
                    year = tomorrow.Year.ToString();
                    month = tomorrow.ToString("MMMM");
                    day = "tomorrow.";
                }
                List<string> productGuid = ChildReports[fnb];
                string valueCondition = string.Empty;
                foreach (string guid in productGuid)
                {
                    valueCondition += "<value>{" + guid + "}</value>";
                }
                //string fetchExp = string.Format(fetch, valueCondition, "%" + year + "%");
                string fetchExp = string.Format(fetch, valueCondition, "%" + year + "%", "%" + month + "%");
                EntityCollection orderItems = CrmService.RetrieveMultiple(new FetchExpression(fetchExp));
                if (orderItems is EntityCollection)
                {
                    sheetCount = sheetCount + 1;
                    List<OrderItem> tourVisitors = SerializeOrderItems(orderItems, fnb);

                    #region GENERATE EXCEL SHEET

                    int tourA = 0, tourC = 0, tourS = 0;
                    csvdata.Append("FnB,");
                    csvdata.Append("Add-ons,");
                    csvdata.Append("Full Name,");
                    csvdata.Append("Email,");
                    csvdata.Append("Phone,");
                    csvdata.Append("Visit Date,");
                    csvdata.Append("Order Date,");
                    csvdata.Append("Country,");
                    csvdata.Append("Tickets,");
                    csvdata.Append("OrderNo");

                    if (tourVisitors.Count > 0)
                    {
                        foreach (OrderItem visitor in tourVisitors)
                        {
                            bool process = false;
                            if (isUllu)
                            {
                                if (DateTime.Today == visitor.VisitDate)
                                    process = true;
                            }
                            else if (DateTime.Today.AddDays(1) == visitor.VisitDate)
                                process = true;

                            if (process)
                            {
                                csvdata.Append(visitor.Park + ",");
                                csvdata.Append(visitor.Addons + ",");
                                csvdata.Append(visitor.CustomerName + ",");
                                csvdata.Append(visitor.CustomerEmail + ",");
                                csvdata.Append(visitor.CustomerPhone + ",");
                                csvdata.Append(visitor.VisitDate.ToString("yyyy-MM-dd") + ",");
                                csvdata.Append(visitor.OrderDate.ToString("yyyy-MM-dd") + ",");
                                csvdata.Append(visitor.CustomerCountry + ",");
                                csvdata.Append("Adults-" + visitor.Adult + " Child - " + visitor.Child + " Senior-" + visitor.Senior + ",");
                                csvdata.Append(visitor.OrderNumber);
                                tourA += visitor.Adult;
                                tourC += visitor.Child;
                                tourS += visitor.Senior;
                            }
                        }
                        csvdata.AppendLine(string.Empty);
                        csvdata.AppendLine(string.Empty);
                        if (tourA != 0)
                            csvdata.AppendLine("Total Adults - " + tourA);
                        if (tourC != 0)
                            csvdata.AppendLine("Total Child - " + tourC);
                        if (tourS != 0)
                            csvdata.AppendLine("Total Senior - " + tourS);
                        if (tourA == 0 && tourC == 0 && tourS == 0)
                        {
                            csvdata.AppendLine(string.Empty);
                            csvdata.AppendLine(string.Empty);
                            string _tempname = fnb.Replace("_", " ").Replace("FnB", "");
                            csvdata.AppendLine("No " + _tempname + " FnB booked " + day);
                        }
                    }
                    else
                    {
                        csvdata.AppendLine(string.Empty);
                        csvdata.AppendLine(string.Empty);
                        string _tempname = fnb.Replace("_", " ").Replace("FnB", "");
                        csvdata.AppendLine("No " + _tempname + " FnB booked " + day);
                    }
                    #endregion
                }
                string path = Path.Combine(FolderPath, fnb + " " + DateTime.Today.ToString("yyyy-MM-dd") + ".csv");
                File.WriteAllText(path, csvdata.ToString());
                paths.Add(path);
            }
            return paths;
        }

        /// <summary>
        /// 
        /// </summary>
        private static void WeekelyReport()
        {
            EntityCollection records = new EntityCollection();
            List<OrderItem> orderItemList = new List<OrderItem>();
            Dictionary<string, string> months = new Dictionary<string, string>();
            months.Add(DateTime.Now.ToString("MMMM"), DateTime.Now.Year.ToString());
            string year = DateTime.Now.Year.ToString();
            string month = DateTime.Now.ToString("MMMM");
            DateTime today = DateTime.Today;
            DateTime lastweek = DateTime.Today.AddDays(-7);
            if (today.Month != lastweek.Month)
            {
                months.Add(lastweek.ToString("MMMM"), lastweek.Year.ToString());
            }

            #region Fetch XML
            string fetch =
                   "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>" +
                     "<entity name='salesorderdetail'>" +
                       "<attribute name='productid' />" +
                       "<attribute name='quantity' />" +
                       "<attribute name='wrs_attributedescription' />" +
                       "<attribute name='wrs_attributesxml' />" +
                       "<attribute name='salesorderdetailid' />" +
                       "<order attribute='productid' descending='false' />" +
                       "<filter type='and'>" +
                         "<condition attribute='wrs_attributedescription' operator='like' value='%{0}%' />" +
                         "<condition attribute='wrs_attributedescription' operator='like' value='%{1}%' />" +
                       "</filter>" +
                       "<link-entity name='product' from='productid' to='productid' visible='false' link-type='outer' alias='PRODUCT'>" +
                         "<attribute name='parentproductid' />" +
                       "</link-entity>" +
                       "<link-entity name='contact' from='contactid' to='wrs_customer' visible='false' link-type='outer' alias='CONTACT'>" +
                         "<attribute name='emailaddress1' />" +
                         "<attribute name='fullname' />" +
                         "<attribute name='address1_telephone1' />" +
                       "</link-entity>" +
                       "<link-entity name='salesorder' from='salesorderid' to='salesorderid' alias='ORDER'>" +
                         "<attribute name='createdon' />" +
                         "<attribute name='customerid' />" +
                         "<attribute name='wrs_id' />" +
                         "<attribute name='wrs_storeid' />" +
                         "<attribute name='billto_country' />" +
                         "<filter type='and'>" +
                           "<condition attribute='statuscode' operator='in'>" +
                             "<value>690970000</value>" +
                             "<value>100001</value>" +
                             "<value>1</value>" +
                           "</condition>" +
                         "</filter>" +
                       "</link-entity>" +
                     "</entity>" +
                   "</fetch>";
            #endregion

            foreach (var item in months)
            {
                EntityCollection orderItems = CrmService.RetrieveMultiple(new FetchExpression(string.Format(fetch, item.Value, item.Key)));
                records.Entities.AddRange(orderItems.Entities);
            }
            if (records != null && records is EntityCollection && records.Entities.Count > 0)
            {
                orderItemList = SerializeOrderItems(records, string.Empty, false);
                if (orderItemList.Count > 0)
                {
                    List<string> path = GenerateExcelReport(orderItemList);
                    string subject = "WRS Booked Ticket " + DateTime.Today.AddDays(-7).ToShortDateString() + " " + DateTime.Today.ToShortDateString();
                    StringBuilder body = new StringBuilder();
                    body.Append("Hello Team,");
                    body.AppendLine("</br>");
                    body.AppendLine("</br>");
                    body.AppendLine("Please find the attached weekly report of post visit customers.");
                    body.AppendLine("</br>");
                    body.AppendLine("</br>");
                    body.AppendLine("Cheers.");
                    CreateAndSendEmail(subject, body.ToString(), ParkManagers["POST_VISIT"], path);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coll"></param>
        /// <param name="reportType"></param>
        /// <param name="isDaily"></param>
        /// <returns></returns>
        public static List<OrderItem> SerializeOrderItems(EntityCollection coll, string reportType, bool isDaily = true)
        {
            List<OrderItem> orderItemList = new List<OrderItem>();
            #region Main Logic
            foreach (Entity orderitem in coll.Entities)
            {
                try
                {
                    string _CustomerEmail = orderitem.Contains("CONTACT.emailaddress1") ? orderitem.GetAttributeValue<AliasedValue>("CONTACT.emailaddress1").Value.ToString() : string.Empty;
                    int OrderNumber = orderitem.Contains("ORDER.wrs_id") ? (int)orderitem.GetAttributeValue<AliasedValue>("ORDER.wrs_id").Value : -1;
                    string store = orderitem.Contains("ORDER.wrs_storeid") ? orderitem.FormattedValues["ORDER.wrs_storeid"] : string.Empty;
                    var recs = coll.Entities.Where(f => (int)f.GetAttributeValue<AliasedValue>("ORDER.wrs_id").Value == OrderNumber);
                    bool exists = orderItemList.Exists(d => d.OrderNumber == OrderNumber);
                    if (recs != null && recs.Count() > 0 && !exists)
                    {
                        string park = string.Empty;
                        string Addons = string.Empty;
                        int luminaCount = 0;
                        Entity oi = recs.FirstOrDefault();
                        OrderItem item = new OrderItem();
                        item.CustomerCountry = oi.Contains("ORDER.billto_country") ? oi.GetAttributeValue<AliasedValue>("ORDER.billto_country").Value.ToString() : string.Empty;
                        item.OrderNumber = OrderNumber;
                        item.OrderDate = oi.Contains("ORDER.createdon") ? ((DateTime)oi.GetAttributeValue<AliasedValue>("ORDER.createdon").Value) : DateTime.Today;
                        item.CustomerEmail = oi.Contains("CONTACT.emailaddress1") ? oi.GetAttributeValue<AliasedValue>("CONTACT.emailaddress1").Value.ToString() : string.Empty;
                        item.CustomerPhone = oi.Contains("CONTACT.address1_telephone1") ? oi.GetAttributeValue<AliasedValue>("CONTACT.address1_telephone1").Value.ToString() : string.Empty;
                        item.CustomerName = oi.Contains("CONTACT.fullname") ? oi.GetAttributeValue<AliasedValue>("CONTACT.fullname").Value.ToString() : oi.GetAttributeValue<AliasedValue>("CONTACT.emailaddress1").Value.ToString();
                        item.VisitDate = GetVistDate(oi.GetAttributeValue<string>("wrs_attributesxml"));
                        if (!isDaily)
                        {
                            item.ParkVisits = GetCustomerParkVisitCount(_CustomerEmail, out luminaCount);
                            item.LuminaVists = luminaCount;
                        }
                        foreach (Entity e in recs)
                        {
                            string ProductName_Description = e.Contains("productid") ? e.GetAttributeValue<EntityReference>("productid").Name : string.Empty;
                            int quantity = e.Contains("quantity") ? Convert.ToInt32(e.FormattedValues["quantity"]) : -1;
                            string ProductFamily_Park = e.Contains("PRODUCT.parentproductid") ? e.FormattedValues["PRODUCT.parentproductid"] : string.Empty;
                            if (ProductFamily_Park.Contains("Admission") || ProductFamily_Park.Contains("ParkHopper"))
                            {
                                if (string.IsNullOrEmpty(park))
                                    park = ProductFamily_Park;
                                if (ProductName_Description.Contains("Adult"))
                                    item.Adult += quantity;
                                if (ProductName_Description.Contains("Child"))
                                    item.Child += quantity;
                                if (ProductName_Description.Contains("Senior"))
                                    item.Senior += quantity;
                            }
                            else
                            {
                                Addons += ProductName_Description + " - " + quantity + ";";
                                if (isDaily)
                                {
                                    if (ProductName_Description.Contains("Adult"))
                                        item.Adult += quantity;
                                    if (ProductName_Description.Contains("Child"))
                                        item.Child += quantity;
                                    if (ProductName_Description.Contains("Senior"))
                                        item.Senior += quantity;
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(park))
                            park = reportType;
                        item.Park = park.Replace("Admission", "").Replace("Singapore Zoo", "Zoo").Replace("Jurong ", "").Replace("ParkHopper 2-Park,", "").Replace("_", "").Replace("Rentals", "").Replace("Tour", "").Replace(",", ";");
                        item.Addons = string.IsNullOrEmpty(Addons) ? "Tickets Only" : Addons.Replace("Singapore Zoo", "SZ").Replace("Jurong Bird Park", "JBP").Replace("Amazon River Quest", "RS ARQ").Replace("Adult", "A").Replace("Child", "C");
                        if (store.Contains("Lumina"))
                            item.Store = "Lumina";
                        else
                            item.Store = "OneStore";
                        orderItemList.Add(item);
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
            #endregion

            return orderItemList;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderItemList"></param>
        private static List<string> GenerateExcelReport(List<OrderItem> orderItemList)
        {
            List<string> paths = new List<string>();
            StringBuilder headers = new StringBuilder();
            StringBuilder csvdata = new StringBuilder();
            if (orderItemList != null && orderItemList.Count > 0)
            {
                var luminaCustomers = orderItemList.Where(d => d.Store == "Lumina").Where(e => DateTime.Today.Subtract(e.VisitDate).Days < 7 && DateTime.Today.Subtract(e.VisitDate).Days >= 0);
                var oneStoreCustomers = orderItemList.Where(d => d.Store != "Lumina").Where(e => DateTime.Today.Subtract(e.VisitDate).Days < 7 && DateTime.Today.Subtract(e.VisitDate).Days >= 0);
                oneStoreCustomers = oneStoreCustomers.OrderByDescending(c => c.VisitDate);
                if (luminaCustomers.Count() > 0)
                    luminaCustomers = luminaCustomers.OrderByDescending(c => c.VisitDate);
                DateTime lastweek = DateTime.Today.AddDays(-7);
                DateTime today = DateTime.Today;
                string name = "WRS Booked Ticket " + lastweek.Day + " " + lastweek.ToString("MMMM") + " to " + today.Day + " " + today.ToString("MMMM");

                int empA = 0, empC = 0, empS = 0;
                #region EMP SHEET

                csvdata.Append("Park,");
                csvdata.Append("Add-ons,");
                csvdata.Append("Full Name,");
                csvdata.Append("Email,");
                csvdata.Append("Phone,");
                csvdata.Append("Visit Date,");
                csvdata.Append("Order Date,");
                csvdata.Append("Country,");
                csvdata.Append("Tickets,");
                csvdata.Append("Previous Visits,");
                csvdata.Append("OrderNo");
                csvdata.AppendLine(string.Empty);
                foreach (OrderItem item in oneStoreCustomers)
                {
                    if (DateTime.Today.Subtract(item.VisitDate).Days < 7)
                    {
                        csvdata.Append(item.Park + ",");
                        csvdata.Append(item.Addons + ",");
                        csvdata.Append(item.CustomerName + ",");
                        csvdata.Append(item.CustomerEmail + ",");
                        csvdata.Append(item.CustomerPhone + ",");
                        csvdata.Append(item.VisitDate.ToString("yyyy-MM-dd") + ",");
                        csvdata.Append(item.OrderDate.ToString("yyyy-MM-dd") + ",");
                        csvdata.Append(item.CustomerCountry + ",");
                        csvdata.Append("Adults-" + item.Adult + " Child - " + item.Child + " Senior-" + item.Senior + ",");
                        csvdata.Append(item.ParkVisits + ",");
                        csvdata.Append(item.OrderNumber);
                        empA += item.Adult;
                        empC += item.Child;
                        empS += item.Senior;
                        csvdata.AppendLine(string.Empty);
                    }
                }
                csvdata.AppendLine(string.Empty);
                csvdata.AppendLine("Total Adults - " + empA);
                csvdata.AppendLine("Total Child - " + empC);
                csvdata.AppendLine("Total Senior - " + empS);
                csvdata.AppendLine(string.Empty);
                string path = Path.Combine(FolderPath, "EMP - " + name + ".csv");
                File.WriteAllText(path, csvdata.ToString());
                paths.Add(path);
                #endregion

                int lumA = 0, lumC = 0, lumS = 0; csvdata = new StringBuilder();
                #region Lumina Sheet

                csvdata.Append("Park,");
                csvdata.Append("Add-ons,");
                csvdata.Append("Full Name,");
                csvdata.Append("Email,");
                csvdata.Append("Phone,");
                csvdata.Append("Visit Date,");
                csvdata.Append("Order Date,");
                csvdata.Append("Country,");
                csvdata.Append("Tickets,");
                csvdata.Append("Previous Visits,");
                csvdata.Append("OrderNo");
                csvdata.AppendLine(string.Empty);

                foreach (OrderItem item in luminaCustomers)
                {
                    if (DateTime.Today.Subtract(item.VisitDate).Days < 7)
                    {
                        csvdata.Append(item.Park + ",");
                        csvdata.Append(item.Addons + ",");
                        csvdata.Append(item.CustomerName + ",");
                        csvdata.Append(item.CustomerEmail + ",");
                        csvdata.Append(item.CustomerPhone + ",");
                        csvdata.Append(item.VisitDate.ToString("yyyy-MM-dd") + ",");
                        csvdata.Append(item.OrderDate.ToString("yyyy-MM-dd") + ",");
                        csvdata.Append(item.CustomerCountry + ",");
                        csvdata.Append("Adults-" + item.Adult + " Child - " + item.Child + " Senior-" + item.Senior + ",");
                        csvdata.Append(item.ParkVisits + ",");
                        csvdata.Append(item.OrderNumber);
                        lumA += item.Adult;
                        lumC += item.Child;
                        lumS += item.Senior;
                        csvdata.AppendLine(string.Empty);
                    }
                }
                csvdata.AppendLine(string.Empty);
                csvdata.AppendLine("Total Adults - " + lumA);
                csvdata.AppendLine("Total Child - " + lumC);
                csvdata.AppendLine("Total Senior - " + lumS);
                #endregion
                string pathL = Path.Combine(FolderPath, "Lumina - " + name + ".csv");
                File.WriteAllText(pathL, csvdata.ToString());
                paths.Add(pathL);
            }
            return paths;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <param name="lumina"></param>
        /// <returns></returns>
        private static int GetCustomerParkVisitCount(string email, out int lumina)
        {
            lumina = 0;
            if (string.IsNullOrEmpty(email))
                return 0;

            string fetch =
                "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>" +
                  "<entity name='salesorder'>" +
                    "<attribute name='salesorderid' />" +
                    "<attribute name='wrs_storeid' />" +
                    "<attribute name='wrs_galaxyordernumber' />" +
                    "<order attribute='wrs_galaxyordernumber' descending='false' />" +
                    "<filter type='and'>" +
                      "<condition attribute='statuscode' operator='in'>" +
                        "<value>100001</value>" +
                        "<value>690970000</value>" +
                        "<value>1</value>" +
                      "</condition>" +
                    "</filter>" +
                    "<link-entity name='contact' from='contactid' to='customerid' alias='aa'>" +
                      "<filter type='and'>" +
                        "<condition attribute='emailaddress1' operator='eq' value='{0}' />" +
                      "</filter>" +
                    "</link-entity>" +
                  "</entity>" +
                "</fetch>";
            EntityCollection coll = CrmService.RetrieveMultiple(new FetchExpression(string.Format(fetch, email)));
            if (coll != null && coll.Entities.Count > 0)
            {
                var luminaRecords = coll.Entities.Where(f => f.GetAttributeValue<EntityReference>("wrs_storeid").Name.Contains("Lumina"));
                if (luminaRecords != null && luminaRecords.Count() > 0)
                {
                    lumina = luminaRecords.Count() > 0 ? luminaRecords.Count() - 1 : 0;
                    return coll.Entities.Count - lumina - 1;
                }
                return coll.Entities.Count - 1;
            }
            else
                return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="attrXml"></param>
        /// <returns></returns>
        private static DateTime GetVistDate(string attrXml)
        {
            Attributes attrs = DeserializeXML(attrXml);
            if (attrs != null && attrs.ProductAttribute.Count > 0)
            {
                return Convert.ToDateTime(attrs.ProductAttribute[0].ProductAttributeValue.Value);
            }
            return DateTime.Today;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IOrganizationService serviceClient()
        {
            CrmServiceClient crmConnD = null;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            crmConnD = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRMD"].ConnectionString);
            crmConnD.OrganizationServiceProxy.Timeout = new TimeSpan(0, 20, 0);
            IOrganizationService crmServiceD = crmConnD.OrganizationServiceProxy;
            return crmServiceD;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="attributeXML"></param>
        /// <returns></returns>
        public static Attributes DeserializeXML(string attributeXML)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Attributes));
            StringReader rdr = new StringReader(attributeXML);
            Attributes resultingMessage = (Attributes)serializer.Deserialize(rdr);
            return resultingMessage;
        }

        private static byte[] StreamFile(string filename)
        {
            byte[] data = new byte[0];
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            try
            {
                // Create a byte array of file stream length
                data = new byte[fs.Length];
                //Read block of bytes from stream into the byte array
                fs.Read(data, 0, System.Convert.ToInt32(fs.Length));
                //Close the File Stream
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }
            return data;
            //return the byte data
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class OrderItem
    {
        /// <summary>
        /// 
        /// </summary>
        public int OrderNumber { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime OrderDate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime VisitDate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string CustomerEmail { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string CustomerPhone { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string CustomerCountry { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Addons { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Park { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int ParkVisits { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int LuminaVists { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Adult { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Child { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Store { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Senior { get; set; }
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
