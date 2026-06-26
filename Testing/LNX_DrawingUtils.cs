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
				Handles.Label(tri.V_Center + (tri.v_sampledNormal * nrmlLineLength), $"SmpldNrm");

				Gizmos.DrawLine(tri.V_Center, tri.V_Center + (tri.V_PathingNormal * nrmlLineLength));
				Handles.Label(tri.V_Center + (tri.V_PathingNormal * nrmlLineLength), $"PN");
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

		public static void DrawStandardFocusTriGizmos(LNX_Triangle tri, float pyramidRaiseAmount, string lblString, Color clr, 
			bool drawTriGizmo = false, float triGzmoRaiseAmt = 0f, bool lblAll = false, bool drawToCtrLines = true)
		{
			Color oldColor = Gizmos.color;

			Gizmos.color = clr;
			Vector3 vRaise = Vector3.up * pyramidRaiseAmount;

			if (drawToCtrLines)
			{
				Gizmos.DrawLine(tri.Verts[0].V_Position, tri.V_Center + vRaise);
				Gizmos.DrawLine(tri.Verts[1].V_Position, tri.V_Center + vRaise);
				Gizmos.DrawLine(tri.Verts[2].V_Position, tri.V_Center + vRaise);
			}

			Handles.Label(tri.V_Center + vRaise, lblString);

			if (drawTriGizmo)
			{
				DrawTriGizmos( tri );
			}

			if (lblAll)
			{
				for (int i = 0; i < 3; i++)
				{
					Handles.Label(tri.Verts[i].V_Position + (tri.Verts[i].v_toCenter * 0.1f), $"v{i}");
				}

				for (int i = 0; i < 3; i++)
				{
					Handles.Label(tri.Edges[i].MidPosition + (tri.Edges[i].v_toCenter * 0.15f), $"e{i}");
				}
			}

			Gizmos.color = oldColor;
		}

		public void DrawStandardEdgeFocusGizmos(LNX_Edge edge, float raiseAmount, string lblString, Color clr, 
			bool drawMidPt = false)
		{
			Color oldColor = Gizmos.color;

			Gizmos.color = clr;
			Vector3 vRaise = Vector3.up * raiseAmount;

			Handles.Label(edge.MidPosition + (vRaise * 1.3f), lblString);

			Gizmos.DrawLine(edge.StartPosition, edge.StartPosition + vRaise);
			Handles.Label(edge.StartPosition + vRaise, "eStrt");

			Gizmos.DrawLine(edge.StartPosition + vRaise, edge.EndPosition + vRaise);
			Gizmos.DrawLine(edge.EndPosition, edge.EndPosition + vRaise);
			Handles.Label(edge.EndPosition + vRaise, "eEnd");

			if (drawMidPt)
			{
				Gizmos.DrawLine(edge.MidPosition + vRaise, edge.MidPosition + vRaise);
				Gizmos.DrawLine(edge.MidPosition, edge.MidPosition + vRaise);
				Handles.Label(edge.MidPosition + vRaise, "Mid");
			}

			Gizmos.color = oldColor;
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

		public static void DrawLabeledPoint(Vector3 pointPos, Vector3 lineEnd, string lbl, Color clr)
		{
			Color oldClr = Gizmos.color;

			Gizmos.color = clr;

			Gizmos.DrawLine(pointPos, lineEnd);
			Handles.Label(lineEnd, lbl);
		}
	}
}
