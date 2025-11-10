using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_GetEdgeBridgeAngleAmount : TDG_base
    {
        public LNX_ComponentGrabber Grabber_PerspectiveEdge;
        public LNX_ComponentGrabber Grabber_OtherEdge;

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

			Grabber_PerspectiveEdge.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_OtherEdge.DrawMyGizmos(Radius_ObjectDebugSpheres);

			if (CurrentPerspectiveEdge == null)
			{
				DBG_Operation += $"{nameof(CurrentPerspectiveEdge)} was null. Returning early...\n";
			}
			if (CurrentOtherEdge == null)
			{
				DBG_Operation += $"{nameof(CurrentOtherEdge)} was null. Returning early...\n";
			}

			DrawStandardEdgeFocusGizmos(CurrentPerspectiveEdge, 0.15f, CurrentPerspectiveEdge.ToString(), Color.yellow);
			DrawStandardEdgeFocusGizmos(CurrentOtherEdge, 0.15f, CurrentOtherEdge.ToString(), Color.yellow);

			DBG_Operation += $"using prspctvEdge: '{CurrentPerspectiveEdge}' and otherEdge: '{CurrentOtherEdge}'\n" +
				$"edges aligned?: '{LNX_Utils.AreEdgesAlignedFromTheirPerspectives(CurrentPerspectiveEdge, CurrentOtherEdge)}'\n" +
				$"\nCommencing operation...\n";

			CurrentOperationResult = LNX_Utils.GetEdgeBridgeAngleAmount( CurrentPerspectiveEdge, CurrentOtherEdge );

			DBG_Operation += $"Result: '{CurrentOperationResult}'\n";

			DrawEdgeBridgeVisual(CurrentPerspectiveEdge, CurrentOtherEdge, Color_BridgeVisual);
		}
    }
}
