using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TapeMeasure : MonoBehaviour
    {
		public List<LNX_ComponentGrabber> RulerGrabbers;

		[Header("DEBUG")]
		[Range(0f, 0.25f)] public float Size_Handles;
		[TextArea(1, 10)] public string DBG_Class;

		public Color Color_tape;
		public Color Color_distMarkers;

		private void OnDrawGizmos()
		{
			int rulerObjectSelectionIndx = -1;
			DBG_Class = "";
			if (RulerGrabbers != null && RulerGrabbers.Count > 0)
			{
				for (int i = 0; i < RulerGrabbers.Count; i++)
				{
					if (Selection.activeObject == RulerGrabbers[i].gameObject)
					{
						rulerObjectSelectionIndx = i;
					}
				}
			}

			if( Selection.activeGameObject != gameObject && rulerObjectSelectionIndx <= -1 )
			{
				return;
			}

			if (RulerGrabbers != null && RulerGrabbers.Count > 1)
			{
				float totalDist = 0;
				float distToSelection = 0f;
				for (int i = 0; i < RulerGrabbers.Count - 1; i++)
				{
					Gizmos.color = Color_tape;

					float dist = Vector3.Distance(RulerGrabbers[i].transform.position, RulerGrabbers[i + 1].transform.position);
					totalDist += dist;
					Gizmos.DrawLine(RulerGrabbers[i].transform.position, RulerGrabbers[i + 1].transform.position);

					Gizmos.color = Color_distMarkers;
					Vector3 midPt = (RulerGrabbers[i].transform.position + RulerGrabbers[i + 1].transform.position) / 2f;
					float raiseIt = 0.2f;
					Gizmos.DrawLine( midPt, midPt + Vector3.up * raiseIt );
					Handles.Label(midPt + Vector3.up * raiseIt, dist.ToString("#.###") );

					if( rulerObjectSelectionIndx > -1 && rulerObjectSelectionIndx < i )
					{
						distToSelection += dist;
					}
				}

				Handles.Label(RulerGrabbers[RulerGrabbers.Count-1].transform.position + Vector3.up * 0.5f, totalDist.ToString("#.###"));

				DBG_Class += $"ruler dist: '{totalDist}'\n" +
					$"rulerObjectSelectionIndx: '{rulerObjectSelectionIndx}'\n" +
					$"";

				if ( distToSelection > 0 && rulerObjectSelectionIndx != RulerGrabbers.Count-1 )
				{
					Handles.Label( 
						RulerGrabbers[rulerObjectSelectionIndx].transform.position + Vector3.up * 0.5f, 
						distToSelection.ToString("#.###")
					);
					DBG_Class += $"distToSelection: '{distToSelection}'\n";
				}
			}
		}
	}
}
