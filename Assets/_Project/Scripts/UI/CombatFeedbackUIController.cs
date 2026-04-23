using LiteRealm.Combat;
using LiteRealm.Core;
using LiteRealm.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    [DisallowMultipleComponent]
    public class CombatFeedbackUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private WeaponManager weaponManager;
        [SerializeField] private GameEventHub eventHub;

        [Header("Crosshair")]
        [SerializeField] private bool buildRuntimeCrosshair = true;
        [SerializeField] private Color crosshairColor = new Color(1f, 1f, 1f, 0.72f);
        [SerializeField] private Vector2 crosshairLineSize = new Vector2(10f, 2f);
        [SerializeField] private float crosshairGap = 8f;

        [Header("Hit Marker")]
        [SerializeField] private Color hitMarkerColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color killMarkerColor = new Color(1f, 0.18f, 0.13f, 1f);
        [SerializeField] private float hitMarkerDuration = 0.16f;
        [SerializeField] private float hitMarkerFadeSpeed = 9f;

        [Header("Damage Vignette")]
        [SerializeField] private Color damageVignetteColor = new Color(0.75f, 0.02f, 0.01f, 0.32f);
        [SerializeField] private float damageFlashAlpha = 0.28f;
        [SerializeField] private float damageFadeSpeed = 4.8f;

        [Header("Status Text")]
        [SerializeField] private Color statusTextColor = new Color(0.9f, 0.96f, 0.95f, 1f);
        [SerializeField] private Color warningTextColor = new Color(1f, 0.32f, 0.24f, 1f);
        [SerializeField] private float statusMessageDuration = 1.25f;
        [SerializeField] private Text statusText;

        [Header("Runtime UI")]
        [SerializeField] private Image damageVignette;
        [SerializeField] private Image[] crosshairLines;
        [SerializeField] private Image[] hitMarkerLines;

        public string CurrentStatusText => statusText != null ? statusText.text : string.Empty;
        public float HitMarkerAlpha => hitMarkerAlpha;
        public float DamageVignetteAlpha => damageVignetteAlpha;

        private GameObject playerObject;
        private float hitMarkerTimer;
        private float hitMarkerAlpha;
        private float damageVignetteAlpha;
        private float statusTimer;
        private Color activeHitMarkerColor;
        private bool uiBuilt;

        private void Awake()
        {
            ResolveReferences();
            BuildRuntimeUiIfNeeded();
            activeHitMarkerColor = hitMarkerColor;
        }

        private void OnEnable()
        {
            ResolveReferences();
            BuildRuntimeUiIfNeeded();
            Subscribe();
        }

        private void Start()
        {
            ResolveReferences();
            BuildRuntimeUiIfNeeded();
            Subscribe();
            ShowWeaponStatus(weaponManager != null ? weaponManager.ActiveWeapon : null);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;

            if (hitMarkerTimer > 0f)
            {
                hitMarkerTimer -= deltaTime;
                hitMarkerAlpha = 1f;
            }
            else
            {
                hitMarkerAlpha = Mathf.MoveTowards(hitMarkerAlpha, 0f, hitMarkerFadeSpeed * deltaTime);
            }

            damageVignetteAlpha = Mathf.MoveTowards(damageVignetteAlpha, 0f, damageFadeSpeed * deltaTime);

            if (statusTimer > 0f)
            {
                statusTimer -= deltaTime;
            }
            else if (statusText != null && !string.IsNullOrEmpty(statusText.text))
            {
                statusText.text = string.Empty;
            }

            ApplyGraphicAlpha(hitMarkerLines, activeHitMarkerColor, hitMarkerAlpha);
            if (damageVignette != null)
            {
                Color color = damageVignetteColor;
                color.a = damageVignetteAlpha;
                damageVignette.color = color;
            }
        }

        public void Bind(PlayerStats stats, WeaponManager weapons, GameEventHub hub)
        {
            Unsubscribe();
            playerStats = stats;
            weaponManager = weapons;
            eventHub = hub;
            playerObject = stats != null ? stats.gameObject : null;
            Subscribe();
        }

        public void ShowMessage(string message, bool warning)
        {
            if (statusText == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            statusText.text = message;
            statusText.color = warning ? warningTextColor : statusTextColor;
            statusTimer = statusMessageDuration;
        }

        private void ResolveReferences()
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    canvas = GetComponent<Canvas>();
                }
            }

            if (playerObject == null)
            {
                playerObject = GameObject.FindGameObjectWithTag("Player");
            }

            if (playerStats == null && playerObject != null)
            {
                playerStats = playerObject.GetComponent<PlayerStats>();
            }

            if (weaponManager == null && playerObject != null)
            {
                weaponManager = playerObject.GetComponent<WeaponManager>();
            }

            if (eventHub == null)
            {
                eventHub = FindObjectOfType<GameEventHub>();
            }
        }

        private void Subscribe()
        {
            if (playerStats != null)
            {
                playerStats.Damaged -= OnPlayerDamaged;
                playerStats.Damaged += OnPlayerDamaged;
            }

            if (weaponManager != null)
            {
                weaponManager.ActiveWeaponChanged -= OnActiveWeaponChanged;
                weaponManager.ActiveWeaponChanged += OnActiveWeaponChanged;
                weaponManager.ReloadStarted -= OnReloadStarted;
                weaponManager.ReloadStarted += OnReloadStarted;
                weaponManager.EmptyMagazineTriggered -= OnEmptyMagazine;
                weaponManager.EmptyMagazineTriggered += OnEmptyMagazine;
                weaponManager.AmmoChanged -= OnAmmoChanged;
                weaponManager.AmmoChanged += OnAmmoChanged;
            }

            if (eventHub != null)
            {
                eventHub.DamageDealt -= OnDamageDealt;
                eventHub.DamageDealt += OnDamageDealt;
            }
        }

        private void Unsubscribe()
        {
            if (playerStats != null)
            {
                playerStats.Damaged -= OnPlayerDamaged;
            }

            if (weaponManager != null)
            {
                weaponManager.ActiveWeaponChanged -= OnActiveWeaponChanged;
                weaponManager.ReloadStarted -= OnReloadStarted;
                weaponManager.EmptyMagazineTriggered -= OnEmptyMagazine;
                weaponManager.AmmoChanged -= OnAmmoChanged;
            }

            if (eventHub != null)
            {
                eventHub.DamageDealt -= OnDamageDealt;
            }
        }

        private void OnDamageDealt(DamageDealtEvent data)
        {
            if (playerObject == null)
            {
                ResolveReferences();
            }

            if (data.Instigator != playerObject || data.Amount <= 0f)
            {
                return;
            }

            activeHitMarkerColor = data.Killed ? killMarkerColor : hitMarkerColor;
            hitMarkerTimer = Mathf.Max(0.02f, hitMarkerDuration);
            hitMarkerAlpha = 1f;
            if (data.Killed)
            {
                ShowMessage("Target down", false);
            }
        }

        private void OnPlayerDamaged(DamageInfo damageInfo)
        {
            if (damageInfo.Amount <= 0f)
            {
                return;
            }

            damageVignetteAlpha = Mathf.Max(damageVignetteAlpha, Mathf.Clamp01(damageFlashAlpha));
        }

        private void OnActiveWeaponChanged(WeaponBase weapon)
        {
            ShowWeaponStatus(weapon);
        }

        private void OnReloadStarted(WeaponBase weapon)
        {
            if (weapon == null)
            {
                ShowMessage("Reloading", false);
                return;
            }

            ShowMessage($"Reloading {weapon.WeaponDisplayName}", false);
        }

        private void OnEmptyMagazine(WeaponBase weapon)
        {
            ShowMessage("No ammo", true);
        }

        private void OnAmmoChanged(int current, int max)
        {
            if (current <= 0 && max > 0)
            {
                ShowMessage("Empty - reload", true);
            }
        }

        private void ShowWeaponStatus(WeaponBase weapon)
        {
            if (weapon != null)
            {
                ShowMessage(weapon.WeaponDisplayName, false);
            }
        }

        private void BuildRuntimeUiIfNeeded()
        {
            if (uiBuilt)
            {
                return;
            }

            if (canvas == null)
            {
                canvas = gameObject.GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = gameObject.AddComponent<Canvas>();
                }

                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 95;
                CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
                if (scaler == null)
                {
                    scaler = gameObject.AddComponent<CanvasScaler>();
                }

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            RectTransform root = canvas.GetComponent<RectTransform>();
            if (root == null)
            {
                uiBuilt = true;
                return;
            }

            if (damageVignette == null)
            {
                damageVignette = CreateImage(root, "DamageVignette", damageVignetteColor);
                SetStretch(damageVignette.rectTransform);
                damageVignette.raycastTarget = false;
            }

            if (buildRuntimeCrosshair && (crosshairLines == null || crosshairLines.Length == 0))
            {
                crosshairLines = new[]
                {
                    CreateLine(root, "CrosshairTop", crosshairLineSize, new Vector2(0f, crosshairGap + crosshairLineSize.x * 0.5f), 90f, crosshairColor),
                    CreateLine(root, "CrosshairBottom", crosshairLineSize, new Vector2(0f, -(crosshairGap + crosshairLineSize.x * 0.5f)), 90f, crosshairColor),
                    CreateLine(root, "CrosshairLeft", crosshairLineSize, new Vector2(-(crosshairGap + crosshairLineSize.x * 0.5f), 0f), 0f, crosshairColor),
                    CreateLine(root, "CrosshairRight", crosshairLineSize, new Vector2(crosshairGap + crosshairLineSize.x * 0.5f, 0f), 0f, crosshairColor)
                };
            }

            if (hitMarkerLines == null || hitMarkerLines.Length == 0)
            {
                Vector2 hitSize = new Vector2(18f, 3f);
                hitMarkerLines = new[]
                {
                    CreateLine(root, "HitMarkerTopLeft", hitSize, new Vector2(-16f, 16f), 45f, Color.clear),
                    CreateLine(root, "HitMarkerTopRight", hitSize, new Vector2(16f, 16f), -45f, Color.clear),
                    CreateLine(root, "HitMarkerBottomLeft", hitSize, new Vector2(-16f, -16f), -45f, Color.clear),
                    CreateLine(root, "HitMarkerBottomRight", hitSize, new Vector2(16f, -16f), 45f, Color.clear)
                };
            }

            if (statusText == null)
            {
                statusText = CreateText(root, "CombatStatusText", string.Empty, 22, statusTextColor);
                RectTransform rect = statusText.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(420f, 40f);
                rect.anchoredPosition = new Vector2(0f, 124f);
            }

            ApplyGraphicAlpha(hitMarkerLines, activeHitMarkerColor, 0f);
            uiBuilt = true;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateLine(Transform parent, string name, Vector2 size, Vector2 anchoredPosition, float rotation, Color color)
        {
            Image image = CreateImage(parent, name, color);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            return image;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = content;
            text.raycastTarget = false;
            return text;
        }

        private static void SetStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ApplyGraphicAlpha(Image[] images, Color color, float alpha)
        {
            if (images == null)
            {
                return;
            }

            color.a *= Mathf.Clamp01(alpha);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null)
                {
                    images[i].color = color;
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallRuntimeHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureRuntimeUi();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureRuntimeUi();
        }

        private static void EnsureRuntimeUi()
        {
            if (FindObjectOfType<CombatFeedbackUIController>() != null)
            {
                return;
            }

            if (GameObject.FindGameObjectWithTag("Player") == null)
            {
                return;
            }

            GameObject go = new GameObject("CombatFeedbackUI", typeof(RectTransform));
            go.AddComponent<CombatFeedbackUIController>();
        }
    }
}
