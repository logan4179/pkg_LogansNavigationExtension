using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_DoesEdgeObstructArea : TDG_base
    {
		public LNX_ComponentGrabber Grabber_ObstructEdge;
		public LNX_Edge ObstructEdge => Grabber_ObstructEdge.CurrentlyGrabbedEdge;

		public LNX_ComponentGrabber Grabber_vert;
		public LNX_Vertex PerspectiveVert => Grabber_vert.CurrentlyGrabbedVert;
		public LNX_ComponentGrabber Grabber_destinationTri;
		public LNX_Triangle DestinationTriangle => Grabber_destinationTri.CurrentlyGrabbedTriangle;

		public bool CurrentResult = false;


		[Header("DEBUG")]
		public string DBG_Method;

		#region HELPERS ========================================

		[ContextMenu("z call CaptureProblemPosition (derived)")]
		public override void CaptureProblemPosition()
		{
			_dataCapture_problems.CaptureDataPoint
			(
				ObstructEdge.MidPosition,
				PerspectiveVert.V_Position, DestinationTriangle.V_Center
			);
		}
		#endregion

		protected override void OnDrawGizmos()
		{
			if
			(
				Selection.activeObject != gameObject &&
				Selection.activeGameObject != Grabber_ObstructEdge.gameObject &&
				Selection.activeGameObject != Grabber_vert.gameObject &&
				Selection.activeGameObject != Grabber_destinationTri.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			DBG_Operation = "";
			DBG_Method = "";

			base.OnDrawGizmos();

			if (ObstructEdge == null)
			{
				DBG_Operation += $"{nameof(ObstructEdge)} was null. Returning early...\n";
				return;
			}

			DrawStandardEdgeFocusGizmos(ObstructEdge, 0.15f, ObstructEdge.ToString(), Color.yellow);


			if (PerspectiveVert == null)
			{
				DBG_Operation += $"{nameof(PerspectiveVert)} was null. Returning early...\n";
				return;
			}
			Gizmos.DrawSphere(PerspectiveVert.V_Position, Radius_ObjectDebugSpheres);

			if (DestinationTriangle == null)
			{
				DBG_Operation += $"{nameof(DestinationTriangle)} was null. Returning early...\n";
				return;
			}
			DrawStandardFocusTriGizmos(DestinationTriangle, 0.1f, "", Color.magenta, true);

			DBG_Operation += $"using ObstructEdge: '{ObstructEdge}', " +
				$"perspective vert: '{PerspectiveVert}', and dest tri: '{DestinationTriangle}'...\n";

			DBG_Operation += $"Commencing operation...\n";

			CurrentResult = LNX_Utils.DoesEdgeObstructArea(
				ObstructEdge, PerspectiveVert, DestinationTriangle, false, Vector3.up, ref DBG_Method
			);

			DBG_Operation += $"Operation returned: '{CurrentResult}'\n";


			if (CurrentResult)
			{
				Gizmos.color = Color.green;
			}
			else
			{
				Gizmos.color = Color.red;
			}

			Grabber_ObstructEdge.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_vert.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_destinationTri.DrawMyGizmos(Radius_ObjectDebugSpheres);
		}
	}
}
