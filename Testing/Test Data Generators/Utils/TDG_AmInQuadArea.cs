using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_AmInQuadArea : TDG_base
    {
		//TODO: Implement in manager and create tests!

        [Header("REFERENCE")]
        public Transform Trans_PosParam;
        public Transform Trans_CrnrA;
        public Transform Trans_CrnrB;
        public Transform Trans_CrnrC;
        public Transform Trans_CrnrD;

		[Header("MatchTDGs")]
		public TDG_DoesEdgeObstructEdgePath _TDG_DoesEdgeObstructEdgePath;


		[Header("RESULTS")]
		public bool CurrentOperationResult = false;

		[Header("DEBUG")]
		[TextArea(1,10)] public string DBG_Method;
		public Color Color_Corners;
		public Vector3 v_lblOffset;

		[ContextMenu("z call CaptureProblemPoint()")]
		public void CaptureProblemPoint()
		{
			_dataCapture_problems.CaptureDataPoint(
				CurrentOperationResult,
				Trans_PosParam.transform.position, Trans_CrnrA.transform.position, Trans_CrnrB.transform.position, Trans_CrnrC.transform.position, Trans_CrnrD.transform.position
			);
		}

		#region HELPERS -----------------------
		[ContextMenu("z call SendToTDG()")]
		public void SendToTDG()
		{
			//Trans_PosParam.position = _TDG_DoesEdgeObstructEdgePath.Grabber_ObstructEdge.transform.position;
			Trans_PosParam.position = _TDG_DoesEdgeObstructEdgePath.CurrentObstructEdge.MidPosition;

			Trans_CrnrA.position = _TDG_DoesEdgeObstructEdgePath.CurrentStartEdge.StartPosition_flat;
			Trans_CrnrB.position = _TDG_DoesEdgeObstructEdgePath.CurrentStartEdge.EndPosition_flat;

			if 
			(
				LNX_Utils.AreEdgesAlignedFromTheirPerspectives
				(
					_TDG_DoesEdgeObstructEdgePath.CurrentStartEdge, _TDG_DoesEdgeObstructEdgePath.CurrentEndEdge
				)
			)
			{
				Trans_CrnrC.position = _TDG_DoesEdgeObstructEdgePath.CurrentEndEdge.EndPosition_flat;
				Trans_CrnrD.position = _TDG_DoesEdgeObstructEdgePath.CurrentEndEdge.StartPosition_flat;
			}
			else
			{
				Trans_CrnrC.position = _TDG_DoesEdgeObstructEdgePath.CurrentEndEdge.StartPosition_flat;
				Trans_CrnrD.position = _TDG_DoesEdgeObstructEdgePath.CurrentEndEdge.EndPosition_flat;
			}
		}
		#endregion

		protected override void OnDrawGizmos()
		{
			if
			(
				Selection.activeGameObject != gameObject &&
				Selection.activeGameObject != transform.parent.gameObject &&
				Selection.activeGameObject != Trans_PosParam.gameObject &&
				Selection.activeGameObject != Trans_CrnrA.gameObject &&
				Selection.activeGameObject != Trans_CrnrB.gameObject &&
				Selection.activeGameObject != Trans_CrnrC.gameObject &&
				Selection.activeGameObject != Trans_CrnrD.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			DBG_Operation = "";
			DBG_Method = "";
			CurrentOperationResult = false;

			Gizmos.color = Color_Corners;
			Gizmos.DrawSphere( Trans_CrnrA.position, Radius_ObjectDebugSpheres );
			Handles.Label(Trans_CrnrA.position + v_lblOffset, "CrnrA");
			Gizmos.DrawLine( Trans_CrnrA.position, Trans_CrnrB.position );
			Gizmos.DrawLine(Trans_CrnrA.position, Trans_CrnrD.position);


			Gizmos.DrawSphere(Trans_CrnrB.position, Radius_ObjectDebugSpheres);
			Handles.Label(Trans_CrnrB.position + v_lblOffset, "CrnrB");

			Gizmos.DrawSphere(Trans_CrnrC.position, Radius_ObjectDebugSpheres);
			Handles.Label(Trans_CrnrC.position + v_lblOffset, "CrnrC");
			Gizmos.DrawLine(Trans_CrnrC.position, Trans_CrnrB.position);
			Gizmos.DrawLine(Trans_CrnrC.position, Trans_CrnrD.position);


			Gizmos.DrawSphere(Trans_CrnrD.position, Radius_ObjectDebugSpheres);
			Handles.Label(Trans_CrnrD.position + v_lblOffset, "CrnrD");

			DBG_Operation += $"Commencing operation...\n";

			CurrentOperationResult = LNX_Utils.AmInQuadArea(
				Trans_PosParam.position, 
				Trans_CrnrA.position, Trans_CrnrB.position, 
				Trans_CrnrC.position, Trans_CrnrD.position, 
				_navmesh.V_SurfaceOrientation, true, ref DBG_Method );

			DBG_Operation += $"Result of AmInQuadArea(): '{CurrentOperationResult}'...\n";

			if( CurrentOperationResult )
			{
				Gizmos.color = Color.green;
				DBG_Operation += $"pos IS in quad area...\n";
			}
			else
			{
				Gizmos.color = Color.red;
				DBG_Operation += $"pos is NOT in quad area...\n";
			}

			Gizmos.DrawSphere(Trans_PosParam.position, Radius_ObjectDebugSpheres);
			Handles.Label(Trans_PosParam.position + v_lblOffset, "POS");

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
