// Copyright Bernhard Klefer (BTC AG, Escherweg 5, 26121 Oldenburg, Germany)
// This file is part of the BTC.UI5Uploader
// BTC.UI5Uploader ist distributed under the BSD 2-clause license (full text see file license).
using ManyConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BTC.UI5Uploader
{
   static class Program
   {
      [STAThread]
      static int Main(string[] args)
      {
         Log.Instance.LineAdded += (s, e) => Console.WriteLine(e.Line);
         Console.WriteLine("UI5Uploader - Upload Open/SAPUI5 applications to a SAP ABAP repository");
         Console.WriteLine("Copyright (c) 2015 Bernhard Klefer, BTC AG. Read license file for details.");

         var commands = ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program));
         return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
      }
   }
}
