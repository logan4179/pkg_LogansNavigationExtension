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
		public LNX_ComponentGrabber startTrans;
		public LNX_ComponentGrabber endTrans;

		public bool RaycastResult = false;

		[Header("PATH")]
		//public List<LNX_ProjectionHit> RaycastHitResults;
		public LNX_Path ResultPath;
		public Color Color_PathPoints;
		[Range(0f, 0.05f)] public float Size_PathPoints;
		[Range(0f, 0.25f)] public float Height_PathPtLabels;

		//[Header("DEBUG")]


		[ContextMenu("z CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			/*
			CapturedStartPositions.Add( startTrans.position );
			CapturedEndPositions.Add( endTrans.position );
			CapturedRaycastResults.Add( RaycastResult );
			*/
			//Debug.Log($"Logged '{rslt_CurrentProjectedPtOnEdge}'...");


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
			//startTrans.position = problemPositions[Index_FocusProblem];
			//endTrans.position = ProblemEndPositions[Index_FocusProblem];

			//startTrans.position = CapturedStartPositions[index_focusProblem];
			//endTrans.position = CapturedEndPositions[index_focusProblem];

			Debug.Log($"{nameof(GoToProblem)}()...");
		}

		[ContextMenu("z call RunRaycast()")]
		public void RunRaycast()
		{
			mthdDbg_Report.Clear();
			ResultPath = LNX_Path.None;

			DBG_Operation = $"{DateTime.Now}\n";

			if (startTrans.CurrentHit == LNX_NavmeshHit.None)
			{
				DBG_Operation += $"start hit is none. Returning early...\n";
				return;
			}

			if (endTrans.CurrentHit == LNX_NavmeshHit.None)
			{
				DBG_Operation += $"end hit is none. Returning early...\n";
				return;
			}

			DBG_Operation += $"using strtHit: '{startTrans.CurrentHit}', endHit: '{endTrans.CurrentHit}'\n";

			mthdDbg_Report.StartReport("TDG_Raycast");

			RaycastResult = _navmesh.Raycast_dbg(startTrans.CurrentHit, endTrans.CurrentHit,
				out ResultPath, ref mthdDbg_Report);

			mthdDbg_Report.EndReport();

			DBG_Operation += $"result: '{RaycastResult}'\n" +
				$"Path: '{(ResultPath.PathPoints == null ? "null" : ResultPath.PointCount)}'\n" +
				$"path dist: '{ResultPath.TotalDistance}'\n";
		}

		protected override void OnDrawGizmos()
		{

			if( AmInUnitTest || Selection.activeObject != gameObject && Selection.activeObject != startTrans.gameObject )
			{
				return;
			}

			base.OnDrawGizmos();


			//RaycastResult = _navmesh.Raycast(startTrans.position, endTrans.position, 3f); //for without path

			if ( AutoCalculate && (startTrans.RecalculatedLastFrame || endTrans.RecalculatedLastFrame) ) //"IF something's changed..." this is to make it a little snappier in the editor...
			{
				RunRaycast();
			}

			if( !RaycastResult )
			{
				Color oldClr = Gizmos.color;
				Gizmos.color = Color_PathPoints;
				Handles.color = Color_PathPoints;

				ResultPath.DrawMyGizmos( Size_PathPoints, Height_PathPtLabels );

				Gizmos.color = oldClr;
				Handles.color = oldClr;
			}

			Gizmos.color = RaycastResult ? Color.red : Color.green;

			Gizmos.DrawLine(startTrans.transform.position, endTrans.transform.position);

			Gizmos.DrawSphere(startTrans.transform.position, Radius_ObjectDebugSpheres);
			//Handles.Label(startTrans.position, "strtTrans");
			Gizmos.DrawSphere(endTrans.transform.position, Radius_ObjectDebugSpheres);
			//Handles.Label(startTrans.position, "endTrans");
		}

		#region HELPERS -------------------------------------
		[ContextMenu("z call GoToDataPoint")]
		public void GoToDataPoint()
		{
			//startTrans.position = CapturedStartPositions[index_focusProblem];
			//endTrans.position = CapturedEndPositions[index_focusProblem];
		}


		[ContextMenu("z call DoEet")]
		public void DoEet()
		{

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
	}
}
