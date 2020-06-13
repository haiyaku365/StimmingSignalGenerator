using OpenToolkit.Audio.OpenAL.Extensions.Creative.EnumerateAll;
using System.Collections.Generic;
using System.Linq;


namespace StimmingSignalGenerator.NAudio.OpenToolkit.OpenAL
{
   public static class ALContextHelper
   {
      //public static string GetDefaultDeviceName() => ALC.GetString(ALDevice.Null, AlcGetString.DefaultDeviceSpecifier);
      public static string GetDefaultDeviceName() => GetAllDevicesName().FirstOrDefault();
      public static IEnumerable<string> GetAllDevicesName() => EnumerateAll.GetStringList(GetEnumerateAllContextStringList.AllDevicesSpecifier);
   }
}
