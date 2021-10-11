using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.Diagnostics;

[CustomEditor(typeof(NetworkScript))]
public class NetworkEditor : Editor
{
    BuildPlayerOptions buildPlayerOptions;
    BuildReport report;
    BuildSummary summary = new BuildSummary();

    static string pathname;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Build"))
        {
            pathname = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");

            buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.locationPathName = pathname + "/BuildGame.exe";
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            buildPlayerOptions.options = BuildOptions.Development | BuildOptions.EnableDeepProfilingSupport;

            report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                UnityEngine.Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
            }

            if (summary.result == BuildResult.Failed)
            {
                UnityEngine.Debug.Log("Build failed");
            }
        }

        if (GUILayout.Button("Play"))
        {
            if (pathname == null)
            {
                UnityEngine.Debug.Log("The <b>pathname</b> was null, so we couldn't start anything.");
                return;
            }

            Process process = new Process();
            process.StartInfo.FileName = pathname + "/BuildGame.exe";
            process.Start();
        }
        GUILayout.EndHorizontal();
    }
}
