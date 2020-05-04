using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace StimmingSignalGenerator.Generators.POCOs
{
   class Preset
   {
      public List<MultiSignal> MultiSignals { get; set; }
      public static Preset FromJson(string jsonString)
         => JsonSerializer.Deserialize<Preset>(jsonString);
      public string ToJson()
         => JsonSerializer.Serialize(this);
   }
}
