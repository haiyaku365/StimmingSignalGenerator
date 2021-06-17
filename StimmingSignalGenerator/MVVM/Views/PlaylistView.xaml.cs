using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DynamicData;
using ReactiveUI;
using StimmingSignalGenerator.MVVM.UiHelper;
using StimmingSignalGenerator.MVVM.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

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
            TrackList.ItemContainerGenerator.ObservableMaterialized()
            .Subscribe(x => OnItemContainerGeneratorMaterialized(x))
            .DisposeWith(disposables);

            // When item remove from TrackList
            TrackList.ItemContainerGenerator.ObservableDematerialized()
            .Subscribe(x => OnItemContainerGeneratorDematerialized(x))
            .DisposeWith(disposables);

            // When item move and container recycled it need to reset drag data manually
            TrackList.ItemContainerGenerator.ObservableRecycled()
            .Subscribe(x =>
            {
               OnItemContainerGeneratorDematerialized(x);
               OnItemContainerGeneratorMaterialized(x);
            }).DisposeWith(disposables);

            // Setup DragOver, Drop handler
            this.AddDisposableHandler(DragDrop.DragOverEvent, DragOver).DisposeWith(disposables);
            this.AddDisposableHandler(DragDrop.DropEvent, Drop).DisposeWith(disposables);
         });
         InitializeComponent();
      }

      private void OnItemContainerGeneratorMaterialized(EventPattern<ItemContainerEventArgs> e)
      {
         foreach (var container in e.EventArgs.Containers)
         {
            var dragData = new DataObject();
            dragData.Set(DataFormats.Text, $"{container.Index}");
            // Each TrackList item DoDragDrop when pointer pressesd
            var containerDisposables = new CompositeDisposable();
            container.ContainerControl.ObservablePointerPressed()
               .ObserveOn(RxApp.MainThreadScheduler)
               .Subscribe(x => Observable.StartAsync(() =>
               {
                  //System.Diagnostics.Debug.WriteLine($"ObservablePointerPressed {dragData.GetText()}");
                  if (x.EventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                  {
                     return DragDrop.DoDragDrop(x.EventArgs, dragData, DragDropEffects.Move);
                  }
                  else
                  {
                     x.EventArgs.Handled = false;
                     return Task.CompletedTask;
                  }
               })).DisposeWith(containerDisposables);

            //keep disposable to dispose when item remove
            trackControlPointerPressedDisposables.Add(
               (container.ContainerControl, containerDisposables)
            );
         }
      }

      private void OnItemContainerGeneratorDematerialized(EventPattern<ItemContainerEventArgs> e)
      {
         foreach (var container in e.EventArgs.Containers)
         {
            //do cleanup for TrackList item
            var trackControlPointerPressedDisposable =
               trackControlPointerPressedDisposables
                  .FirstOrDefault(x => x.control == container.ContainerControl);
            trackControlPointerPressedDisposable.disposable.Dispose();
            trackControlPointerPressedDisposables.Remove(trackControlPointerPressedDisposable);
         }
      }

      private void DragOver(object sender, DragEventArgs e)
      {
         // Only allow Move as Drop Operations.
         e.DragEffects &= DragDropEffects.Move;

         // Only allow if the dragged data contains text.
         if (!e.Data.Contains(DataFormats.Text))
            e.DragEffects = DragDropEffects.None;
      }
      private void Drop(object sender, DragEventArgs e)
      {
         if (!e.Data.Contains(DataFormats.Text)) return;
         if (!int.TryParse(e.Data.GetText(), out int dragFromIdx)) return;

         var dropToIdx = TrackList.Items.OfType<object>().IndexOf((e.Source as IControl).DataContext);
         //System.Diagnostics.Debug.WriteLine($"Droped {dragFromIdx}->{dropToIdx}");
         ViewModel.MoveTrack(dragFromIdx, dropToIdx);
      }
      private void InitializeComponent()
      {
         AvaloniaXamlLoader.Load(this);
      }
   }
}