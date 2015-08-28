// Copyright Bernhard Klefer (BTC AG, Escherweg 5, 26121 Oldenburg, Germany)
// This file is part of the BTC.UI5Uploader
// BTC.UI5Uploader ist distributed under the BSD 2-clause license (full text see file license).
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace BTC.UI5Uploader
{
   /// <summary>
   /// Credentials for a SAP System
   /// </summary>
   [XmlType("UI5UploadCredentials")]
   public class Credentials
   {
      /// <summary>
      /// Empty constructor used by XML deserialization
      /// </summary>
      public Credentials() { }
      /// <summary>
      /// Creates a new Credential-Container instance 
      /// </summary>
      /// <param name="system">Url of the SAP systems upload service</param>
      /// <param name="mandant">SAP mandant of the <paramref name="username"/></param>
      /// <param name="username">Username</param>
      public Credentials(string system, int mandant, string username)
      {
         this.System = system;
         this.Mandant = mandant;
         this.Username = username;
      }
      /// <summary>
      /// Url of the SAP systems upload service
      /// </summary>
      [XmlAttribute]
      public string System { get; set; }
      /// <summary>
      /// SAP Mandant of the <see cref="Username"/>
      /// </summary>
      [XmlAttribute]
      public int Mandant { get; set; }
      /// <summary>
      /// Username
      /// </summary>
      [XmlAttribute]
      public string Username { get; set; }
      /// <summary>
      /// Password
      /// </summary>
      [XmlIgnore]
      public string Password { get; set; }
      /// <summary>
      /// The encrypted Password for XML serialization
      /// </summary>
      [XmlText]
      public string EncryptedPassword
      {
         get
         {
            return Crypto.EncryptStringAES(Password, "cc54b3a5473b74817480425ab2191537483cb9af7ddd3726914912bdaabdc7e7");
         }
         set
         {
            Password = Crypto.DecryptStringAES(value, "cc54b3a5473b74817480425ab2191537483cb9af7ddd3726914912bdaabdc7e7");
         }
      }
   }
}
