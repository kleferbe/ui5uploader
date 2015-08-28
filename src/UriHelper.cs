// Copyright Bernhard Klefer (BTC AG, Escherweg 5, 26121 Oldenburg, Germany)
// This file is part of the BTC.UI5Uploader
// BTC.UI5Uploader ist distributed under the BSD 2-clause license (full text see file license).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace BTC.UI5Uploader
{
   /// <summary>
   /// Helper class with extension methods and helper methods to build URIs
   /// </summary>
   static class UriHelper
   {
      /// <summary>
      /// Adds the name-value-pair to the query.
      /// </summary>
      /// <param name="uri">The Uri to add the query parameter to</param>
      /// <param name="name">Name of the query parameter</param>
      /// <param name="value">Value of the query parameter</param>
      /// <returns>The Uri including the new query parameter</returns>
      public static Uri AddQuery(this Uri uri, string name, string value)
      {
         var ub = new UriBuilder(uri);

         // decodes urlencoded pairs from uri.Query to HttpValueCollection
         var httpValueCollection = HttpUtility.ParseQueryString(uri.Query);
         httpValueCollection.Add(name, value);

         // this code block is taken from httpValueCollection.ToString() method
         // and modified so it encodes strings with HttpUtility.UrlEncode
         if (httpValueCollection.Count == 0)
            ub.Query = String.Empty;
         else
         {
            var sb = new StringBuilder();

            for (int i = 0; i < httpValueCollection.Count; i++)
            {
               string text = httpValueCollection.GetKey(i);
               {
                  text = HttpUtility.UrlEncode(text);

                  string val = (text != null) ? (text + "=") : string.Empty;
                  string[] vals = httpValueCollection.GetValues(i);

                  if (sb.Length > 0)
                     sb.Append('&');

                  if (vals == null || vals.Length == 0)
                     sb.Append(val);
                  else
                  {
                     if (vals.Length == 1)
                     {
                        sb.Append(val);
                        sb.Append(HttpUtility.UrlEncode(vals[0]));
                     }
                     else
                     {
                        for (int j = 0; j < vals.Length; j++)
                        {
                           if (j > 0)
                              sb.Append('&');

                           sb.Append(val);
                           sb.Append(HttpUtility.UrlEncode(vals[j]));
                        }
                     }
                  }
               }
            }

            ub.Query = sb.ToString();
         }

         return ub.Uri;
      }

      /// <summary>
      /// Create a valid URI to the ui5 upload service if the user has given only the host (and port).
      /// The scheme is set to http if missing and the path is set to /sap/bc/ui5upload.
      /// </summary>
      /// <param name="system">the uri to the sap upload service as given by the user.</param>
      /// <returns></returns>
      public static Uri CreateUri(string system)
      {
         if (!System.Text.RegularExpressions.Regex.IsMatch(system, @"^\w+://")) system = "http://" + system;
         var builder = new UriBuilder(system);
         if (builder.Path == "/") builder.Path = "/sap/bc/ui5upload";
         return builder.Uri;
      }
   }
}
