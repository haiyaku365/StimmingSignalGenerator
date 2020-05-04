using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.Generators.POCOs
{
   class BasicSignal
   {
      public double Frequency { get; set; }
      public double ZeroCrossingPosition { get; set; }
      public double Gain { get; set; }
      public BasicSignalType Type { get; set; }
      public List<BasicSignal> AMSignals { get; set; }
      public List<BasicSignal> FMSignals { get; set; }

      public Generators.BasicSignal ToObject()
      {
         var obj = new Generators.BasicSignal()
         {
            Frequency = Frequency,
            Gain = Gain,
            Type = Type
         };
         foreach (var item in AMSignals)
         {
            obj.AddAMSignal(item.ToObject());
         }
         foreach (var item in FMSignals)
         {
            obj.AddFMSignal(item.ToObject());
         }
         return obj;
      }
   }
}
