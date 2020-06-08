using DynamicData;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace StimmingSignalGenerator.NAudio
{
   public class WasapiAudioPlayer : AudioPlayerBase
   {
      public override ReadOnlyObservableCollection<string> AudioDevices => audioDevices;

      private MMNotificationClient mMNotificationClient;
      private MMDeviceEnumerator MMDeviceEnumerator { get; }
      private Subject<(MMNotificationClient.ChangedType type, string deviceId)> deviceChangedSubject;
      private SourceList<MMDevice> MMAudioDevicesSourceList { get; }
      private MMDevice selectedMMAudioDevice;
      private readonly ReadOnlyObservableCollection<string> audioDevices;
      public WasapiAudioPlayer(IWaveProvider waveProvider) : base(waveProvider)
      {
         deviceChangedSubject = new Subject<(MMNotificationClient.ChangedType, string)>().DisposeWith(Disposables);
         MMDeviceEnumerator = new MMDeviceEnumerator().DisposeWith(Disposables);

         MMAudioDevicesSourceList = new SourceList<MMDevice>().DisposeWith(Disposables);
         MMAudioDevicesSourceList.Connect()
            .AutoRefreshOnObservable(x =>
               deviceChangedSubject.Where(x =>
                  x.type == MMNotificationClient.ChangedType.DeviceStateChanged ||
                  x.type == MMNotificationClient.ChangedType.DeviceAdded ||
                  x.type == MMNotificationClient.ChangedType.DeviceRemoved &&
                  MMAudioDevicesSourceList.Items.Select(x => x.ID).Contains(x.deviceId)
               ))
            .Transform(x => MMDeviceToString(x), true)
            .ObserveOn(RxApp.MainThreadScheduler) // Make sure this is only right before the Bind()
            .Bind(out audioDevices)
            .Subscribe()
            .DisposeWith(Disposables);

         this.WhenAnyValue(x => x.SelectedAudioDevice)
            .Subscribe(_ =>
            {
               selectedMMAudioDevice = string.IsNullOrEmpty(SelectedAudioDevice) ?
                  GetDefaultMMDevice() :
                  StringToMMDevice(SelectedAudioDevice);
            })
            .DisposeWith(Disposables);

         MMAudioDevicesSourceList.AddRange(GetMMDevices());

         SelectedAudioDevice = MMDeviceToString(GetDefaultMMDevice());

         // MMDevice name change notify to refresh collection
         mMNotificationClient = new MMNotificationClient(deviceChangedSubject);
         MMDeviceEnumerator.RegisterEndpointNotificationCallback(mMNotificationClient);
      }
      protected override void Dispose(bool disposing)
      {
         if (disposing)
         {
            // dispose managed state (managed objects)
            MMDeviceEnumerator.UnregisterEndpointNotificationCallback(mMNotificationClient);
         }
         base.Dispose(disposing);
      }

      private class MMNotificationClient : IMMNotificationClient
      {
         public enum ChangedType
         {
            DefaultDeviceChanged,
            DeviceAdded,
            DeviceRemoved,
            DeviceStateChanged,
            PropertyValueChanged
         }
         private readonly Subject<(ChangedType, string)> subject;
         public MMNotificationClient(Subject<(ChangedType, string)> subject)
         {
            this.subject = subject;
         }
         public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
            => subject.OnNext((ChangedType.DefaultDeviceChanged, defaultDeviceId));
         public void OnDeviceAdded(string pwstrDeviceId)
            => subject.OnNext((ChangedType.DeviceAdded, pwstrDeviceId));
         public void OnDeviceRemoved(string deviceId)
            => subject.OnNext((ChangedType.DeviceRemoved, deviceId));
         public void OnDeviceStateChanged(string deviceId, DeviceState newState)
            => subject.OnNext((ChangedType.DeviceStateChanged, deviceId));
         public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
            => subject.OnNext((ChangedType.PropertyValueChanged, pwstrDeviceId));
      }

      protected override IWavePlayer CreateWavePlayer()
         => new WasapiOut(selectedMMAudioDevice, AudioClientShareMode.Exclusive, true, Latency);

      private string MMDeviceToString(MMDevice mMDevice)
         => $"{mMDevice.FriendlyName}[{mMDevice.State}]";

      private MMDevice StringToMMDevice(string device)
         => MMAudioDevicesSourceList.Items.FirstOrDefault(d => device.Contains(d.FriendlyName));

      private MMDevice GetDefaultMMDevice()
      {
         // If not use ID to select AudioDevice from AudioDevices 
         // it will be different object and fail combo box initialization
         var defaultDevId = MMDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
         return MMAudioDevicesSourceList.Items.FirstOrDefault(d => d.ID == defaultDevId);
      }

      private MMDevice[] GetMMDevices()
         => MMDeviceEnumerator
            .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active | DeviceState.Unplugged)
            .ToArray();

      public override void Play()
      {
         if (selectedMMAudioDevice?.State != DeviceState.Active)
         {
            Stop();
            return;
         }
         base.Play();
      }

   }
}
