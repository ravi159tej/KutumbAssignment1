using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace PuppetFace
{
	[CustomEditor(typeof(EyeMotion))]
	public class EyeMotionEditor : Editor
	{
		// Start is called before the first frame update
		public EyeMotion em;
		public bool LookAround = false;
		public bool UpDown = false;
		public bool Blink = false;
        private static string _puppetFacePath;

        void OnEnable()
		{
			em = (EyeMotion)target;
            _puppetFacePath = "Assets" + RecursivelyFindFolderPath().Substring(Application.dataPath.Length);

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
		// Update is called once per frame
		public override void OnInspectorGUI()
		{
			
			if (em.LookAtTarget == null)
			{
				GUILayout.Space(10);
				string LogoPath = _puppetFacePath +"/Textures/GUI/PuppetFaceLogo.png";
				Texture LogoAsset = AssetDatabase.LoadAssetAtPath(LogoPath, typeof(Texture)) as Texture;
				GUILayout.BeginHorizontal();

				GUILayout.BeginVertical();

				em.Head = (Transform)EditorGUILayout.ObjectField("Head", em.Head, typeof(Transform), true);
				em.LookAtTarget = (Transform)EditorGUILayout.ObjectField("LookAt Target", em.LookAtTarget, typeof(Transform), true);

				GUILayout.EndVertical();

				GUILayout.Label(LogoAsset, GUILayout.Width(40), GUILayout.Height(40));
				GUILayout.EndHorizontal();



			

				if (GUILayout.Button("CREATE LOOKAT TARGET", GUILayout.Width(100), GUILayout.Height(25), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true)))
				{
					GameObject lookatTarget =new GameObject();
					lookatTarget.AddComponent<LookAtControl>();
					lookatTarget.name = "Look At Target";
					em.LookAtTarget = lookatTarget.transform;
					if(em.Head!=null)
						lookatTarget.transform.position = em.Head.position + Vector3.forward * em.Head.position.y*10f;
					Selection.activeGameObject = lookatTarget;
				}
			}
			else
			{
				//DrawDefaultInspector();

				GUILayout.Space(10);
				string LogoPath = _puppetFacePath +"/Textures/GUI/PuppetFaceLogo.png";
				Texture LogoAsset = AssetDatabase.LoadAssetAtPath(LogoPath, typeof(Texture)) as Texture;
				GUILayout.BeginHorizontal();

				GUILayout.BeginVertical();

				em.FaceSkin = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("FaceSkin", em.FaceSkin, typeof(SkinnedMeshRenderer), true);
				em.LookAround = EditorGUILayout.Toggle("Look Around", em.LookAround);

				GUILayout.EndVertical();

				GUILayout.Label(LogoAsset, GUILayout.Width(40), GUILayout.Height(40));
				GUILayout.EndHorizontal();

				

				if (em.LookAround)
				{
					GUILayout.BeginVertical(EditorStyles.helpBox);
					LookAround = EditorGUILayout.Foldout(LookAround, "Look Around");
					if (LookAround)
					{
						EditorGUI.BeginChangeCheck();
						em.EyeL = (Transform)EditorGUILayout.ObjectField("EyeL", em.EyeL, typeof(Transform), true);
						if (EditorGUI.EndChangeCheck())
						{
							if (em.EyeL != null)
							{
								Quaternion r1 = em.EyeL.rotation;
								em.EyeL.LookAt(em.LookAtTarget);
								Quaternion r2 = em.EyeL.rotation;
								em.EyeLDefault = r1 * Quaternion.Inverse(r2);
								em.EyeL.rotation = r1;

							}
						}

						EditorGUI.BeginChangeCheck();
						em.EyeR = (Transform)EditorGUILayout.ObjectField("EyeR", em.EyeR, typeof(Transform), true);
						if (EditorGUI.EndChangeCheck())
						{
							if (em.EyeR != null)
							{
								Quaternion r1 = em.EyeR.rotation;
								em.EyeR.LookAt(em.LookAtTarget);
								Quaternion r2 = em.EyeR.rotation;
								em.EyeRDefault = r1 * Quaternion.Inverse(r2);
								em.EyeR.rotation = r1;

							}
						}

						em.Head = (Transform)EditorGUILayout.ObjectField("Head", em.Head, typeof(Transform), true);
						em.LookAtTarget = (Transform)EditorGUILayout.ObjectField("LookAt Target", em.LookAtTarget, typeof(Transform), true);

						em.LookAroundScale = EditorGUILayout.Vector2Field("LookAroundScale", em.LookAroundScale);
						em.LookRandomAmount = EditorGUILayout.FloatField("LookRandomAmount", em.LookRandomAmount);
						em.LookAroundSpeed = EditorGUILayout.FloatField("LookAroundSpeed", em.LookAroundSpeed);
					}
					GUILayout.EndVertical();
				}


				em.Blink = EditorGUILayout.Toggle("Blink", em.Blink);

				if (em.Blink && em.FaceSkin != null)
				{
					GUILayout.BeginVertical(EditorStyles.helpBox);

					Blink = EditorGUILayout.Foldout(Blink, "Blink");
					if (Blink)
					{
						em.BlinkRandomAmount = EditorGUILayout.FloatField("BlinkRandomAmount", em.BlinkRandomAmount);
						em.BlinkSpeed = EditorGUILayout.FloatField("BlinkSpeed", em.BlinkSpeed);
						em.BlinkIndex = EditorGUILayout.Popup("Eyes Blink Blend Shape", em.BlinkIndex, GetArrayBlendShapes(em.FaceSkin));

						em.EyeLidBones = EditorGUILayout.Foldout(em.EyeLidBones, "Eye Lid Bones");

						if (em.EyeLidBones)
						{
							if (GUILayout.Button("Set Closed Pose", GUILayout.Width(100), GUILayout.Height(15), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true)))
							{
								em.EyeLidTopLClosed = em.EyeLidTopL.localRotation;
								em.EyeLidTopRClosed = em.EyeLidTopR.localRotation;
								em.EyeLidBottomLClosed = em.EyeLidBottomL.localRotation;
								em.EyeLidBottomRClosed = em.EyeLidBottomR.localRotation;
								em.EyeLidTopL.localRotation = em.EyeLidTopLDefault;
								em.EyeLidTopR.localRotation = em.EyeLidTopRDefault;
								em.EyeLidBottomL.localRotation = em.EyeLidBottomLDefault;
								em.EyeLidBottomR.localRotation = em.EyeLidBottomRDefault;

							}
							EditorGUI.BeginChangeCheck();
							em.EyeLidTopL = (Transform)EditorGUILayout.ObjectField("Eye Lid Top L", em.EyeLidTopL, typeof(Transform), true);
							if (EditorGUI.EndChangeCheck())
							{
								if (em.EyeLidTopL != null)
								{
									em.EyeLidTopLDefault = em.EyeLidTopL.localRotation;
									em.EyeLidTopLClosed = em.EyeLidTopL.localRotation;
									em.EyeLidTopLUp = em.EyeLidTopL.localRotation;
									em.EyeLidTopLDown = em.EyeLidTopL.localRotation;
								}
							}
							
							EditorGUI.BeginChangeCheck();
							em.EyeLidTopR = (Transform)EditorGUILayout.ObjectField("Eye Lid Top R", em.EyeLidTopR, typeof(Transform), true);
							if (EditorGUI.EndChangeCheck())
							{
								if (em.EyeLidTopR != null)
								{
									em.EyeLidTopRDefault = em.EyeLidTopR.localRotation;
									em.EyeLidTopRClosed = em.EyeLidTopR.localRotation;
									em.EyeLidTopRUp = em.EyeLidTopR.localRotation;
									em.EyeLidTopRDown = em.EyeLidTopR.localRotation;
								}
							}
							
							EditorGUI.BeginChangeCheck();
							em.EyeLidBottomL = (Transform)EditorGUILayout.ObjectField("Eye Lid Bottom L", em.EyeLidBottomL, typeof(Transform), true);
							if (EditorGUI.EndChangeCheck())
							{
								if (em.EyeLidBottomL != null)
								{
									em.EyeLidBottomLDefault = em.EyeLidBottomL.localRotation;
									em.EyeLidBottomLClosed = em.EyeLidBottomL.localRotation;
									em.EyeLidBottomLUp = em.EyeLidBottomL.localRotation;
									em.EyeLidBottomLDown = em.EyeLidBottomL.localRotation;
								}
							}
							
							EditorGUI.BeginChangeCheck();
							em.EyeLidBottomR = (Transform)EditorGUILayout.ObjectField("Eye Lid Bottom R", em.EyeLidBottomR, typeof(Transform), true);
							if (EditorGUI.EndChangeCheck())
							{
								if (em.EyeLidBottomR != null)
								{
									em.EyeLidBottomRDefault = em.EyeLidBottomR.localRotation;
									em.EyeLidBottomRClosed = em.EyeLidBottomR.localRotation;
									em.EyeLidBottomRUp = em.EyeLidBottomR.localRotation;
									em.EyeLidBottomRDown = em.EyeLidBottomR.localRotation;

								}
							}
							
						}

					}
					GUILayout.EndVertical();
				}

				

				em.UpDown = EditorGUILayout.Toggle("Up Down", em.UpDown);

				if (em.UpDown && em.FaceSkin != null)
				{
					GUILayout.BeginVertical(EditorStyles.helpBox);

					UpDown = EditorGUILayout.Foldout(UpDown, "Up Down");
					if (UpDown)
					{
						em.EyesUpIndex = EditorGUILayout.Popup("Eyes Up Blend Shape", em.EyesUpIndex, GetArrayBlendShapes(em.FaceSkin));
						em.EyesDownIndex = EditorGUILayout.Popup("Eyes Down Blend Shape", em.EyesDownIndex, GetArrayBlendShapes(em.FaceSkin));
						em.EyeLidBonesUpDown = EditorGUILayout.Foldout(em.EyeLidBonesUpDown, "Eye Lid Bones");

						if (em.EyeLidBonesUpDown)
						{
							if (GUILayout.Button("Set Up Pose", GUILayout.Width(100), GUILayout.Height(15), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true)))
							{
								em.EyeLidTopLUp = em.EyeLidTopL.localRotation;
								em.EyeLidTopRUp = em.EyeLidTopR.localRotation;
								em.EyeLidBottomLUp = em.EyeLidBottomL.localRotation;
								em.EyeLidBottomRUp = em.EyeLidBottomR.localRotation;
								em.EyeLidTopL.localRotation = em.EyeLidTopLDefault;
								em.EyeLidTopR.localRotation = em.EyeLidTopRDefault;
								em.EyeLidBottomL.localRotation = em.EyeLidBottomLDefault;
								em.EyeLidBottomR.localRotation = em.EyeLidBottomRDefault;

							}
							if (GUILayout.Button("Set Down Pose", GUILayout.Width(100), GUILayout.Height(15), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true)))
							{
								em.EyeLidTopLDown = em.EyeLidTopL.localRotation;
								em.EyeLidTopRDown = em.EyeLidTopR.localRotation;
								em.EyeLidBottomLDown = em.EyeLidBottomL.localRotation;
								em.EyeLidBottomRDown = em.EyeLidBottomR.localRotation;
								em.EyeLidTopL.localRotation = em.EyeLidTopLDefault;
								em.EyeLidTopR.localRotation = em.EyeLidTopRDefault;
								em.EyeLidBottomL.localRotation = em.EyeLidBottomLDefault;
								em.EyeLidBottomR.localRotation = em.EyeLidBottomRDefault;

							}
							
						}
						em.LookDirection = EditorGUILayout.Vector3Field("Look Up Vector", em.LookDirection);
						em.LookUpDownAmount = EditorGUILayout.FloatField("Look Up Down Amount", em.LookUpDownAmount);
						em.LookUpDownOffset = EditorGUILayout.FloatField("Look Up Down Offset", em.LookUpDownOffset);

					}


					GUILayout.EndVertical();
				}
				


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
