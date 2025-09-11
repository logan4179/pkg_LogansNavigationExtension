using System.Collections;
using System.Collections.Generic;
using System.IO;
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
		public List<bool> CapturedRaycastResults = new List<bool>();

		public bool RaycastResult = false;

		[Header("PATH")]
		//public List<LNX_ProjectionHit> RaycastHitResults;
		public LNX_Path ResultPath;
		public Color Color_PathPoints;
		[Range(0f, 0.05f)] public float Size_PathPoints;
		[Range(0f, 0.25f)] public float Height_PathPtLabels;

		[Header("DEBUG")]
		[TextArea(1,15)]
		public string DBG_NavmeshRaycastRprt;
		[TextArea(1, 20)]
		public string DBG_NavmeshProjectionRprt;

		[HideInInspector] public Vector3 CachedLastStartPos;
		[HideInInspector] public Vector3 CachedLastEndPos;

		[ContextMenu("z CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			CapturedStartPositions.Add( startTrans.position );
			CapturedEndPositions.Add( endTrans.position );
			CapturedRaycastResults.Add( RaycastResult );
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

			//RaycastResult = _navmesh.Raycast(startTrans.position, endTrans.position, 3f); //for without path

			if ( startTrans.position != CachedLastStartPos || endTrans.position != CachedLastEndPos ) //"IF something's changed..." this is to make it a little snappier in the editor...
			{
				//Debug.Log("refreshing odg...");
				RaycastResult = _navmesh.Raycast( startTrans.position, endTrans.position, 3f, out ResultPath );
			}

			if( !RaycastResult )
			{
				Color oldClr = Gizmos.color;
				Gizmos.color = Color_PathPoints;
				Handles.color = Color_PathPoints;

				/*
				for ( int i = 0; i < ResultPath.PathPoints.Count; i++ )
				{
					Gizmos.DrawSphere( ResultPath.PathPoints[i].V_Position, Size_PathPoints );

					Gizmos.DrawLine(
						ResultPath.PathPoints[i].V_Position, ResultPath.PathPoints[i].V_Position + (Vector3.up * Height_PathPtLabels)
					);
					Handles.Label(
						ResultPath.PathPoints[i].V_Position + (Vector3.up * Height_PathPtLabels), $"{i}"
					);

					if (i > 0)
					{
						Handles.DrawDottedLine(
							ResultPath.PathPoints[i-1].V_Position, ResultPath.PathPoints[i].V_Position, 8f
						);
					}
				}
				*/

				ResultPath.DrawMyGizmos( Size_PathPoints, Height_PathPtLabels );

				Gizmos.color = oldClr;
				Handles.color = oldClr;
			}

			Gizmos.color = RaycastResult ? Color.red : Color.green;

			Gizmos.DrawLine(startTrans.position, endTrans.position);

			Gizmos.DrawSphere(startTrans.position, Radius_ObjectDebugSpheres);
			//Handles.Label(startTrans.position, "strtTrans");
			Gizmos.DrawSphere(endTrans.position, Radius_ObjectDebugSpheres);
			//Handles.Label(startTrans.position, "endTrans");

			DBG_NavmeshRaycastRprt = _navmesh.DBGRaycast;
			DBG_NavmeshProjectionRprt = _navmesh.DBG_NavmeshProjection;

			CachedLastStartPos = startTrans.position;
			CachedLastEndPos = endTrans.position;
		}

		#region HELPERS -------------------------------------
		[ContextMenu("z call GoToDataPoint")]
		public void GoToDataPoint()
		{
			startTrans.position = CapturedStartPositions[index_focusProblem];
			endTrans.position = CapturedEndPositions[index_focusProblem];
		}

		[ContextMenu("z call DoEet")]
		public void DoEet()
		{
			CapturedRaycastResults = new List<bool>();

			for ( int i = 0; i < CapturedStartPositions.Count; i++ )
			{
				bool rslt = _navmesh.Raycast(CapturedStartPositions[i], CapturedEndPositions[i], 3f);

				CapturedRaycastResults.Add( rslt );
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
	}
}
