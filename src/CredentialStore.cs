// Copyright Bernhard Klefer (BTC AG, Escherweg 5, 26121 Oldenburg, Germany)
// This file is part of the BTC.UI5Uploader
// BTC.UI5Uploader ist distributed under the BSD 2-clause license (full text see file license).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace BTC.UI5Uploader
{
   /// <summary>
   /// Stores a collection of SAP system credentials in a XML file.
   /// </summary>
   [XmlType("CredentialStore")]
   public class CredentialStore
   {
      /// <summary>
      /// Creates a new and empty instance. Use <see cref="Load(string)"/> to load stored credentials into the instance.
      /// </summary>
      public CredentialStore()
      {
         XMLCredentials = new List<Credentials>();
      }

      /// <summary>
      /// The list of credentials for XML Serialization
      /// </summary>
      [XmlElement("Credentials")]
      public List<Credentials> XMLCredentials { get; set; }
                                 
      /// <summary>
      /// Searches the store for specific credentials
      /// </summary>
      /// <param name="system">Url of the SAP systems upload service</param>
      /// <param name="mandant">SAP mandant of the <paramref name="username"/></param>
      /// <param name="username">Username</param>
      /// <returns>Credentials of the specified user of the specified system or <c>null</c>, if no credentials could be found in this store.</returns>
      public Credentials GetCredentials(string system, int mandant, string username)
      {
         return XMLCredentials.FirstOrDefault(c => c.System.Equals(system, StringComparison.OrdinalIgnoreCase) && c.Mandant == mandant && c.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
      }
      /// <summary>
      /// Removes the specified credentials from this store. Please remember to call <see cref="Save()"/> to persist the changes.
      /// </summary>
      /// <param name="system">Url of the SAP systems upload service</param>
      /// <param name="mandant">SAP mandant of the <paramref name="username"/></param>
      /// <param name="username">Username</param>
      /// <returns><c>true</c>, if the credentials were found and have been removed.</returns>
      public bool RemoveCredentials(string system, int mandant, string username)
      {
         var old = GetCredentials(system, mandant, username);
         if (old != null) XMLCredentials.Remove(old);
         return old != null;
      }
      /// <summary>
      /// Adds the given credentials to the  store or updates the credentials if an entry exists for the username, mandant and system.
      /// </summary>
      /// <param name="newCredentials">The credentials to add</param>
      public void SetCredentials(Credentials newCredentials)
      {
         RemoveCredentials(newCredentials.System, newCredentials.Mandant, newCredentials.Username);
         XMLCredentials.Add(newCredentials);
      }

      static readonly XmlSerializer serializer = new XmlSerializer(typeof(CredentialStore));
      /// <summary>
      /// Loads the credential store from the Credentials.xml file located in the folder specified in the CredentialFiles application setting.
      /// </summary>
      /// <returns>Loaded store or empty store if the file does not exist.</returns>
      public static CredentialStore Load()
      {
         var credentialPath = Environment.ExpandEnvironmentVariables(UI5Uploader.Properties.Settings.Default.CredentialFiles);
         Directory.CreateDirectory(credentialPath);
         var credentialFile = Path.Combine(credentialPath, "Credentials.xml");
         var result = new CredentialStore();
         if (File.Exists(credentialFile))
         {
            result.Load(credentialFile);
         }
         else
         {
            Log.Instance.Write("New Credential Store created.");
         }
         return result;
      }
      /// <summary>
      /// Loads the credential store from the specified file.
      /// </summary>
      /// <param name="fileName">File to load the contents of this credential store from.</param>
      public void Load(string fileName)
      {
         using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
         {
            var store = (CredentialStore)serializer.Deserialize(stream);
            this.XMLCredentials = store.XMLCredentials;
         }
      }
      /// <summary>
      /// Saves the credential store to the specified file.
      /// </summary>
      /// <param name="fileName">File to save the contents of this credential store to.</param>
      public void Save(string fileName)
      {
         using (var stream = new MemoryStream())
         {
            serializer.Serialize(stream, this);
            File.WriteAllBytes(fileName, stream.ToArray());
         }
      }
      /// <summary>
      /// Saves the credential store to the Credentials.xml file located in the folder specified in the CredentialFiles application setting.
      /// </summary>
      public void Save()
      {
         var credentialPath = Environment.ExpandEnvironmentVariables(UI5Uploader.Properties.Settings.Default.CredentialFiles);
         Directory.CreateDirectory(credentialPath);
         var credentialFile = Path.Combine(credentialPath, "Credentials.xml");
         Save(credentialFile);
      }
   }
}
