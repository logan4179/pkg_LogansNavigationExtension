using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
	public class TDG_AmInVectorCone_edgeOverload : TDG_base
	{
		public ComponentGrabber Grabber_edge;
		public LNX_Edge EdgeParameter => Grabber_edge.CurrentlyGrabbedEdge;
		public LNX_Triangle EdgeParamTri => _navmesh.Triangles[EdgeParameter.TriangleIndex];

		public ComponentGrabber Grabber_PerspectiveVert;
		public LNX_Vertex PerspectiveVert => Grabber_PerspectiveVert.CurrentlyGrabbedVert;
		public LNX_Triangle PerspectiveTri => _navmesh.Triangles[PerspectiveVert.TriangleIndex];

		[Header("RESULTS")]
		public bool CurrentOperationResult;


		[Header("DEBUG")]
		public string DBG_Method;
		public Color Color_Corners;
		public Vector3 v_lblOffset;

		protected override void OnDrawGizmos()
		{
			if
			(
				Selection.activeGameObject != gameObject &&
				Selection.activeGameObject != Grabber_edge.gameObject &&
				Selection.activeGameObject != Grabber_PerspectiveVert.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			DBG_Operation = "";
			DBG_Method = "";
			CurrentOperationResult = false;

			//Gizmos.DrawSphere(Grabber_PerspectiveVert.transform.position, Radius_ObjectDebugSpheres);
			Grabber_PerspectiveVert.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Handles.Label(Grabber_PerspectiveVert.transform.position + v_lblOffset, "PrspctvVert");

			//Gizmos.DrawSphere( Grabber_edge.transform.position, Radius_ObjectDebugSpheres );
			Grabber_edge.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Handles.Label(Grabber_edge.transform.position + v_lblOffset, "EdgeParam");



			if( EdgeParameter == null )
			{
				DBG_Operation += $"ERROR! EdgeParameter is null...\n";
				return;
			}

			if (PerspectiveVert == null)
			{
				DBG_Operation += $"ERROR! PerspectiveVert is null...\n";
				return;
			}

			Gizmos.color = Color_Corners;
			Gizmos.DrawSphere(PerspectiveVert.V_Position, Radius_ObjectDebugSpheres);

			DBG_Operation += $"using...\n" +
				$"edge: '{EdgeParameter}', vrt: '{PerspectiveVert}'...\n";

			DBG_Operation += $"Commencing operation...\n";

			CurrentOperationResult = LNX_Utils.AmInVertexCone(
				EdgeParameter, 
				//PerspectiveVert.V_ToFirstSiblingVert, PerspectiveVert.V_ToSecondSiblingVert,
				PerspectiveVert,
				Vector3.up, ref DBG_Method,
				true
			);

			DBG_Operation += $"Result of AmInVectorCone(): '{CurrentOperationResult}'...\n";

			if (CurrentOperationResult)
			{
				Gizmos.color = Color.green;
				DBG_Operation += $"pos IS in Vector cone...\n";

				DrawStandardEdgeFocusGizmos(EdgeParameter, 0.1f, EdgeParameter.ToString(), Color.green);

			}
			else
			{
				Gizmos.color = Color.red;
				DrawStandardEdgeFocusGizmos(EdgeParameter, 0.1f, EdgeParameter.ToString(), Color.red);

				DBG_Operation += $"pos is NOT in vector cone...\n";
			}

			DrawTriGizmo(PerspectiveTri, Color.magenta);

		}
	}
}
