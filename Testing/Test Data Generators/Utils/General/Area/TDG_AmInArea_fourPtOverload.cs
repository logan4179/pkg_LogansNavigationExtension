using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_AmInArea_fourPtOverload : TDG_base
    {
		//TODO: Implement in manager and create tests!

        [Header("REFERENCE")]
		public LNX_ComponentGrabber Grabber_Pos;
		public LNX_ComponentGrabber Grabber_CrnrA;
		public LNX_ComponentGrabber Grabber_CrnrB;
		public LNX_ComponentGrabber Grabber_CrnrC;
		public LNX_ComponentGrabber Grabber_CrnrD;


		[Header("MatchTDGs")]
		public TDG_DoesEdgeObstructEdgePath _TDG_DoesEdgeObstructEdgePath;
		public TDG_GetTriPath _TDG_getTriPath;
		public TDG_DoesEdgeObstructTriPath _tdg_doesEdgeObstructTriPath;

		[Header("RESULTS")]
		public bool CurrentOperationResult = false;

		[Header("DEBUG")]
		[TextArea(1,15)] public string DBG_Method;
		public Vector3 v_lblOffset;

		[ContextMenu("z call CaptureProblemPoint()")]
		public void CaptureProblemPoint()
		{
			_dataCapture_problems.CaptureDataPoint(
				CurrentOperationResult,
				Grabber_Pos.transform.position,
				Grabber_CrnrA.transform.position, Grabber_CrnrB.transform.position, Grabber_CrnrC.transform.position, Grabber_CrnrD.transform.position
			);
		}

		#region HELPERS -----------------------
		[ContextMenu("z call SendToTDG()")]
		public void SendToTDG()
		{
			int mode = 2;
			
			if( mode == 0 )
			{
				/*
				// Use the following for TDG_DoesEdgeObstructEdgePath....
				//Trans_PosParam.position = _TDG_DoesEdgeObstructEdgePath.Grabber_ObstructEdge.transform.position;
				Trans_PosParam.position = _TDG_DoesEdgeObstructEdgePath.CurrentObstructEdge.MidPosition;

				Trans_CrnrA.position = _TDG_DoesEdgeObstructEdgePath.CurrentStartEdge.StartPosition_flat;
				Trans_CrnrB.position = _TDG_DoesEdgeObstructEdgePath.CurrentStartEdge.EndPosition_flat;

				if 
				(
					LNX_Utils.AreEdgesAlignedFromTheirPerspectives
					(
						_TDG_DoesEdgeObstructEdgePath.CurrentStartEdge, _TDG_DoesEdgeObstructEdgePath.CurrentEndEdge
					)
				)
				{
					Trans_CrnrC.position = _TDG_DoesEdgeObstructEdgePath.CurrentEndEdge.EndPosition_flat;
					Trans_CrnrD.position = _TDG_DoesEdgeObstructEdgePath.CurrentEndEdge.StartPosition_flat;
				}
				else
				{
					Trans_CrnrC.position = _TDG_DoesEdgeObstructEdgePath.CurrentEndEdge.StartPosition_flat;
					Trans_CrnrD.position = _TDG_DoesEdgeObstructEdgePath.CurrentEndEdge.EndPosition_flat;
				}
				*/
			}
			else if( mode == 1) //TDG_GetTriPath
			{
				/*
				Trans_CrnrA.position = _TDG_getTriPath.CurrentResult.crnrA;
				Trans_CrnrB.position = _TDG_getTriPath.CurrentResult.crnrB;
				Trans_CrnrC.position = _TDG_getTriPath.CurrentResult.crnrC;
				Trans_CrnrD.position = _TDG_getTriPath.CurrentResult.crnrD;
				*/
			}
			else if( mode == 2 ) // TDG_DoesEdgeObstructTriPath()
			{
				string s = "";
				Grabber_CrnrA.transform.position = _tdg_doesEdgeObstructTriPath.TriA.Edges[0].StartPosition;
				Grabber_CrnrB.transform.position = _tdg_doesEdgeObstructTriPath.TriA.Edges[0].EndPosition;
				Grabber_CrnrC.transform.position = _tdg_doesEdgeObstructTriPath.TriB.Edges[1].EndPosition;
				Grabber_CrnrD.transform.position = _tdg_doesEdgeObstructTriPath.TriB.Edges[1].StartPosition;
				Grabber_Pos.transform.position = _tdg_doesEdgeObstructTriPath.ObstructEdge.MidPosition;
			}
			
		}
		#endregion

		protected override void OnDrawGizmos()
		{
			if
			(
				Selection.activeGameObject != gameObject &&
				Selection.activeGameObject != Grabber_Pos.gameObject &&
				Selection.activeGameObject != Grabber_CrnrA.gameObject &&
				Selection.activeGameObject != Grabber_CrnrB.gameObject &&
				Selection.activeGameObject != Grabber_CrnrC.gameObject &&
				Selection.activeGameObject != Grabber_CrnrD.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			DBG_Operation = "";
			DBG_Method = "";
			CurrentOperationResult = false;

			DBG_Operation += $"Commencing operation...\n";

			CurrentOperationResult = LNX_Utils.AmInArea(
				Grabber_Pos.transform.position,
				Grabber_CrnrA.transform.position, Grabber_CrnrB.transform.position,
				Grabber_CrnrC.transform.position, Grabber_CrnrD.transform.position, 
				_navmesh.V_SurfaceOrientation, false, ref DBG_Method );

			DBG_Operation += $"Result of AmInQuadArea(): '{CurrentOperationResult}'...\n";

			if( CurrentOperationResult )
			{
				Gizmos.color = Color.green;
				DBG_Operation += $"pos IS in quad area...\n";
			}
			else
			{
				Gizmos.color = Color.red;
				DBG_Operation += $"pos is NOT in quad area...\n";
			}

			Grabber_Pos.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_CrnrA.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_CrnrB.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_CrnrC.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_CrnrD.DrawMyGizmos(Radius_ObjectDebugSpheres);

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
