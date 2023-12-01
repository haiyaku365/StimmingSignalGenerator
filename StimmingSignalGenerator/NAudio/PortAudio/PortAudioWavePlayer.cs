using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortAudioSharp;
using System.Runtime.InteropServices;
using System.Threading;
using OpenTK.Audio.OpenAL;
using NAudio.Utils;
using System.IO;
using SkiaSharp;
using System.Buffers;
using Microsoft.VisualStudio.Threading;

namespace StimmingSignalGenerator.NAudio.PortAudio
{
    class PortAudioWavePlayer : IWavePlayer
    {
        public float Volume { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public PlaybackState PlaybackState { get; private set; }

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        public int Latency { get; }

        public WaveFormat OutputWaveFormat => sourceProvider.WaveFormat;
        private int _deviceIndex;
        private DeviceInfo _deviceInfo;

        private IWaveProvider sourceProvider;
        private int bufferSizeByte;
        private CircularBuffer sourceBuffer;
        private byte[] bufferWrite;
        private byte[] bufferRead;
        private byte[] bufferWriteLastestBlock;
        private AsyncAutoResetEvent sourceBufferDequeuedEvent;

        private PortAudioSharp.Stream stream;
        public PortAudioWavePlayer(int audioDeviceIndex, int latency)
        {
            _deviceIndex = audioDeviceIndex;
            _deviceInfo = PortAudioSharp.PortAudio.GetDeviceInfo(audioDeviceIndex);
            Latency = latency;
            sourceBufferDequeuedEvent = new(false);
        }

        public void Init(IWaveProvider waveProvider)
        {
            sourceProvider = waveProvider;
            bufferSizeByte = OutputWaveFormat.ConvertLatencyToByteSize(Latency);
            sourceBuffer = new CircularBuffer(bufferSizeByte);
            bufferWriteLastestBlock = new byte[OutputWaveFormat.BlockAlign];

            var param = new StreamParameters
            {
                device = _deviceIndex,
                channelCount = OutputWaveFormat.Channels,
                sampleFormat = SampleFormat.Float32,
                suggestedLatency = _deviceInfo.defaultLowInputLatency,
                hostApiSpecificStreamInfo = IntPtr.Zero,
            };

            StreamCallbackResult callback(
                IntPtr input, IntPtr output,
                UInt32 frameCount,
                ref StreamCallbackTimeInfo timeInfo,
                StreamCallbackFlags statusFlags,
                IntPtr userData
                )
            {
                int cnt = (int)frameCount * OutputWaveFormat.BlockAlign;
                bufferWrite = BufferHelpers.Ensure(bufferWrite, cnt);
                var byteReadCnt = sourceBuffer.Read(bufferWrite, 0, cnt);
                sourceBufferDequeuedEvent.Set();

                #region prevent wave jump when not enough buffer
                if (byteReadCnt >= OutputWaveFormat.BlockAlign)
                {
                    // Copy latest data to use when not enough buffer.
                    Array.Copy(
                        bufferWrite, byteReadCnt - OutputWaveFormat.BlockAlign,
                        bufferWriteLastestBlock, 0, OutputWaveFormat.BlockAlign);
                }
                while (byteReadCnt < cnt)
                {
                    // When running out of buffer data (Latency too low).
                    // Fill the rest of buffer with latest data
                    // so wave does not jump.
                    bufferWrite[byteReadCnt] = bufferWriteLastestBlock[byteReadCnt % OutputWaveFormat.BlockAlign];
                    byteReadCnt++;
                }
                #endregion

                Marshal.Copy(bufferWrite, 0, output, cnt);
                return StreamCallbackResult.Continue;
            }
            stream = new PortAudioSharp.Stream(
                        inParams: null, outParams: param, sampleRate: waveProvider.WaveFormat.SampleRate,
                        framesPerBuffer: 0,
                        streamFlags: StreamFlags.NoFlag,
                        callback: callback,
                        userData: IntPtr.Zero
                    );
        }

        private readonly CancellationTokenSource fillSourceBufferWorkerCts = new();
        private Task fillSourceBufferWorker;
        private async Task FillSourceBufferTaskAsync()
        {
            fillSourceBufferWorkerCts.TryReset();
            while (!fillSourceBufferWorkerCts.Token.IsCancellationRequested)
            {
                var bufferSpace = sourceBuffer.MaxLength - sourceBuffer.Count;
                if (bufferSpace > 0)
                {
                    FillSourceBuffer(bufferSpace);
                }
                await sourceBufferDequeuedEvent.WaitAsync(fillSourceBufferWorkerCts.Token);
            }
        }

        private void FillSourceBuffer(int bufferSpace)
        {
            bufferRead = BufferHelpers.Ensure(bufferRead, bufferSpace);
            sourceProvider.Read(bufferRead, 0, bufferSpace);
            sourceBuffer.Write(bufferRead, 0, bufferSpace);
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Play()
        {
            if (PlaybackState != PlaybackState.Playing)
            {
                FillSourceBuffer(sourceBuffer.MaxLength);
                fillSourceBufferWorker = Task.Factory.StartNew(
                    function: FillSourceBufferTaskAsync,
                    cancellationToken: CancellationToken.None,
                    creationOptions:
                        TaskCreationOptions.RunContinuationsAsynchronously |
                        TaskCreationOptions.LongRunning,
                    scheduler: TaskScheduler.Default);
                stream.Start();
                PlaybackState = PlaybackState.Playing;
            }
        }

        public void Stop()
        {
            if (PlaybackState != PlaybackState.Stopped)
            {
                stream.Stop();
                fillSourceBufferWorkerCts.Cancel();
                while (!stream.IsStopped && fillSourceBufferWorker.Status != TaskStatus.Running) { Thread.Sleep(30); };
                fillSourceBufferWorker.Dispose();
                PlaybackState = PlaybackState.Stopped;
                PlaybackStopped.Invoke(this, new StoppedEventArgs(null));
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
                    stream.Close();
                    stream.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                Array.Clear(bufferWrite);
                bufferWrite = null;
                Array.Clear(bufferRead);
                bufferRead = null;
                disposedValue = true;
            }
        }

        // // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~PortAudioWavePlayer()
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
