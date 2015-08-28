// Copyright Bernhard Klefer (BTC AG, Escherweg 5, 26121 Oldenburg, Germany)
// This file is part of the BTC.UI5Uploader
// BTC.UI5Uploader ist distributed under the BSD 2-clause license (full text see file license).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BTC.UI5Uploader
{
   /// <summary>
   /// The upload engine.
   /// Construct the engine with the path to a eclipse ui5 project folder. 
   /// If existing, the .Ui5RepositoryUploadParameters is then read to configure the default engine settings.
   /// Configure the Settings of the Engine with its properties and call <see cref="Upload()"/> to start the Upload.
   /// </summary>
   class Engine
   {
      /// <summary>
      /// Path to the eclipse ui5 project to upload. You can  call <see cref="RetrieveSettingsFromConfig()"/> after assigning a new value to this property to update the engine settings.
      /// </summary>
      public string ProjectDir { get; set; }
      /// <summary>
      /// Target name of the UI5 application in the ABAP repository.
      /// </summary>
      public string AppName { get; set; }
      /// <summary>
      /// Description of the UI5 application used for creation in the ABAP repository.
      /// </summary>
      public string AppDescription { get; set; }
      /// <summary>
      /// The package (devclass) to upload the application to. 
      /// </summary>
      public string Package { get; set; }
      /// <summary>
      /// The transport request to register the uploaded items to. Can be empty if <see cref="Package"/> is <c>"$TMP"</c>.
      /// </summary>
      public string TransportRequest { get; set; }
      /// <summary>
      /// Accessinformation and Credentials to acces the SAP system. If <see cref="Credentials.Username"/> is <c>null</c>, a single sign on authentification is performed.
      /// </summary>
      public Credentials Credentials { get; set; }
      /// <summary>
      /// <c>true</c> to enable a test upload. The Upload is simulated an nothing is actually written in the SAP system.
      /// </summary>
      public bool TestMode { get; set; }
      /// <summary>
      /// Enable Delta mode to only register changed files in the <see cref="TransportRequest"/>.
      /// </summary>
      public bool DeltaMode { get; set; }
      /// <summary>
      /// The external codepage to use in the bsp and mime repostitory. Default is utf-8.
      /// </summary>
      public string ExternalCodepage { get; set; }
      /// <summary>
      /// Timeout for the upload process.
      /// </summary>
      public int Timeout { get; set; }
      /// <summary>
      /// Ignore certificate errors when connecting through https.
      /// </summary>
      public bool IgnoreCertificateErrors { get; set; }
      /// <summary>
      /// List of ignore masks. Each file to upload is tested against the ignore masks. If it matches, it is not uploaded. 
      /// Either a literal match or a Regex if the string starts with '^' and ends with '$'. Both variants are case sensitive.
      /// </summary>
      public List<string> IgnoreMasks { get; private set; }

      private string cleanedConfigFileContent;

      /// <summary>
      /// Creates a new instance. Please assign a <see cref="ProjectDir"/> afterwards.
      /// </summary>
      public Engine()
      {
         this.IgnoreMasks = new List<string> { "WebContent/META-INF", "WebContent/WEB-INF", ".Ui5RepositoryUploadParameters" }; //Both folders are used by eclipses webserver for lokal testing. .Ui5RepositoryUploadParameters will be zipped separately after some modifications.
         this.DeltaMode = true;
         this.ExternalCodepage = "utf-8";
         Timeout = 30000;
      }
      /// <summary>
      /// Creates a new instace and configures it for the given eclipse ui5 project.
      /// </summary>
      /// <param name="projectDir">Path to the eclipse ui5 project to upload</param>
      public Engine(string projectDir)
         : this()
      {
         this.ProjectDir = projectDir;
         RetrieveSettingsFromConfig();
      }
      /// <summary>
      /// Reads the .Ui5RepositoryUploadParameters in the <see cref="ProjectDir"/> and configures this instance with the settings.
      /// </summary>
      public void RetrieveSettingsFromConfig()
      {
         try
         {
            if (string.IsNullOrEmpty(ProjectDir)) throw new InvalidOperationException("ProjectDir not set.");
            AppName = System.IO.Path.GetFileName(ProjectDir);
            Package = "$TMP";
            TransportRequest = string.Empty;

            var configFile = Path.Combine(ProjectDir, ".Ui5RepositoryUploadParameters");
            if (File.Exists(configFile))
            {
               var configFileContent = File.ReadAllText(configFile);
               configFileContent = System.Text.RegularExpressions.Regex.Replace(configFileContent, @"SAPUI5ApplicationName=(.+)", m => { AppName = m.Groups[1].Value.Trim(); return string.Empty; });
               configFileContent = System.Text.RegularExpressions.Regex.Replace(configFileContent, @"SAPUI5ApplicationPackage=(.+)", m => { Package = m.Groups[1].Value.Trim(); return string.Empty; });
               configFileContent = System.Text.RegularExpressions.Regex.Replace(configFileContent, @"WorkbenchRequest=(.+)", m => { TransportRequest = m.Groups[1].Value.Trim(); return string.Empty; });
               configFileContent = System.Text.RegularExpressions.Regex.Replace(configFileContent, @"SAPUI5ApplicationDescription=(.+)", m => { AppDescription = m.Groups[1].Value.Trim(); return string.Empty; });
               configFileContent = System.Text.RegularExpressions.Regex.Replace(configFileContent, @"ExternalCodePage=(.+)", m => { ExternalCodepage = m.Groups[1].Value.Trim(); return string.Empty; });
               cleanedConfigFileContent = configFileContent;
            }
            else
            {
               Log.Instance.Write("Warning: Configuration File \".Ui5RepositoryUploadParameters\" not found in project folder.");
            }
            var ignoreFile = Path.Combine(ProjectDir, ".Ui5RepositoryIgnore");
            if (File.Exists(ignoreFile))
            {
               IgnoreMasks.AddRange(File.ReadAllLines(ignoreFile).Where(l => !string.IsNullOrWhiteSpace(l)));
            }
         }
         catch (Exception e)
         {
            Log.Instance.Write(e.ToString());
         }
      }

      /// <summary>
      /// Uploads the UI5 Project int <see cref="ProjectDir"/> to the SAP system specified by <see cref="Credentials"/>. If <see cref="Credentials.Username"/> is <c>null</c>, a single sign on authentification is performed.
      /// </summary>
      public void Upload()
      {
         if (string.IsNullOrWhiteSpace(ProjectDir)) throw new InvalidOperationException("ProjectDir not set.");
         if (!Directory.Exists(ProjectDir)) throw new InvalidOperationException("ProjectDir not set to a folder.");
         if (Credentials == null) throw new InvalidOperationException("Credentials not set.");
         if (string.IsNullOrWhiteSpace(Credentials.System)) throw new InvalidOperationException("Credentials.System not set.");
         if (string.IsNullOrWhiteSpace(AppName)) throw new InvalidOperationException("AppName not set.");

         if (TestMode) Log.Instance.Write("Test mode on!");

         try
         {
            var uri = new Uri(this.Credentials.System);
            uri = uri.AddQuery("sap-client", Credentials.Mandant.ToString());
            uri = uri.AddQuery("sapui5applicationname", AppName);
            uri = uri.AddQuery("sapui5applicationdescription", AppDescription);
            uri = uri.AddQuery("sapui5applicationpackage", Package);
            uri = uri.AddQuery("workbenchrequest", TransportRequest);
            uri = uri.AddQuery("externalcodepage", ExternalCodepage);
            uri = uri.AddQuery("deltamode", DeltaMode ? "true" : "false");
            uri = uri.AddQuery("testmode", TestMode ? "true" : "false");

            if (IgnoreCertificateErrors)
            {
               System.Net.ServicePointManager.ServerCertificateValidationCallback += ServerCertificateValidationIgnoreAll;
            }

            var cookieContainer = new System.Net.CookieContainer();
            //Try to Authenticate with a HEAD request and retrieve an authentification token cookie before uploading the whole application.
            var headRequest = System.Net.HttpWebRequest.Create(uri);
            headRequest.PreAuthenticate = false;
            headRequest.Timeout = 10000;
            headRequest.Method = System.Net.WebRequestMethods.Http.Head;
            ((System.Net.HttpWebRequest)headRequest).CookieContainer = cookieContainer;

            if (Credentials.Username != null)
            {
               headRequest.Credentials = new System.Net.NetworkCredential(Credentials.Username, Credentials.Password);
            }
            else //SSO
            {
               headRequest.ImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
               headRequest.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
            }
            headRequest.GetResponse().Close();

            //The actual POST request with the ziped project.
            var request = System.Net.HttpWebRequest.Create(uri);
            request.Timeout = Timeout * 1000;
            request.PreAuthenticate = true;
            request.UseDefaultCredentials = false;
            ((System.Net.HttpWebRequest)request).CookieContainer = cookieContainer; //Contains the authentification token if acquired
            if (Credentials.Username != null)                                       //if not: credentials to reauthenficiate.
            {
               request.Credentials = new System.Net.NetworkCredential(Credentials.Username, Credentials.Password);
            }
            else //SSO
            {
               request.ImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
               request.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
            }
            Log.Instance.Write("Uploading project...");
            request.Method = System.Net.WebRequestMethods.Http.Post;
            request.ContentType = "application/zip";
            using (var stream = request.GetRequestStream())
            {
               using (var zipHelper = new ZipHelper(stream))
               {
                  zipHelper.Zip(".Ui5RepositoryUploadParameters", Encoding.UTF8.GetBytes(cleanedConfigFileContent));
                  zipHelper.Zip(ProjectDir, new[] { ".Ui5Repository*", "WebContent" }, IgnoreMasks.ToArray());
               }
               stream.Close();
            }
            Log.Instance.Write("Installing App...");
            var response = (System.Net.HttpWebResponse)request.GetResponse();
            HandleResponse(response);
         }
         catch (System.Net.WebException ex)
         {
            if (ex.Response != null)
            {
               HandleResponse((System.Net.HttpWebResponse)ex.Response);
            }
            else
            {
               Log.Instance.Write(ex.ToString());
               Log.Instance.Write("Upload failed!");
               throw new UploadFailedException();
            }
         }
      }

      /// <summary>
      /// Eventhandler for the System.Net.ServicePointManager.ServerCertificateValidationCallback that accepts all certificates.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="certificate"></param>
      /// <param name="chain"></param>
      /// <param name="sslPolicyErrors"></param>
      /// <returns>Always <c>true</c></returns>
      private bool ServerCertificateValidationIgnoreAll(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
      {
         if (sslPolicyErrors != System.Net.Security.SslPolicyErrors.None)
         {
            Log.Instance.Write("Certificate has errors. Ignoring.");
         }
         return true;
      }
      /// <summary>
      /// Write something about the response to the Log and raise some exceptions if the upload failed.
      /// </summary>
      /// <param name="response">The WebResponse</param>
      private void HandleResponse(System.Net.HttpWebResponse response)
      {
         using (var reader = new StreamReader(response.GetResponseStream()))
         {
            Log.Instance.Write("### Begin of response ##########################" + Environment.NewLine + reader.ReadToEnd().Replace("\n", "\r\n"));
            Log.Instance.Write("### End of response   ##########################");
         }
         switch (response.StatusCode)
         {
            case System.Net.HttpStatusCode.OK:
               Log.Instance.Write("Upload successfull!");
               break;
            case System.Net.HttpStatusCode.PartialContent:
               Log.Instance.Write("Upload finished with warnings.");
               break;
            case System.Net.HttpStatusCode.Unauthorized:
               Log.Instance.Write("Not Authorized!");
               throw new NotAuthorizedException();
            default:
               Log.Instance.Write("Upload failed!");
               throw new UploadFailedException();
         }
         response.Close();
      }

   }
}
