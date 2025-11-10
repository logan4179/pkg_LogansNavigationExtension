using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_AmInArea_threeCornerOverload : TDG_base
    {
		//TODO: HAVEN'T YET IMPLEMENTED THIS TDG

		public LNX_ComponentGrabber Grabber_Pos;
		public LNX_ComponentGrabber Grabber_CrnrA;
		public LNX_ComponentGrabber Grabber_CrnrB;
		public LNX_ComponentGrabber Grabber_CrnrC;

		[Header("CURRENT CAPTURE")]
		public bool CurrentResult = false;

		[Header("GOTO")]
		public TDG_DoesEdgeObstructArea _tdg_doesEdgeObstructArea;

		[Header("DEBUG")]
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

		}

		[ContextMenu("z call SendToTDG")]
		public void SendToTDG()
		{
			Grabber_Pos.transform.position = _tdg_doesEdgeObstructArea.ObstructEdge.MidPosition;
			Grabber_CrnrA.transform.position = _tdg_doesEdgeObstructArea.PerspectiveVert.V_Position;
			Grabber_CrnrB.transform.position = _tdg_doesEdgeObstructArea.DestinationTriangle.Verts[0].V_Position;
			Grabber_CrnrC.transform.position = _tdg_doesEdgeObstructArea.DestinationTriangle.Verts[1].V_Position;

		}
		#endregion


		protected override void OnDrawGizmos()
		{
			DBG_Operation = "";

			if
			(
				Selection.activeObject != gameObject &&
				Selection.activeGameObject != Grabber_Pos.gameObject &&
				Selection.activeGameObject != Grabber_CrnrA.gameObject &&
				Selection.activeGameObject != Grabber_CrnrB.gameObject &&
				Selection.activeGameObject != Grabber_CrnrC.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			base.OnDrawGizmos();

			DBG_Operation += $"Commencing operation...\n";

			CurrentResult = LNX_Utils.AmInArea(
				Grabber_Pos.transform.position,
				Grabber_CrnrA.transform.position, Grabber_CrnrB.transform.position, Grabber_CrnrC.transform.position,
				_navmesh.V_SurfaceOrientation, false/*, ref DBG_Method*/
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

			Grabber_Pos.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_CrnrA.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_CrnrB.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_CrnrC.DrawMyGizmos(Radius_ObjectDebugSpheres);
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
