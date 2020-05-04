using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StimmingSignalGenerator.Generators.POCOs
{
   class MultiSignal
   {
      public double Gain { get; set; }
      public List<BasicSignal> BasicSignals { get; set; }

      public Generators.MultiSignal ToObject()
      {
         var obj = new Generators.MultiSignal() { Gain = Gain };
         foreach (var item in BasicSignals)
         {
            obj.AddSignal(item.ToObject());
         }
         return obj;
      }
   }
}
