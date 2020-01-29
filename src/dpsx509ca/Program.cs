using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace dpsx509ca
{
    public static class Program
    {
        // The Provisioning Hub IDScope.

        // For this sample either:
        // - pass this value as a command-prompt argument
        // - set the DPS_IDSCOPE environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_idScope = "0ne000B785B";

        // In your Device Provisioning Service please go to "Manage enrollments" and select "Individual Enrollments".
        // Select "Add individual enrollment" then fill in the following:
        // Mechanism: X.509
        // Certificate: 
        //    You can generate a self-signed certificate by running the GenerateTestCertificate.ps1 powershell script.
        //    Select the public key 'certificate.cer' file. ('certificate.pfx' contains the private key and is password protected.)
        //    For production code, it is advised that you install the certificate in the CurrentUser (My) store.
        // DeviceID: iothubx509device1

        // X.509 certificates may also be used for enrollment groups.
        // In your Device Provisioning Service please go to "Manage enrollments" and select "Enrollment Groups".
        // Select "Add enrollment group" then fill in the following:
        // Group name: <your  group name>
        // Attestation Type: Certificate
        // Certificate Type: 
        //    choose CA certificate then link primary and secondary certificates 
        //    OR choose Intermediate certificate and upload primary and secondary certificate files
        // You may also change other enrollemtn group parameters according to your needs

        private const string GlobalDeviceEndpoint = "global.azure-devices-provisioning.net";
        private static string s_certificateFileName = "../../device.pfx";

        public static int Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(s_idScope) && (args.Length > 0))
            {
                s_idScope = args[0];
            }

            if (string.IsNullOrWhiteSpace(s_idScope))
            {
                Console.WriteLine("ProvisioningDeviceClientX509 <IDScope>");
                return 1;
            }

            X509Certificate2 certificate = LoadProvisioningCertificate();

            using (var security = new SecurityProviderX509Certificate(certificate))

            // Select one of the available transports:
            // To optimize for size, reference only the protocols used by your application.
            using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly))
            // using (var transport = new ProvisioningTransportHandlerHttp())
            // using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly))
            // using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly))
            {
                ProvisioningDeviceClient provClient =
                    ProvisioningDeviceClient.Create(GlobalDeviceEndpoint, s_idScope, security, transport);

                var sample = new ProvisioningDeviceClientSample(provClient, security);
                sample.RunSampleAsync().GetAwaiter().GetResult();
            }

            return 0;
        }

        private static X509Certificate2 LoadProvisioningCertificate()
        {
            var certificateCollection = new X509Certificate2Collection();
            var dir = System.IO.Directory.GetCurrentDirectory();
            certificateCollection.Import(s_certificateFileName);

            X509Certificate2 certificate = null;

            foreach (X509Certificate2 element in certificateCollection)
            {
                Console.WriteLine($"Found certificate: {element?.Thumbprint} {element?.Subject}; PrivateKey: {element?.HasPrivateKey}");
                if (certificate == null && element.HasPrivateKey)
                {
                    certificate = element;
                }
                else
                {
                    element.Dispose();
                }
            }

            if (certificate == null)
            {
                throw new FileNotFoundException($"{s_certificateFileName} did not contain any certificate with a private key.");
            }
            else
            {
                Console.WriteLine($"Using certificate {certificate.Thumbprint} {certificate.Subject}");
            }

            return certificate;
        }
    }
}
