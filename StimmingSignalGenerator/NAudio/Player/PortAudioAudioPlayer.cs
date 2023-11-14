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

namespace StimmingSignalGenerator.NAudio.Player
{
    internal class PortAudioAudioPlayer : AudioPlayerBase
    {
        public PortAudioAudioPlayer(IWaveProvider waveProvider) : base(waveProvider)
        {
            PortAudioSharp.PortAudio.Initialize();
            Disposable.Create(() => { PortAudioSharp.PortAudio.Terminate(); }).DisposeWith(Disposables);
            audioDevices = new ObservableCollection<string>(GetAudioDevices());

            SelectedAudioDevice = GetDefaultOutputDeviceName();
        }
        public override ReadOnlyObservableCollection<string> AudioDevices => new(audioDevices);
        private readonly ObservableCollection<string> audioDevices;
        protected override IWavePlayer CreateWavePlayer()
            => new PortAudioWavePlayer(SelectedAudioDevice.GetDeviceInfoIndex(), Latency);

        private static string GetDefaultOutputDeviceName()
        {
            int devIdx = PortAudioSharp.PortAudio.DefaultOutputDevice;
            return devIdx < 0 ?
                    string.Empty :
                    PortAudioSharp.PortAudio.GetDeviceInfo(devIdx).ToString(devIdx);
        }

        private static string[] GetAudioDevices()
        {
            List<string> audioDevice = new();

            for (int i = 0; i < PortAudioSharp.PortAudio.DeviceCount; i++)
            {
                var deviceInfo = PortAudioSharp.PortAudio.GetDeviceInfo(i);
                if (deviceInfo.maxOutputChannels >= 2)
                {
                    audioDevice.Add(deviceInfo.ToString(i));
                }
            }
            return audioDevice.ToArray();
        }

    }
    public static class DeviceInfoExt
    {
        public static string ToString(this DeviceInfo deviceInfo, int index)
            => $"[{index}]{deviceInfo.name} SampleRate:{deviceInfo.defaultSampleRate} MaxChannels:{deviceInfo.maxOutputChannels} OutputLatency(L/H):{deviceInfo.defaultLowOutputLatency}/{deviceInfo.defaultHighOutputLatency}";
        public static int GetDeviceInfoIndex(this string str)
        {
            var idxStr = Regex.Match(str, @"^\[(\d)\].*").Groups[1].Value;
            return int.TryParse(idxStr, out int idx) ? idx : PortAudioSharp.PortAudio.NoDevice;
        }

    }

}
