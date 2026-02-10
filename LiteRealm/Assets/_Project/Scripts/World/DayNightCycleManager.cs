using System;
using LiteRealm.Core;
using UnityEngine;

namespace LiteRealm.World
{
    public class DayNightCycleManager : MonoBehaviour
    {
        [SerializeField] [Min(0.1f)] private float dayLengthMinutes = 24f;
        [SerializeField] [Range(0f, 1f)] private float startTimeNormalized = 0.25f;

        [Header("Lighting")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Gradient ambientColorGradient;
        [SerializeField] private Gradient fogColorGradient;
        [SerializeField] private AnimationCurve sunIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private float maxSunIntensity = 1.2f;
        [SerializeField] private float skyboxRotationSpeed = 360f;

        [Header("References")]
        [SerializeField] private GameEventHub eventHub;

        public event Action<bool> NightStateChanged;
        public event Action<float, int> TimeUpdated;

        public float DayLengthMinutes => dayLengthMinutes;
        public Light SunLight => sunLight;
        public GameEventHub EventHub => eventHub;
        public float NormalizedTime { get; private set; }
        public int CurrentDay { get; private set; }
        public bool IsNight { get; private set; }

        private void Awake()
        {
            if (ambientColorGradient == null || ambientColorGradient.colorKeys.Length == 0)
            {
                ambientColorGradient = BuildDefaultAmbientGradient();
            }

            if (fogColorGradient == null || fogColorGradient.colorKeys.Length == 0)
            {
                fogColorGradient = BuildDefaultFogGradient();
            }

            NormalizedTime = Mathf.Repeat(startTimeNormalized, 1f);
            CurrentDay = 1;
            EvaluateLighting(true);
        }

        private void Update()
        {
            float dayDurationSeconds = dayLengthMinutes * 60f;
            float deltaNormalized = Time.deltaTime / dayDurationSeconds;
            float previous = NormalizedTime;
            NormalizedTime = Mathf.Repeat(NormalizedTime + deltaNormalized, 1f);

            if (NormalizedTime < previous)
            {
                CurrentDay++;
            }

            EvaluateLighting(false);
        }

        public void SetTimeNormalized(float normalized)
        {
            NormalizedTime = Mathf.Repeat(normalized, 1f);
            EvaluateLighting(false);
        }

        public void SetDayLengthMinutes(float minutes)
        {
            dayLengthMinutes = Mathf.Max(0.1f, minutes);
        }

        public void SetDayAndTime(int day, float normalized)
        {
            CurrentDay = Mathf.Max(1, day);
            NormalizedTime = Mathf.Repeat(normalized, 1f);
            EvaluateLighting(false);
        }

        public string GetDisplayTime24h()
        {
            float totalMinutes = NormalizedTime * 24f * 60f;
            int hour = Mathf.FloorToInt(totalMinutes / 60f) % 24;
            int minute = Mathf.FloorToInt(totalMinutes % 60f);
            return $"{hour:00}:{minute:00}";
        }

        private void EvaluateLighting(bool force)
        {
            float sunAngle = (NormalizedTime * 360f) - 90f;
            float sunHeight = Mathf.Sin(NormalizedTime * Mathf.PI * 2f);
            bool nowNight = sunHeight <= 0f;

            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
                float intensity01 = Mathf.Clamp01((sunHeight + 1f) * 0.5f);
                float evaluatedIntensity = sunIntensityCurve.Evaluate(intensity01) * maxSunIntensity;
                sunLight.intensity = evaluatedIntensity;
            }

            RenderSettings.ambientLight = ambientColorGradient.Evaluate(Mathf.Clamp01((sunHeight + 1f) * 0.5f));
            RenderSettings.fogColor = fogColorGradient.Evaluate(Mathf.Clamp01((sunHeight + 1f) * 0.5f));

            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Rotation"))
            {
                float rotation = NormalizedTime * skyboxRotationSpeed;
                RenderSettings.skybox.SetFloat("_Rotation", rotation);
            }

            if (force || nowNight != IsNight)
            {
                IsNight = nowNight;
                NightStateChanged?.Invoke(IsNight);
                eventHub?.RaiseNightStateChanged(IsNight);
            }

            TimeUpdated?.Invoke(NormalizedTime, CurrentDay);
            eventHub?.RaiseTimeChanged(NormalizedTime, CurrentDay);
        }

        private void Reset()
        {
            ambientColorGradient = BuildDefaultAmbientGradient();
            fogColorGradient = BuildDefaultFogGradient();
        }

        private Gradient BuildDefaultAmbientGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.08f, 0.08f, 0.14f), 0f),
                    new GradientColorKey(new Color(0.75f, 0.73f, 0.68f), 0.5f),
                    new GradientColorKey(new Color(0.08f, 0.08f, 0.14f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            return gradient;
        }

        private Gradient BuildDefaultFogGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.04f, 0.04f, 0.08f), 0f),
                    new GradientColorKey(new Color(0.58f, 0.63f, 0.68f), 0.5f),
                    new GradientColorKey(new Color(0.04f, 0.04f, 0.08f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            return gradient;
        }
    }
}
