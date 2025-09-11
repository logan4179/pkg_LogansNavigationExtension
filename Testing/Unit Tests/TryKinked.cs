using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.UI.Image;

namespace LogansNavigationExtension
{
    public class TryKinked : MonoBehaviour
    {
		/*
		I'm considering that determining if there are any "kinks" in the navmesh 
		(triangles that are pointed against the surface normal) may be useful 
		for some operations...
		 */

        public Transform transA;
        public Transform transB;

		[Header("Angle")]
		public Vector3 angleParamA;
		public Vector3 angleParamB;
		public float AngleResult;

		[Header("Dot")]
		public Vector3 dotParamA;
		public Vector3 dotParamB;
		public float DotResult;
		public float DotResultSign;

		private void OnDrawGizmos()
		{
			angleParamA = transA.up;
			angleParamB = transB.up;

			AngleResult = Vector3.Angle( angleParamA, angleParamB);




			dotParamA = transA.up;
			dotParamB = transB.up;

			DotResult = Vector3.Dot( dotParamA, dotParamB);
			DotResultSign = Mathf.Sign( DotResult);

		}
	}
}
