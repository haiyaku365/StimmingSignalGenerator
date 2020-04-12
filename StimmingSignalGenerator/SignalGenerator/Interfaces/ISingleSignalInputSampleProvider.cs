using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.SignalGenerator.Interfaces
{
   interface ISingleSignalInputSampleProvider : ISampleProvider
   {
      ISampleProvider InputSample { get; set; }
   }
}
