using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using UnityEditor;
using System.Diagnostics;
using System;
using System.Globalization;

namespace PuppetFace
{
	
	[RequireComponent(typeof(AudioSource))]
	[AddComponentMenu("Puppet Face/LipSync")]
	public class LipSync : MonoBehaviour
	{

		public int LipSyncIndex = 0;
		public TextAsset[] LipSyncFiles;
		public AudioClip[] AudioClips;
		public int[] BlendShapeIndexes = new int[] { 99999, 99999, 99999, 99999, 99999, 99999, 99999, 99999, 99999 };
		public Transform[] FaceBones;

		public float Strength = 1f;

		public bool PlayOnAwake = false;
		public bool PlayAll = false;
		public bool Repeat = false;

		private bool isPlaying = false;
		public bool Initialized = false;
        public bool NewAudioAdded = false;

        public SkinnedMeshRenderer Skin;
		public LipSync[] LipSyncs = new LipSync[0];

		private AudioSource _audioSource;

		public List<ListFaceShapes> FaceShapes = new List<ListFaceShapes>();

		public List<ListTransformStates> TransformStates = new List<ListTransformStates>();

		[HideInInspector]
		public ListTransformStates _currentTransforms = new ListTransformStates();

		public float TimeVal = 0;
        public float _internalTimeVal = 0f;

        public bool IsPlaying
		{
			get
			{
				return isPlaying;
			}

			set
			{
				isPlaying = value;
			}
		}

		// Use this for initialization
		void Awake()
		{
			#if UNITY_EDITOR

			if (!Initialized)
				InitializeFromFile();
			else
			#endif
				_audioSource = GetComponent<AudioSource>();

			if (PlayOnAwake)
				Play(LipSyncIndex);
		}
		private void LateUpdate()
		{
			if (IsPlaying)
			{
				EvaluateLipSync(LipSyncIndex);
				if (TimeVal >= _audioSource.clip.length)
				{
					if (LipSyncIndex + 1 < AudioClips.Length)
					{
						if (PlayAll)
						{
							LipSyncIndex++;
							Play(LipSyncIndex, 0f);

						}
						else if (Repeat)
						{
							Play(LipSyncIndex);
						}
						else
						{
							Stop();
						}
					}
					else if (Repeat)
					{
						LipSyncIndex = 0;
						Play(LipSyncIndex);
					}
					else
					{
						Stop();
					}
				}
			}
            if (_internalTimeVal != TimeVal)
            {
                _internalTimeVal = TimeVal;
                SetPhoneme(LipSyncIndex, TimeVal);
            }
        }
		public void Play(int index, float startTime = 0f)
		{
			for (int i = 0; i < LipSyncs.Length; i++)
			{
				LipSyncs[i].Play(index, startTime);
			}
			if (_audioSource!=null && AudioClips != null && index < AudioClips.Length && index>=0)
			{
				_audioSource.clip = AudioClips[index];
				_audioSource.Play();
				if (_audioSource.time >= startTime && startTime >=0f)
				{
					_audioSource.time = startTime;
					TimeVal = startTime;
				}
				IsPlaying = true;
			}
		}
		public void EvaluateLipSync(int index)
		{
			SetPhoneme(index, TimeVal);
			TimeVal += Time.deltaTime;
		}
		public void Stop()
		{
			for (int i = 0; i < LipSyncs.Length; i++)
			{
				LipSyncs[i].Stop();
			}
			IsPlaying = false;
			TimeVal = 0f;
			_audioSource.Stop();

		}
		public void InitializeFromData(ListFaceShapes FaceShapesData, int index)
		{
			for (int i = 0; i < LipSyncs.Length; i++)
			{
				LipSyncs[i].LipSyncFiles = LipSyncFiles;
				LipSyncs[i].AudioClips = AudioClips;
				LipSyncs[i].InitializeFromData(FaceShapesData, index);
			}
            if (FaceShapes.Count > index)
            {
                FaceShapes[index] = FaceShapesData;
                #if UNITY_EDITOR
                EditorUtility.SetDirty(this);
                #endif
            }
        }
		   #if UNITY_EDITOR
		public void InitializeFromFile()
		{
			for (int i = 0; i < LipSyncs.Length; i++)
			{
				LipSyncs[i].LipSyncFiles = LipSyncFiles;
				LipSyncs[i].AudioClips = AudioClips;
				LipSyncs[i].InitializeFromFile();
			}
			FaceShapes.Clear();
			for (int t = 0; t < LipSyncFiles.Length; t++)
			{
				string path = AssetDatabase.GetAssetPath(LipSyncFiles[t]);// "Assets/LipSync/output.xml";
                //UnityEngine.Debug.Log("path before " + path);
                path = Application.dataPath + path.Substring(6);
                //UnityEngine.Debug.Log("path after " + path);

                if (new FileInfo(path).Length == 0)
				{
					continue;
				}
				var stream = new StreamReader(path);
				XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
				xmlDoc.LoadXml(stream.ReadToEnd());
				XmlNodeList mouthCueList = xmlDoc.GetElementsByTagName("mouthCues");
				ListFaceShapes newFaceShape = new ListFaceShapes();
				foreach (XmlNode mouthCue in mouthCueList)
				{
					XmlNodeList mouthCuesList = xmlDoc.GetElementsByTagName("mouthCue");
					foreach (XmlNode mouthCues in mouthCuesList)
					{
						float startVal = float.Parse(mouthCues.Attributes["start"].Value, CultureInfo.InvariantCulture);
						float endVal = float.Parse(mouthCues.Attributes["end"].Value, CultureInfo.InvariantCulture);
						string mouthShape = mouthCues.ChildNodes[0].Value;
						float bsID = -1;
						if (mouthCues.Attributes["blendShapeID"] != null)
						{
							bsID = float.Parse(mouthCues.Attributes["blendShapeID"].Value, CultureInfo.InvariantCulture);
						}
						newFaceShape.Add(new FaceShape(startVal, endVal, mouthShape, 1f, (int)bsID));
					}
				}
				FaceShapes.Add(newFaceShape);

			}
			
			_audioSource = GetComponent<AudioSource>();
			_audioSource.playOnAwake = false;
			Initialized = true;
			
			
		}
#endif
		private int GetIndexFromMouthShape(string mouthShape)
		{			
			switch (mouthShape)
			{
				case "A":
					return BlendShapeIndexes[0];
				case "B":
					return BlendShapeIndexes[1];
				case "C":
					return BlendShapeIndexes[2];
				case "D":
					return BlendShapeIndexes[3];
				case "E":
					return BlendShapeIndexes[4];
				case "F":
					return BlendShapeIndexes[5];
				case "G":
					return BlendShapeIndexes[6];
				case "H":
					return BlendShapeIndexes[7];
				case "X":
					return BlendShapeIndexes[8];

			}
			return 0;
		}
		private int GetOrderFromMouthShape(string mouthShape )
		{
			switch (mouthShape)
			{
				case "A":
					return 0;
				case "B":
					return 1;
				case "C":
					return 2;
				case "D":
					return 3;
				case "E":
					return 4;
				case "F":
					return 5;
				case "G":
					return 6;
				case "H":
					return 7;
				case "X":
					return 8;

			}
			return 0;
		}
#if UNITY_EDITOR
		public string[] GetPhonemes(int index)
		{
			List<string> phonemeList = new List<string>();
			string path = AssetDatabase.GetAssetPath(LipSyncFiles[index]);// "Assets/LipSync/output.xml";
			StreamReader stream = new StreamReader(path);
			if (new FileInfo(path).Length == 0)
			{
				return phonemeList.ToArray();
			}
			XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
			xmlDoc.LoadXml(stream.ReadToEnd());
			XmlNodeList mouthCueList = xmlDoc.GetElementsByTagName("mouthCues");
			foreach (XmlNode mouthCue in mouthCueList)
			{
				XmlNodeList mouthCuesList = xmlDoc.GetElementsByTagName("mouthCue");
				foreach (XmlNode mouthCues in mouthCuesList)
				{
					float startVal = float.Parse(mouthCues.Attributes["start"].Value, CultureInfo.InvariantCulture);
					float endVal = float.Parse(mouthCues.Attributes["end"].Value, CultureInfo.InvariantCulture);
					float strength = 1f;
					if (mouthCues.Attributes["strength"] !=null)
						strength = float.Parse(mouthCues.Attributes["strength"].Value, CultureInfo.InvariantCulture);
					string mouthShape = mouthCues.ChildNodes[0].Value;

                    float bsID = -1;
                    if (mouthCues.Attributes["blendShapeID"] != null)
                    {
                        bsID = float.Parse(mouthCues.Attributes["blendShapeID"].Value, CultureInfo.InvariantCulture);
                    }

                    phonemeList.Add(startVal + ";" + endVal + ";" + mouthShape + ";" + strength + ";" + bsID);
				}
			}
			return phonemeList.ToArray();
		}
#endif
		public void ResetPhonemeBones()
		{
			if (TransformStates.Count > 0)
			{
                for (int f = 0; f < FaceBones.Length; f++)
				{
                    _currentTransforms.transformStates[f].position = TransformStates[8].transformStates[f].position;
					_currentTransforms.transformStates[f].rotation = TransformStates[8].transformStates[f].rotation;
					_currentTransforms.transformStates[f].scale = TransformStates[8].transformStates[f].scale;


				}
			}
		}

		public void SetPhonemeOnly(int index, float amount)
		{
			if (!float.IsNaN(amount))
			{
				int blendShapeIndex = BlendShapeIndexes[index];

				Skin.SetBlendShapeWeight(blendShapeIndex, amount); if (TransformStates[index].transformStates.Count > 0)
				{
					for (int i = 0; i < FaceBones.Length; i++)
					{
						_currentTransforms.transformStates[i].position = Vector3.Lerp(_currentTransforms.transformStates[i].position, TransformStates[index].transformStates[i].position, amount / 100f);
						_currentTransforms.transformStates[i].rotation = Quaternion.Lerp(_currentTransforms.transformStates[i].rotation, TransformStates[index].transformStates[i].rotation, amount / 100f);
						_currentTransforms.transformStates[i].scale = Vector3.Lerp(_currentTransforms.transformStates[i].scale, TransformStates[index].transformStates[i].scale, amount / 100f);

						FaceBones[i].localPosition = _currentTransforms.transformStates[i].position;
						FaceBones[i].localRotation = _currentTransforms.transformStates[i].rotation;
						FaceBones[i].localScale = _currentTransforms.transformStates[i].scale;


					}
				}
			}
		}

		public void SetPhoneme(int index, float timeVal)
		{
			
			for (int i = 0; i < LipSyncs.Length; i++)
			{				
				LipSyncs[i].SetPhoneme(index, timeVal);
			}
			if (FaceShapes.Count > 0)
			{

				foreach (FaceShape f in FaceShapes[index].faceShapes)
				{
					int blendShapeIndex = GetIndexFromMouthShape(f.shapeName);
					if(Skin.sharedMesh.blendShapeCount > blendShapeIndex)
						Skin.SetBlendShapeWeight(blendShapeIndex, 0);
					if(f.blendShapeIndex>=0)
						Skin.SetBlendShapeWeight(f.blendShapeIndex, 0);



				}
				if (TransformStates.Count > 0)
				{
					for (int f = 0; f < FaceBones.Length; f++)
					{

						_currentTransforms.transformStates[f].position = TransformStates[8].transformStates[f].position;
						_currentTransforms.transformStates[f].rotation = TransformStates[8].transformStates[f].rotation;
						_currentTransforms.transformStates[f].scale = TransformStates[8].transformStates[f].scale;


					}
				}

				foreach (FaceShape f in FaceShapes[index].faceShapes)
				{
					float shapeDuration = (f.end - f.start);

					float end = (f.end - f.start) * (1.35f - 0.35f * Mathf.Clamp01(shapeDuration)) + f.start;
					float start = (f.start - f.end) * (1.35f - 0.35f * Mathf.Clamp01(shapeDuration)) + f.end;
					if (end >= timeVal && timeVal >= start)
					{

						float t = (timeVal - start) / (end - start);
						t = Mathf.Sin(t * Mathf.PI) * Strength * f.strength * 100f;
						int blendShapeIndex = GetIndexFromMouthShape(f.shapeName);
						

						if (f.blendShapeIndex >= 0)
						{
							Skin.SetBlendShapeWeight(f.blendShapeIndex, t);
						}
						else if (Skin.sharedMesh.blendShapeCount > blendShapeIndex)
							Skin.SetBlendShapeWeight(blendShapeIndex, t);

						if (TransformStates.Count > 0)
						{
							int order = GetOrderFromMouthShape(f.shapeName);
							if (TransformStates[order].transformStates.Count > 0)
							{
								for (int i = 0; i < FaceBones.Length; i++)
								{
									_currentTransforms.transformStates[i].position = Vector3.Lerp(_currentTransforms.transformStates[i].position, TransformStates[order].transformStates[i].position, t/100f);
									_currentTransforms.transformStates[i].rotation = Quaternion.Lerp(_currentTransforms.transformStates[i].rotation, TransformStates[order].transformStates[i].rotation, t / 100f);
									_currentTransforms.transformStates[i].scale = Vector3.Lerp(_currentTransforms.transformStates[i].scale, TransformStates[order].transformStates[i].scale, t / 100f);
																 
									FaceBones[i].localPosition = _currentTransforms.transformStates[i].position;
									FaceBones[i].localRotation = _currentTransforms.transformStates[i].rotation;
									FaceBones[i].localScale = _currentTransforms.transformStates[i].scale;
									
								}
							}
						}

					}

				}
			}
			else
				Initialized = false;
		}
		public void SetIndex(int index)
		{
			LipSyncIndex = Mathf.Clamp(index, 0, LipSyncFiles.Length-1);
			for (int i = 0; i < LipSyncs.Length; i++)
			{
				LipSyncs[i].SetIndex(index);
			}
		}

		public int getBlendShapeName(string val)
		{
			Mesh m = Skin.sharedMesh;
			
			for (int i = 0; i < m.blendShapeCount; i++)
			{
				string s = m.GetBlendShapeName(i);
				if (s == val)
					return i;
			}
			return -1;
		}
#if UNITY_EDITOR
		static public string ConvertAudioToPhoneme(AudioClip audioclip)
		{
			string audioFilePath = "";
			
			string fullPathAudio;
			if (audioclip == null) fullPathAudio = "Assets";
			else fullPathAudio = AssetDatabase.GetAssetPath(audioclip);
			string fullPathOutput = fullPathAudio.Replace(".wav", ".xml");
			audioFilePath = fullPathOutput;
            string puppetFacePath = RecursivelyFindFolderPath();
            string fullPath = puppetFacePath + "/Tools/LipSync/rhubarb/rhubarb.exe ";
			System.IO.File.WriteAllText(audioFilePath, "");

			ProcessStartInfo startInfo = new ProcessStartInfo(fullPath);
			startInfo.WindowStyle = ProcessWindowStyle.Normal;
			startInfo.Arguments = "-o \"" + fullPathOutput + "\" -f xml -r phonetic \"" + fullPathAudio + "\"";
			Process.Start(startInfo);
			
			return audioFilePath;

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
#endif
    }
    [Serializable]
    public class FaceShape
    {
        public float start;
        public float end;
        public string shapeName;
        public float strength;
        public int blendShapeIndex;
        public FaceShape(float s, float e, string shape, float strengthVal, int blendShapeId = -1)
        {
            start = s;
            end = e;
            shapeName = shape;
            strength = strengthVal;
            blendShapeIndex = blendShapeId;
        }

    }
    [Serializable]
    public class ListFaceShapes
    {
        public List<FaceShape> faceShapes = new List<FaceShape>();
        public FaceShape this[int key]
        {
            get
            {
                return faceShapes[key];
            }
            set
            {
                faceShapes[key] = value;
            }
        }
        public void Add(FaceShape fs)
        {
            faceShapes.Add(fs);
        }
    }
    [Serializable]
    public class TransformState
    {
        public Vector3 position = Vector3.zero;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 scale = Vector3.one;


        public TransformState(Vector3 pos, Quaternion rot, Vector3 scal)
        {
            position = pos;
            rotation = rot;
            scale = scal;

        }

    }
    [Serializable]
    public class ListTransformStates
    {
        public List<TransformState> transformStates = new List<TransformState>();
        public TransformState this[int key]
        {
            get
            {
                return transformStates[key];
            }
            set
            {
                transformStates[key] = value;
            }
        }
        public void Add(TransformState fs)
        {
            transformStates.Add(fs);
        }
    }

}
