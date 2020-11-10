using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Net;

namespace AADB2C
{
    public class Functions
    {
        private readonly IConfidentialClientApplication confidentialClientApplication;
        private readonly IGraphServiceClient graphServiceClient;
        private readonly ExtensionService extensionService;
        private readonly string[] authorizationScopes = new[] { "openid" };
        private readonly string userProperties;

        private const string CustomerClaim = "Customer";

        public Functions(IConfidentialClientApplication confidentialClientApplication, IGraphServiceClient graphServiceClient, ExtensionService extensionService)
        {
            this.confidentialClientApplication = confidentialClientApplication;
            this.graphServiceClient = graphServiceClient;
            this.extensionService = extensionService;
            userProperties = $"id,displayName,identities,{extensionService.GetExtensionByName(CustomerClaim)}";
        }

        [FunctionName(nameof(B2C))]
        public async Task<IActionResult> B2C(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            var uri = await confidentialClientApplication.GetAuthorizationRequestUrl(authorizationScopes).ExecuteAsync();
            return new RedirectResult(uri.AbsoluteUri);
        }

        [FunctionName(nameof(Auth))]
        public async Task<IActionResult> Auth(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "b2c/auth")] HttpRequest req,
            ILogger log)
        {
            var code = req.Query["code"];
            var delegateAuthenticationProvider = new DelegateAuthenticationProvider(async request =>
            {
                var token = await confidentialClientApplication.AcquireTokenByAuthorizationCode(authorizationScopes, code).ExecuteAsync();
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(token.CreateAuthorizationHeader());
            });
            var userGraphServiceClient = new GraphServiceClient(delegateAuthenticationProvider);
            var me = await userGraphServiceClient.Me.Request().GetAsync();
            return new OkObjectResult(me);
        }

        [FunctionName(nameof(Users))]
        public async Task<IActionResult> Users(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users")] HttpRequest req,
            ILogger log)
        {
            var users = await graphServiceClient.Users.Request().Select(userProperties).GetAsync();
            return new OkObjectResult(users);
        }

        [FunctionName(nameof(CreateUser))]
        public async Task<IActionResult> CreateUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users")] HttpRequest req,
            ILogger log)
        {
            var payload = await req.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<User>(payload);
            user.AdditionalData = extensionService.TransformUserData(user.AdditionalData);
            await graphServiceClient.Users.Request().AddAsync(user);
            return new StatusCodeResult((int)HttpStatusCode.Created);
        }

        [FunctionName(nameof(GetUser))]
        public async Task<IActionResult> GetUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{userPrincipalName}")] HttpRequest req,
            string userPrincipalName,
            ILogger log)
        {
            var user = await graphServiceClient.Users[userPrincipalName].Request().Select(userProperties).GetAsync();
            log.LogInformation("Customer: {Customer}", user.AdditionalData[extensionService.GetExtensionByName(CustomerClaim)]);
            return new OkObjectResult(user);
        }

        [FunctionName(nameof(UpdateUser))]
        public async Task<IActionResult> UpdateUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/{userPrincipalName}")] HttpRequest req,
            string userPrincipalName,
            ILogger log)
        {
            var payload = await req.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<User>(payload);
            user.AdditionalData = extensionService.TransformUserData(user.AdditionalData);
            await graphServiceClient.Users[userPrincipalName].Request().UpdateAsync(user);
            return new StatusCodeResult((int)HttpStatusCode.NoContent);
        }

        [FunctionName(nameof(DeleteUser))]
        public async Task<IActionResult> DeleteUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "users/{userPrincipalName}")] HttpRequest req,
            string userPrincipalName,
            ILogger log)
        {
            await graphServiceClient.Users[userPrincipalName].Request().DeleteAsync();
            return new StatusCodeResult((int)HttpStatusCode.NoContent);
        }
    }
}
