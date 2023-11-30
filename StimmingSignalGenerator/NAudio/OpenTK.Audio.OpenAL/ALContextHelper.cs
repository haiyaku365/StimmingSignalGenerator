using System.Collections.Generic;
using System.Linq;
using OpenTK.Audio.OpenAL;

namespace StimmingSignalGenerator.NAudio.OpenTK.Audio.OpenAL
{
   public static class ALContextHelper
   {
      //public static string GetDefaultDeviceName() => ALC.GetString(ALDevice.Null, AlcGetString.DefaultDeviceSpecifier);
      public static string GetDefaultDeviceName() => GetAllDevicesName().FirstOrDefault();
      public static IEnumerable<string> GetAllDevicesName() => ALC.EnumerateAll.GetStringList(GetEnumerateAllContextStringList.AllDevicesSpecifier);
   }
}
