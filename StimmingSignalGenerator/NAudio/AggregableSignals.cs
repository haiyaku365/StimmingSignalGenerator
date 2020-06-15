using NAudio.Utils;
using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace StimmingSignalGenerator.NAudio
{
   public class AggregableSignals : ISampleProvider
   {
      private readonly List<BasicSignal> signals;
      private readonly float seed;
      private readonly Func<float, float, BasicSignal, float> aggregateFunction;
      private float[] aggregateBuffer;
      public WaveFormat WaveFormat => Constants.Wave.DefaultMonoWaveFormat;

      /// <summary>
      /// Signal that aggregate from multiple signals
      /// </summary>
      /// <param name="seed">Initial signal value</param>
      /// <param name="aggregateFunction">Func(float currentValue,float NextValue,BasicSignal currentSignal) return float</param>
      public AggregableSignals(float seed, Func<float, float, BasicSignal, float> aggregateFunction)
      {
         signals = new List<BasicSignal>();
         this.seed = seed;
         this.aggregateFunction = aggregateFunction;
      }
      public void Add(BasicSignal signal)
      {
         lock (signals)
         {
            signals.Add(signal);
         }
      }
      public void Remove(BasicSignal signal)
      {
         lock (signals)
         {
            signals.Remove(signal);
         }
      }

      public int Read(float[] buffer, int offset, int count)
      {
         int read = 0;
         Array.Fill(buffer, seed, offset, count);
         // read signals
         lock (signals)
         {
            foreach (var signal in signals)
            {
               aggregateBuffer = BufferHelpers.Ensure(aggregateBuffer, count);
               read = signal.Read(aggregateBuffer, offset, count);
               for (int i = offset; i < count; i++)
               {
                  buffer[i] = aggregateFunction(buffer[i], aggregateBuffer[i], signal);
               }
            }
         }
         return read;
      }
   }
}
