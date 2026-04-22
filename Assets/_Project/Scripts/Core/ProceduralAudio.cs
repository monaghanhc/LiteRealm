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

        public static AudioClip CreateEmptyMagazineClip()
        {
            float duration = 0.07f;
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];
            int clickAt = Mathf.Max(1, samples / 8);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = i <= clickAt ? 1f : Mathf.Exp(-t * 18f);
                float metalTone = Mathf.Sin(2f * Mathf.PI * 3800f * i / SampleRate);
                data[i] = metalTone * envelope * 0.22f;
            }

            AudioClip clip = AudioClip.Create("ProceduralEmptyMagazine", samples, 1, SampleRate, false);
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

        public static AudioClip CreateZombieAttackClip()
        {
            return CreateNoiseEnvelopeClip("ProceduralZombieAttack", 0.22f, 0.42f, 3.2f, 84f);
        }

        public static AudioClip CreateZombieHitClip()
        {
            return CreateNoiseEnvelopeClip("ProceduralZombieHit", 0.14f, 0.34f, 8f, 132f);
        }

        public static AudioClip CreateZombieDeathClip()
        {
            return CreateNoiseEnvelopeClip("ProceduralZombieDeath", 0.55f, 0.38f, 2.4f, 58f);
        }

        public static AudioClip CreatePlayerHurtClip()
        {
            return CreateNoiseEnvelopeClip("ProceduralPlayerHurt", 0.18f, 0.32f, 6.5f, 190f);
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

        private static AudioClip CreateNoiseEnvelopeClip(string name, float duration, float gain, float decay, float toneHz)
        {
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / Mathf.Max(1, samples - 1);
                float envelope = Mathf.Exp(-t * decay);
                float noise = Random.value * 2f - 1f;
                float tone = Mathf.Sin(2f * Mathf.PI * toneHz * i / SampleRate);
                data[i] = Mathf.Clamp((noise * 0.72f + tone * 0.28f) * envelope * gain, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
