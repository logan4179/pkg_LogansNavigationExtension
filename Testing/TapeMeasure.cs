using System.Collections.Generic;
using System.Threading.Tasks.Sources;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TapeMeasure : MonoBehaviour
    {
		//public List<LNX_ComponentGrabber> RulerGrabbers;
		public List<TapeMeasureNotch> Notches;
		public List<GameObject> DrawWhenSelectedObjects;

		[Header("SETTINGS")]
		public bool DrawAngles = true;

		[Header("DEBUG")]
		[Range(0f, 0.25f)] public float Size_Handles;
		[TextArea(1, 10)] public string DBG_Class;

		public Color Color_tape;
		public Color Color_notches;
		public Color Color_distMarkers;
		public Color Color_angleLabels;

		private void OnDrawGizmos()
		{
			int rulerObjectSelectionIndx = -1;
			bool foundValidSelectedObject = false;
			DBG_Class = "";
			if (Notches != null && Notches.Count > 0)
			{
				for (int i = 0; i < Notches.Count; i++)
				{
					if (Selection.activeObject == Notches[i].gameObject)
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

			if (Notches != null && Notches.Count > 1)
			{
				float totalDist = 0;
				float distToSelection = 0f;
				Notches[0].Dist_FromLast = 0f;
				Notches[0].Dist_SoFar = 0f;

				for (int i = 0; i < Notches.Count; i++)
				{
					Gizmos.color = Color_tape;

					Notches[i].DrawMyGizmos(i, Notches.Count, Size_Handles, Color_notches, 
						i > 0 ? Notches[i-1].transform.position : Notches[i].transform.position, 
						i < Notches.Count - 1 ? Notches[i+1].transform.position : Notches[i].transform.position, 
						totalDist, DrawAngles
					);
					totalDist += Notches[i].Dist_FromLast;

					if ( i < Notches.Count - 1 )
					{
						if( rulerObjectSelectionIndx > -1 && rulerObjectSelectionIndx < i )
						{
							distToSelection += Notches[i + 1].Dist_FromLast;
						}
					}


					/*
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
					*/
				}

				for (int i = 0; i < Notches.Count; i++)
				{
					Notches[i].Dist_EntireRuler = totalDist;
				}

					//if (RulerGrabbers.Count > 2)
					//{
					Handles.Label(Notches[Notches.Count-1].transform.position + Vector3.up * Size_Handles, totalDist.ToString("final:\n#.###"));
				//}

				DBG_Class += $"ruler dist: '{totalDist}'\n" +
					$"rulerObjectSelectionIndx: '{rulerObjectSelectionIndx}'\n" +
					$"";

				if ( distToSelection > 0 && rulerObjectSelectionIndx != Notches.Count-1 )
				{
					Handles.Label(
						Notches[rulerObjectSelectionIndx].transform.position + Vector3.up * 0.5f, 
						distToSelection.ToString("#.###")
					);
					DBG_Class += $"distToSelection: '{distToSelection}'\n";
				}
			}
		}
	}
}
