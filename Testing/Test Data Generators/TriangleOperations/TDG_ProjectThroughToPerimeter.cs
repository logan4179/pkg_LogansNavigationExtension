using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_ProjectThroughToPerimeter : TDG_base
    {
		[SerializeField] private Transform trans_start;

		public LNX_Triangle CurrentTriangle;
		public LNX_Edge ProjectedEdge;
		public LNX_ProjectionHit CurrentHit;

		[Header("DATA CAPTURE")]
		public List<Vector3> CapturedStartPositions = new List<Vector3>();
		public List<Vector3> CapturedEndPositions = new List<Vector3>();
		public List<Vector3> CapturedTriCenters = new List<Vector3>();
		public List<Vector3> CapturedEdgeMidPoints = new List<Vector3>();
		public List<Vector3> CapturedProjectionPoints = new List<Vector3>();

		//[Header("DEBUG")]


		[ContextMenu("z call SampleFocusTri()")]
		public void SampleFocusTri()
		{
			Debug.Log($"{nameof(SampleFocusTri)}()...");

			LNX_ProjectionHit hit = LNX_ProjectionHit.None;

			if ( _navmesh.SamplePosition(trans_start.position, out hit, 2f, false) )
			{
				CurrentTriangle = _navmesh.Triangles[hit.Index_Hit];
				Debug.Log($"Succesful sample! Set new triangle to: '{hit.Index_Hit}'");
			}
			else
			{
				Debug.Log($"sample unsuccesful...");
			}
		}

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			CapturedStartPositions.Add(trans_start.position);
			CapturedEndPositions.Add(transform.position);
			CapturedTriCenters.Add( CurrentTriangle.V_Center);
			CapturedEdgeMidPoints.Add( ProjectedEdge.MidPosition);
			CapturedProjectionPoints.Add( CurrentHit.HitPosition);

			DrawDataPointCapture( CapturedStartPositions[CapturedStartPositions.Count - 1], Color.magenta );
			DrawDataPointCapture( CapturedEndPositions[CapturedEndPositions.Count - 1], Color.magenta );
			DrawDataPointCapture( CapturedProjectionPoints[CapturedProjectionPoints.Count - 1], Color.magenta );

			Debug.Log($"Logged '{CurrentHit.HitPosition}'...");
		}

		protected override void OnDrawGizmos()
		{
			DBG_Operation = "";

			if ( Selection.activeGameObject != gameObject && Selection.activeGameObject != trans_start.gameObject && Selection.activeGameObject != transform.parent.gameObject )
			{
				DBG_Operation += $"OnDrawGizmos short-circuit. Something wrong with selection...";
				return;
			}

			if (CurrentTriangle == null)
			{
				DBG_Operation += $"OnDrawGizmos short-circuit. Need to sample a focus triangle...";
				Debug.LogWarning($"Need to sample a focus triangle...");
				return;
			}

			base.OnDrawGizmos();
			DBG_Operation += $"Commencing operation using triangle '{CurrentTriangle.Index_inCollection}'...\n";

			DrawStandardFocusTriGizmos( CurrentTriangle, 1f, $"tri{CurrentTriangle.Index_inCollection}");

			CurrentHit = CurrentTriangle.ProjectThroughToPerimeter
			(
				trans_start.position,
				transform.position
			);

			DBG_Operation += $"completed operation. {nameof(CurrentHit)} now: '{CurrentHit}'...\n\n" +
				$"triangle report..............\n" +
				$"{CurrentTriangle.dbg_prjctThrhToPerim}\nend of report............\n";

			if( CurrentHit.Index_Hit > -1 && CurrentHit.Index_Hit < 3 )
			{
				ProjectedEdge = CurrentTriangle.Edges[CurrentHit.Index_Hit];
				DrawStandardEdgeFocusGizmos( ProjectedEdge, 0.1f, $"edge{ProjectedEdge.MyCoordinate.ComponentIndex}", Color.green );

				Gizmos.DrawCube( CurrentHit.HitPosition, Vector3.one * 0.05f );
			}

			Gizmos.color = CurrentHit.Equals(LNX_ProjectionHit.None) ? Color.red : Color.green;

			Gizmos.DrawLine( trans_start.position, transform.position );

			Gizmos.DrawSphere(trans_start.position, Radius_ObjectDebugSpheres);
			//Handles.Label(startTrans.position, "strtTrans");
			Gizmos.DrawSphere(transform.position, Radius_ObjectDebugSpheres);
			//Handles.Label(startTrans.position, "endTrans");
		}

		#region HELPERS ---------------------------------------
		[ContextMenu("z call GoToDataPoint")]
		public void GoToDataPoint()
		{
			Debug.LogError("not yet implemented");
		}
		#endregion

		#region WRITING-------------------------------------
		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			bool rslt = TDG_Manager.WriteTestObjectToJson(TDG_Manager.filePath_testData_projectThroughToPerimeter, this);

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
			if ( !File.Exists(TDG_Manager.filePath_testData_projectThroughToPerimeter) )
			{
				Debug.LogError($"path '{TDG_Manager.filePath_testData_projectThroughToPerimeter}' didn't exist. returning early...");
				return;
			}

			string myJsonString = File.ReadAllText(TDG_Manager.filePath_testData_projectThroughToPerimeter);

			JsonUtility.FromJsonOverwrite(myJsonString, this);

			EditorUtility.SetDirty(this);
		}
		#endregion
	}
}
