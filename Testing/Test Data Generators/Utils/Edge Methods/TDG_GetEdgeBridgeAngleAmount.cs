using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_GetEdgeBridgeAngleAmount : TDG_base
    {
        public ComponentGrabber Grabber_PerspectiveEdge;
        public ComponentGrabber Grabber_OtherEdge;

		public LNX_Edge CurrentPerspectiveEdge => Grabber_PerspectiveEdge.CurrentlyGrabbedEdge;
		public LNX_Edge CurrentOtherEdge => Grabber_OtherEdge.CurrentlyGrabbedEdge;

		[Header("RESULTS")]
		public float CurrentOperationResult = -1f;

		[Header("GOTO")]
		public TDG_GetWidestEdgeFromPerspective_edgeOverload _tdgA;

		[Header("DEBUG")]
		public string DBG_Method;
		public Color Color_Edges;
		public Color Color_BridgeVisual;

		#region HELPERS========================================
		public void GrabComponents()
		{
			Grabber_PerspectiveEdge.GrabComponent();
			Grabber_OtherEdge.GrabComponent();
		}

		[ContextMenu("z call GoToTDG()")]
		public void GoToTDG()
		{
			Grabber_PerspectiveEdge.transform.position = _tdgA.PerspectiveEdgeGrabber.transform.position;
			Grabber_OtherEdge.transform.position = _tdgA.OtherTriGrabber.transform.position;

			GrabComponents();
		}
		#endregion

		protected override void OnDrawGizmos()
		{
            if( Selection.activeGameObject != gameObject && 
                Selection.activeGameObject != Grabber_PerspectiveEdge.gameObject &&
                Selection.activeGameObject != Grabber_OtherEdge.gameObject
            )
            {
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
            }


			DBG_Operation = "";
			DBG_Method = "";
			CurrentOperationResult = -1f;

			base.OnDrawGizmos();
			bool prblm = false;

			Gizmos.DrawSphere(Grabber_PerspectiveEdge.transform.position, Radius_ObjectDebugSpheres);
			Handles.Label(Grabber_PerspectiveEdge.transform.position + (Vector3.up * Radius_ObjectDebugSpheres), "PrspctvEdge");

			Gizmos.DrawSphere(Grabber_OtherEdge.transform.position, Radius_ObjectDebugSpheres);
			Handles.Label(Grabber_OtherEdge.transform.position + (Vector3.up * Radius_ObjectDebugSpheres), "OtherEdge");


			if (CurrentPerspectiveEdge != null)
			{
				DrawStandardEdgeFocusGizmos(CurrentPerspectiveEdge, 0.15f, "", Color_Edges);
			}
			else
			{
				DBG_Operation += $"Problem! CurrentPerspectiveEdge is null...\n";
				prblm = true;
			}

			if (CurrentOtherEdge != null)
			{
				DrawStandardEdgeFocusGizmos(CurrentOtherEdge, 0.15f, "", Color_Edges);
			}
			else
			{
				DBG_Operation += $"Problem! CurrentOtherEdge is null...\n";
				prblm = true;
			}

			if (prblm)
			{
				DBG_Operation += $"Can't resolve one of the parameters. Returning early...\n";
				return;
			}

			DBG_Operation += $"using prspctvEdge: '{CurrentPerspectiveEdge}' and otherEdge: '{CurrentOtherEdge}'\n" +
				$"edges aligned?: '{LNX_Utils.AreEdgesAlignedFromTheirPerspectives(CurrentPerspectiveEdge, CurrentOtherEdge)}'\n" +
				$"\nCommencing operation...\n";

			CurrentOperationResult = LNX_Utils.GetEdgeBridgeAngleAmount( CurrentPerspectiveEdge, CurrentOtherEdge );

			DBG_Operation += $"Result: '{CurrentOperationResult}'\n";

			DrawEdgeBridgeVisual(CurrentPerspectiveEdge, CurrentOtherEdge, Color_BridgeVisual);
		}
    }
}
