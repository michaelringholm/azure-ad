using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Graph;
using System;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Newtonsoft.Json;

namespace azure_ad_console
{
    // https://developer.microsoft.com/en-us/graph/graph-explorer?request=users?$filter=startswith(givenName,%27J%27)&method=GET&version=v1.0#
    // https://developer.microsoft.com/en-us/graph/docs/concepts/auth_v2_service
    public class MicrosoftGraphAPI : IAuthProvider
    {
        private GraphServiceClient _graphClient;
        private IConfiguration _configuration;
        private String _token;

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
            _token = azureServiceTokenProvider.GetAccessTokenAsync(microsoftGraphEndpoint).ConfigureAwait(false).GetAwaiter().GetResult();

            Task authenticationDelegate(System.Net.Http.HttpRequestMessage req)
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                return Task.CompletedTask;
            }

            _graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(authenticationDelegate));
        }

        private string callEndpoint(string url)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            HttpResponseMessage response = client.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            string responseBody = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            return responseBody;
        }        

        public string GetGroupId(string displayName, string onPremisesSid)
        {
            if (string.IsNullOrWhiteSpace(displayName) && string.IsNullOrWhiteSpace(onPremisesSid))
                return "";

            bool hasOnPremSid = !string.IsNullOrWhiteSpace(onPremisesSid);
            string filter = hasOnPremSid ? $"onPremisesSecurityIdentifier eq '{onPremisesSid}'" : $"displayName eq '{displayName}'";
            var azureGroup = _graphClient.Groups.Request().Filter(filter).GetAsync().ConfigureAwait(false).GetAwaiter().GetResult().SingleOrDefault();
            return azureGroup == null ? "" : azureGroup.Id;
        }

        public IEnumerable<Object> GetGroupMembers(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return new List<Object>();

            const int MaxUsersPerGroup = 500;

            var members = _graphClient.Groups[groupId].Members.Request().Top(MaxUsersPerGroup).GetAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            // A group member can also be a sub-group, skip everything that is not a user
            //var result = members.Where(m => m is Microsoft.Graph.User).Cast<Microsoft.Graph.User>().Select(u => new Object{ u.Id, u.UserPrincipalName, u.DisplayName});
            return null;
        }

        public IEnumerable<Group> GetGroups()
        {            
            var currentPage = _graphClient.Groups.Request().OrderBy("DisplayName").GetAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var groups = currentPage.ToList();
            
            while(currentPage.NextPageRequest != null) {                
                currentPage = currentPage.NextPageRequest.GetAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                groups.AddRange(currentPage.ToList());
            }
            return groups;
        }

        public IEnumerable<SimpleGroup> GetGroups(String userId)
        {            
            var url = $"https://graph.microsoft.com/v1.0/users/{userId}/memberOf";
            String responseBody = callEndpoint(url);
            var oData = JsonConvert.DeserializeObject<OData>(responseBody);            
            return oData.Value;
        }

        public IEnumerable<User> GetUsers()
        {            
            var currentPage = _graphClient.Users.Request().OrderBy("UserPrincipalName").GetAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var users = currentPage.ToList();
            
            while(currentPage.NextPageRequest != null) {                
                currentPage = currentPage.NextPageRequest.GetAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                users.AddRange(currentPage.ToList());
            }
            return users;
        }

        public User GetUser(String userPrincipalName)
        {            
            var currentPage = _graphClient.Users.Request().Filter($"userPrincipalName eq '{userPrincipalName}'").GetAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var user = currentPage.SingleOrDefault();
            return user;
        }        

        public Group CreateGroup(String groupName)
        {            
            var group = _graphClient.Groups.Request().OrderBy("DisplayName").GetAsync().ConfigureAwait(false).GetAwaiter().GetResult();
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
            }).ConfigureAwait(false).GetAwaiter().GetResult();
            return user;
        }
    }
}
