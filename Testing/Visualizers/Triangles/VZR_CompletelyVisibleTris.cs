using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LogansNavigationExtension
{
    public class VZR_CompletelyVisibleTris : TDG_base
    {
		public int Index_FocusTri = 0;
		public LNX_Triangle FocusTri => _navmesh.Triangles[Index_FocusTri];

		[Header("DEBUG")]
		[TextArea(1,20)] public string DBG_Method;
		public Color Color_visibleTris;
		public Color Color_obstructEdge;

		[Header("RESULTS")]
		public int[] Indices_knownFullyVisibleTriangles;

		[ContextMenu("z call CaptureComponents()")]
		public void CaptureComponents()
		{
			Debug.Log($"{nameof(CaptureComponents)}()...");

			LNX_ProjectionHit hit = LNX_ProjectionHit.None;

			if ( _navmesh.SamplePosition(transform.position, out hit, 2f, false) )
			{
				Index_FocusTri = hit.Index_Hit;
				Debug.Log($"Succesful sample! Set new triangle to: '{hit.Index_Hit}'");
			}
			else
			{
				Debug.LogWarning($"sample unsuccesful...");
			}
		}

		[HideInInspector] public Vector3 lastPos = Vector3.zero;

		protected override void OnDrawGizmos()
		{
			DBG_Operation = "";

			if ( Selection.activeObject != gameObject )
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			if( lastPos != transform.position || FocusTri == null )
			{
				CaptureComponents();
				lastPos = transform.position;
			}

			DrawStandardFocusTriGizmos( FocusTri, 0.15f, Index_FocusTri.ToString(), Color.magenta, true, 0.05f, false );

			Gizmos.DrawSphere(transform.position, Radius_ObjectDebugSpheres);

			DBG_Operation += $"Focused on tri: '{Index_FocusTri}'\n";
			
			if( Indices_knownFullyVisibleTriangles == null )
			{
				DBG_Operation += $"fully visible tri list is currently null...\n";
			}
			else if(Indices_knownFullyVisibleTriangles.Length <= 0 )
			{
				DBG_Operation += $"known fully visible tris list length: '{Indices_knownFullyVisibleTriangles.Length}'. Returning early...\n";
			}
			else
			{
				DBG_Operation += $"known fully visible tris list length: '{Indices_knownFullyVisibleTriangles.Length}'...\n";
			}

			DBG_Method = FocusTri.DBG_FullyVisible;

			Color oldClr = Gizmos.color;
			Gizmos.color = Color_visibleTris;
			for ( int i = 0; i < Indices_knownFullyVisibleTriangles.Length; i++ )
			{
				//DrawStandardFocusTriGizmos(_navmesh.Triangles[FocusTri.KnownFullyVisibleTriangleIndices[i]], 
				//0.10f, _navmesh.Triangles[FocusTri.KnownFullyVisibleTriangleIndices[i]].ToString(), Color_visibleTris );
				DrawTriGizmo(_navmesh.Triangles[Indices_knownFullyVisibleTriangles[i]], Color_visibleTris, 0.015f);
				Gizmos.DrawLine
				(
					_navmesh.Triangles[Indices_knownFullyVisibleTriangles[i]].V_Center,
					_navmesh.Triangles[Indices_knownFullyVisibleTriangles[i]].V_Center + (Vector3.up * 0.25f)
				);
			}
			Gizmos.color = oldClr;

			LNX_Edge[] trmnlEdges = _navmesh.GetTerminalEdges(false);
			for ( int i_trmnlEdge = 0; i_trmnlEdge < trmnlEdges.Length; i_trmnlEdge++ )
			{
				DrawStandardEdgeFocusGizmos( trmnlEdges[i_trmnlEdge], 0.5f, trmnlEdges[i_trmnlEdge].ToString(), Color_obstructEdge );
			}
		}

		[ContextMenu("z call CalculateVisibility()")]
		public void CalculateVisibility()
		{
			Debug.Log($"{nameof(CalculateVisibility)}()...");

			//_navmesh.CalculateVisibility();


			FocusTri.CalculateCompletelyVisibleTris( _navmesh, _navmesh.GetTerminalEdges(false) );

			Indices_knownFullyVisibleTriangles = FocusTri.KnownFullyVisibleTriangleIndices;
		}
	}
}
