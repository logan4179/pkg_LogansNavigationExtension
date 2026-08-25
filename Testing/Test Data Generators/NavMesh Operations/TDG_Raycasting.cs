using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LogansNavigationExtension
{
    public class TDG_Raycasting : TDG_base
    {
		public LNX_ComponentGrabber startGrabber;
		public LNX_ComponentGrabber endGrabber;

		[Header("RESULTS")]
		public bool RaycastResult = false;
		//public List<LNX_ProjectionHit> RaycastHitResults;
		public LNX_Path ResultPath;

		[Header("DATA")]
		public List<RaycastResultEntry_v0> ResultEntries;

		[Header("DEBUG")]
		public Color Color_PathPoints;
		[Range(0f, 0.05f)] public float Size_PathPoints;
		[Range(0f, 0.25f)] public float Height_PathPtLabels;

		[ContextMenu("z CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			Debug.Log($"{nameof(CaptureDataPoint)}()...");
			/*
			CapturedStartPositions.Add( startTrans.position );
			CapturedEndPositions.Add( endTrans.position );
			CapturedRaycastResults.Add( RaycastResult );
			*/
			//Debug.Log($"Logged '{rslt_CurrentProjectedPtOnEdge}'...");

			ResultEntries.Add( new RaycastResultEntry_v0(startGrabber.CurrentHit, endGrabber.CurrentHit, ResultPath, RaycastResult) );
		}

		[ContextMenu("z CaptureProblemPosition (override)()")]
		public void CaptureProblemPosition_override()
		{
			Debug.Log("from override");

			//_dataCapture_problems.CaptureDataPoint( startTrans.position, endTrans.position );

			Debug.Log($"{nameof(CaptureProblemPosition_override)}()...");
		}

		[ContextMenu("z GoToProblem()")]
		public void GoToProblem()
		{
			Debug.Log($"going to '{Index_FocusOn}'...");
			if (startGrabber.SnapTo != LNX_Component.None)
			{
				Debug.LogWarning($"WARNING! startgrabber snap set to: '{startGrabber.SnapTo}'...");
			}
			if (endGrabber.SnapTo != LNX_Component.None)
			{
				Debug.LogWarning($"WARNING! endGrabber snap set to: '{endGrabber.SnapTo}'...");
			}

			startGrabber.Index_TriRestrict = -1;
			endGrabber.Index_TriRestrict = -1;

			startGrabber.transform.position = _dataCapture_problems.VectorCaptureLists[0].vectors[Index_FocusOn];
			startGrabber.GrabComponent();

			endGrabber.transform.position = _dataCapture_problems.VectorCaptureLists[1].vectors[Index_FocusOn];
			endGrabber.GrabComponent();
		}

		[ContextMenu("z call RunRaycast()")]
		public void RunRaycast()
		{
			mthdDbg_Report.Clear();
			ResultPath = null;

			DBG_Operation = $"{DateTime.Now}\n";

			if (startGrabber.CurrentHit == LNX_NavmeshHit.None)
			{
				DBG_Operation += $"start hit is none. Returning early...\n";
				Debug.LogWarning($"start hit is none. Returning early...\n");

				return;
			}

			if (endGrabber.CurrentHit == LNX_NavmeshHit.None)
			{
				DBG_Operation += $"end hit is none. Returning early...\n";
				Debug.LogWarning($"end hit is none. Returning early...\n");
				return;
			}

			DBG_Operation += $"using strtHit: '{startGrabber.CurrentHit}', endHit: '{endGrabber.CurrentHit}'\n";

			long totalMs = 0;
			long totalTicks = 0;
			if ( UseDebugVersion )
			{
				DBG_Operation += $"using debug version...\n";
				mthdDbg_Report.StartReport("TDG_Raycast");
				System.Diagnostics.Stopwatch stpWtch = System.Diagnostics.Stopwatch.StartNew();
				RaycastResult = _navmesh.Raycast_dbg(startGrabber.CurrentHit, endGrabber.CurrentHit,
					out ResultPath, ref mthdDbg_Report);
				stpWtch.Stop();
				totalMs = stpWtch.ElapsedMilliseconds;
				totalTicks = stpWtch.ElapsedTicks;
				mthdDbg_Report.EndReport();
			}
			else
			{
				DBG_Operation += $"using real version...\n";

				System.Diagnostics.Stopwatch stpWtch = System.Diagnostics.Stopwatch.StartNew();
				RaycastResult = _navmesh.Raycast(
					startGrabber.CurrentHit, endGrabber.CurrentHit,out ResultPath
				);
				stpWtch.Stop();
				totalMs = stpWtch.ElapsedMilliseconds;
				totalTicks = stpWtch.ElapsedTicks;
			}

			DBG_Operation += $"result: '{RaycastResult}'\n" +
				$"path: '{ResultPath}'\n" +
				$"Path: '{(ResultPath.PathPoints == null ? "null" : ResultPath.PointCount)}'\n" +
				$"path dist: '{ResultPath.TotalDistance}'\n" +
				$"total ms: '{totalMs}', total ticks: '{totalTicks}'\n";
		}

		protected override void OnDrawGizmos()
		{

			if( AmInUnitTest || Selection.activeObject != gameObject && Selection.activeObject != startGrabber.gameObject )
			{
				return;
			}

			base.OnDrawGizmos();


			//RaycastResult = _navmesh.Raycast(startTrans.position, endTrans.position, 3f); //for without path

			if ( AutoRun && (startGrabber.RecalculatedLastFrame || endGrabber.RecalculatedLastFrame) ) //"IF something's changed..." this is to make it a little snappier in the editor...
			{
				RunRaycast();
			}

			if (ResultPath != null)
			{
				Color oldClr = Gizmos.color;
				Gizmos.color = Color_PathPoints;
				Handles.color = Color_PathPoints;

				ResultPath.DrawMyGizmos( Size_PathPoints, Height_PathPtLabels );

				Gizmos.color = oldClr;
				Handles.color = oldClr;
			}

			Gizmos.color = RaycastResult ? Color.red : Color.green;

			Gizmos.DrawLine(startGrabber.transform.position, endGrabber.transform.position);

			Gizmos.DrawSphere(startGrabber.transform.position, Radius_ObjectDebugSpheres);
			//Handles.Label(startTrans.position, "strtTrans");
			Gizmos.DrawSphere(endGrabber.transform.position, Radius_ObjectDebugSpheres);
			//Handles.Label(startTrans.position, "endTrans");
		}

		#region HELPERS -------------------------------------
		[ContextMenu("z call GoToDataPoint")]
		public void GoToDataPoint()
		{
			Debug.Log($"going to '{Index_FocusOn}'...");
			if( startGrabber.SnapTo != LNX_Component.None )
			{
				Debug.LogWarning($"WARNING! startgrabber snap set to: '{startGrabber.SnapTo}'...");
			}
			if ( endGrabber.SnapTo != LNX_Component.None)
			{
				Debug.LogWarning($"WARNING! endGrabber snap set to: '{endGrabber.SnapTo}'...");
			}

			startGrabber.Index_TriRestrict = -1;
			endGrabber.Index_TriRestrict = -1;

			startGrabber.transform.position = _dataCapture.VectorCaptureLists[0].vectors[Index_FocusOn];
			startGrabber.GrabComponent();

			endGrabber.transform.position = _dataCapture.VectorCaptureLists[1].vectors[Index_FocusOn];
			endGrabber.GrabComponent();
		}


		[ContextMenu("z call DoEet")]
		public void DoEet()
		{
			ResultEntries = new List<RaycastResultEntry_v0>();
			for (int i = 0; i < _dataCapture.VectorCaptureLists.Count; i++)
			{

			}
		}
		#endregion

		#region WRITING-------------------------------------
		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			bool rslt = TDG_Manager.WriteTestObjectToJson(TDG_Manager.filePath_testData_Raycasting, this);

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
			if ( !File.Exists(TDG_Manager.filePath_testData_Raycasting) )
			{
				Debug.LogError($"path '{TDG_Manager.filePath_testData_Raycasting}' didn't exist. returning early...");
				return;
			}

			string myJsonString = File.ReadAllText(TDG_Manager.filePath_testData_Raycasting);

			JsonUtility.FromJsonOverwrite(myJsonString, this);

			EditorUtility.SetDirty(this);
		}
		#endregion

		[System.Serializable]
		public struct RaycastResultEntry_v0
		{
			[TextArea(1,5)]
			public string Description;
			public bool AmProblem;

			[Header("PARAMETERS")]
			public LNX_NavmeshHit startHit;
			public LNX_NavmeshHit endHit;

			[Header("RESULTS")]
			public LNX_Path outPath;
			public bool OperationResult;

			/*
			public RaycastResultEntry_v0( TDG_VectorCaptureList cptrList )
			{
				Description = "";

				startHit = new LNX_NavmeshHit(
			}
			*/

			public RaycastResultEntry_v0(LNX_NavmeshHit strtHit, LNX_NavmeshHit ndHt, LNX_Path otPth, bool opRslt)
			{
				Description = "";
				AmProblem = false;
				startHit = strtHit;
				endHit = ndHt;
				outPath = otPth;
				OperationResult = opRslt;
			}
		}
	}
}
