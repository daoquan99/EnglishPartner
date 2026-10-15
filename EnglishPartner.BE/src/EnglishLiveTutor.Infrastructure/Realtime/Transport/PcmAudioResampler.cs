using System.Buffers.Binary;

namespace EnglishLiveTutor.Infrastructure.Realtime;

public static class PcmAudioResampler
{
    public static string Resample24kTo16kBase64(string base64Pcm24k)
    {
        if (string.IsNullOrWhiteSpace(base64Pcm24k))
            return string.Empty;

        byte[] inputBytes = Convert.FromBase64String(base64Pcm24k);
        int inputSamples = inputBytes.Length / 2;
        if (inputSamples == 0)
            return string.Empty;

        int outputSamples = (int)((long)inputSamples * 16000 / 24000);
        byte[] outputBytes = new byte[outputSamples * 2];

        for (int i = 0; i < outputSamples; i++)
        {
            double srcIndex = i * 1.5;
            int idx0 = (int)srcIndex;
            int idx1 = Math.Min(idx0 + 1, inputSamples - 1);
            double frac = srcIndex - idx0;

            short s0 = BinaryPrimitives.ReadInt16LittleEndian(inputBytes.AsSpan(idx0 * 2, 2));
            short s1 = BinaryPrimitives.ReadInt16LittleEndian(inputBytes.AsSpan(idx1 * 2, 2));

            short interpolated = (short)Math.Clamp(Math.Round(s0 + (s1 - s0) * frac), short.MinValue, short.MaxValue);
            BinaryPrimitives.WriteInt16LittleEndian(outputBytes.AsSpan(i * 2, 2), interpolated);
        }

        return Convert.ToBase64String(outputBytes);
    }
}
