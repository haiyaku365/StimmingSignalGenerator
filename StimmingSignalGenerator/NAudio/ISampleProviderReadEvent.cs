using NAudio.Wave;
using System;

namespace StimmingSignalGenerator.NAudio
{
   public interface ISampleProviderReadEvent : ISampleProvider
   {
      event EventHandler OnRead;
      event EventHandler OnReaded;
   }
}
