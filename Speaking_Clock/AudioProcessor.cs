using NAudio.Wave;
using NWaves.Filters;

namespace Speaking_Clock;

public class AudioProcessor
{
    private static readonly WienerFilter WienerFilter = new(3, 1000);

    public static byte[] ApplyWienerFilter(byte[] inputBuffer, WaveFormat waveFormat)
    {
        // Step 1: Convert byte buffer (PCM) to float array for processing
        var bytesPerSample = waveFormat.BitsPerSample / 8;
        var sampleCount = inputBuffer.Length / bytesPerSample;
        var floatSamples = new float[sampleCount];

        // Assuming 16-bit PCM input
        for (var i = 0; i < sampleCount; i++)
        {
            // Convert each 16-bit PCM value to float in range -1.0 to 1.0
            var sample = BitConverter.ToInt16(inputBuffer, i * bytesPerSample);
            floatSamples[i] = sample / 32768f;
        }

        // Step 2: Apply the Wiener Filter from NWaves library
        // NWaves assumes input is a float array, but we'll process sample by sample

        var filteredSamples = new float[sampleCount];

        for (var i = 0; i < sampleCount; i++)
            // Process each sample individually
            filteredSamples[i] = WienerFilter.Process(floatSamples[i]);

        // Step 3: Convert processed float samples back to byte buffer (PCM)
        var outputBuffer = new byte[inputBuffer.Length];
        for (var i = 0; i < sampleCount; i++)
        {
            // Convert float sample back to 16-bit PCM
            var sample = (short)Math.Max(Math.Min(filteredSamples[i] * 32768, 32767), -32768);
            var byteSample = BitConverter.GetBytes(sample);
            Buffer.BlockCopy(byteSample, 0, outputBuffer, i * bytesPerSample, bytesPerSample);
        }

        return outputBuffer;
    }
}