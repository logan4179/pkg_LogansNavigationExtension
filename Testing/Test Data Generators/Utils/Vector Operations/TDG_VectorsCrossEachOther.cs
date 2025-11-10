using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_VectorsCrossEachOther : TDG_base
    {
		public LNX_ComponentGrabber Grabber_LineA_start;
		public LNX_ComponentGrabber Grabber_LineA_end;
		public LNX_ComponentGrabber Grabber_LineB_start;
		public LNX_ComponentGrabber Grabber_LineB_end;

		[Header("RESULTS")]
		public bool Result;


		[Header("SEND TO")]
		public TDG_DoesEdgeObstructArea _tdg_doesEdgeObstructArea;

		[Header("DEBUG")]
		public Color Clr_edges = Color.white;

		public string DBG_Method;

		#region HELPERS =================================================

		[ContextMenu("z call SendToTDG")]
		public void SendToTDG()
		{
			Grabber_LineA_start.transform.position = _tdg_doesEdgeObstructArea.ObstructEdge.StartPosition;
			Grabber_LineA_end.transform.position = _tdg_doesEdgeObstructArea.ObstructEdge.EndPosition;

			Grabber_LineB_start.transform.position = _tdg_doesEdgeObstructArea.DestinationTriangle.Verts[0].V_Position;
			Grabber_LineB_end.transform.position = _tdg_doesEdgeObstructArea.DestinationTriangle.Verts[1].V_Position;

		}
		#endregion

		protected override void OnDrawGizmos()
		{
			if
			(
				Selection.activeGameObject != gameObject &&
				Selection.activeGameObject != Grabber_LineA_start.gameObject &&
				Selection.activeGameObject != Grabber_LineA_end.gameObject &&
				Selection.activeGameObject != Grabber_LineB_start.gameObject &&
				Selection.activeGameObject != Grabber_LineB_end.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			DBG_Operation = "";
			DBG_Method = "";

			base.OnDrawGizmos();

			Grabber_LineA_start.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_LineA_end.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_LineB_start.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_LineB_end.DrawMyGizmos(Radius_ObjectDebugSpheres);

			DBG_Operation += $"Commencing operation...\n";

			Result = LNX_Utils.VectorsCrossEachOther
			(
				Grabber_LineA_start.transform.position,
				Grabber_LineA_end.transform.position,
				Grabber_LineB_start.transform.position,
				Grabber_LineB_end.transform.position, 
				Vector3.up, false
			);

			DBG_Operation += $"result: '{Result}'\n";

			if( Result )
			{
				Gizmos.color = Color.green;
			}
			else
			{
				Gizmos.color = Color.red;
			}

			Gizmos.DrawLine(Grabber_LineA_start.transform.position, Grabber_LineA_end.transform.position);
			Gizmos.DrawLine(Grabber_LineB_start.transform.position, Grabber_LineB_end.transform.position);

		}
	}
}
