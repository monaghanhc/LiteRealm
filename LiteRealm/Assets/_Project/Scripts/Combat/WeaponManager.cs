using System;
using System.Collections.Generic;
using LiteRealm.CameraSystem;
using LiteRealm.Core;
using LiteRealm.Player;
using UnityEngine;

namespace LiteRealm.Combat
{
    public class WeaponManager : MonoBehaviour
    {
        [SerializeField] private List<WeaponBase> weapons = new List<WeaponBase>();
        [SerializeField] private int startingWeaponIndex;

        [Header("Input")]
        [SerializeField] private KeyCode reloadKey = KeyCode.R;
        [SerializeField] private KeyCode fireFallbackKey = KeyCode.Mouse0;
        [SerializeField] private ExplorationInput explorationInput;

        [Header("References")]
        [SerializeField] private Camera aimCamera;
        [SerializeField] private PlayerCameraController cameraController;
        [SerializeField] private GameEventHub eventHub;
        [SerializeField] private LayerMask hitMask = ~0;

        public event Action<WeaponBase> ActiveWeaponChanged;
        public event Action<int, int> AmmoChanged;

        public WeaponBase ActiveWeapon { get; private set; }

        private void Awake()
        {
            if (explorationInput == null)
            {
                explorationInput = GetComponent<ExplorationInput>();
            }

            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }

            if (cameraController == null && aimCamera != null)
            {
                cameraController = aimCamera.GetComponent<PlayerCameraController>();
            }

            if (weapons.Count == 0)
            {
                WeaponBase[] found = GetComponentsInChildren<WeaponBase>(true);
                weapons.AddRange(found);
            }

            for (int i = 0; i < weapons.Count; i++)
            {
                WeaponBase weapon = weapons[i];
                if (weapon == null)
                {
                    continue;
                }

                weapon.AmmoChanged += OnWeaponAmmoChanged;
                weapon.gameObject.SetActive(false);
            }

            Equip(Mathf.Clamp(startingWeaponIndex, 0, Mathf.Max(0, weapons.Count - 1)));
        }

        private void OnDestroy()
        {
            for (int i = 0; i < weapons.Count; i++)
            {
                WeaponBase weapon = weapons[i];
                if (weapon == null)
                {
                    continue;
                }

                weapon.AmmoChanged -= OnWeaponAmmoChanged;
            }
        }

        private void Update()
        {
            if (ActiveWeapon == null)
            {
                return;
            }

            HandleSwapInput();
            HandleCombatInput();
        }

        private void HandleSwapInput()
        {
            if (weapons.Count <= 1)
            {
                return;
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
            {
                EquipRelative(1);
            }
            else if (scroll < 0f)
            {
                EquipRelative(-1);
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Equip(0);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Equip(1);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Equip(2);
            }
        }

        private void HandleCombatInput()
        {
            if (ReadReloadPressedThisFrame())
            {
                TryReloadActiveWeapon();
            }

            if (!ReadFireHeld())
            {
                return;
            }

            TryFireActiveWeapon();
        }

        public bool TryFireActiveWeapon()
        {
            if (ActiveWeapon == null)
            {
                return false;
            }

            WeaponFireContext fireContext = new WeaponFireContext
            {
                AimCamera = aimCamera,
                HitMask = hitMask,
                Instigator = gameObject,
                EventHub = eventHub
            };

            bool fired = ActiveWeapon.TryFire(fireContext);
            if (!fired)
            {
                return false;
            }

            if (cameraController != null)
            {
                float yawKick = UnityEngine.Random.Range(-ActiveWeapon.RecoilYaw, ActiveWeapon.RecoilYaw);
                cameraController.AddRecoil(ActiveWeapon.RecoilPitch, yawKick);
            }

            AmmoChanged?.Invoke(ActiveWeapon.CurrentAmmo, ActiveWeapon.MagazineSize);
            return true;
        }

        public bool TryReloadActiveWeapon()
        {
            if (ActiveWeapon == null)
            {
                return false;
            }

            return ActiveWeapon.TryStartReload(this);
        }

        public void Equip(int index)
        {
            if (index < 0 || index >= weapons.Count)
            {
                return;
            }

            if (ActiveWeapon != null)
            {
                ActiveWeapon.gameObject.SetActive(false);
            }

            ActiveWeapon = weapons[index];
            if (ActiveWeapon != null)
            {
                ActiveWeapon.gameObject.SetActive(true);
                ActiveWeaponChanged?.Invoke(ActiveWeapon);
                AmmoChanged?.Invoke(ActiveWeapon.CurrentAmmo, ActiveWeapon.MagazineSize);
            }
        }

        public void EquipRelative(int direction)
        {
            if (weapons.Count == 0)
            {
                return;
            }

            int currentIndex = weapons.IndexOf(ActiveWeapon);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int newIndex = (currentIndex + direction + weapons.Count) % weapons.Count;
            Equip(newIndex);
        }

        private void OnWeaponAmmoChanged(WeaponBase weapon)
        {
            if (weapon == ActiveWeapon)
            {
                AmmoChanged?.Invoke(weapon.CurrentAmmo, weapon.MagazineSize);
            }
        }

        private bool ReadReloadPressedThisFrame()
        {
            if (explorationInput != null)
            {
                return explorationInput.ReloadPressedThisFrame();
            }

            return Input.GetKeyDown(reloadKey);
        }

        private bool ReadFireHeld()
        {
            if (explorationInput != null)
            {
                return explorationInput.FireHeld();
            }

            return Input.GetKey(fireFallbackKey);
        }
    }
}