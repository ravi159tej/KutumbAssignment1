using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode]
public class LookAtControl : MonoBehaviour
{
	private Mesh _mesh;
	void Start()
	{
		string path = "ViewFinder";
		_mesh = (Mesh)Resources.Load(path, typeof(Mesh));
	}

	private void OnDrawGizmos()
	{
	
		Gizmos.DrawMesh(_mesh, 0, transform.position, transform.rotation, transform.localScale);
		

	}
}
