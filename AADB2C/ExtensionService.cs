using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace AADB2C
{
    public class ExtensionService
    {
        private readonly string extensionPrefix;

        public ExtensionService(IConfiguration configuration)
        {
            var extensionsAppId = configuration.GetValue<string>("AAD_B2C_EXTENSIONS_APP_ID").Replace("-", string.Empty);
            extensionPrefix = $"extension_{extensionsAppId}_";
        }

        public IDictionary<string, object>? TransformUserData(IDictionary<string, object> userData) => 
            userData?.ToDictionary(extension => $"{extensionPrefix}{extension.Key}", extension => extension.Value);

        public string GetExtensionByName(string extensionName) => $"{extensionPrefix}{extensionName}";
    }
}
