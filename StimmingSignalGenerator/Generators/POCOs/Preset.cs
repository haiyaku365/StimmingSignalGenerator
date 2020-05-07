using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StimmingSignalGenerator.Generators.POCOs
{
   class Preset
   {
      public List<MultiSignal> MultiSignals { get; set; }

      public async Task SaveFileAsync(string fileName = "PresetFile.json")
      {
         File.Delete(GetSavePath(fileName));
         using (FileStream fs = File.OpenWrite(GetSavePath(fileName)))
         {
            await JsonSerializer.SerializeAsync(fs, this);
         };
      }
      public static async Task<Preset> LoadFileAsync(string fileName = "PresetFile.json")
      {
         using (FileStream fs = File.OpenRead(GetSavePath(fileName)))
         {
            return await JsonSerializer.DeserializeAsync<Preset>(fs);
         }
      }

      const string PresetLocation = "Preset";
      static readonly string PresetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PresetLocation);
      static string GetSavePath(string fileName)
      {
         Directory.CreateDirectory(PresetPath);
         return Path.Combine(PresetPath, fileName);
      }
   }
}
