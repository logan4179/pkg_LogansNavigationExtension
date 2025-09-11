using System.Collections;
using System.Collections.Generic;
using System.IO;
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

		public List<bool> CapturedProjectionResults = new List<bool>();

		/// <summary>Solely for identifying the triangle that the current iterated data point should use </summary>
		public List<Vector3> CapturedTriangleCenterPositions = new List<Vector3>();

		/// <summary>Solely for identifying the edge that the current iterated data point should use </summary>
		public List<Vector3> CapturedEdgeCenterPositions = new List<Vector3>();

		public bool CurrentProjectionResult = false;
		public Vector3 CurrentProjectedPosition = Vector3.zero;

		//[Header("DEBUG")]


		[ContextMenu("z CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			CapturedStartPositions.Add(startTrans.position);
			CapturedEndPositions.Add(endTrans.position);
			CapturedProjectedPositions.Add( CurrentProjectedPosition );
			CapturedProjectionResults.Add( CurrentProjectionResult );
			CapturedTriangleCenterPositions.Add( _navmesh.GetTriangle(EdgeCoordinate).V_Center );
			CapturedEdgeCenterPositions.Add(
				_navmesh.GetTriangle(EdgeCoordinate).Edges[EdgeCoordinate.ComponentIndex].MidPosition
			);


			DrawDataPointCapture(CapturedStartPositions[CapturedStartPositions.Count - 1], Color.magenta);
			DrawDataPointCapture(CapturedEndPositions[CapturedEndPositions.Count - 1], Color.magenta);
			DrawDataPointCapture(CapturedProjectedPositions[CapturedProjectedPositions.Count - 1], Color.magenta);

			Debug.Log(
				$"Captured start pos: '{CapturedStartPositions[CapturedStartPositions.Count - 1]}', " +
				$"endpos: '{CapturedEndPositions[CapturedEndPositions.Count - 1]}', " +
				$"and projectedPos: '{CapturedProjectedPositions[CapturedProjectedPositions.Count - 1]}'..."
			);
		}

		/// <summary>
		/// Runs through all captured start and end positions, and re-calculates their projected positions.
		/// </summary>
		[ContextMenu("z RecaptureDataPointsAfterApiChange()")]
		public void RecaptureDataPointsAfterApiChange()
		{
			//Note: didn't finish this bc I'm not sure I can do this because of the need to designate an edge and triangle...
			#region SHORT-CIRCUIT----------------------------------------------------
			if ( CapturedStartPositions == null )
			{
				Debug.LogError( $"{nameof(CapturedStartPositions)} collection is null..." );
				return;
			}
			else if ( CapturedStartPositions.Count <= 0 )
			{
				Debug.LogError($"{nameof(CapturedStartPositions)} collection count is '{CapturedStartPositions.Count}'...");
				return;
			}

			if (CapturedEndPositions == null)
			{
				Debug.LogError($"{nameof(CapturedEndPositions)} collection is null...");
				return;
			}
			else if (CapturedEndPositions.Count <= 0)
			{
				Debug.LogError($"{nameof(CapturedEndPositions)} collection count is '{CapturedEndPositions.Count}'...");
				return;
			}

			if ( CapturedStartPositions.Count != CapturedEndPositions.Count )
			{
				Debug.LogError($"{nameof(CapturedStartPositions)} and {nameof(CapturedEndPositions)} collections counts are different...");
				return;
			}
			#endregion

			CapturedProjectedPositions = new List<Vector3>();
			CapturedTriangleCenterPositions = new List<Vector3>();
			CapturedEdgeCenterPositions = new List<Vector3>();

			for( int i = 0; i < CapturedStartPositions.Count; i++ )
			{
				//LNX_Triangle startTri = _navmesh.GetTriangle(  ); //what do I do here?
				LNX_ProjectionHit hit = LNX_ProjectionHit.None;

				if ( _navmesh.SamplePosition(CapturedStartPositions[i], out hit, 2f, false) )
				{



					EdgeCoordinate = new LNX_ComponentCoordinate(hit.Index_Hit, EdgeCoordinate.ComponentIndex);
					SetDebuggerFocusToMine();
					Debug.Log($"Succesful sample! Set new edgecoordinate to: '{EdgeCoordinate.ToString()}'");
				}
				else
				{
					Debug.Log($"sample unsuccesful...");
				}

				CurrentProjectionResult =
				_navmesh.GetEdge(EdgeCoordinate).DoesProjectionIntersectEdge
				(
					CapturedStartPositions[i],
					CapturedEndPositions[i],
					_navmesh.GetSurfaceNormal(),
					out CurrentProjectedPosition
				);
			}

			Debug.Log(
				$"Captured start pos: '{CapturedStartPositions[CapturedStartPositions.Count - 1]}', " +
				$"endpos: '{CapturedEndPositions[CapturedEndPositions.Count - 1]}', " +
				$"and projectedPos: '{CapturedProjectedPositions[CapturedProjectedPositions.Count - 1]}'..."
			);
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
			DBG_Operation = "";

			if (Selection.activeObject != gameObject && Selection.activeObject != startTrans.gameObject)
			{
				return;
			}

			base.OnDrawGizmos();

			if ( EdgeCoordinate.TrianglesIndex < 0 || EdgeCoordinate.ComponentIndex < 0 )
			{
				DBG_Operation += $"{nameof(EdgeCoordinate)} OnDrawGizmos short-circuit. {nameof(EdgeCoordinate)}: '{EdgeCoordinate}'...";
				return;
			}

			DrawStandardFocusTriGizmos( _navmesh.Triangles[EdgeCoordinate.TrianglesIndex], 1f, $"tri{EdgeCoordinate.TrianglesIndex}" );
			DrawStandardEdgeFocusGizmos( _navmesh.GetEdge(EdgeCoordinate), 0.1f, "", Color.magenta );

			DBG_Operation += $"Commencing edge project...\n" +
				$"projection report says:\n" +
				$"---------------------------------\n";
			CurrentProjectionResult =
				_navmesh.GetEdge(EdgeCoordinate).DoesProjectionIntersectEdge
				(
					startTrans.position,
					endTrans.position,
					_navmesh.GetSurfaceNormal(),
					out CurrentProjectedPosition
				);
			DBG_Operation += $"=============================\n";

			Gizmos.color = CurrentProjectionResult ? Color.green : Color.red;

			Gizmos.DrawLine(startTrans.position, endTrans.position);

			Gizmos.DrawSphere(startTrans.position, Radius_ObjectDebugSpheres);
			//Handles.Label(startTrans.position, "strtTrans");
			Gizmos.DrawSphere(endTrans.position, Radius_ObjectDebugSpheres);
			//Handles.Label(startTrans.position, "endTrans");

			Gizmos.DrawCube( CurrentProjectedPosition, Vector3.one * Radius_ProjectPos );
		}

		#region HELPERS -----------------------------------------------------
		/// <summary>
		/// Logs the data points to the console. This can be useful for saving the values in 
		/// a text document when refactoring this TDG
		/// </summary>
		[ContextMenu("z call SayDataPoints()")]
		public void SayDataPoints()
		{
			string s = $"{nameof(CapturedStartPositions)}------------------------------\n";
			if ( CapturedStartPositions == null )
			{
				Debug.LogError( "collection was null..." );
			}
			else
			{
				s += ( $"logging '{CapturedStartPositions.Count}' {nameof(CapturedStartPositions)}..." );
				for (int i = 0; i < CapturedStartPositions.Count; i++)
				{
					s += LNX_UnitTestUtilities.LongVectorString(CapturedStartPositions[i]) + "\n";
				}
			}

			s += $"\n{nameof(CapturedEndPositions)}------------------------------\n";
			if (CapturedEndPositions == null)
			{
				Debug.LogError("collection was null...");
			}
			else
			{
				s += ($"logging '{CapturedEndPositions.Count}' {nameof(CapturedEndPositions)}...\n");
				for (int i = 0; i < CapturedEndPositions.Count; i++)
				{
					s += LNX_UnitTestUtilities.LongVectorString(CapturedEndPositions[i]) + "\n";
				}
			}

			s += $"\n{nameof(CapturedProjectedPositions)}------------------------------\n";
			if (CapturedProjectedPositions == null)
			{
				Debug.LogError("collection was null...");
			}
			else
			{
				s += ($"logging '{CapturedProjectedPositions.Count}' {nameof(CapturedProjectedPositions)}...\n");
				for (int i = 0; i < CapturedProjectedPositions.Count; i++)
				{
					s += LNX_UnitTestUtilities.LongVectorString(CapturedProjectedPositions[i]) + "\n";
				}
			}

			s += $"\n{nameof(CapturedTriangleCenterPositions)}------------------------------\n";
			if (CapturedTriangleCenterPositions == null)
			{
				Debug.LogError("collection was null...");
			}
			else
			{
				s += ($"logging '{CapturedTriangleCenterPositions.Count}' {nameof(CapturedTriangleCenterPositions)}...\n");
				for (int i = 0; i < CapturedTriangleCenterPositions.Count; i++)
				{
					s += LNX_UnitTestUtilities.LongVectorString(CapturedTriangleCenterPositions[i]) + "\n";
				}
			}

			s += $"\n{nameof(CapturedEdgeCenterPositions)}------------------------------\n";
			if (CapturedEdgeCenterPositions == null)
			{
				Debug.LogError("collection was null...");
			}
			else
			{
				s += ($"logging '{CapturedEdgeCenterPositions.Count}' {nameof(CapturedEdgeCenterPositions)}...\n");
				for (int i = 0; i < CapturedEdgeCenterPositions.Count; i++)
				{
					s += LNX_UnitTestUtilities.LongVectorString(CapturedEdgeCenterPositions[i]) + "\n";
				}
			}

			Debug.Log(s);
		}

		[ContextMenu("z call HelpAfterRefactor()")]
		public void HelpAfterRefactor()
		{

		}
		#endregion

		#region WRITING-------------------------------------
		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			bool rslt = TDG_Manager.WriteTestObjectToJson(TDG_Manager.filePath_testData_doesProjectionIntersectEdge, this );

			if (rslt)
			{
				LastWriteTime = System.DateTime.Now.ToString();
				return true;

			}

			return false;
		}

		[ContextMenu("z call RecreateMeFromJson()")]
		public void RecreateMeFromJson()
		{
			if (!File.Exists(TDG_Manager.filePath_testData_doesProjectionIntersectEdge))
			{
				Debug.LogError($"path '{TDG_Manager.filePath_testData_doesProjectionIntersectEdge}' didn't exist. returning early...");
				return;
			}

			string myJsonString = File.ReadAllText(TDG_Manager.filePath_testData_doesProjectionIntersectEdge);

			JsonUtility.FromJsonOverwrite(myJsonString, this);

			EditorUtility.SetDirty(this);
		}
		#endregion
	}
}
