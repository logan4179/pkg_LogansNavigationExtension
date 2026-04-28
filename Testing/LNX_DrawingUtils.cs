using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LogansNavigationExtension
{
    public class LNX_DrawingUtils
    {
		public static void DrawTriGizmos(LNX_Triangle tri, Color edgeColor, bool drawTriLbls, bool drwEdgs, bool drwEdgeLbls, 
			float edgeLblLineLength, bool drwVrtLbls, float vrtLblLineLength, bool drwNrmlLns, float nrmlLineLength
		)
		{
			/*
			GUIStyle gstl_label = GUIStyle.none;
			gstl_label.normal.textColor = amKosher ? Color.white : Color.red;
			*/
			if (drawTriLbls)
			{
				//Handles.Label(tri.V_Center, tri.Index_inCollection.ToString(), gstl_label);
				Handles.Label(tri.V_Center, tri.Index_inCollection.ToString() );

			}

			#region EDGES -------------------------------------------------------
			Gizmos.color = edgeColor;
			Handles.color = edgeColor;
			
			//Draw borders...
			if (drwEdgs)
			{
				Gizmos.DrawLine( tri.Edges[0].StartPosition, tri.Edges[0].EndPosition );
				Gizmos.DrawLine(tri.Edges[1].StartPosition, tri.Edges[1].EndPosition);
				Gizmos.DrawLine(tri.Edges[2].StartPosition, tri.Edges[2].EndPosition);
			}

			Gizmos.color = Color.white;

			if (drwEdgeLbls)
			{
				Vector3 pos = tri.Edges[0].MidPosition + (tri.Edges[0].v_Cross * edgeLblLineLength);
				Gizmos.DrawLine(tri.Edges[0].MidPosition, pos);
				//Handles.Label(pos, "e0", gstl_label);
				Handles.Label(pos, "e0");

				pos = tri.Edges[1].MidPosition + (tri.Edges[1].v_Cross * edgeLblLineLength);
				Gizmos.DrawLine(tri.Edges[1].MidPosition, pos);
				//Handles.Label(tri.Edges[1].MidPosition + (tri.Edges[1].v_Cross * edgeLblLineLength), "e1", gstl_label);
				Handles.Label(tri.Edges[1].MidPosition + (tri.Edges[1].v_Cross * edgeLblLineLength), "e1");

				pos = tri.Edges[2].MidPosition + (tri.Edges[2].v_Cross * edgeLblLineLength);
				Gizmos.DrawLine(tri.Edges[2].MidPosition, pos);
				//Handles.Label(tri.Edges[2].MidPosition + (tri.Edges[2].v_Cross * edgeLblLineLength), "e2", gstl_label);
				Handles.Label( tri.Edges[2].MidPosition + (tri.Edges[2].v_Cross * edgeLblLineLength), "e2" );

			}
			#endregion

			#region VERTS -----------------------------------------------
			if (drwVrtLbls)
			{
				Handles.Label(tri.Verts[0].V_Position + (tri.Verts[0].v_toCenter.normalized * vrtLblLineLength), "v0");
				Gizmos.DrawLine(tri.Verts[0].V_Position, 
					tri.Verts[0].V_Position + (tri.Verts[0].v_toCenter.normalized * vrtLblLineLength) 
				);

				Handles.Label(tri.Verts[1].V_Position + (tri.Verts[1].v_toCenter.normalized * vrtLblLineLength), "v1");
				Gizmos.DrawLine(tri.Verts[1].V_Position,
					tri.Verts[1].V_Position + (tri.Verts[1].v_toCenter.normalized * vrtLblLineLength)
				);

				Handles.Label(tri.Verts[2].V_Position + (tri.Verts[2].v_toCenter.normalized * vrtLblLineLength), "v2");
				Gizmos.DrawLine(tri.Verts[2].V_Position,
					tri.Verts[2].V_Position + (tri.Verts[2].v_toCenter.normalized * vrtLblLineLength)
				);
			}
			#endregion

			if (drwNrmlLns)
			{
				Gizmos.DrawLine(tri.V_Center, tri.V_Center + (tri.v_sampledNormal * nrmlLineLength));
				Handles.Label(tri.V_Center + (tri.v_sampledNormal * nrmlLineLength), $"N");
			}
		}

		public static void DrawTriGizmos(LNX_Triangle tri)
		{
			Gizmos.DrawLine(tri.Verts[0].V_Position, tri.Verts[1].V_Position);
			Gizmos.DrawLine(tri.Verts[1].V_Position, tri.Verts[2].V_Position);
			Gizmos.DrawLine(tri.Verts[2].V_Position, tri.Verts[0].V_Position);
		}

		public static void DrawTriGizmos(LNX_Triangle tri, Color trmnlEdgeClr)
		{
			Color startClr = Gizmos.color;

			if (tri.Edges[0].AmTerminal)
			{
				Gizmos.color = trmnlEdgeClr;
			}
			DrawEdgeGizmo(tri.Edges[0]);
			Gizmos.color = startClr;

			if (tri.Edges[1].AmTerminal)
			{
				Gizmos.color = trmnlEdgeClr;
			}
			DrawEdgeGizmo(tri.Edges[1]);
			Gizmos.color = startClr;

			if (tri.Edges[2].AmTerminal)
			{
				Gizmos.color = trmnlEdgeClr;
			}
			DrawEdgeGizmo(tri.Edges[2]);
		}

		public static void DrawEdgeGizmo(LNX_Edge edge)
		{
			Gizmos.DrawLine(edge.StartPosition, edge.EndPosition);
		}

		public static void DrawTriHandles(LNX_Triangle tri, float thickness)
		{
			Handles.DrawLine(tri.Verts[0].V_Position, tri.Verts[1].V_Position, thickness);
			Handles.DrawLine(tri.Verts[1].V_Position, tri.Verts[2].V_Position, thickness);
			Handles.DrawLine(tri.Verts[2].V_Position, tri.Verts[0].V_Position, thickness);
		}
	}
}
