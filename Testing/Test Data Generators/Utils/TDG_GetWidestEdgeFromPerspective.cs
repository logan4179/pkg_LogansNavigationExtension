using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_GetWidestEdgeFromPerspective : TDG_base
    {
		//TODO: NEED TO CREATE DATA AND FULLY INTEGRATE THIS INTO THE TDG MANAGER CLASS

		public Transform Trans_triGrabber;

		public int CurrentTriIndex = 0;
		LNX_Triangle CurrentTriangle => _navmesh.Triangles[CurrentTriIndex];


		[Header("CURRENT RESULTS")]
		public int Index_CurrentEdge = 0;


		[Header("DATA")]
		public List<Vector3> CapturedPerspectivePositions = new List<Vector3>();
		public List<Vector3> CapturedTriCenterPositions = new List<Vector3>();


		//[Header("DEBUG")]

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			CapturedPerspectivePositions.Add(transform.position);
			CapturedTriCenterPositions.Add(CurrentTriangle.V_Center);

			DrawDataPointCapture(CapturedPerspectivePositions[CapturedPerspectivePositions.Count - 1],
				Color.magenta
			);


			Debug.Log($"'{CapturedPerspectivePositions[CapturedPerspectivePositions.Count - 1]}'");
		}

		#region HELPERS ---------------------------------------------------
		[ContextMenu("z call SampleFocusTri()")]
		public void SampleFocusTri()
		{
			Debug.Log($"{nameof(SampleFocusTri)}()...");

			LNX_NavmeshHit hit = LNX_NavmeshHit.None;

			if (_navmesh.SamplePosition(Trans_triGrabber.position, out hit, 2f, false))
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

			Debug.Log(CurrentTriangle.GetAnomolyString(_navmesh));
		}

		[ContextMenu("z call DoEet()")]
		public void DoEet()
		{

		}

		[ContextMenu("z call SendToDataPoint")]
		public void SendToDataPoint()
		{
			transform.position = CapturedPerspectivePositions[Index_GoToProblem];
		}
		#endregion

		protected override void OnDrawGizmos()
		{
			DBG_Operation = "";

			if (Selection.activeObject != gameObject && Selection.activeObject != Trans_triGrabber.gameObject )
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Object not selected";
				return;
			}

			if (CurrentTriangle == null)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. CurrentTriangle null";
				return;
			}

			DBG_Operation += $"CurrentTriangle: '{CurrentTriangle.Index_inCollection}' at '{CurrentTriangle.V_Center}'\n";

			base.OnDrawGizmos();

			DrawStandardFocusTriGizmos(CurrentTriangle, 1f, $"tri{CurrentTriangle.Index_inCollection}", Color.magenta);
			Index_CurrentEdge = -1;

			DBG_Operation += $"Commencing operation...\n";
			string s = "";
			LNX_Edge foundEdge = LNX_Utils.GetWidestEdgeFromPerspective(transform.position, CurrentTriangle, ref s);
			DBG_Operation += $"Completed operation. found edge null?: '{foundEdge == null}'\n";

			if (foundEdge != null)
			{
				Index_CurrentEdge = foundEdge.ComponentIndex;
				DrawStandardEdgeFocusGizmos( foundEdge, 0.3f, "foundEdge", Color.yellow );

				Gizmos.DrawLine(transform.position, foundEdge.StartPosition);
				Gizmos.DrawLine(transform.position, foundEdge.EndPosition);

				DBG_Operation += $"angToStart: '{Vector3.SignedAngle(CurrentTriangle.V_FlattenedCenter - transform.position,foundEdge.StartPosition - transform.position,_navmesh.GetSurfaceProjectionVector())}'\n";

				DBG_Operation += $"angToEnd: '{Vector3.SignedAngle(CurrentTriangle.V_FlattenedCenter - transform.position,foundEdge.EndPosition - transform.position,_navmesh.GetSurfaceProjectionVector())}'\n";
			}

			DBG_Operation += $"Operation complete. \n" +
				$"";

			Gizmos.DrawSphere(transform.position, Radius_ObjectDebugSpheres);
			Gizmos.DrawSphere(Trans_triGrabber.position, Radius_ObjectDebugSpheres * 0.5f);
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
