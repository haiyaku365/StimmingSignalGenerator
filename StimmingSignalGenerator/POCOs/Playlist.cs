using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace StimmingSignalGenerator.POCOs
{
   public class Playlist
   {
      [JsonIgnore] public string Name { get; set; }
      public List<Track> Tracks { get; set; }
   }
}
