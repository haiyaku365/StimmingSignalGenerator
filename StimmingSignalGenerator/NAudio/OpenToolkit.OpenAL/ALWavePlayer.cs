using NAudio.Wave;
using OpenToolkit.Audio.OpenAL;
using System;
using System.Threading;

namespace StimmingSignalGenerator.NAudio.OpenToolkit.OpenAL
{
   class ALWavePlayer : IWavePlayer
   {
      public float Volume { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

      public PlaybackState PlaybackState { get; private set; }

      /// <summary>
      /// Gets or sets the Device
      /// Should be set before a call to Init
      /// </summary>
      public string DeviceName { get; set; }
      /// <summary>
      /// Indicates playback has stopped automatically
      /// </summary>
      public event EventHandler<StoppedEventArgs> PlaybackStopped;

      /// <summary>
      /// Gets or sets the desired latency in milliseconds
      /// Should be set before a call to Init
      /// </summary>
      public int DesiredLatency { get; set; }

      public int bufferSizeByte;

      /// <summary>
      /// Gets or sets the number of buffers used
      /// Should be set before a call to Init
      /// </summary>
      public int NumberOfBuffers { get; set; }

      private ALDevice device;
      private ALContext context;
      private IWaveProvider sourceProvider;

      private readonly SynchronizationContext syncContext;
      private AutoResetEvent callbackEvent;

      private int alSource;
      private int[] alBuffers;
      public ALWavePlayer(string deviceName)
      {
         DeviceName = deviceName;

         syncContext = SynchronizationContext.Current;
      }
      public void Init(IWaveProvider waveProvider)
      {
         if (waveProvider.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
            throw new ArgumentException("Input wave provider must be PCM", "sourceProvider");
         if (waveProvider.WaveFormat.BitsPerSample != 16)
            throw new ArgumentException("Input wave provider must be 16 bit", "sourceProvider");

         callbackEvent = new AutoResetEvent(false);

         bufferSizeByte = waveProvider.WaveFormat.ConvertLatencyToByteSize(DesiredLatency);

         CheckAndRaiseStopOnALError();

         device = ALC.OpenDevice(DeviceName);
         CheckAndRaiseStopOnALError();

         context = ALC.CreateContext(device, (int[])null);
         CheckAndRaiseStopOnALError();

         ALC.MakeContextCurrent(context);
         CheckAndRaiseStopOnALError();

         AL.GenSource(out alSource);
         CheckAndRaiseStopOnALError();

         AL.Source(alSource, ALSourcef.Gain, 1f);
         CheckAndRaiseStopOnALError();

         sourceProvider = waveProvider;

         alBuffers = new int[NumberOfBuffers];
         for (int i = 0; i < NumberOfBuffers; i++)
         {
            AL.GenBuffer(out alBuffers[i]);
            CheckAndRaiseStopOnALError();
         }
         ReadAndQueueBuffers(alBuffers);
      }

      private void ReadAndQueueBuffers(int[] _alBuffers)
      {
         for (int i = 0; i < _alBuffers.Length; i++)
         {
            //read source
            byte[] buffer = new byte[bufferSizeByte];
            sourceProvider.Read(buffer, 0, buffer.Length);

            CheckAndRaiseStopOnALError();

            //fill and queue buffer
            AL.BufferData(_alBuffers[i], ALFormat.Stereo16, buffer, buffer.Length, sourceProvider.WaveFormat.SampleRate);
            CheckAndRaiseStopOnALError();
            AL.SourceQueueBuffer(alSource, _alBuffers[i]);
            CheckAndRaiseStopOnALError();
         }
      }

      public void Pause()
      {
         throw new NotImplementedException();
      }

      public void Play()
      {
         if (alBuffers == null)
         {
            throw new InvalidOperationException("Must call Init first");
         }
         if (PlaybackState != PlaybackState.Playing)
         {
            if (PlaybackState == PlaybackState.Stopped)
            {
               callbackEvent.Set(); // give the thread a kick
               ThreadPool.QueueUserWorkItem(state => PlaybackThread(), null);
               PlaybackState = PlaybackState.Playing;
            }
            else
            {
               PlaybackState = PlaybackState.Playing;
               callbackEvent.Set(); // give the thread a kick
            }
         }
      }

      public void Stop()
      {
         if (PlaybackState != PlaybackState.Stopped)
         {
            PlaybackState = PlaybackState.Stopped;

            callbackEvent.Set();

            CheckAndRaiseStopOnALError();

            AL.SourceStop(alSource);
            CheckAndRaiseStopOnALError();

            //detach buffer to be able to delete
            AL.Source(alSource, ALSourcei.Buffer, 0);
            CheckAndRaiseStopOnALError();

            AL.DeleteBuffers(alBuffers);
            CheckAndRaiseStopOnALError();

            alBuffers = null;

            AL.DeleteSource(alSource);
            CheckAndRaiseStopOnALError();

         }
      }

      private void PlaybackThread()
      {
         Exception exception = null;
         try
         {
            DoPlayback();
         }
         catch (Exception e)
         {
            exception = e;
         }
         finally
         {
            PlaybackState = PlaybackState.Stopped;
            // we're exiting our background thread
            RaisePlaybackStoppedEvent(exception);
         }
      }

      private void DoPlayback()
      {
         int processed, state;

         while (PlaybackState == PlaybackState.Playing)
         {
            CheckAndRaiseStopOnALError();

            AL.GetSource(alSource, ALGetSourcei.BuffersProcessed, out processed);
            CheckAndRaiseStopOnALError();
            AL.GetSource(alSource, ALGetSourcei.SourceState, out state);
            CheckAndRaiseStopOnALError();

            if (processed > 0) //there are processed buffers
            {
               //unqueue
               int[] unqueueBuffers = AL.SourceUnqueueBuffers(alSource, processed);
               CheckAndRaiseStopOnALError();
               if (PlaybackState == PlaybackState.Playing)
               {
                  //refill it back in
                  ReadAndQueueBuffers(unqueueBuffers);
               }
            }

            if ((ALSourceState)state != ALSourceState.Playing)
            {
               AL.SourcePlay(alSource);
               CheckAndRaiseStopOnALError();
            }

            callbackEvent.WaitOne(1);
         }
      }

      /// <summary>
      /// Check for AL error
      /// </summary>
      /// <param name="errStr">Error string if error, empty string if not.</param>
      /// <returns>true if error</returns>
      public static bool CheckALError(out string errStr)
      {
         ALError error = AL.GetError();
         bool isErr = error != ALError.NoError;
         errStr = isErr ? AL.GetErrorString(error) : string.Empty;
         return isErr;
      }

      public void CheckAndRaiseStopOnALError()
      {
         if (CheckALError(out var errStr))
         {
            RaisePlaybackStoppedEvent(new Exception($"ALError:{errStr}"));
         }
      }

      private void RaisePlaybackStoppedEvent(Exception e)
      {
         var handler = PlaybackStopped;
         if (handler != null)
         {
            if (syncContext == null)
            {
               handler(this, new StoppedEventArgs(e));
            }
            else
            {
               syncContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
            }
         }
      }

      private bool disposedValue;
      protected virtual void Dispose(bool disposing)
      {
         if (!disposedValue)
         {
            if (disposing)
            {
               // dispose managed state (managed objects)
               Stop();
               ALC.MakeContextCurrent(ALContext.Null);
               ALC.DestroyContext(context);
               ALC.CloseDevice(device);
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            disposedValue = true;
         }
      }

      // // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
      // ~AudioPlayer()
      // {
      //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      //     Dispose(disposing: false);
      // }

      public void Dispose()
      {
         // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
         Dispose(disposing: true);
         GC.SuppressFinalize(this);
      }
   }
}
