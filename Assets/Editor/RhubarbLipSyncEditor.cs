using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

public class RhubarbLipSyncEditor : EditorWindow
{
    public AudioClip audioClip;
    public string rhubarbPath = "";
    public bool openAfter = true;

    [MenuItem("Tools/Rhubarb Lip Sync")]
    public static void ShowWindow() =>
        GetWindow<RhubarbLipSyncEditor>("Rhubarb Lip Sync");

    void OnGUI()
    {
        GUILayout.Label("Rhubarb Lip Sync Generator", EditorStyles.boldLabel);

        rhubarbPath = EditorGUILayout.TextField("Rhubarb Executable", rhubarbPath);
        audioClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", audioClip, typeof(AudioClip), false);
        openAfter = EditorGUILayout.Toggle("Open XML After", openAfter);

        if (GUILayout.Button("Generate XML"))
        {
            if (audioClip == null)
            {
                EditorUtility.DisplayDialog("Error", "Select an AudioClip.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(rhubarbPath) || !File.Exists(rhubarbPath))
            {
                EditorUtility.DisplayDialog("Error", "Set a correct Rhubarb executable path.", "OK");
                return;
            }

            GenerateXML();
        }
    }

    void GenerateXML()
    {
        string clipPath = AssetDatabase.GetAssetPath(audioClip);
        string fullAudioPath = Path.GetFullPath(clipPath);
        string xmlPath = Path.ChangeExtension(fullAudioPath, ".xml");

        string args = $"-f xml -o \"{xmlPath}\" \"{fullAudioPath}\"";

        var psi = new ProcessStartInfo()
        {
            FileName = rhubarbPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var proc = Process.Start(psi);
        proc.WaitForExit();

        if (!File.Exists(xmlPath))
        {
            UnityEngine.Debug.LogError("Rhubarb failed.");
            EditorUtility.DisplayDialog("Error", "Rhubarb failed. Check the console.", "OK");
            return;
        }

        AssetDatabase.Refresh();

        if (openAfter)
            EditorUtility.RevealInFinder(xmlPath);

        EditorUtility.DisplayDialog("Success", "XML generated:\n" + xmlPath, "OK");
    }
}
