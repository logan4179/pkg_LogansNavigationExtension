using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_PositionIsBetweenTriPoints : TDG_base
    {
		//TODO: HAVEN'T YET IMPLEMENTED THIS TDG

        public Transform Trans_PosParam;
        public Transform Trans_TriPtA;
        public Transform Trans_TriPtB;
        public Transform Trans_TriPtC;

		[Header("CURRENT CAPTURE")]
		public bool CurrentResult = false;

		[Header("DEBUG")]
		public string DBG_Method;

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			Debug.Log($"{nameof(CaptureDataPoint)}()");

		}

		#region HELPERS ---------------------------------------------------
		[ContextMenu("z call CaptureComponents()")]
		public void CaptureComponents()
		{
			Debug.Log($"{nameof(CaptureComponents)}()...");


		}

		[ContextMenu("z call SendToDataPoint")]
		public void SendToDataPoint()
		{
			//transform.position = CapturedPerspectivePositions[Index_GoToDataPoint];
		}

		[ContextMenu("z call CaptureProblemPosition (derived)")]
		public override void CaptureProblemPosition()
		{

		}

		[ContextMenu("z call SendToProblemPosition (derived)")]
		public override void SendToProblemPosition()
		{

		}
		#endregion

		[HideInInspector] public Vector3 Trans_TriPointA_lastpos;
		[HideInInspector] public Vector3 Trans_TriPtB_lastpos;
		[HideInInspector] public Vector3 Trans_TriPtC_lastpos;
		[HideInInspector] public Vector3 Trans_PosParam_lastpos;

		protected override void OnDrawGizmos()
		{
			DBG_Operation = "";

			if
			(
				Selection.activeObject != gameObject &&
				Selection.activeGameObject != Trans_PosParam.gameObject &&
				Selection.activeGameObject != Trans_TriPtA.gameObject &&
				Selection.activeGameObject != Trans_TriPtB.gameObject &&
				Selection.activeGameObject != Trans_TriPtC.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			base.OnDrawGizmos();

			/*
			if (
				Trans_PosParam.position != Trans_PosParam_lastpos ||
				Trans_TriPtA.position != Trans_TriPointA_lastpos ||
				Trans_TriPtB.position != Trans_TriPtB_lastpos ||
				Trans_TriPtC.position != Trans_TriPtC_lastpos
			)
			{
				CaptureComponents();

				Trans_PosParam_lastpos = Trans_PosParam.position;
				Trans_TriPointA_lastpos = Trans_TriPtA.position;
				Trans_TriPtB_lastpos = Trans_TriPtB.position;
				Trans_TriPtC_lastpos = Trans_TriPtC.position;
			}
			*/

			Gizmos.DrawSphere(Trans_PosParam.position, Radius_ObjectDebugSpheres );
			Gizmos.DrawSphere(Trans_TriPtA.position, Radius_ObjectDebugSpheres * 0.5f);
			Gizmos.DrawSphere(Trans_TriPtB.position, Radius_ObjectDebugSpheres * 0.5f);
			Gizmos.DrawSphere(Trans_TriPtC.position, Radius_ObjectDebugSpheres * 0.5f);


			DBG_Operation += $"Commencing operation...\n";

			CurrentResult = LNX_Utils.PositionIsBetweenTriPoints(
				Trans_PosParam.position, 
				Trans_TriPtA.position, Trans_TriPtB.position, Trans_TriPtC.position,
				_navmesh.V_SurfaceOrientation/*, ref DBG_Method*/
			);

			DBG_Operation += $"Operation returned: '{CurrentResult}'\n";

			if (CurrentResult)
			{
				Gizmos.color = Color.green;
			}
			else
			{
				Gizmos.color = Color.red;
			}

			Gizmos.DrawLine( Trans_TriPtA.position, Trans_TriPtB.position );
			Gizmos.DrawLine(Trans_TriPtA.position, Trans_TriPtC.position);
			Gizmos.DrawLine(Trans_TriPtB.position, Trans_TriPtC.position);

			float upAmt = 0.5f;
			Gizmos.DrawLine(Trans_TriPtA.position, Trans_TriPtA.position + (Vector3.up * upAmt) );
			Handles.Label(Trans_TriPtA.position + (Vector3.up * upAmt), $"A'{Trans_TriPtA.position}'");

			Gizmos.DrawLine(Trans_TriPtB.position, Trans_TriPtB.position + (Vector3.up * upAmt));
			Handles.Label(Trans_TriPtB.position + (Vector3.up * upAmt), $"B'{Trans_TriPtB.position}'");

			Gizmos.DrawLine(Trans_TriPtC.position, Trans_TriPtC.position + (Vector3.up * upAmt));
			Handles.Label(Trans_TriPtC.position + (Vector3.up * upAmt), $"C'{Trans_TriPtC.position}'");

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
