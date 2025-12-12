using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Compilation;
using System;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
//using SFB;

namespace PuppetFace
{
	public class PuppetFaceEditor : EditorWindow
	{
		public Vector2 scrollPosition = Vector2.zero;
		public static List<string> blendShapeTextures;
		public static SkinnedMeshRenderer CurrentSkinnedModel;
		public static GameObject CurrentBlendShapeModel;
		public static int CurrentBlendShapeIndex = -1;
		public static int CurrentSelectedPhonemeMarker = -1;

		public static float TimeVal = 0;
		public static bool Playing = false;
		public static float ShortStop = -1f;


		public static int WaveFormWidth = 1500;
		public static int WaveFormPosX = 0;
		public static int WaveFormPosY = 0;
		public static int WaveFormOffsetX = 0;

		private List<PhonemeMarker> _phonemeMarkers;
		private GUIStyle _phonemeMarkerStyle;
		private GUIStyle _defaultStyle;

		private static PuppetFaceEditor _puppetFaceEditor;
		private static AudioSource AudioSourceLoaded;
		public static AudioClip AudioClipLoaded;
		private static LipSync LipSyncLoaded;
		private static LipSync[] LipSyncInScene;
		private static string[] LipSyncInSceneString;

		private static int LipSyncSelected;

		private static string[] AudioClipNames;

		float editorDeltaTime = 0f;
		float lastTimeSinceStartup = 0f;

		private string[] options;
		private static List<int> selectedOptions;
		private static int flags = -1;
		private static int BlendShapeType = 1;
		private static int BonePoseIndex = -1;

        public static int AudioClipsToConvert = 0;
        public static List<string> filesToConvert = new List<string>();
        public static string[] waveNames = new string[0];

        private static string _puppetFacePath;

        [MenuItem("Window/Puppet Face/Lip Sync Editor")]
		public static void ShowWindow()
		{

            _puppetFaceEditor = (PuppetFaceEditor)EditorWindow.GetWindow(typeof(PuppetFaceEditor));
			_puppetFaceEditor.titleContent = new GUIContent();
			_puppetFaceEditor.titleContent.text = "Lip Sync Editor";
			_puppetFaceEditor.position = new Rect(100, 200, 1000, 600);
			_puppetFaceEditor.name = "Lip Sync Editor";

			blendShapeTextures = new List<string>();
			GetEditorData();
			if (blendShapeTextures.Count == 0)
			{
				for (int i = 0; i < 10; i++)
				{
					string faceShape = (_puppetFacePath +"/Textures/GUI/img/lisa" + i + ".png");
					if (!blendShapeTextures.Contains(faceShape))
						blendShapeTextures.Add(faceShape);
				}
				SetEditorData();
			}

			_puppetFaceEditor.Show();
		}
		[MenuItem("Window/Puppet Face/Component/Lip Sync")]
		public static void AddLipSync()
		{
			Selection.activeGameObject.AddComponent<LipSync>();
		}
		[MenuItem("Window/Puppet Face/Component/Blend Shape Manager")]
		public static void AddBlendShapeManager()
		{
			Selection.activeGameObject.AddComponent<BlendShapeManager>();
		}
		[MenuItem("Window/Puppet Face/Component/Performance Capture")]
		public static void AddPerformanceCapture()
		{
			Selection.activeGameObject.AddComponent<PerformanceCapture>();
		}
		[MenuItem("Window/Puppet Face/Component/Eye Motion")]
		public static void AddEyeMotion()
		{
			Selection.activeGameObject.AddComponent<EyeMotion>();
		}
		
		private void OnEnable()
		{
            _puppetFacePath = "Assets" + RecursivelyFindFolderPath().Substring(Application.dataPath.Length);

            _phonemeMarkerStyle = new GUIStyle();
			_defaultStyle = new GUIStyle();
			string phonemeMarkerPath = _puppetFacePath + "/Textures/GUI/phonemeMarker.png";
			_phonemeMarkerStyle.normal.background = EditorGUIUtility.Load(phonemeMarkerPath) as Texture2D;
			_phonemeMarkerStyle.normal.textColor = Color.white;
			_defaultStyle.normal.textColor = Color.white;


			_phonemeMarkerStyle.border = new RectOffset(12, 12, 12, 12);
			CheckForLipSyncs();
		}

		private static void CheckForLipSyncs()
		{
			LipSyncInScene = FindObjectsOfType<LipSync>();
			LipSyncInSceneString = new string[LipSyncInScene.Length];
			for (int i = 0; i < LipSyncInScene.Length; i++)
			{
				LipSyncInSceneString[i] = LipSyncInScene[i].gameObject.name;
			}
		}

		void OnGUI()
		{
			if (_puppetFaceEditor == null)
			{
				_puppetFaceEditor = (PuppetFaceEditor)EditorWindow.GetWindow(typeof(PuppetFaceEditor));
				_puppetFaceEditor.name = "Lip Sync Editor";
				RefreshNodes(Vector2.zero);
			}

			int widthPad = 0;
			int heightPad = 20;
			Color defaultColor = GUI.backgroundColor;
			string BGPath = _puppetFacePath + "/Textures/GUI/BG.png";
			string BGDarkPath = _puppetFacePath + "/Textures/GUI/BGDark.png";

			Texture BGAsset = AssetDatabase.LoadAssetAtPath(BGPath, typeof(Texture)) as Texture;
			Texture BGDarkAsset = AssetDatabase.LoadAssetAtPath(BGDarkPath, typeof(Texture)) as Texture;

			GUI.DrawTexture(new Rect(0, 0, 9999, 9999), BGAsset, ScaleMode.StretchToFill);
			GUI.backgroundColor = defaultColor;
			GetEditorData();

			GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 50), EditorStyles.helpBox);
			GUILayout.EndArea();
			GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, 70), EditorStyles.helpBox);
			GUILayout.Space(25);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();


			bool notYetLoaded = false;

			if (!notYetLoaded)
			{
				CheckForLipSyncs();
				if (LipSyncInScene.Length > 0)
				{
					LipSyncSelected = Mathf.Clamp(LipSyncSelected, 0, LipSyncInScene.Length - 1);
					notYetLoaded = CheckForSelectedLipSync(LipSyncInScene[LipSyncSelected].gameObject, notYetLoaded);
				}

			}
            //Debug.Log(waveNames.Length);

            if (waveNames.Length == 0)
            {
                SetAudioClipData();
            }

            /*EditorGUI.BeginChangeCheck();

            AudioClipsToConvert = EditorGUILayout.MaskField(AudioClipsToConvert, waveNames);

            if (EditorGUI.EndChangeCheck())
            {
                SetAudioClipData();
            }*/
            if (GUILayout.Button("Convert Audio (File)"))
			{
				ConvertAudioToLipSyncFile();
			}
            if (GUILayout.Button("Convert Audio (Folder)"))
            {
                ConvertAudioToLipSyncFolder();
            }
            if (GUILayout.Button("Reload"))
			{
				LipSyncLoaded.Initialized = false;
				ReloadNodes(Vector2.zero);
			}

			if (GUILayout.Button("Save"))
			{
				SavePhonemes();
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.EndArea();

			GUILayout.BeginArea(new Rect(11, 0, Screen.width - 22, Screen.height - 20));

			if (_phonemeMarkers == null)
			{
				_phonemeMarkers = new List<PhonemeMarker>();
			}

			if (Selection.activeGameObject)
			{
				MeshRenderer mr = Selection.activeGameObject.GetComponent<MeshRenderer>();
				MeshModifier mm = Selection.activeGameObject.GetComponent<MeshModifier>();
				SkinnedMeshRenderer smr = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();
				if (mm == null)
					CurrentSkinnedModel = smr;

				if (Selection.activeGameObject.GetComponent<AudioSource>() && Selection.activeGameObject.GetComponent<LipSync>())
				{
					notYetLoaded = CheckForSelectedLipSync(Selection.activeGameObject, notYetLoaded);
				}

				else if (mr != null && mm == null)
				{
					AudioSourceLoaded = null;
					LipSyncLoaded = null;
				}
			}

			if (LipSyncLoaded != null)
			{                
                if (AudioSourceLoaded != null && AudioClipLoaded != null && AudioClipNames != null && AudioClipNames.Length > 0)
				{
					heightPad += 50;

					GUI.DrawTexture(new Rect(widthPad, heightPad, Screen.width - 10, 160), BGDarkAsset, ScaleMode.StretchToFill);

				}

				GUILayout.BeginArea(new Rect(0, 10, Screen.width - 20, 20), EditorStyles.helpBox);

				GUILayout.BeginHorizontal();
				if (LipSyncInScene.Length > 0)
				{
					EditorGUI.BeginChangeCheck();

					LipSyncSelected = EditorGUILayout.Popup(LipSyncSelected, LipSyncInSceneString);

					if (EditorGUI.EndChangeCheck())
					{
						CheckForLipSyncs();
						if (LipSyncInScene.Length > 0)
						{
							LipSyncSelected = Mathf.Clamp(LipSyncSelected, 0, LipSyncInScene.Length - 1);
							Selection.activeGameObject = LipSyncInScene[LipSyncSelected].gameObject;
							notYetLoaded = CheckForSelectedLipSync(LipSyncInScene[LipSyncSelected].gameObject, notYetLoaded);
							LipSyncLoaded = LipSyncInScene[LipSyncSelected];
						}

					}
				}

				if (LipSyncLoaded.LipSyncFiles != null && LipSyncLoaded.LipSyncFiles.Length > 0)
				{
					EditorGUI.BeginChangeCheck();
					LipSyncLoaded.LipSyncIndex = EditorGUILayout.Popup(LipSyncLoaded.LipSyncIndex, AudioClipNames);
					if (EditorGUI.EndChangeCheck())
					{
						LipSyncLoaded.SetIndex(LipSyncLoaded.LipSyncIndex);
						AudioClipLoaded = LipSyncLoaded.AudioClips[LipSyncLoaded.LipSyncIndex];
						AudioSourceLoaded.clip = AudioClipLoaded;
						ReloadNodes(Vector2.zero);
						DrawNodes();
					}
                    if(LipSyncLoaded.NewAudioAdded)
                    {
                        ReloadNodes(Vector2.zero);
                        LipSyncLoaded.NewAudioAdded = false;
                    }
                }

				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.EndArea();

				if (AudioSourceLoaded != null && AudioClipLoaded != null && AudioClipNames != null && AudioClipNames.Length > 0)
				{
					if (AudioSourceLoaded != null && AudioClipLoaded != null && AudioClipNames != null && AudioClipNames.Length > 0)
					{
						EditorGUI.BeginChangeCheck();
						TimeVal = GUI.HorizontalSlider(new Rect(new Vector2(WaveFormPosX, heightPad + 20), new Vector2(WaveFormWidth, 100)), TimeVal, 0, AudioClipLoaded.length);
						if (EditorGUI.EndChangeCheck())
						{
							if (!Application.isPlaying)
							{
								if (AudioSourceLoaded.clip.length > TimeVal)
								{
									SetLipSync();
									AudioSourceLoaded.time = TimeVal;
									ShortStop = .2f;
									AudioSourceLoaded.Play();
								}
							}

						}
					}

					WaveFormPosY = heightPad;
					

					Texture PlayButtonAsset = AssetDatabase.LoadAssetAtPath(_puppetFacePath +"/Textures/GUI/PlayButton.png", typeof(Texture)) as Texture;
					Texture PlayButtonOnAsset = AssetDatabase.LoadAssetAtPath(_puppetFacePath +"/Textures/GUI/PlayButtonOn.png", typeof(Texture)) as Texture;
					Texture StopButtonAsset = AssetDatabase.LoadAssetAtPath(_puppetFacePath +"/Textures/GUI/StopButton.png", typeof(Texture)) as Texture;
					Texture StopButtonOnAsset = AssetDatabase.LoadAssetAtPath(_puppetFacePath +"/Textures/GUI/StopButtonOn.png", typeof(Texture)) as Texture;

					GUIStyle buttonStyle = new GUIStyle();
					buttonStyle.stretchWidth = true;
					buttonStyle.stretchHeight = true;
                    if (GUI.Button(new Rect(widthPad + 30, heightPad - 35, 32, 32), Playing ? PlayButtonOnAsset : PlayButtonAsset, buttonStyle))
                    {
                        if (!Playing && !Application.isPlaying)
                        {
                            //PlayClip(clip);
                            AudioSourceLoaded.time = TimeVal;
                            AudioSourceLoaded.Play();
                            Playing = true;
                        }
                    }

                    if (GUI.Button(new Rect(widthPad + 62, heightPad - 35, 32, 32), Playing ? StopButtonAsset : StopButtonOnAsset, buttonStyle))
                    {
                        if (!Application.isPlaying)
                        {
                            AudioSourceLoaded.Stop();
                            ShortStop = -1f;
                            Playing = false;
                        }

                    }
                    GUIStyle textFieldStyle = new GUIStyle();
                    textFieldStyle.normal.textColor = Color.white;
                    textFieldStyle.normal.background = BGDarkAsset as Texture2D;
                    textFieldStyle.contentOffset = new Vector2(10, 10);
                    TimeVal = float.Parse(GUI.TextField(new Rect(widthPad + 94, heightPad - 35, 100, 32), TimeVal.ToString(), textFieldStyle));

                    PaintWaveformSpectrum(AudioClipLoaded, 1, new Rect(WaveFormPosX, WaveFormPosY, WaveFormWidth, 100), new Color(14f / 255f, 229f / 255f, 198f / 255f, 1f));

                    heightPad += 20;
                    if (notYetLoaded)
                    {
                        ReloadNodes(Vector2.zero);
                    }
                    DrawNodes();
                    
					if (LipSyncLoaded.Skin == null)
					{
						LipSyncLoaded.Skin = LipSyncLoaded.GetComponent<SkinnedMeshRenderer>();
						return;
					}
					string[] BlendShapeNames = GetArrayBlendShapes(LipSyncLoaded.Skin);
					for (int i = 0; i < 9; i++)
					{
						CreateLipSyncBlendShapeButton(i, widthPad + (i * 110), heightPad, BlendShapeNames);
					}
					heightPad += 150;
					if (LipSyncLoaded!=null && LipSyncLoaded.Skin != null && LipSyncLoaded.Skin.sharedMesh != null)
					{
						if (options == null || options.Length!= LipSyncLoaded.Skin.sharedMesh.subMeshCount)
						{
							options = new string[LipSyncLoaded.Skin.sharedMesh.subMeshCount];
							for (int i = 0; i < options.Length; i++)
							{
								if (i < LipSyncLoaded.Skin.sharedMaterials.Length)
									options[i] = LipSyncLoaded.Skin.sharedMaterials[i].name;
								else
									options[i] = ("Mesh_" + i);


							}
							flags = -1;
						}
						EditorGUI.LabelField(new Rect(widthPad + 15, heightPad + 185, 100, 16), "Sub Mesh");
						EditorGUI.BeginChangeCheck();
						flags = EditorGUI.MaskField(new Rect(widthPad + 94, heightPad + 185, 100, 16), flags, options);
						if(EditorGUI.EndChangeCheck())
                        {
							options = new string[LipSyncLoaded.Skin.sharedMesh.subMeshCount];
							for (int i = 0; i < options.Length; i++)
							{
								if (i < LipSyncLoaded.Skin.sharedMaterials.Length)
									options[i] = LipSyncLoaded.Skin.sharedMaterials[i].name;
								else
									options[i] = ("Mesh_" + i);


							}
						}
						selectedOptions = new List<int>();
						for (int i = 0; i < options.Length; i++)
						{
							if ((flags & (1 << i)) == (1 << i)) selectedOptions.Add(i);
						}
						EditorGUI.LabelField(new Rect(widthPad + 225, heightPad + 185, 100, 16), "Type");
						string[] blendshapeTypes = { "From Bind Pose", "From Bind Pose (Skinned)", "From Current Pose" };
						BlendShapeType = EditorGUI.Popup(new Rect(widthPad + 284, heightPad + 185, 100, 16),BlendShapeType, blendshapeTypes);
							

					}

				}
                

				ProcessNodeEvents(Event.current);
				ProcessEvents(Event.current);
				if (GUI.changed)
				{
					if (LipSyncLoaded != null)
					{
						LipSyncLoaded.InitializeFromData(MakeFaceShapeData(_phonemeMarkers), LipSyncLoaded.LipSyncIndex);
					}
					Repaint();
				}
				string LogoPath = _puppetFacePath +"/Textures/GUI/PuppetFaceLogo.png";
				Texture LogoAsset = AssetDatabase.LoadAssetAtPath(LogoPath, typeof(Texture)) as Texture;
				GUI.DrawTexture(new Rect(_puppetFaceEditor.position.width - 84, 0, 64, 64), LogoAsset, ScaleMode.ScaleToFit);
			}

			GUILayout.EndArea();




		}

        private static void SetAudioClipData()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);
            FileInfo[] fileInfo = directoryInfo.GetFiles("*.wav", SearchOption.AllDirectories);
            waveNames = new string[fileInfo.Length];
            //List<DirectoryInfo> uniqueDirs = new List<DirectoryInfo>();
            for (int i = 0; i < waveNames.Length; i++)
            {
                waveNames[i] = fileInfo[i].Name;// (fileInfo[i].Directory.Name + "/" + fileInfo[i].Name);
                /*if(!uniqueDirs.Contains(fileInfo[i].Directory))
                {
                    uniqueDirs.Add(fileInfo[i].Directory);
                }*/
            }

            filesToConvert.Clear();
            for (int i = 0; i < waveNames.Length; i++)
            {
                if ((AudioClipsToConvert & (1 << i)) == (1 << i)) filesToConvert.Add(fileInfo[i].FullName);
            }
        }

        private static bool CheckForSelectedLipSync(GameObject lipSyncGO, bool notYetLoaded)
		{
			if (AudioSourceLoaded == null)
			{
				notYetLoaded = true;

			}
			LipSyncLoaded = lipSyncGO.GetComponent<LipSync>();
			LipSyncInScene = FindObjectsOfType<LipSync>();
			LipSyncInSceneString = new string[LipSyncInScene.Length];
			for (int i = 0; i < LipSyncInScene.Length; i++)
			{
				LipSyncInSceneString[i] = LipSyncInScene[i].gameObject.name;
				if (LipSyncLoaded == LipSyncInScene[i])
					LipSyncSelected = i;
			}

			AudioSourceLoaded = lipSyncGO.GetComponent<AudioSource>();

			if (LipSyncLoaded.AudioClips != null && LipSyncLoaded.AudioClips.Length > 0 && LipSyncLoaded.LipSyncIndex < LipSyncLoaded.LipSyncFiles.Length)
			{
				LipSyncLoaded.LipSyncIndex = Mathf.Clamp(LipSyncLoaded.LipSyncIndex, 0, LipSyncLoaded.LipSyncFiles.Length - 1);
				AudioClipLoaded = LipSyncLoaded.AudioClips[LipSyncLoaded.LipSyncIndex];
				AudioSourceLoaded.clip = AudioClipLoaded;
				AudioClipNames = new string[LipSyncLoaded.LipSyncFiles.Length];
				for (int i = 0; i < LipSyncLoaded.LipSyncFiles.Length; i++)
				{
					if (LipSyncLoaded.LipSyncFiles[i] != null)
						AudioClipNames[i] = LipSyncLoaded.LipSyncFiles[i].name;
				}

			}
			else
			{
				AudioClipLoaded = null;

			}

			return notYetLoaded;
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
        private void ConvertAudioToLipSyncFile()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);
            FileInfo[] fileInfo = directoryInfo.GetFiles("*.wav", SearchOption.AllDirectories);
            string startDir = "";
            if (fileInfo.Length > 0)
                startDir = fileInfo[0].Directory.FullName;
            string path = EditorUtility.OpenFilePanel("Select Wav Files", startDir, "wav");
            if (path == "")
                return;
            string[] paths = new string[1] { path };
            ConvertAudioToLipSyncAll(paths);
        }
        private void ConvertAudioToLipSyncFolder()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);
            FileInfo[] fileInfo = directoryInfo.GetFiles("*.wav", SearchOption.AllDirectories);
            string startDir = "";
            if (fileInfo.Length > 0)
                startDir = fileInfo[0].Directory.FullName;
            string path = EditorUtility.OpenFolderPanel("Select Wav Files", startDir, "");
            if (path == "")
                return;
            string[] files = Directory.GetFiles(path);           
            List<string> wavFiles = new List<string>();
            foreach (string file in files)
                if (file.EndsWith(".wav"))
                    wavFiles.Add(file);

            ConvertAudioToLipSyncAll(wavFiles.ToArray());

        }
        private void ConvertAudioToLipSyncAll(string[] files)
		{
            /*var extensions = new[] {
			new ExtensionFilter("Sound Files", "mp3", "wav" ),};
			string[] files = StandaloneFileBrowser.OpenFilePanel("Select Wav Files", "", extensions, true);
            */

            if (files.Length != 0)
			{
				foreach (string path in files)
				{
					string relativepath = path;
					
					relativepath = "Assets" + path.Substring(Application.dataPath.Length);
					relativepath = "Assets" + path.Substring(Application.dataPath.Length);
					
					//Debug.Log(relativepath + "  " + path);
					AudioClip audioCip = AssetDatabase.LoadAssetAtPath(relativepath, typeof(AudioClip)) as AudioClip;
					string lipSyncPath = LipSync.ConvertAudioToPhoneme(audioCip);

                    // get correct lip sync
                    if (LipSyncLoaded == null)
                    {
                        if (Selection.activeGameObject != null)
                        {
                            LipSyncLoaded = Selection.activeGameObject.GetComponent<LipSync>();
                            //LipSync lipSync = Selection.activeGameObject.GetComponent<LipSync>();
                            //if (lipSync == null)
                            //{
                            if (LipSyncLoaded != null)
                            {
                                SkinnedMeshRenderer smr = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();
                                if (smr == null)
                                {
                                    MeshRenderer mr = Selection.activeGameObject.GetComponent<MeshRenderer>();
                                    MeshFilter mf = Selection.activeGameObject.GetComponent<MeshFilter>();

                                    if (mr == null)
                                    {
                                        Debug.LogWarning("Please select a gameObject with a SkinnedMeshRenderer / MeshRenderer & MeshFilter");
                                        return;
                                    }
                                    else
                                    {
                                        Material[] mats = mr.sharedMaterials;
                                        smr = Selection.activeGameObject.AddComponent<SkinnedMeshRenderer>();
                                        smr.sharedMesh = mf.sharedMesh;
                                        smr.sharedMaterials = mats;
                                        DestroyImmediate(mf);
                                        DestroyImmediate(mr);
                                    }
                                }
                                LipSyncLoaded.Skin = smr;
                            }
                            else
                                Debug.LogWarning("Please Select A GameObject With A lipSync Component");

                            //}
                        }
                        else
                        {
                            LipSync[] lipSyncsInScene = FindObjectsOfType<LipSync>();
                            if (lipSyncsInScene.Length > 0)
                                LipSyncLoaded = lipSyncsInScene[0];
                            else
                                Debug.LogWarning("Please Select A GameObject With A lipSync Component");
                        }
                    }

                    if (LipSyncLoaded != null)
					{
                        Selection.activeGameObject = LipSyncLoaded.gameObject;

                        List<AudioClip> audioClips = new List<AudioClip>();
						if (LipSyncLoaded.AudioClips != null && LipSyncLoaded.AudioClips.Length > 0)
							audioClips = LipSyncLoaded.AudioClips.ToList();

                        int addedIndex = 0;
                        if (audioClips.Contains(audioCip))
                        {
                            addedIndex = audioClips.IndexOf(audioCip);
                        }
                        else
                        {
                            audioClips.Add(audioCip);
                            addedIndex = audioClips.Count - 1;
                        }
                        LipSyncLoaded.AudioClips = audioClips.ToArray();

						AssetDatabase.Refresh();

						List<TextAsset> lipSyncFiles = new List<TextAsset>();
						if (LipSyncLoaded.LipSyncFiles != null && LipSyncLoaded.LipSyncFiles.Length > 0)
							lipSyncFiles = LipSyncLoaded.LipSyncFiles.ToList();

						relativepath = lipSyncPath;
						if (lipSyncPath.StartsWith(Application.dataPath))
						{
							relativepath = "Assets" + lipSyncPath.Substring(Application.dataPath.Length);
						}


						TextAsset textAsset = AssetDatabase.LoadAssetAtPath(relativepath, typeof(TextAsset)) as TextAsset;
                        if (addedIndex < lipSyncFiles.Count)
                            lipSyncFiles[addedIndex] = textAsset;
                        else
                        {
                            lipSyncFiles.Add(textAsset);
                            addedIndex = LipSyncLoaded.LipSyncFiles.Length - 1;

                        }

                        LipSyncLoaded.LipSyncFiles = lipSyncFiles.ToArray();
						AudioSourceLoaded = LipSyncLoaded.gameObject.GetComponent<AudioSource>();

                        LipSyncLoaded.LipSyncIndex = addedIndex;// ;
						LipSyncLoaded.Initialized = false;
						LipSyncLoaded.InitializeFromData(MakeFaceShapeData(_phonemeMarkers), LipSyncLoaded.LipSyncIndex);
                        //LipSyncLoaded.InitializeFromFile();                        



                    }
                }
			}
		}


		public void SavePhonemes()
		{

			LipSyncLoaded.InitializeFromData(MakeFaceShapeData(_phonemeMarkers), LipSyncLoaded.LipSyncIndex);

			if (LipSyncLoaded != null)
			{

				if (LipSyncLoaded.FaceShapes != null && LipSyncLoaded.FaceShapes.Count > LipSyncLoaded.LipSyncIndex)
				{
					var serializer = new XmlSerializer(typeof(LipSyncType));
					string path = AssetDatabase.GetAssetPath(LipSyncLoaded.LipSyncFiles[LipSyncLoaded.LipSyncIndex]);// "Assets/LipSync/output.xml";

					LipSyncLoaded.FaceShapes[LipSyncLoaded.LipSyncIndex].faceShapes.Clear();
					List<FaceShape> newFaceShape = new List<FaceShape>();
					LipSyncType test = new LipSyncType();
					test.mouthCues = new List<mouthCue>();
					for (int i = 0; i < _phonemeMarkers.Count; i++)
					{
						mouthCue cue = new mouthCue();
						cue.start = _phonemeMarkers[i].Start;
						cue.end = _phonemeMarkers[i].End;
						cue.shapeName = _phonemeMarkers[i].Label;
						cue.strength = _phonemeMarkers[i].Strength;
						cue.blendShapeID = _phonemeMarkers[i].BlendShapeID;

						test.mouthCues.Add(cue);
						newFaceShape.Add(new FaceShape(_phonemeMarkers[i].Start, _phonemeMarkers[i].End, _phonemeMarkers[i].Label, cue.strength, cue.blendShapeID));
					}
					LipSyncLoaded.FaceShapes[LipSyncLoaded.LipSyncIndex].faceShapes = newFaceShape;
					var stream = new FileStream(path, FileMode.Create);
					serializer.Serialize(stream, test);
					stream.Close();
					Debug.Log(LipSyncLoaded.LipSyncFiles[LipSyncLoaded.LipSyncIndex].name + " Saved.");
					//UnityEngine.Debug.Log("Saving path " + path);
				}
			}
		}
		static public string GetPrestonBlairNamesFromIndex(int index)
		{
			switch (index)
			{
				case 0:
					return "MBP";
				case 1:
					return "etc";
				case 2:
					return "E";
				case 3:
					return "AI";
				case 4:
					return "O";
				case 5:
					return "U";
				case 6:
					return "FV";
				case 7:
					return "L";
				case 8:
					return "rest";

			}
			return "";
		}
		static public string GetPrestonBlairNamesFromLetter(string letter)
		{
			switch (letter)
			{
				case "A":
					return "MBP";
				case "B":
					return "etc";
				case "C":
					return "E";
				case "D":
					return "AI";
				case "E":
					return "O";
				case "F":
					return "U";
				case "G":
					return "FV";
				case "H":
					return "L";
				case "X":
					return "rest";

			}
			return "";
		}

		private static void CreateLipSyncBlendShapeButton(int id, int widthPad, int heightPad, string[] BlendShapeNames)
		{

			string faceShape = (_puppetFacePath +"/Textures/GUI/img/lisa" + id + ".png");

			Texture faceShapeTexture = AssetDatabase.LoadAssetAtPath(faceShape, typeof(Texture)) as Texture;
			if (CurrentBlendShapeModel != null)
			{
				if (CurrentBlendShapeIndex != id)
				{
					GUI.color = new Color(1, 1, 1, .5f);
				}
				else
				{
					if (GUI.Button(new Rect(widthPad + 10, heightPad + 230 + faceShapeTexture.height * .25f, faceShapeTexture.width * .25f, 20), "Discard"))
					{
						LipSyncLoaded.GetComponent<SkinnedMeshRenderer>().enabled = true;
						if (CurrentBlendShapeModel != null)
							DestroyImmediate(CurrentBlendShapeModel);
						Selection.activeGameObject = LipSyncLoaded.GetComponent<SkinnedMeshRenderer>().gameObject;
						CurrentBlendShapeModel = null;
						
						if (LipSyncLoaded.TransformStates.Count > 0)
						{
							for (int j = 0; j < LipSyncLoaded.FaceBones.Length; j++)
							{
								FaceControl fc = LipSyncLoaded.FaceBones[j].gameObject.GetComponent<FaceControl>();
								if (fc != null)
									DestroyImmediate(fc);

								LipSyncLoaded.FaceBones[j].localPosition = LipSyncLoaded._currentTransforms.transformStates[j].position;
								LipSyncLoaded.FaceBones[j].localRotation = LipSyncLoaded._currentTransforms.transformStates[j].rotation;
								LipSyncLoaded.FaceBones[j].localScale = LipSyncLoaded._currentTransforms.transformStates[j].scale;

							}

						}

						BonePoseIndex = -1;
					}
				}
			}
			else if (BonePoseIndex != -1)
			{
				if (BonePoseIndex == id)
				{
					if (GUI.Button(new Rect(widthPad + 10, heightPad + 230 + faceShapeTexture.height * .25f, faceShapeTexture.width * .25f, 20), "Discard"))
					{
						if (LipSyncLoaded.TransformStates.Count > 0)
						{
							for (int j = 0; j < LipSyncLoaded.FaceBones.Length; j++)
							{
								FaceControl fc = LipSyncLoaded.FaceBones[j].gameObject.GetComponent<FaceControl>();
								if (fc != null)
									DestroyImmediate(fc);

								LipSyncLoaded.FaceBones[j].localPosition = LipSyncLoaded._currentTransforms.transformStates[j].position;
								LipSyncLoaded.FaceBones[j].localRotation = LipSyncLoaded._currentTransforms.transformStates[j].rotation;
								LipSyncLoaded.FaceBones[j].localScale = LipSyncLoaded._currentTransforms.transformStates[j].scale;

							}

						}

						BonePoseIndex = -1;
					}
				}
			}
			if (GUI.Button(new Rect(widthPad + 10, heightPad + 190, faceShapeTexture.width * .25f, faceShapeTexture.height * .25f), faceShapeTexture))
			{
				if (LipSyncLoaded.BlendShapeIndexes[id] > LipSyncLoaded.Skin.sharedMesh.blendShapeCount)
					LipSyncLoaded.BlendShapeIndexes[id] = LipSyncLoaded.Skin.sharedMesh.blendShapeCount;

				if(CurrentBlendShapeModel == null)
                {				
					GameObject SelectedGO = LipSyncLoaded.gameObject;
					SkinnedMeshRenderer currentSMR = SelectedGO.GetComponent<SkinnedMeshRenderer>();
					Selection.activeGameObject = SelectedGO;
					if (currentSMR != null)
					{
						if(LipSyncLoaded.FaceBones!=null && LipSyncLoaded.FaceBones.Length>0)
							EnableBonePoseEdit(id);

						if (LipSyncLoaded.BlendShapeIndexes[id] >= BlendShapeNames.Length - 1)
							CurrentBlendShapeModel = MakeNewBlendShapeModel(GetPrestonBlairNamesFromIndex(id), -1);
						else
							CurrentBlendShapeModel = MakeNewBlendShapeModel(BlendShapeNames[LipSyncLoaded.BlendShapeIndexes[id]], LipSyncLoaded.BlendShapeIndexes[id]);

						CurrentBlendShapeIndex = id;

						if (flags == 0)
						{
							currentSMR.SetBlendShapeWeight(LipSyncLoaded.BlendShapeIndexes[id], 100);
							currentSMR.enabled = true;
						}

					}

					return;				

				}
				else
                {
					BlendShapeManager.SetBlendShape(CurrentBlendShapeModel, LipSyncLoaded.Skin, BlendShapeType);
					LipSyncLoaded.Skin.enabled = true;
					if(LipSyncLoaded.BlendShapeIndexes[id]<LipSyncLoaded.Skin.sharedMesh.blendShapeCount)
						LipSyncLoaded.Skin.SetBlendShapeWeight(LipSyncLoaded.BlendShapeIndexes[id], 0);

					for (int i = 0; i < BlendShapeNames.Length; i++)
					{
						if (CurrentBlendShapeModel.name == BlendShapeNames[i])
						{
							LipSyncLoaded.BlendShapeIndexes[id] = i;
						}
					}

					DestroyImmediate(CurrentBlendShapeModel);
					Selection.activeGameObject = LipSyncLoaded.gameObject;
					CurrentBlendShapeModel = null;
					SetBonePose(id);
					CurrentBlendShapeIndex = -1;
				}

				/*
				if (Selection.activeGameObject != null)
				{
					MeshModifier mm = Selection.activeGameObject.GetComponent<MeshModifier>();
					if (mm == null)
					{

						
						Selection.activeGameObject = LipSyncLoaded.gameObject;

						GameObject SelectedGO = LipSyncLoaded.gameObject;
						SkinnedMeshRenderer currentSMR = SelectedGO.GetComponent<SkinnedMeshRenderer>();
						if (currentSMR != null)
						{
							EnableBonePoseEdit(id);

							if (LipSyncLoaded.BlendShapeIndexes[id] >= BlendShapeNames.Length - 1)
								CurrentBlendShapeModel = MakeNewBlendShapeModel(GetPrestonBlairNamesFromIndex(id), -1);
							else
								CurrentBlendShapeModel = MakeNewBlendShapeModel(BlendShapeNames[LipSyncLoaded.BlendShapeIndexes[id]], LipSyncLoaded.BlendShapeIndexes[id]);

							CurrentBlendShapeIndex = id;

							if (flags == 0)
							{
								currentSMR.SetBlendShapeWeight(LipSyncLoaded.BlendShapeIndexes[id], 100);
								currentSMR.enabled = true;
							}

						}
						

						return;
					}
					if (mm.TargetSkin != null)
					{
						CurrentSkinnedModel = mm.TargetSkin;
						CurrentBlendShapeModel = mm.gameObject;
						CurrentBlendShapeIndex = id;

					}
					if (CurrentBlendShapeModel != null)
					{
						BlendShapeManager.SetBlendShape(CurrentBlendShapeModel, CurrentSkinnedModel, BlendShapeType);
						CurrentSkinnedModel.enabled = true;
						CurrentSkinnedModel.SetBlendShapeWeight(LipSyncLoaded.BlendShapeIndexes[id], 0);

						for (int i = 0; i < BlendShapeNames.Length; i++)
						{
							if (CurrentBlendShapeModel.name == BlendShapeNames[i])
							{
								LipSyncLoaded.BlendShapeIndexes[id] = i;
							}
						}

						DestroyImmediate(CurrentBlendShapeModel);
						Selection.activeGameObject = CurrentSkinnedModel.gameObject;
						CurrentBlendShapeModel = null;
						SetBonePose(id);

					}
					
				}
				else
				{
					if (CurrentBlendShapeModel != null)
					{
						Selection.activeGameObject = CurrentBlendShapeModel;
						return;
					}
					else if(LipSyncLoaded!=null)
						Selection.activeGameObject = LipSyncLoaded.gameObject;

				}*/

			}
			GUI.color = Color.white;
			LipSyncLoaded.BlendShapeIndexes[id] = EditorGUI.Popup(new Rect(widthPad + 10, heightPad + 275, faceShapeTexture.width * .25f, 20), "", LipSyncLoaded.BlendShapeIndexes[id], BlendShapeNames);
			if (LipSyncLoaded != null && LipSyncLoaded.FaceBones != null && LipSyncLoaded.FaceBones.Length > 0)
			{
				if (BonePoseIndex >= 0)
				{
					if(BonePoseIndex == id)
						GUI.color = new Color(14f / 255f, 229f / 255f, 198f / 255f, 1f);
					if (GUI.Button(new Rect(widthPad + 10, heightPad + 295, faceShapeTexture.width * .25f, 15), "Edit Bone Pose"))
                    {
                        SetBonePose(id);
                    }
                    GUI.color = Color.white;

				}
				else if (GUI.Button(new Rect(widthPad + 10, heightPad + 295, faceShapeTexture.width * .25f, 15), "Edit Bone Pose"))
                {
                    EnableBonePoseEdit(id);
					Selection.activeGameObject = LipSyncLoaded.FaceBones[0].gameObject;

				}
			}



		}

        private static void SetBonePose(int id)
        {
            // Set Bones

            if (LipSyncLoaded.TransformStates.Count > 0)
            {
                for (int j = 0; j < LipSyncLoaded.FaceBones.Length; j++)
                {
                    FaceControl fc = LipSyncLoaded.FaceBones[j].gameObject.GetComponent<FaceControl>();
                    if (fc != null)
                        DestroyImmediate(fc);

                    LipSyncLoaded.TransformStates[id].transformStates[j].position = LipSyncLoaded.FaceBones[j].localPosition;
                    LipSyncLoaded.TransformStates[id].transformStates[j].rotation = LipSyncLoaded.FaceBones[j].localRotation;
                    LipSyncLoaded.TransformStates[id].transformStates[j].scale = LipSyncLoaded.FaceBones[j].localScale;

					// If Rest Pose, set initial position
					if (id == 8)
					{
						TransformState ts = new TransformState(LipSyncLoaded.FaceBones[j].localPosition, LipSyncLoaded.FaceBones[j].localRotation, LipSyncLoaded.FaceBones[j].localScale);
						LipSyncLoaded._currentTransforms.transformStates[j] = ts;
					}


					LipSyncLoaded.FaceBones[j].localPosition = LipSyncLoaded._currentTransforms.transformStates[j].position;
                    LipSyncLoaded.FaceBones[j].localRotation = LipSyncLoaded._currentTransforms.transformStates[j].rotation;
                    LipSyncLoaded.FaceBones[j].localScale = LipSyncLoaded._currentTransforms.transformStates[j].scale;




                }

            }

            BonePoseIndex = -1;
        }

        private static void EnableBonePoseEdit(int id)
        {
            BonePoseIndex = id;

            if (LipSyncLoaded.TransformStates.Count == 0 || LipSyncLoaded.FaceBones.Length != LipSyncLoaded.TransformStates[0].transformStates.Count || LipSyncLoaded._currentTransforms.transformStates.Count == 0)
            {
                Debug.Log("Initialising Bones");
                LipSyncLoaded.TransformStates.Clear();

                for (int i = 0; i < 9; i++)
                {
                    ListTransformStates tl = new ListTransformStates();
                    for (int j = 0; j < LipSyncLoaded.FaceBones.Length; j++)
                    {
                        TransformState ts = new TransformState(LipSyncLoaded.FaceBones[j].localPosition, LipSyncLoaded.FaceBones[j].localRotation, LipSyncLoaded.FaceBones[j].localScale);
                        tl.transformStates.Add(ts);
                        if (i == 8)
                            LipSyncLoaded._currentTransforms.transformStates.Add(ts);
                    }
                    LipSyncLoaded.TransformStates.Add(tl);

                }
            }


            for (int j = 0; j < LipSyncLoaded.FaceBones.Length; j++)
            {
				FaceControl fc = LipSyncLoaded.FaceBones[j].gameObject.GetComponent<FaceControl>();
				if(fc==null)
					LipSyncLoaded.FaceBones[j].gameObject.AddComponent<FaceControl>();
                LipSyncLoaded.FaceBones[j].localPosition = LipSyncLoaded.TransformStates[id].transformStates[j].position;
                LipSyncLoaded.FaceBones[j].localRotation = LipSyncLoaded.TransformStates[id].transformStates[j].rotation;
                LipSyncLoaded.FaceBones[j].localScale = LipSyncLoaded.TransformStates[id].transformStates[j].scale;
                if (id == 8)
                {
                    TransformState ts = new TransformState(LipSyncLoaded.FaceBones[j].localPosition, LipSyncLoaded.FaceBones[j].localRotation, LipSyncLoaded.FaceBones[j].localScale);
                    LipSyncLoaded._currentTransforms.transformStates[j] = ts;
                }
            }
        }

        public void SetLipSync()
		{
			if (LipSyncLoaded != null)
			{
				if (!LipSyncLoaded.Initialized)
					LipSyncLoaded.InitializeFromFile();
				LipSyncLoaded.SetPhoneme(LipSyncLoaded.LipSyncIndex, TimeVal);
			}
		}
		void Update()
		{
			if (!Application.isPlaying)
			{
				SetEditorDeltaTime();
				if (Playing)
				{
					if (TimeVal < AudioClipLoaded.length)
					{
						TimeVal += editorDeltaTime;
					}
					else
					{
						Playing = false;
						TimeVal = 0f;
					}
				}

				if (Playing)
				{
					ShortStop -= Time.deltaTime;
					SetLipSync();
					Repaint();


				}
				else if (ShortStop >= 0f)
				{
					ShortStop -= Time.deltaTime;
				}
				else
				{
					if (AudioSourceLoaded != null)
						AudioSourceLoaded.Stop();
					Playing = false;
				}
			}
		}
		private void SetEditorDeltaTime()
		{
			if (lastTimeSinceStartup == 0f)
			{
				lastTimeSinceStartup = (float)EditorApplication.timeSinceStartup;
			}
			editorDeltaTime = (float)EditorApplication.timeSinceStartup - lastTimeSinceStartup;
			lastTimeSinceStartup = (float)EditorApplication.timeSinceStartup;
		}
		private void DrawNodes()
		{
			if (_phonemeMarkers != null)
			{
				for (int i = 0; i < _phonemeMarkers.Count; i++)
				{
					_phonemeMarkers[i].Draw();
				}
			}
		}

		private void ProcessEvents(Event e)
		{
			switch (e.type)
			{
				case EventType.MouseDown:
					if (e.button == 1)
					{
						ProcessContextMenu(e.mousePosition);
					}
					break;
				
				case EventType.KeyDown:
					if (e.keyCode == KeyCode.Space && !Application.isPlaying)
					{
						if (AudioSourceLoaded != null)
						{
							if (!Playing)
							{
								AudioSourceLoaded.time = TimeVal;
								AudioSourceLoaded.Play();
								Playing = true;
							}
							else
							{
								AudioSourceLoaded.Stop();
								ShortStop = -1f;
								Playing = false;
							}
						}
					}
					break;
			}
		}

		private void ProcessNodeEvents(Event e)
		{
			if (_phonemeMarkers != null)
			{
				for (int i = _phonemeMarkers.Count - 1; i >= 0; i--)
				{
					bool guiChanged = _phonemeMarkers[i].ProcessEvents(e);

					if (guiChanged)
					{
						GUI.changed = true;

						break;
					}
				}
			}
		}

		private void ProcessContextMenu(Vector2 mousePosition)
		{
			GenericMenu genericMenu = new GenericMenu();
			genericMenu.AddItem(new GUIContent("MBP"), false, () => OnClickAddNode(mousePosition, "A"));
			genericMenu.AddItem(new GUIContent("etc"), false, () => OnClickAddNode(mousePosition, "B"));
			genericMenu.AddItem(new GUIContent("E"), false, () => OnClickAddNode(mousePosition, "C"));
			genericMenu.AddItem(new GUIContent("AI"), false, () => OnClickAddNode(mousePosition, "D"));
			genericMenu.AddItem(new GUIContent("O"), false, () => OnClickAddNode(mousePosition, "E"));
			genericMenu.AddItem(new GUIContent("U"), false, () => OnClickAddNode(mousePosition, "F"));
			genericMenu.AddItem(new GUIContent("FV"), false, () => OnClickAddNode(mousePosition, "G"));
			genericMenu.AddItem(new GUIContent("L"), false, () => OnClickAddNode(mousePosition, "H"));
			genericMenu.AddItem(new GUIContent("rest"), false, () => OnClickAddNode(mousePosition, "X"));
			genericMenu.AddItem(new GUIContent("Delete"), false, () => DeleteNode(mousePosition));
			int bsCount = LipSyncLoaded.Skin.sharedMesh.blendShapeCount;
			for(int id =0;id< bsCount;id++)
            {
				if (!LipSyncLoaded.BlendShapeIndexes.Contains(id))
				{
					int index = id;
					string bsName = LipSyncLoaded.Skin.sharedMesh.GetBlendShapeName(id);
					genericMenu.AddItem(new GUIContent("All Blend Shapes/" + bsName + " (" + id+ ")"), false, () => OnClickAddNode(mousePosition, bsName, index));
				}

			}
			genericMenu.ShowAsContext();
		}
		private void DeleteNode(Vector2 mousePosition)
		{

			for (int i = 0; i < _phonemeMarkers.Count; i++)
			{
                if(_phonemeMarkers[i].IsMarkerUnderMouse(mousePosition))
                {
                    _phonemeMarkers.RemoveAt(i);
                    break;
                }
				/*if (CurrentSelectedPhonemeMarker >= 0 && i == CurrentSelectedPhonemeMarker)
				{
					_phonemeMarkers.RemoveAt(i);
					break;
				}*/
				/*else if (_phonemeMarkers[i].rect.Contains(mousePosition))
				{
					_phonemeMarkers.RemoveAt(i);
					break;
				}*/
			}
			RefreshNodes(mousePosition);
			Repaint();
            SetLipSync();


        }
        private void OnClickAddNode(Vector2 mousePosition, string shapeName, int blendShapeIndex = -1)
		{          
            if (_phonemeMarkers == null)
			{
				_phonemeMarkers = new List<PhonemeMarker>();
			}

            float markerPos = ((mousePosition.x - WaveFormPosX) / (float)WaveFormWidth) * (float)AudioClipLoaded.length;

            // check if hovering over phoneme
            for (int i = 0; i < _phonemeMarkers.Count; i++)
            {
                if(_phonemeMarkers[i].IsMarkerUnderMouse(mousePosition))
                {
                    markerPos = ((_phonemeMarkers[i].rect.center.x - WaveFormPosX) / (float)WaveFormWidth) * (float)AudioClipLoaded.length;
                    DeleteNode(mousePosition);
                    RefreshNodes(mousePosition);

                }

            }

			_phonemeMarkers.Add(new PhonemeMarker(new Vector2(markerPos, WaveFormPosY + 100), 70, 70, _phonemeMarkerStyle, shapeName, markerPos - 0.05f, markerPos + 0.05f, 1f, _phonemeMarkers.Count, blendShapeIndex));
            CurrentSelectedPhonemeMarker = _phonemeMarkers.Count - 1;


            RefreshNodes(mousePosition);
			Repaint();
            SetLipSync();

        }
		private void ReloadNodes(Vector2 mousePosition)
		{

			if (AudioSourceLoaded && LipSyncLoaded && AudioClipLoaded && LipSyncLoaded.LipSyncFiles.Length > 0)
			{
				if (_phonemeMarkers == null)
				{
					_phonemeMarkers = new List<PhonemeMarker>();
				}
				_phonemeMarkers.Clear();
				string[] _phonemeArray = LipSyncLoaded.GetPhonemes(LipSyncLoaded.LipSyncIndex);
				for (int i = 0; i < _phonemeArray.Length; i++)
				{
					string[] vals = _phonemeArray[i].Split(';');
					float timeVal = (float.Parse(vals[1]) + float.Parse(vals[0])) * .5f;
					_phonemeMarkers.Add(new PhonemeMarker(new Vector2(WaveFormPosX - 35 + (timeVal / AudioClipLoaded.length) * WaveFormWidth, WaveFormPosY + 100), 70, 70, _phonemeMarkerStyle, vals[2], float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[3]), i, int.Parse(vals[4])));

				}

				Debug.Log("Loaded " + LipSyncLoaded.LipSyncFiles[LipSyncLoaded.LipSyncIndex].name);
			}
		}
		private void RefreshNodes(Vector2 mousePosition)
		{

			if (AudioSourceLoaded && LipSyncLoaded && AudioClipLoaded)
			{
				if (_phonemeMarkers == null || _phonemeMarkers.Count == 0)
				{
					//ReloadNodes(mousePosition);
					//DrawNodes();
				}
				else
				{

					for (int i = _phonemeMarkers.Count - 1; i >= 0; i--)
					{

						float timeVal = (_phonemeMarkers[i].Start + _phonemeMarkers[i].End) * 0.5f;// (float.Parse(vals[1]) + float.Parse(vals[0]))/2f;
						_phonemeMarkers[i] = (new PhonemeMarker(new Vector2(WaveFormPosX - 35 + (timeVal / AudioClipLoaded.length) * WaveFormWidth, WaveFormPosY + 100), 70, 70, _phonemeMarkerStyle, _phonemeMarkers[i].Label, _phonemeMarkers[i].Start, _phonemeMarkers[i].End, _phonemeMarkers[i].Strength, i, _phonemeMarkers[i].BlendShapeID));

					}
				}
				if (_phonemeMarkers != null && _phonemeMarkers.Count > 0)
					LipSyncLoaded.InitializeFromData(MakeFaceShapeData(_phonemeMarkers), LipSyncLoaded.LipSyncIndex);

			}
		}

		public ListFaceShapes MakeFaceShapeData(List<PhonemeMarker> pms)
		{
			ListFaceShapes FaceShapeData = new ListFaceShapes();
			for (int t = 0; t < pms.Count; t++)
			{
				FaceShapeData.Add(new FaceShape(pms[t].Start, pms[t].End, pms[t].Label, pms[t].Strength, pms[t].BlendShapeID ));

			}
			return FaceShapeData;

		}
		static void MakeThisABlendShapeModel()
		{
			GameObject SelectedGO = Selection.activeGameObject;
			SkinnedMeshRenderer smr = SelectedGO.GetComponent<SkinnedMeshRenderer>();
			CurrentSkinnedModel = smr;

			MeshModifier meshModifier = SelectedGO.AddComponent<MeshModifier>();
			meshModifier.TargetSkin = smr;

		}
		static GameObject MakeNewBlendShapeModel(string blendShapeName, int index = 0)
		{
			GameObject SelectedGO = Selection.activeGameObject;
			SkinnedMeshRenderer currentSMR = SelectedGO.GetComponent<SkinnedMeshRenderer>();
			if (currentSMR == null)
			{
				MeshRenderer mr = SelectedGO.GetComponent<MeshRenderer>();
				if (mr == null)
				{

					Debug.LogWarning("Please select a mesh");
					return null;

				}
				else
				{
					currentSMR = SelectedGO.AddComponent<SkinnedMeshRenderer>();
				}
			}
			CurrentSkinnedModel = currentSMR;
			currentSMR.enabled = false;
			GameObject newBlendShapeGO = new GameObject();
			newBlendShapeGO.name = blendShapeName;

			if (BlendShapeType == 1)
			{
				SkinnedMeshRenderer smr = newBlendShapeGO.AddComponent<SkinnedMeshRenderer>();
				MakeBlendShapeMesh(SelectedGO.GetComponent<SkinnedMeshRenderer>().sharedMesh, index, currentSMR, smr, null, null);

			}
			else
			{
				MeshRenderer mrNew = newBlendShapeGO.AddComponent<MeshRenderer>();
				MeshFilter mf = newBlendShapeGO.AddComponent<MeshFilter>();
				MakeBlendShapeMesh(SelectedGO.GetComponent<SkinnedMeshRenderer>().sharedMesh, index, currentSMR,null, mrNew, mf);

			}




			newBlendShapeGO.transform.position = SelectedGO.transform.position;
			newBlendShapeGO.transform.rotation = SelectedGO.transform.rotation;
			newBlendShapeGO.transform.localScale = SelectedGO.transform.localScale;
			MeshModifier meshModifier = newBlendShapeGO.AddComponent<MeshModifier>();
			meshModifier.TargetSkin = currentSMR;
			meshModifier.BlendShapeType = BlendShapeType;
			meshModifier.ConnectedVertexThreshold = 0f;
			Selection.activeGameObject = newBlendShapeGO;
			

			return newBlendShapeGO;
		}
		static public Mesh MakeBlendShapeMesh(Mesh mesh, int index,SkinnedMeshRenderer currentSMR, SkinnedMeshRenderer smr, MeshRenderer mr, MeshFilter mf)
		{
			Mesh newMesh = new Mesh();
			if (BlendShapeType == 2)
				currentSMR.BakeMesh(newMesh);
			else
				newMesh.vertices = currentSMR.sharedMesh.vertices;


			Vector3[] deltas = new Vector3[mesh.vertexCount];
			Vector3[] deltaNormals = new Vector3[mesh.vertexCount];
			Vector3[] deltaTangents = new Vector3[mesh.vertexCount];

			if (index >= 0)
			{
				currentSMR.SetBlendShapeWeight(index, 0);
				mesh.GetBlendShapeFrameVertices(index, 0, deltas, deltaNormals, deltaTangents);
				int vertCount = mesh.vertexCount;

				if (BlendShapeType == 2)
				{
					Vector3[] verts = newMesh.vertices;
					Vector3[] normals = newMesh.normals;

					for (int i = 0; i < vertCount; i++)
					{
						deltas[i] += verts[i];
					}
				}
				else
				{
					Vector3[] verts = mesh.vertices;
					Vector3[] normals = mesh.normals;
					for (int i = 0; i < vertCount; i++)
					{
						deltas[i] += verts[i];

					}
				}

				newMesh.vertices = deltas;

			}

			
			
			newMesh.colors = mesh.colors;
			newMesh.normals = mesh.normals;
			newMesh.uv = mesh.uv;
			newMesh.bindposes = mesh.bindposes;
			newMesh.boneWeights = mesh.boneWeights;
			newMesh.tangents = mesh.tangents;
			newMesh.subMeshCount = selectedOptions.Count;// mesh.subMeshCount;
			Material[] mats = new Material[selectedOptions.Count];
			for (int i = 0; i < selectedOptions.Count; i++)
			{
				newMesh.SetTriangles(mesh.GetTriangles(selectedOptions[i]), i);
				mats[i] = currentSMR.sharedMaterials[selectedOptions[i]];

			}
			/*
			newMesh.subMeshCount = mesh.subMeshCount;

			for (int i = 0; i < mesh.subMeshCount; i++)
			{
				newMesh.SetTriangles(mesh.GetTriangles(i), i);
			}*/
			if (smr == null)
			{
				mr.sharedMaterials = mats;
				mf.sharedMesh = newMesh;
			}
			else
            {
				smr.sharedMaterials = mats;
				smr.sharedMesh = newMesh;
				smr.bones = currentSMR.bones;
			}
			


			return newMesh;
		}
		static void FocusCameraOnGameObject(Camera c, GameObject go)
		{
			Bounds b = go.GetComponent<Renderer>().bounds;
			Vector3 max = b.size;
			float radius = Mathf.Max(max.x, Mathf.Max(max.y, max.z));
			float dist = 0.666f * radius / (Mathf.Sin(c.fieldOfView * Mathf.Deg2Rad));
			Vector3 pos = Vector3.forward * dist + b.center;
			c.transform.position = pos;
			c.transform.LookAt(b.center);
		}

		public static Texture2D TakeScreenShot(Camera viewCam = null)
		{
			if (viewCam == null)
				viewCam = SceneView.currentDrawingSceneView.camera;
			return Screenshot(viewCam);
		}
		public static void SetEditorData()
		{
			string savedData = blendShapeTextures.Count + ";";
			for (int i = 0; i < blendShapeTextures.Count; i++)
			{
				savedData += blendShapeTextures[i] + ";";
			}
			EditorPrefs.SetString("_PuppetFaceBlendShapeTexturePaths", savedData);

		}
		public static void GetEditorData()
		{
			string savedData = EditorPrefs.GetString("_PuppetFaceBlendShapeTexturePaths");

			if (savedData != "")
			{

				string[] savedDataSplit = savedData.Split(';');
				blendShapeTextures = new List<string>();
				blendShapeTextures.Clear();
				if (savedDataSplit.Length > 0)
				{
					int numberTextures = int.Parse(savedDataSplit[0]);
					if (numberTextures > 0)
					{
						for (int i = 0; i < numberTextures; i++)
						{
							blendShapeTextures.Add(savedDataSplit[i + 1]);
						}
					}
				}
			}
		}
		static Texture2D Screenshot(Camera cam)
		{
			if (cam == null)
			{
				Debug.LogWarning("Need a preview camera on the selected BlendShape");
				return null;
			}

			int resWidth = 408; //cam.pixelWidth;
			int resHeight = 334;// cam.pixelHeight;
			resWidth = (int)(float)(resWidth);
			resHeight = (int)(float)(resHeight);

			RenderTexture rt = new RenderTexture(resWidth, resHeight, 32);
			cam.targetTexture = rt;
			Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.ARGB32, false);
			cam.Render();
			RenderTexture.active = rt;
			screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
			screenShot.Apply();
			cam.targetTexture = null;
			RenderTexture.active = null; // JC: added to avoid errors
			DestroyImmediate(rt);
			return screenShot;
		}

		public static Texture2D SaveScreenshotToFile(string fileName, Camera cam)
		{
			Texture2D screenShot = Screenshot(cam);
			byte[] bytes = screenShot.EncodeToPNG();
			Debug.Log("Saving " + fileName);

			System.IO.File.WriteAllBytes(fileName, bytes);
			return screenShot;
		}
		public Texture2D PaintWaveformSpectrum(AudioClip audio, float saturation, Rect rect, Color col)
		{
			int widthPad = (int)rect.min.x;
			int heightPad = (int)rect.min.y;
			int width = (int)rect.width;
			int height = (int)rect.height;
			int windowWidth = (int)_puppetFaceEditor.position.width - 20;
			WaveFormWidth = Mathf.Max(WaveFormWidth, windowWidth);

			if (AudioSourceLoaded != null && AudioClipLoaded != null && LipSyncLoaded != null)
			{

				if (Event.current.button == 2)
				{
					if (Mathf.Abs(Event.current.delta.x) > 0)
					{
						WaveFormPosX += (int)(Event.current.delta.x * .5f);

						WaveFormPosX = (int)Mathf.Clamp(WaveFormPosX, (int)_puppetFaceEditor.position.width - WaveFormWidth - 20, 1);

						RefreshNodes(Vector2.zero);
						Repaint();
					}
				}



				if (Event.current.type == EventType.ScrollWheel)
				{

					if (windowWidth - WaveFormWidth <= 0)
					{

						WaveFormWidth -= 100 * (int)Event.current.delta.y;
						WaveFormWidth = Mathf.Max(WaveFormWidth, windowWidth);
						WaveFormPosX = (int)Mathf.Clamp(WaveFormPosX, (int)_puppetFaceEditor.position.width - WaveFormWidth - 20, 1);

						RefreshNodes(Vector2.zero);
						Repaint();

					}
				}
			}



			int sampleCountOriginal = 1;
			int sampleCount = 1;

			if (audio != null)
			{
				sampleCountOriginal = audio.samples;
			}

			sampleCount = (int)((int)(audio.channels * sampleCountOriginal) * (((float)windowWidth) / (float)WaveFormWidth));

			int startVal = -1 * (int)(sampleCountOriginal * (((float)WaveFormPosX) / (float)WaveFormWidth));
			
			if (startVal < 0)
				startVal = 0;

			Texture2D tex = new Texture2D(windowWidth, height, TextureFormat.RGBA32, false);
			float[] samples = new float[sampleCount];
			float[] waveform = new float[windowWidth];
			int packSize = 1;
			if (audio != null)
			{
                if (audio.loadType == AudioClipLoadType.DecompressOnLoad)
                {
                    audio.GetData(samples, startVal);
                }
                
                packSize = (sampleCount / windowWidth) + 1;


            }
			int s = 0;
			for (int i = 0; i < sampleCount; i += packSize)
			{
				waveform[s] = Mathf.Abs(samples[i]);
				s++;
			}

			for (int x = 0; x < windowWidth; x++)
			{
				for (int y = 0; y < height; y++)
				{
					tex.SetPixel(x, y, new Color(0.35f, 0.35f, 0.35f, 1f));
				}
			}
			for (int x = 0; x < waveform.Length; x++)
			{
				for (int y = 0; y <= waveform[x] * ((float)height); y++)
				{
					tex.SetPixel(x, (height / 2) + y, col);
					tex.SetPixel(x, (height / 2) - y, col);
				}



			}
			tex.Apply();

			GUI.DrawTexture(new Rect(new Vector2(0, heightPad + 20), new Vector2(Mathf.Min(WaveFormWidth, (int)_puppetFaceEditor.position.width - 20), 100)), tex);
			if (audio != null)
			{
				for (int x = 0; x < audio.length; x++)
				{
					float val = ((float)x / (float)audio.length);

					Handles.DrawLine(new Vector3(WaveFormPosX + (float)val * WaveFormWidth, rect.min.y), new Vector3(widthPad + (float)val * WaveFormWidth, rect.min.y + 120));

					string timeEncoded = ((float)x).ToString("00") + ":00";
					GUI.Label(new Rect(new Vector2(WaveFormPosX + (float)val * WaveFormWidth, rect.min.y), 50 * Vector2.one), timeEncoded, _defaultStyle);

					float offset = (WaveFormWidth / (audio.length * 30f));
					for (int v = 0; v < 30; v++)
					{
						if (v % 30 == 0)
							Handles.color = new Color(1, 1, 1, .5f);
						else if (v % 30 == 15)
							Handles.color = new Color(1, 1, 1, .25f);
						else
							Handles.color = new Color(1, 1, 1, .05f);


						Handles.DrawLine(new Vector3(WaveFormPosX + (float)val * WaveFormWidth + v * offset, rect.min.y + 10), new Vector3(WaveFormPosX + (float)val * WaveFormWidth + v * offset, rect.min.y + 120));
					}
					Handles.color = new Color(1, 1, 1, 1f);
					int linePos = (int)((TimeVal * WaveFormWidth) / AudioClipLoaded.length);

					Handles.DrawLine(new Vector3(WaveFormPosX + linePos, heightPad + 20), new Vector3(WaveFormPosX + linePos, heightPad + 120));
					Handles.DrawLine(new Vector3(WaveFormPosX + linePos + 1, heightPad + 20), new Vector3(WaveFormPosX + linePos + 1, heightPad + 120));

				}
			}




			return tex;
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