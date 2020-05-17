using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels.Interface
{
   public interface ISourceCacheViewModel
   {
      public int Id { get; internal set; }
      public string Name { get; set; }
   }
}
