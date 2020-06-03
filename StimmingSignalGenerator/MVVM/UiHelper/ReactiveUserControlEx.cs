using Avalonia.ReactiveUI;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading;

namespace StimmingSignalGenerator.MVVM.UiHelper
{
   public class ReactiveUserControlEx<TViewModel> : ReactiveUserControl<TViewModel> where TViewModel : class
   {
      /// <summary>
      /// In design mode view model can be null and break design view.
      /// Use this method to avoid that.
      /// </summary>
      /// <param name="action"></param>
      protected void SafeViewModel(Action<TViewModel> action)
         => this.WhenAnyValue(x => x.ViewModel)
            .Where(x => x != null)
            .Take(1)
            .Subscribe(x => action(x), token: new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token);
   }
}
