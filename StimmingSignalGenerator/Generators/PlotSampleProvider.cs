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
   class PlotSampleProvider : ISingleSignalInputSampleProvider
   {
      public ISampleProvider InputSample { get; set; }
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
               if (isHighDefinition)
                  lineSeries.Decimator = null;
               else
                  lineSeries.Decimator = Decimator.Decimate;
            }
         }
      }
      public WaveFormat WaveFormat => InputSample.WaveFormat;

      private bool isHighDefinition;
      private readonly LineSeries lineSeries = new LineSeries();
      private readonly SynchronizationContext synchronizationContext;

      public PlotSampleProvider(
         ISampleProvider inputSample)
         : this(inputSample, SynchronizationContext.Current) { }

      public PlotSampleProvider(
      ISampleProvider inputSample,
      SynchronizationContext synchronizationContext)
      {
         InputSample = inputSample;

         PlotModel = new PlotModel();
         PointLimit = WaveFormat.SampleRate / 4;
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

         lineSeries.Decimator = Decimator.Decimate;
         this.synchronizationContext = synchronizationContext;

         PlotModel.Series.Add(lineSeries);
      }

      int xIdx;
      public int Read(float[] buffer, int offset, int count)
      {
         var read = InputSample.Read(buffer, offset, count);
         if (!IsEnable) return read;

         if (lineSeries.Points.Count > PointLimit)
         {
            //shift out old data
            var oldPoints = lineSeries.Points.Skip(read).ToArray();
            lineSeries.Points.Clear();
            for (int i = 0; i < oldPoints.Length; i++)
            {
               lineSeries.Points.Add(new DataPoint(i, oldPoints[i].Y));
            }
            xIdx = lineSeries.Points.Count;
         }
         //insert new data
         for (int i = 0; i < read; i++)
         {
            lineSeries.Points.Add(new DataPoint(xIdx + i, buffer[i]));
         }
         xIdx += read;
         synchronizationContext.Post(_ => PlotModel.InvalidatePlot(true), null);

         return read;
      }
   }
}
