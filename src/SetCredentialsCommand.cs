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
   /// Command line command to set and save credentials for a SAP system and a user.
   /// </summary>
   class SetCredentialsCommand : ConsoleCommand
   {
      public SetCredentialsCommand()
      {
         Mandant = 100;
         SkipsCommandSummaryBeforeRunning();
         IsCommand("password", oneLineDescription: "Set and save the credentials for a SAP system");
         HasRequiredOption("s|system=", "SAP System URL", o => System = UriHelper.CreateUri(o).ToString());
         HasOption("m|mandant=", "Mandant (default: 100)", o => Mandant = o == null ? 100 : int.Parse(o));
         HasRequiredOption("u|username=", "Username", o => Username = o);
         HasOption("p|password=", "Password. If not supplied you will be prompted for it.", o => Password = o);
         HasOption("c|clear", "Clear Password", o => ClearPassword = true);

      }
      public string System { get; set; }
      public int Mandant { get; set; }
      public string Username { get; set; }
      public string Password { get; set; }
      public bool ClearPassword { get; set; }

      public override int Run(string[] remainingArguments)
      {
         var store = CredentialStore.Load();

         if (ClearPassword)
         {
            var removed = store.RemoveCredentials(System, Mandant, Username);
            store.Save();
            if (removed) Log.Instance.Write("Credentials have been removed.");
            return 0;
         }

         var credentials = new Credentials(System, Mandant, Username);
         if (!string.IsNullOrWhiteSpace(Password))
         {
            credentials.Password = Password;
            store.SetCredentials(credentials);
            store.Save();
            return 0;
         }
         var password1 = GetPasswordFromConsole(string.Format("Enter Password for user {0}: ", Username));
         if (string.IsNullOrEmpty(password1))
         {
            Log.Instance.Write("User abort.");
            return 5;
         }
         var password2 = GetPasswordFromConsole("Retype Password: ");
         if (password1 != password2)
         {
            Log.Instance.Write("Password mismatch");
            return 2;
         }
         credentials.Password = password1;
         store.SetCredentials(credentials);
         store.Save();
         return 0;


      }

      public static string GetPasswordFromConsole(string prompt)
      {
         string password = "";
         try
         {
            var x = Console.KeyAvailable;
            Console.Write(prompt);
            ConsoleKeyInfo info = Console.ReadKey(true);
            while (info.Key != ConsoleKey.Enter)
            {
               if (info.Key != ConsoleKey.Backspace)
               {
                  password += info.KeyChar;
                  info = Console.ReadKey(true);
               }
               else if (info.Key == ConsoleKey.Backspace)
               {
                  if (!string.IsNullOrEmpty(password))
                  {
                     password = password.Substring
                     (0, password.Length - 1);
                  }
                  info = Console.ReadKey(true);
               }
            }
            Console.WriteLine();
         }
         catch (InvalidOperationException)
         {
            //StdIn is redirected. Running from Eclipse?
            Console.WriteLine("WARNING: Console input has been redirected.");
            Console.WriteLine("         The Password will be displayed on screen!");
            Console.WriteLine("         You can use the 'UI5Uploader password' command to save your password.");
            Console.WriteLine("         Press Enter (empty password) to abort.");
            Console.Write(prompt);
            password = Console.ReadLine();
         }
         return password;
      }



   }
}
