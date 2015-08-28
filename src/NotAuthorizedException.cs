// Copyright Bernhard Klefer (BTC AG, Escherweg 5, 26121 Oldenburg, Germany)
// This file is part of the BTC.UI5Uploader
// BTC.UI5Uploader ist distributed under the BSD 2-clause license (full text see file license).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTC.UI5Uploader
{
   /// <summary>
   /// Exception for a failed authorization.
   /// </summary>
   [Serializable]
   public class NotAuthorizedException : Exception
   {
      public NotAuthorizedException() { }
      public NotAuthorizedException(string message) : base(message) { }
      public NotAuthorizedException(string message, Exception inner) : base(message, inner) { }
      protected NotAuthorizedException(
       System.Runtime.Serialization.SerializationInfo info,
       System.Runtime.Serialization.StreamingContext context)
         : base(info, context) { }
   }
}
