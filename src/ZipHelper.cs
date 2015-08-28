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
   /// Helper-Class for recursive zipping of a folder.
   /// </summary>
   class ZipHelper : IDisposable
   {
      ICSharpCode.SharpZipLib.Zip.ZipOutputStream zipStream;
      /// <summary>
      /// Creates a new Instance that will write to the specified stream
      /// </summary>
      /// <param name="stream">Stream to write the zipped data to</param>
      public ZipHelper(Stream stream)
      {
         zipStream = new ICSharpCode.SharpZipLib.Zip.ZipOutputStream(stream);
         zipStream.SetLevel(9);
         zipStream.UseZip64 = ICSharpCode.SharpZipLib.Zip.UseZip64.Off;

      }
      /// <summary>
      /// Zips the specified <paramref name="baseFolder"/>.
      /// </summary>
      /// <param name="baseFolder">The folder to zip</param>
      /// <param name="filesToZip">
      /// the files and directories within the <paramref name="baseFolder"/> to include in the zip. 
      /// These names are only matched in the <paramref name="baseFolder"/> but not recursive.
      /// You can use the default globbing (* and ?).
      /// </param>
      /// <param name="filesToIgnore">Names of the files or directories to exclude from zip. Full case sensitive match. No globbing. Directories have a trailing "/".</param>
      public void Zip(string baseFolder, string[] filesToZip, string[] filesToIgnore)
      {
         var baseDir = new DirectoryInfo(baseFolder);
         foreach (var fileMask in filesToZip)
         {
            foreach (var file in baseDir.GetFiles(fileMask, SearchOption.TopDirectoryOnly))
            {
               if (IgnoreFile(baseDir.FullName, file.FullName, filesToIgnore)) continue;
               var zipEntry = new ICSharpCode.SharpZipLib.Zip.ZipEntry(file.Name);
               zipStream.PutNextEntry(zipEntry);
               using (var fileStream = file.OpenRead()) fileStream.CopyTo(zipStream);
            }
            foreach (var subDir in baseDir.GetDirectories(fileMask, SearchOption.TopDirectoryOnly))
            {
               if (IgnoreFile(baseDir.FullName, subDir.FullName, filesToIgnore)) continue;
               ZipDirectory(zipStream, baseDir, subDir, filesToIgnore);
            }
         }
      }
      /// <summary>
      /// Zips the specified <paramref name="baseFolder"/>.
      /// </summary>
      /// <param name="fileName">Relative filename in zip.</param>
      /// <param name="contentStream">The content of the file</param>
      public void Zip(string fileName, Stream contentStream)
      {
         var nameTransform = new ICSharpCode.SharpZipLib.Zip.ZipNameTransform();
         var relativeName = nameTransform.TransformFile(fileName);
         var zipEntry = new ICSharpCode.SharpZipLib.Zip.ZipEntry(relativeName);
         zipStream.PutNextEntry(zipEntry);
         contentStream.CopyTo(zipStream);
      }
      /// <summary>
      /// Zips the specified <paramref name="baseFolder"/>.
      /// </summary>
      /// <param name="fileName">Relative filename in zip.</param>
      /// <param name="content">The content of the file</param>
      public void Zip(string fileName, byte[] content)
      {
         if (content==null) return;
         var nameTransform = new ICSharpCode.SharpZipLib.Zip.ZipNameTransform();
         var relativeName = nameTransform.TransformFile(fileName);
         var zipEntry = new ICSharpCode.SharpZipLib.Zip.ZipEntry(relativeName);
         zipStream.PutNextEntry(zipEntry);
         zipStream.Write(content, 0, content.Length);
      }
      /// <summary>
      /// Zips the specified <paramref name="baseFolder"/> to  the <paramref name="stream"/>.
      /// </summary>
      /// <param name="stream">Stream to write the compressed data to.</param>
      /// <param name="baseFolder">The folder to zip</param>
      /// <param name="filesToZip">
      /// the files and directories within the <paramref name="baseFolder"/> to include in the zip. 
      /// These names are only matched in the <paramref name="baseFolder"/> but not recursive.
      /// You can use the default globbing (* and ?).
      /// </param>
      /// <param name="filesToIgnore">Names of the files or directories to exclude from zip. Full case sensitive match. No globbing. Directories have a trailing "/".</param>
      public static void Zip(Stream stream, string baseFolder, string[] filesToZip, string[] filesToIgnore)
      {
         using (var s = new ICSharpCode.SharpZipLib.Zip.ZipOutputStream(stream))
         {
            s.SetLevel(9);
            s.UseZip64 = ICSharpCode.SharpZipLib.Zip.UseZip64.Off;
            var baseDir = new DirectoryInfo(baseFolder);
            foreach (var fileMask in filesToZip)
            {
               foreach (var file in baseDir.GetFiles(fileMask, SearchOption.TopDirectoryOnly))
               {
                  if (IgnoreFile(baseDir.FullName, file.FullName, filesToIgnore)) continue;
                  var zipEntry = new ICSharpCode.SharpZipLib.Zip.ZipEntry(file.Name);
                  s.PutNextEntry(zipEntry);
                  using (var fileStream = file.OpenRead()) fileStream.CopyTo(s);
               }
               foreach (var subDir in baseDir.GetDirectories(fileMask, SearchOption.TopDirectoryOnly))
               {
                  if (IgnoreFile(baseDir.FullName, subDir.FullName, filesToIgnore)) continue;
                  ZipDirectory(s, baseDir, subDir, filesToIgnore);
               }

            }
            s.Finish();
         }
      }
      /// <summary>
      /// Recursive Zipping of a directory.
      /// </summary>
      /// <param name="zipStream"></param>
      /// <param name="baseDir"></param>
      /// <param name="dir"></param>
      /// <param name="filesToIgnore"></param>
      private static void ZipDirectory(ICSharpCode.SharpZipLib.Zip.ZipOutputStream zipStream, DirectoryInfo baseDir, DirectoryInfo dir, string[] filesToIgnore)
      {
         var nameTransform = new ICSharpCode.SharpZipLib.Zip.ZipNameTransform(baseDir.FullName);
         foreach (var file in dir.GetFiles())
         {
            if (IgnoreFile(baseDir.FullName, file.FullName, filesToIgnore)) continue;
            var relativeName = nameTransform.TransformFile(file.FullName);
            var zipEntry = new ICSharpCode.SharpZipLib.Zip.ZipEntry(relativeName);
            zipStream.PutNextEntry(zipEntry);
            using (var fileStream = file.OpenRead()) fileStream.CopyTo(zipStream);
         }
         foreach (var subDir in dir.GetDirectories())
         {
            if (IgnoreFile(baseDir.FullName, subDir.FullName, filesToIgnore)) continue;
            ZipDirectory(zipStream, baseDir, subDir, filesToIgnore);
         }
      }
      /// <summary>
      /// Test if file is matched by an ignore mask.
      /// </summary>
      /// <param name="baseFolder">basefolder for relative filenames in ignore masks</param>
      /// <param name="fullFilename">full filename</param>
      /// <param name="ignoreMasks">list of ignore masks. Either a literal match or a Regex if the string starts with '^' and ends with '$'. Both variants are case sensitive.</param>
      /// <returns></returns>
      private static bool IgnoreFile(string baseFolder, string fullFilename, IEnumerable<string> ignoreMasks)
      {
         var relativeFilename = fullFilename;
         if (!baseFolder.EndsWith("\\")) baseFolder += "\\";
         if (fullFilename.StartsWith(baseFolder, StringComparison.OrdinalIgnoreCase))
         {
            relativeFilename = fullFilename.Substring(baseFolder.Length);
         }
         relativeFilename = relativeFilename.Replace('\\', '/');
         foreach (var mask in ignoreMasks)
         {
            if (mask.StartsWith("^") && mask.EndsWith("$")) //Regex match
            {
               try
               {
                  if (System.Text.RegularExpressions.Regex.IsMatch(relativeFilename, mask)) return true;
               }
               catch
               {
                  //Invalid Regex.
               }
            }
            else //Literal match
            {
               if (relativeFilename.Equals(mask, StringComparison.Ordinal)) return true;
            }
         }
         return false;
      }

      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing)
      {
         if (disposing)
         {
            if (zipStream != null)
            {
               zipStream.Finish();
               zipStream.Dispose();
            }
         }
      }
   }
}
