using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_EdgeEncompassesVertSweep : TDG_base
    {
		public ComponentGrabber Grabber_Vert;
		public ComponentGrabber Grabber_Edge;
		public LNX_Vertex GrabbedVert => Grabber_Vert.CurrentlyGrabbedVert;
		public LNX_Edge GrabbedEdge => Grabber_Edge.CurrentlyGrabbedEdge;

		[Header("RESULTS")]
		public bool CurrentOperationResult;

		[Header("DEBUG")]
		public string DBG_Method;
		public Color Color_Corners;
		public Vector3 v_lblOffset;

		#region HELPERS =======================================
		[ContextMenu("z call CaptureProblemPoint()")]
		public void CaptureProblemPoint()
		{
			_dataCapture_problems.CaptureDataPoint(false, GrabbedVert.V_Position, GrabbedEdge.MidPosition);
		}


		#endregion

		protected override void OnDrawGizmos()
		{
			if
			(
				Selection.activeGameObject != gameObject &&
				Selection.activeGameObject != Grabber_Vert.gameObject &&
				Selection.activeGameObject != Grabber_Edge.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			DBG_Operation = "";
			DBG_Method = "";
			CurrentOperationResult = false;

			Gizmos.color = Color_Corners;
			//Gizmos.DrawSphere(Grabber_Vert.transform.position, Radius_ObjectDebugSpheres);
			Grabber_Vert.DrawMyGizmos(Radius_ObjectDebugSpheres);
			//Handles.Label(Trans_Crnr.position + v_lblOffset, "Vert");

			Grabber_Edge.DrawMyGizmos(Radius_ObjectDebugSpheres);

			if (GrabbedVert == null)
			{
				DBG_Operation += $"Grabbed vert is null. Returning early...\n";
				return;
			}

			if (GrabbedEdge == null)
			{
				DBG_Operation += $"GrabbedEdge is null. Returning early...\n";
				return;
			}

			DBG_Operation += $"using GrabbedVert: '{GrabbedVert}', and GrabbedEdge: '{GrabbedEdge}'...\n";

			DBG_Operation += $"\nCommencing operation...\n";

			CurrentOperationResult = LNX_Utils.EdgeEncompassesVertSweep( GrabbedEdge, GrabbedVert, Vector3.up, ref DBG_Method );

			DBG_Operation += $"\nResult of EdgeEncompassesVertSweep(): '{CurrentOperationResult}'...\n";

			if (CurrentOperationResult)
			{
				Gizmos.color = Color.green;
				DBG_Operation += $"edge DOES encompass vert sweep...\n";
			}
			else
			{
				Gizmos.color = Color.red;
				DBG_Operation += $"edge does NOT encompass vert sweep...\n";
			}

			DrawStandardEdgeFocusGizmos(GrabbedEdge, 0.15f, GrabbedEdge.ToString(), Color.yellow);

			float len = 0.5f;
			Gizmos.DrawSphere(GrabbedVert.V_Position, 0.25f);
			Gizmos.DrawLine(GrabbedVert.V_Position, GrabbedVert.V_Position + (GrabbedVert.V_ToFirstSiblingVert * len));
			Handles.Label(GrabbedVert.V_Position + (GrabbedVert.V_ToFirstSiblingVert * len), "firstSib");

			Gizmos.DrawLine(GrabbedVert.V_Position, GrabbedVert.V_Position + (GrabbedVert.V_ToSecondSiblingVert * len));
			Handles.Label(GrabbedVert.V_Position + (GrabbedVert.V_ToSecondSiblingVert * len), "secondSib");

		}
	}
}
