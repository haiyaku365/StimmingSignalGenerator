using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortAudioSharp;
using StimmingSignalGenerator.NAudio.PortAudio;
using System.Reactive.Linq;
using DynamicData;
using System.Reactive.Disposables;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace StimmingSignalGenerator.NAudio.Player
{
    internal class PortAudioAudioPlayer : AudioPlayerBase
    {
        public PortAudioAudioPlayer(IWaveProvider waveProvider) : base(waveProvider)
        {
            PortAudioSharp.PortAudio.Initialize();
            Disposable.Create(PortAudioSharp.PortAudio.Terminate).DisposeWith(Disposables);
            audioDevices = new ObservableCollection<string>(GetAudioDevices());

            SelectedAudioDevice = GetDefaultOutputDeviceName();
        }
        public override ReadOnlyObservableCollection<string> AudioDevices => new(audioDevices);
        private readonly ObservableCollection<string> audioDevices;
        protected override IWavePlayer CreateWavePlayer()
            => new PortAudioWavePlayer(DeviceInfoExt.GetIndex(SelectedAudioDevice), Latency);

        private static string GetDefaultOutputDeviceName()
        {
            int devIdx = PortAudioSharp.PortAudio.DefaultOutputDevice;
            return devIdx < 0 ?
                    string.Empty :
                    PortAudioSharp.PortAudio.GetDeviceInfo(devIdx).ToString(devIdx);
        }

        private string[] GetAudioDevices()
        {
            List<string> audioDevice = new();

            for (int i = 0; i < PortAudioSharp.PortAudio.DeviceCount; i++)
            {
                if (IsDeviceSupport(i, WaveProvider.WaveFormat))
                {
                    var deviceInfo = PortAudioSharp.PortAudio.GetDeviceInfo(i);
                    audioDevice.Add(deviceInfo.ToString(i));
                }
            }
            return audioDevice.ToArray();
        }
        private static bool IsDeviceSupport(int deviceIndex, WaveFormat waveFormat)
        {
            var outParams = new StreamParameters
            {
                device = deviceIndex,
                channelCount = waveFormat.Channels,
                sampleFormat = SampleFormat.Float32,
                suggestedLatency = 0,// this field is ignore
                hostApiSpecificStreamInfo = IntPtr.Zero
            };
            nint outParamsPointer = Marshal.AllocHGlobal(Marshal.SizeOf(outParams));
            Marshal.StructureToPtr(outParams, outParamsPointer, fDeleteOld: true);

            bool isSupport = Native.Pa_IsFormatSupported(IntPtr.Zero, outParamsPointer, waveFormat.SampleRate) == 0;
            Marshal.FreeHGlobal(outParamsPointer);
            if (isSupport)
            {
                // test steam
                try
                {
                    static StreamCallbackResult callback(
                        IntPtr input, IntPtr output,
                        UInt32 frameCount,
                        ref StreamCallbackTimeInfo timeInfo,
                        StreamCallbackFlags statusFlags,
                        IntPtr userData
                        )
                    {
                        return StreamCallbackResult.Complete;
                    }
                    using (var stream = new PortAudioSharp.Stream(
                                inParams: null, outParams: outParams, sampleRate: waveFormat.SampleRate,
                                framesPerBuffer: 0,
                                streamFlags: StreamFlags.NoFlag,
                                callback: callback,
                                userData: IntPtr.Zero
                            ))
                    {
                        stream.Start();
                        stream.Stop();
                        stream.Close();
                    };
                }
                catch (PortAudioException)
                {
                    return false;
                }

                return true;
            }
            return false;
        }
    }

    public static class DeviceInfoExt
    {
        public static string ToString(this DeviceInfo deviceInfo, int index)
            => $"[{index}]{deviceInfo.name} SampleRate:{deviceInfo.defaultSampleRate} MaxChannels:{deviceInfo.maxOutputChannels} OutputLatency(L/H):{deviceInfo.defaultLowOutputLatency}/{deviceInfo.defaultHighOutputLatency}";
        public static int GetIndex(string str)
        {
            var idxStr = DeviceInfoIdxRegex.Match(str).Groups[1].Value;
            return int.TryParse(idxStr, out int idx) ? idx : PortAudioSharp.PortAudio.NoDevice;
        }
        static readonly Regex DeviceInfoIdxRegex = new(@"^\[(\d.*)\].*");
    }

}
