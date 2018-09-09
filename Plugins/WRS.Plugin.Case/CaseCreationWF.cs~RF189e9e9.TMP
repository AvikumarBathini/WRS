using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WRS.Plugin.Case
{

    public sealed class CaseCreationWF : CodeActivity
    {
        protected override void Execute(CodeActivityContext executionContext)
        {
            //Create the tracing service
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            //Create the context
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            EntityReference email = Email.Get<EntityReference>(executionContext);
            if (email != null)
            {
                string _team = string.Empty;
                string _caseType = string.Empty;
                Entity _email = service.Retrieve(email.LogicalName, email.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
                if (_email != null)
                {
                    DateTime _createdOn = _email.GetAttributeValue<DateTime>("createdon");
                    string _subject = _email.Contains("subject") ? _email.GetAttributeValue<string>("subject") : "New Enquiry";
                    EntityReference _from = _email.Contains("from") ? _email.GetAttributeValue<EntityCollection>("from").Entities.FirstOrDefault().GetAttributeValue<EntityReference>("partyid") : null;
                    if (_from.LogicalName == "systemuser")
                    {
                        string addressused = _email.GetAttributeValue<EntityCollection>("from").Entities.FirstOrDefault().GetAttributeValue<string>("addressused");
                        _from = GetOwnerID(service, "contact", "emailaddress1", addressused);
                    }
                    if (_email.Contains("torecipients"))
                    {
                        Entity _case = new Entity("incident");
                        string torecipients = _email.GetAttributeValue<string>("torecipients");
                        if (torecipients.Contains("feedback@wrs.com.sg"))
                        {
                            _team = "GE";
                            _caseType = "Suggestion";
                            _case.Attributes.Add("wrs_suggestion", true);
                            _case.Attributes.Add("wrs_suggestionhiddensla", true);
                            _case.Attributes.Add("prioritycode", new OptionSetValue(4));
                        }
                        else if (torecipients.Contains("enquiry@wrs.com.sg"))
                        {
                            _team = "GE";
                            _caseType = "Enquiry";
                            _case.Attributes.Add("wrs_enquiry", true);
                            _case.Attributes.Add("wrs_enquiryhiddensla", true);
                            _case.Attributes.Add("prioritycode", new OptionSetValue(1));
                        }
                        else if (torecipients.Contains("members@wrs.com.sg"))
                        {
                            _team = "Members";
                            _caseType = "Enquiry";
                            _case.Attributes.Add("wrs_enquiry", true);
                            _case.Attributes.Add("wrs_enquiryhiddensla", true);
                            _case.Attributes.Add("prioritycode", new OptionSetValue(1));
                        }
                        else if (torecipients.Contains("membership@wrs.com.sg"))
                        {
                            _team = "Membership";
                            _caseType = "Enquiry";
                            _case.Attributes.Add("wrs_enquiry", true);
                            _case.Attributes.Add("wrs_enquiryhiddensla", true);
                            _case.Attributes.Add("prioritycode", new OptionSetValue(1));
                        }
                        _case.Attributes.Add("ownerid", GetOwnerID(service, "team", "name", _team));
                        _case.Attributes.Add("title", _subject);
                        _case.Attributes.Add("caseorigincode", new OptionSetValue(2));
                        _case.Attributes.Add("wrs_casetypes", _caseType);
                        _case.Attributes.Add("overriddencreatedon", _createdOn);
                        _case.Attributes.Add("createdon", _createdOn);
                        _case.Attributes.Add("customerid", _from);
                        Guid _caseId = service.Create(_case);

                        Entity tempemail = new Entity(_email.LogicalName, _email.Id);
                        tempemail.Attributes.Add("regardingobjectid", new EntityReference("incident", _caseId));
                        service.Update(tempemail);
                    }
                }
            }
        }

        private EntityReference GetOwnerID(IOrganizationService service, string entityname, string attributename, string attributevalue)
        {
            QueryExpression exp = new QueryExpression(entityname);
            exp.Criteria.AddCondition(attributename, ConditionOperator.Equal, attributevalue);

            EntityCollection results = service.RetrieveMultiple(exp);
            if (results != null && results.Entities.Count > 0)
            {
                return new EntityReference(results[0].LogicalName, results[0].Id);
            }
            else
            {
                Entity contact = new Entity("contact");
                contact.Attributes.Add("emailaddress1", attributevalue);
                contact.Attributes.Add("lastname", attributevalue);
                Guid contactId = service.Create(contact);
                return new EntityReference("contact", contactId);
            }
            WhoAmIResponse resp = ((WhoAmIResponse)service.Execute(new WhoAmIRequest()));
            return new EntityReference("systemuser", resp.UserId);
        }

        [Input("Email")]
        [ReferenceTarget("email")]
        public InArgument<EntityReference> Email { get; set; }

    }
}
