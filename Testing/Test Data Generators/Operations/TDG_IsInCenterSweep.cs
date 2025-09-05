using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace LogansNavigationExtension
{
    public class TDG_IsInCenterSweep : TDG_base
	{
		public LNX_Triangle CurrentTriangle;
		public bool CurrentRslt_vert0;
		public bool CurrentRslt_vert1;
		public bool CurrentRslt_vert2;

		[Header("DATA")]
		public List<Vector3> CapturedStartPositions = new List<Vector3>();
		public List<Vector3> CapturedTriCenterPositions = new List<Vector3>();

        public List<bool> Results_Vert0 = new List<bool>();
		public List<Vector3> CapturedVertPositions_vert0 = new List<Vector3>();

		public List<bool> Results_Vert1 = new List<bool>();
		public List<Vector3> CapturedVertPositions_vert1 = new List<Vector3>();

		public List<bool> Results_Vert2 = new List<bool>();
		public List<Vector3> CapturedVertPositions_vert2 = new List<Vector3>();

		[Header("DEBUG")]
		public int Index_GoToDataPoint = 0;

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
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
		}

		#region HELPERS ---------------------------------------------------
		[ContextMenu("z call SampleFocusTri()")]
		public void SampleFocusTri()
		{
			Debug.Log($"{nameof(SampleFocusTri)}()...");

			LNX_ProjectionHit hit = LNX_ProjectionHit.None;

			if (_navmesh.SamplePosition(transform.position, out hit, 2f, false))
			{
				CurrentTriangle = _navmesh.Triangles[hit.Index_Hit];
				Debug.Log($"Succesful sample! Set new triangle to: '{hit.Index_Hit}'");
			}
			else
			{
				Debug.Log($"sample unsuccesful...");
			}
		}

		[ContextMenu("z call DoEet()")]
		public void DoEet()
		{
			CapturedVertPositions_vert0 = new List<Vector3>();
			CapturedVertPositions_vert1 = new List<Vector3>();
			CapturedVertPositions_vert2 = new List<Vector3>();

			for ( int i = 0; i < CapturedStartPositions.Count; i++ )
			{
				LNX_Triangle tri = _navmesh.GetTriangle( CapturedTriCenterPositions[i] );

				CapturedVertPositions_vert0.Add(tri.Verts[0].V_Position);
				CapturedVertPositions_vert1.Add(tri.Verts[1].V_Position);
				CapturedVertPositions_vert2.Add(tri.Verts[2].V_Position);

			}
		}
		#endregion

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

			base.OnDrawGizmos();

			DrawStandardFocusTriGizmos(CurrentTriangle, 1f, $"tri{CurrentTriangle.Index_inCollection}");

			CurrentRslt_vert0 = false;
			CurrentRslt_vert1 = false;
			CurrentRslt_vert2 = false;

			DBG_Operation += $"Commencing operation...\n";
			CurrentRslt_vert0 = CurrentTriangle.Verts[0].IsInCenterSweep( transform.position );
			CurrentRslt_vert1 = CurrentTriangle.Verts[1].IsInCenterSweep(transform.position);
			CurrentRslt_vert2 = CurrentTriangle.Verts[2].IsInCenterSweep(transform.position);

			DBG_Operation += $"Operation complete. \n" +

				$"rslt (vert0): '{CurrentRslt_vert0}'\n" +
				$"rprt------\n" +
				$"{CurrentTriangle.Verts[0].DBG_IsInCenterSweep}\n\n" +

				$"rslt (vert1): '{CurrentRslt_vert1}'\n" +
				$"rprt------\n" +
				$"{CurrentTriangle.Verts[1].DBG_IsInCenterSweep}\n\n" +

				$"rslt (vert2): '{CurrentRslt_vert2}'\n" +
				$"rprt------\n" +
				$"{CurrentTriangle.Verts[2].DBG_IsInCenterSweep}\n\n" +
				$"";

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
