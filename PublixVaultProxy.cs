using System.Net;
using System.Net.Http;
using System.Runtime.Remoting;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PublixVaultProxy
{
    public interface IVaultProxyService
    {
        Task<bool> RetrieveSecretFromRemoteVaultAsync();
        string VaultUrl { get; set; }
    }
    public class VaultProxyService : IVaultProxyService
    {
        private  readonly HttpClient _httpClient;
        private static string CacheFilePath = "";
        public string VaultUrl { get; set; }    

        public VaultProxyService(string vaultUrl,HttpClient httpClient)
        {
           VaultUrl = vaultUrl;
            _httpClient = httpClient;   
        }


        /// <summary>
        /// Accessing the vault with the given address for secret using an http client instance
        /// Returns the secret (string) if vault is accessable or returns null
        /// Http client disposes once action completes or exception throws
        /// </summary>

        private static string Encrypt(string plainText, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, scope);
            File.WriteAllBytes(CacheFilePath, encryptedBytes);

            return Convert.ToBase64String(encryptedBytes);

        }

        public async Task<bool> RetrieveSecretFromRemoteVaultAsync()
        {

            try
            {
                var response = await _httpClient.GetAsync(VaultUrl);
                await Task.Delay(500);
                if( response.StatusCode != HttpStatusCode.OK)
                {
                   
                    return false;
                }
                string secret = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(secret))
                {
                    CacheFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "offlineToken.dat");
                    Encrypt(secret);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (HttpRequestException)
            {
                return false;

            }
        }


    }
}
