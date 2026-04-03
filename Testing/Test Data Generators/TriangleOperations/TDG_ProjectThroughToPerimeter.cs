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
		public LNX_ComponentGrabber Grabber_OuterPos;

		public LNX_Triangle CurrentTriangle => Grabber_CurrentTri.CurrentlyGrabbedTriangle;
		public LNX_Edge ProjectedEdge;
		public LNX_NavmeshHit CurrentHit;

		[Header("GO TO")]
		public TDG_TryProjectPathThrough _tdg_projectPathThrough;

		//[Header("DEBUG")]

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			_dataCapture.CaptureDataPoint(
				Grabber_CurrentTri.transform.position, Grabber_OuterPos.transform.position,
				CurrentTriangle.V_Center, ProjectedEdge.MidPosition, CurrentHit.Position
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

			if (CurrentTriangle == null)
			{
				DBG_Operation += $"OnDrawGizmos short-circuit. Need to sample a focus triangle...";
				Debug.LogWarning($"Need to sample a focus triangle...");
				return;
			}

			base.OnDrawGizmos();

			DBG_Operation = "";
			DBG_Method = "";
			CurrentHit = LNX_NavmeshHit.None;

			DrawStandardFocusTriGizmos( CurrentTriangle, 1f, $"", Color.magenta);

			DBG_Operation += $"using triangle '{CurrentTriangle.Index_inCollection}'...\n" +
				$"commencing operation...\n";

			if ( !CurrentTriangle.ProjectThroughToPerimeter
			(
				Grabber_CurrentTri.transform.position, Grabber_OuterPos.transform.position, out CurrentHit, ref DBG_Method)
			)
			{
				ProjectedEdge = CurrentTriangle.Edges[CurrentHit.TriIndex];
				DrawStandardEdgeFocusGizmos( ProjectedEdge, 0.1f, $"edge{ProjectedEdge.MyCoordinate.ComponentIndex}", Color.green );

				Gizmos.DrawCube( CurrentHit.Position, Vector3.one * 0.025f );
				Handles.Label(CurrentHit.Position + (Vector3.up * 0.03f), "hitPosition" );
			}

			DBG_Operation += $"completed operation. {nameof(CurrentHit)} now: '{CurrentHit}'...\n";


			Gizmos.color = CurrentHit.Equals(LNX_NavmeshHit.None) ? Color.red : Color.green;

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
