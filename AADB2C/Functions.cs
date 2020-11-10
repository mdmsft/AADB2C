using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Net;

namespace AADB2C
{
    public class Functions
    {
        private readonly IConfidentialClientApplication confidentialClientApplication;
        private readonly IGraphServiceClient graphServiceClient;
        private readonly IMemoryCache memoryCache;

        public Functions(IConfidentialClientApplication confidentialClientApplication, IGraphServiceClient graphServiceClient, IMemoryCache memoryCache)
        {
            this.confidentialClientApplication = confidentialClientApplication;
            this.graphServiceClient = graphServiceClient;
            this.memoryCache = memoryCache;
        }

        [FunctionName(nameof(B2C))]
        public async Task<IActionResult> B2C(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            var uri = await confidentialClientApplication.GetAuthorizationRequestUrl(null).ExecuteAsync();
            return new RedirectResult(uri.AbsoluteUri);
        }

        [FunctionName(nameof(Auth))]
        public async Task<IActionResult> Auth(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "b2c/auth")] HttpRequest req,
            ILogger log)
        {
            var code = req.Query["code"];
            var token = await confidentialClientApplication.AcquireTokenByAuthorizationCode(null, code).ExecuteAsync();
            var cacheKey = token.Account.HomeAccountId.Identifier;
            if (!memoryCache.TryGetValue<IGraphServiceClient>(cacheKey, out var userGraphServiceClient))
            {
                var delegateAuthenticationProvider = new DelegateAuthenticationProvider(request =>
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
                    return Task.CompletedTask;
                });
                userGraphServiceClient = new GraphServiceClient(delegateAuthenticationProvider);
                var cacheEntry = memoryCache.CreateEntry(cacheKey);
                cacheEntry.SetValue(userGraphServiceClient).SetAbsoluteExpiration(token.ExpiresOn);
            }
            var me = userGraphServiceClient.Me;
            return new OkResult();
        }

        [FunctionName(nameof(CreateUser))]
        public async Task<IActionResult> CreateUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users")] HttpRequest req,
            ILogger log)
        {
            var payload = await req.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<User>(payload);
            await graphServiceClient.Users.Request().AddAsync(user);
            return new StatusCodeResult((int)HttpStatusCode.Created);
        }

        [FunctionName(nameof(UpdateUser))]
        public async Task<IActionResult> UpdateUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users")] HttpRequest req,
            ILogger log)
        {
            var payload = await req.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<User>(payload);
            await graphServiceClient.Users[user.UserPrincipalName].Request().UpdateAsync(user);
            return new StatusCodeResult((int)HttpStatusCode.OK);
        }
    }
}
