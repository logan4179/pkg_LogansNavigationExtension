using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class MeasuringStick : MonoBehaviour
    {
		public LNX_NavMesh _NavMesh;

		public LNX_ComponentGrabber Grabber_start;
		public LNX_ComponentGrabber Grabber_end;
		public LNX_ComponentGrabber Grabber_corner;

		[Header("SEND TO")]
		public LNX_ComponentCoordinate Coord_SendStart_toVert;
		public LNX_ComponentCoordinate Coord_SendEnd_toVert;
		public int Index_SendStart_ToTriCtr;
		public int Index_SendEnd_ToTriCtr;

		[Header("DEBUG")]
		[Range(0f, 0.25f)] public float Size_Handles;
        [TextArea(1,10)] public string DBG_Class;

		public Color Color_angle;

		#region HELPERS -------------------------
		[ContextMenu("z call SendComponentsTo()")]
		public void SendComponentsTo()
		{
			//trans_start.position = _NavMesh.Triangles[Index_SendStart_ToTriCtr].V_Center;
			//trans_end.position = _NavMesh.GetVertexAtCoordinate(Coord_SendEnd_toVert).V_Position;
		}
		#endregion

		private void OnDrawGizmos()
		{
			/*
			if
			(
				Selection.activeObject != gameObject &&
				Selection.activeGameObject != trans_start.gameObject &&
				Selection.activeGameObject != trans_end.gameObject
			)
			{
				DBG_Class = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}
			*/

			Grabber_start.DrawMyGizmos(Size_Handles);
			Grabber_end.DrawMyGizmos(Size_Handles);
			Grabber_corner.DrawMyGizmos(Size_Handles);

			Vector3 v_crnrToStart = Vector3.Normalize(Grabber_start.transform.position - Grabber_corner.transform.position);
			Vector3 v_crnrToEnd = Vector3.Normalize(Grabber_end.transform.position - Grabber_corner.transform.position);

			DBG_Class = $"Dist: '{Vector3.Distance(Grabber_start.transform.position, Grabber_end.transform.position)}'\n" +
				$"Angle: '{Vector3.Angle(v_crnrToStart, v_crnrToEnd)}'" +
				$"";
		}
	}
}
