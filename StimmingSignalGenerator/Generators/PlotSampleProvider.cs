using NAudio.Wave;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using StimmingSignalGenerator.Generators.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace StimmingSignalGenerator.Generators
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
               for (int i = 0; i < lineSeries.Length; i++)
               {
                  if (isHighDefinition)
                  {
                     lineSeries[i].Decimator = null;
                     lineSeries[i].MinimumSegmentLength = 2;
                  }
                  else
                  {
                     lineSeries[i].Decimator = Decimator.Decimate;
                     lineSeries[i].MinimumSegmentLength = 5;
                  }
               }
            }
         }
      }
      public WaveFormat WaveFormat => InputSample.WaveFormat;

      private bool isHighDefinition;
      private readonly LineSeries[] lineSeries;
      private readonly int lineCount;
      private readonly SynchronizationContext synchronizationContext;
      private static readonly Random rand = new Random();
      public PlotSampleProvider(
         ISampleProvider inputSample)
         : this(inputSample, SynchronizationContext.Current) { }

      public PlotSampleProvider(
      ISampleProvider inputSample,
      SynchronizationContext synchronizationContext)
      {
         InputSample = inputSample;
         lineCount = inputSample.WaveFormat.Channels;
         lineSeries = new LineSeries[lineCount];
         Array.ForEach(lineSeries, x => x = new LineSeries());
         lineSeries[0] = new LineSeries() { Color = OxyColor.FromArgb(180, 0, 0, 0), MinimumSegmentLength = 5 };
         if (lineCount > 1)
         {
            lineSeries[1] = new LineSeries() { Color = OxyColor.FromArgb(180, 255, 0, 0), MinimumSegmentLength = 5 };
            for (int i = 2; i < lineCount; i++)
            {
               var (r, g, b) = Helper.ColorHelper.HsvToRgb(rand.Next(0, 360), 1, 1);
               lineSeries[i] = new LineSeries() { Color = OxyColor.FromArgb(180, r, g, b), MinimumSegmentLength = 5 };
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
              AbsoluteMaximum = PointLimit
           });

         this.synchronizationContext = synchronizationContext;
         for (int i = 0; i < lineCount; i++)
         {
            lineSeries[i].Decimator = Decimator.Decimate;
            PlotModel.Series.Add(lineSeries[i]);
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
         synchronizationContext.Post(_ => PlotModel.InvalidatePlot(true), null);

         return read;
      }
   }
}
