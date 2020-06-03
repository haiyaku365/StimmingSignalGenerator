using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Input;
using Avalonia.Interactivity;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;

namespace StimmingSignalGenerator.MVVM.UiHelper
{
   public static class ControlEventToObservableExtension
   {
      public static IObservable<EventPattern<PointerPressedEventArgs>> ObservablePointerPressed
         (this IControl control, IScheduler scheduler)
         => Observable.FromEventPattern<PointerPressedEventArgs>(
            h => control.PointerPressed += h,
            h => control.PointerPressed -= h,
            scheduler);
      public static IObservable<EventPattern<PointerPressedEventArgs>> ObservablePointerPressed
         (this IControl control)
         => Observable.FromEventPattern<PointerPressedEventArgs>(
            h => control.PointerPressed += h,
            h => control.PointerPressed -= h);


      public static IObservable<EventPattern<ItemContainerEventArgs>> ObservableMaterialized
         (this IItemContainerGenerator itemContainerGenerator)
         => Observable.FromEventPattern<ItemContainerEventArgs>(
            h => itemContainerGenerator.Materialized += h,
            h => itemContainerGenerator.Materialized -= h);
      public static IObservable<EventPattern<ItemContainerEventArgs>> ObservableDematerialized
         (this IItemContainerGenerator itemContainerGenerator)
         => Observable.FromEventPattern<ItemContainerEventArgs>(
            h => itemContainerGenerator.Dematerialized += h,
            h => itemContainerGenerator.Dematerialized -= h);


      public static IObservable<EventPattern<SpinEventArgs>> ObservableSpinned
         (this NumericUpDown numericUpDown)
          => Observable.FromEventPattern<SpinEventArgs>(
            h => numericUpDown.Spinned += h,
            h => numericUpDown.Spinned -= h);
      public static IObservable<EventPattern<KeyEventArgs>> ObservableKeyDown
         (this IControl control)
         => Observable.FromEventPattern<KeyEventArgs>(
            h => control.KeyDown += h,
            h => control.KeyDown -= h);
      public static IObservable<EventPattern<RoutedEventArgs>> ObservableLostFocus
         (this IControl control)
         => Observable.FromEventPattern<RoutedEventArgs>(
            h => control.LostFocus += h,
            h => control.LostFocus -= h);
   }
}
