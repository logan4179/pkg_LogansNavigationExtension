using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class AngleFinder : MonoBehaviour
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

		#endregion

		private void OnDrawGizmos()
		{
			if
			(
				Selection.activeObject != gameObject &&
				Selection.activeGameObject != Grabber_start.gameObject &&
				Selection.activeGameObject != Grabber_corner.gameObject &&
				Selection.activeGameObject != Grabber_end.gameObject
			)
			{
				DBG_Class = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}


			Grabber_start.DrawMyGizmos(Size_Handles);
			Grabber_end.DrawMyGizmos(Size_Handles);
			Grabber_corner.DrawMyGizmos(Size_Handles);

			Gizmos.color = Color_angle;
			Gizmos.DrawLine(Grabber_start.transform.position, Grabber_corner.transform.position);
			Gizmos.DrawLine(Grabber_corner.transform.position, Grabber_end.transform.position);

			Gizmos.color = Color.aquamarine;
			Gizmos.DrawLine(Grabber_start.transform.position, Grabber_end.transform.position);

			Vector3 v_crnrToStart = Vector3.Normalize(Grabber_start.transform.position - Grabber_corner.transform.position);
			Vector3 v_crnrToEnd = Vector3.Normalize(Grabber_end.transform.position - Grabber_corner.transform.position);

			DBG_Class = $"Angle: '{Vector3.Angle(v_crnrToStart, v_crnrToEnd)}'\n" +
				$"";

		}
	}
}
