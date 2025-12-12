using UnityEngine;
using System.Collections;
namespace PuppetFace
{
	[ExecuteInEditMode]
	public class MeshModifier : MonoBehaviour
	{

		public struct VertexInfo
		{
			public Vector3 point;
			public Vector3 prevPoint;
		}
		public struct VertexInfoFull
		{
			public Vector3 point;
			public int[] connectedIndexes;
			public float[] connectedLengths;
			public bool moved;
			public int id;
			public float workingLength;
		}
		public enum PaintType { Move, Pull, Push, Smooth, Erase };
		public enum SelectionType { World, Topology };
		public PaintType paintType;
		public SelectionType selectionType;
		public float Radius = 0.02f;
		public float Strength = 1f;
		public bool Mirror = true;
		public SkinnedMeshRenderer TargetSkin;
		[HideInInspector]
		public int Index;
		public float ConnectedVertexThreshold = 0f;
		public bool DispayVerts = false;
		public bool RecalculateNormals = false;
		public int BlendShapeType = 0;

		[HideInInspector]
		public VertexInfo[] _vertexData;
		[HideInInspector]
		public VertexInfo[] _vertexDataOutput;
		[HideInInspector]
		public VertexInfoFull[] _vertexDataFull;
		[HideInInspector]
		public Mesh _newMesh;
		[HideInInspector]
		public MeshFilter _mf;
		[HideInInspector]
		public SkinnedMeshRenderer _smr;
		[HideInInspector]
		public Vector3[] _verts;
		[HideInInspector]
		public Vector3[] _normals;
		[HideInInspector]
		public ComputeShader _shader;
		[HideInInspector]
		public MeshCollider _meshCol;
		[HideInInspector]
		public MeshModifier.PaintType _previousTool = MeshModifier.PaintType.Move;
		[HideInInspector]
		public bool ShiftDown = false;
		[HideInInspector]
		public bool ControlDown = false;

	}
}
