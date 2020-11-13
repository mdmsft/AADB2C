using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace AADB2C.Functions
{
    public class AuthorizationService
    {
        private readonly string tenantName;
        private readonly string clientName;

        public AuthorizationService(IConfiguration configuration)
        {
            tenantName = configuration.GetValue<string>("B2C_TENANT_NAME");
            clientName = configuration.GetValue<string>("B2C_CLIENT_NAME");
        }

        private string UserImpersonationScope =>
            $"https://{tenantName}.onmicrosoft.com/{clientName}/user_impersonation";

        public IEnumerable<string> AuthorizationScopes =>
            new[] { UserImpersonationScope };
    }
}
