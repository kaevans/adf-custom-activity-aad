using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

/*  For more information:
 *  - Use custom activities in an Azure Data Factory pipeline https://docs.microsoft.com/en-us/azure/data-factory/transform-data-using-dotnet-custom-activity
 *  - Authenticating to Azure AD in daemon apps with certificates https://azure.microsoft.com/en-us/resources/samples/active-directory-dotnet-daemon-certificate-credential/
 */

namespace customactivity
{
    class Program
    {
        //
        // The Client ID is used by the application to uniquely identify itself to Azure AD.
        // The Cert Name is the subject name of the certificate used to authenticate this application to Azure AD.
        // The Tenant is the name of the Azure AD tenant in which this application is registered.
        // The AAD Instance is the instance of Azure, for example public Azure or Azure China.
        // The Authority is the sign-in URL of the tenant.
        //
        private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string certName = ConfigurationManager.AppSettings["ida:CertName"];

        static string authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);

        

        //URLs for Key Vault
        private static string keyVaultUri = ConfigurationManager.AppSettings["keyvault:VaultUri"];
        private static string keyVaultSecretName = ConfigurationManager.AppSettings["keyvault:SecretName"];

        private static AuthenticationContext authContext = null;
        private static ClientAssertionCertificate certCred = null;

        private static int errorCode;

        static int Main(string[] args)
        {
            // Return code so that exceptions provoke a non null return code for the daemon
            errorCode = 0;

            errorCode = RunAsync().GetAwaiter().GetResult();


            return errorCode;
        }

        static async Task<int> RunAsync()
        {
            // Return code so that exceptions provoke a non null return code for the daemon
            errorCode = 0;

            // Create the authentication context to be used to acquire tokens.
            authContext = new AuthenticationContext(authority);

            // Initialize the Certificate Credential to be used by ADAL.
            X509Certificate2 cert = ReadCertificateFromStore(certName);
            if (cert == null)
            {
                Console.WriteLine($"Cannot find active certificate '{certName}' in certificates for current user. Please check configuration");
                return -1;
            }

            // Then create the certificate credential client assertion.
            certCred = new ClientAssertionCertificate(clientId, cert);

            // Call the desired service

            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(
                   (authority, resource, scope) => GetAccessToken(authority, resource, scope, certCred)));

            var secret = await keyVaultClient.GetSecretAsync(keyVaultUri, keyVaultSecretName);
            Console.WriteLine(secret.Value);
            return errorCode;
        }


        /// <summary>
        /// Reads the certificate
        /// </summary>
        private static X509Certificate2 ReadCertificateFromStore(string certName)
        {
            X509Certificate2 cert = null;
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certCollection = store.Certificates;

            // Find unexpired certificates.
            X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

            // From the collection of unexpired certificates, find the ones with the correct name.
            X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certName, false);

            // Return the first certificate in the collection, has the right name and is current.
            cert = signingCert.OfType<X509Certificate2>().OrderByDescending(c => c.NotBefore).FirstOrDefault();
            store.Close();
            return cert;
        }


        /// <summary>
        /// Get an access token from Azure AD using client credentials.
        /// If the attempt to get a token fails because the server is unavailable, retry twice after 3 seconds each
        /// </summary>
        private static async Task<string> GetAccessToken(string authority, string resource, string scope, ClientAssertionCertificate assertionCert)
        {
            //
            // Get an access token from Azure AD using client credentials.
            // If the attempt to get a token fails because the server is unavailable, retry twice after 3 seconds each.
            //
            AuthenticationResult result = null;
            int retryCount = 0;
            bool retry = false;

            do
            {
                retry = false;
                errorCode = 0;

                try
                {
                    // ADAL includes an in memory cache, so this call will only send a message to the server if the cached token is expired.
                    result = await authContext.AcquireTokenAsync(resource, assertionCert);
                }
                catch (AdalException ex)
                {
                    if (ex.ErrorCode == "temporarily_unavailable")
                    {
                        retry = true;
                        retryCount++;
                        Thread.Sleep(3000);
                    }

                    Console.WriteLine(
                        String.Format("An error occurred while acquiring a token\nTime: {0}\nError: {1}\nRetry: {2}\n",
                        DateTime.Now.ToString(),
                        ex.ToString(),
                        retry.ToString()));

                    errorCode = -1;
                }

            } while ((retry == true) && (retryCount < 3));
            return result.AccessToken;
        }


    }

}
