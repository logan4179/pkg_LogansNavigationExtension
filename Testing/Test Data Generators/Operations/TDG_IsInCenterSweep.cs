using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace LogansNavigationExtension
{
    public class TDG_IsInCenterSweep : TDG_base
	{
		public int CurrentTriIndex = 0;

		LNX_Triangle CurrentTriangle => _navmesh.Triangles[CurrentTriIndex];
		public bool CurrentRslt_vert0;
		public bool CurrentRslt_vert1;
		public bool CurrentRslt_vert2;

		//[Header("DATA")]


		//[Header("DEBUG")]

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			//todo: implement with new data structure
			/*
			CapturedStartPositions.Add(transform.position);
			CapturedTriCenterPositions.Add( CurrentTriangle.V_Center );

			Results_Vert0.Add( CurrentRslt_vert0 );
			CapturedVertPositions_vert0.Add(CurrentTriangle.Verts[0].V_Position );

			Results_Vert1.Add(CurrentRslt_vert1 );
			CapturedVertPositions_vert1.Add(CurrentTriangle.Verts[1].V_Position);

			Results_Vert2.Add(CurrentRslt_vert2 );
			CapturedVertPositions_vert2.Add(CurrentTriangle.Verts[2].V_Position);

			DrawDataPointCapture(CapturedStartPositions[CapturedStartPositions.Count - 1],
				Color.magenta
			);


			Debug.Log( $"'{CapturedStartPositions[CapturedStartPositions.Count - 1]}'" );
			*/
		}

		#region HELPERS ---------------------------------------------------
		[ContextMenu("z call SampleFocusTri()")]
		public void SampleFocusTri()
		{
			Debug.Log($"{nameof(SampleFocusTri)}()...");

			LNX_NavmeshHit hit = LNX_NavmeshHit.None;

			if (_navmesh.SamplePosition(transform.position, out hit, 2f, false))
			{
				CurrentTriIndex = hit.TriIndex;
				Debug.Log($"Succesful sample! Set new triangle to: '{hit.TriIndex}'");
			}
			else
			{
				Debug.Log($"sample unsuccesful...");
			}
		}

		[ContextMenu("z call SayFocusTri()")]
		public void SayFocusTri()
		{
			Debug.Log($"{nameof(SayFocusTri)}()...");

			_navmesh.Triangles[CurrentTriIndex].SayCurrentInfo(_navmesh);

			Debug.Log( CurrentTriangle.GetAnomolyString(_navmesh) );
		}



		[ContextMenu("z call SendToDataPoint")]
		public void SendToDataPoint()
		{
			//transform.position = CapturedStartPositions[Index_GoToProblem];
		}
		#endregion

		[TextArea(1,10)] public string DBGAngleTo;
		protected override void OnDrawGizmos()
		{
			DBG_Operation = "";

			if ( Selection.activeObject != gameObject )
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Object not selected";
				return;
			}

			if( CurrentTriangle == null )
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. CurrentTriangle null";
				return;
			}

			DBG_Operation += $"CurrentTriangle: '{CurrentTriangle.Index_inCollection}' at '{CurrentTriangle.V_Center}'\n";

			DBGAngleTo = $"first sibling pathpts count: '{CurrentTriangle.Verts[0].FirstSiblingRelationship.PathTo.PathPoints.Count}'\n" +
				$"second sibling pathpts count: '{CurrentTriangle.Verts[0].SecondSiblingRelationship.PathTo.PathPoints.Count}'\n";

			DBGAngleTo += $"to first sibling prop: '{CurrentTriangle.Verts[0].V_ToFirstSiblingVert}'\n" +
				//$"calc: '{}'\n" +
				$"";

			base.OnDrawGizmos();

			DrawStandardFocusTriGizmos(CurrentTriangle, 1f, $"tri{CurrentTriangle.Index_inCollection}", Color.magenta );

			CurrentRslt_vert0 = false;
			CurrentRslt_vert1 = false;
			CurrentRslt_vert2 = false;

			DBG_Operation += $"Commencing operation...\n";

			CurrentRslt_vert0 = CurrentTriangle.Verts[0].ProjectionIsInCenterSweep( transform.position - CurrentTriangle.Verts[0].V_Position );
			CurrentRslt_vert1 = CurrentTriangle.Verts[1].ProjectionIsInCenterSweep(transform.position - CurrentTriangle.Verts[1].V_Position);
			CurrentRslt_vert2 = CurrentTriangle.Verts[2].ProjectionIsInCenterSweep(transform.position - CurrentTriangle.Verts[2].V_Position);

			DBG_Operation += $"Operation complete.";

			Gizmos.DrawSphere( transform.position, Radius_ObjectDebugSpheres );

			Gizmos.color = CurrentRslt_vert0 ? Color.green : Color.red;
			Gizmos.DrawSphere( CurrentTriangle.Verts[0].V_Position, Radius_ProjectPos );

			Gizmos.color = CurrentRslt_vert1 ? Color.green : Color.red;
			Gizmos.DrawSphere( CurrentTriangle.Verts[1].V_Position, Radius_ProjectPos );

			Gizmos.color = CurrentRslt_vert2 ? Color.green : Color.red;
			Gizmos.DrawSphere( CurrentTriangle.Verts[2].V_Position, Radius_ProjectPos );

		}

		#region WRITING ----------------------------------------------------
		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			bool rslt = TDG_Manager.WriteTestObjectToJson(TDG_Manager.filePath_testData_isInCenterSweep, this);

			if (rslt)
			{
				LastWriteTime = System.DateTime.Now.ToString();
				return true;
			}

			return false;
		}
		#endregion
	}
}
