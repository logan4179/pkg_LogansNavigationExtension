using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEngine;
using static UnityEngine.UI.Image;

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
		public int OperationMode = 0;

		[Header("Focus On")]
		public LNX_ComponentCoordinate Coord_ProjectPointOnEdge = LNX_ComponentCoordinate.None;

		[Header("Edge Projecting")]
		// DATA ------------------------------------------------------------------------
		public List<Vector3> StartPositions_EdgeProjecting = new List<Vector3>();
		public List<Vector3> EndPositions_EdgeProjecting = new List<Vector3>();
		public List<Vector3> CapturedTriCenters_EdgeProjecting = new List<Vector3>();
		public List<Vector3> CapturedEdgeMidPoints_EdgePRojecting = new List<Vector3>();
		public List<int> CapturedEdgeIndices = new List<int>();
		public List<bool> CapturedResults_EdgeProjecting = new List<bool>();
		public List<Vector3> CapturedProjectionPoints_EdgeProjecting = new List<Vector3>();

		[Header("Perimeter Projecting")]
		public List<Vector3> StartPositions_PerimeterProjecting = new List<Vector3>();
		public List<Vector3> EndPositions_PerimeterProjecting = new List<Vector3>();
		public List<Vector3> CapturedTriCenters_PerimeterProjecting = new List<Vector3>();
		public List<Vector3> CapturedEdgeMidPoints_PerimeterProjecting = new List<Vector3>();
		public List<Vector3> CapturedProjectionPoints_PerimeterProjecting = new List<Vector3>();

		[Header("Shape Projecting")]
		public List<Vector3> StartPositions_ShapeProjecting = new List<Vector3>();
		public List<Vector3> EndPositions_ShapeProjecting = new List<Vector3>();
		public List<Vector3> CapturedTriCenters_ShapeProjecting = new List<Vector3>();
		public List<Vector3> CapturedProjectionPositions_ShapeProjecting = new List<Vector3>();
		public List<bool> CapturedResults_ShapeProjectiong = new List<bool>();

		//UTIL----------------------------------------------------------------------------------------------
		LNX_ProjectionHit lnxStartHit = LNX_ProjectionHit.None;

		//[Header("TRUTH")]
		public bool OperationUsesFocusCoordinate
		{
			get
			{
				return OperationMode == 0;
			}
		}

		[Header("DEBUG")]
		[SerializeField] private string DBG_class;

		[SerializeField] private bool amDebuggingDataPoints = true;
		[SerializeField] float radius_dataPoints = 0.05f;
		[SerializeField] Color color_dataPoints = Color.white;
		public int Index_GoToDataPoint = 0;

		[ContextMenu("z call GoToDataPoint()")]
		public void GoToDataPoint()
		{
			if (OperationMode == 0)
			{
				trans_start.position = StartPositions_EdgeProjecting[Index_GoToDataPoint];
				transform.position = EndPositions_EdgeProjecting[Index_GoToDataPoint];
			}
			else if (OperationMode == 1 )
			{
				trans_start.position = StartPositions_PerimeterProjecting[Index_GoToDataPoint];
				transform.position = EndPositions_PerimeterProjecting[Index_GoToDataPoint];
			}
		}

		[ContextMenu("z call GenerateDataCollection()")]
		public void GenerateDataCollection() //Modify this method as needed to create data collections when you add them and need them generated for the data output file
		{
			//CapturedProjectionPoints_EdgeProjecting = new List<Vector3>();
			CapturedEdgeMidPoints_EdgePRojecting = new List<Vector3>();

			focusTri = _mgr.Triangles[Coord_ProjectPointOnEdge.TrianglesIndex];
			focusedEdge = _mgr.GetEdgeAtCoordinate(Coord_ProjectPointOnEdge);

			for ( int i = 0; i < StartPositions_EdgeProjecting.Count; i++ )
			{
				ProjectionReturnResult =
					focusTri.DoesProjectionIntersectEdge(
						StartPositions_EdgeProjecting[i], EndPositions_EdgeProjecting[i], Coord_ProjectPointOnEdge.ComponentIndex, out projectionPositionResult
					);

				//CapturedProjectionPoints_EdgeProjecting.Add( projectionResult );
				CapturedEdgeMidPoints_EdgePRojecting.Add( focusedEdge.MidPosition );
			}
		}

		[ContextMenu("z call CaptureForEdgeProject()")]
		public void CaptureForEdgeProject()
		{
			StartPositions_EdgeProjecting.Add( trans_start.position );
			EndPositions_EdgeProjecting.Add( transform.position );
			CapturedTriCenters_EdgeProjecting.Add( _mgr.Triangles[Coord_ProjectPointOnEdge.TrianglesIndex].V_Center );
			CapturedEdgeMidPoints_EdgePRojecting.Add( focusedEdge.MidPosition );
			CapturedResults_EdgeProjecting.Add( ProjectionReturnResult );
			CapturedEdgeIndices.Add( Coord_ProjectPointOnEdge.ComponentIndex );
			CapturedProjectionPoints_EdgeProjecting.Add( projectionPositionResult );

			Debug.Log($"Logged '{ProjectionReturnResult}'...");
		}

		[ContextMenu("z call CaptureForPerimeterProject()")]
		public void CaptureForPerimeterProject()
		{
			StartPositions_PerimeterProjecting.Add( trans_start.position );
			EndPositions_PerimeterProjecting.Add( transform.position );
			CapturedTriCenters_PerimeterProjecting.Add( focusTri.V_Center );
			CapturedEdgeMidPoints_PerimeterProjecting.Add( focusedEdge.MidPosition );
			CapturedProjectionPoints_PerimeterProjecting.Add( projectionPositionResult );

			Debug.Log($"Logged '{projectionPositionResult}'...");
		}

		[ContextMenu("z call CaptureForShapeProject()")]
		public void CaptureForShapeProject()
		{
			StartPositions_ShapeProjecting.Add( trans_start.position );
			EndPositions_ShapeProjecting.Add( transform.position );
			CapturedTriCenters_ShapeProjecting.Add( focusTri.V_Center );
			CapturedProjectionPositions_ShapeProjecting.Add( projectionPositionResult );
			CapturedResults_ShapeProjectiong.Add( ProjectionReturnResult );

			Debug.Log($"Logged '{projectionPositionResult}'...");
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


		public LNX_Edge focusedEdge = null;
		public LNX_Triangle focusTri = null;
		public Vector3 projectionPositionResult = Vector3.zero;
		public bool ProjectionReturnResult = false;

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();

			if ( Selection.activeGameObject != gameObject && trans_start != null && Selection.activeGameObject != trans_start.gameObject )
			{
				return;
			}

			DBG_class = "";

			Vector3 v_LblRaise = Vector3.up * 0.05f;
			focusedEdge = null;
			focusTri = null;
			projectionPositionResult = Vector3.zero;
			GUIStyle styl_FocusLettering = new GUIStyle();
			styl_FocusLettering.normal.textColor = Color.magenta;

			#region HIGHLIGHT FOCUSED TRI AND EDGE---------------------
			if ( OperationUsesFocusCoordinate ) 
			{
				focusTri = _mgr.Triangles[Coord_ProjectPointOnEdge.TrianglesIndex];
				focusedEdge = _mgr.GetEdgeAtCoordinate(Coord_ProjectPointOnEdge);
				DBG_class += $"Determined focused tri to be: '{focusTri.Index_inCollection}'\n" +
					$"and focusedEdge to be: '{focusedEdge.MyCoordinate}'\n" +
					$"focusedEdge start vert coord: '{focusedEdge.StartVertCoordinate.ComponentIndex}'\n" +
					$"focusedEdge endVertCoord: '{focusedEdge.EndVertCoordinate.ComponentIndex}\n" +
					$"";

				Handles.Label(focusedEdge.MidPosition + v_LblRaise, $"FocusEdge({focusedEdge.MyCoordinate.ComponentIndex})", styl_FocusLettering);
				Gizmos.DrawLine(focusedEdge.StartPosition, focusedEdge.EndPosition);
				Handles.Label(focusedEdge.StartPosition + v_LblRaise, "start");
				Handles.Label(focusedEdge.EndPosition + v_LblRaise, "end");

				Gizmos.color = Color.magenta;
				Handles.color = Color.magenta;


				Handles.Label( focusTri.V_Center + v_LblRaise, "FocusTri", styl_FocusLettering);
			}
			#endregion

			if (OperationMode == 0)
			{
				DBG_class += $"Operation: 'IsProjectedPointOnEdge()'...\n";
				//rslt_CurrentProjectedPtOnEdge = 
				//_mgr.GetEdgeAtCoordinate(Coord_ProjectPointOnEdge).IsProjectedPointOnEdge(trans_start.position, transform.position );

				ProjectionReturnResult =
					focusTri.DoesProjectionIntersectEdge(trans_start.position, transform.position, Coord_ProjectPointOnEdge.ComponentIndex, out projectionPositionResult);

				if (ProjectionReturnResult)
				{
					Gizmos.color = Color.green;

					Gizmos.DrawCube(projectionPositionResult, Vector3.one * 0.05f);
				}
				else
				{
					Gizmos.color = Color.red;
				}

				Gizmos.DrawLine(trans_start.position, transform.position);

				#region Draw the data points....
				if (amDebuggingDataPoints)
				{
					Gizmos.color = color_dataPoints;

					if (StartPositions_EdgeProjecting != null && StartPositions_EdgeProjecting.Count > 0)
					{
						for (int i = 0; i < StartPositions_EdgeProjecting.Count; i++)
						{
							Gizmos.DrawSphere(StartPositions_EdgeProjecting[i], radius_dataPoints);
							Gizmos.DrawSphere(EndPositions_EdgeProjecting[i], radius_dataPoints);
							Gizmos.DrawLine(StartPositions_EdgeProjecting[i], EndPositions_EdgeProjecting[i]);
						}
					}
				}
				#endregion

				Handles.Label(focusTri.Verts[focusedEdge.StartVertCoordinate.ComponentIndex].V_Position + (v_LblRaise * 1.5f), "startVert");
			}
			else if (OperationMode == 1) // PERIMETER PROJECTING----------------
			{
				DBG_class += $"Operation: 'PERIMETER PROJECTING'...\n";

				lnxStartHit = LNX_ProjectionHit.None;

				if (_mgr.SamplePosition(trans_start.position, out lnxStartHit, 3f))
				{
					DBG_class += $"sampled start tri: '{lnxStartHit.Index_hitTriangle}' at: '{lnxStartHit.HitPosition}', \n";

					focusTri = _mgr.Triangles[lnxStartHit.Index_hitTriangle];

					projectionPositionResult = focusTri.ProjectThroughToPerimeter(
						trans_start.position, transform.position, out focusedEdge
					);

					if (focusedEdge == null)
					{
						Debug.Log("projected edge was null...");

						Gizmos.color = Color.red;
					}
					else
					{
						Gizmos.color = Color.green;

						DBG_class += $"Determined focusedEdge to be: '{focusedEdge.MyCoordinate}'\n" +
							$"focusedEdge start vert coord: '{focusedEdge.StartVertCoordinate.ComponentIndex}'\n" +
							$"focusedEdge endVertCoord: '{focusedEdge.EndVertCoordinate.ComponentIndex}\n" +
							$"";

						Handles.Label(focusedEdge.MidPosition + v_LblRaise, $"FocusEdge({focusedEdge.MyCoordinate.ComponentIndex})", styl_FocusLettering);
						Gizmos.DrawLine(
							focusedEdge.StartPosition + (v_LblRaise * 0.7f),
							focusedEdge.EndPosition + (v_LblRaise * 0.7f)
						);

						Gizmos.DrawLine(focusedEdge.StartPosition, focusedEdge.StartPosition + (v_LblRaise * 0.7f));
						Gizmos.DrawLine(focusedEdge.EndPosition, focusedEdge.EndPosition + (v_LblRaise * 0.7f));

						Handles.Label(focusedEdge.StartPosition + v_LblRaise, "start");
						Handles.Label(focusedEdge.EndPosition + v_LblRaise, "end");

						Gizmos.DrawCube(projectionPositionResult, Vector3.one * 0.05f);

					}

					#region Draw the data points....
					if (amDebuggingDataPoints)
					{
						Gizmos.color = color_dataPoints;

						if (StartPositions_PerimeterProjecting != null && StartPositions_PerimeterProjecting.Count > 0)
						{
							for (int i = 0; i < StartPositions_PerimeterProjecting.Count; i++)
							{
								Gizmos.DrawSphere(StartPositions_PerimeterProjecting[i], radius_dataPoints);
								Gizmos.DrawSphere(EndPositions_PerimeterProjecting[i], radius_dataPoints);
								Gizmos.DrawLine(StartPositions_PerimeterProjecting[i], EndPositions_PerimeterProjecting[i]);
							}
						}
					}
					#endregion
				}
				else
				{
					Debug.Log($"can't sample at current position");
				}

				if ( focusedEdge != null )
				{
					Gizmos.color = Color.green;
				}
				else
				{
					Gizmos.color = Color.red;
				}

				Gizmos.DrawLine(trans_start.position, transform.position);

			}
			else if ( OperationMode == 2 )
			{
				DBG_class += $"Operation: 'SHAPE PROJECTING'...\n";
				lnxStartHit = LNX_ProjectionHit.None;
				projectionPositionResult = Vector3.zero;

				if ( _mgr.SamplePosition(transform.position, out lnxStartHit, 1f, false) )
				{
					DBG_class += $"sampled start tri: '{lnxStartHit.Index_hitTriangle}' at: '{lnxStartHit.HitPosition}', \n";

					focusTri = _mgr.Triangles[lnxStartHit.Index_hitTriangle];

					DrawStandardFocusTriGizmos(focusTri, 0.5f, $"tri({focusTri.Index_inCollection})");
					/* dws
					Gizmos.color = Color.magenta;
					Handles.color = Color.magenta;
					Vector3 vRaise = Vector3.up * 0.5f;
					Gizmos.DrawLine( focusTri.Verts[0].Position, focusTri.V_center + vRaise );
					Gizmos.DrawLine(focusTri.Verts[1].Position, focusTri.V_center + vRaise);
					Gizmos.DrawLine(focusTri.Verts[2].Position, focusTri.V_center + vRaise);
					Handles.Label( focusTri.V_center + vRaise, $"tri({focusTri.Index_inCollection})" );
					*/

					ProjectionReturnResult = focusTri.IsInShapeProject( 
						transform.position, out projectionPositionResult
					);

					DBG_class += $"projection report: \n" +
						$"{focusTri.DBG_IsInShapeProjectAlongNormal}\n";

					if ( ProjectionReturnResult )
					{
						DBG_class += ( $"IsInShapeProject operation returned true!\n" +
							$"projected position: '{projectionPositionResult}'\n" +
							$"");
						Gizmos.color = Color.green;

						Gizmos.DrawCube(projectionPositionResult, Vector3.one * 0.05f);

					}
					else
					{
						Debug.Log("IsInShapeProject operation returned false...");

						Gizmos.color = Color.red;
					}
				}
				else
				{
					Gizmos.color = Color.red;

					Debug.Log($"can't sample at current position");
				}

				Gizmos.DrawLine(trans_start.position, transform.position);

			}

		}
	}
}