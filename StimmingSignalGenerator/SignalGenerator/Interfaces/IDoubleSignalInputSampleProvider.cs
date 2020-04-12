using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.SignalGenerator.Interfaces
{
   interface IDoubleSignalInputSampleProvider : ISampleProvider
   {
      ISampleProvider InputSampleA { get; set; }
      ISampleProvider InputSampleB { get; set; }
   }
}
