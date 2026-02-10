#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LiteRealm.EditorTools
{
    public class ProjectDoctorWindow : EditorWindow
    {
        private DoctorReport currentReport;
        private Vector2 scroll;
        private readonly List<string> installLog = new List<string>();
        private readonly List<string> quickPlayLog = new List<string>();

        [MenuItem("Tools/LiteRealm/Project Doctor/Open Window")]
        public static void OpenWindow()
        {
            ProjectDoctorWindow window = GetWindow<ProjectDoctorWindow>("Project Doctor");
            window.minSize = new Vector2(700f, 450f);
            window.RunChecks();
            window.Show();
        }

        [MenuItem("Tools/LiteRealm/Project Doctor/Run Checks (Console)")]
        public static void RunChecksConsole()
        {
            DoctorReport report = ProjectDoctorRunner.RunChecks();
            ProjectDoctorRunner.LogReport(report);
        }

        private void OnEnable()
        {
            if (currentReport == null)
            {
                RunChecks();
            }
        }

        private void OnGUI()
        {
            DrawTopButtons();
            DrawStatusSummary();
            DrawResults();
            DrawQuickPlayLog();
            DrawInstallLog();
        }

        private void DrawTopButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Run Checks", GUILayout.Height(28f)))
            {
                RunChecks();
            }

            if (GUILayout.Button("Ensure Folder Structure", GUILayout.Height(28f)))
            {
                ProjectDoctorRunner.EnsureRequiredFoldersExist();
                RunChecks();
            }

            if (GUILayout.Button("Ensure Required Tags/Layers", GUILayout.Height(28f)))
            {
                ProjectDoctorRunner.EnsureRequiredTagsAndLayers(out string summary);
                Debug.Log($"ProjectDoctor: {summary}");
                RunChecks();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Set Input Handling = Both", GUILayout.Height(24f)))
            {
                ProjectDoctorRunner.SetInputHandlingMode(InputHandlingMode.Both);
                RunChecks();
            }

            if (GUILayout.Button("Set Input Handling = New", GUILayout.Height(24f)))
            {
                ProjectDoctorRunner.SetInputHandlingMode(InputHandlingMode.New);
                RunChecks();
            }

            if (GUILayout.Button("Quick Play Check", GUILayout.Height(24f)))
            {
                quickPlayLog.Clear();
                bool ok = ProjectDoctorRunner.RunQuickPlayCheck(out List<string> lines);
                quickPlayLog.Add(ok ? "Quick Play Check: PASS" : "Quick Play Check: FAIL");
                quickPlayLog.AddRange(lines);
            }

            EditorGUI.BeginDisabledGroup(ProjectDoctorRunner.IsInstallInProgress);
            if (GUILayout.Button("Install Missing Required Packages", GUILayout.Height(24f)))
            {
                installLog.Clear();
                ProjectDoctorRunner.InstallMissingRequiredPackages(
                    line =>
                    {
                        installLog.Add(line);
                        Repaint();
                    },
                    () =>
                    {
                        installLog.Add("Done.");
                        RunChecks();
                        Repaint();
                    });
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8f);
        }

        private void DrawStatusSummary()
        {
            if (currentReport == null)
            {
                EditorGUILayout.HelpBox("No report available.", MessageType.Info);
                return;
            }

            MessageType type = MessageType.Info;
            if (currentReport.ErrorCount > 0)
            {
                type = MessageType.Error;
            }
            else if (currentReport.WarningCount > 0)
            {
                type = MessageType.Warning;
            }

            string message = $"ProjectDoctor report generated at {currentReport.GeneratedAtUtc:O}\n"
                           + $"Errors: {currentReport.ErrorCount} | Warnings: {currentReport.WarningCount}";
            EditorGUILayout.HelpBox(message, type);
        }

        private void DrawResults()
        {
            if (currentReport == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Checks", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);

            for (int i = 0; i < currentReport.Results.Count; i++)
            {
                DoctorCheckResult result = currentReport.Results[i];
                if (result == null)
                {
                    continue;
                }

                DrawResult(result);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawResult(DoctorCheckResult result)
        {
            Color oldColor = GUI.color;
            GUI.color = GetColorForResult(result);

            EditorGUILayout.BeginVertical("box");
            GUI.color = oldColor;

            string status = result.Passed ? "PASS" : "FAIL";
            EditorGUILayout.LabelField($"[{status}] {result.Code}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(result.Message, EditorStyles.wordWrappedLabel);

            if (!string.IsNullOrWhiteSpace(result.FixHint))
            {
                EditorGUILayout.LabelField($"Fix: {result.FixHint}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawInstallLog()
        {
            if (installLog.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Package Install Log", EditorStyles.boldLabel);
            for (int i = 0; i < installLog.Count; i++)
            {
                EditorGUILayout.LabelField($"- {installLog[i]}", EditorStyles.miniLabel);
            }
        }

        private void DrawQuickPlayLog()
        {
            if (quickPlayLog.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Quick Play Check", EditorStyles.boldLabel);
            for (int i = 0; i < quickPlayLog.Count; i++)
            {
                EditorGUILayout.LabelField($"- {quickPlayLog[i]}", EditorStyles.miniLabel);
            }
        }

        private void RunChecks()
        {
            currentReport = ProjectDoctorRunner.RunChecks();
        }

        private static Color GetColorForResult(DoctorCheckResult result)
        {
            if (result.Passed)
            {
                return new Color(0.45f, 0.75f, 0.45f);
            }

            return result.Severity == DoctorSeverity.Error
                ? new Color(0.9f, 0.45f, 0.45f)
                : new Color(0.95f, 0.75f, 0.35f);
        }
    }
}
#endif
