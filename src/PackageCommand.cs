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
   /// Commanline command to create a UI5 application package. 
   /// The generated Package can be deployed manually to a ABAP repository with the Report /UI5/UI5_REPOSITORY_LOAD_HTTP
   /// </summary>
   class PackageCommand : ConsoleCommand
   {
      public PackageCommand()
      {
         DeltaMode = true;
         AppSubFolder = "webapp";
         IsCommand("package", oneLineDescription: "Create UI5 Application Package for manual Upload via /UI5/UI5_REPOSITORY_LOAD");
         SkipsCommandSummaryBeforeRunning();
         HasRequiredOption("src|ProjectFolder=", "Root folder of the UI5 Project to upload", o => ProjectFolder = o);
         HasOption("app|appfolder=", "Path to the folder containing the application files. Relative to -src. Default: webapp", o => AppSubFolder = o);
         HasOption("AppName=", "Target name of the UI5-Application. Extracted from .Ui5RepositoryUploadParameters if option not given.", o => AppName = o);
         HasOption("AppDescription=", "Description of the UI5-Application. Needed only when the Application is going to be created. Extracted from .Ui5RepositoryUploadParameters if option not given.", o => AppDescription = o);
         HasOption("Package=", "Target Package. Extracted from .Ui5RepositoryUploadParameters if option not given.", o => Package = o);
         HasOption("Transport=", "Transport Request. Extracted from .Ui5RepositoryUploadParameters if option not given.", o => TransportRequest = o);
         HasOption("disableDeltaMode", "Disable delta mode which adds only changed items to the Transport.", o => DeltaMode = false);
         HasAdditionalArguments(1,"<Target file name>");
      }
      public string ProjectFolder { get; set; }
      public string AppSubFolder { get; set; }
      public string AppName { get; set; }
      public string AppDescription { get; set; }
      public string Package { get; set; }
      public string TransportRequest { get; set; }
      public bool DeltaMode { get; set; }
      public int Timeout { get; set; }

      public override int Run(string[] remainingArguments)
      {
         var engine = new Engine(ProjectFolder);
         if (!string.IsNullOrWhiteSpace(AppName)) engine.AppName = AppName;
         if (!string.IsNullOrWhiteSpace(AppDescription)) engine.AppDescription = AppDescription;
         if (!string.IsNullOrWhiteSpace(Package)) engine.Package = Package;
         if (TransportRequest != null) engine.TransportRequest = TransportRequest;
         engine.DeltaMode = DeltaMode;
         engine.Timeout = Timeout;
         engine.AppSubDir = AppSubFolder;
         var targetFilename = remainingArguments[0];
         Log.Instance.Write("Project folder: {0}", engine.ProjectDir);
         Log.Instance.Write("App name: {0}", engine.AppName);
         Log.Instance.Write("Package: {0}", engine.Package);
         Log.Instance.Write("Transport: {0}", engine.TransportRequest);
         Log.Instance.Write("Ignore Masks: {0}", string.Join(", ", engine.IgnoreMasks));

         try
         {
            engine.Bundle(targetFilename);
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
