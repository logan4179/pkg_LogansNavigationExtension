using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_DoesEdgeObstructQuadArea : TDG_base
    {
        public LNX_ComponentGrabber Grabber_ObstructEdge;
		public LNX_Edge ObstructEdge => Grabber_ObstructEdge.CurrentlyGrabbedEdge;

        public LNX_ComponentGrabber Grabber_crnrA;
        public LNX_ComponentGrabber Grabber_crnrB;
        public LNX_ComponentGrabber Grabber_crnrC;
        public LNX_ComponentGrabber Grabber_crnrD;

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
				Grabber_crnrA.transform.position, Grabber_crnrB.transform.position, Grabber_crnrC.transform.position, Grabber_crnrD.transform.position
			);
		}
		#endregion

		protected override void OnDrawGizmos()
		{
			if
			(
				Selection.activeObject != gameObject &&
				Selection.activeGameObject != Grabber_ObstructEdge.gameObject &&
				Selection.activeGameObject != Grabber_crnrA.gameObject &&
				Selection.activeGameObject != Grabber_crnrB.gameObject &&
				Selection.activeGameObject != Grabber_crnrC.gameObject &&
				Selection.activeGameObject != Grabber_crnrD.gameObject
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

			Grabber_crnrA.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_crnrB.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_crnrC.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_crnrD.DrawMyGizmos(Radius_ObjectDebugSpheres);


			LNX_Quad q = new LNX_Quad( 
				Grabber_crnrA.transform.position, Grabber_crnrB.transform.position, Grabber_crnrC.transform.position,
				Grabber_crnrD.transform.position );

			DBG_Operation += $"using ObstructEdge: '{ObstructEdge}', and quad: '{q}'...\n";

			DBG_Operation += $"Commencing operation...\n";

			CurrentResult = LNX_Utils.DoesEdgeObstructQuadArea( 
				ObstructEdge, Grabber_crnrA.transform.position, Grabber_crnrB.transform.position, 
				Grabber_crnrC.transform.position, Grabber_crnrD.transform.position, 
				true, ref DBG_Method 
			);

			DBG_Operation += $"Operation returned: '{CurrentResult}'\n";

			if( CurrentResult )
			{
				Gizmos.color = Color.green;
			}
			else
			{
				Gizmos.color = Color.red;
			}

			DrawQuadVisual(q);

		}
	}
}
