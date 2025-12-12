using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
namespace PuppetFace
{

	[CustomEditor(typeof(LipSync))]
	public class LipSyncEditor : Editor
	{
		LipSync lipsync;
        private static string _puppetFacePath;
        private static string _puppetFacePathFull;

        void OnEnable()
        {
            _puppetFacePath = "Assets" + RecursivelyFindFolderPath().Substring(Application.dataPath.Length);
            _puppetFacePathFull = RecursivelyFindFolderPath();
            //Debug.Log("path " + _puppetFacePath);
            //Debug.Log("full path " + _puppetFacePathFull);

            lipsync = (LipSync)target;
			lipsync.Skin = lipsync.GetComponent<SkinnedMeshRenderer>();
			if (lipsync.Skin == null)
				lipsync.Skin = lipsync.gameObject.AddComponent<SkinnedMeshRenderer>();
		}

		// Update is called once per frame
		public override void OnInspectorGUI()
		{
			GUILayout.Space(10);
            
			//string RhubarbPath = Application.dataPath+"/PuppetFace/Tools/LipSync/rhubarb/";
            string RhubarbPath = _puppetFacePathFull + "/Tools/LipSync/rhubarb/";

            if (!File.Exists(RhubarbPath+ "rhubarb.exe"))
			{
				GUILayout.BeginVertical();
				if (GUILayout.Button("Download Lipsync Converter (Rhubarb)"))
				{
					if (EditorUtility.DisplayDialog("Downloading Rhubarb",
				   "This will download Rhubarb LipSync Extractor. Please extract it into the folder:\n Assets\\PuppetFace\\Tools\\LipSync\\rhubarb", "Yes", "Cancel"))
					{
						Application.OpenURL("https://wix.anyfileapp.net/dl?id=553246736447566b58313958713474514f43544c754355524c495a6e49325742506d6d6f57356d6c4d66366a4258713576526d5146632f70774b34333830424d72797945342f4b4b526e6775684b72324a33635a4d773d3d");
						Debug.Log("Please extract it into the folder:");
						Debug.Log(RhubarbPath);

						EditorUtility.RevealInFinder(RhubarbPath);
					}
				}
				GUILayout.EndVertical();

			}
			else
			{
				string LogoPath = _puppetFacePath +"/Textures/GUI/PuppetFaceLogo.png";
				Texture LogoAsset = AssetDatabase.LoadAssetAtPath(LogoPath, typeof(Texture)) as Texture;
				GUILayout.BeginHorizontal();

				GUILayout.BeginVertical();

				if (GUILayout.Button("Launch Editor"))
				{
					EditorWindow.GetWindow(typeof(PuppetFaceEditor), false, "Lip Sync Editor");
				}
				lipsync.Skin = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skinned Mesh", lipsync.Skin, typeof(SkinnedMeshRenderer), true);

				GUILayout.EndVertical();

				GUILayout.Label(LogoAsset, GUILayout.Width(40), GUILayout.Height(40));
				GUILayout.EndHorizontal();


				GUILayout.Space(10);

				if (lipsync.LipSyncFiles != null)
				{
					string[] lipSyncFileNames = new string[lipsync.LipSyncFiles.Length];
					for (int i = 0; i < lipSyncFileNames.Length; i++)
					{
						if (lipsync.LipSyncFiles[i] != null)
							lipSyncFileNames[i] = lipsync.LipSyncFiles[i].name;
					}
					lipsync.LipSyncIndex = EditorGUILayout.Popup("Lip Sync", lipsync.LipSyncIndex, lipSyncFileNames);
				}
				serializedObject.Update();
				GUILayout.BeginVertical(EditorStyles.helpBox);

				SerializedProperty LipSyncFiles = serializedObject.FindProperty("LipSyncFiles");
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(LipSyncFiles, true);
				if (EditorGUI.EndChangeCheck())
					serializedObject.ApplyModifiedProperties();

				SerializedProperty AudioClips = serializedObject.FindProperty("AudioClips");
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(AudioClips, true);
				if (EditorGUI.EndChangeCheck())
					serializedObject.ApplyModifiedProperties();


				GUILayout.EndVertical();


				lipsync.Strength = EditorGUILayout.Slider("Strength", lipsync.Strength, 0f, 1f);
				lipsync.PlayOnAwake = EditorGUILayout.Toggle("Play On Awake", lipsync.PlayOnAwake);
				lipsync.PlayAll = EditorGUILayout.Toggle("Play All", lipsync.PlayAll);
				lipsync.Repeat = EditorGUILayout.Toggle("Repeat", lipsync.Repeat);
				lipsync.TimeVal = EditorGUILayout.FloatField("Time", lipsync.TimeVal);


				if (lipsync.IsPlaying)
				{
					if (GUILayout.Button("Stop"))
					{
						Stop();
					}
				}
				else
				{
					if (GUILayout.Button("Play"))
					{
						Play();
					}

				}
				GUILayout.BeginVertical(EditorStyles.helpBox);

				SerializedProperty FaceBones = serializedObject.FindProperty("FaceBones");
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(FaceBones, true);
				if (EditorGUI.EndChangeCheck())
					serializedObject.ApplyModifiedProperties();

				SerializedProperty OtherLipSyncs = serializedObject.FindProperty("LipSyncs");
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(OtherLipSyncs, true);
				if (EditorGUI.EndChangeCheck())
					serializedObject.ApplyModifiedProperties();

				GUILayout.EndVertical();
			}
		}
		void Play()
		{
			lipsync.Play(lipsync.LipSyncIndex, lipsync.TimeVal); 
		}
		void Stop()
		{
			lipsync.Stop();
		}
        private static string RecursivelyFindFolderPath()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);
            DirectoryInfo[] dirInfos = directoryInfo.GetDirectories("*", SearchOption.AllDirectories);
            foreach (DirectoryInfo d in dirInfos)
            {
                if (d.Name == "PuppetFace" && d.Parent.Name != "Gizmos")
                    return d.FullName;
            }
            return "";
        }
    }
}
