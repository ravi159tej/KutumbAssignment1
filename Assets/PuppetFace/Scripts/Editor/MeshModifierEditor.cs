using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PuppetFace
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(MeshModifier))]
	public class MeshModifierEditor : Editor
	{
		MeshModifier[] _meshModifiers;

		public struct Driver
		{
			public Vector3 point;
			public float radius;

		}


		public Driver[] driverInfo = new Driver[2];
		public ComputeBuffer vertexBuffer;
		public ComputeBuffer driverBuffer;

		public Mesh mesh;
		public int[] tris;
		public int[] trisDistinct;

		public int[] verts;

		public bool Hit = false;
		public Vector2 mousePosition;
		public Vector3 screenPos;
		public Vector3 newPos;
		public float PrevRadius;
		public bool Drawing = true;
		public bool DrawingStarted = false;
		public bool RadiusChange = false;

		private bool _useComputeShader = false;

		Tool lastTool = Tool.None;

		private float _internalScale = 1f;

		public bool InitializeTopology = false;
		public bool CheckClosest = true;

		private float _internalConnectedVertThreshold = 0f;
        private static string _puppetFacePath;


        void OnDisable()
		{
			Tools.current = lastTool;
		}
		// Use this for initialization
		void OnEnable()
		{
            _puppetFacePath = "Assets" + RecursivelyFindFolderPath().Substring(Application.dataPath.Length);

            Initialize();
		}
		void TopologyInitializer()
		{
			foreach (MeshModifier m in _meshModifiers)
			{
				int vertCount = mesh.vertexCount;
				for (int i = 0; i < vertCount; i++)
				{
					if (InitializeTopology)
					{
						float[] connectedLengths;
						int[] connectedVerts = GetConnectedVertices(i, out connectedLengths, m, CheckClosest);

						m._vertexDataFull[i].connectedIndexes = connectedVerts;
						m._vertexDataFull[i].connectedLengths = connectedLengths;
					}

				}
			}
		}
		void TopologyInitializer(MeshModifier m)
		{
			int vertCount = mesh.vertexCount;
			if (InitializeTopology)
			{
				for (int i = 0; i < vertCount; i++)
				{
					if (i % 200 == 0)
					{
						if (EditorUtility.DisplayCancelableProgressBar("Creating Topology", "Making Connections (To speed up high-res meshes, select less submeshes)", (float)i / (float)vertCount))
						{
							EditorUtility.ClearProgressBar();
							_internalConnectedVertThreshold = m.ConnectedVertexThreshold;
							//InitializeTopology = false;
							break;
						}
					}
					float[] connectedLengths;
					int[] connectedVerts = GetConnectedVertices(i, out connectedLengths, m, CheckClosest);

					m._vertexDataFull[i].connectedIndexes = connectedVerts;
					m._vertexDataFull[i].connectedLengths = connectedLengths;


				}
				EditorUtility.ClearProgressBar();

			}

		}
		
		void Initialize()
		{

			Object[] monoObjects = targets;
			_meshModifiers = new MeshModifier[monoObjects.Length];
			for (int i = 0; i < monoObjects.Length; i++)
			{
				_meshModifiers[i] = monoObjects[i] as MeshModifier;
			}

			foreach (MeshModifier m in _meshModifiers)
			{
				lastTool = Tools.current;
				Tools.current = Tool.None;

				m._smr = m.GetComponent<SkinnedMeshRenderer>();

				if (m._smr == null)
				{
					m._mf = m.GetComponent<MeshFilter>();
					mesh = m._mf.sharedMesh;
				}
				else
				{
					mesh = m._smr.sharedMesh;
				}
				_internalScale = m.GetComponent<Renderer>().bounds.size.y;
				m.Radius = _internalScale / 15f;
				List<int> trisList = new List<int>();

				for (int i=0;i<mesh.subMeshCount;i++)
                {
					int[] trisToAdd = mesh.GetTriangles(i);

					trisList.AddRange(trisToAdd);
				}
				tris = trisList.ToArray();
				trisDistinct = tris.Distinct().ToArray();

				Debug.Log("tri count " + tris.Length/3f);
				InitializeTopology = true;

				if (tris.Length / 3f > 120000)
				{
					Debug.LogWarning("Mesh exceeds 120K tris, unable to index mesh. Try choosing a submesh.");
					InitializeTopology = false;
				}
				else if (tris.Length / 3f > 60000)
                {
					//InitializeTopology = false;
					Debug.LogWarning("Mesh exceeds 60K tris, overlapping vertices ignored");
					CheckClosest = false;
				}
				else
				{
					
				}

				m._meshCol = m.GetComponent<MeshCollider>();
				if (m._meshCol == null)
				{
					m._meshCol = m.gameObject.AddComponent<MeshCollider>();
					m._meshCol.sharedMesh = mesh;
				}

				if (_useComputeShader)
				{
					string shaderName = "MeshModify";
					ComputeShader[] compShaders = (ComputeShader[])Resources.FindObjectsOfTypeAll(typeof(ComputeShader));
					for (int i = 0; i < compShaders.Length; i++)
					{
						if (compShaders[i].name == shaderName)
						{
							m._shader = compShaders[i];
							break;
						}
					}

				}
				if (m._newMesh == null || m._vertexDataFull == null)
				{
					m._vertexData = new MeshModifier.VertexInfo[mesh.vertexCount];
					m._vertexDataOutput = new MeshModifier.VertexInfo[mesh.vertexCount];
					m._vertexDataFull = new MeshModifier.VertexInfoFull[mesh.vertexCount];
					m._verts = mesh.vertices;
					m._normals = mesh.normals;

					Vector3[] originalVerts = new Vector3[0];
					if (m.BlendShapeType == 2)
					{
						if (m.TargetSkin != null)
						{
							Mesh bakedMesh = new Mesh();
							m.TargetSkin.BakeMesh(bakedMesh);
							originalVerts = bakedMesh.vertices;
						}

					}
					else if (m.TargetSkin != null)
						originalVerts = m.TargetSkin.sharedMesh.vertices;
					int vertCount = mesh.vertexCount;
					Vector3[] vertArray = mesh.vertices;
					
					for (int i = 0; i < vertCount; i++)
					{
						m._vertexData[i].point = (vertArray[i]);
						m._vertexData[i].prevPoint = (vertArray[i]);
						if (m.TargetSkin != null)
							m._vertexDataOutput[i].point = originalVerts[i];
						else
							m._vertexDataOutput[i].point = (vertArray[i]);

						m._vertexDataFull[i].connectedIndexes = new int[0];
						m._vertexDataFull[i].connectedLengths = new float[0];
						if (InitializeTopology)
						{
							if (i % 200 == 0)
							{
								if (EditorUtility.DisplayCancelableProgressBar("Creating Topology", "Generating connections (For high-res meshes, select less submeshes)", (float)i / (float)vertCount))
								{
									EditorUtility.ClearProgressBar();
									m._vertexDataFull[i].connectedIndexes = null;
									m._vertexDataFull[i].connectedLengths = new float[0];
									InitializeTopology = false;

								}
							}
							float[] connectedLengths;
							int[] connectedVerts = GetConnectedVertices(i, out connectedLengths, m, CheckClosest);

							m._vertexDataFull[i].connectedIndexes = connectedVerts;
							m._vertexDataFull[i].connectedLengths = connectedLengths;
						}
						m._vertexDataFull[i].point = (vertArray[i]);
						m._vertexDataFull[i].id = i;
						m._vertexDataFull[i].workingLength = 0f;
					}
					EditorUtility.ClearProgressBar();
					if(InitializeTopology)
						m.selectionType = MeshModifier.SelectionType.Topology;

					m._newMesh = new Mesh();
					m._newMesh.vertices = mesh.vertices;
					m._newMesh.subMeshCount = mesh.subMeshCount;
					for (int index = 0; index < mesh.subMeshCount; index++)
					{
						m._newMesh.SetTriangles(mesh.GetTriangles(index), index);
					}
					m._newMesh.normals = mesh.normals;
					m._newMesh.tangents = mesh.tangents;
					m._newMesh.uv = mesh.uv;
					m._newMesh.name = m.name + "_BS";
					m._newMesh.colors = mesh.colors;
					m._newMesh.bindposes = mesh.bindposes;
					m._newMesh.boneWeights = mesh.boneWeights;

					CopyAllBlendShapes(ref mesh,ref m._newMesh);
					/*GoToBindPose(m._smr);
					m.transform.position = Vector3.zero;
					m.transform.rotation = Quaternion.identity;
					m.transform.localScale = Vector3.one;*/

				}
				//SaveMesh(m.newMesh);		
				//verts = new Vector3[mesh.vertexCount];

				m._meshCol = m.GetComponent<MeshCollider>();

				if (_useComputeShader)
				{
					vertexBuffer = new ComputeBuffer(m._vertexData.Length, 64);
					vertexBuffer.SetData(m._vertexData);
					driverBuffer = new ComputeBuffer(driverInfo.Length, 16);
				}

				EditorUtility.SetDirty(m._newMesh);
				AssetDatabase.SaveAssets();
			}

		}

        public static void GoToBindPose(SkinnedMeshRenderer smr)
        {				
			
			Matrix4x4[] bindPoses = smr.sharedMesh.bindposes;
			for (int j = 0; j < bindPoses.Length; j++)
			{
				for (int i = 0; i < bindPoses.Length; i++)
				{					
					Matrix4x4 localMatrix = bindPoses[i];
					if (smr.bones[i].parent != null)
					{
						localMatrix = (localMatrix * smr.bones[i].parent.worldToLocalMatrix.inverse).inverse;
					}
					else
						localMatrix = localMatrix.inverse;
					smr.bones[i].localPosition = localMatrix.MultiplyPoint(Vector3.zero);
					smr.bones[i].localRotation = Quaternion.LookRotation(localMatrix.GetColumn(2), localMatrix.GetColumn(1));
					smr.bones[i].localScale = new Vector3(localMatrix.GetColumn(0).magnitude, localMatrix.GetColumn(1).magnitude, localMatrix.GetColumn(2).magnitude);
					
				}
			}
			



		}

        public static void CopyAllBlendShapes(ref Mesh sourceMesh, ref Mesh destMesh)
		{
			Vector3[] dVertices = new Vector3[sourceMesh.vertexCount];
			Vector3[] dNormals = new Vector3[sourceMesh.vertexCount];
			Vector3[] dTangents = new Vector3[sourceMesh.vertexCount];

			for (int shape = 0; shape < sourceMesh.blendShapeCount; shape++)
			{
				for (int frame = 0; frame < sourceMesh.GetBlendShapeFrameCount(shape); frame++)
				{
					string shapeName = sourceMesh.GetBlendShapeName(shape);
					float frameWeight = sourceMesh.GetBlendShapeFrameWeight(shape, frame);

					sourceMesh.GetBlendShapeFrameVertices(shape, frame, dVertices, dNormals, dTangents);

					destMesh.AddBlendShapeFrame(shapeName, frameWeight, dVertices, dNormals, dTangents);
				}
			}
		}
		public float DistanceOptimised(Vector3 firstPosition, Vector3 secondPosition)
		{
			Vector3 heading;
			
			heading.x = firstPosition.x - secondPosition.x;
			heading.y = firstPosition.y - secondPosition.y;
			heading.z = firstPosition.z - secondPosition.z;

			float distanceSquared = heading.x * heading.x + heading.y * heading.y + heading.z * heading.z;
			return Mathf.Sqrt(distanceSquared);

			
		}
		int[] GetConnectedVertices(int index, out float[] connectedLengths, MeshModifier m, bool checkClosest = true )
		{
			List<int> connectedVerts = new List<int>();

			for (int t = 0; t < tris.Length; t += 3)
			{

				//int c1 = tris[t];
				//int c2 = tris[t + 1];
				//int c3 = tris[t + 2];
				if (tris[t] == index)
                {
					connectedVerts.Add(tris[t + 1]);
					connectedVerts.Add(tris[t + 2]);
				}				
				else if (tris[t + 1] == index)
                {
					connectedVerts.Add(tris[t]);
					connectedVerts.Add(tris[t + 2]);
				}			
				else if(tris[t + 2] == index)
				{
					connectedVerts.Add(tris[t]);
					connectedVerts.Add(tris[t + 1]);
				}			
				

			}
			
			int count = trisDistinct.Length;
			if (checkClosest)
			{
				for (int i = 0; i < count; i++)
				{
					if(m._verts[trisDistinct[i]].Equals(m._verts[index]))
						connectedVerts.Add(i);
					//if (DistanceOptimised(m._verts[trisDistinct[i]], m._verts[index]) <= m.ConnectedVertexThreshold)
					//	connectedVerts.Add(i);
				}
			}
			connectedVerts = connectedVerts.Distinct().ToList();
			connectedLengths = new float[connectedVerts.Count];
			count = connectedVerts.Count;
			for (int i = 0; i < count; i++)
			{
				connectedLengths[i] = DistanceOptimised(m._verts[connectedVerts[i]], m._verts[index]);
			}
			return connectedVerts.ToArray();

		}
		public void OnDestroy()
		{
			if (_useComputeShader)
			{
				vertexBuffer.Release();
				driverBuffer.Release();
			}
		}
		public override void OnInspectorGUI()
		{
			foreach (MeshModifier m in _meshModifiers)
			{
				string GUI_push = (_puppetFacePath +"/Textures/GUI/GUI_Push.png");
				Texture GUI_pushTexture = AssetDatabase.LoadAssetAtPath(GUI_push, typeof(Texture)) as Texture;
				string GUI_pushDown = (_puppetFacePath +"/Textures/GUI/GUI_PushDown.png");
				Texture GUI_pushDownTexture = AssetDatabase.LoadAssetAtPath(GUI_pushDown, typeof(Texture)) as Texture;

				string GUI_pull = (_puppetFacePath +"/Textures/GUI/GUI_pull.png");
				Texture GUI_pullTexture = AssetDatabase.LoadAssetAtPath(GUI_pull, typeof(Texture)) as Texture;
				string GUI_pullDown = (_puppetFacePath +"/Textures/GUI/GUI_pullDown.png");
				Texture GUI_pullDownTexture = AssetDatabase.LoadAssetAtPath(GUI_pullDown, typeof(Texture)) as Texture;

				string GUI_smooth = (_puppetFacePath +"/Textures/GUI/GUI_Smooth.png");
				Texture GUI_smoothTexture = AssetDatabase.LoadAssetAtPath(GUI_smooth, typeof(Texture)) as Texture;
				string GUI_smoothDown = (_puppetFacePath +"/Textures/GUI/GUI_SmoothDown.png");
				Texture GUI_smoothDownTexture = AssetDatabase.LoadAssetAtPath(GUI_smoothDown, typeof(Texture)) as Texture;

				string GUI_move = (_puppetFacePath +"/Textures/GUI/GUI_Move.png");
				Texture GUI_moveTexture = AssetDatabase.LoadAssetAtPath(GUI_move, typeof(Texture)) as Texture;
				string GUI_moveDown = (_puppetFacePath +"/Textures/GUI/GUI_MoveDown.png");
				Texture GUI_moveDownTexture = AssetDatabase.LoadAssetAtPath(GUI_moveDown, typeof(Texture)) as Texture;

				string GUI_erase = (_puppetFacePath +"/Textures/GUI/GUI_Erase.png");
				Texture GUI_eraseTexture = AssetDatabase.LoadAssetAtPath(GUI_erase, typeof(Texture)) as Texture;
				string GUI_eraseDown = (_puppetFacePath +"/Textures/GUI/GUI_EraseDown.png");
				Texture GUI_eraseDownTexture = AssetDatabase.LoadAssetAtPath(GUI_eraseDown, typeof(Texture)) as Texture;




				GUIStyle guistyle = new GUIStyle();
				guistyle.fixedHeight = 0;
				guistyle.fixedWidth = 0;
				guistyle.stretchWidth = true;

				GUILayout.Space(20);
				GUILayout.BeginHorizontal();
				GUILayout.Space(10);

				if (m.paintType == MeshModifier.PaintType.Move)
				{
					if (GUILayout.Button(new GUIContent(GUI_moveDownTexture, "Move Tool (W).\n\nThis tool moves the vertices. Click and drag to move the vertices around.\n\nHold Control To Erase.\nHold Shift to Smooth\n"), guistyle))
					{
						m.paintType = MeshModifier.PaintType.Move;
					}
				}
				else
				{
					if (GUILayout.Button(new GUIContent(GUI_moveTexture, "Move Tool (W).\n\nThis tool moves the vertices. Click and drag to move the vertices around.\n\nHold Control To Erase.\nHold Shift to Smooth\n"), guistyle))
					{
						m.paintType = MeshModifier.PaintType.Move;
					}
				}


				if (m.paintType == MeshModifier.PaintType.Pull)
				{
					if (GUILayout.Button(new GUIContent(GUI_pullDownTexture, "Push Tool (E).\n\nThis tool pushes the vertices out by their normals,\n\nHold Control To Pull.\nHold Shift to Smooth\n"), guistyle))
					{
						m.paintType = MeshModifier.PaintType.Pull;
					}
				}
				else
				{
					if (GUILayout.Button(new GUIContent(GUI_pullTexture, "Push Tool (E).\n\nThis tool pushes the vertices out by their normals,\n\nHold Control To Pull.\nHold Shift to Smooth\n"), guistyle))
					{
						m.paintType = MeshModifier.PaintType.Pull;
					}
				}


				if (m.paintType == MeshModifier.PaintType.Push)
				{
					if (GUILayout.Button(new GUIContent(GUI_pushDownTexture, "Pull Tool (R).\n\nThis tool pulls the vertices out by their normals,\n\nHold Control To Push.\nHold Shift to Smooth\n"), guistyle))
					{
						m.paintType = MeshModifier.PaintType.Push;
					}
				}
				else
				{
					if (GUILayout.Button(new GUIContent(GUI_pushTexture, "Pull Tool (R).\n\nThis tool pulls the vertices out by their normals,\n\nHold Control To Push.\nHold Shift to Smooth\n"), guistyle))
					{
						m.paintType = MeshModifier.PaintType.Push;
					}
				}



				if (m.paintType == MeshModifier.PaintType.Smooth)
				{
					if (GUILayout.Button(new GUIContent(GUI_smoothDownTexture, "Smooth Tool (T).\n\nThis tool smooths the vertices out\n\nHold Control To Erase.\n"), guistyle))
					{
						m.paintType = MeshModifier.PaintType.Smooth;
					}
				}
				else
				{
					if (GUILayout.Button(new GUIContent(GUI_smoothTexture, "Smooth Tool (T).\n\nThis tool smooths the vertices out\n\nHold Control To Erase.\n"), guistyle))
					{
						m.paintType = MeshModifier.PaintType.Smooth;
					}
				}
				if (m.paintType == MeshModifier.PaintType.Erase)
				{
					if (GUILayout.Button(new GUIContent(GUI_eraseDownTexture, "Erase Tool (Y).\n\nThis tool transforms the vertices back to their original position,\n"), guistyle))
					{
						m.paintType = MeshModifier.PaintType.Erase;
					}
				}
				else
				{
					if (GUILayout.Button(new GUIContent(GUI_eraseTexture, "Erase Tool (Y).\n\nThis tool transforms the vertices back to their original position,\n"), guistyle))
					{
						m.paintType = MeshModifier.PaintType.Erase;
					}
				}
				GUILayout.EndHorizontal();
				GUILayout.Space(10);
				DrawDefaultInspector();
				if (GUILayout.Button("Set Blend Shape"))
				{
					
					if (m.TargetSkin == m._smr)
					{
						Debug.LogWarning("The Target SkinnnedMeshRenderer needs to be different to this gameObject");

					}
					else if (m.TargetSkin != null)
					{
						PuppetFace.BlendShapeManager BlendShapeManager = m.TargetSkin.GetComponent<PuppetFace.BlendShapeManager>();
						if (BlendShapeManager != null)
						{
							if (m.Index < BlendShapeManager.BlendShapeTextures.Length && m.Index >= 0)
								BlendShapeManager.BlendShapeTextures[m.Index] = TakeScreenShot();
							else
							{
								List<Texture2D> texs = new List<Texture2D>(BlendShapeManager.BlendShapeTextures);
								texs.Add(TakeScreenShot());
								BlendShapeManager.BlendShapeTextures = texs.ToArray();
							}
						}						
						SetBlendShape(m);
						m.TargetSkin.enabled = true;
						Selection.activeGameObject = m.TargetSkin.gameObject;
						if (m._smr != m.TargetSkin)
							DestroyImmediate(m.gameObject);
					}
					else
					{
						Debug.LogWarning("Needs a Target SkinnedMeshRenderer");
					}
				}
				if (GUILayout.Button("Reset"))
				{
					if (_internalConnectedVertThreshold != m.ConnectedVertexThreshold)
					{
						TopologyInitializer(m);
						_internalConnectedVertThreshold = m.ConnectedVertexThreshold;
					}

					for (int i = 0; i < m._verts.Length; i++)
					{
						m._verts[i] = m._vertexData[i].point;
						m._newMesh.vertices = m._verts;
					}
				}
				if (GUILayout.Button("Discard"))
				{
					if (m.TargetSkin == m._smr)
					{
						Debug.LogWarning("The Target SkinnnedMeshRenderer needs to be different to this gameObject");

					}
					else if (m.TargetSkin != null)
					{
						m.TargetSkin.enabled = true;
						Selection.activeGameObject = m.TargetSkin.gameObject;
						DestroyImmediate(m.gameObject);
					}
					else
					{
						Debug.LogWarning("Needs a Target SkinnedMeshRenderer");
					}
				}
			}
		}

		public void SetBlendShape(MeshModifier m)
		{
			string BlendShapeName = m.name;
			Mesh mesh = m._newMesh;

			Mesh meshCurrent = new Mesh();
			if (m.BlendShapeType == 2)
				m.TargetSkin.BakeMesh(meshCurrent);
			else
				meshCurrent = m.TargetSkin.sharedMesh;

			Vector3[] deltas = new Vector3[mesh.vertexCount];
			Vector3[] deltaNormals = new Vector3[mesh.vertexCount];
			Vector3[] deltaTangents = new Vector3[mesh.vertexCount];
			if (mesh.vertexCount != meshCurrent.vertexCount)
			{
				Debug.LogWarning("Vert Count mismatch " + mesh.vertexCount + " " + meshCurrent.vertexCount);
			}
			int vertCount = mesh.vertexCount;
			Vector3[] verts = mesh.vertices;
			Vector3[] vertsCurrent = meshCurrent.vertices;
			Vector3[] normals = mesh.normals;
			Vector4[] tangents = mesh.tangents;
			Vector3[] normalsCurrent = m.TargetSkin.sharedMesh.normals;
			Vector4[] tangentsCurrent = m.TargetSkin.sharedMesh.tangents;
			if (tangents.Length > 0)
			{
				for (int i = 0; i < vertCount; i++)
				{
					deltas[i] = verts[i] - vertsCurrent[i];
					deltaNormals[i] = normals[i] - normalsCurrent[i];
					deltaTangents[i] = tangents[i] - tangentsCurrent[i];
				}
			}
			else
            {
				for (int i = 0; i < vertCount; i++)
				{
					deltas[i] = verts[i] - vertsCurrent[i];
					deltaNormals[i] = normals[i] - normalsCurrent[i];
				}
			}

			ReplaceBlendShape(BlendShapeName, deltas, deltaNormals, deltaTangents, m);


		}

		public void ReplaceBlendShape(string blendShapeName, Vector3[] deltasNew, Vector3[] deltaNormalsNew, Vector3[] deltaTangentsNew, MeshModifier m)
		{
			string BlendShapeName = m.name;
							

			Mesh tmpMesh = new Mesh();
			tmpMesh.vertices = m.TargetSkin.sharedMesh.vertices;
			Vector3[] dVertices = new Vector3[m.TargetSkin.sharedMesh.vertexCount];
			Vector3[] dNormals = new Vector3[m.TargetSkin.sharedMesh.vertexCount];
			Vector3[] dTangents = new Vector3[m.TargetSkin.sharedMesh.vertexCount];
			bool added = false;
			for (int shape = 0; shape < m.TargetSkin.sharedMesh.blendShapeCount; shape++)
			{
				for (int frame = 0; frame < m.TargetSkin.sharedMesh.GetBlendShapeFrameCount(shape); frame++)
				{
					string shapeName = m.TargetSkin.sharedMesh.GetBlendShapeName(shape);
					float frameWeight = m.TargetSkin.sharedMesh.GetBlendShapeFrameWeight(shape, frame);

					m.TargetSkin.sharedMesh.GetBlendShapeFrameVertices(shape, frame, dVertices, dNormals, dTangents);

					if (shapeName == blendShapeName)
					{
						dVertices = deltasNew;
						dNormals = deltaNormalsNew;
						dTangents = deltaTangentsNew;
						added = true;
					}



					tmpMesh.AddBlendShapeFrame(shapeName, frameWeight, dVertices, dNormals, dTangents);
				}
			}
			Mesh myMesh = SaveMesh(m.TargetSkin.sharedMesh);
			m.TargetSkin.sharedMesh = myMesh;

			m.TargetSkin.sharedMesh.ClearBlendShapes();

			for (int shape = 0; shape < tmpMesh.blendShapeCount; shape++)
			{
				for (int frame = 0; frame < tmpMesh.GetBlendShapeFrameCount(shape); frame++)
				{
					string shapeName = tmpMesh.GetBlendShapeName(shape);
					float frameWeight = tmpMesh.GetBlendShapeFrameWeight(shape, frame);

					tmpMesh.GetBlendShapeFrameVertices(shape, frame, dVertices, dNormals, dTangents);

					myMesh.AddBlendShapeFrame(shapeName, frameWeight, dVertices, dNormals, dTangents);
				}
			}
			if (!added)
			{
				m.TargetSkin.sharedMesh.AddBlendShapeFrame(BlendShapeName, 100f, deltasNew, deltaNormalsNew, deltaTangentsNew);
			}
			EditorUtility.SetDirty(m.TargetSkin.sharedMesh);
			AssetDatabase.SaveAssets();


		}
		public Mesh SaveMesh(Mesh mesh, bool Duplicate = false)
		{
			string path = AssetDatabase.GetAssetPath(mesh);
			string extension = Path.GetExtension(path);

			if (extension == ".asset" && !Duplicate)
			{
				return mesh;
			}
			else
			{
				string[] pathSplit = path.Split('/');
				string meshPath = "";
				for (int i = 0; i < pathSplit.Length - 1; i++)
				{
					meshPath += pathSplit[i] + "/";
				}
				if (meshPath == "" || (meshPath.Contains("Library")))
					meshPath = "Assets/";


				string outMeshPath = meshPath + mesh.name + "P3D.asset";

				outMeshPath = AssetDatabase.GenerateUniqueAssetPath(outMeshPath);

				Mesh newMesh = new Mesh();

				newMesh.vertices = mesh.vertices;
				newMesh.colors = mesh.colors;
				newMesh.normals = mesh.normals;
				newMesh.uv = mesh.uv;
				newMesh.bindposes = mesh.bindposes;
				newMesh.boneWeights = mesh.boneWeights;
				newMesh.tangents = mesh.tangents;
				newMesh.subMeshCount = mesh.subMeshCount;

				for (int index = 0; index < mesh.subMeshCount; index++)
				{
					newMesh.SetTriangles(mesh.GetTriangles(index), index);
				}

				AssetDatabase.CreateAsset(newMesh, outMeshPath);
				Debug.Log("Saving mesh into " + outMeshPath);
				return AssetDatabase.LoadAssetAtPath(outMeshPath, typeof(Mesh)) as Mesh;
			}
		}
		void RunShader(MeshModifier m)
		{

			ComputeBuffer driverBuffer = new ComputeBuffer(driverInfo.Length, 16);
			driverBuffer.SetData(driverInfo);

			int kernelMove = m._shader.FindKernel("MoveVertex");
			m._shader.SetBuffer(kernelMove, "vertexBuffer", vertexBuffer);
			m._shader.SetBuffer(kernelMove, "driverBuffer", driverBuffer);
			m._shader.Dispatch(kernelMove, m._vertexData.Length / 16, 1, 1);



			vertexBuffer.GetData(m._vertexDataOutput);

			for (int i = 0; i < m._vertexDataOutput.Length; i++)
			{
				m._verts[i] = m._vertexDataOutput[i].point;
			}

			m._newMesh.vertices = m._verts;
			if (m._smr != null)
				m._smr.sharedMesh = m._newMesh;
			else
				m._mf.sharedMesh = m._newMesh;
			driverBuffer.Release();
		}

		public RaycastHit hit;


		void OnSceneGUI()
		{
			foreach (MeshModifier m in _meshModifiers)
			{
				Tools.current = Tool.None;
				Camera cam = SceneView.GetAllSceneCameras()[0];
				m.Radius = Mathf.Max(0.000001f, m.Radius);
				driverInfo[0].radius = 1f / m.Radius;
				Event e = Event.current;

				if (!m.ShiftDown && e.shift)
					m._previousTool = m.paintType;
				if (e.shift)
				{
					m.ShiftDown = true;

				}
				if (!m.ControlDown && e.control)
					m._previousTool = m.paintType;

				if (e.control)
				{
					m.ControlDown = true;

				}


				switch (e.type)
				{
					case EventType.KeyDown:
						if (e.keyCode == KeyCode.B)
						{
							mousePosition = Event.current.mousePosition;
							PrevRadius = m.Radius;
							if(!DrawingStarted)
								Drawing = false;
							RadiusChange = true;
						}
						if (e.keyCode == KeyCode.LeftAlt)
						{
							if (!DrawingStarted)
								Drawing = false;
						}

						if (e.keyCode == KeyCode.W)
						{
							m.paintType = MeshModifier.PaintType.Move;
						}
						if (e.keyCode == KeyCode.E)
						{
							m.paintType = MeshModifier.PaintType.Pull;
						}
						if (e.keyCode == KeyCode.R)
						{
							m.paintType = MeshModifier.PaintType.Push;
						}
						if (e.keyCode == KeyCode.T)
						{
							m.paintType = MeshModifier.PaintType.Smooth;
						}
						if (e.keyCode == KeyCode.Y)
						{
							m.paintType = MeshModifier.PaintType.Erase;
						}


						break;

					case EventType.KeyUp:
						if (e.keyCode == KeyCode.B)
						{
							RadiusChange = false;
							Drawing = true;
						}
						if (e.keyCode == KeyCode.LeftAlt)
						{
							Drawing = true;
						}


						break;
					case EventType.MouseDown:

						if (Drawing && Event.current.button == 0)
						{
							DrawingStarted = true;
							Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
							if (Physics.Raycast(ray, out hit, 100.0f))
							{
								driverInfo[0].point = m.transform.InverseTransformPoint(hit.point);
								Hit = true;
							}
							driverInfo[1].point = driverInfo[0].point;
							screenPos = cam.WorldToScreenPoint(hit.point);
						}
						break;
					case EventType.MouseDrag:

						if (Drawing && Hit)
						{
							DrawingStarted = true;
							driverInfo[1].point = m.transform.InverseTransformPoint(newPos);

						}

						break;
					case EventType.MouseUp:
						if (Drawing)
						{
							DrawingStarted = false;
							Hit = false;

							m._meshCol.sharedMesh = null;
							m._meshCol.sharedMesh = m._newMesh;
							driverInfo[1].point = driverInfo[0].point;

							if (_useComputeShader)
							{
								for (int i = 0; i < m._vertexDataOutput.Length; i++)
								{
									m._vertexData[i].prevPoint = m._vertexDataOutput[i].point;
									m._vertexData[i].point = m._vertexDataOutput[i].point;
								}

								vertexBuffer.Release();
								vertexBuffer = new ComputeBuffer(m._vertexData.Length, 64);
								vertexBuffer.SetData(m._vertexData);
							}
						}
						break;



				}
				if (m.ShiftDown)
					m.paintType = MeshModifier.PaintType.Smooth;
				if (m.ControlDown)
				{
					if (m.paintType == MeshModifier.PaintType.Move || m.paintType == MeshModifier.PaintType.Smooth)
						m.paintType = MeshModifier.PaintType.Erase;
					if (m._previousTool == MeshModifier.PaintType.Push)
					{
						m.paintType = MeshModifier.PaintType.Pull;
					}
					else if (m._previousTool == MeshModifier.PaintType.Pull)
					{
						m.paintType = MeshModifier.PaintType.Push;
					}
				}

				if (m.ShiftDown && !e.shift)
				{
					m.paintType = m._previousTool;
					m.ShiftDown = false;
				}

				if (m.ControlDown && !e.control)
				{
					m.paintType = m._previousTool;
					m.ControlDown = false;
				}


				if (Drawing)
				{
					Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
					newPos = cam.WorldToScreenPoint(ray.origin);
					newPos.z = screenPos.z;
					newPos = cam.ScreenToWorldPoint(newPos);

					if (Physics.Raycast(ray, out hit, 100000.0f))
					{
						Handles.DrawWireDisc(hit.point, (cam.transform.position - hit.point).normalized, m.Radius);
						if (m.Mirror)
						{
							Vector3 reflPos = Vector3.Reflect(hit.point - m.transform.position, Vector3.left) + m.transform.position;
							Handles.DrawWireDisc(reflPos, (cam.transform.position - hit.point).normalized, m.Radius);
						}

					}
					else
					{
						Handles.DrawWireDisc(newPos, (cam.transform.position - newPos).normalized, m.Radius);
						if (m.Mirror)
						{
							Vector3 reflPos = Vector3.Reflect(newPos - m.transform.position, Vector3.left) + m.transform.position;
							Handles.DrawWireDisc(reflPos, (cam.transform.position - hit.point).normalized, m.Radius);
						}
					}

				}
				else
				{
					Handles.DrawWireDisc(newPos, (cam.transform.position - hit.point).normalized, m.Radius);
					if (m.Mirror)
					{
						Vector3 reflPos = Vector3.Reflect(newPos - m.transform.position, Vector3.left) + m.transform.position;
						Handles.DrawWireDisc(reflPos, (cam.transform.position - hit.point).normalized, m.Radius);
					}
					if (RadiusChange)
						m.Radius = Mathf.Abs(PrevRadius + (Event.current.mousePosition.x - mousePosition.x) * (0.001f * _internalScale));
				}

				GUIUtility.GetControlID(FocusType.Passive);
				HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

				if (_useComputeShader)
					RunShader(m);

				Vector3 dir = driverInfo[1].point - driverInfo[0].point;
				int id = GetClosestVert(dir, driverInfo[0].point, driverInfo[1].point, m);

				Vector3 reflectedStart = m.transform.InverseTransformPoint(Vector3.Reflect(m.transform.TransformPoint(driverInfo[0].point) - m.transform.position, Vector3.left) + m.transform.position);
				Vector3 reflectedEnd = m.transform.InverseTransformPoint(Vector3.Reflect(m.transform.TransformPoint(driverInfo[1].point) - m.transform.position, Vector3.left) + m.transform.position);
				Vector3 dirReflected = reflectedEnd - reflectedStart;
				int id2 = 0;
				if (m.Mirror)
				{
					id2 = GetClosestVert(dirReflected, reflectedStart, reflectedEnd, m);
				}
				Undo.RecordObject(m, "record mesh");
				if (m.selectionType == MeshModifier.SelectionType.Topology)
				{
					SurfaceSearchAndMove(dir, id, driverInfo[0].point.x, m);
					if (m.Mirror)
					{
						//movedVerts
						SurfaceSearchAndMove(dirReflected, id2, reflectedStart.x, m, -1);
					}

				}

				m._newMesh.vertices = m._verts;
				if (m.RecalculateNormals)
				{
					m._newMesh.RecalculateNormals();
					m._newMesh.RecalculateTangents();
				}
				m._newMesh.RecalculateBounds();

				if (m._smr != null)
					m._smr.sharedMesh = m._newMesh;
				else
					m._mf.sharedMesh = m._newMesh;

				Handles.color = new Color(14f / 255f, 229f / 255f, 198f / 255f, 1f);
				Vector3[] p1 = new Vector3[2];
				p1[0] = cam.ScreenToWorldPoint(new Vector3(0, 0, cam.nearClipPlane + 1));
				p1[1] = cam.ScreenToWorldPoint(new Vector3(0, cam.pixelHeight, cam.nearClipPlane + 1));
				Vector3[] p2 = new Vector3[2];
				p2[0] = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, 0, cam.nearClipPlane + 1));
				p2[1] = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, cam.pixelHeight, cam.nearClipPlane + 1));
				Vector3[] p3 = new Vector3[2];
				p3[0] = cam.ScreenToWorldPoint(new Vector3(0, 0, cam.nearClipPlane + 1));
				p3[1] = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, 0, cam.nearClipPlane + 1));
				Vector3[] p4 = new Vector3[2];
				p4[0] = cam.ScreenToWorldPoint(new Vector3(0, cam.pixelHeight, cam.nearClipPlane + 1));
				p4[1] = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, cam.pixelHeight, cam.nearClipPlane + 1));
				Handles.DrawAAPolyLine(15f, p1);
				Handles.DrawAAPolyLine(15f, p2);
				Handles.DrawAAPolyLine(15f, p3);
				Handles.DrawAAPolyLine(15f, p4);

				SceneView.RepaintAll();
			}
		}

		private void SurfaceSearchAndMove(Vector3 dir, int id, float startX, MeshModifier m, int mirror = 1)
		{
			float strength = 1f;
			List<int> movedVerts = new List<int>();
			Queue<MeshModifier.VertexInfoFull> vertQueue = new Queue<MeshModifier.VertexInfoFull>();
			m._vertexDataFull[id].moved = true;
			vertQueue.Enqueue(m._vertexDataFull[id]);
			movedVerts.Add(id);
			while (vertQueue.Count > 0)
			{
				MeshModifier.VertexInfoFull v = vertQueue.Dequeue();
				strength = 1f - v.workingLength / m.Radius;
				if (m.DispayVerts)
				{
					Handles.color = new Color(1, 1, 1.2f, 1);
					Handles.DrawSolidDisc(m.transform.TransformPoint(m._verts[v.id]), Vector3.forward, strength * .0025f);
				}



				Vector3 direction = dir;
				if (m.Mirror)
				{
					if (Mathf.Abs(m._vertexDataFull[v.id].point.x) < 0.001f * _internalScale)
					{
						direction.x = 0;
					}

					if (startX <= 0 && m._vertexDataFull[v.id].point.x <= 0f)
					{
						if (m.paintType == MeshModifier.PaintType.Move)
							Move(direction, m, v.id, strength);
						else if (m.paintType == MeshModifier.PaintType.Erase)
							Erase(direction, v.id, 0, m);
						else if (m.paintType == MeshModifier.PaintType.Smooth)
							Smooth(direction, m, v.id, 0);


						movedVerts.Add(v.id);
					}
					if (startX > 0 && m._vertexDataFull[v.id].point.x > 0f)
					{
						if (m.paintType == MeshModifier.PaintType.Move)
							Move(direction, m, v.id, strength);
						else if (m.paintType == MeshModifier.PaintType.Erase)
							Erase(direction, v.id, 0, m);
						else if (m.paintType == MeshModifier.PaintType.Smooth)
							Smooth(direction, m, v.id, 0);
						movedVerts.Add(v.id);
					}
				}
				else
				{
					if (m.paintType == MeshModifier.PaintType.Move)
						Move(direction, m, v.id, strength);
					else if (m.paintType == MeshModifier.PaintType.Erase)
						Erase(direction, v.id, 0, m);
					else if (m.paintType == MeshModifier.PaintType.Smooth)
						Smooth(direction, m, v.id, 0);
					movedVerts.Add(v.id);
				}

				for (int index = 0; index < v.connectedIndexes.Length; index++)
				{
					float newLength = v.connectedLengths[index] + v.workingLength;
					if (!m._vertexDataFull[v.connectedIndexes[index]].moved && newLength < m.Radius)
					{
						m._vertexDataFull[v.connectedIndexes[index]].moved = true;
						m._vertexDataFull[v.connectedIndexes[index]].workingLength = newLength;
						movedVerts.Add(v.connectedIndexes[index]);
						vertQueue.Enqueue(m._vertexDataFull[v.connectedIndexes[index]]);
					}
				}
			}

			for (int i = 0; i < movedVerts.Count; i++)
			{
				m._vertexDataFull[movedVerts[i]].moved = false;
				m._vertexDataFull[movedVerts[i]].workingLength = 0f;
			}
		}

		public int GetClosestVert(Vector3 dir, Vector3 startPos, Vector3 endPos, MeshModifier m)
		{
			int id = 0;
			float dist = 9999999;
			for (int i = 0; i < m._vertexDataFull.Length; i++)
			{
				m._vertexDataFull[i].moved = false;
				m._vertexDataFull[i].workingLength = 0f;
				if (dir.magnitude == 0f)
					m._vertexDataFull[i].point = m._verts[i];


				float testDist = Vector3.Distance(m._vertexDataFull[i].point, startPos);

				if (dist > testDist)
				{
					id = i;
					dist = testDist;
				}

				if (m.paintType == MeshModifier.PaintType.Move && m.selectionType == MeshModifier.SelectionType.World)
				{

					float strength = 1f - Mathf.Clamp01(testDist / m.Radius);
					if (strength > 0)
					{
						Vector3 direction = dir;
						if (m.Mirror)
						{
							if (Mathf.Abs(m._vertexDataFull[i].point.x) < 0.001f*_internalScale)
							{
								direction.x = 0;
							}


							if (startPos.x >= 0 && m._vertexDataFull[i].point.x >= 0)
								m._verts[i] = m._vertexDataFull[i].point + direction * strength;

							if (startPos.x < 0 && m._vertexDataFull[i].point.x < 0)
								m._verts[i] = m._vertexDataFull[i].point + direction * strength;

						}
						else
							m._verts[i] = m._vertexDataFull[i].point + direction * strength;
					}

					if (m.DispayVerts)
					{
						Handles.color = new Color(1, 1, 1, 1);
						Handles.DrawSolidDisc(m.transform.TransformPoint(m._verts[i]), Vector3.forward, strength * .0025f);
					}



				}
				else if (m.selectionType == MeshModifier.SelectionType.World)
				{

					if (m.paintType == MeshModifier.PaintType.Erase)
					{
						Erase(dir, i, testDist, m);
					}
					else if (m.paintType == MeshModifier.PaintType.Smooth)
					{
						Smooth(dir, m, i, testDist);
					}
				}
			}
			if (m.paintType == MeshModifier.PaintType.Push || m.paintType == MeshModifier.PaintType.Pull)
			{

				for (int i = 0; i < m._vertexDataFull.Length; i++)
				{
					float testDist = Vector3.Distance(m._vertexDataFull[i].point, endPos);

					float strength = 1f - Mathf.Clamp01(testDist / m.Radius);
					if (strength > 0 && dir.magnitude > 0f)
					{
						Vector3 pushDirection = m._newMesh.normals[i].normalized;
						if (m.selectionType == MeshModifier.SelectionType.World)
						{
							pushDirection = m._newMesh.normals[id].normalized;

						}
						if (m.paintType == MeshModifier.PaintType.Push)
							m._verts[i] -= pushDirection * strength * .0005f * m.Strength * _internalScale;
						else
							m._verts[i] += pushDirection * strength * .0005f * m.Strength * _internalScale;

						if (m.selectionType == MeshModifier.SelectionType.Topology)
						{
							for (int index = 0; index < m._vertexDataFull[i].connectedIndexes.Length; index++)
							{
								if (m._vertexDataFull[i].connectedLengths[index] == 0)
									m._verts[m._vertexDataFull[i].connectedIndexes[index]] = m._verts[i];
							}
						}
					}

				}
			}

			return id;
		}
		private static void Move(Vector3 dir, MeshModifier m, int i, float strength)
		{
			m._verts[i] = m._vertexDataFull[i].point + dir * strength;
			for (int index = 0; index < m._vertexDataFull[i].connectedIndexes.Length; index++)
			{
				if (m._vertexDataFull[i].connectedLengths[index] == 0)
					m._verts[m._vertexDataFull[i].connectedIndexes[index]] = m._verts[i];
			}
		}
		private static void Smooth(Vector3 dir, MeshModifier m, int i, float testDist)
		{
			float strength = 1f - Mathf.Clamp01(testDist / m.Radius);
			if (strength > 0 && dir.magnitude > 0)
			{
				Vector3 average = m._verts[i];
				for (int index = 0; index < m._vertexDataFull[i].connectedIndexes.Length; index++)
				{
					average += m._verts[m._vertexDataFull[i].connectedIndexes[index]];
				}
				average /= m._vertexDataFull[i].connectedIndexes.Length + 1;
				m._verts[i] = Vector3.Lerp(m._verts[i], average, m.Strength * strength * .1f);
				for (int index = 0; index < m._vertexDataFull[i].connectedIndexes.Length; index++)
				{
					if (m._vertexDataFull[i].connectedLengths[index] == 0)
						m._verts[m._vertexDataFull[i].connectedIndexes[index]] = m._verts[i];
				}
			}

		}

		private void Erase(Vector3 dir, int i, float testDist, MeshModifier m)
		{
			float strength = 1f - Mathf.Clamp01((testDist / m.Radius));
			if (strength > 0 && dir.magnitude > 0f)
			{
				m._verts[i] = Vector3.Lerp(m._verts[i], m._vertexDataOutput[i].point, strength * .025f * m.Strength);

			}

		}

		public Texture2D TakeScreenShot()
		{
			Camera viewCam = SceneView.lastActiveSceneView.camera;

			return Screenshot(viewCam);
		}

		Texture2D Screenshot(Camera cam)
		{
			if (cam == null)
			{
				Debug.LogWarning("Need a preview camera on the selected BlendShape");
				return null;
			}

			int resWidth = 80; //cam.pixelWidth;
			int resHeight = 80;// cam.pixelHeight;
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

		public Texture2D SaveScreenshotToFile(string fileName, Camera cam)
		{
			Texture2D screenShot = Screenshot(cam);
			byte[] bytes = screenShot.EncodeToPNG();
			Debug.Log("Saving " + fileName);

			System.IO.File.WriteAllBytes(fileName, bytes);
			return screenShot;
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
