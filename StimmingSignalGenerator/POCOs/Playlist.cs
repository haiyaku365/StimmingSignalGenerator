using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StimmingSignalGenerator.POCOs
{
   public class Playlist
   {
      [JsonIgnore] public string SavePath { get; set; }
      [JsonIgnore] public string Name { get; set; }
      public string Note { get; set; }
      public List<Track> Tracks { get; set; }
   }
}
