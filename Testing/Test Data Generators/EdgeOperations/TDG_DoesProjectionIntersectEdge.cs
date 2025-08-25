using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_DoesProjectionIntersectEdge : TDG_base
	{
		public Transform startTrans;
		public Transform endTrans;
		public LNX_ComponentCoordinate EdgeCoordinate;

		[Header("DATA CAPTURE")]
		public List<Vector3> CapturedStartPositions = new List<Vector3>();
		public List<Vector3> CapturedEndPositions = new List<Vector3>();
		public List<Vector3> CapturedProjectedPositions = new List<Vector3>();

		public bool CurrentProjectionResult = false;
		public Vector3 CurrentProjectedPosition = Vector3.zero;

		[Header("DEBUG")]
		[Range(0f, 0.3f)] public float Radius_Objects = 0.2f;
		[Range(0f, 0.15f)] public float Radius_ProjectPos = 0.1f;

		[TextArea(1, 20)]
		public string DBG_EdgeProjectionReport;


		[ContextMenu("z CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			CapturedStartPositions.Add(startTrans.position);
			CapturedEndPositions.Add(endTrans.position);
			CapturedProjectedPositions.Add( CurrentProjectedPosition );

			//Debug.Log($"Logged '{rslt_CurrentProjectedPtOnEdge}'...");
		}

		[ContextMenu("z CaptureProblemPosition (override)()")]
		public void CaptureProblemPosition_override()
		{
			Debug.Log("from override");

			problemPositions.Add(startTrans.position);
			problemEndPositions.Add(endTrans.position);

			Debug.Log($"{nameof(CaptureProblemPosition_override)}()...");
		}

		[ContextMenu("z GoToProblem()")]
		public void GoToProblem()
		{
			//startTrans.position = problemPositions[index_focusProblem];
			//endTrans.position = ProblemEndPositions[index_focusProblem];

			startTrans.position = CapturedStartPositions[index_focusProblem];
			endTrans.position = CapturedEndPositions[index_focusProblem];

			Debug.Log($"{nameof(GoToProblem)}()...");
		}

		[ContextMenu("z SampleFocusTri()")]
		public void SampleFocusTri()
		{
			Debug.Log($"{nameof(SampleFocusTri)}()...");

			LNX_ProjectionHit hit = LNX_ProjectionHit.None;

			if ( _navmesh.SamplePosition(startTrans.position, out hit, 2f, false) )
			{
				EdgeCoordinate = new LNX_ComponentCoordinate( hit.Index_Hit, EdgeCoordinate.ComponentIndex );
				SetDebuggerFocusToMine();
				Debug.Log($"Succesful sample! Set new edgecoordinate to: '{EdgeCoordinate.ToString()}'");
			}
			else
			{
				Debug.Log($"sample unsuccesful...");
			}
		}

		[ContextMenu("z SetDebuggerFocusToMine()")]
		public void SetDebuggerFocusToMine()
		{
			Debug.Log($"{nameof(SetDebuggerFocusToMine)}()...");

			_debugger.Index_TriFocus = EdgeCoordinate.TrianglesIndex;
		}

		protected override void OnDrawGizmos()
		{
			DBG_EdgeProjectionReport = "";

			if (Selection.activeObject != gameObject && Selection.activeObject != startTrans.gameObject)
			{
				return;
			}

			base.OnDrawGizmos();

			if ( EdgeCoordinate.TrianglesIndex < 0 || EdgeCoordinate.ComponentIndex < 0 )
			{
				DBG_EdgeProjectionReport += $"{nameof(EdgeCoordinate)} OnDrawGizmos short-circuit. {nameof(EdgeCoordinate)}: '{EdgeCoordinate}'...";
				return;
			}

			DrawStandardFocusTriGizmos( _navmesh.Triangles[EdgeCoordinate.TrianglesIndex], 1f, $"tri{EdgeCoordinate.TrianglesIndex}" );
			DrawStandardEdgeFocusGizmos( _navmesh.GetEdgeAtCoordinate(EdgeCoordinate), 0.1f, "" );

			DBG_EdgeProjectionReport += $"Commencing edge project...\n" +
				$"projection report says:\n" +
				$"---------------------------------\n";
			CurrentProjectionResult =
				_navmesh.GetEdgeAtCoordinate(EdgeCoordinate).DoesProjectionIntersectEdge
				(
					startTrans.position,
					endTrans.position,
					_navmesh.GetSurfaceNormal(),
					ref DBG_EdgeProjectionReport,
					out CurrentProjectedPosition
				);
			DBG_EdgeProjectionReport += $"=============================\n";

			Gizmos.color = CurrentProjectionResult ? Color.green : Color.red;

			Gizmos.DrawLine(startTrans.position, endTrans.position);

			Gizmos.DrawSphere(startTrans.position, Radius_Objects);
			//Handles.Label(startTrans.position, "strtTrans");
			Gizmos.DrawSphere(endTrans.position, Radius_Objects);
			//Handles.Label(startTrans.position, "endTrans");

			Gizmos.DrawCube( CurrentProjectedPosition, Vector3.one * Radius_ProjectPos );
		}
	}
}
