using Avalonia.ReactiveUI;
using ReactiveUI;
using System;
using System.Reactive.Linq;

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
      {
         this.WhenAnyValue(x => x.ViewModel)
            .Where(x => x != null)
            .Timeout(TimeSpan.FromMinutes(1)) //cancel in 1 min if not find any
            .Take(1)
            .Subscribe(
               onNext: x => action(x),
               onError: x =>
               {
                  if (x is TimeoutException) return;
                  throw x;
               });
      }
   }
}
