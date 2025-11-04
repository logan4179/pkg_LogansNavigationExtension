using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_DoesEdgeObstructEdgePath : TDG_base
    {
		//TODO: Implement in manager and create tests!

		public ComponentGrabber Grabber_StartEdge;
        public ComponentGrabber Grabber_EndEdge;
		public ComponentGrabber Grabber_ObstructEdge;

		public LNX_Edge CurrentStartEdge => Grabber_StartEdge.CurrentlyGrabbedEdge;
		public LNX_Edge CurrentEndEdge => Grabber_EndEdge.CurrentlyGrabbedEdge;
		public LNX_Edge CurrentObstructEdge => Grabber_ObstructEdge.CurrentlyGrabbedEdge;

		[Header("RESULTS")]
		public bool CurrentOperationResult = false;

		[Header("DEBUG")]
		public string DBG_Method;
		
		public Color Color_Edges;
		public Color Color_BridgeVisual;

		[ContextMenu("z call GrabComponents")]
		public void CaptureDataPoint()
		{
			_dataCapture.CaptureDataPoint( CurrentOperationResult, CurrentStartEdge.MidPosition_flat, CurrentEndEdge.MidPosition_flat, CurrentObstructEdge.MidPosition_flat );
		}

		#region HELPERS ---------------------------------------------------
		[ContextMenu("z call GrabComponents")]
		public void	GrabComponents()
		{

		}

		/*
		[ContextMenu("z call DoEet()")]
		public void DoEet()
		{

		}
		*/

		/*[ContextMenu("z call GotoIt()")]
		public void GotoIt()
		{

		}
		*/

		/*
		[ContextMenu("z call SendToDataPoint")]
		public void SendToDataPoint()
		{
			transform.position = CapturedPerspectivePositions[Index_GoToDataPoint];
		}
		*/

		[ContextMenu("z call CaptureProblemPoint")]
		public void CaptureProblemPoint()
		{
			_dataCapture_problems.CaptureDataPoint(CurrentOperationResult, CurrentStartEdge.MidPosition_flat, CurrentEndEdge.MidPosition_flat, CurrentObstructEdge.MidPosition_flat);
		}
		#endregion


		protected override void OnDrawGizmos()
		{
			if
			(
				Selection.activeGameObject != gameObject &&
				Selection.activeGameObject != Grabber_StartEdge.gameObject &&
				Selection.activeGameObject != Grabber_EndEdge.gameObject && 
				Selection.activeGameObject != Grabber_ObstructEdge.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			DBG_Operation = "";
			DBG_Method = "";
			CurrentOperationResult = false;

			base.OnDrawGizmos();
			bool prblm = false;

			Gizmos.DrawSphere(Grabber_StartEdge.transform.position, Radius_ObjectDebugSpheres);
			Handles.Label(Grabber_StartEdge.transform.position + (Vector3.up * Radius_ObjectDebugSpheres * 0.75f), "GrabStart");

			Gizmos.DrawSphere(Grabber_EndEdge.transform.position, Radius_ObjectDebugSpheres);
			Handles.Label(Grabber_EndEdge.transform.position + (Vector3.up * Radius_ObjectDebugSpheres * 0.75f), "GrabEnd");

			Gizmos.DrawSphere(Grabber_ObstructEdge.transform.position, Radius_ObjectDebugSpheres);
			Handles.Label(Grabber_ObstructEdge.transform.position + (Vector3.up * Radius_ObjectDebugSpheres * 0.75f), "GrabObstrct");


			if ( CurrentStartEdge != null )
			{
				DrawStandardEdgeFocusGizmos( CurrentStartEdge, 0.15f, "", Color_Edges );
			}
			else
			{
				DBG_Operation += $"Problem! CurrentStartEdge is null...\n";
				prblm = true;
			}

			if (CurrentEndEdge != null)
			{
				DrawStandardEdgeFocusGizmos(CurrentEndEdge, 0.15f, "", Color_Edges);
			}
			else
			{
				DBG_Operation += $"Problem! CurrentEndEdge is null...\n";
				prblm = true;
			}

			if (CurrentObstructEdge == null)
			{
				DBG_Operation += $"Problem! CurrentObstructEdge is null...\n";
				prblm = true;
			}

			if ( !prblm )
			{
				DBG_Operation += $"using start: '{CurrentStartEdge}' and end: '{CurrentEndEdge}'\n" +
					$"with obstruct: '{CurrentObstructEdge}'\n" +
					$"edges aligned?: '{LNX_Utils.AreEdgesAlignedFromTheirPerspectives(CurrentStartEdge, CurrentEndEdge)}'\n" +
					$"\nCommencing operation...\n";

				CurrentOperationResult = LNX_Utils.DoesEdgeObstructEdgePath(CurrentObstructEdge, CurrentStartEdge, CurrentEndEdge, ref DBG_Method);
			}
			else
			{
				DBG_Operation += $"Can't resolve one of the parameters. Returning early...\n";
				return;
			}

				DBG_Operation += $"Result: '{CurrentOperationResult}'\n";

			if( CurrentOperationResult )
			{
				DBG_Operation += $"The edge DOES obstruct...\n";
				DrawStandardEdgeFocusGizmos(CurrentObstructEdge, 0.15f, "", Color.red, true );
			}
			else
			{
				DBG_Operation += $"The edge does NOT obstruct...\n";

				DrawStandardEdgeFocusGizmos(CurrentObstructEdge, 0.15f, "", Color.green, true );
			}

			DrawEdgeBridgeVisual(CurrentStartEdge, CurrentEndEdge, Color_BridgeVisual );

		}

		#region WRITING ----------------------------------------------------
		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			bool rslt = TDG_Manager.WriteTestObjectToJson(TDG_Manager.filePath_testData_isInCenterSweep, this);

			if (rslt)
			{
				LastWriteTime = System.DateTime.Now.ToString();
				return true;
			}

			return false;
		}
		#endregion
	}
}
