
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using OpenCvSharp.Demo;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif
using UnityEditor;

namespace PuppetFace
{
	public enum HeadOrientation { XYZ, XZY, YXZ, YZX, ZXY, ZYX };
	[AddComponentMenu("Puppet Face/PerformanceCapture")]
	public class PerformanceCapture : WebCamera
	{		
		
		public TextAsset faces;
		public TextAsset eyes;
		public TextAsset shapes;
		private Transform[] _trackers;
		public bool MakeTrackers = false;

		public SkinnedMeshRenderer Skin;
		public FaceProcessorLive<WebCamTexture> processor;

		[Header("Open")]
		public bool Open = true;
		public Vector2 OpenMinMax = new Vector2(10f, 60f);
		public Queue<float> OpenMinMaxAverage = new Queue<float>();
		public int OpenMouthIndex = 0;

		[Header("Oh")]
		public bool Oh = true;
		public Vector2 OhMinMax = new Vector2(110f, 140f);
		public Queue<float> OhMinMaxAverage = new Queue<float>();
		public int OhMouthIndex = 1;

		[Header("Corners")]
		public bool Corners = false;
		public Vector2 CornersMinMax = new Vector2(-10f, 15f);
		public Queue<float> CornersMinMaxAverage = new Queue<float>();
		public int CornersMouthIndex = 2;

		[Header("Head")]
		public bool HeadRot = true;
		public Vector3 RotationLimits = new Vector3(30f, 60f, 45f);
		public int HeadSmoothing = 7;
		public Queue<Vector3> rotationAverage = new Queue<Vector3>();
		public Vector3 HeadForwardDirection = new Vector3(0f,0f,1f);
		public Quaternion HeadForwardBaked = Quaternion.identity;
		public HeadOrientation Orientation;
		[Header("Jaw")]
		public bool Jaw = false;
		public Vector2 JawMinMax = new Vector2(180f, 210f);
		public Queue<float> JawMinMaxAverage = new Queue<float>();
		public Transform JawBone ;
		public Quaternion JawDefault = Quaternion.identity;
		public Quaternion JawOpen = Quaternion.identity;

		[Header("Brows")]
		public bool EyeBrows = false;
		public Vector2 EyeBrowsMinMax = new Vector2(-10f, 15f);
		public Queue<float> EyeBrowsMinMaxAverage = new Queue<float>();
		public int EyeBrowsIndex = 2;



		public bool CalibrateBase = false;
		public bool CalibrateOpen = false;
		public bool CalibrateOh = false;
		public bool CalibrateCorners = false;
		public bool CalibrateEyeBrows = false;

		public int OpenBlendFrameCount, OpenSmoothing = 4;
		public int OhSmoothing = 4;
		public int CornersSmoothing = 4;
		public int EyeBrowsSmoothing = 4;

		public int MicrophoneDeviceIndex = 0;
		public int MinFrequency =10;
		public int MaxFrequency = 44100;


		public Transform RotBone;
		public Text Text;
		public Text TextOh;
		public Text TextCorner;

		private bool isCalibrating = false;
		private float OpenCallibrate = 0f;


		public AnimationClip clip;
		#if UNITY_EDITOR
		private GameObjectRecorder m_Recorder;
		private AudioSource audioSource;
		private GameObject _animationRoot;


#endif
		public bool isRecording = false;
		public List<float> tempRecording = new List<float>();
		public float[] recordedClip;

		public Animator Anim;

		
		/// <summary>
		/// Default initializer for MonoBehavior sub-classes
		/// </summary>
		protected override void Awake()
		{
			base.Awake();
			base.forceFrontalCamera = true; // we work with frontal cams here, let's force it for macOS s MacBook doesn't state frontal cam correctly

			byte[] shapeDat = shapes.bytes;
			if (shapeDat.Length == 0)
			{
				string errorMessage =
					"In order to have Face Landmarks working you must download special pre-trained shape predictor " +
					"available for free via DLib library website and replace a placeholder file located at " +
					"\"OpenCV+Unity/Assets/Resources/shape_predictor_68_face_landmarks.bytes\"\n\n" +
					"Without shape predictor demo will only detect face rects.";

			#if UNITY_EDITOR
				// query user to download the proper shape predictor
				if (UnityEditor.EditorUtility.DisplayDialog("Shape predictor data missing", errorMessage, "Download", "OK, process with face rects only"))
					Application.OpenURL("http://dlib.net/files/shape_predictor_68_face_landmarks.dat.bz2");
			#else
             UnityEngine.Debug.Log(errorMessage);
			#endif
			}

			processor = new FaceProcessorLive<WebCamTexture>();
			processor.Initialize(faces.text, eyes.text, shapes.bytes);

			processor.DataStabilizer.Enabled = true;        // enable stabilizer
			processor.DataStabilizer.Threshold = 1.0;       // threshold value in pixels
			processor.DataStabilizer.SamplesCount = 2;      // how many samples do we need to compute stable data

			processor.Performance.Downscale = 256;          // processed image is pre-scaled down to N px by long side
			processor.Performance.SkipRate = 1;             // we actually process only each Nth frame (and every frame for skipRate = 0)
			if (MakeTrackers)
			{
				_trackers = new Transform[69];
				for (int i = 0; i < 69; i++)
				{
					_trackers[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
					_trackers[i].name = ("tracker " + i);
					_trackers[i].parent = transform;
				}
			}
			#if UNITY_EDITOR
			audioSource = GetComponent<AudioSource>();

			if (Anim == null)
			{
				Debug.LogWarning("Please select an animator to save the animation caputure");
				return;
			}
			_animationRoot = Anim.gameObject;
			
			m_Recorder = new GameObjectRecorder(_animationRoot);
			if(JawBone!=null)
				m_Recorder.BindComponentsOfType<Transform>(JawBone.gameObject, false);
			if (RotBone != null)
				m_Recorder.BindComponentsOfType<Transform>(RotBone.gameObject, false);
			if (JawBone != null)
				m_Recorder.BindComponentsOfType<Transform>(JawBone.gameObject, false);
			
			string path = AnimationUtility.CalculateTransformPath(transform, Anim.transform);

			if (Skin != null)
			{

				if (OpenMouthIndex < Skin.sharedMesh.blendShapeCount)
				{
					string OpenName = Skin.sharedMesh.GetBlendShapeName(OpenMouthIndex);

					EditorCurveBinding ecb = EditorCurveBinding.FloatCurve(path, typeof(SkinnedMeshRenderer), "blendShape." + OpenName);
					m_Recorder.Bind(ecb);
				}
				if (OhMouthIndex < Skin.sharedMesh.blendShapeCount)
				{
					string OhName = Skin.sharedMesh.GetBlendShapeName(OhMouthIndex);

					EditorCurveBinding ecb1 = EditorCurveBinding.FloatCurve(path, typeof(SkinnedMeshRenderer), "blendShape."+OhName);
					m_Recorder.Bind(ecb1);
				}
				if (CornersMouthIndex < Skin.sharedMesh.blendShapeCount)
				{
					string CornersName = Skin.sharedMesh.GetBlendShapeName(CornersMouthIndex);

					EditorCurveBinding ecb2 = EditorCurveBinding.FloatCurve(path, typeof(SkinnedMeshRenderer), "blendShape." + CornersName);
					m_Recorder.Bind(ecb2);
				}
			}
			#endif
		}
#if UNITY_EDITOR

		void ResizeRecording()
		{
			if (isRecording)
			{
				int length = 441000;
				float[] clipData = new float[length];
				audioSource.clip.GetData(clipData, 0);
				tempRecording.AddRange(clipData);
				Invoke("ResizeRecording", 10);
			}
		}
		public void StartRecording()
		{
			if (Microphone.devices.Length > 0)
			{
				audioSource.clip = Microphone.Start(Microphone.devices[MicrophoneDeviceIndex], true, 10, Mathf.Min(MaxFrequency, 44100));
				Invoke("ResizeRecording", 10);
			}
		}
		public void EndRecording()
		{
			int length = Microphone.GetPosition(Microphone.devices[MicrophoneDeviceIndex]);

			Microphone.End(null);
			float[] clipData = new float[length];
			audioSource.clip.GetData(clipData, 0);

			
			float[] fullClip = new float[clipData.Length + tempRecording.Count];
			for (int i = 0; i<fullClip.Length; i++)
			{
				if (i<tempRecording.Count)
					fullClip[i] = tempRecording[i];
				else
					fullClip[i] = clipData[i - tempRecording.Count];
			}

			recordedClip = fullClip;
			audioSource.clip = AudioClip.Create("recorded samples", fullClip.Length, 1, 44100, false);
			audioSource.clip.SetData(fullClip, 0);
			audioSource.loop = true;

			clip = new AnimationClip();
			AssetDatabase.SaveAssets();
			m_Recorder.SaveToClip(clip);
			string animName = AssetDatabase.GenerateUniqueAssetPath("Assets/" + _animationRoot.name + "_FaceCapture.anim");
			string audioName = AssetDatabase.GenerateUniqueAssetPath("Assets/" + _animationRoot.name + "_AudioCapture.wav");

			AssetDatabase.CreateAsset(clip, animName);

			SavWav.Save(audioName, audioSource.clip);

			

		}
#endif




		void Calibrate(float avOpen, Vector3 avRot, float avOh, float avCorners,float avJaw, float avEyebrows)
		{
			if (!isCalibrating  && CalibrateBase)
			{
				isCalibrating = true;
			}
			if (isCalibrating && !CalibrateBase)
			{
				isCalibrating = false;
				OpenCallibrate = 0f;
				OpenBlendFrameCount = OpenSmoothing;
				OpenMinMaxAverage.Clear();
			}
			if (CalibrateOpen)
			{
				if(!float.IsNaN(avOpen))
					OpenMinMax = new Vector2(OpenMinMax.x, avOpen);
				if (!float.IsNaN(avJaw))
					JawMinMax = new Vector2(JawMinMax.x, avJaw);
			}
			if (CalibrateOh)
			{
				if (!float.IsNaN(avOh))
					OhMinMax = new Vector2(avOh, OhMinMax.y);
			}
			if (CalibrateCorners)
			{
				if (!float.IsNaN(avCorners))
					CornersMinMax = new Vector2(avCorners, CornersMinMax.y);
			}
			if (CalibrateEyeBrows)
			{
				if (!float.IsNaN(avCorners))
					EyeBrowsMinMax = new Vector2(avEyebrows, EyeBrowsMinMax.y);
			}

			if (isCalibrating)
			{
				OpenBlendFrameCount = 20;
				if (OpenCallibrate < avOpen)
				{
					OpenCallibrate = avOpen;
				}
				if (!float.IsNaN(OpenCallibrate))
					OpenMinMax = new Vector2(OpenCallibrate, OpenMinMax.y );
				if (!float.IsNaN(avOh))
					OhMinMax = new Vector2(OhMinMax.x, avOh);
				if (!float.IsNaN(avCorners))
					CornersMinMax = new Vector2(CornersMinMax.x, avCorners);
				if (!float.IsNaN(avJaw))
					JawMinMax = new Vector2(avJaw, JawMinMax.y);
				if (!float.IsNaN(avEyebrows))
					EyeBrowsMinMax = new Vector2(EyeBrowsMinMax.x, avEyebrows);


			}

		}
#if UNITY_EDITOR
		private void LateUpdate()
		{
			if (isRecording)
			{
				m_Recorder.TakeSnapshot(Time.deltaTime);

			}
		}
#endif
		public static Quaternion LookAtDirection(GameObject looker, Vector3 lookAtDirection, Vector3 lookerForwardVector)
		{
			Transform inputTransform = looker.transform;
			inputTransform.rotation = Quaternion.LookRotation(lookAtDirection); //calculating look rotation

			inputTransform.Rotate(GetForwardVectorAngleOffest(lookerForwardVector), Space.Self);

			return inputTransform.rotation;
		}

		static Vector3 GetForwardVectorAngleOffest(Vector3 forwardVector)
		{
			if (forwardVector == Vector3.up)
				return new Vector3(90, 0, 0);

			if (forwardVector == Vector3.right)
				return new Vector3(0, -90, 0);

			return new Vector3(0, 0, 0); ;
		}
		/// <summary>
		/// Per-frame video capture processor
		/// </summary>
		protected override bool ProcessTexture(WebCamTexture input, ref Texture2D output)
		{
			

			if (processor.Faces.Count > 0)
			{
				if (MakeTrackers)
				{
					for (int i = 0; i < processor.Faces[0].Marks.Length; i++)
					{
						_trackers[i].position = new Vector3(-processor.Faces[0].Marks[i].X, -processor.Faces[0].Marks[i].Y, 0);
					}
				}

				if (processor.Faces[0].Marks.Length > 0)
				{
					float localScale = (float)processor.Faces[0].Region.Height;

					float rotY = (processor.Faces[0].Marks[29].X / localScale) * 500f;
					float rotYMin = (processor.Faces[0].Marks[1].X / localScale) * 500f;
					float rotYMaxMin = (((processor.Faces[0].Marks[15].X / localScale) * 500f - (processor.Faces[0].Marks[1].X / localScale) * 500f));


					float rotX1 = (Mathf.Abs((processor.Faces[0].Marks[1].Y / localScale) * 500f + (processor.Faces[0].Marks[15].Y / localScale) * 500f)) * .5f;
					float rotX2 = (rotX1 - (processor.Faces[0].Marks[29].Y / localScale) * 500f);

					Vector2 rotZ = new Vector2(processor.Faces[0].Marks[27].X, processor.Faces[0].Marks[27].Y) - new Vector2(processor.Faces[0].Marks[30].X, processor.Faces[0].Marks[30].Y);

					float rotXBlend = (rotX2 * .01f) * .5f + .5f;
					float rotYBlend = (rotY - rotYMin) / rotYMaxMin;

					float rotationX = Mathf.Lerp(RotationLimits.x, -RotationLimits.x, rotXBlend);
					float rotationY = Mathf.Lerp(RotationLimits.y, -RotationLimits.y, rotYBlend);
					float rotationZ = Mathf.Lerp(-RotationLimits.z, RotationLimits.z, .5f + .5f * Vector2.Dot(rotZ.normalized, Vector2.right));

					float mouthOpenAmount = 0f;
					float avOpen = 0f;
					float avOh = 0f;
					float avCorners = 0f;
					float avJaw = 0f;
					float avEyebrows = 0f;

					Vector3 avRot = Vector3.zero;

					if (HeadRot)
					{

						if (rotationAverage.Count > HeadSmoothing)
						{
							rotationAverage.Dequeue();
						}
						rotationAverage.Enqueue(new Vector3(rotationX, rotationY, rotationZ));
						avRot = Vector3.zero;
						float i = 0f;
						foreach (Vector3 value in rotationAverage)
						{
							if (!float.IsNaN(value.x))
							{
								avRot += value;
								i += 1f;
							}
						}
						if (i > 0f)
							avRot /= i;
						rotationX = avRot.x;
						rotationY = avRot.y;
						rotationZ = avRot.z;

						RotBone.rotation = Quaternion.Euler(avRot)* HeadForwardBaked;
						
					}



					if (Open && Skin!=null)
					{
						float openMin = ((0.5f * ((processor.Faces[0].Marks[52].Y) + (processor.Faces[0].Marks[63].Y))) / localScale) * 500f;
						float openMax = ((0.5f * ((processor.Faces[0].Marks[67].Y) + (processor.Faces[0].Marks[58].Y))) / localScale) * 500f;
						if (OpenMinMaxAverage.Count > OpenBlendFrameCount)
						{
							OpenMinMaxAverage.Dequeue();
						}
						OpenMinMaxAverage.Enqueue(openMax - openMin);
						avOpen = 0f;
						float i = 0f;
						foreach (float value in OpenMinMaxAverage)
						{
							if (!float.IsNaN(value))
							{
								avOpen += value;
								i += 1f;
							}
						}
						if(avOpen>0)
							avOpen /= i;
						avOpen -= Mathf.Abs(rotationX);

						mouthOpenAmount = Mathf.Clamp01(((avOpen - OpenMinMax.x) / (OpenMinMax.y - OpenMinMax.x)));
						if(OpenMouthIndex< Skin.sharedMesh.blendShapeCount)
							Skin.SetBlendShapeWeight(OpenMouthIndex, 100f * Mathf.Clamp01(mouthOpenAmount));
						if(JawBone !=null)
							JawBone.localRotation = Quaternion.Lerp(JawDefault, JawOpen, Mathf.Clamp01(mouthOpenAmount));

					}
					

					if (Oh && Skin != null)
					{
						float openMin = (processor.Faces[0].Marks[49].X / localScale) * 500f;
						float openMax = (processor.Faces[0].Marks[55].X / localScale) * 500f;
						if (OhMinMaxAverage.Count > OhSmoothing)
						{
							OhMinMaxAverage.Dequeue();
						}						
						OhMinMaxAverage.Enqueue(openMax - openMin);
						avOh = 0f;
						float i = 0f;
						foreach (float value in OhMinMaxAverage)
						{
							avOh += value;
							i += 1f;
						}
						if(avOh>0)
							avOh /= i;


						float mouthOhAmount = Mathf.Clamp01((avOh - OhMinMax.x) / Mathf.Abs(OhMinMax.y - OhMinMax.x));
						if (OhMouthIndex < Skin.sharedMesh.blendShapeCount)
							Skin.SetBlendShapeWeight(OhMouthIndex, 100f * (Mathf.Pow(1f - Mathf.Clamp01(mouthOhAmount) , 1f) ));
					}

					TrackPoints(processor.Faces[0].Marks[52].Y , processor.Faces[0].Marks[49].Y, rotationY, localScale, ref avCorners, ref Corners, ref CornersMinMaxAverage, ref CornersMinMax, ref CornersMouthIndex);
					
					TrackPoints(processor.Faces[0].Marks[29].Y, processor.Faces[0].Marks[24].Y, rotationY, localScale, ref avEyebrows, ref EyeBrows, ref EyeBrowsMinMaxAverage, ref EyeBrowsMinMax, ref EyeBrowsIndex);

					Calibrate(avOpen, avRot, avOh, avCorners, avJaw, avEyebrows);
				}

				

			}
			processor.ProcessTexture(input, TextureParameters);

			processor.MarkDetected();


			output = OpenCvSharp.Unity.MatToTexture(processor.Image, output);   // if output is valid texture it's buffer will be re-used, otherwise it will be re-created

			return true;
		}
		public void TrackPoints(float inputMin, float inputMax, float rotationY, float localScale, ref float avCorners, ref bool toggle, ref Queue<float> CornersMinMaxAverage, ref Vector2 cornersMinMax, ref int BlendShapeIndex)
        {
            if (toggle && Skin != null)
			{
				float openMin = (inputMin / localScale) * 500f;
				float openMax = (inputMax / localScale) * 500f;
				if (CornersMinMaxAverage.Count > CornersSmoothing)
				{
                   CornersMinMaxAverage.Dequeue();
				}

				CornersMinMaxAverage.Enqueue(openMax - openMin);
				avCorners = 0f;
				float i = 0f;
				foreach (float value in CornersMinMaxAverage)
				{
					avCorners += value;
					i += 1f;
				}
				if (avCorners > 0)
					avCorners /= i;

				avCorners -= Mathf.Abs(rotationY);
                float mouthCornerAmount = Mathf.Clamp01(((avCorners) - cornersMinMax.x) / (cornersMinMax.y - cornersMinMax.x) /*+ 2f * (rotationX/ (2*RotationLimits.x))*/);
				if (BlendShapeIndex < Skin.sharedMesh.blendShapeCount)
					Skin.SetBlendShapeWeight(BlendShapeIndex, 100f * (1f - Mathf.Clamp01(mouthCornerAmount)));

			}
		}
	}
}
