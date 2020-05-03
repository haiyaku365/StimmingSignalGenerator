using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;

namespace StimmingSignalGenerator.SignalGenerator
{
   public static class SampleProviderExt
   {
      public static ISampleProvider Gain(
         this ISampleProvider SourceSampleProvider,
         Func<float, float> gainFuntion)
         => new GainControlSampleProvider(SourceSampleProvider, gainFuntion);

      public static IObservable<EventPattern<SampleEventArgs>> ToObservable(this NotifyingSampleProvider notifyingSampleProvider) 
         => Observable.FromEventPattern<SampleEventArgs>(
            h => notifyingSampleProvider.Sample += h,
            h => notifyingSampleProvider.Sample -= h);
   }
}
