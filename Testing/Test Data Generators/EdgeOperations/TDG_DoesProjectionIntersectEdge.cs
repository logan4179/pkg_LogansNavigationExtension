using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_DoesProjectionIntersectEdge : TDG_base
	{
		[Header("START OF DERIVED CLASS-------------------")]

		[SerializeField] protected LNX_NavMeshDebugger _debugger;

		public LNX_ComponentGrabber StartPointGrabber;
		public LNX_ComponentGrabber EndPointGrabber;
		public LNX_ComponentGrabber EdgeGrabber;
		public LNX_Edge CurrentlyGrabbedEdge => EdgeGrabber.CurrentlyGrabbedEdge;
		/*
		public Transform startTrans;
		public Transform endTrans;
		//public LNX_ComponentCoordinate EdgeCoordinate;*/

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
		public LNX_NavmeshHit CurrentProjectedHit = LNX_NavmeshHit.None;

		//[Header("DEBUG")]

		[ContextMenu("z CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			CapturedStartPositions.Add(StartPointGrabber.transform.position);
			CapturedEndPositions.Add(EndPointGrabber.transform.position);
			CapturedProjectedPositions.Add( CurrentProjectedHit.Position );
			CapturedProjectionResults.Add( CurrentProjectionResult );
			CapturedTriangleCenterPositions.Add( _navmesh.GetTriangle(EdgeGrabber.transform.position).V_Center );
			CapturedEdgeCenterPositions.Add( CurrentlyGrabbedEdge.MidPosition );


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

			/*
			CapturedProjectedPositions = new List<Vector3>();
			CapturedTriangleCenterPositions = new List<Vector3>();
			CapturedEdgeCenterPositions = new List<Vector3>();

			for( int i = 0; i < CapturedStartPositions.Count; i++ )
			{
				//LNX_Triangle startTri = _navmesh.GetTriangle(  ); //what do I do here?
				LNX_NavmeshHit hit = LNX_NavmeshHit.None;

				if ( _navmesh.SamplePosition(CapturedStartPositions[i], out hit, 2f, false) )
				{

					EdgeCoordinate = new LNX_ComponentCoordinate(hit.TriIndex, CurrentlyGrabbedEdge.ComponentIndex);
					SetDebuggerFocusToMine();
					Debug.Log($"Succesful sample! Set new edgecoordinate to: '{CurrentlyGrabbedEdge.ToString()}'");
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
					_navmesh.GetSurfaceNormalVector(),
					out CurrentProjectedPosition
				);
			}

			Debug.Log(
				$"Captured start pos: '{CapturedStartPositions[CapturedStartPositions.Count - 1]}', " +
				$"endpos: '{CapturedEndPositions[CapturedEndPositions.Count - 1]}', " +
				$"and projectedPos: '{CapturedProjectedPositions[CapturedProjectedPositions.Count - 1]}'..."
			);
			*/
		}

		[ContextMenu("z CaptureProblemPosition (override)()")]
		public void CaptureProblemPosition_override()
		{
			Debug.Log("from override");

			if ( CurrentlyGrabbedEdge == null )
			{
				Debug.LogError("currently grabbed edge is null...");
				return;
			}

			_dataCapture_problems.CaptureDataPoint
			(
				StartPointGrabber.transform.position,
				EndPointGrabber.transform.position, 
				CurrentlyGrabbedEdge.MidPosition
			);

			Debug.Log($"{nameof(CaptureProblemPosition_override)}()...");
		}

		[ContextMenu("z GoToProblem()")]
		public void GoToProblem()
		{
			//startTrans.position = problemPositions[index_focusProblem];
			//endTrans.position = ProblemEndPositions[index_focusProblem];

			
			//startTrans.position = CapturedStartPositions[index_focusProblem];
			//endTrans.position = CapturedEndPositions[index_focusProblem];
			
			//TODO: Implement now that I'm using the TDG_DataCapture object...

			Debug.Log($"{nameof(GoToProblem)}()...");
		}

		[ContextMenu("z SetDebuggerFocusToMine()")]
		public void SetDebuggerFocusToMine()
		{
			Debug.Log($"{nameof(SetDebuggerFocusToMine)}()...");

			_debugger.Grabber_FocusTri.transform.position = _navmesh.Triangles[CurrentlyGrabbedEdge.TriangleIndex].V_Center;
		}

		protected override void OnDrawGizmos()
		{

			if ( 
				AmInUnitTest || 
				(Selection.activeObject != gameObject && Selection.activeObject != StartPointGrabber.gameObject && 
				Selection.activeGameObject != EndPointGrabber.gameObject && Selection.activeGameObject != EdgeGrabber.gameObject) 
			)
			{
				DBG_Operation = "short-circuit";

				return;
			}

			base.OnDrawGizmos();

			if ( CurrentlyGrabbedEdge == null )
			{
				DBG_Operation += $"OnDrawGizmos short-circuit. {nameof(CurrentlyGrabbedEdge)} is null...";
				return;
			}

			if( 
				StartPointGrabber.RecalculatedLastFrame ||
				EndPointGrabber.RecalculatedLastFrame ||
				EdgeGrabber.RecalculatedLastFrame
			)
			{
				CurrentProjectedHit = LNX_NavmeshHit.None;
				CurrentProjectionResult = false;
				mthdDbg_Report.Clear();

				DBG_Operation = $"{DateTime.Now}\n";

				DBG_Operation += $"using origin param: '{StartPointGrabber.transform.position}', " +
					$"dest param: '{EndPointGrabber.transform.position}'\n" +
					$"using edge: '{CurrentlyGrabbedEdge}'\n" +
					$"Commencing edge project...\n";

				if( UseDebugVersion )
				{
					DBG_Operation += $"(using debug version...)\n";
					mthdDbg_Report.StartReport();

					CurrentProjectionResult =
					CurrentlyGrabbedEdge.DoesProjectionIntersectEdge_dbg
					(
						StartPointGrabber.transform.position,
						EndPointGrabber.transform.position,
						out CurrentProjectedHit,
						ref mthdDbg_Report,
						true
					);

					mthdDbg_Report.EndReport();
				}
				else
				{
					CurrentProjectionResult =
						CurrentlyGrabbedEdge.DoesProjectionIntersectEdge
						(
							StartPointGrabber.transform.position,
							EndPointGrabber.transform.position,
							out CurrentProjectedHit,
							false
						);
				}

				DBG_Operation += $"result: '{CurrentProjectionResult}'\n" +
					$"projected Hit: '{CurrentProjectedHit}'";
			}

			DrawStandardFocusTriGizmos(_navmesh.Triangles[CurrentlyGrabbedEdge.TriangleIndex], 1f, $"tri{CurrentlyGrabbedEdge.TriangleIndex}", Color.magenta);
			DrawStandardEdgeFocusGizmos(CurrentlyGrabbedEdge, 0.1f, CurrentlyGrabbedEdge.ToString(), Color.yellow);

			Gizmos.color = CurrentProjectionResult ? Color.green : Color.red;

			Gizmos.DrawLine(StartPointGrabber.transform.position, EndPointGrabber.transform.position);

			Gizmos.DrawSphere(StartPointGrabber.transform.position, Radius_ObjectDebugSpheres);
			//Handles.Label(startTrans.position, "strtTrans");
			Gizmos.DrawSphere(EndPointGrabber.transform.position, Radius_ObjectDebugSpheres);
			//Handles.Label(startTrans.position, "endTrans");

			if( CurrentProjectedHit != LNX_NavmeshHit.None )
			{
				Gizmos.color = Color.yellow;
				Gizmos.DrawCube( CurrentProjectedHit.Position, Vector3.one * Radius_ProjectPos );

				LNX_DrawingUtils.DrawLabeledPoint(CurrentProjectedHit.Position,
					CurrentProjectedHit.Position + Vector3.up * Radius_ProjectPos * 15f, "hit", Color.yellow
				);

				Gizmos.color = Color.yellowGreen;
				Gizmos.DrawLine(StartPointGrabber.transform.position, CurrentProjectedHit.Position);
			}

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
