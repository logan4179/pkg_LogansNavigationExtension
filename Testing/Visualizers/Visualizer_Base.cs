using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class Visualizer_Base : MonoBehaviour
    {
		[TextArea(1, 5)] public string Description;

		[SerializeField] protected LNX_NavMesh _navmesh;

		[Header("DEBUG (BASE)")]
		[Range(0f, 0.3f)] public float Radius_ObjectDebugSpheres = 0.2f;

		[TextArea(1, 20)]
		public string DBG_Operation;

		public void DrawTriGizmo(LNX_Triangle tri, Color col)
		{
			Color oldColor = Gizmos.color;

			Gizmos.color = col;
			Handles.color = col;

			GUIStyle gstl_vertLines = new GUIStyle();
			gstl_vertLines.normal.textColor = col;

			//Draw borders...
			Handles.DrawLine(tri.Verts[0].V_Position, tri.Verts[1].V_Position);
			Handles.DrawLine(tri.Verts[1].V_Position, tri.Verts[2].V_Position);
			Handles.DrawLine(tri.Verts[2].V_Position, tri.Verts[0].V_Position);

			Gizmos.color = oldColor;
		}

		public void DrawStandardFocusTriGizmos(LNX_Triangle tri, float raiseAmount, string lblString, Color clr)
		{
			Color oldColor = Gizmos.color;

			Gizmos.color = clr;
			Vector3 vRaise = Vector3.up * raiseAmount;

			Gizmos.DrawLine(tri.Verts[0].V_Position, tri.V_Center + vRaise);
			Gizmos.DrawLine(tri.Verts[1].V_Position, tri.V_Center + vRaise);
			Gizmos.DrawLine(tri.Verts[2].V_Position, tri.V_Center + vRaise);

			Handles.Label(tri.V_Center + vRaise, lblString);

			DrawTriGizmo(tri, clr);

			Gizmos.color = oldColor;
		}

		public void DrawStandardEdgeFocusGizmos(LNX_Edge edge, float raiseAmount, string lblString, Color clr, bool incldStrtAndEndLbls = false )
		{
			Color oldColor = Gizmos.color;

			Gizmos.color = clr;
			Vector3 vRaise = Vector3.up * raiseAmount;

			Handles.Label(edge.MidPosition + vRaise, edge.ToString());

			Gizmos.DrawLine(edge.StartPosition, edge.StartPosition + vRaise);


			Gizmos.DrawLine(edge.StartPosition + vRaise, edge.EndPosition + vRaise);
			Gizmos.DrawLine(edge.EndPosition, edge.EndPosition + vRaise);

			if( incldStrtAndEndLbls )
			{
				Handles.Label(edge.StartPosition + vRaise, "eStrt");
				Handles.Label(edge.EndPosition + vRaise, "eEnd");
			}

			Gizmos.color = oldColor;
		}
	}
}
