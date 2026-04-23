using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LiteRealm.World
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public class ImmersiveVisualQualityController : MonoBehaviour
    {
        [Header("Quality")]
        [SerializeField] private bool forceHighestQuality = true;
        [SerializeField] private float shadowDistance = 180f;
        [SerializeField] private int antiAliasing = 4;
        [SerializeField] private float lodBias = 2.35f;
        [SerializeField] private int maximumLODLevel = 0;

        [Header("Lighting")]
        [SerializeField] private Color sunColor = new Color(1f, 0.91f, 0.76f, 1f);
        [SerializeField] private float sunIntensity = 1.42f;
        [SerializeField] private Vector3 sunEuler = new Vector3(42f, -31f, 0f);
        [SerializeField] private Color ambientSky = new Color(0.58f, 0.72f, 0.92f, 1f);
        [SerializeField] private Color ambientEquator = new Color(0.34f, 0.43f, 0.39f, 1f);
        [SerializeField] private Color ambientGround = new Color(0.10f, 0.095f, 0.085f, 1f);
        [SerializeField] [Range(0f, 2f)] private float ambientIntensity = 0.95f;
        [SerializeField] [Range(0f, 1f)] private float reflectionIntensity = 0.68f;

        [Header("Atmosphere")]
        [SerializeField] private bool enableFog = true;
        [SerializeField] private FogMode fogMode = FogMode.ExponentialSquared;
        [SerializeField] private Color fogColor = new Color(0.50f, 0.60f, 0.67f, 1f);
        [SerializeField] private float fogDensity = 0.0021f;

        [Header("Sky")]
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private Color skyTint = new Color(0.48f, 0.65f, 0.90f, 1f);
        [SerializeField] private Color skyGroundColor = new Color(0.31f, 0.34f, 0.36f, 1f);
        [SerializeField] [Range(0.2f, 2f)] private float skyExposure = 1.12f;
        [SerializeField] [Range(0.1f, 5f)] private float atmosphereThickness = 1.28f;

        [Header("World Surfaces")]
        [SerializeField] private bool polishRuntimeMaterials = true;
        [SerializeField] private float terrainDetailDistance = 92f;
        [SerializeField] private float terrainTreeDistance = 430f;
        [SerializeField] private float terrainTreeBillboardDistance = 96f;
        [SerializeField] private float terrainBasemapDistance = 1200f;

        private Material runtimeSkyboxMaterial;

        private void Awake()
        {
            Apply();
        }

        private void OnDestroy()
        {
            if (runtimeSkyboxMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(runtimeSkyboxMaterial);
            }
            else
            {
                DestroyImmediate(runtimeSkyboxMaterial);
            }
        }

        public void Apply()
        {
            ApplyQuality();
            ApplyLighting();
            ApplyWorldSurfaces();
            ApplyCameras();
        }

        private void ApplyQuality()
        {
            if (forceHighestQuality && QualitySettings.names != null && QualitySettings.names.Length > 0)
            {
                QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, false);
            }

            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowDistance = shadowDistance;
            QualitySettings.antiAliasing = Mathf.Max(0, antiAliasing);
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.softParticles = true;
            QualitySettings.realtimeReflectionProbes = true;
            QualitySettings.lodBias = lodBias;
            QualitySettings.maximumLODLevel = Mathf.Max(0, maximumLODLevel);
            QualitySettings.skinWeights = SkinWeights.FourBones;
        }

        private void ApplyLighting()
        {
            Material skybox = ResolveSkyboxMaterial();
            if (skybox != null)
            {
                RenderSettings.skybox = skybox;
            }

            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                Light[] lights = FindObjectsOfType<Light>();
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i] != null && lights[i].type == LightType.Directional)
                    {
                        sun = lights[i];
                        break;
                    }
                }
            }

            if (sun != null)
            {
                RenderSettings.sun = sun;
                sun.color = sunColor;
                sun.intensity = sunIntensity;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.92f;
                sun.shadowBias = 0.035f;
                sun.shadowNormalBias = 0.28f;
                sun.transform.rotation = Quaternion.Euler(sunEuler);
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.ambientIntensity = ambientIntensity;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = 128;
            RenderSettings.reflectionIntensity = reflectionIntensity;
            RenderSettings.reflectionBounces = 1;
            RenderSettings.fog = enableFog;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
        }

        private Material ResolveSkyboxMaterial()
        {
            Material skybox = skyboxMaterial != null ? skyboxMaterial : RenderSettings.skybox;
            Shader shader = Shader.Find("Skybox/Procedural");
            if (skybox == null && shader != null)
            {
                if (runtimeSkyboxMaterial == null)
                {
                    runtimeSkyboxMaterial = new Material(shader)
                    {
                        name = "Runtime_ProceduralSkybox"
                    };
                }

                skybox = runtimeSkyboxMaterial;
            }

            if (skybox == null)
            {
                return null;
            }

            if (shader != null && skybox.shader != shader && runtimeSkyboxMaterial == null)
            {
                runtimeSkyboxMaterial = new Material(shader)
                {
                    name = "Runtime_ProceduralSkybox"
                };
                skybox = runtimeSkyboxMaterial;
            }

            SetMaterialFloat(skybox, "_SunDisk", 2f);
            SetMaterialFloat(skybox, "_SunSize", 0.032f);
            SetMaterialFloat(skybox, "_SunSizeConvergence", 5.5f);
            SetMaterialFloat(skybox, "_AtmosphereThickness", atmosphereThickness);
            SetMaterialColor(skybox, "_SkyTint", skyTint);
            SetMaterialColor(skybox, "_GroundColor", skyGroundColor);
            SetMaterialFloat(skybox, "_Exposure", skyExposure);
            return skybox;
        }

        private void ApplyWorldSurfaces()
        {
            Terrain[] terrains = FindObjectsOfType<Terrain>();
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (terrain == null)
                {
                    continue;
                }

                terrain.drawInstanced = true;
                terrain.heightmapPixelError = 3f;
                terrain.detailObjectDistance = terrainDetailDistance;
                terrain.detailObjectDensity = 0.82f;
                terrain.treeDistance = terrainTreeDistance;
                terrain.treeBillboardDistance = terrainTreeBillboardDistance;
                terrain.treeCrossFadeLength = 24f;
                terrain.basemapDistance = terrainBasemapDistance;
            }

            if (!polishRuntimeMaterials)
            {
                return;
            }

            Renderer[] renderers = FindObjectsOfType<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                Material[] materials = renderer.sharedMaterials;
                for (int j = 0; j < materials.Length; j++)
                {
                    PolishMaterial(materials[j], renderer.name);
                }
            }
        }

        private static void PolishMaterial(Material material, string rendererName)
        {
            if (material == null)
            {
                return;
            }

            string materialName = material.name.ToLowerInvariant();
            string objectName = string.IsNullOrEmpty(rendererName) ? string.Empty : rendererName.ToLowerInvariant();

            if (materialName.Contains("water") || objectName.Contains("water"))
            {
                SetMaterialFloat(material, "_Glossiness", 0.82f);
                SetMaterialFloat(material, "_Metallic", 0f);
                SetMaterialColor(material, "_Color", new Color(0.13f, 0.34f, 0.48f, 0.68f));
                return;
            }

            if (materialName.Contains("rock") || materialName.Contains("granite") || objectName.Contains("rock"))
            {
                SetMaterialFloat(material, "_Glossiness", 0.34f);
                SetMaterialFloat(material, "_Metallic", 0.02f);
                return;
            }

            if (materialName.Contains("leaf") || materialName.Contains("needle") || objectName.Contains("crown"))
            {
                SetMaterialFloat(material, "_Glossiness", 0.18f);
                SetMaterialFloat(material, "_Metallic", 0f);
                return;
            }

            if (materialName.Contains("bark") || objectName.Contains("trunk"))
            {
                SetMaterialFloat(material, "_Glossiness", 0.12f);
                SetMaterialFloat(material, "_Metallic", 0f);
                return;
            }

            if (materialName.Contains("rifle") || materialName.Contains("gunmetal") || objectName.Contains("receiver"))
            {
                SetMaterialFloat(material, "_Glossiness", 0.48f);
                SetMaterialFloat(material, "_Metallic", 0.58f);
            }
        }

        private static void ApplyCameras()
        {
            Camera[] cameras = FindObjectsOfType<Camera>();
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera == null)
                {
                    continue;
                }

                camera.allowHDR = true;
                camera.allowMSAA = true;
                camera.useOcclusionCulling = true;
                camera.backgroundColor = RenderSettings.fogColor;
                camera.nearClipPlane = Mathf.Min(camera.nearClipPlane, 0.03f);
                camera.farClipPlane = Mathf.Max(camera.farClipPlane, 1000f);
            }
        }

        private static void SetMaterialFloat(Material material, string propertyName, float value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void SetMaterialColor(Material material, string propertyName, Color value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureControllerExists();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureControllerExists();
        }

        private static void EnsureControllerExists()
        {
            if (FindObjectOfType<ImmersiveVisualQualityController>() != null)
            {
                return;
            }

            if (GameObject.FindGameObjectWithTag("Player") == null)
            {
                return;
            }

            GameObject host = new GameObject("ImmersiveVisualQuality");
            host.AddComponent<ImmersiveVisualQualityController>();
        }
    }
}
