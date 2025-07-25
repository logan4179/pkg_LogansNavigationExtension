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
		public LNX_ComponentCoordinate FocusCoordinate = LNX_ComponentCoordinate.None;

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

		[Header("DEBUG")]
		[Range(0f, 0.5f)] public float Radius_TestObject = 0.2f;
		[Range(0f, 0.25f)] public float size_projectionObject = 0.2f;
		public Color color_projectionObject;

		public Color Color_IfTrue = Color.white;
		public Color Color_IfFalse = Color.white;

		[SerializeField] private bool amDebuggingDataPoints = true;
		[SerializeField] float radius_dataPoints = 0.05f;
		[SerializeField] Color color_dataPoints = Color.white;
		public int Index_GoToDataPoint = 0;

		[SerializeField] private string DBG_class;
		[Space(10)]
		[SerializeField, TextArea(0,20)] private string DBG_FocusedTri;

		[Header("OTHER")]
		public LNX_ProjectionHit projectionHit = LNX_ProjectionHit.None;



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

			focusTri = _navmesh.Triangles[FocusCoordinate.TrianglesIndex];
			focusedEdge = _navmesh.GetEdgeAtCoordinate(FocusCoordinate);

			for ( int i = 0; i < StartPositions_EdgeProjecting.Count; i++ )
			{
				ProjectionReturnResult =
					focusTri.DoesProjectionIntersectGivenEdge(
						StartPositions_EdgeProjecting[i], EndPositions_EdgeProjecting[i], FocusCoordinate.ComponentIndex, out projectionPositionResult
					);

				//CapturedProjectionPoints_EdgeProjecting.Add( projectionResult );
				CapturedEdgeMidPoints_EdgePRojecting.Add( focusedEdge.MidPosition );
			}
		}

		#region DATA CAPTURE -------------------------------------------
		[ContextMenu("z call CaptureForEdgeProject()")]
		public void CaptureForEdgeProject()
		{
			StartPositions_EdgeProjecting.Add( trans_start.position );
			EndPositions_EdgeProjecting.Add( transform.position );
			CapturedTriCenters_EdgeProjecting.Add( _navmesh.Triangles[FocusCoordinate.TrianglesIndex].V_Center );
			CapturedEdgeMidPoints_EdgePRojecting.Add( focusedEdge.MidPosition );
			CapturedResults_EdgeProjecting.Add( ProjectionReturnResult );
			CapturedEdgeIndices.Add( FocusCoordinate.ComponentIndex );
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
			CapturedProjectionPoints_PerimeterProjecting.Add( projectionHit.HitPosition );

			Debug.Log($"Logged '{projectionHit.HitPosition}'...");
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
		#endregion

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
			if ( Selection.activeGameObject != gameObject  )
			{
				return;
			}

			base.OnDrawGizmos();


			DBG_class = "";

			Vector3 v_LblRaise = Vector3.up * 0.07f;
			focusedEdge = null;
			focusTri = null;
			projectionPositionResult = Vector3.zero;
			GUIStyle styl_FocusLettering = new GUIStyle();
			styl_FocusLettering.normal.textColor = Color.magenta;

			if (OperationMode == 0)// IsProjectedPointOnEdge----------------
			{
				DBG_class += $"Operation: '{nameof(LNX_Triangle.DoesProjectionIntersectGivenEdge)}()'...\n";

				focusTri = _navmesh.Triangles[FocusCoordinate.TrianglesIndex];
				focusedEdge = _navmesh.GetEdgeAtCoordinate(FocusCoordinate);
				Handles.Label(focusedEdge.MidPosition + v_LblRaise, $"FocusEdge({focusedEdge.MyCoordinate.ComponentIndex})", styl_FocusLettering);
				Gizmos.DrawLine(focusedEdge.StartPosition, focusedEdge.EndPosition);
				Handles.Label(focusedEdge.StartPosition + v_LblRaise, "start");
				Handles.Label(focusedEdge.EndPosition + v_LblRaise, "end");

				//rslt_CurrentProjectedPtOnEdge = 
				//_mgr.GetEdgeAtCoordinate(Coord_ProjectPointOnEdge).IsProjectedPointOnEdge(trans_start.position, transform.position );

				ProjectionReturnResult =
					focusTri.DoesProjectionIntersectGivenEdge(trans_start.position, transform.position, FocusCoordinate.ComponentIndex, out projectionPositionResult);

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

				Handles.Label(focusTri.Verts[focusedEdge.StartVertCoordinate.ComponentIndex].V_Position + (v_LblRaise * 1.5f), "startVert");
			}
			else if ( OperationMode == 1 ) // PERIMETER PROJECTING----------------
			{
				DBG_class += $"Operation: 'PERIMETER PROJECTING'...\n";

				focusTri = _navmesh.Triangles[FocusCoordinate.TrianglesIndex];

				DBG_class += $"focusTri: '{focusTri.Index_inCollection}'\n" +
					$"now attempting to sample a position...\n";

				lnxStartHit = LNX_ProjectionHit.None;

				if ( _navmesh.SamplePosition(trans_start.position, out lnxStartHit, 3f) )
				{
					DBG_class += $"Sampled position at: '{lnxStartHit.HitPosition}'\n" +
						$"trying ProjectThroughToPerimeter()......\n";

					projectionHit = LNX_ProjectionHit.None;

					projectionHit = focusTri.ProjectThroughToPerimeter(trans_start.position, transform.position);

					if( projectionHit.Index_Hit > -1 && projectionHit.Index_Hit < 3 )
					{
						focusedEdge = focusTri.Edges[projectionHit.Index_Hit];
						Gizmos.color = Color.green;

						DBG_class += $"Determined focusedEdge to be: '{focusedEdge.MyCoordinate}'\n";

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
					else
					{
						DBG_class += $"projection hit index ({projectionHit.Index_Hit}) wasn't in correct bounds. Can't resolve an edge.\n" +
							$"dumping ProjectThroughToPerimeter report...\n" +
							$"---------------------------------\n" +
							$"{focusTri.dbg_prjctThrhToPerim}\n";
						Gizmos.color = Color.red;
					}
				}
				else
				{
					DBG_class += $"Failed to sample a start position. Returning early...\n";
					Gizmos.color = Color.red;
				}

				Gizmos.DrawLine(trans_start.position, transform.position);
				Gizmos.DrawSphere(transform.position, Radius_TestObject);

			}
			else if ( OperationMode == 2) // SHAPE PROJECTING----------------
			{
				DBG_class += $"Operation: 'SHAPE PROJECTING'...\n";

				focusTri = _navmesh.Triangles[FocusCoordinate.TrianglesIndex];

				lnxStartHit = LNX_ProjectionHit.None;
				projectionPositionResult = Vector3.zero;

				ProjectionReturnResult = focusTri.IsInShapeProject(
					transform.position, out projectionPositionResult
				);

				DBG_class += $"IsInShapeProject() report: \n" +
					$"{focusTri.DBG_IsInShapeProject}\n";

				if ( ProjectionReturnResult )
				{
					DBG_class += ($"IsInShapeProject operation returned true!\n" +
						$"projected position: '{projectionPositionResult}'\n" +
						$"");

					Gizmos.color = color_projectionObject;
					Gizmos.DrawLine( transform.position, projectionPositionResult );
					Gizmos.DrawCube( projectionPositionResult, Vector3.one * size_projectionObject );

					Gizmos.color = Color_IfTrue;
				}
				else
				{
					Debug.Log("IsInShapeProject operation returned false...");

					Gizmos.color = Color_IfFalse;
				}

				Gizmos.DrawSphere(transform.position, Radius_TestObject );
			}

			DrawStandardFocusTriGizmos(focusTri, v_LblRaise.y, $"tri{focusTri.Index_inCollection}");

			if (focusTri == null)
			{
				DBG_FocusedTri = "null";
			}
			else
			{
				DBG_FocusedTri = focusTri.GetCurrentInfoString();
			}

			#region Draw the data points....
			if ( amDebuggingDataPoints )
			{
				Gizmos.color = color_dataPoints;

				if( OperationMode == 0 )
				{
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
				else if( OperationMode == 1 )
				{
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

			}
			#endregion
		}
	}
}