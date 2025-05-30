using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
	// Handles all projection type tests
	// Note: this needs to be placed on the object considered the start transform

    public class TDG_Projecting : TDG_base
    {
		[SerializeField] private Transform trans_start;

		/// <summary>
		/// 0 is for LNX_Edge.Edge.IsProjectedPointOnEdge(), 
		/// 1 is for...
		/// </summary>
		public int OperationMode_ForDebugging = 0;

		[Header("Focus On")]
		public LNX_ComponentCoordinate Coord_ProjectPointOnEdge = LNX_ComponentCoordinate.None;

		[Header("Edge Projecting")]
		public bool CurrentProjectedPtOnEdgeRslt = false;
		public List<Vector3> StartPositions_EdgeProjecting = new List<Vector3>();
		public List<Vector3> EndPositions_EdgeProjecting = new List<Vector3>();
		public List<bool> CapturedResults_EdgeProjecting = new List<bool>();

		[SerializeField] private string DBG_;

		[ContextMenu("z CaptureForEdgeProject()")]
		public void CaptureForEdgeProject()
		{
			StartPositions_EdgeProjecting.Add( trans_start.position );
			EndPositions_EdgeProjecting.Add( transform.position );
			CapturedResults_EdgeProjecting.Add( CurrentProjectedPtOnEdgeRslt );
			Debug.Log($"Logged '{CurrentProjectedPtOnEdgeRslt}'...");
		}

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();

			if ( Selection.activeGameObject != gameObject && Selection.activeGameObject != trans_start.gameObject )
			{
				return;
			}

			if ( OperationMode_ForDebugging == 0 )
			{
				LNX_Triangle focusTri = _mgr.Triangles[Coord_ProjectPointOnEdge.TrianglesIndex];
				LNX_Edge focusedEdge = _mgr.GetEdgeAtCoordinate(Coord_ProjectPointOnEdge);
				Gizmos.color = Color.magenta;
				Handles.color = Color.magenta;
				GUIStyle stl = new GUIStyle();
				stl.normal.textColor = Color.magenta;

				Vector3 gizmoLineEndPos = focusTri.V_center + (Vector3.up * 0.7f);
				Gizmos.DrawLine(focusTri.V_center, 
					gizmoLineEndPos );

				Handles.Label(gizmoLineEndPos, "FocusedTri", stl);
				Gizmos.DrawLine(focusedEdge.StartPosition, focusedEdge.StartPosition + (Vector3.up * 0.35f));
				Gizmos.DrawLine(focusedEdge.EndPosition, focusedEdge.EndPosition + (Vector3.up * 0.35f));

				//------------------------------------------------------------------------------------------
				Vector3 vTo = transform.position - trans_start.position;
				CurrentProjectedPtOnEdgeRslt = _mgr.GetEdgeAtCoordinate(Coord_ProjectPointOnEdge).IsProjectedPointOnEdge(trans_start.position, vTo);

				if ( CurrentProjectedPtOnEdgeRslt )
				{
					Gizmos.color = Color.green;
				}
				else
				{
					Gizmos.color = Color.red;
				}

				Gizmos.DrawLine(trans_start.position, transform.position);



			}
		}
	}
}