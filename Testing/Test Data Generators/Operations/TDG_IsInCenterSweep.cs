using System;
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
		public LNX_ComponentGrabber _Grabber_Pos;
		public LNX_ComponentGrabber _Grabber_Triangle;

		[Header("PARAM")]
		public bool IncludeOnPerim;

		LNX_Triangle CurrentTriangle => _Grabber_Triangle.CurrentlyGrabbedTriangle;

		[Header("RESULTS")]
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

		[ContextMenu("z call SendToDataPoint")]
		public void SendToDataPoint()
		{
			//transform.position = CapturedStartPositions[Index_GoToProblem];
		}
		#endregion

		public bool UseDebugMethodVersion;
		protected override void OnDrawGizmos()
		{
			if ( Selection.activeObject != gameObject )
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Object not selected";
				mthdDbg_Report.Clear();
				return;
			}

			if( CurrentTriangle == null )
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. CurrentTriangle null";
				mthdDbg_Report.Clear();

				return;
			}

			if( !_Grabber_Pos.RecalculatedLastFrame && !_Grabber_Triangle.RecalculatedLastFrame )
			{
				return;
			}

			DBG_Operation = $"{DateTime.Now}\n" +
				$"using CurrentTriangle: '{CurrentTriangle.Index_inCollection}'..." +
				$"using pos: '{transform.position}'\n";

			base.OnDrawGizmos();

			DrawStandardFocusTriGizmos(CurrentTriangle, 1f, $"tri{CurrentTriangle.Index_inCollection}", Color.magenta );

			CurrentRslt_vert0 = false;
			CurrentRslt_vert1 = false;
			CurrentRslt_vert2 = false;

			DBG_Operation += $"Commencing operation...\n";

			if( UseDebugMethodVersion )
			{
				mthdDbg_Report.StartReport();
				mthdDbg_Report.Log("=============================================================");
				CurrentRslt_vert0 = CurrentTriangle.Verts[0].ProjectionIsInCenterSweep_dbg(
					transform.position - CurrentTriangle.Verts[0].V_Position, ref mthdDbg_Report,
					IncludeOnPerim
				);
				mthdDbg_Report.Log("=============================================================");
				CurrentRslt_vert1 = CurrentTriangle.Verts[1].ProjectionIsInCenterSweep_dbg(
					transform.position - CurrentTriangle.Verts[1].V_Position, ref mthdDbg_Report, 
					IncludeOnPerim
				);
				mthdDbg_Report.Log("=============================================================");
				CurrentRslt_vert2 = CurrentTriangle.Verts[2].ProjectionIsInCenterSweep_dbg(
					transform.position - CurrentTriangle.Verts[2].V_Position, ref mthdDbg_Report, 
					IncludeOnPerim
				);
				mthdDbg_Report.EndReport();
			}
			else
			{
				CurrentRslt_vert0 = CurrentTriangle.Verts[0].ProjectionIsInCenterSweep(transform.position - CurrentTriangle.Verts[0].V_Position);
				CurrentRslt_vert1 = CurrentTriangle.Verts[1].ProjectionIsInCenterSweep(transform.position - CurrentTriangle.Verts[1].V_Position);
				CurrentRslt_vert2 = CurrentTriangle.Verts[2].ProjectionIsInCenterSweep(transform.position - CurrentTriangle.Verts[2].V_Position);

			}

			DBG_Operation += $"Operation complete.\n" +
				$"v0 rslt: '{CurrentRslt_vert0}'\n" +
				$"v1 rslt: '{CurrentRslt_vert1}'\n" +
				$"v2 rslt: '{CurrentRslt_vert2}'\n" +

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
