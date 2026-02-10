#if UNITY_EDITOR
using UnityEditor;

namespace LiteRealm.EditorTools
{
    [InitializeOnLoad]
    public static class ProjectDoctorAutoRun
    {
        static ProjectDoctorAutoRun()
        {
            EditorApplication.delayCall += RunIfEnabled;
        }

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(ProjectDoctorConstants.AutoRunPrefKey, true);
            set => EditorPrefs.SetBool(ProjectDoctorConstants.AutoRunPrefKey, value);
        }

        [MenuItem("Tools/LiteRealm/Project Doctor/Auto Run On Domain Reload")]
        private static void ToggleAutoRun()
        {
            Enabled = !Enabled;
            Menu.SetChecked("Tools/LiteRealm/Project Doctor/Auto Run On Domain Reload", Enabled);
        }

        [MenuItem("Tools/LiteRealm/Project Doctor/Auto Run On Domain Reload", true)]
        private static bool ToggleAutoRunValidate()
        {
            Menu.SetChecked("Tools/LiteRealm/Project Doctor/Auto Run On Domain Reload", Enabled);
            return true;
        }

        private static void RunIfEnabled()
        {
            if (!Enabled)
            {
                return;
            }

            DoctorReport report = ProjectDoctorRunner.RunChecks();
            ProjectDoctorRunner.LogReport(report);
        }
    }
}
#endif
