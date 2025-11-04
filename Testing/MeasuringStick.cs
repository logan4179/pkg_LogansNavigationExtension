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
        [SerializeField] private Transform trans_start;
		public bool SnapStartToVerts = false;
		public bool SnapStartToTriCtrs = false;

        [SerializeField] private Transform trans_end;
		public bool SnapEndToVerts = false;
		public bool SnapEndToTriCtrs = false;

		[SerializeField] private Transform trans_angle;
		public bool SnapAngleToVerts = false;
		public bool SnapAngleToTriCtrs = false;

		[Header("SEND TO")]
		public LNX_ComponentCoordinate Coord_SendStart_toVert;
		public LNX_ComponentCoordinate Coord_SendEnd_toVert;
		public int Index_SendStart_ToTriCtr;
		public int Index_SendEnd_ToTriCtr;

		[Header("DEBUG")]
		public Color Color_handles;
		[Range(0f, 0.25f)] public float Size_Handles;
        [TextArea(1,10)] public string DBG_Class;

		public Color Color_angle;

		#region HELPERS -------------------------
		[ContextMenu("z call SendComponentsTo()")]
		public void SendComponentsTo()
		{
			trans_start.position = _NavMesh.Triangles[Index_SendStart_ToTriCtr].V_Center;
			trans_end.position = _NavMesh.GetVertexAtCoordinate(Coord_SendEnd_toVert).V_Position;
		}
		#endregion

		[HideInInspector, SerializeField] private Vector3 v_lstStrtPos = Vector3.zero;
		[HideInInspector, SerializeField] private Vector3 v_lstEndPos = Vector3.zero;
		[HideInInspector, SerializeField] private Vector3 v_lstAnglePos = Vector3.zero;

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

			#region SNAPPING ---------------------------------------
			if ( trans_start.position != v_lstEndPos )
			{
				if( SnapStartToVerts )
				{
					LNX_Vertex vrt = _NavMesh.GetClosestVertexToPosition(trans_start.position );
					if( Vector3.Distance(trans_start.position, vrt.V_Position) < 0.1f )
					{
						trans_start.position = vrt.V_Position;
					}
				}

				if ( SnapStartToTriCtrs )
				{
					LNX_Triangle tri = _NavMesh.GetClosestTriangleToPosition(trans_start.position);
					if (Vector3.Distance(trans_start.position, tri.V_Center) < 0.1f)
					{
						trans_start.position = tri.V_Center;
					}
				}
			}

			if (trans_end.position != v_lstEndPos)
			{
				if ( SnapEndToVerts )
				{
					Debug.Log("asdf");
					LNX_Vertex vert = _NavMesh.GetClosestVertexToPosition(trans_end.position);
					if (Vector3.Distance(trans_end.position, vert.V_Position) < 0.1f)
					{
						trans_end.position = vert.V_Position;
					}
				}

				if (SnapEndToTriCtrs)
				{
					LNX_Triangle tri = _NavMesh.GetClosestTriangleToPosition(trans_end.position);
					if (Vector3.Distance(trans_end.position, tri.V_Center) < 0.1f)
					{
						trans_end.position = tri.V_Center;
					}
				}
			}

			if ( trans_angle.position != v_lstAnglePos )
			{
				if (SnapAngleToVerts)
				{
					Debug.Log("asdf");
					LNX_Vertex vert = _NavMesh.GetClosestVertexToPosition(trans_angle.position);
					if (Vector3.Distance(trans_angle.position, vert.V_Position) < 0.1f)
					{
						trans_angle.position = vert.V_Position;
					}
				}

				if (SnapAngleToTriCtrs)
				{
					LNX_Triangle tri = _NavMesh.GetClosestTriangleToPosition(trans_angle.position);
					if (Vector3.Distance(trans_angle.position, tri.V_Center) < 0.1f)
					{
						trans_angle.position = tri.V_Center;
					}
				}
			}
			#endregion

			Gizmos.DrawSphere(trans_start.position, Size_Handles);
			Handles.Label(trans_start.position + (Vector3.up * 0.15f), "start");

			Gizmos.DrawSphere(trans_end.position, Size_Handles);
			Handles.Label(trans_end.position + (Vector3.up * 0.15f), "end");

			Gizmos.DrawLine( trans_start.position, trans_end.position );

			Gizmos.color = Color_angle;
			Handles.color = Color_angle;
			Gizmos.DrawSphere(trans_angle.position, Size_Handles * 0.7f);
			Handles.Label(trans_angle.position + (Vector3.up * 0.15f), "angle");
			Gizmos.DrawLine(trans_start.position, trans_angle.position);

			DBG_Class = $"Dist: '{Vector3.Distance(trans_start.position, trans_end.position)}'\n" +
				$"";

			v_lstStrtPos = trans_start.position;
			v_lstEndPos = trans_end.position;
			v_lstAnglePos = trans_angle.position;
		}
	}
}
