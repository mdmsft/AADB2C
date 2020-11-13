using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace AADB2C.Functions
{
    public class ExtensionService
    {
        private readonly string extensionPrefix;

        public ExtensionService(IConfiguration configuration)
        {
            var extensionsAppId = configuration.GetValue<string>("B2C_EXTENSIONS_CLIENT_ID").Replace("-", string.Empty);
            extensionPrefix = $"extension_{extensionsAppId}_";
        }

        public IDictionary<string, object>? TransformUserData(IDictionary<string, object> userData) => 
            userData?.ToDictionary(extension => $"{extensionPrefix}{extension.Key}", extension => extension.Value);

        public string GetExtensionByName(string extensionName) =>
            $"{extensionPrefix}{extensionName}";
    }
}
