using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace StimmingSignalGenerator.Helper
{
   public static class CompressionBase64
   {
      /// <summary>
      /// Compress unicode string then encode to base64 string.
      /// </summary>
      /// <param name="str"></param>
      /// <returns></returns>
      public static string Compress(string str)
      {
         var data = System.Text.Encoding.Unicode.GetBytes(str);
         using (var compressedStream = new MemoryStream())
         using (var zipStream = new GZipStream(compressedStream, CompressionLevel.Optimal))
         {
            zipStream.Write(data, 0, data.Length);
            zipStream.Close();
            return Convert.ToBase64String(compressedStream.ToArray());
         }
      }

      /// <summary>
      /// Decode base64 string then decompress to unicode string.
      /// </summary>
      /// <param name="base64String"></param>
      /// <returns></returns>
      public static string Decompress(string base64String)
      {
         var data = Convert.FromBase64String(base64String);
         using (var compressedStream = new MemoryStream(data))
         using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
         using (var resultStream = new MemoryStream())
         {
            var buffer = new byte[4096];
            int read;

            while ((read = zipStream.Read(buffer, 0, buffer.Length)) > 0)
            {
               resultStream.Write(buffer, 0, read);
            }
            return System.Text.Encoding.Unicode.GetString(resultStream.ToArray());
         }
      }

   }
}
