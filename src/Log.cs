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
   /// A simple Log. Singleton.
   /// </summary>
   class Log
   {
      StringBuilder sb = new StringBuilder();
      private Log() { }
      /// <summary>
      /// The singleton instance
      /// </summary>
      public static readonly Log Instance = new Log();
      /// <summary>
      /// Returns the text of the log.
      /// </summary>
      /// <returns>The text of the log.</returns>
      public string GetLog() { return sb.ToString(); }
      /// <summary>
      /// Write a new line to the log.
      /// </summary>
      /// <param name="s">The Text to write to the log.</param>
      public void Write(string s)
      {
         s = string.Format("[{1:T}] {0}", s, DateTime.Now);
         sb.AppendLine(s);
         OnLineAdded(s);
      }
      /// <summary>
      /// Write a new line to the log.
      /// </summary>
      /// <param name="format">The formatstring of the line</param>
      /// <param name="args">The arguments for the formatstring.</param>
      public void Write(string format, params object[] args)
      {
         Write(string.Format(format, args));
      }
      /// <summary>
      /// EventArgs for a Log event.
      /// </summary>
      public class LogEventArgs: EventArgs{      
         /// <summary>
         /// Creates a new instance
         /// </summary>
         /// <param name="line">The new line of text in the log.</param>
         public LogEventArgs(string line){
            this.Line=line;
         }
         /// <summary>
         /// The new line of text in the log.
         /// </summary>
         public string Line { get; private set; }
      }
      /// <summary>
      /// Event called when a new line of text is added to the log.
      /// </summary>
      public event EventHandler<LogEventArgs> LineAdded;
      /// <summary>
      /// Call the <see cref="LineAdded"/> event.
      /// </summary>
      /// <param name="line">The new line of text in the log.</param>
      private void OnLineAdded(string line)
      {
         if (LineAdded != null) LineAdded(this, new LogEventArgs(line));
      }
      
   }
}
