using System;

namespace StimmingSignalGenerator.Helper
{
   public static class ColorHelper
   {
      // https://www.splinter.com.au/converting-hsv-to-rgb-colour-using-c/
      /// <summary>
      /// Convert HSV to RGB
      /// h is from 0-360
      /// s,v values are 0-1
      /// r,g,b values are 0-255
      /// Based upon http://ilab.usc.edu/wiki/index.php/HSV_And_H2SV_Color_Space#HSV_Transformation_C_.2F_C.2B.2B_Code_2
      /// </summary>
      public static (byte r, byte g, byte b) HsvToRgb(double h, double S, double V)
      {
         double H = h;
         while (H < 0) { H += 360; };
         while (H >= 360) { H -= 360; };
         double R, G, B;
         if (V <= 0)
         { R = G = B = 0; }
         else if (S <= 0)
         {
            R = G = B = V;
         }
         else
         {
            double hf = H / 60.0;
            int i = (int)Math.Floor(hf);
            double f = hf - i;
            double pv = V * (1 - S);
            double qv = V * (1 - S * f);
            double tv = V * (1 - S * (1 - f));
            switch (i)
            {

               // Red is the dominant color

               case 0:
                  R = V;
                  G = tv;
                  B = pv;
                  break;

               // Green is the dominant color

               case 1:
                  R = qv;
                  G = V;
                  B = pv;
                  break;
               case 2:
                  R = pv;
                  G = V;
                  B = tv;
                  break;

               // Blue is the dominant color

               case 3:
                  R = pv;
                  G = qv;
                  B = V;
                  break;
               case 4:
                  R = tv;
                  G = pv;
                  B = V;
                  break;

               // Red is the dominant color

               case 5:
                  R = V;
                  G = pv;
                  B = qv;
                  break;

               // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

               case 6:
                  R = V;
                  G = tv;
                  B = pv;
                  break;
               case -1:
                  R = V;
                  G = pv;
                  B = qv;
                  break;

               // The color is not defined, we should throw an error.

               default:
                  //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                  R = G = B = V; // Just pretend its black/white
                  break;
            }
         }
         var r = Clamp((int)(R * 255.0));
         var g = Clamp((int)(G * 255.0));
         var b = Clamp((int)(B * 255.0));
         return (r, g, b);
      }

      /// <summary>
      /// Clamp a value to 0-255
      /// </summary>
      private static byte Clamp(int i)
      {
         if (i < 0) return 0;
         if (i > 255) return 255;
         return (byte)i;
      }
   }
}
