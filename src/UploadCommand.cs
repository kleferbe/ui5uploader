// Copyright Bernhard Klefer (BTC AG, Escherweg 5, 26121 Oldenburg, Germany)
// This file is part of the BTC.UI5Uploader
// BTC.UI5Uploader ist distributed under the BSD 2-clause license (full text see file license).
using ManyConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTC.UI5Uploader
{
   /// <summary>
   /// Commanline command to upload a UI5 application to a SAP ABAP repository
   /// </summary>
   class UploadCommand : ConsoleCommand
   {
      public UploadCommand()
      {
         Mandant = 100;
         DeltaMode = true;
         Timeout = 30000;
         AppSubFolder = "webapp";
         IsCommand("upload", oneLineDescription: "Upload UI5 Application to a SAP Backend");
         SkipsCommandSummaryBeforeRunning();
         HasRequiredOption("s|system=", "SAP System URL", o => System = UriHelper.CreateUri(o).ToString());
         HasOption("m|mandant=", "Mandant (default: 100)", o => Mandant = o == null ? 100 : int.Parse(o));
         HasOption("u|username=", "Username. Omit for Single Sign On.", o => Username = o);

         HasOption("p|password=", "Password. You can set and save the Password with the \"password\" command. If not given and not stored, you will be prompted for it.", o => Password = o);
         HasRequiredOption("src|ProjectFolder=", "Root folder of the UI5 Project to upload", o => ProjectFolder = o);
         HasOption("app|appfolder=", "Path to the folder containing the application files. Relative to -src. Default webapp", o => AppSubFolder = o);
         HasOption("AppName=", "Target name of the UI5-Application. Extracted from .Ui5RepositoryUploadParameters if option not given.", o => AppName = o);
         HasOption("AppDescription=", "Description of the UI5-Application. Needed only when the Application is going to be created. Extracted from .Ui5RepositoryUploadParameters if option not given.", o => AppDescription = o);
         HasOption("Package=", "Target Package. Extracted from .Ui5RepositoryUploadParameters if option not given.", o => Package = o);
         HasOption("Transport=", "Transport Request. Extracted from .Ui5RepositoryUploadParameters if option not given.", o => TransportRequest = o);
         HasOption("f|force", "Skip confirmation and upload immediately.", o => SkipConfirmation = true);
         HasOption("test", "Test run. The installation will be simulated.", o => TestMode = true);
         HasOption("disableDeltaMode", "Disable delta mode which adds only changed items to the Transport.", o => DeltaMode = false);
         HasOption("ignoreCertificateErrors", "Ignore SSL certificate errors when connecting via https.", o => IgnoreCertificateErrors = true);
         HasOption("timeout=", "Time to wait for the upload and installation of the app in seconds. Default 30000", o => { if (o != null) Timeout = int.Parse(o); });
      }
      public string System { get; set; }
      public int Mandant { get; set; }
      public string Username { get; set; }
      public string Password { get; set; }
      public string ProjectFolder { get; set; }
      public string AppSubFolder { get; set; }
      public string AppName { get; set; }
      public string AppDescription { get; set; }
      public string Package { get; set; }
      public string TransportRequest { get; set; }
      public bool SkipConfirmation { get; set; }
      public bool TestMode { get; set; }
      public bool DeltaMode { get; set; }
      public int Timeout { get; set; }
      public bool IgnoreCertificateErrors { get; set; }

      public override int Run(string[] remainingArguments)
      {
         var engine = new Engine(ProjectFolder);
         if (!string.IsNullOrWhiteSpace(AppName)) engine.AppName = AppName;
         if (!string.IsNullOrWhiteSpace(AppDescription)) engine.AppDescription = AppDescription;
         if (!string.IsNullOrWhiteSpace(Package)) engine.Package = Package;
         if (TransportRequest != null) engine.TransportRequest = TransportRequest;
         engine.DeltaMode = DeltaMode;
         engine.TestMode = TestMode;
         engine.Timeout = Timeout;
         engine.AppSubDir = AppSubFolder;
         engine.IgnoreCertificateErrors = IgnoreCertificateErrors;
         Credentials credentials = null;
         if (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password))
         {
            credentials = new Credentials(System, Mandant, Username);
            credentials.Password = Password;
         }
         else if (!string.IsNullOrWhiteSpace(Username))
         {
            var credentialStore = CredentialStore.Load();
            credentials = credentialStore.GetCredentials(System, Mandant, Username);
            if (credentials == null)
            {
               credentials = new Credentials(System, Mandant, Username);
               credentials.Password = SetCredentialsCommand.GetPasswordFromConsole(string.Format("Enter Password for user {0}: ", Username));
               if (string.IsNullOrEmpty(credentials.Password)) {
                  Log.Instance.Write("User abort.");
                  return 5;
               }
               Console.Write("Save Password? [Y/N] ");
               var answer = Console.ReadLine();
               if (answer == null)
               {
                  Log.Instance.Write("Non interactive console. Aborting.");
                  return 6;
               }
               if (answer.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase))
               {
                  credentialStore.SetCredentials(credentials);
                  credentialStore.Save();
               }

            }
         }
         else //SSO
         {
            credentials = new Credentials(System, Mandant, null);
         }
         engine.Credentials = credentials;
         Log.Instance.Write("System: {0}", engine.Credentials.System);
         Log.Instance.Write("Mandant: {0}", engine.Credentials.Mandant);
         Log.Instance.Write("Username: {0}", engine.Credentials.Username ?? "SSO");
         Log.Instance.Write("Project folder: {0}", engine.ProjectDir);
         Log.Instance.Write("App name: {0}", engine.AppName);
         Log.Instance.Write("Package: {0}", engine.Package);
         Log.Instance.Write("Transport: {0}", engine.TransportRequest);
         Log.Instance.Write("Ignore Masks: {0}", string.Join(", ", engine.IgnoreMasks));

         if (!SkipConfirmation)
         {
            Console.WriteLine("Do you want to upload? [Y/N]");
            var answer = Console.ReadLine();
            if (answer == null)
            {
               Log.Instance.Write("Non interactive console. Use -f to disable this prompt.");
               return 6;
            }
            if (!answer.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase))
            {
               Log.Instance.Write("User abort.");
               return 0;
            }
         }
         try
         {
            engine.Upload();
         }
         catch (UploadFailedException)
         {
            return 2;
         }
         catch (NotAuthorizedException)
         {
            var credentialStore = CredentialStore.Load();
            credentials = credentialStore.GetCredentials(System, Mandant, Username);
            if (credentials != null)
            {
               Log.Instance.Write("Removing saved password.");
               credentialStore.RemoveCredentials(System, Mandant, Username);
               credentialStore.Save();
            }
            return 1;

         }
         catch (Exception e)
         {
            Log.Instance.Write(e.ToString());
            return 3;
         }
         return 0;
      }
   }
}
