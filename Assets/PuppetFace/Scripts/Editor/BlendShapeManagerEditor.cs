using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace PuppetFace
{
	public class InputPopup : EditorWindow
	{	
		public string newName = "BlendShapeName";
		void OnGUI()
		{
			EditorGUILayout.LabelField("Set The Name OF The Blend Shape", EditorStyles.wordWrappedLabel);
			newName = EditorGUILayout.TextField(newName);
			if (GUILayout.Button("Done"))
			{
				Selection.activeGameObject.name = newName;
				this.Close();
			}
		}
	}

	[CustomEditor(typeof(BlendShapeManager))]
	public class BlendShapeManagerEditor : Editor
	{
		public BlendShapeManager b;
		public Vector2 ScrollPos;
        private static string _puppetFacePath;

        void OnEnable()
		{
            _puppetFacePath = "Assets" + RecursivelyFindFolderPath().Substring(Application.dataPath.Length);

            Initialize();
		}
		public Vector2 RedrawAllWindows()
		{
			Vector2 center = Vector2.zero;
			int windowCount = 0;

			EditorWindow[] allWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();
			foreach (EditorWindow ew in allWindows)
			{
				windowCount++;
				center += ew.position.center;
				ew.Repaint();				
				//Debug.Log(ew);
			}
			center /= windowCount;
			return center;
		}
		void Initialize()
		{
			
			b = (BlendShapeManager)target;
			b.Skin = b.GetComponent<SkinnedMeshRenderer>();
			if (b.Skin == null)
				b.Skin = b.gameObject.AddComponent<SkinnedMeshRenderer>();
			b.BlendShapes = b.GetArrayBlendShapes(b.Skin);
			if (b.BlendShapeTextures.Length == 0)
			{
				b.index = 0;
				b.NeedsInitializing = true;
			}
			
		}
		
		string[] options;
		List<int> selectedOptions ;

        public override void OnInspectorGUI()
		{
			
			

			GUILayout.Space(10);
			string LogoPath = _puppetFacePath +"/Textures/GUI/PuppetFaceLogo.png";
			Texture LogoAsset = AssetDatabase.LoadAssetAtPath(LogoPath, typeof(Texture)) as Texture;
			GUILayout.BeginHorizontal();

			GUILayout.BeginVertical();

			if (GUILayout.Button("CREATE NEW BLEND SHAPE", GUILayout.Width(100), GUILayout.Height(25), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true)))
			{
                SetSceneViewGizmos(true);
                MeshModifier mm = Selection.activeGameObject.GetComponent<MeshModifier>();
				if (mm == null)
				{
					MakeNewBlendShapeModel("NewBlendShape", -1);

					return;
				}

			}
			if (GUILayout.Button("RENDER SNAP SHOTS", GUILayout.Width(100), GUILayout.Height(15), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true)))
			{
				b.BlendShapeTextures = new Texture2D[0];
				b.index = 0;
				b.NeedsInitializing = true;

			}
			if (b.Skin != null && b.Skin.sharedMesh !=null)
			{
				if(options == null)
                {
					options = new string[b.Skin.sharedMesh.subMeshCount];
					for(int i=0;i< options.Length;i++)
                    {
						if (i < b.Skin.sharedMaterials.Length)
							options[i] = b.Skin.sharedMaterials[i].name;
						else
							options[i] = ("Mesh_" + i);


					}
				}
				
				b.flags = EditorGUILayout.MaskField("Sub Meshes", b.flags, options);
				selectedOptions = new List<int>();
				for (int i = 0; i < options.Length; i++)
				{
					if ((b.flags & (1 << i)) == (1 << i)) selectedOptions.Add(i);
				}
				string[] blendshapeTypes = {  "From Bind Pose", "From Bind Pose (Skinned)", "From Current Pose" };
				b.BlendShapeType = EditorGUILayout.Popup("Type" ,b.BlendShapeType, blendshapeTypes);


			}
			GUILayout.EndVertical();

			GUILayout.Label(LogoAsset, GUILayout.Width(40), GUILayout.Height(40));
			GUILayout.EndHorizontal();
									
			
			GUILayout.Space(10);

			int steps = (int)((float)Screen.width / 100f);
			//Debug.Log(b.BlendShapes[0]);
			if (b.BlendShapes.Length > 1)
			{
				ScrollPos = GUILayout.BeginScrollView(ScrollPos, GUILayout.Height(140 + 100 * (int)((b.BlendShapes.Length / steps))), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true));

				int hor = 0;
				for (int i = 0; i < b.BlendShapes.Length; i++)
				{

					if (i % steps == 0)
					{
						GUILayout.BeginHorizontal();
						hor++;
					}

					GUILayout.BeginVertical();
					CreateBlendShapeButton(i);
					GUILayout.EndVertical();
					if ((i + 1) % steps == 0)
					{
						hor--;
						GUILayout.EndHorizontal();
					}

				}
				if (hor > 0)
					GUILayout.EndHorizontal();

				GUILayout.EndScrollView();
			}


		}
		
		GameObject MakeNewBlendShapeModel(string blendShapeName, int index = 0)
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
			currentSMR.enabled = false;
			GameObject newBlendShapeGO = new GameObject();
			newBlendShapeGO.name = blendShapeName;
			if (b.BlendShapeType == 1)
			{
				SkinnedMeshRenderer smr = newBlendShapeGO.AddComponent<SkinnedMeshRenderer>();
				MakeBlendShapeMesh(SelectedGO.GetComponent<SkinnedMeshRenderer>().sharedMesh, index, currentSMR, smr, null, null);

			}
			else
			{
				MeshFilter mf = newBlendShapeGO.AddComponent<MeshFilter>();
				MeshRenderer mrNew = newBlendShapeGO.AddComponent<MeshRenderer>();
				MakeBlendShapeMesh(SelectedGO.GetComponent<SkinnedMeshRenderer>().sharedMesh, index, currentSMR,null, mf, mrNew);
			}



			newBlendShapeGO.transform.position = SelectedGO.transform.position;
			newBlendShapeGO.transform.rotation = SelectedGO.transform.rotation;
			newBlendShapeGO.transform.localScale = SelectedGO.transform.localScale;
			MeshModifier meshModifier = newBlendShapeGO.AddComponent<MeshModifier>();
			meshModifier.Index = index;
			meshModifier.TargetSkin = currentSMR;
			meshModifier.BlendShapeType = b.BlendShapeType;
			Selection.activeGameObject = newBlendShapeGO;
			GameObject newCameraGO = new GameObject();
			Camera newCam = newCameraGO.AddComponent<Camera>();
			newCam.enabled = false;
			newCam.fieldOfView = 15f;
			newCam.backgroundColor = new Color(0.35f, 0.35f, 0.35f, 1f);

			newCam.transform.localPosition = 9999f * Vector3.forward;
			newCameraGO.transform.parent = newBlendShapeGO.transform;

			return newBlendShapeGO;
		}
		public Mesh MakeBlendShapeMesh(Mesh mesh, int index, SkinnedMeshRenderer currentSMR,SkinnedMeshRenderer smr, MeshFilter mf, MeshRenderer mr)
		{
			Mesh newMesh = new Mesh();
			Vector3[] deltas = new Vector3[mesh.vertexCount];
			Vector3[] deltaNormals = new Vector3[mesh.vertexCount];
			Vector3[] deltaTangents = new Vector3[mesh.vertexCount];
			if (b.BlendShapeType == 2)
				currentSMR.BakeMesh(newMesh);
			else
				newMesh.vertices = currentSMR.sharedMesh.vertices;
			if (index >= 0)
			{
				currentSMR.SetBlendShapeWeight(index, 0);
				mesh.GetBlendShapeFrameVertices(index, 0, deltas, deltaNormals, deltaTangents);
				int vertCount = mesh.vertexCount;

				if (b.BlendShapeType == 2)
				{
					Vector3[] verts = newMesh.vertices;
					Vector3[] normals = currentSMR.sharedMesh.normals;

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
			newMesh.tangents = mesh.tangents;

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
			/*for (int i = 0; i < mesh.subMeshCount; i++)
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
		private void CreateBlendShapeButton(int id)
		{

			string faceShape = (_puppetFacePath +"/Textures/GUI/img/lisaNew.png");

			Texture faceShapeTexture = AssetDatabase.LoadAssetAtPath(faceShape, typeof(Texture)) as Texture;
			if (b.BlendShapeTextures != null && id < b.BlendShapeTextures.Length)
				faceShapeTexture = b.BlendShapeTextures[id];



			if (id < b.BlendShapes.Length - 1)
			{


				GUIStyle guistyleLabel = new GUIStyle();
				guistyleLabel.fixedHeight = 0;
				guistyleLabel.fixedWidth = 0;
				guistyleLabel.normal.textColor = Color.white;
				guistyleLabel.fontSize = 10;


				GUIStyle guistyle = new GUIStyle();
				guistyle.fixedHeight = 0;
				guistyle.fixedWidth = 0;

				GUILayout.Label(b.BlendShapes[id].Substring(0, Mathf.Clamp(b.BlendShapes[id].Length, 0, 12)), guistyleLabel);
				GUILayout.Space(5);
				if (GUILayout.Button(faceShapeTexture, guistyle))
				{
                    SetSceneViewGizmos(true);
                    MeshModifier mm = Selection.activeGameObject.GetComponent<MeshModifier>();
					if (mm == null)
					{
						MakeNewBlendShapeModel(b.BlendShapes[id], id);

						return;
					}


				}
			}
			
			GUI.color = Color.white;

			if (b.BlendShapes.Length > 0 && id < b.BlendShapes.Length - 1 && b.Skin != null)
			{
				b.Skin.SetBlendShapeWeight(id, GUILayout.HorizontalSlider(b.Skin.GetBlendShapeWeight(id), 0, 100, GUILayout.Width(80)));
				GUILayout.Space(15);
				if (GUILayout.Button("DELETE", GUILayout.Width(80), GUILayout.Height(15), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false)))
				{
					b.DeleteBlendShape(b.Skin, b.BlendShapes[id]);
					var blendShapeList = new List<string>(b.BlendShapes);
					blendShapeList.RemoveAt(id);
					b.BlendShapes = blendShapeList.ToArray();

					var blendShapeTextureList = new List<Texture2D>(b.BlendShapeTextures);
					blendShapeTextureList.RemoveAt(id);
					b.BlendShapeTextures = blendShapeTextureList.ToArray();

				}				
				GUILayout.Space(20);

			}
		}
        public static void SetSceneViewGizmos(bool gizmosOn)
        {
            #if UNITY_2019_1_OR_NEWER
            UnityEditor.SceneView sv =UnityEditor.EditorWindow.GetWindow<UnityEditor.SceneView>();
            sv.drawGizmos = gizmosOn;
            #endif
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
