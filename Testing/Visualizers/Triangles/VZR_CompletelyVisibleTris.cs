using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class VZR_CompletelyVisibleTris : Visualizer_Base
    {
		public int Index_FocusTri = 0;
		public LNX_Triangle FocusTri => _navmesh.Triangles[Index_FocusTri];

		[TextArea(1,15)] public string DBG_Method;

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

		private void OnDrawGizmos()
		{
			DBG_Operation = "";

			if ( Selection.activeObject != gameObject )
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			if( lastPos != transform.position )
			{
				CaptureComponents();
				lastPos = transform.position;
			}

			DrawStandardFocusTriGizmos( FocusTri, 0.5f, Index_FocusTri.ToString(), Color.magenta );

			Gizmos.DrawSphere(transform.position, Radius_ObjectDebugSpheres);

			DBG_Operation += $"Focused on tri: '{Index_FocusTri}'\n" +
				$"visible tris: '{FocusTri.KnownFullyVisibleTriangleIndices.Length}'\n" +
				$"";

			DBG_Method = FocusTri.DBG_FullyVisible;

			for ( int i = 0; i < FocusTri.KnownFullyVisibleTriangleIndices.Length; i++ )
			{
				DrawStandardFocusTriGizmos(_navmesh.Triangles[FocusTri.KnownFullyVisibleTriangleIndices[i]], 
					0.25f, _navmesh.Triangles[FocusTri.KnownFullyVisibleTriangleIndices[i]].ToString(), Color.green);
			}

			LNX_Edge[] trmnlEdges = _navmesh.GetTerminalEdges(false);
			for ( int i_trmnlEdge = 0; i_trmnlEdge < trmnlEdges.Length; i_trmnlEdge++ )
			{
				DrawStandardEdgeFocusGizmos( trmnlEdges[i_trmnlEdge], 0.5f, trmnlEdges[i_trmnlEdge].ToString(), Color.blue );
			}
		}

		[ContextMenu("z call CalculateVisibility()")]
		public void CalculateVisibility()
		{
			Debug.Log($"{nameof(CalculateVisibility)}()...");

			//_navmesh.CalculateVisibility();


			FocusTri.CalculateCompletelyVisibleTris( _navmesh, _navmesh.GetTerminalEdges(false) );
		}
	}
}
