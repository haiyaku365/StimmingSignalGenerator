using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.Generators.Interfaces
{
   interface IDoubleSignalInputSampleProvider : ISampleProvider
   {
      ISampleProvider SourceA { get; set; }
      ISampleProvider SourceB { get; set; }
   }
}
