using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_DoesEdgeObstructTriPath : TDG_base
    {
		//TODO: NEED TO CREATE DATA AND FULLY INTEGRATE THIS INTO THE TDG MANAGER CLASS
		//todo: can get rid of a lot of the code here by implementing component grabbers, and datacapturers, etc

		[Header("START OF DERIVED CLASS-------------------")]
		public LNX_ComponentGrabber Grabber_ObstructEdge;
		public LNX_ComponentGrabber Grabber_TriA;
		public LNX_ComponentGrabber Grabber_TriB;

		//[Header("CURRENT RESULTS")]
		[HideInInspector] public bool CurrentResult;
		public LNX_Triangle TriA => Grabber_TriA.CurrentlyGrabbedTriangle;
		public LNX_Triangle TriB => Grabber_TriB.CurrentlyGrabbedTriangle;
		public LNX_Edge ObstructEdge => Grabber_ObstructEdge.CurrentlyGrabbedEdge;

		[Header("DEBUG")]
		public bool ReverseBridgeLineVisuals = false;
		[Range(0f, 0.25f)] public float edgeRaise = 0.15f;
		[Range(0f, 0.1f)] public float triRaise = 0.5f;

		public string DBG_Method;


		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			Debug.Log($"{nameof(CaptureDataPoint)}()");

		}

		#region HELPERS ---------------------------------------------------

		[ContextMenu("z call SendToDataPoint")]
		public void SendToDataPoint()
		{
			//transform.position = CapturedPerspectivePositions[Index_GoToDataPoint];
		}

		[ContextMenu("z call CaptureProblemPosition (derived)")]
		public override void CaptureProblemPosition()
		{
			_dataCapture_problems.CaptureDataPoint(ObstructEdge.MidPosition, TriA.V_Center, TriB.V_Center);
		}

		[ContextMenu("z call SendToProblemPosition (derived)")]
		public void SendToProblemPosition()
		{


		}
		#endregion

		protected override void OnDrawGizmos()
		{
			if
			(
				Selection.activeGameObject != gameObject &&
				Selection.activeGameObject != Grabber_ObstructEdge.gameObject &&
				Selection.activeGameObject != Grabber_TriA.gameObject &&
				Selection.activeGameObject != Grabber_TriB.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			DBG_Operation = "";
			DBG_Method = "";

			base.OnDrawGizmos();

			DrawStandardFocusTriGizmos(TriA, triRaise, "triA", Color.magenta, true, triRaise, true, false);
			Grabber_TriA.DrawMyGizmos(Radius_ObjectDebugSpheres);

			DrawStandardFocusTriGizmos(TriB, triRaise, "triB", Color.magenta, true, triRaise, true, false);
			Grabber_TriB.DrawMyGizmos(Radius_ObjectDebugSpheres);


			DBG_Operation += $"using triA: '{TriA}', and triB: '{TriB}', and obstruct edge: '{ObstructEdge}'...\n";

			DBG_Operation += $"Commencing operation...\n";

			CurrentResult = LNX_Utils.DoesEdgeObstructTriPath(
				ObstructEdge, TriA, TriB,
				ref DBG_Method
			);

			DBG_Operation += $"Operation returned: '{CurrentResult}'\n";

			if (CurrentResult)
			{
				DrawStandardEdgeFocusGizmos( ObstructEdge, edgeRaise * 1.55f, "ObstrctEdge", Color.green, true );
			}
			else
			{
				DrawStandardEdgeFocusGizmos(ObstructEdge, edgeRaise * 1.55f, "ObstrctEdge", Color.red, true);
			}
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