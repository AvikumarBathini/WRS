using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WRS.Plugin.Case
{
    public class CaseCreate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the execution context from the service provider.
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            if (context.Depth > 3)
                return;
            // The InputParameters collection contains all the data passed in the message request.
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.
                Entity targetEntity = (Entity)context.InputParameters["Target"];

                // Verify that the target entity represents an entity type you are expecting. 
                // For example, an account. If not, the plug-in was not registered correctly.
                if (targetEntity.LogicalName != "incident")
                    return;

                // Obtain the organization service reference which you will need for
                // web service calls.
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                if (targetEntity != null)
                {
                    Entity _case = service.Retrieve(targetEntity.LogicalName, targetEntity.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet("wrs_caseoriginhiddensla", "wrs_enquiryhiddensla", "wrs_complainthiddensla", "wrs_complimenthiddensla", "wrs_suggestionhiddensla"));
                    int wrs_caseoriginhiddensla = _case.Contains("wrs_caseoriginhiddensla") ? _case.GetAttributeValue<OptionSetValue>("wrs_caseoriginhiddensla").Value : -1;
                    if (context.PostEntityImages.Contains("PostImage"))
                    {
                        Entity postImage = (Entity)context.PostEntityImages["PostImage"];
                        if (postImage != null)
                        {
                            Entity _newCase = GetEntity(postImage, _case);

                            if (wrs_caseoriginhiddensla != 2 && wrs_caseoriginhiddensla != 1 && wrs_caseoriginhiddensla != 3)
                            {
                                if (postImage.Contains("caseorigincode") && postImage["caseorigincode"] != null)
                                {
                                    _newCase["wrs_caseoriginhiddensla"] = new OptionSetValue(((OptionSetValue)postImage["caseorigincode"]).Value);
                                }
                            }
                            if (context.MessageName.ToLower() == "create")
                                _newCase["wrs_slastartdatehidden"] = DateTime.Now;

                            if (_newCase.Attributes.Count > 0)
                                service.Update(_newCase);
                        }
                    }
                    else
                    {
                        Entity _newCase = new Entity(targetEntity.LogicalName, targetEntity.Id);
                        if (context.MessageName.ToLower() == "create")
                        {
                            _newCase["wrs_slastartdatehidden"] = DateTime.Now;
                            service.Update(_newCase);
                        }
                    }
                }
            }
        }

        private Entity GetEntity(Entity postImage, Entity _case)
        {
            Entity _newCase = new Entity(postImage.LogicalName, postImage.Id);
            bool wrs_enquiryhiddensla = _case.Contains("wrs_enquiryhiddensla") ? _case.GetAttributeValue<bool>("wrs_enquiryhiddensla") : false;
            bool wrs_complainthiddensla = _case.Contains("wrs_complainthiddensla") ? _case.GetAttributeValue<bool>("wrs_complainthiddensla") : false;
            bool wrs_complimenthiddensla = _case.Contains("wrs_complimenthiddensla") ? _case.GetAttributeValue<bool>("wrs_complimenthiddensla") : false;
            bool wrs_suggestionhiddensla = _case.Contains("wrs_suggestionhiddensla") ? _case.GetAttributeValue<bool>("wrs_suggestionhiddensla") : false;
            if (!wrs_complainthiddensla && !wrs_complimenthiddensla && !wrs_enquiryhiddensla && !wrs_suggestionhiddensla)
            {
                if (postImage.Contains("wrs_enquiry") && (bool)postImage["wrs_enquiry"])
                {
                    _newCase["wrs_enquiryhiddensla"] = (bool)postImage["wrs_enquiry"];
                    return _newCase;
                }

                if (postImage.Contains("wrs_complaint") && (bool)postImage["wrs_complaint"])
                {
                    _newCase["wrs_complainthiddensla"] = (bool)postImage["wrs_complaint"];
                    return _newCase;
                }

                if (postImage.Contains("wrs_compliment") && (bool)postImage["wrs_compliment"])
                {
                    _newCase["wrs_complimenthiddensla"] = (bool)postImage["wrs_compliment"];
                    return _newCase;
                }

                if (postImage.Contains("wrs_suggestion") && (bool)postImage["wrs_suggestion"])
                {
                    _newCase["wrs_suggestionhiddensla"] = (bool)postImage["wrs_suggestion"];
                    return _newCase;
                }
            }
            return _newCase;
        }
    }
}
