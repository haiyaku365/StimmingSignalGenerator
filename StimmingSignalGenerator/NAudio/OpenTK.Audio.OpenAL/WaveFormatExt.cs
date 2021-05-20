using NAudio.Wave;
using OpenTK.Audio.OpenAL;
using System;

namespace StimmingSignalGenerator.NAudio.OpenTK.Audio.OpenAL
{
   public static class WaveFormatExt
   {
      public static ALFormat ToALFormat(this WaveFormat waveFormat)
      {
         if (waveFormat.Encoding != WaveFormatEncoding.Pcm)
            throw new ArgumentException("Wave format must be PCM", nameof(waveFormat));
         if (waveFormat.Channels == 1)
         {
            if (waveFormat.BitsPerSample == 8) return ALFormat.Mono8;
            else if (waveFormat.BitsPerSample == 16) return ALFormat.Mono16;
            else throw new ArgumentException("Wave format must be 8 or 16 bit", nameof(waveFormat));
         }
         else if (waveFormat.Channels == 2)
         {
            if (waveFormat.BitsPerSample == 8) return ALFormat.Stereo8;
            else if (waveFormat.BitsPerSample == 16) return ALFormat.Stereo16;
            else throw new ArgumentException("Wave format must be 8 or 16 bit", nameof(waveFormat));
         }
         else throw new ArgumentException("Wave format must be 1 or 2 channels", nameof(waveFormat));
      }
   }
}
