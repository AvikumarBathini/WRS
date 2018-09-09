using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WRS.Plugin.Email
{
    public class EmailUpdate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the execution context from the service provider.
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // The InputParameters collection contains all the data passed in the message request.
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.
                Entity targetEntity = (Entity)context.InputParameters["Target"];

                // Verify that the target entity represents an entity type you are expecting. 
                // For example, an account. If not, the plug-in was not registered correctly.
                if (targetEntity.LogicalName != "email")
                    return;

                Entity postImageEntity = (context.PostEntityImages != null && context.PostEntityImages.Contains("postImage")) ? context.PostEntityImages["postImage"] : null;

                // Obtain the organization service reference which you will need for
                // web service calls.
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                if (targetEntity != null && postImageEntity != null)
                {
                    if (postImageEntity.Contains("directioncode") && (bool)postImageEntity["directioncode"] && postImageEntity.FormattedValues.Contains("statuscode") && postImageEntity.FormattedValues["statuscode"].ToString() == "Sent")
                    {
                        string content = postImageEntity["description"].ToString();
                        string title = postImageEntity["subject"].ToString().Trim();
                        string sender = postImageEntity["sender"].ToString().Trim();
                        string recipients = postImageEntity["torecipients"].ToString().Trim();

                        if (content.Trim() != string.Empty && !sender.Contains("ncs@wrs.com.sg") && !sender.Contains("acc.crm@wrs.com.sg"))
                        {
                            Guid kbArticleid = CreateKBArticle(service, title, content);

                            if (kbArticleid != null && kbArticleid != Guid.Empty)
                            {
                                SubmitAndPublishArticle(service, kbArticleid);
                            }
                        }
                    }
                }
            }

            //throw new InvalidPluginExecutionException();
        }

        Guid GetSubjectByTitle(IOrganizationService service, string title)
        {
            Guid responseValue = Guid.Empty;
            var query = new QueryExpression("subject");
            query.ColumnSet = new ColumnSet("subjectid");
            var condition1 = new ConditionExpression("title", ConditionOperator.Equal, title);
            query.Criteria.Conditions.Add(condition1);
            var result = service.RetrieveMultiple(query);
            if (result.Entities.Count > 0)
            {
                if (result.Entities[0].Contains("subjectid"))
                {
                    responseValue = result.Entities[0].GetAttributeValue<Guid>("subjectid");
                }
            }
            return responseValue;
        }

        Guid GetKBArticleTemplateByTitle(IOrganizationService service, string title)
        {
            Guid responseValue = Guid.Empty;
            var query = new QueryExpression("kbarticletemplate");
            query.ColumnSet = new ColumnSet("kbarticletemplateid");
            var condition1 = new ConditionExpression("title", ConditionOperator.Equal, title);
            query.Criteria.Conditions.Add(condition1);
            var result = service.RetrieveMultiple(query);
            if (result.Entities.Count > 0)
            {
                if (result.Entities[0].Contains("kbarticletemplateid"))
                {
                    responseValue = result.Entities[0].GetAttributeValue<Guid>("kbarticletemplateid");
                }
            }
            return responseValue;
        }

        Guid CreateKBArticle(IOrganizationService service, string title, string content)
        {
            //Guid subjectId = GetSubjectByTitle(service, "General");
            //Guid templateId = GetKBArticleTemplateByTitle(service, "Standard KB Article");

            //if (subjectId != Guid.Empty && templateId != Guid.Empty)
            //{
            //    Entity kbarticle = new Entity("kbarticle");
            //    kbarticle["title"] = title;
            //    kbarticle["articlexml"] = @"<articledata><section id='0'><content><![CDATA[" + content + "]]></content></section><section id='1'><content><![CDATA[]]></content></section></articledata>";
            //    kbarticle["keywords"] = title;
            //    kbarticle["subjectid"] = new EntityReference("subject", subjectId);
            //    kbarticle["kbarticletemplateid"] = new EntityReference("kbarticletemplate", templateId);

            //    return service.Create(kbarticle);
            //}

            
            Entity kbarticle = new Entity("knowledgearticle");
            kbarticle["title"] = title;
            kbarticle["content"] = content;
            kbarticle["keywords"] = title.Replace(" ", ",");

            return service.Create(kbarticle);
        }

        void SubmitAndPublishArticle(IOrganizationService service, Guid articleId)
        {
            // Retrieve the knowledge article record
            Entity myKnowledgeArticle = (Entity)service.Retrieve("knowledgearticle", articleId, new ColumnSet("statecode"));

            // Update the knowledge article record
            myKnowledgeArticle["statecode"] = new OptionSetValue(3);
            UpdateRequest updateKnowledgeArticle = new UpdateRequest
            {
                Target = myKnowledgeArticle
            };
            service.Execute(updateKnowledgeArticle);


            //#region Submit the articles

            //service.Execute(new SetStateRequest
            //{
            //    EntityMoniker = new EntityReference("kbarticle", articleId),
            //    State = new OptionSetValue(2),//unapproved
            //    Status = new OptionSetValue(2)//unapproved
            //});

            //#endregion

            //#region Approve and Publish the article

            //service.Execute(new SetStateRequest
            //{
            //    EntityMoniker = new EntityReference("kbarticle", articleId),
            //    State = new OptionSetValue(3),//published
            //    Status = new OptionSetValue(3)//published
            //});

            //#endregion
        }
    }
}
