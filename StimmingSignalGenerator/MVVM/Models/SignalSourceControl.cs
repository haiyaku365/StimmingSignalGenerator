using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.MVVM.Models
{
   class SignalSourceControl
   {
      //TODO Make config serializable
      public SignalGeneratorType SignalType { get; set; }
      public float Frequency { get; set; }
      public float Volume { get; set; }
   }
}
