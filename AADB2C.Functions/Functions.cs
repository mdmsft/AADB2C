using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System.Net;

namespace AADB2C.Functions
{
    public class Functions
    {
        private readonly IConfidentialClientApplication confidentialClientApplication;
        private readonly IGraphServiceClient graphServiceClient;
        private readonly ExtensionService extensionService;
        private readonly AuthorizationService authorizationService;
        private readonly IApiService apiService;
        private readonly string userProperties;

        private const string CustomerClaim = "Customer";

        public Functions(IConfidentialClientApplication confidentialClientApplication, IGraphServiceClient graphServiceClient, ExtensionService extensionService, AuthorizationService authorizationService, IApiService apiService)
        {
            this.confidentialClientApplication = confidentialClientApplication;
            this.graphServiceClient = graphServiceClient;
            this.extensionService = extensionService;
            this.authorizationService = authorizationService;
            this.apiService = apiService;
            userProperties = $"{nameof(User.Id)}, {nameof(User.DisplayName)}, {nameof(User.GivenName)}, {nameof(User.Surname)}, {nameof(User.Identities)}, {extensionService.GetExtensionByName(CustomerClaim)}";
        }

        [FunctionName(nameof(Root))]
        public async Task<IActionResult> Root(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "/")] HttpRequest req,
            ILogger log)
        {
            var uri = await confidentialClientApplication.GetAuthorizationRequestUrl(authorizationService.AuthorizationScopes).ExecuteAsync();
            return new RedirectResult(uri.AbsoluteUri);
        }

        [FunctionName(nameof(B2C))]
        public async Task<IActionResult> B2C(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            var code = req.Query["code"];
            var token = await confidentialClientApplication.AcquireTokenByAuthorizationCode(authorizationService.AuthorizationScopes, code).ExecuteAsync();
            var result = await apiService.CallSecureApiAsync(token.AccessToken);
            return new OkObjectResult(result);
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
