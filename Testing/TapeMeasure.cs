using System.Collections.Generic;
using System.Threading.Tasks.Sources;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TapeMeasure : MonoBehaviour
    {
		public List<LNX_ComponentGrabber> RulerGrabbers;
		public List<GameObject> DrawWhenSelectedObjects;

		[Header("SETTINGS")]
		public bool DrawAngles = true;

		[Header("DEBUG")]
		[Range(0f, 0.25f)] public float Size_Handles;
		[TextArea(1, 10)] public string DBG_Class;

		public Color Color_tape;
		public Color Color_distMarkers;
		public Color Color_angleLabels;

		private void OnDrawGizmos()
		{
			int rulerObjectSelectionIndx = -1;
			bool foundValidSelectedObject = false;
			DBG_Class = "";
			if (RulerGrabbers != null && RulerGrabbers.Count > 0)
			{
				for (int i = 0; i < RulerGrabbers.Count; i++)
				{
					if (Selection.activeObject == RulerGrabbers[i].gameObject)
					{
						rulerObjectSelectionIndx = i;
						foundValidSelectedObject = true;
					}
				}
			}

			if ( DrawWhenSelectedObjects != null && DrawWhenSelectedObjects.Count > 0 )
			{
				foreach (GameObject go in DrawWhenSelectedObjects)
				{
					if(Selection.activeGameObject == go)
					{
						foundValidSelectedObject = true;
						break;
					}
				}
			}

			if ( Selection.activeGameObject != gameObject && !foundValidSelectedObject )
			{
				return;
			}

			if (RulerGrabbers != null && RulerGrabbers.Count > 1)
			{
				float totalDist = 0;
				float distToSelection = 0f;
				for (int i = 0; i < RulerGrabbers.Count; i++)
				{
					Gizmos.color = Color_tape;

					if( i < RulerGrabbers.Count - 1 )
					{
						float dist = Vector3.Distance(RulerGrabbers[i].transform.position, RulerGrabbers[i + 1].transform.position);
						totalDist += dist;
						Gizmos.DrawLine(RulerGrabbers[i].transform.position, RulerGrabbers[i + 1].transform.position);
					
						Vector3 midPt = (RulerGrabbers[i].transform.position + RulerGrabbers[i + 1].transform.position) / 2f;
						LNX_DrawingUtils.DrawLabeledPoint(
							midPt, midPt + (Vector3.up * Size_Handles) + (Vector3.right * 0.01f),
							dist.ToString("#.##"), Color_distMarkers

						);

						if( rulerObjectSelectionIndx > -1 && rulerObjectSelectionIndx < i )
						{
							distToSelection += dist;
						}
					}
					
					if( DrawAngles && i > 0 )
					{
						Gizmos.color = Color_angleLabels;
						Vector3 vToPrev = Vector3.Normalize( 
							RulerGrabbers[i - 1].transform.position - RulerGrabbers[i].transform.position
						);

						Vector3 vToNext = Vector3.zero;

						if ( i < RulerGrabbers.Count - 1 )
						{
							vToNext = Vector3.Normalize(
								RulerGrabbers[i + 1].transform.position - RulerGrabbers[i].transform.position
							);
						}
						else if( RulerGrabbers[0].transform.position == RulerGrabbers[i].transform.position )
						{
							vToNext = Vector3.Normalize(
								RulerGrabbers[1].transform.position - RulerGrabbers[0].transform.position
							);
						}
						
						Vector3 vLblPos = Vector3.Normalize((vToPrev + vToNext) / 2f);
						if( vLblPos ==  Vector3.zero )
						{
							vLblPos = Vector3.up * 0.01f;
						}

						LNX_DrawingUtils.DrawLabeledPoint(RulerGrabbers[i].transform.position,
							RulerGrabbers[i].transform.position + (vLblPos * Size_Handles * 0.9f),
							$"ang\n'{Vector3.Angle(vToPrev, vToNext).ToString("#.##")}'", Color_angleLabels
						);
					}
				}

				//if (RulerGrabbers.Count > 2)
				//{
					Handles.Label(RulerGrabbers[RulerGrabbers.Count-1].transform.position + Vector3.up * Size_Handles, totalDist.ToString("final:\n#.###"));
				//}

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
