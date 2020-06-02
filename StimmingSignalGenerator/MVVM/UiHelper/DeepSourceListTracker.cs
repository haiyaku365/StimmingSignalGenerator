using DynamicData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace StimmingSignalGenerator.MVVM.UiHelper
{
   public interface IDeepSourceList<T>
   {
      public IObservable<T> ObservableItemAdded { get; }
      public IObservable<T> ObservableItemRemoved { get; }
   }
   public class DeepSourceListTracker<T> : IDisposable
      where T : IDeepSourceList<T>
   {
      public IObservable<T> ObservableItemAdded => subjectOfItemAdded.AsObservable();
      public IObservable<T> ObservableItemRemoved => subjectOfItemRemoved.AsObservable();

      private readonly ReplaySubject<T> subjectOfItemAdded;
      private readonly ReplaySubject<T> subjectOfItemRemoved;
      private readonly List<(T item, CompositeDisposable disposable)> innerDisposables;

      public DeepSourceListTracker(params IObservableList<T>[] sourceLists)
      {
         subjectOfItemAdded = new ReplaySubject<T>().DisposeWith(Disposables);
         subjectOfItemRemoved = new ReplaySubject<T>().DisposeWith(Disposables);
         innerDisposables = new List<(T, CompositeDisposable)>();
         foreach (var sourceList in sourceLists)
         {
            sourceList.Connect()
               .OnItemAdded(item =>
               {
                  subjectOfItemAdded.OnNext(item);
                  // sub to inner item
                  var disposables = new CompositeDisposable(2).DisposeWith(Disposables);
                  item.ObservableItemAdded
                     .Subscribe(x => subjectOfItemAdded.OnNext(x))
                     .DisposeWith(disposables);
                  item.ObservableItemRemoved
                     .Subscribe(x => subjectOfItemRemoved.OnNext(x))
                     .DisposeWith(disposables);
                  innerDisposables.Add((item, disposables));
               })
               .OnItemRemoved(item =>
               {
                  subjectOfItemRemoved.OnNext(item);
                  //cleanup inner sub
                  var innerDisposable = innerDisposables.First(x => x.item.Equals(item));
                  innerDisposable.disposable.Dispose();
                  innerDisposables.Remove(innerDisposable);
               })
               .Subscribe()
               .DisposeWith(Disposables);
         }
      }

      private CompositeDisposable Disposables { get; } = new CompositeDisposable();
      private bool disposedValue;
      protected virtual void Dispose(bool disposing)
      {
         if (!disposedValue)
         {
            if (disposing)
            {
               // dispose managed state (managed objects)
               Disposables?.Dispose();
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            disposedValue = true;
         }
      }

      // // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
      // ~BasicSignalViewModel()
      // {
      //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      //     Dispose(disposing: false);
      // }

      public void Dispose()
      {
         // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
         Dispose(disposing: true);
         GC.SuppressFinalize(this);
      }
   }
}
