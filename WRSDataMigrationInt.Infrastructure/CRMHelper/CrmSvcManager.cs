using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel.Description;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using WRSDataMigrationInt.Infrastructure.LoggerExceptionHandling;
using Microsoft.Xrm.Sdk.Discovery;

namespace WRSDataMigrationInt.Infrastructure.CRMHelper
{
    public class CrmSvcManager : IDisposable
    {
        private OrganizationServiceProxy _orgService;
        private readonly string _serverName;
        private readonly string _orgName;
        private readonly int _port;
        private readonly string _domain;
        private readonly string _userName;
        private readonly string _password;

        public CrmSvcManager(bool isWindowsAuth = true)
        {
            _domain = ConfigHelper.GetSysParam<string>("crm.userDomain");
            _serverName = ConfigHelper.GetSysParam<string>("crm.serverName");
            _port = ConfigHelper.GetSysParam<int>("crm.port");
            _orgName = ConfigHelper.GetSysParam<string>("crm.orgName");
            _userName = ConfigHelper.GetSysParam<string>("crm.userName");
            _password = ConfigHelper.GetSysParam<string>("crm.password");

            InitializationCRMService(isWindowsAuth);
        }

        public OrganizationServiceProxy OrganizationService
        {
            get { return _orgService; }
        }

        private void InitializationCRMService(bool isWindowsAuth)
        {
            try
            { 
                ServicePointManager.Expect100Continue = false;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                var serviceUrl = ConfigHelper.GetSysParam<string>("crm.serviceURL");             
                var credentials = new ClientCredentials();
                //AuthenticationCredentials authCredentials = new AuthenticationCredentials();

                credentials.Windows.ClientCredential = isWindowsAuth
                                                           ? CredentialCache.DefaultNetworkCredentials
                                                           : new NetworkCredential(_userName, _password, _domain);

                var organizationUri = new Uri(serviceUrl); //new Uri(string.Format(serviceUrl, _serverName, _port, _orgName));
                 
                 
                _orgService = new OrganizationServiceProxy(organizationUri, null, credentials, null);
                _orgService.ServiceConfiguration.CurrentServiceEndpoint.Behaviors.Add(new ProxyTypesBehavior());
                _orgService.Timeout = new TimeSpan(0, 10, 0);
            }
            catch (Exception ex)
            {
                ExceptionPolicyExtension.HandleExceptionForLogOnly(ex);
                throw;
            }
        }

        /// <summary>
        /// Obtain the AuthenticationCredentials based on AuthenticationProviderType.
        /// </summary>
        /// <param name="service">A service management object.</param>
        /// <param name="endpointType">An AuthenticationProviderType of the CRM environment.</param>
        /// <returns>Get filled credentials.</returns>
        //private AuthenticationCredentials GetCredentials<TService>(IServiceManagement<TService> service, AuthenticationProviderType endpointType)
        //{
        //    AuthenticationCredentials authCredentials = new AuthenticationCredentials();

        //    switch (endpointType)
        //    {
        //        case AuthenticationProviderType.ActiveDirectory:
        //            authCredentials.ClientCredentials.Windows.ClientCredential =
        //                new System.Net.NetworkCredential(_userName,
        //                    _password,
        //                    _domain);
        //            break;
        //        case AuthenticationProviderType.LiveId:
        //            authCredentials.ClientCredentials.UserName.UserName = _userName;
        //            authCredentials.ClientCredentials.UserName.Password = _password;
        //            authCredentials.SupportingCredentials = new AuthenticationCredentials();
        //            authCredentials.SupportingCredentials.ClientCredentials =
        //                Microsoft.Crm.Services.Utility.DeviceIdManager.LoadOrRegisterDevice();
        //            break;
        //        default: // For Federated and OnlineFederated environments.                    
        //            authCredentials.ClientCredentials.UserName.UserName = _userName;
        //            authCredentials.ClientCredentials.UserName.Password = _password;
        //            // For OnlineFederated single-sign on, you could just use current UserPrincipalName instead of passing user name and password.
        //            // authCredentials.UserPrincipalName = UserPrincipal.Current.UserPrincipalName;  // Windows Kerberos

        //            // The service is configured for User Id authentication, but the user might provide Microsoft
        //            // account credentials. If so, the supporting credentials must contain the device credentials.
        //            if (endpointType == AuthenticationProviderType.OnlineFederation)
        //            {
        //                IdentityProvider provider = service.GetIdentityProvider(authCredentials.ClientCredentials.UserName.UserName);
        //                if (provider != null & provider.IdentityProviderType == IdentityProviderType.LiveId)
        //                {
        //                    authCredentials.SupportingCredentials = new AuthenticationCredentials();
        //                    authCredentials.SupportingCredentials.ClientCredentials =
        //                        Microsoft.Crm.Services.Utility.DeviceIdManager.LoadOrRegisterDevice();
        //                }
        //            }

        //            break;
        //    }

        //    return authCredentials;
        //}

        public Dictionary<string, string> GetEntitysByEntityName(string entityName, string keyName, string valueName)
        {
            var retVal = new Dictionary<string, string>();
            var query = new QueryExpression
            {
                EntityName = entityName,
                ColumnSet = new ColumnSet(keyName, valueName),
                Criteria = new FilterExpression()
            };

            IEnumerable<Entity> entities = RetrieveMultipleEntity(query);

            foreach (var item in entities)
            {
                retVal.Add(item.Attributes[keyName].ToString(), item.Attributes[valueName].ToString());
            }

            return retVal;
        }

        public Dictionary<int, string> GetOptionSetByName(string optionName)
        {
            var dicOptionSet = new Dictionary<int, string>(); 
            var retrieveOptionSetRequest = new RetrieveOptionSetRequest { Name = optionName };
            var retrieveOptionSetResponse = (RetrieveOptionSetResponse)_orgService.Execute(retrieveOptionSetRequest);
            var optionSet = (OptionSetMetadata)retrieveOptionSetResponse.OptionSetMetadata; 
             
            return ConvertOptionMetadataCollectionToDictionary(dicOptionSet, optionSet.Options);
        }

        private Dictionary<int, string> ConvertOptionMetadataCollectionToDictionary(Dictionary<int, string> dicOptionSet, IEnumerable<OptionMetadata> optionset, bool enlanguage = false)
        {
            foreach (var item in optionset)
            {
                if (item.Value.HasValue)
                {
                    if (enlanguage && item.Label.UserLocalizedLabel.LanguageCode != 1033)  // en
                    {
                        continue;
                    }

                    dicOptionSet.Add(item.Value.Value, item.Label.UserLocalizedLabel.Label);
                }
            }

            return dicOptionSet;
        }
         
        public Guid CreateEntity(Entity entity)
        {
            return _orgService.Create(entity);
        }

        public void DeleteEntity(Entity entity)
        {
            _orgService.Delete(entity.LogicalName, entity.Id);
        }

        public void UpdateEntity(Entity entity)
        {
            _orgService.Update(entity);
        }

        public Entity RetrieveEntity(string entityName, Guid id, ColumnSet columnSet)
        {
            return _orgService.Retrieve(entityName, id, columnSet);
        }

        public IEnumerable<Entity> RetrieveMultipleEntity(QueryExpression query)
        {
            return _orgService.RetrieveMultiple(query).Entities;
        }
         
        public IEnumerable<Entity> RetrieveMultipleEntity(QueryByAttribute query)
        {
            return _orgService.RetrieveMultiple(query).Entities;
        }

        public IEnumerable<Entity> RetrieveMultipleEntity(FetchExpression query)
        {
            return _orgService.RetrieveMultiple(query).Entities;
        }

        public EntityCollection RetrieveMultiple(QueryExpression query)
        {
            return _orgService.RetrieveMultiple(query);
        }

        public OrganizationResponse Execute(OrganizationRequest request)
        {
            return _orgService.Execute(request);
        }

        public void Dispose()
        {
            if (_orgService != null)
            {
                _orgService.Dispose();
            }
        }
    }
}