using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_AmInVectorCone : TDG_base
    {
		public Transform Trans_pos;
        public Transform Trans_Crnr;
        public Transform Trans_LegA;
        public Transform Trans_LegB;

		[Header("RESULTS")]
		public bool CurrentOperationResult;

		//[Header("SEND TO")]

		[Header("DEBUG")]
		public Color Color_Corners;
		public Vector3 v_lblOffset;

		#region HELPERS =================================================
		[ContextMenu("z call GrabProblemPoint()")]
		public void GrabProblemPoint()
		{
			Debug.Log($"{nameof(GrabProblemPoint)}()...");

			_dataCapture_problems.CaptureDataPoint(
				Trans_pos.position, Trans_Crnr.position, Trans_LegA.position, Trans_LegB.position
			);
		}

		[ContextMenu("z call SendToTDG")]
		public void SendToTDG()
		{
			/*
			Trans_pos.transform.position = _tdg_doesEdgeObstructArea.ObstructEdge.MidPosition;
			Trans_Crnr.transform.position = _tdg_doesEdgeObstructArea.PerspectiveVert.V_Position;
			Trans_LegA.transform.position = _tdg_doesEdgeObstructArea.DestinationTriangle.Verts[0].V_Position;
			Trans_LegB.transform.position = _tdg_doesEdgeObstructArea.DestinationTriangle.Verts[1].V_Position;
			*/
		}
		#endregion

		public void RunOperation()
		{
			mthdDbg_Report.Clear();

			bool oldRslt = CurrentOperationResult;

			CurrentOperationResult = false;

			DBG_Operation = $"{System.DateTime.Now}...\n";

			DBG_Operation += $"using position: '{Trans_pos.position}'\n" +
				$"";

			DBG_Operation += $"\ncommencing operation...\n";

			System.Diagnostics.Stopwatch stpWtch;

			if (UseDebugVersion)
			{
				DBG_Operation += $"using DEBUG version...\n";
				mthdDbg_Report.StartReport();
				stpWtch = System.Diagnostics.Stopwatch.StartNew();

				CurrentOperationResult = LNX_Utils.AmInVectorCone(
					LNX_Utils.FlatVector(Trans_pos.position - Trans_Crnr.position).normalized,
					LNX_Utils.FlatVector(Trans_LegA.position - Trans_Crnr.position).normalized,
					LNX_Utils.FlatVector(Trans_LegB.position - Trans_Crnr.position).normalized,
					Vector3.up,
					true
				);

				stpWtch.Stop();
				mthdDbg_Report.EndReport();
			}
			else
			{
				DBG_Operation += $"using REGULAR version...\n";
				stpWtch = System.Diagnostics.Stopwatch.StartNew();

				CurrentOperationResult = LNX_Utils.AmInVectorCone(
					LNX_Utils.FlatVector(Trans_pos.position - Trans_Crnr.position).normalized,
					LNX_Utils.FlatVector(Trans_LegA.position - Trans_Crnr.position).normalized,
					LNX_Utils.FlatVector(Trans_LegB.position - Trans_Crnr.position).normalized,
					Vector3.up,
					true
				);

				stpWtch.Stop();
			}

			if ( CurrentOperationResult != oldRslt )
			{
				Debug.DrawLine(Trans_pos.position, Trans_pos.position + (Vector3.up * 3f), Color.magenta, 1f );
			}

			DBG_Operation += $"operation complete. OperationResult: '{CurrentOperationResult}'\n";


			DBG_Operation += $"operation took '{stpWtch.ElapsedTicks}' ticks...\n";
		}


		protected override void OnDrawGizmos()
		{
			if
			(
				Selection.activeGameObject != gameObject &&
				Selection.activeGameObject != Trans_Crnr.gameObject &&
				Selection.activeGameObject != Trans_LegA.gameObject &&
				Selection.activeGameObject != Trans_LegB.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			DBG_Operation = "";

			Gizmos.color = Color_Corners;
			Gizmos.DrawSphere(Trans_Crnr.position, Radius_ObjectDebugSpheres);
			Handles.Label(Trans_Crnr.position + v_lblOffset, "Crnr");

			Gizmos.DrawSphere(Trans_LegA.position, Radius_ObjectDebugSpheres);
			Handles.Label(Trans_LegA.position + v_lblOffset, "LegA");
			Gizmos.DrawLine(Trans_Crnr.position, Trans_LegA.position);

			Gizmos.DrawSphere(Trans_LegB.position, Radius_ObjectDebugSpheres);
			Handles.Label(Trans_LegB.position + v_lblOffset, "LegB");
			Gizmos.DrawLine(Trans_Crnr.position, Trans_LegB.position);

			DBG_Operation += $"Commencing operation...\n";

			RunOperation();
			/*
			CurrentOperationResult = LNX_Utils.AmInVectorCone(
				LNX_Utils.FlatVector(Trans_pos.position - Trans_Crnr.position).normalized,
				LNX_Utils.FlatVector(Trans_LegA.position - Trans_Crnr.position).normalized,
				LNX_Utils.FlatVector(Trans_LegB.position - Trans_Crnr.position).normalized,
				Vector3.up,
				true
			);
			*/


			if (CurrentOperationResult)
			{
				Gizmos.color = Color.green;
			}
			else
			{
				Gizmos.color = Color.red;
			}

			Gizmos.DrawSphere(Trans_pos.position, Radius_ObjectDebugSpheres);
			Handles.Label(Trans_pos.position + v_lblOffset, "Pos");
			Gizmos.DrawLine(Trans_Crnr.position, Trans_pos.position);

		}
    }
}
