using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Fiddler;

namespace ArchiveBookScrape
{
    public class FiddlerProxy
    {
        //Setting machine trust as CefSharp is cross-process. Admin priviledge required.
        private static bool setMachineTrust(X509Certificate2 oRootCert)
        {
            try
            {
                X509Store certStore = new X509Store(StoreName.Root,
                                                    StoreLocation.LocalMachine);
                X509KeyStorageFlags storageFlags = X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet;

                X509Certificate2 certWithPersistedKey =
                    new X509Certificate2(
                        oRootCert.Export(X509ContentType.Pkcs12, ""),
                        "",
                        storageFlags);
                certStore.Open(OpenFlags.ReadWrite);
                try
                {
                    certStore.Add(certWithPersistedKey);
                }
                finally
                {
                    certStore.Close();
                }
                return true;
            }
            catch (Exception eX)
            {
                return false;
            }
        }

        public void InitializeCertificate()
        {
            //var certCheck = GetFiddlerCert();
            string filename = AppDomain.CurrentDomain.BaseDirectory + "FiddlerCert";
            bool installed = File.Exists(filename);
            Debug.WriteLine("Certificate installed: " + installed);

            if (installed)
            {
                string[] cert = Encoding.UTF8.GetString(File.ReadAllBytes(filename)).Split(':');

                Debug.WriteLine("Cert: " + cert[0]);
                Debug.WriteLine("Key: " + cert[1]);

                FiddlerApplication.Prefs.SetStringPref("fiddler.certmaker.bc.cert",
                    cert[0]); // Read the Key from Application Configuration
                FiddlerApplication.Prefs.SetStringPref("fiddler.certmaker.bc.key",
                    cert[1]); // Read the Cert from Application Configuration
            }
            else
            {
                if (!Fiddler.CertMaker.createRootCert())
                    throw new Exception("Can't create root certificate");
                if (!setMachineTrust(Fiddler.CertMaker.GetRootCertificate()))
                    throw new Exception("Can't trust certificate");

                string cert = FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.bc.cert", null)
                    + ":" + FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.bc.key", null);
                
                File.WriteAllBytes(filename, Encoding.UTF8.GetBytes(cert));
            }
        }

        public async Task<string> Initialize()
        {
            InitializeCertificate();

            //FiddlerApplication.BeforeResponse += delegate (Session oS)
            //{
            //    Debug.WriteLine($"Proxing : {oS.ResponseHeaders.ToString()}");
            //};

            // The default flags are your best bet
            FiddlerCoreStartupFlags oFCSF = 
                (FiddlerCoreStartupFlags.Default & ~FiddlerCoreStartupFlags.RegisterAsSystemProxy);

            // Start listening on random port
            FiddlerApplication.Startup(8877, oFCSF);

            int port = FiddlerApplication.oProxy.ListenPort;
            string proxy = "127.0.0.1" + $":{ port}";

            while (!FiddlerApplication.IsStarted())
            {
                Debug.WriteLine("Starting Proxy");
                await Task.Delay(3000);
            }
            
            //Fiddler.URLMonInterop.SetProxyInProcess("127.0.0.1:8877", "<-loopback>");
            return proxy;
        }

        public void Stop()
        {
            FiddlerApplication.Shutdown();
        }
    }
}
