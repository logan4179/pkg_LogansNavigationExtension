using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_Raycasting : TDG_base
    {
		public Transform startTrans;
		public Transform endTrans;

		public List<Vector3> CapturedStartPositions = new List<Vector3>();
		public List<Vector3> CapturedEndPositions = new List<Vector3>();

		public bool RaycastResult = false;

		[Header("DEBUG")]
		[Range(0f,0.1f)] public float Radius_Objects = 0.2f;
		[TextArea(1,20)]
		public string DBG_NavmeshRaycastRprt;
		[TextArea(1, 20)]
		public string DBG_NavmeshProjectionRprt;

		[ContextMenu("z CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			CapturedStartPositions.Add( startTrans.position );
			CapturedEndPositions.Add( endTrans.position );

			//Debug.Log($"Logged '{rslt_CurrentProjectedPtOnEdge}'...");
		}

		[ContextMenu("z CaptureProblemPosition (override)()")]
		public void CaptureProblemPosition_override()
		{
			Debug.Log("from override");

			problemPositions.Add( startTrans.position );
			problemEndPositions.Add( endTrans.position );

			Debug.Log($"{nameof(CaptureProblemPosition_override)}()...");
		}

		[ContextMenu("z GoToProblem()")]
		public void GoToProblem()
		{
			//startTrans.position = problemPositions[Index_FocusProblem];
			//endTrans.position = ProblemEndPositions[Index_FocusProblem];

			startTrans.position = CapturedStartPositions[index_focusProblem];
			endTrans.position = CapturedEndPositions[index_focusProblem];

			Debug.Log($"{nameof(GoToProblem)}()...");
		}

		[ContextMenu("z SayPositions()")]
		public void SayPositions()
		{

			Debug.Log($"startpos: '{LNX_UnitTestUtilities.LongVectorString(startTrans.position)}' endpos: '{endTrans.position}'");
		}

		protected override void OnDrawGizmos()
		{
			DBG_NavmeshRaycastRprt = "";

			if( Selection.activeObject != gameObject && Selection.activeObject != startTrans.gameObject )
			{
				return;
			}

			base.OnDrawGizmos();

			RaycastResult = _navmesh.Raycast(startTrans.position, endTrans.position, 3f);

			Gizmos.color = RaycastResult ? Color.red : Color.green;

			Gizmos.DrawLine(startTrans.position, endTrans.position);

			Gizmos.DrawSphere(startTrans.position, Radius_Objects);
			//Handles.Label(startTrans.position, "strtTrans");
			Gizmos.DrawSphere(endTrans.position, Radius_Objects);
			//Handles.Label(startTrans.position, "endTrans");

			DBG_NavmeshRaycastRprt = _navmesh.DBGRaycast;
			DBG_NavmeshProjectionRprt = _navmesh.DBG_NavmeshProjection;
		}
	}
}
