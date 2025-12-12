using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PuppetFace
{
    [ExecuteInEditMode]
    public class FaceControl : MonoBehaviour
    {
        public float Radius = 0.005f;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(2000,2000,2000);
            Gizmos.DrawSphere(transform.position, Radius);

        }
        
    }
}
