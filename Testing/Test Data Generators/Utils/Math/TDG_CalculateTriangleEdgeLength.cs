using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_CalculateTriangleEdgeLength : TDG_base
    {
		public LNX_ComponentGrabber Grabber_AngA;
        public LNX_ComponentGrabber Grabber_AngB;
		public LNX_ComponentGrabber Grabber_AngC;

		[Header("RESULTS")]
		public float Result;

		[Header("DEBUG")]
		public Color Clr_edges = Color.white;
		
		public string DBG_Method;

		protected override void OnDrawGizmos()
		{
			if
			(
				Selection.activeGameObject != gameObject &&
				Selection.activeGameObject != Grabber_AngA.gameObject &&
				Selection.activeGameObject != Grabber_AngB.gameObject &&
				Selection.activeGameObject != Grabber_AngC.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			DBG_Operation = "";
			DBG_Method = "";

			base.OnDrawGizmos();

			Grabber_AngA.DrawMyGizmos( Radius_ObjectDebugSpheres );
			Grabber_AngB.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_AngC.DrawMyGizmos(Radius_ObjectDebugSpheres);

			float angA = Vector3.Angle
			(
				LNX_Utils.FlatVector(Grabber_AngB.transform.position - Grabber_AngA.transform.position).normalized,
				LNX_Utils.FlatVector(Grabber_AngC.transform.position - Grabber_AngA.transform.position).normalized
			);

			float angB = Vector3.Angle
			(
				LNX_Utils.FlatVector(Grabber_AngA.transform.position - Grabber_AngB.transform.position).normalized,
				LNX_Utils.FlatVector(Grabber_AngC.transform.position - Grabber_AngB.transform.position).normalized
			);

			float lenB = Vector3.Distance
			(
				LNX_Utils.FlatVector(Grabber_AngA.transform.position),
				LNX_Utils.FlatVector(Grabber_AngC.transform.position)
			);

			DBG_Operation += $"Commencing operation...\n";

			Result = LNX_Utils.CalculateTriangleEdgeLength( angA, angB, lenB );

			DBG_Operation += $"result: '{Result}'\n" +
				$"when verified: '{Vector3.Distance(LNX_Utils.FlatVector(Grabber_AngB.transform.position), LNX_Utils.FlatVector(Grabber_AngC.transform.position))}'";

			DBG_Operation += $"alt: '{LNX_Utils.CalculateTriangleSideLength( -1f, angA, lenB, angB, -1f, -1f )}'\n";

			GUIStyle gstl = new GUIStyle();
			gstl.normal.textColor = Color.green;

			// edgeA
			Handles.Label(
				(Grabber_AngB.transform.position + Grabber_AngC.transform.position) / 2f,
				"lenA(" +
				Vector3.Distance(LNX_Utils.FlatVector(Grabber_AngB.transform.position), LNX_Utils.FlatVector(Grabber_AngC.transform.position)).ToString("#.###") +
				")",
				gstl
			);
			// angA
			Handles.Label(
				Grabber_AngA.transform.position + (Vector3.up * 0.35f),
				angA.ToString()
			);

			// edgeB
			Handles.Label(
				(Grabber_AngA.transform.position + Grabber_AngC.transform.position) / 2f,
				"lenB(" + 
				Vector3.Distance(LNX_Utils.FlatVector(Grabber_AngA.transform.position), LNX_Utils.FlatVector(Grabber_AngC.transform.position)).ToString("#.###") +
				")"
			);
			// angB
			Handles.Label(
				Grabber_AngB.transform.position + (Vector3.up * 0.35f),
				angB.ToString()
			);
         
			// edgeC
			Handles.Label(
				(Grabber_AngA.transform.position + Grabber_AngB.transform.position) / 2f,
				"lenC(" +
				Vector3.Distance(LNX_Utils.FlatVector(Grabber_AngA.transform.position), LNX_Utils.FlatVector(Grabber_AngB.transform.position)).ToString("#.###") +
				")"
			);
			// angC
			/*Handles.Label(
				Grabber_AngC.transform.position + (Vector3.up * 0.25f),
				angC.ToString()
			);*/
		}
	}
}
