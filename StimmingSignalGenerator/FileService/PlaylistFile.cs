using Avalonia.Controls;
using Splat;
using StimmingSignalGenerator.MVVM.ViewModels;
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
   static class PlaylistFile
   {
      public static async Task SaveAsync(this Playlist playlist)
      {
         CreatePlaylistDir();
         saveFileDialog.Directory = PlaylistPath;
         saveFileDialog.InitialFileName = string.IsNullOrWhiteSpace(playlist.Name) ? GetNextFileName() : playlist.Name;
         var savePath = await saveFileDialog.ShowAsync(Window);
         if (savePath == null) return;
         File.Delete(savePath);
         using (FileStream fs = File.OpenWrite(savePath))
         {
            await JsonSerializer.SerializeAsync(fs, playlist, new JsonSerializerOptions { WriteIndented = true });
         };
      }

      public static async Task<Playlist> LoadAsync()
      {
         CreatePlaylistDir();
         openFileDialog.Directory = PlaylistPath;
         var loadPath = await openFileDialog.ShowAsync(Window);
         if (loadPath.Length == 0) return null;
         return await LoadAsync(loadPath[0]);
      }

      public static async Task<Playlist> LoadFirstFileAsync()
      {
         CreatePlaylistDir();
         var loadPath = 
            Directory
               .GetFiles(PlaylistPath, "*.json")
               .OrderBy(x=>x)
               .FirstOrDefault();
         return await LoadAsync(loadPath);
      }

      public static async Task<Playlist> LoadAsync(string path)
      {
         if (!File.Exists(path)) return null;
         using (FileStream fs = File.OpenRead(path))
         {
            var poco = await JsonSerializer.DeserializeAsync<Playlist>(fs);
            poco.Name = Path.GetFileNameWithoutExtension(path);
            return poco;
         }
      }
      
      private static readonly Regex defaultFileRegex = 
         new Regex(@$"(?:{Constants.File.PlaylistNamePrefix})(\d*)(?:.json)$");
      private static string GetNextFileName()
      {
         int maxNum =
               Directory.EnumerateFiles(PlaylistPath).DefaultIfEmpty("0")
                  .Max(x => int.TryParse(defaultFileRegex.Match(x).Groups[1].Value, out int num) ? num : 0);
         return $"{Constants.File.PlaylistNamePrefix}{maxNum + 1}.json";
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

      private static void CreatePlaylistDir() => Directory.CreateDirectory(PlaylistPath);

      
      private static string PlaylistPath => 
         Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.File.PlaylistDirectoryName);
   }
}
