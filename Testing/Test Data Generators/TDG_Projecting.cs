using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
	// Handles all projection type tests
	// Note: this needs to be placed on the object considered the "destination" transform

    public class TDG_Projecting : TDG_base
    {
		[SerializeField] private Transform trans_start;

		/// <summary>
		/// This allows me to use this single TDG for multiple different projection tests.
		/// 0 is for LNX_Edge.Edge.IsProjectedPointOnEdge(), 
		/// 1 is for...
		/// </summary>
		public int OperationMode_ForDebugging = 0;

		[Header("Focus On")]
		public LNX_ComponentCoordinate Coord_ProjectPointOnEdge = LNX_ComponentCoordinate.None;

		[Header("Edge Projecting")]
		public bool rslt_CurrentProjectedPtOnEdge = false;
		public List<Vector3> StartPositions_EdgeProjecting = new List<Vector3>();
		public List<Vector3> EndPositions_EdgeProjecting = new List<Vector3>();
		public List<Vector3> CapturedTriCenters_EdgeProjecting = new List<Vector3>();
		public List<int> CapturedEdgeIndices = new List<int>();

		public List<bool> CapturedResults_EdgeProjecting = new List<bool>();

		[Header("DEBUG")]
		[SerializeField] private string DBG_class;

		[SerializeField] private bool amDebuggingDataPoints = true;
		[SerializeField] float radius_dataPoints = 0.05f;
		[SerializeField] Color color_dataPoints = Color.white;

		[ContextMenu("z CaptureForEdgeProject()")]
		public void CaptureForEdgeProject()
		{
			StartPositions_EdgeProjecting.Add( trans_start.position );
			EndPositions_EdgeProjecting.Add( transform.position );
			CapturedTriCenters_EdgeProjecting.Add( _mgr.Triangles[Coord_ProjectPointOnEdge.TrianglesIndex].V_center );
			CapturedResults_EdgeProjecting.Add( rslt_CurrentProjectedPtOnEdge );
			CapturedEdgeIndices.Add( Coord_ProjectPointOnEdge.ComponentIndex );

			Debug.Log($"Logged '{rslt_CurrentProjectedPtOnEdge}'...");
		}

		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			bool rslt = TDG_Manager.WriteTestObjectToJson(TDG_Manager.filePath_testData_projectingTests, this);

			if (rslt)
			{
				LastWriteTime = System.DateTime.Now.ToString();
				return true;

			}

			return false;
		}

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();

			if ( Selection.activeGameObject != gameObject && Selection.activeGameObject != trans_start.gameObject )
			{
				return;
			}

			#region HIGHLIGHT FOCUSED TRI AND EDGE---------------------
			LNX_Triangle focusTri = _mgr.Triangles[Coord_ProjectPointOnEdge.TrianglesIndex];
			LNX_Edge focusedEdge = _mgr.GetEdgeAtCoordinate(Coord_ProjectPointOnEdge);
			DBG_class = $"Determined focused tri to be: '{focusTri.Index_inCollection}'\n" +
				$"and focusedEdge to be: '{focusedEdge.MyCoordinate}'\n";

			Gizmos.color = Color.magenta;
			Handles.color = Color.magenta;
			GUIStyle stl = new GUIStyle();
			stl.normal.textColor = Color.magenta;

			Vector3 v_raise = Vector3.up * 0.05f;
			Handles.Label( focusTri.V_center + v_raise, "FocusTri", stl);

			Handles.Label(focusedEdge.MidPosition + v_raise, "FocusEdge", stl);
			Gizmos.DrawLine( focusedEdge.StartPosition, focusedEdge.EndPosition );

			Handles.Label(focusedEdge.StartPosition + v_raise, "start");
			Handles.Label(focusedEdge.EndPosition + v_raise, "end");

			#endregion

			if ( OperationMode_ForDebugging == 0 )
			{
				DBG_class += $"using 'IsProjectedPointOnEdge()'...\n";
				//rslt_CurrentProjectedPtOnEdge = 
					//_mgr.GetEdgeAtCoordinate(Coord_ProjectPointOnEdge).IsProjectedPointOnEdge(trans_start.position, transform.position );

				rslt_CurrentProjectedPtOnEdge =
					_mgr.GetTriangle(Coord_ProjectPointOnEdge).IsProjectedPointOnEdge(trans_start.position, transform.position, Coord_ProjectPointOnEdge.ComponentIndex);



				if ( rslt_CurrentProjectedPtOnEdge )
				{
					Gizmos.color = Color.green;
				}
				else
				{
					Gizmos.color = Color.red;
				}

				Gizmos.DrawLine(trans_start.position, transform.position);

				#region Draw the data points....
				if( amDebuggingDataPoints )
				{
					Gizmos.color = color_dataPoints;

					if ( StartPositions_EdgeProjecting != null && StartPositions_EdgeProjecting.Count > 0 )
					{
						for ( int i = 0; i < StartPositions_EdgeProjecting.Count; i++ )
						{
							Gizmos.DrawSphere( StartPositions_EdgeProjecting[i], radius_dataPoints );
							Gizmos.DrawSphere( EndPositions_EdgeProjecting[i], radius_dataPoints );
							Gizmos.DrawLine(StartPositions_EdgeProjecting[i], EndPositions_EdgeProjecting [i] );
						}
					}
				}

				#endregion

			}
		}
	}
}