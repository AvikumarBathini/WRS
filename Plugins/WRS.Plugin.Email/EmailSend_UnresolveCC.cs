using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WRS.Plugin.Email
{
    public class EmailSend_UnresolveCC : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the execution context from the service provider.
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            Entity preImageEntity = (context.PreEntityImages != null && context.PreEntityImages.Contains("preImage")) ? context.PreEntityImages["preImage"] : null;

            // Obtain the organization service reference which you will need for
            // web service calls.
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (preImageEntity != null)
            {
                if (preImageEntity.Contains("cc") && preImageEntity["cc"] != null)
                {
                    var recipient = ((EntityCollection)preImageEntity.Attributes["cc"]).Entities.FirstOrDefault<Entity>();
                    if (!recipient.Attributes.Contains("partyid"))
                    {
                        Entity newContact = new Entity("contact");
                        newContact["lastname"] = recipient.Attributes["addressused"];
                        newContact["emailaddress1"] = recipient.Attributes["addressused"];
                        newContact["fullname"] = recipient.Attributes["addressused"];
                        newContact["wrs_accounttype"] = new OptionSetValue(167320001);

                        Guid newContactId = service.Create(newContact);

                        Entity anotherRecipient = new Entity("activityparty");
                        anotherRecipient.Attributes["partyid"] = new EntityReference("contact", newContactId);
                        ((EntityCollection)preImageEntity.Attributes["cc"]).Entities[0] = anotherRecipient;

                        service.Update(preImageEntity);
                    }
                }
            }

        }
    }
}
