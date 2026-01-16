using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
	public class TDG_AmBetweenConcurrentLines : TDG_base

	{
		//TODO: NEED TO CREATE DATA AND FULLY INTEGRATE THIS INTO THE TDG MANAGER CLASS
		public Transform trans_posParameter;
		public Transform trans_LIneAStart, trans_LineAEnd;
		public Transform trans_LIneBStart, trans_LineBEnd;


		[Header("CURRENT RESULTS")]
		public bool CurrentResult;

		[Header("DATA")]
		public List<Vector3> CapturedPerspectivePositions = new List<Vector3>();
		public List<Vector3> CapturedTriCenterPositions = new List<Vector3>();

		[Header("GOTO")]
		public LNX_ComponentCoordinate GotoCoord_EdgeA;
		public LNX_ComponentCoordinate GotoCoord_EdgeB;
		public LNX_ComponentCoordinate GotoCoord_ObstructEdge;

		public bool SwitchOrientation = false;
		[Range(0, 2)] public int StartMidOrEnd = 0;

		//[Header("DEBUG")]

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			//CapturedPerspectivePositions.Add(transform.position);
			//CapturedTriCenterPositions.Add(CurrentTriangle.V_Center);

			DrawDataPointCapture(CapturedPerspectivePositions[CapturedPerspectivePositions.Count - 1],
				Color.magenta
			);


			Debug.Log($"'{CapturedPerspectivePositions[CapturedPerspectivePositions.Count - 1]}'");
		}

		#region HELPERS ---------------------------------------------------
		[ContextMenu("z call DoEet()")]
		public void DoEet()
		{

		}

		[ContextMenu("z call GotoIt()")]
		public void GotoIt()
		{
			trans_LIneAStart.position = _navmesh.GetEdge(GotoCoord_EdgeA).StartPosition;
			trans_LineAEnd.position = _navmesh.GetEdge(GotoCoord_EdgeA).EndPosition;




			if( StartMidOrEnd == 0 )
			{
				trans_posParameter.position = _navmesh.GetEdge(GotoCoord_ObstructEdge).StartPosition;
			}
			else if (StartMidOrEnd == 1)
			{
				trans_posParameter.position = _navmesh.GetEdge(GotoCoord_ObstructEdge).MidPosition;
			}
			else if (StartMidOrEnd == 2)
			{
				trans_posParameter.position = _navmesh.GetEdge(GotoCoord_ObstructEdge).EndPosition;
			}

			if (SwitchOrientation)
			{
				trans_LIneBStart.position = _navmesh.GetEdge(GotoCoord_EdgeB).EndPosition;
				trans_LineBEnd.position = _navmesh.GetEdge(GotoCoord_EdgeB).StartPosition;
			}
			else
			{
				trans_LIneBStart.position = _navmesh.GetEdge(GotoCoord_EdgeB).StartPosition;
				trans_LineBEnd.position = _navmesh.GetEdge(GotoCoord_EdgeB).EndPosition;
			}
		}

		[ContextMenu("z call SendToDataPoint")]
		public void SendToDataPoint()
		{
			transform.position = CapturedPerspectivePositions[Index_GoToProblem];
		}
		#endregion

		protected override void OnDrawGizmos()
		{
			DBG_Operation = "";

			if 
			( 
				Selection.activeObject != gameObject && 
				Selection.activeGameObject != trans_posParameter.gameObject &&
				Selection.activeObject != trans_LIneAStart.gameObject && 
				Selection.activeGameObject != trans_LineAEnd.gameObject && 
				Selection.activeGameObject != trans_LIneBStart.gameObject && 
				Selection.activeGameObject != trans_LineBEnd.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			base.OnDrawGizmos();

			DBG_Operation += $"Commencing operation...\n";


			DBG_Method = "";
			CurrentResult = LNX_Utils.AmBetweenConcurrentLines(
				trans_posParameter.position, 
				trans_LIneAStart.position, trans_LineAEnd.position,
				trans_LIneBStart.position, trans_LineBEnd.position, _navmesh.GetSurfaceNormalVector(), ref DBG_Method
			);

			if( CurrentResult )
			{
				Gizmos.color = Color.green;
			}
			else
			{
				Gizmos.color = Color.red;
			}

			Gizmos.DrawSphere(trans_posParameter.position, Radius_ObjectDebugSpheres);
			Handles.Label(trans_posParameter.position, "posParam");

			Gizmos.DrawLine(trans_LIneAStart.position, trans_LineAEnd.position);
			Handles.Label(trans_LIneAStart.position, "startA");
			Handles.Label(trans_LineAEnd.position, "endA");

			Gizmos.DrawLine(trans_LIneBStart.position, trans_LineBEnd.position);
			Handles.Label(trans_LIneBStart.position, "startB");
			Handles.Label(trans_LineBEnd.position, "endB");
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