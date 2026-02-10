using UnityEngine;

namespace LiteRealm.Core
{
    /// <summary>
    /// Creates minimal procedural AudioClips when no assets are assigned, so SFX always play.
    /// </summary>
    public static class ProceduralAudio
    {
        private const int SampleRate = 22050;

        public static AudioClip CreateShootClip()
        {
            float duration = 0.08f;
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = 1f - (t * t);
                data[i] = (Random.value * 2f - 1f) * envelope * 0.4f;
            }
            AudioClip clip = AudioClip.Create("ProceduralShoot", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateReloadClip()
        {
            float duration = 0.06f;
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];
            int clickAt = samples / 4;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = i <= clickAt ? 1f : Mathf.Exp(-(i - clickAt) / (samples * 0.1f));
                data[i] = (Random.value * 2f - 1f) * envelope * 0.25f;
            }
            AudioClip clip = AudioClip.Create("ProceduralReload", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateImpactClip()
        {
            float duration = 0.05f;
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = Mathf.Exp(-t * 8f);
                data[i] = (Random.value * 2f - 1f) * envelope * 0.3f;
            }
            AudioClip clip = AudioClip.Create("ProceduralImpact", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateFootstepClip()
        {
            float duration = 0.06f;
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = Mathf.Exp(-t * 6f);
                data[i] = (Random.value * 2f - 1f) * envelope * 0.2f;
            }
            AudioClip clip = AudioClip.Create("ProceduralFootstep", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
