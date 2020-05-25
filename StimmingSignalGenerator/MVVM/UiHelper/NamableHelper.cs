﻿using DynamicData;
using StimmingSignalGenerator.MVVM.ViewModels.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StimmingSignalGenerator.MVVM.UiHelper
{
   internal static class NamableHelper
   {
      internal static T SetName<T>(this T vm,
         string namePrefix, SourceList<T> sourceList)
         where T : INamable
      {
         vm.Name = GetNextName(namePrefix, sourceList);
         return vm;
      }
      private static string GetNextName<T>(string prefix, SourceList<T> sourceList)
         where T : INamable
      {
         int maxNum = 0;
         Regex nameRegex = new Regex($@"(?:{prefix})(\d*)$");
         if (sourceList.Items.Count() > 0)
         {
            maxNum = sourceList.Items
                     .Max(x => int.TryParse(nameRegex.Match(x.Name).Groups[1].Value, out int num) ? num : 0);
         }
         return $"{prefix}{maxNum + 1}";
      }
   }
}