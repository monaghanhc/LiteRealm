#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LiteRealm.EditorTools
{
    [InitializeOnLoad]
    public static class LiteRealmAutoPlayBootstrap
    {
        private const string AutoPreparePrefKey = "LiteRealm.AutoPrepareOnEditorLoad";
        private const string SessionRanKey = "LiteRealm.AutoPrepareOnEditorLoad.Ran";

        static LiteRealmAutoPlayBootstrap()
        {
            EditorApplication.delayCall += TryAutoPrepare;
        }

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(AutoPreparePrefKey, true);
            set => EditorPrefs.SetBool(AutoPreparePrefKey, value);
        }

        [MenuItem("Tools/LiteRealm/Bootstrap/Auto Prepare On Editor Load")]
        private static void ToggleAutoPrepare()
        {
            Enabled = !Enabled;
            Menu.SetChecked("Tools/LiteRealm/Bootstrap/Auto Prepare On Editor Load", Enabled);
        }

        [MenuItem("Tools/LiteRealm/Bootstrap/Auto Prepare On Editor Load", true)]
        private static bool ToggleAutoPrepareValidate()
        {
            Menu.SetChecked("Tools/LiteRealm/Bootstrap/Auto Prepare On Editor Load", Enabled);
            return true;
        }

        [MenuItem("Tools/LiteRealm/Bootstrap/Prepare For Play Now")]
        private static void PrepareNow()
        {
            SessionState.SetBool(SessionRanKey, true);
            PrepareProjectForPlay();
        }

        private static void TryAutoPrepare()
        {
            if (!Enabled || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (SessionState.GetBool(SessionRanKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionRanKey, true);
            PrepareProjectForPlay();
        }

        private static void PrepareProjectForPlay()
        {
            if (!File.Exists(ProjectDoctorConstants.MainScenePath))
            {
                Debug.Log("LiteRealm: Main scene missing. Running bootstrap setup.");
                LiteRealmBootstrapper.SetupAndValidate();
            }

            MainMenuSceneBuilder.EnsureMainMenuScene();

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ProjectDoctorConstants.MainMenuScenePath);
            if (sceneAsset == null)
            {
                Debug.LogWarning($"LiteRealm: Could not load {ProjectDoctorConstants.MainMenuScenePath}. Falling back to Main scene.");
                sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ProjectDoctorConstants.MainScenePath);
            }

            if (sceneAsset != null && EditorSceneManager.playModeStartScene != sceneAsset)
            {
                EditorSceneManager.playModeStartScene = sceneAsset;
                Debug.Log($"LiteRealm: Play Mode Start Scene set to {AssetDatabase.GetAssetPath(sceneAsset)}");
            }
        }
    }
}
#endif
