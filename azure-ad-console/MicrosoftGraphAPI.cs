using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Graph;
using System;
using Microsoft.Extensions.Configuration;

namespace azure_ad_console
{
    public class MicrosoftGraphAPI : IAuthProvider
    {
        private GraphServiceClient _graphClient;
        private IConfiguration _configuration;

        public MicrosoftGraphAPI(IConfiguration configuration)
        {
            _configuration = configuration;
            string connectionString = null;
            string appId = _configuration.GetSection("appId").Value;
            string appSecret = _configuration.GetSection("appSecret").Value;
            string tenantId = _configuration.GetSection("tenantId").Value;
            connectionString = $"RunAs=App;AppId={appId};TenantId={tenantId};AppKey={appSecret}";

            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider(connectionString);

            string microsoftGraphEndpoint = "https://graph.microsoft.com";
            string token = azureServiceTokenProvider.GetAccessTokenAsync(microsoftGraphEndpoint).GetAwaiter().GetResult();

            Task authenticationDelegate(System.Net.Http.HttpRequestMessage req)
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return Task.CompletedTask;
            }

            _graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(authenticationDelegate));
        }

        public string GetGroupId(string displayName, string onPremisesSid)
        {
            if (string.IsNullOrWhiteSpace(displayName) && string.IsNullOrWhiteSpace(onPremisesSid))
                return "";

            bool hasOnPremSid = !string.IsNullOrWhiteSpace(onPremisesSid);
            string filter = hasOnPremSid ? $"onPremisesSecurityIdentifier eq '{onPremisesSid}'" : $"displayName eq '{displayName}'";
            var azureGroup = _graphClient.Groups.Request().Filter(filter).GetAsync().GetAwaiter().GetResult().SingleOrDefault();
            return azureGroup == null ? "" : azureGroup.Id;
        }

        public IEnumerable<Object> GetGroupMembers(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return new List<Object>();

            const int MaxUsersPerGroup = 500;

            var members = _graphClient.Groups[groupId].Members.Request().Top(MaxUsersPerGroup).GetAsync().GetAwaiter().GetResult();
            // A group member can also be a sub-group, skip everything that is not a user
            //var result = members.Where(m => m is Microsoft.Graph.User).Cast<Microsoft.Graph.User>().Select(u => new Object{ u.Id, u.UserPrincipalName, u.DisplayName});
            return null;
        }

        public IEnumerable<Group> GetGroups()
        {            
            var groups = _graphClient.Groups.Request().OrderBy("DisplayName").GetAsync().GetAwaiter().GetResult();
            return groups;
        }

        public IEnumerable<User> GetUsers()
        {            
            var users = _graphClient.Users.Request().OrderBy("UserPrincipalName").GetAsync().GetAwaiter().GetResult();
            return users;
        }

        public Group CreateGroup(String groupName)
        {            
            var group = _graphClient.Groups.Request().OrderBy("DisplayName").GetAsync().GetAwaiter().GetResult();
            return null;
        } 

        public User CreateUser(String userPrincipalName, String displayName, String mailNickName, String password) {
            var user = _graphClient.Users.Request().AddAsync(new User
            {
                AccountEnabled = true,
                DisplayName = displayName,
                MailNickname = mailNickName,
                PasswordProfile = new PasswordProfile
                {
                    Password = password
                },
                UserPrincipalName = userPrincipalName
            }).GetAwaiter().GetResult();
            return user;
        }
    }
}
