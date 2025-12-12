#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using System.IO;

namespace PuppetFace
{
	[ExecuteInEditMode]
	[AddComponentMenu("Puppet Face/BlendShapeManager")]
	public class BlendShapeManager : MonoBehaviour
	{
		public Texture2D[] BlendShapeTextures = new Texture2D[0];
		public SkinnedMeshRenderer Skin;
		public string[] BlendShapes;
		public bool NeedsInitializing = false;
		public int index = 0;
		public int flags = -1;
		public int BlendShapeType = 2;
		private float _timer = -1f;
		public void Update()
		{
			if (NeedsInitializing && BlendShapes.Length>1)
			{
				if (_timer >= 0f)
				{
					_timer += Time.deltaTime;
					if (_timer > .1f)
					{
						index++;
						_timer = -1f;
					}
				}
				else
				{
					List<Texture2D> currentTextures = new List<Texture2D>(BlendShapeTextures);

					if (index > 0)
						Skin.SetBlendShapeWeight(index - 1, 0);

					if (index < BlendShapes.Length - 1)
						Skin.SetBlendShapeWeight(index, 100);

					Camera cam = SceneView.lastActiveSceneView.camera;

					if (index == 0)
					{
						Screenshot(cam);
						_timer = 0f;
					}
					else
					{
						currentTextures.Add(Screenshot(cam));

						if (index < BlendShapes.Length - 1)
							_timer = 0f;

						else
						{
							NeedsInitializing = false;

						}
						BlendShapeTextures = currentTextures.ToArray();
					}
				}
			}
		}
		public Texture2D Screenshot(Camera cam)
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
			//cam.aspect = (float)resWidth / (float)resHeight;

			RenderTexture rt = new RenderTexture(resWidth, resHeight, 32);
			cam.targetTexture = rt;
			Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.ARGB32, false);
			cam.Render();
			RenderTexture.active = rt;
			screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
			screenShot.Apply();
			cam.targetTexture = null;
			RenderTexture.active = null; 
			DestroyImmediate(rt);
			return screenShot;
		}
		public static void SetBlendShape(GameObject blendShapeGO,  SkinnedMeshRenderer TargetSkin,int BlendShapeType)
		{
			string BlendShapeName = blendShapeGO.name;
			Mesh blendShapeMesh = GetMesh(blendShapeGO);

			Mesh meshCurrent = new Mesh(); // TargetSkin.sharedMesh;
			if (BlendShapeType == 2)
				TargetSkin.BakeMesh(meshCurrent);
			else
				meshCurrent = TargetSkin.sharedMesh;

			Vector3[] deltas = new Vector3[blendShapeMesh.vertexCount];
			Vector3[] deltaNormals = new Vector3[blendShapeMesh.vertexCount];
			Vector3[] deltaTangents = new Vector3[blendShapeMesh.vertexCount];

			int vertCount = blendShapeMesh.vertexCount;
			Vector3[] verts = blendShapeMesh.vertices;
			Vector3[] vertsCurrent = meshCurrent.vertices;
			Vector3[] normals = blendShapeMesh.normals;
			Vector4[] tangents = blendShapeMesh.tangents;
			Vector3[] normalsCurrent = TargetSkin.sharedMesh.normals;
			Vector4[] tangentsCurrent = TargetSkin.sharedMesh.tangents;

			for (int i = 0; i < vertCount; i++)
			{
				deltas[i] = verts[i] - vertsCurrent[i];
				deltaNormals[i] = normals[i] - normalsCurrent[i];
				deltaTangents[i] = tangents[i] - tangentsCurrent[i];

			}


			ReplaceBlendShape(blendShapeMesh, TargetSkin, BlendShapeName, deltas, deltaNormals, deltaTangents);

		}

		public static void ReplaceBlendShape(Mesh blendShapeMesh, SkinnedMeshRenderer TargetSkin, string blendShapeName, Vector3[] deltasNew, Vector3[] deltaNormalsNew, Vector3[] deltaTangentsNew)
		{

			

			Mesh tmpMesh = new Mesh();
			tmpMesh.vertices = TargetSkin.sharedMesh.vertices;
			Vector3[] dVertices = new Vector3[TargetSkin.sharedMesh.vertexCount];
			Vector3[] dNormals = new Vector3[TargetSkin.sharedMesh.vertexCount];
			Vector3[] dTangents = new Vector3[TargetSkin.sharedMesh.vertexCount];
			bool added = false;
			for (int shape = 0; shape < TargetSkin.sharedMesh.blendShapeCount; shape++)
			{
				for (int frame = 0; frame < TargetSkin.sharedMesh.GetBlendShapeFrameCount(shape); frame++)
				{
					string shapeName = TargetSkin.sharedMesh.GetBlendShapeName(shape);
					float frameWeight = TargetSkin.sharedMesh.GetBlendShapeFrameWeight(shape, frame);

					TargetSkin.sharedMesh.GetBlendShapeFrameVertices(shape, frame, dVertices, dNormals, dTangents);

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
			Mesh myMesh = SaveMesh(TargetSkin.sharedMesh);
			TargetSkin.sharedMesh = myMesh;
			TargetSkin.sharedMesh.ClearBlendShapes();

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
				TargetSkin.sharedMesh.AddBlendShapeFrame(blendShapeName, 100f, deltasNew, deltaNormalsNew, deltaTangentsNew);
			}
			EditorUtility.SetDirty(TargetSkin.sharedMesh);
			AssetDatabase.SaveAssets();


		}

		public void DeleteBlendShape(SkinnedMeshRenderer TargetSkin, string blendShapeName)
		{

			Mesh myMesh = SaveMesh(TargetSkin.sharedMesh);
			TargetSkin.sharedMesh = myMesh;

			Mesh tmpMesh = new Mesh();
			tmpMesh.vertices = myMesh.vertices;
			Vector3[] dVertices = new Vector3[myMesh.vertexCount];
			Vector3[] dNormals = new Vector3[myMesh.vertexCount];
			Vector3[] dTangents = new Vector3[myMesh.vertexCount];
			for (int shape = 0; shape < myMesh.blendShapeCount; shape++)
			{
				for (int frame = 0; frame < myMesh.GetBlendShapeFrameCount(shape); frame++)
				{
					string shapeName = myMesh.GetBlendShapeName(shape);
					if (shapeName != blendShapeName)
					{
						float frameWeight = myMesh.GetBlendShapeFrameWeight(shape, frame);
						myMesh.GetBlendShapeFrameVertices(shape, frame, dVertices, dNormals, dTangents);
						tmpMesh.AddBlendShapeFrame(shapeName, frameWeight, dVertices, dNormals, dTangents);
					}
				}
			}

			TargetSkin.sharedMesh.ClearBlendShapes();

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
			
			EditorUtility.SetDirty(TargetSkin.sharedMesh);
			AssetDatabase.SaveAssets();


		}
		public static Mesh SaveMesh(Mesh mesh, bool Duplicate = false)
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
		public static Mesh GetMesh(GameObject go)
		{
			MeshFilter mf = go.GetComponent<MeshFilter>();
			if (mf && mf.sharedMesh)
				return mf.sharedMesh;

			SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
			if (smr && smr.sharedMesh)
				return smr.sharedMesh;
			
			return null;
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
			arr[m.blendShapeCount] = "Make Blend Shape";
			return arr;
		}
	}
}
#endif
