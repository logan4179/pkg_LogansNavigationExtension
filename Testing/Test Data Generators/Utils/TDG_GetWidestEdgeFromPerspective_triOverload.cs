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

		public LNX_ComponentGrabber Grabber_PerspectiveTri;
		public LNX_ComponentGrabber Grabber_OtherTri;

		LNX_Triangle PerspectiveTriangle => Grabber_PerspectiveTri.CurrentlyGrabbedTriangle;

		LNX_Triangle OtherTriangle => Grabber_OtherTri.CurrentlyGrabbedTriangle;


		[Header("CURRENT RESULTS")]
		public int Index_CurrentEdge = 0;

		[Header("DEBUG")]
		[TextArea(1,20)] public string DBG_Method;
		public Color Color_perspectiveTri;
		public Color Color_otherTri;


		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			_dataCapture.CaptureDataPoint
			(
				PerspectiveTriangle.V_Center, OtherTriangle.V_Center, PerspectiveTriangle.Edges[Index_CurrentEdge].MidPosition
			);			
		}

		[ContextMenu("z call CaptureProblemPosition()")]
		public override void CaptureProblemPosition()
		{
			_dataCapture_problems.CaptureDataPoint(
				Grabber_PerspectiveTri.transform.position, Grabber_OtherTri.transform.position, PerspectiveTriangle.Edges[Index_CurrentEdge].MidPosition				
			);
		}

		#region HELPERS ---------------------------------------------------
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

		protected override void OnDrawGizmos()
		{
			DBG_Operation = "";
			DBG_Method = "";

			if (
				Selection.activeObject != gameObject && 
				Selection.activeObject != Grabber_PerspectiveTri.gameObject && 
				Selection.activeGameObject != Grabber_OtherTri.gameObject
			)
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

			base.OnDrawGizmos();

			DBG_Operation += $"using PerspectiveTriangle: '{PerspectiveTriangle.Index_inCollection}' at '{PerspectiveTriangle.V_Center}'\n" +
				$"OtherTriangle: '{OtherTriangle.Index_inCollection}' at '{OtherTriangle.V_Center}'\n";
			/*
			if( PerspectiveTriangle == OtherTriangle)
			{
				DBG_Operation += $"OnDrawGizmos short-circuit. Perspective and other triangles the same.";
				return;
			}*/

			DrawStandardFocusTriGizmos(PerspectiveTriangle, 0.1f, "", Color_perspectiveTri, true, 0.01f, true);
			//DrawTriGizmo(PerspectiveTriangle, Color.magenta, 0.01f);

			DrawStandardFocusTriGizmos(OtherTriangle, 0.1f, "", Color_otherTri, true, 0.01f, true);
			//DrawTriGizmo(OtherTriangle, Color.magenta, 0.01f);

			Index_CurrentEdge = -1;

			DBG_Operation += $"Commencing operation...\n";
			LNX_Edge foundEdge = LNX_Utils.GetWidestEdgeFromPerspective(PerspectiveTriangle, OtherTriangle, ref DBG_Method);
			DBG_Operation += $"Completed operation. found edge null?: '{foundEdge == null}'\n";

			if (foundEdge != null)
			{
				DBG_Operation += $"foundedge: '{foundEdge}'\n";
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

			Grabber_PerspectiveTri.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_OtherTri.DrawMyGizmos(Radius_ObjectDebugSpheres);
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
