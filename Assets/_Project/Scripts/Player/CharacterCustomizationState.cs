using System;
using UnityEngine;

namespace LiteRealm.Player
{
    [Serializable]
    public struct CharacterCustomizationData
    {
        public int SkinToneIndex;
        public int HairStyleIndex;
        public int HairColorIndex;
        public int OutfitColorIndex;

        public static CharacterCustomizationData Default => new CharacterCustomizationData
        {
            SkinToneIndex = 1,
            HairStyleIndex = 0,
            HairColorIndex = 1,
            OutfitColorIndex = 0
        };
    }

    public static class CharacterCustomizationState
    {
        private const string SkinKey = "LiteRealm.Character.SkinTone";
        private const string HairStyleKey = "LiteRealm.Character.HairStyle";
        private const string HairColorKey = "LiteRealm.Character.HairColor";
        private const string OutfitKey = "LiteRealm.Character.Outfit";

        public static readonly Color[] SkinTones =
        {
            new Color(0.72f, 0.49f, 0.34f, 1f),
            new Color(0.86f, 0.64f, 0.46f, 1f),
            new Color(0.55f, 0.34f, 0.22f, 1f),
            new Color(0.94f, 0.78f, 0.62f, 1f)
        };

        public static readonly Color[] HairColors =
        {
            new Color(0.08f, 0.06f, 0.045f, 1f),
            new Color(0.23f, 0.15f, 0.08f, 1f),
            new Color(0.58f, 0.43f, 0.25f, 1f),
            new Color(0.62f, 0.64f, 0.62f, 1f)
        };

        public static readonly Color[] OutfitColors =
        {
            new Color(0.10f, 0.16f, 0.13f, 1f),
            new Color(0.16f, 0.18f, 0.22f, 1f),
            new Color(0.24f, 0.16f, 0.10f, 1f),
            new Color(0.28f, 0.30f, 0.24f, 1f)
        };

        public static readonly string[] HairStyleNames =
        {
            "Short",
            "Mohawk",
            "Buzz",
            "Hood"
        };

        private static bool loaded;
        private static CharacterCustomizationData current;

        public static CharacterCustomizationData Current
        {
            get
            {
                EnsureLoaded();
                return current;
            }
            set
            {
                current = Clamp(value);
                Save();
            }
        }

        public static CharacterCustomizationData Clamp(CharacterCustomizationData data)
        {
            data.SkinToneIndex = Mathf.Clamp(data.SkinToneIndex, 0, SkinTones.Length - 1);
            data.HairStyleIndex = Mathf.Clamp(data.HairStyleIndex, 0, HairStyleNames.Length - 1);
            data.HairColorIndex = Mathf.Clamp(data.HairColorIndex, 0, HairColors.Length - 1);
            data.OutfitColorIndex = Mathf.Clamp(data.OutfitColorIndex, 0, OutfitColors.Length - 1);
            return data;
        }

        public static Color GetSkinColor(CharacterCustomizationData data)
        {
            return SkinTones[Mathf.Clamp(data.SkinToneIndex, 0, SkinTones.Length - 1)];
        }

        public static Color GetHairColor(CharacterCustomizationData data)
        {
            return HairColors[Mathf.Clamp(data.HairColorIndex, 0, HairColors.Length - 1)];
        }

        public static Color GetOutfitColor(CharacterCustomizationData data)
        {
            return OutfitColors[Mathf.Clamp(data.OutfitColorIndex, 0, OutfitColors.Length - 1)];
        }

        public static string GetHairStyleName(CharacterCustomizationData data)
        {
            return HairStyleNames[Mathf.Clamp(data.HairStyleIndex, 0, HairStyleNames.Length - 1)];
        }

        public static void Load()
        {
            CharacterCustomizationData defaults = CharacterCustomizationData.Default;
            current = new CharacterCustomizationData
            {
                SkinToneIndex = PlayerPrefs.GetInt(SkinKey, defaults.SkinToneIndex),
                HairStyleIndex = PlayerPrefs.GetInt(HairStyleKey, defaults.HairStyleIndex),
                HairColorIndex = PlayerPrefs.GetInt(HairColorKey, defaults.HairColorIndex),
                OutfitColorIndex = PlayerPrefs.GetInt(OutfitKey, defaults.OutfitColorIndex)
            };
            current = Clamp(current);
            loaded = true;
        }

        public static void Save()
        {
            PlayerPrefs.SetInt(SkinKey, current.SkinToneIndex);
            PlayerPrefs.SetInt(HairStyleKey, current.HairStyleIndex);
            PlayerPrefs.SetInt(HairColorKey, current.HairColorIndex);
            PlayerPrefs.SetInt(OutfitKey, current.OutfitColorIndex);
            PlayerPrefs.Save();
            loaded = true;
        }

        private static void EnsureLoaded()
        {
            if (!loaded)
            {
                Load();
            }
        }
    }
}
