using Avalonia.Threading;
using NAudio.Wave;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;


namespace StimmingSignalGenerator.NAudio.OxyPlot
{
   public class PlotSampleProvider : ISampleProvider
   {
      public ISampleProvider InputSample { get; }
      public PlotModel PlotModel { get; }
      public int PointLimit { get; }
      public bool IsEnable { get; set; }
      public bool IsHighDefinition
      {
         get => isHighDefinition;
         set
         {
            if (isHighDefinition != value)
            {
               isHighDefinition = value;
               foreach (var line in lineSeries)
               {
                  SetLineHD(line, isHighDefinition);
               }
            }
         }
      }

      public WaveFormat WaveFormat => InputSample.WaveFormat;
      public event EventHandler<OnInvalidatePlotPostedEventArgs> OnInvalidatePlotPosted;

      public int MinPlotUpdateIntervalMilliseconds
      {
         get => minPlotUpdateIntervalMilliseconds;
         set
         {
            minPlotUpdateIntervalMilliseconds = value;
            if (minPlotUpdateIntervalMilliseconds < absoluteMinPlotUpdateIntervalMilliseconds)
               minPlotUpdateIntervalMilliseconds = absoluteMinPlotUpdateIntervalMilliseconds;
         }
      }

      private bool isHighDefinition;
      private readonly AliasedLineSeries[] lineSeries;
      private readonly int lineCount;
      private static readonly Random rand = new Random();
      private int minPlotUpdateIntervalMilliseconds = absoluteMinPlotUpdateIntervalMilliseconds;
      public const int absoluteMinPlotUpdateIntervalMilliseconds = 33;
      public PlotSampleProvider(ISampleProvider inputSample)
      {
         InputSample = inputSample;
         lineCount = inputSample.WaveFormat.Channels;
         lineSeries = new AliasedLineSeries[lineCount];
         lineSeries[0] = new AliasedLineSeries()
         {
            Color = OxyColor.FromArgb(180, 0, 0, 0)
         };
         if (lineCount > 1)
         {
            lineSeries[1] = new AliasedLineSeries()
            {
               Color = OxyColor.FromArgb(180, 255, 0, 0)
            };
            for (int i = 2; i < lineCount; i++)
            {
               var (r, g, b) = Helper.ColorHelper.HsvToRgb(rand.Next(0, 360), 1, 1);
               lineSeries[i] = new AliasedLineSeries()
               {
                  Color = OxyColor.FromArgb(180, r, g, b)
               };
            }
         }

         PlotModel = new PlotModel();
         PointLimit = WaveFormat.SampleRate / 8;
         PlotModel.Axes.Add(
           new LinearAxis
           {
              Position = AxisPosition.Left,
              Minimum = -1.2,
              Maximum = 1.2,
              AbsoluteMinimum = -1.2,
              AbsoluteMaximum = 1.2,
              IsZoomEnabled = false,
              TextColor = OxyColor.FromArgb(0, 0, 0, 0),
              ExtraGridlines = new[] { -1d, 1d },
              ExtraGridlineColor = OxyColor.FromAColor(0xa0, OxyColors.Red)
           });
         PlotModel.Axes.Add(
           new LinearAxis
           {
              Position = AxisPosition.Bottom,
              Minimum = 0,
              Maximum = PointLimit,
              AbsoluteMinimum = 0,
              AbsoluteMaximum = PointLimit,
              TextColor = OxyColor.FromArgb(0, 0, 0, 0)
           });

         foreach (var line in lineSeries)
         {
            SetLineHD(line, IsHighDefinition);
            PlotModel.Series.Add(line);
         }
      }

      int xIdx;

      public int Read(float[] buffer, int offset, int count)
      {
         var read = InputSample.Read(buffer, offset, count);
         var countPerLine = count / lineCount;
         if (!IsEnable) return read;

         for (int c = 0; c < lineCount; c++)
         {
            if (lineSeries[c].Points.Count > PointLimit)
            {
               //shift out old data
               var oldPoints = lineSeries[c].Points.Skip(countPerLine).ToArray();
               lineSeries[c].Points.Clear();
               for (int i = 0; i < oldPoints.Length; i++)
               {
                  lineSeries[c].Points.Add(new DataPoint(i, oldPoints[i].Y));
               }
               xIdx = lineSeries[c].Points.Count;
            }
         }

         //insert new data
         for (int i = 0; i < countPerLine; i++)
         {
            for (int c = 0; c < lineCount; c++)
            {
               lineSeries[c].Points.Add(new DataPoint(xIdx + i, buffer[i * lineCount + c]));
            }
         }
         xIdx += read;

         // Only update when time pass to prevent over update and cause ui unresponsive
         if (stopwatch.ElapsedMilliseconds > MinPlotUpdateIntervalMilliseconds)
         {
            Dispatcher.UIThread.Post(() => PlotModel.InvalidatePlot(false));
            OnInvalidatePlotPosted?.Invoke(this, new OnInvalidatePlotPostedEventArgs(stopwatch.ElapsedMilliseconds));
            stopwatch.Restart();
         }

         return read;
      }

      private readonly Stopwatch stopwatch = Stopwatch.StartNew();
      public class OnInvalidatePlotPostedEventArgs : EventArgs
      {
         public OnInvalidatePlotPostedEventArgs(long elapsedMilliseconds)
         {
            ElapsedMilliseconds = elapsedMilliseconds;
         }

         public long ElapsedMilliseconds { get; }

      }
      private void SetLineHD(AliasedLineSeries lineSeries, bool isHD)
      {
         if (isHD)
         {
            lineSeries.Decimator = null;
            lineSeries.MinimumSegmentLength = 2;
            lineSeries.Aliased = false;
         }
         else
         {
            lineSeries.Decimator = Decimator.Decimate;
            lineSeries.MinimumSegmentLength = 3;
            lineSeries.Aliased = true;
         }
      }

      // Performance friendly LineSeries
      //https://github.com/oxyplot/oxyplot/issues/1286
      public class AliasedLineSeries : LineSeries
      {

         List<ScreenPoint> outputBuffer = null;

         public bool Aliased { get; set; } = true;

         protected override void RenderLine(IRenderContext rc, OxyRect clippingRect, IList<ScreenPoint> pointsToRender)
         {
            var dashArray = this.ActualDashArray;

            if (this.outputBuffer == null)
            {
               this.outputBuffer = new List<ScreenPoint>(pointsToRender.Count);
            }

            rc.DrawClippedLine(clippingRect,
                               pointsToRender,
                               this.MinimumSegmentLength * this.MinimumSegmentLength,
                               this.GetSelectableColor(this.ActualColor),
                               this.StrokeThickness,
                               dashArray,
                               this.LineJoin,
                               this.Aliased,
                               this.outputBuffer);

         }
      }
   }
}
