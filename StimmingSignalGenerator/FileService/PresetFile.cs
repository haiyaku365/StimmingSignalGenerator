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
   static class PresetFile
   {
      public static async Task SavePresetAsync(this Preset preset)
      {
         saveFileDialog.Directory = PresetPath;
         saveFileDialog.InitialFileName = string.IsNullOrWhiteSpace(preset.Name) ? GetNextFileName() : preset.Name;
         var savePath = await saveFileDialog.ShowAsync(Window);
         if (savePath == null) return;
         File.Delete(savePath);
         using (FileStream fs = File.OpenWrite(savePath))
         {
            await JsonSerializer.SerializeAsync(fs, preset, new JsonSerializerOptions { WriteIndented = true });
         };
      }

      public static async Task<Preset> LoadPresetAsync()
      {
         openFileDialog.Directory = PresetPath;
         var loadPath = await openFileDialog.ShowAsync(Window);
         if (loadPath.Length == 0) return null;
         using (FileStream fs = File.OpenRead(loadPath[0]))
         {
            var poco = await JsonSerializer.DeserializeAsync<Preset>(fs);
            poco.Name = Path.GetFileNameWithoutExtension(loadPath[0]);
            return poco;
         }
      }

      private static readonly Regex defaultFileRegex = new Regex(@"(?:Preset)(\d*)(?:.json)$");
      private static string GetNextFileName()
      {
         int maxNum;
         try
         {
            maxNum =
               Directory.EnumerateFiles(PresetPath).DefaultIfEmpty("0")
                  .Max(x => int.TryParse(defaultFileRegex.Match(x).Groups[1].Value, out int num) ? num : 0);
         }
         catch (DirectoryNotFoundException)
         {
            Directory.CreateDirectory(PresetPath);
            maxNum = 0;
         }
         catch (Exception) { throw; }

         return $"Preset{maxNum + 1}.json";
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

      private const string PresetLocation = "Preset";
      private static readonly string PresetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PresetLocation);
   }
}
