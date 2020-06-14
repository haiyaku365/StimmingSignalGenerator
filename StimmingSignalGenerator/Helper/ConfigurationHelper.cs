using System;
using System.Configuration;
using System.Reactive.Disposables;

namespace StimmingSignalGenerator.Helper
{
   public static class ConfigurationHelper
   {
      public static bool GetConfigOrDefault(string key, bool defaultValue)
         => bool.TryParse(ConfigurationManager.AppSettings[key], out bool confValue) ?
            confValue : defaultValue;
      public static double GetConfigOrDefault(string key, double defaultValue)
         => double.TryParse(ConfigurationManager.AppSettings[key], out double confValue) ?
            confValue : defaultValue;
      public static int GetConfigOrDefault(string key, int defaultValue)
         => int.TryParse(ConfigurationManager.AppSettings[key], out int confValue) ?
            confValue : defaultValue;
      public static TEnum GetConfigOrDefault<TEnum>(string key, TEnum defaultValue) where TEnum : struct
         => Enum.TryParse(ConfigurationManager.AppSettings[key], out TEnum confValue) ?
            confValue :
            defaultValue;
      public static string GetConfigOrDefault(string key, string defaultValue)
         => ConfigurationManager.AppSettings[key] ?? defaultValue;

      public static IDisposable AddUpdateAppSettingsOnDispose(string key, Func<string> getValue)
         => Disposable.Create(() => AddUpdateAppSettings(key, getValue()));

      public static void AddUpdateAppSettings(string key, string value)
      {
         if (ConfigurationManager.AppSettings[key] == null)
         {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;
            settings.Add(key, value);
            configFile.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
         }
         else
         {
            ConfigurationManager.AppSettings[key] = value;
         }
      }

      public static IDisposable SaveAppSettingsOnDispose()
         => Disposable.Create(() =>
         {
            try
            {
               var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
               var settings = configFile.AppSettings.Settings;
               foreach (var key in ConfigurationManager.AppSettings.AllKeys)
               {
                  if (settings[key] == null)
                  {
                     settings.Add(key, ConfigurationManager.AppSettings[key]);
                  }
                  else
                  {
                     settings[key].Value = ConfigurationManager.AppSettings[key];
                  }
               }
               configFile.Save(ConfigurationSaveMode.Modified);
               ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
               Console.WriteLine("Error writing app settings");
            }
         });
   }
}