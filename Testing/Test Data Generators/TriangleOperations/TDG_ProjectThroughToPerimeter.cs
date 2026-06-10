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
		public LNX_ComponentGrabber Grabber_CurrentTri;
		public LNX_Triangle CurrentlyGrabbedTriangle => Grabber_CurrentTri.CurrentlyGrabbedTriangle;
		public LNX_ComponentGrabber Grabber_OuterPos;
		public Vector3 CurrentlyGrabbedOuterPos => Grabber_OuterPos.GetCurrentlyGrabbedPosition();

		public LNX_Triangle CurrentTriangle => Grabber_CurrentTri.CurrentlyGrabbedTriangle;
		public LNX_Edge ProjectedEdge;
		public LNX_NavmeshHit perimHitParam;

		[Header("GO TO")]
		public TDG_TryProjectPathThrough _tdg_projectPathThrough;

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			_dataCapture.CaptureDataPoint(
				Grabber_CurrentTri.transform.position, Grabber_OuterPos.transform.position,
				CurrentTriangle.V_Center, ProjectedEdge.MidPosition, perimHitParam.Position
			);
		}

		[ContextMenu("z call GoToTDG()")]
		public void GoToTDG()
		{
			Grabber_CurrentTri.transform.position = _tdg_projectPathThrough.StartVert.V_Position;
			Grabber_OuterPos.transform.position = _tdg_projectPathThrough.EndVert.V_Position;
		}

		protected override void OnDrawGizmos()
		{
			#region SHORT-CIRCUITING ===============================
			if
			( 
				AmInUnitTest || 
				Selection.activeGameObject != gameObject && 
				Selection.activeGameObject != Grabber_CurrentTri.gameObject && 
				Selection.activeGameObject != Grabber_OuterPos.gameObject 
			)
			{
				DBG_Operation += $"OnDrawGizmos short-circuit. Something wrong with selection...";
				return;
			}

			DBG_Operation = "";
			perimHitParam = LNX_NavmeshHit.None;

			if (CurrentTriangle == null)
			{
				DBG_Operation += $"OnDrawGizmos short-circuit. Need to sample a focus triangle...";
				Debug.LogWarning($"Need to sample a focus triangle...");
				return;
			}
			#endregion

			base.OnDrawGizmos();


			DrawStandardFocusTriGizmos( CurrentTriangle, 1f, $"", Color.magenta);

			DBG_Operation += $"Recalcluated: '{DateTime.Now}'...\n" +
				$"using triangle '{CurrentTriangle.Index_inCollection}'...\n" +
				$"commencing operation...\n";

			mthdDbg_Report.StartReport( "" );
			/*if ( CurrentTriangle.ProjectThroughToPerimeter
				(
					Grabber_CurrentTri.transform.position, Grabber_OuterPos.transform.position, out currentHitParam, ref DBG_Method
				)
			)*/

			/*
			if (CurrentTriangle.ProjectThroughToPerimeter_dbg
				(
					//Grabber_CurrentTri.transform.position, Grabber_OuterPos.transform.position, out currentHitParam, ref mDbg_Report //todo: dws
					Grabber_CurrentTri.CurrentHit, Grabber_OuterPos.CurrentHit, out perimHitParam, ref mthdDbg_Report

				)
			)
			{
				DBG_Operation += $"projection returned true. perimHitParam: '{perimHitParam}'...\n";
				ProjectedEdge = CurrentTriangle.Edges[perimHitParam.EdgeIndex];
				DrawStandardEdgeFocusGizmos( ProjectedEdge, 0.1f, $"edge{ProjectedEdge.MyCoordinate.ComponentIndex}", Color.green );

				Gizmos.DrawCube( perimHitParam.Position, Vector3.one * 0.025f );
				Handles.Label(perimHitParam.Position + (Vector3.up * 0.03f), "hitPosition" );
			}
			else
			{
				DBG_Operation += $"CurrentTriangle.ProjectThroughToPerimeter() returned false...\n";
			}
			*/

			mthdDbg_Report.EndReport();

			DBG_Operation += $"completed operation. {nameof(perimHitParam)} now: '{perimHitParam}'...\n";


			Gizmos.color = perimHitParam.Equals(LNX_NavmeshHit.None) ? Color.red : Color.green;

			Grabber_CurrentTri.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_OuterPos.DrawMyGizmos(Radius_ObjectDebugSpheres);

			/*
			Gizmos.DrawLine( Grabber_CurrentTri.transform.position, transform.position );

			Gizmos.DrawSphere(trans_start.position, Radius_ObjectDebugSpheres);
			//Handles.Label(startTrans.position, "strtTrans");
			Gizmos.DrawSphere(transform.position, Radius_ObjectDebugSpheres);
			//Handles.Label(startTrans.position, "endTrans");
			*/
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
