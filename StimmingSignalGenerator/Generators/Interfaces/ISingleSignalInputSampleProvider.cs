using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.Generators.Interfaces
{
   interface ISingleSignalInputSampleProvider : ISampleProvider
   {
      ISampleProvider InputSample { get; set; }
   }
}
