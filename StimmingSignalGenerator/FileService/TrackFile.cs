using Avalonia.Controls;
using Splat;
using StimmingSignalGenerator.POCOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StimmingSignalGenerator.FileService
{
   static class TrackFile
   {
      public static async Task SaveTrackAsync(this Track track)
      {
         saveFileDialog.Directory = TrackPath;
         saveFileDialog.InitialFileName = string.IsNullOrWhiteSpace(track.Name) ? GetNextFileName() : track.Name;
         var savePath = await saveFileDialog.ShowAsync(Window);
         if (savePath == null) return;
         File.Delete(savePath);
         using (FileStream fs = File.OpenWrite(savePath))
         {
            await JsonSerializer.SerializeAsync(fs, track, new JsonSerializerOptions { WriteIndented = true });
         };
      }

      public static async Task<Track> LoadTrackAsync()
      {
         openFileDialog.Directory = TrackPath;
         var loadPath = await openFileDialog.ShowAsync(Window);
         if (loadPath.Length == 0) return null;
         using (FileStream fs = File.OpenRead(loadPath[0]))
         {
            var poco = await JsonSerializer.DeserializeAsync<Track>(fs);
            poco.Name = Path.GetFileNameWithoutExtension(loadPath[0]);
            return poco;
         }
      }

      private static readonly Regex defaultFileRegex = new Regex(@"(?:Track)(\d*)(?:.json)$");
      private static string GetNextFileName()
      {
         int maxNum;
         try
         {
            maxNum =
               Directory.EnumerateFiles(TrackPath).DefaultIfEmpty("0")
                  .Max(x => int.TryParse(defaultFileRegex.Match(x).Groups[1].Value, out int num) ? num : 0);
         }
         catch (DirectoryNotFoundException)
         {
            Directory.CreateDirectory(TrackPath);
            maxNum = 0;
         }
         catch (Exception) { throw; }

         return $"Track{maxNum + 1}.json";
      }

      private static readonly List<FileDialogFilter> fileDialogFilters =
         new List<FileDialogFilter> { {
                  new FileDialogFilter { Name = "json", Extensions = new List<string> { "json" } } }
            };
      private static readonly SaveFileDialog saveFileDialog =
         new SaveFileDialog
         {
            DefaultExtension = ".json",
            Filters = fileDialogFilters
         };
      private static readonly OpenFileDialog openFileDialog =
         new OpenFileDialog
         {
            AllowMultiple = false,
            Filters = fileDialogFilters
         };

      private static Window _window;
      private static Window Window => _window ??= Locator.Current.GetService<Window>();

      private const string TrackLocation = "Track";
      private static readonly string TrackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TrackLocation);
   }
}
