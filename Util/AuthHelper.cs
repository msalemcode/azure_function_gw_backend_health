using System;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.Services.AppAuthentication;

namespace GatwaybackendHealth.Util
{
    class AuthHelper
    {
        public static async Task<string> GetTokenAsync()
        {
            var target = "https://management.azure.com";
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync(target);
            return accessToken;

        }
        
    }
}
