using PortAudioSharp;
using System.Drawing.Printing;
using System.IO;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace StimmingSignalGenerator.NAudio.PortAudio
{
    internal static partial class Native
    {
        public const string PortAudioDLL = "portaudio";

        /// <summary>
        /// https://portaudio.com/docs/v19-doxydocs/portaudio_8h.html#abdb313743d6efef26cecdae787a2bd3d <br/>
        /// Determine whether it would be possible to open a stream with the specified parameters.
        /// </summary>
        /// <param name="inputParameters">
        /// A structure that describes the input parameters used to open a stream.<br/>
        /// The suggestedLatency field is ignored.See PaStreamParameters for a description of these parameters.<br/>
        /// inputParameters must be NULL for output-only streams.</param>
        /// <param name="outputParameters">
        /// A structure that describes the output parameters used to open a stream.<br/>
        /// The suggestedLatency field is ignored.See PaStreamParameters for a description of these parameters.<br/>
        /// outputParameters must be NULL for input-only streams.</param>
        /// <param name="sampleRate">
        /// The required sampleRate. <br/>
        /// For full-duplex streams it is the sample rate for both input and output</param>
        /// <returns>
        /// Returns 0 if the format is supported, and an error code indicating why the format is not supported otherwise.<br/>
        /// The constant paFormatIsSupported is provided to compare with the return value for success.
        /// </returns>
        [DllImport(PortAudioDLL)]
        public static extern int Pa_IsFormatSupported(nint inputParameters, nint outputParameters, double sampleRate);
    }
}
