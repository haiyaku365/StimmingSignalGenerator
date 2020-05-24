using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DynamicData;
using ReactiveUI;
using StimmingSignalGenerator.MVVM.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace StimmingSignalGenerator.MVVM.Views
{
   public class PlaylistView : ReactiveUserControl<PlaylistViewModel>
   {
      ListBox TrackList => this.FindControl<ListBox>("TrackList");
      readonly List<(IControl control, IDisposable disposable)>
         trackControlPointerPressedDisposables
         = new List<(IControl, IDisposable)>();
      public PlaylistView()
      {
         this.WhenActivated(disposables =>
         {
            // When new item add to TrackList
            Observable.FromEventPattern<ItemContainerEventArgs>(
               h => TrackList.ItemContainerGenerator.Materialized += h,
               h => TrackList.ItemContainerGenerator.Materialized -= h)
            .Subscribe(x =>
            {
               foreach (var container in x.EventArgs.Containers)
               {
                  var dragData = new DataObject();
                  dragData.Set(DataFormats.Text, $"{container.Index}");
                  // Each TrackList item DoDragDrop when pointer pressesd
                  var pointerPressedDisposable =
                     Observable.FromEventPattern<PointerPressedEventArgs>(
                        h => container.ContainerControl.PointerPressed += h,
                        h => container.ContainerControl.PointerPressed -= h,
                        RxApp.MainThreadScheduler)
                     .Subscribe(x => Observable.StartAsync(() =>
                        DragDrop.DoDragDrop(x.EventArgs, dragData, DragDropEffects.Move)));
                  //keep disposable to dispose when item remove
                  trackControlPointerPressedDisposables.Add(
                     (container.ContainerControl, pointerPressedDisposable)
                  );
               }
            })
            .DisposeWith(disposables);

            //when item remove from TrackList
            Observable.FromEventPattern<ItemContainerEventArgs>(
               h => TrackList.ItemContainerGenerator.Dematerialized += h,
               h => TrackList.ItemContainerGenerator.Dematerialized -= h)
            .Subscribe(x =>
            {
               foreach (var container in x.EventArgs.Containers)
               {
                  //do cleanup for TrackList item
                  var trackControlPointerPressedDisposable =
                     trackControlPointerPressedDisposables
                        .FirstOrDefault(x => x.control == container.ContainerControl);
                  trackControlPointerPressedDisposable.disposable.Dispose();
                  trackControlPointerPressedDisposables.Remove(trackControlPointerPressedDisposable);
               }
            })
            .DisposeWith(disposables);

            // Setup DragOver, Drop handler
            this.AddHandler(DragDrop.DragOverEvent, DragOver).DisposeWith(disposables);
            this.AddHandler(DragDrop.DropEvent, Drop).DisposeWith(disposables);
         });
         InitializeComponent();
      }

      void DragOver(object sender, DragEventArgs e)
      {
         // Only allow Move as Drop Operations.
         e.DragEffects &= DragDropEffects.Move;

         // Only allow if the dragged data contains text.
         if (!e.Data.Contains(DataFormats.Text))
            e.DragEffects = DragDropEffects.None;
      }
      void Drop(object sender, DragEventArgs e)
      {
         if (!e.Data.Contains(DataFormats.Text)) return;
         if (!int.TryParse(e.Data.GetText(), out int dragFromIdx)) return;
         var dropToIdx = TrackList.Items.OfType<object>().IndexOf((e.Source as IControl).DataContext);
         ViewModel.MoveTrack(dragFromIdx, dropToIdx);
      }

      private void InitializeComponent()
      {
         AvaloniaXamlLoader.Load(this);
      }
   }
}