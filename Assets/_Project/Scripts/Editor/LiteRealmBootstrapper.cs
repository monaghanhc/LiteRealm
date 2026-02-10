#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LiteRealm.EditorTools
{
    public static class LiteRealmBootstrapper
    {
        [MenuItem("Tools/LiteRealm/Bootstrap/Setup + Validate (Step 1-5)")]
        public static void SetupAndValidate()
        {
            ProjectDoctorRunner.EnsureRequiredFoldersExist();
            ProjectDoctorRunner.EnsureRequiredTagsAndLayers(out string tagLayerSummary);
            ProjectDoctorRunner.SetInputHandlingMode(InputHandlingMode.Both);

            MainSceneStep5QuestPersistenceBuilder.ApplyStep5();

            DoctorReport report = ProjectDoctorRunner.RunChecks();
            ProjectDoctorRunner.LogReport(report);

            Debug.Log(
                "LiteRealm bootstrap complete.\n" +
                $"Tag/Layer setup: {tagLayerSummary}\n" +
                $"ProjectDoctor errors: {report.ErrorCount}, warnings: {report.WarningCount}");
        }

        [MenuItem("Tools/LiteRealm/Bootstrap/Open Main Scene")]
        public static void OpenMainScene()
        {
            EditorSceneManager.OpenScene(ProjectDoctorConstants.MainScenePath, OpenSceneMode.Single);
        }
    }
}
#endif
