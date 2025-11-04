using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace LogansNavigationExtension
{
    public class TDG_GetWidestEdgeFromPerspective_triOverload : TDG_base
    {
		//TODO: NEED TO CREATE DATA AND FULLY INTEGRATE THIS INTO THE TDG MANAGER CLASS

		public Transform Trans_perspectiveTriGrabber;
		public Transform Trans_otherTriGrabber;

		public int PerspectiveTriIndex = 0;
		LNX_Triangle PerspectiveTriangle => _navmesh.Triangles[PerspectiveTriIndex];

		public int OtherTriIndex = 0;
		LNX_Triangle OtherTriangle => _navmesh.Triangles[OtherTriIndex];


		[Header("CURRENT RESULTS")]
		public int Index_CurrentEdge = 0;


		[Header("DATA")]
		public List<Vector3> CapturedPerspectiveTriCenters = new List<Vector3>();
		public List<Vector3> CapturedOtherTriCenters = new List<Vector3>();
		public List<Vector3> CapturedResultEdgeCenters = new List<Vector3>();

		//[Header("SAVED POSITIONS")]


		[Header("DEBUG")]
		[TextArea(1,20)] public string DBG_Method;
		public Color Color_perspectiveTri;
		public Color Color_otherTri;


		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{

			CapturedPerspectiveTriCenters.Add( PerspectiveTriangle.V_Center );
			CapturedOtherTriCenters.Add( OtherTriangle.V_Center );
			CapturedResultEdgeCenters.Add( PerspectiveTriangle.Edges[Index_CurrentEdge].MidPosition );

			DrawDataPointCapture(CapturedPerspectiveTriCenters[CapturedPerspectiveTriCenters.Count - 1],
				Color.magenta
			);

			DrawDataPointCapture(CapturedOtherTriCenters[CapturedOtherTriCenters.Count - 1],
				Color.magenta
			);
			DrawDataPointCapture(CapturedResultEdgeCenters[CapturedResultEdgeCenters.Count - 1],
				Color.magenta
			);


			Debug.Log($"'{CapturedPerspectiveTriCenters[CapturedPerspectiveTriCenters.Count - 1]}'");
			
		}

		[ContextMenu("z call CaptureProblemPosition()")]
		public override void CaptureProblemPosition()
		{
			_dataCapture_problems.CaptureDataPoint(Trans_perspectiveTriGrabber.transform.position, Trans_otherTriGrabber.transform.position );
		}

		#region HELPERS ---------------------------------------------------
		[ContextMenu("z call SampleTris()")]
		public void SampleTris()
		{
			Debug.Log($"{nameof(SampleTris)}()...");

			LNX_ProjectionHit hit = LNX_ProjectionHit.None;

			if (_navmesh.SamplePosition(Trans_perspectiveTriGrabber.position, out hit, 2f, false))
			{
				PerspectiveTriIndex = hit.Index_Hit;
				Debug.Log($"Succesful sample! Set new triangle to: '{hit.Index_Hit}'");
			}
			else
			{
				Debug.Log($"sample unsuccesful...");
			}

			if (_navmesh.SamplePosition(Trans_otherTriGrabber.position, out hit, 2f, false))
			{
				OtherTriIndex = hit.Index_Hit;
				Debug.Log($"Succesful sample! Set new triangle to: '{hit.Index_Hit}'");
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

			PerspectiveTriangle.SayCurrentInfo(_navmesh);

			Debug.Log(PerspectiveTriangle.GetAnomolyString(_navmesh));

			OtherTriangle.SayCurrentInfo(_navmesh);

			Debug.Log(OtherTriangle.GetAnomolyString(_navmesh));
		}

		[ContextMenu("z call DoEet()")]
		public void DoEet()
		{

		}

		[ContextMenu("z call SendToDataPoint")]
		public void SendToDataPoint()
		{
			//transform.position = CapturedPerspectivePositions[Index_GoToDataPoint];
		}
		#endregion


		[SerializeReference] private Vector3 v_prspctv_lastPos = Vector3.zero;
		[SerializeReference] private Vector3 v_other_lastPos = Vector3.zero;
		protected override void OnDrawGizmos()
		{
			DBG_Operation = "";
			DBG_Method = "";

			if (Selection.activeObject != gameObject && Selection.activeObject != Trans_otherTriGrabber.gameObject && Selection.activeGameObject != Trans_perspectiveTriGrabber.gameObject)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Object not selected";
				return;
			}

			if (PerspectiveTriangle == null)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. PerspectiveTriangle null";
				return;
			}

			if (OtherTriangle == null)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. OtherTriangle null";
				return;
			}


			DBG_Operation += $"PerspectiveTriangle: '{PerspectiveTriangle.Index_inCollection}' at '{PerspectiveTriangle.V_Center}'\n" +
				$"OtherTriangle: '{OtherTriangle.Index_inCollection}' at '{OtherTriangle.V_Center}'\n";

			base.OnDrawGizmos();

			if( Trans_perspectiveTriGrabber.position != v_prspctv_lastPos || Trans_otherTriGrabber.position != v_other_lastPos )
			{
				SampleTris();
			}

			if( PerspectiveTriangle == OtherTriangle)
			{
				DBG_Operation += $"OnDrawGizmos short-circuit. Perspective and other triangles the same.";
				return;
			}

			DrawStandardFocusTriGizmos(PerspectiveTriangle, 0.1f, $"pspctvTri", Color_perspectiveTri, true, 0.01f, true);
			//DrawTriGizmo(PerspectiveTriangle, Color.magenta, 0.01f);

			DrawStandardFocusTriGizmos(OtherTriangle, 0.1f, $"otherTri", Color_otherTri, true, 0.01f, true);
			//DrawTriGizmo(OtherTriangle, Color.magenta, 0.01f);

			Index_CurrentEdge = -1;

			DBG_Operation += $"Commencing operation...\n";
			LNX_Edge foundEdge = LNX_Utils.GetWidestEdgeFromPerspective(PerspectiveTriangle, OtherTriangle, ref DBG_Method);
			DBG_Operation += $"Completed operation. found edge null?: '{foundEdge == null}'\n";

			if (foundEdge != null)
			{
				Index_CurrentEdge = foundEdge.ComponentIndex;
				DrawStandardEdgeFocusGizmos(foundEdge, 0.3f, "foundEdge", Color.yellow);
				Gizmos.color = Color.green;
			}
			else
			{
				Gizmos.color = Color.red;
			}


				DBG_Operation += $"Operation complete. \n" +
					$"";

			Gizmos.DrawSphere(Trans_perspectiveTriGrabber.position, Radius_ObjectDebugSpheres);
			Gizmos.DrawSphere(Trans_otherTriGrabber.position, Radius_ObjectDebugSpheres);

			v_prspctv_lastPos = Trans_perspectiveTriGrabber.position;
			v_other_lastPos = Trans_otherTriGrabber.position;
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
