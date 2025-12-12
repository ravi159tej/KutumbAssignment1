using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;

namespace PuppetFace
{
	[CustomEditor(typeof(PerformanceCapture))]
	public class PerformanceCaptureEditor : Editor
	{
		public PerformanceCapture pc;
		public bool Open = false;
		public bool OpenCalibrating = false;

		public bool Oh = false;
		public bool Corners = false;		
		public bool HeadRot = false;
		public bool Jaw = false;
		public bool EyeBrows = false;
        private static string _puppetFacePath;

        void OnEnable()
		{
            _puppetFacePath = "Assets" + RecursivelyFindFolderPath().Substring(Application.dataPath.Length);

            pc = (PerformanceCapture)target;
			if (pc.faces == null)
			{
				TextAsset t = AssetDatabase.LoadAssetAtPath(_puppetFacePath +"/Scripts/faceDefaults.xml", typeof(TextAsset)) as TextAsset;
				pc.faces = t;
			}
			if (pc.eyes == null)
			{
				TextAsset t = AssetDatabase.LoadAssetAtPath(_puppetFacePath +"/Scripts/eyeDefaults.xml", typeof(TextAsset)) as TextAsset;
				pc.eyes = t;
			}
			if (pc.shapes == null)
			{
				TextAsset t = AssetDatabase.LoadAssetAtPath(_puppetFacePath +"/Scripts/shape_predictor_68_face_landmarks.bytes", typeof(TextAsset)) as TextAsset;
				pc.shapes = t;
			}
			if (pc.Surface == null)
			{
				GameObject rawImageGO = new GameObject("Performance Feed");
				GameObject newCanvas = new GameObject("Performance Canvas");
				rawImageGO.AddComponent<RawImage>();
				Canvas canv = newCanvas.AddComponent<Canvas>();
				canv.renderMode = RenderMode.ScreenSpaceCamera;
				canv.worldCamera = Camera.main;
				rawImageGO.transform.SetParent(newCanvas.transform);
				rawImageGO.GetComponent<RectTransform>().anchoredPosition3D = Vector3.zero;
				rawImageGO.GetComponent<RectTransform>().localScale = Vector3.one*.5f;
				rawImageGO.transform.localRotation = Quaternion.identity;
				
				pc.Surface = rawImageGO;
			}
			if (pc.Skin == null && pc.GetComponent<SkinnedMeshRenderer>()!=null)
			{
				pc.Skin = pc.GetComponent<SkinnedMeshRenderer>();
			}


		}
		public string[] GetArrayBlendShapes(SkinnedMeshRenderer skin)
		{
			Mesh m = skin.sharedMesh;

			string[] arr;
			arr = new string[m.blendShapeCount + 1];

			for (int i = 0; i < m.blendShapeCount; i++)
			{
				arr[i] = m.GetBlendShapeName(i);
			}
			arr[m.blendShapeCount] = "Not Set";
			return arr;
		}
		public override void OnInspectorGUI()
		{
			
			GUILayout.Space(10);
			WebCamDevice[] devices = WebCamTexture.devices;
			string[] deviceNames = new string[devices.Length];
			for (int i = 0; i < devices.Length; i++)
			{
				deviceNames[i] = devices[i].name;
			}

			pc.DeviceIndex = EditorGUILayout.Popup(pc.DeviceIndex, deviceNames);

			string[] micDevices = Microphone.devices;			
			pc.MicrophoneDeviceIndex = EditorGUILayout.Popup(pc.MicrophoneDeviceIndex, micDevices);
			if(micDevices.Length>0)
				Microphone.GetDeviceCaps(micDevices[pc.DeviceIndex], out pc.MinFrequency, out pc.MaxFrequency);
			if (pc.MinFrequency == 0 && pc.MaxFrequency == 0)
			{
				pc.MaxFrequency = 44100;
			}

			string LogoPath = _puppetFacePath +"/Textures/GUI/PuppetFaceLogo.png";
			Texture LogoAsset = AssetDatabase.LoadAssetAtPath(LogoPath, typeof(Texture)) as Texture;
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical();

			pc.Skin = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skin", pc.Skin, typeof(SkinnedMeshRenderer), true);
			pc.Anim = (Animator)EditorGUILayout.ObjectField("Animator", pc.Anim, typeof(Animator), true);
			GUILayout.EndVertical();

			GUILayout.Label( LogoAsset, GUILayout.Width(40), GUILayout.Height(40));
			GUILayout.EndHorizontal();



			if (!pc.CalibrateBase)
			{
				if (GUILayout.Button("Calibrate Default Pose", GUILayout.Width(100), GUILayout.Height(15), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true)))
				{
					pc.CalibrateBase = true;
				}
			}
			else
			{
				GUI.color = Color.green;
				if (GUILayout.Button("Stop Calibrating Default Pose", GUILayout.Width(100), GUILayout.Height(15), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true)))
				{
					pc.CalibrateBase = false;
				}
				GUI.color = Color.white;
			}

			pc.HeadRot = EditorGUILayout.Toggle("Head Rotation", pc.HeadRot);
			if (pc.HeadRot)
			{
				GUILayout.BeginVertical(EditorStyles.helpBox);

				HeadRot = EditorGUILayout.Foldout(HeadRot, "Head Rotation");
				if (HeadRot)
				{
					EditorGUI.BeginChangeCheck();
					pc.RotBone = (Transform)EditorGUILayout.ObjectField("Head Bone", pc.RotBone, typeof(Transform), true);
					if (EditorGUI.EndChangeCheck())
					{
						pc.HeadForwardBaked = pc.RotBone.rotation;
					}
					pc.RotationLimits = EditorGUILayout.Vector3Field("Rotation Amount", pc.RotationLimits);
					EditorGUI.BeginChangeCheck();
					pc.HeadSmoothing = EditorGUILayout.IntSlider(new GUIContent("Smoothing"), pc.HeadSmoothing, 1, 20);
					if (EditorGUI.EndChangeCheck())
					{
						pc.rotationAverage.Clear();
					}
				}
				GUILayout.EndVertical();

			}
			

			pc.Open = EditorGUILayout.Toggle("Open", pc.Open);

			if (pc.Open && pc.Skin != null)
			{
				GUILayout.BeginVertical(EditorStyles.helpBox);

				Open = EditorGUILayout.Foldout(Open, "Open");
				if (Open)
				{
					pc.OpenMinMax = EditorGUILayout.Vector2Field("Open Min Max", pc.OpenMinMax);
										
					if (!pc.CalibrateOpen)
					{
						if (GUILayout.Button("Calibrate Open Pose", GUILayout.Width(100), GUILayout.Height(15), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true)))
						{
							pc.CalibrateOpen = true;
						}
					}
					else
					{
						GUI.color = Color.green;
						if (GUILayout.Button("Stop Calibrating Open Pose", GUILayout.Width(100), GUILayout.Height(15), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true)))
						{
							pc.CalibrateOpen = false;
						}
						GUI.color = Color.white;
					}
					pc.OpenMouthIndex = EditorGUILayout.Popup("Open Blend Shape", pc.OpenMouthIndex, GetArrayBlendShapes(pc.Skin));
					EditorGUI.BeginChangeCheck();
					pc.JawBone = (Transform)EditorGUILayout.ObjectField("Jaw Bone", pc.JawBone, typeof(Transform), true);
					if (EditorGUI.EndChangeCheck())
					{
						pc.JawDefault = pc.JawBone.localRotation;
					}

					if (pc.JawBone != null)
					{
						if (GUILayout.Button("Set Open Pose", GUILayout.Width(100), GUILayout.Height(15), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true)))
						{
							pc.JawOpen = pc.JawBone.localRotation;
							pc.JawBone.localRotation = pc.JawDefault;
						}
					}
					EditorGUI.BeginChangeCheck();
					pc.OpenSmoothing = EditorGUILayout.IntSlider(new GUIContent("Smoothing"), pc.OpenSmoothing, 1, 20);
					if (EditorGUI.EndChangeCheck())
					{
						pc.OpenBlendFrameCount = pc.OpenSmoothing;
						pc.OpenMinMaxAverage.Clear();
					}
				}
				GUILayout.EndVertical();


			}


			pc.Oh = EditorGUILayout.Toggle("Oh", pc.Oh);

			if (pc.Oh && pc.Skin != null)
			{
				GUILayout.BeginVertical(EditorStyles.helpBox);

				Oh = EditorGUILayout.Foldout(Oh, "Oh");
				if (Oh)
				{
					pc.OhMinMax = EditorGUILayout.Vector2Field("Oh Min Max", pc.OhMinMax);
					
					if (!pc.CalibrateOh)
					{
						if (GUILayout.Button("Calibrate Oh Pose", GUILayout.Width(100), GUILayout.Height(15), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true)))
						{
							pc.CalibrateOh = true;
						}
					}
					else
					{
						GUI.color = Color.green;
						if (GUILayout.Button("Stop Calibrating Oh Pose", GUILayout.Width(100), GUILayout.Height(15), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true)))
						{
							pc.CalibrateOh = false;
						}
						GUI.color = Color.white;
					}
					pc.OhMouthIndex = EditorGUILayout.Popup("Oh Blend Shape", pc.OhMouthIndex, GetArrayBlendShapes(pc.Skin));
					EditorGUI.BeginChangeCheck();
					pc.OhSmoothing = EditorGUILayout.IntSlider(new GUIContent("Smoothing"), pc.OhSmoothing, 1, 20);
					if (EditorGUI.EndChangeCheck())
					{
						pc.OhMinMaxAverage.Clear();
					}

				}
				GUILayout.EndVertical();

			}

			CreatePoseGUI("Smile", ref pc.Corners, ref Corners, ref pc.CornersMinMax, ref pc.CalibrateCorners, ref pc.CornersMouthIndex, ref pc.CornersSmoothing,ref pc.CornersMinMaxAverage);

			CreatePoseGUI("Eyebrows", ref pc.EyeBrows, ref EyeBrows, ref pc.EyeBrowsMinMax, ref pc.CalibrateEyeBrows, ref pc.EyeBrowsIndex, ref pc.EyeBrowsSmoothing, ref pc.EyeBrowsMinMaxAverage);

			if (!pc.isRecording)
			{
				if (GUILayout.Button("Capture", GUILayout.Width(100), GUILayout.Height(25), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true)))
				{
					pc.isRecording = true;
					pc.StartRecording();
				}
			}
			else
			{
				GUI.color = Color.green;
				if (GUILayout.Button("Stop Capture", GUILayout.Width(100), GUILayout.Height(25), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true)))
				{
					pc.isRecording = false;
					pc.EndRecording();

				}
				GUI.color = Color.white;
			}
						

		}
		public void CreatePoseGUI(string name,ref bool toggle, ref bool toggleFoldout, ref Vector2 minMax, ref bool Calibrate, ref int BlendShapeIndex, ref int Smoothing, ref Queue<float> minMaxAverage)
		{
			toggle = EditorGUILayout.Toggle(name, toggle);
			if (toggle && pc.Skin != null)
			{
				GUILayout.BeginVertical(EditorStyles.helpBox);
				toggleFoldout = EditorGUILayout.Foldout(toggleFoldout, name);
				if (toggleFoldout)
				{

					minMax = EditorGUILayout.Vector2Field(name +" MinMax", minMax);

					if (!Calibrate)
					{
						if (GUILayout.Button("Calibrate " + name + " Pose", GUILayout.Width(100), GUILayout.Height(15), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true)))
						{
							Calibrate = true;
						}
					}
					else
					{
						GUI.color = Color.green;
						if (GUILayout.Button("Stop Calibrating " + name + " Pose", GUILayout.Width(100), GUILayout.Height(15), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true)))
						{
							Calibrate = false;
						}
						GUI.color = Color.white;
					}
					BlendShapeIndex = EditorGUILayout.Popup(name + " Blend Shape", BlendShapeIndex, GetArrayBlendShapes(pc.Skin));
					EditorGUI.BeginChangeCheck();
					Smoothing = EditorGUILayout.IntSlider(new GUIContent("Smoothing"), Smoothing, 1, 20);
					if (EditorGUI.EndChangeCheck())
					{
						minMaxAverage.Clear();
					}
				}
				GUILayout.EndVertical();
			}
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
