using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class PlaceholderSFXGenerator : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Audio/Generate Placeholder SFX")]
    public static void GenerateSFX()
    {
        string path = "Assets/Audio";
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        CreateTone("place.wav", 440, 0.1f);
        CreateTone("clear.wav", 880, 0.2f);
        CreateTone("click.wav", 1000, 0.05f);
        CreateTone("gameover.wav", 220, 0.5f);

        AssetDatabase.Refresh();
        Debug.Log("Generated placeholder SFX in Assets/Audio");
    }

    private static void CreateTone(string filename, float frequency, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t);
            
            // Fade out
            if (i > sampleCount - 1000)
            {
                samples[i] *= (sampleCount - i) / 1000f;
            }
        }

        AudioClip clip = AudioClip.Create(filename, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);

        SavWav.Save("Assets/Audio/" + filename, clip);
    }
#endif
}

public static class SavWav
{
    public static void Save(string filepath, AudioClip clip)
    {
        if (!filepath.ToLower().EndsWith(".wav")) filepath += ".wav";
        
        Directory.CreateDirectory(Path.GetDirectoryName(filepath));

        using (var fileStream = CreateEmpty(filepath))
        {
            ConvertAndWrite(fileStream, clip);
            WriteHeader(fileStream, clip);
        }
    }

    private static FileStream CreateEmpty(string filepath)
    {
        var fileStream = new FileStream(filepath, FileMode.Create);
        byte emptyByte = new byte();

        for (int i = 0; i < 44; i++) //wav header is 44 bytes
        {
            fileStream.WriteByte(emptyByte);
        }

        return fileStream;
    }

    private static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
    {
        var samples = new float[clip.samples];
        clip.GetData(samples, 0);

        Int16[] intData = new Int16[samples.Length];
        Byte[] bytesData = new Byte[samples.Length * 2];
        const float rescaleFactor = 32767; //to convert float to Int16

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            Byte[] byteArr = new Byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    private static void WriteHeader(FileStream fileStream, AudioClip clip)
    {
        var hz = clip.frequency;
        var channels = clip.channels;
        var samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        UInt16 two = 2;
        UInt16 one = 1;

        Byte[] audioFormat = BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);

        Byte[] numChannels = BitConverter.GetBytes(one);
        fileStream.Write(numChannels, 0, 2);

        Byte[] sampleRate = BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);

        Byte[] byteRate = BitConverter.GetBytes(hz * 2);
        fileStream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort)(2);
        fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        UInt16 bps = 16;
        Byte[] bitsPerSample = BitConverter.GetBytes(bps);
        fileStream.Write(bitsPerSample, 0, 2);

        Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(datastring, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(samples * 2);
        fileStream.Write(subChunk2, 0, 4);

        fileStream.Close();
    }
}
