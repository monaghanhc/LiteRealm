using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteRealm.Player
{
    [DisallowMultipleComponent]
    public class PlayerCharacterVisualController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Transform visualRoot;

        [Header("Build")]
        [SerializeField] private bool buildOnAwake = true;
        [SerializeField] private bool hideRootRenderer = true;
        [SerializeField] private float visualScale = 1f;

        [Header("Animation")]
        [SerializeField] private bool animateProceduralBody = true;
        [SerializeField] private float walkSwing = 9f;
        [SerializeField] private float sprintSwing = 15f;
        [SerializeField] private float bodyBob = 0.024f;
        [SerializeField] private float animationSmoothing = 15f;

        private readonly List<Renderer> skinRenderers = new List<Renderer>();
        private readonly List<Renderer> hairRenderers = new List<Renderer>();
        private readonly List<Renderer> outfitRenderers = new List<Renderer>();
        private readonly List<Renderer> armorRenderers = new List<Renderer>();
        private readonly List<GameObject> hairStyleObjects = new List<GameObject>();
        private readonly List<Material> runtimeMaterials = new List<Material>();

        private Transform leftArm;
        private Transform rightArm;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform torso;
        private Vector3 visualRootBaseLocalPosition;
        private float animationTime;
        private CharacterCustomizationData currentData;

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }

            if (hideRootRenderer)
            {
                Renderer rootRenderer = GetComponent<Renderer>();
                if (rootRenderer != null)
                {
                    rootRenderer.enabled = false;
                }
            }

            if (buildOnAwake)
            {
                RebuildVisual();
            }
        }

        private void Update()
        {
            if (animateProceduralBody)
            {
                AnimateBody(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < runtimeMaterials.Count; i++)
            {
                if (runtimeMaterials[i] != null)
                {
                    DestroyRuntime(runtimeMaterials[i]);
                }
            }

            runtimeMaterials.Clear();
        }

        public void RebuildVisual()
        {
            ClearVisual();
            currentData = CharacterCustomizationState.Current;

            GameObject root = new GameObject("CharacterVisual");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one * visualScale;
            visualRoot = root.transform;
            visualRootBaseLocalPosition = visualRoot.localPosition;

            BuildBody();
            ApplyCustomization(currentData);
        }

        public void ApplyCustomization(CharacterCustomizationData data)
        {
            currentData = CharacterCustomizationState.Clamp(data);
            ApplyColor(skinRenderers, CharacterCustomizationState.GetSkinColor(currentData), 0.18f, 0f);
            ApplyColor(hairRenderers, CharacterCustomizationState.GetHairColor(currentData), 0.22f, 0f);
            ApplyColor(outfitRenderers, CharacterCustomizationState.GetOutfitColor(currentData), 0.34f, 0.05f);
            ApplyColor(armorRenderers, new Color(0.055f, 0.065f, 0.07f, 1f), 0.48f, 0.28f);

            for (int i = 0; i < hairStyleObjects.Count; i++)
            {
                hairStyleObjects[i].SetActive(i == currentData.HairStyleIndex);
            }
        }

        private void BuildBody()
        {
            Material skin = CreateRuntimeMaterial("Skin", CharacterCustomizationState.GetSkinColor(currentData), 0.18f, 0f);
            Material hair = CreateRuntimeMaterial("Hair", CharacterCustomizationState.GetHairColor(currentData), 0.22f, 0f);
            Material outfit = CreateRuntimeMaterial("Outfit", CharacterCustomizationState.GetOutfitColor(currentData), 0.30f, 0.03f);
            Material armor = CreateRuntimeMaterial("Armor", new Color(0.047f, 0.055f, 0.06f, 1f), 0.52f, 0.34f);
            Material accent = CreateRuntimeMaterial("Accent", new Color(0.34f, 0.43f, 0.36f, 1f), 0.28f, 0.04f);

            torso = Primitive("TacticalJacket", PrimitiveType.Capsule, new Vector3(0f, 1.03f, 0f), new Vector3(0.38f, 0.48f, 0.30f), outfit, outfitRenderers);
            Primitive("VestPlate", PrimitiveType.Cube, new Vector3(0f, 1.12f, 0.19f), new Vector3(0.52f, 0.58f, 0.08f), armor, armorRenderers);
            Primitive("Belt", PrimitiveType.Cube, new Vector3(0f, 0.68f, 0.02f), new Vector3(0.58f, 0.09f, 0.36f), armor, armorRenderers);
            Primitive("Backpack", PrimitiveType.Cube, new Vector3(0f, 1.08f, -0.25f), new Vector3(0.52f, 0.66f, 0.16f), accent, outfitRenderers);

            Transform neck = Primitive("Neck", PrimitiveType.Cylinder, new Vector3(0f, 1.50f, 0f), new Vector3(0.10f, 0.11f, 0.10f), skin, skinRenderers);
            neck.localRotation = Quaternion.Euler(0f, 0f, 0f);
            Primitive("Head", PrimitiveType.Sphere, new Vector3(0f, 1.74f, 0.02f), new Vector3(0.26f, 0.30f, 0.25f), skin, skinRenderers);
            Primitive("FaceMask", PrimitiveType.Cube, new Vector3(0f, 1.64f, 0.23f), new Vector3(0.34f, 0.10f, 0.05f), armor, armorRenderers);

            leftArm = Limb("LeftArm", -0.38f, 1.08f, outfit, outfitRenderers);
            rightArm = Limb("RightArm", 0.38f, 1.08f, outfit, outfitRenderers);
            leftLeg = Leg("LeftLeg", -0.17f, outfit, outfitRenderers);
            rightLeg = Leg("RightLeg", 0.17f, outfit, outfitRenderers);

            Primitive("LeftGlove", PrimitiveType.Sphere, new Vector3(-0.55f, 0.62f, 0.04f), new Vector3(0.11f, 0.09f, 0.09f), armor, armorRenderers);
            Primitive("RightGlove", PrimitiveType.Sphere, new Vector3(0.55f, 0.62f, 0.04f), new Vector3(0.11f, 0.09f, 0.09f), armor, armorRenderers);
            Primitive("LeftBoot", PrimitiveType.Cube, new Vector3(-0.17f, 0.08f, 0.06f), new Vector3(0.18f, 0.13f, 0.30f), armor, armorRenderers);
            Primitive("RightBoot", PrimitiveType.Cube, new Vector3(0.17f, 0.08f, 0.06f), new Vector3(0.18f, 0.13f, 0.30f), armor, armorRenderers);

            hairStyleObjects.Add(BuildHairShort(hair));
            hairStyleObjects.Add(BuildHairMohawk(hair));
            hairStyleObjects.Add(BuildHairBuzz(hair));
            hairStyleObjects.Add(BuildHood(outfit));
        }

        private Transform Limb(string name, float x, float y, Material material, List<Renderer> renderers)
        {
            Transform arm = Primitive(name, PrimitiveType.Capsule, new Vector3(x, y, 0.01f), new Vector3(0.13f, 0.36f, 0.13f), material, renderers);
            arm.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? -11f : 11f);
            return arm;
        }

        private Transform Leg(string name, float x, Material material, List<Renderer> renderers)
        {
            Transform leg = Primitive(name, PrimitiveType.Capsule, new Vector3(x, 0.36f, 0f), new Vector3(0.15f, 0.36f, 0.15f), material, renderers);
            leg.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? 2f : -2f);
            return leg;
        }

        private GameObject BuildHairShort(Material material)
        {
            Transform hair = Primitive("HairShort", PrimitiveType.Sphere, new Vector3(0f, 1.92f, -0.01f), new Vector3(0.25f, 0.10f, 0.23f), material, hairRenderers);
            return hair.gameObject;
        }

        private GameObject BuildHairMohawk(Material material)
        {
            GameObject root = new GameObject("HairMohawk");
            root.transform.SetParent(visualRoot, false);
            for (int i = 0; i < 4; i++)
            {
                Transform spike = Primitive("Spike_" + i, PrimitiveType.Cube, new Vector3(0f, 1.94f + i * 0.02f, -0.11f + i * 0.07f), new Vector3(0.08f, 0.20f, 0.045f), material, hairRenderers);
                spike.SetParent(root.transform, true);
            }

            return root;
        }

        private GameObject BuildHairBuzz(Material material)
        {
            Transform buzz = Primitive("HairBuzz", PrimitiveType.Sphere, new Vector3(0f, 1.88f, 0.01f), new Vector3(0.27f, 0.055f, 0.24f), material, hairRenderers);
            return buzz.gameObject;
        }

        private GameObject BuildHood(Material material)
        {
            Transform hood = Primitive("Hood", PrimitiveType.Sphere, new Vector3(0f, 1.78f, -0.03f), new Vector3(0.34f, 0.36f, 0.31f), material, outfitRenderers);
            return hood.gameObject;
        }

        private Transform Primitive(string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material, List<Renderer> bucket)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(visualRoot, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyRuntime(collider);
            }

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                bucket?.Add(renderer);
            }

            return go.transform;
        }

        private void AnimateBody(float deltaTime)
        {
            if (visualRoot == null || playerController == null)
            {
                return;
            }

            float speed = playerController.PlanarSpeed;
            float targetSwing = playerController.IsSprinting ? sprintSwing : walkSwing;
            if (!playerController.IsGrounded || speed < 0.15f)
            {
                targetSwing = 0f;
            }

            animationTime += deltaTime * Mathf.Lerp(4f, 8f, Mathf.Clamp01(speed / 7f));
            float swing = Mathf.Sin(animationTime) * targetSwing;
            float bob = Mathf.Abs(Mathf.Sin(animationTime)) * bodyBob * Mathf.Clamp01(speed / 4.5f);

            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, visualRootBaseLocalPosition + Vector3.up * bob, animationSmoothing * deltaTime);
            SetLocalX(leftArm, swing);
            SetLocalX(rightArm, -swing);
            SetLocalX(leftLeg, -swing * 0.65f);
            SetLocalX(rightLeg, swing * 0.65f);
            if (torso != null)
            {
                torso.localRotation = Quaternion.Slerp(torso.localRotation, Quaternion.Euler(0f, 0f, Mathf.Sin(animationTime * 0.5f) * 1.5f), animationSmoothing * deltaTime);
            }
        }

        private static void SetLocalX(Transform target, float xAngle)
        {
            if (target != null)
            {
                target.localRotation = Quaternion.Euler(xAngle, target.localEulerAngles.y, target.localEulerAngles.z);
            }
        }

        private void ApplyColor(List<Renderer> renderers, Color color, float smoothness, float metallic)
        {
            Material material = null;
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null && renderers[i].sharedMaterial != null)
                {
                    material = renderers[i].sharedMaterial;
                    break;
                }
            }

            if (material == null)
            {
                material = CreateRuntimeMaterial("RuntimeCharacterMaterial", color, smoothness, metallic);
            }

            UpdateMaterial(material, color, smoothness, metallic);
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].sharedMaterial = material;
                }
            }
        }

        private Material CreateRuntimeMaterial(string materialName, Color color, float smoothness, float metallic)
        {
            Shader shader = Shader.Find("Standard");
            Material material = new Material(shader != null ? shader : Shader.Find("Diffuse"));
            material.name = materialName;
            material.color = color;
            material.enableInstancing = true;
            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_SpecColor"))
            {
                material.SetColor("_SpecColor", Color.Lerp(Color.black, Color.white, Mathf.Clamp01(smoothness * 0.35f)));
            }

            runtimeMaterials.Add(material);
            return material;
        }

        private static void UpdateMaterial(Material material, Color color, float smoothness, float metallic)
        {
            if (material == null)
            {
                return;
            }

            material.color = color;
            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_SpecColor"))
            {
                material.SetColor("_SpecColor", Color.Lerp(Color.black, Color.white, Mathf.Clamp01(smoothness * 0.35f)));
            }
        }

        private void ClearVisual()
        {
            if (visualRoot != null)
            {
                DestroyRuntime(visualRoot.gameObject);
            }

            visualRoot = null;
            skinRenderers.Clear();
            hairRenderers.Clear();
            outfitRenderers.Clear();
            armorRenderers.Clear();
            hairStyleObjects.Clear();
            leftArm = null;
            rightArm = null;
            leftLeg = null;
            rightLeg = null;
            torso = null;
        }

        private static void DestroyRuntime(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsurePlayerVisualExists();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsurePlayerVisualExists();
        }

        private static void EnsurePlayerVisualExists()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                return;
            }

            if (player.GetComponent<PlayerCharacterVisualController>() == null)
            {
                player.AddComponent<PlayerCharacterVisualController>();
            }
        }
    }
}
