using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TapeMeasureNotch : MonoBehaviour
    {

        public float Dist_SoFar;
		[Space(5f)]
        public float Dist_FromLast;
		[Space(5f)]

		public float Dist_EntireRuler;

		[Header("ANGLE")]
		public float AngleAtBend;

		public void DrawMyGizmos(int indx, int notchCount, float handleSize, 
			Color clr, Vector3 vPrev, Vector3 vNext, float dstSoFr, bool drwAngls )
        {
			Dist_FromLast = Vector3.Distance(vPrev, transform.position);
			Dist_SoFar = dstSoFr + Dist_FromLast;

			Gizmos.DrawLine( transform.position, transform.position + (Vector3.up * 0.25f) );
			LNX_DrawingUtils.DrawLabeledPoint(
	                transform.position, transform.position + (Vector3.up * 0.25f),
	                indx.ToString(), clr
            );

			if ( indx > 0 )
			{
				Gizmos.DrawLine(vPrev, transform.position);
				Vector3 midPt = (vPrev + transform.position) / 2f;
				LNX_DrawingUtils.DrawLabeledPoint(
					midPt, midPt + (Vector3.up * handleSize) + (Vector3.right * 0.01f),
					Dist_FromLast.ToString("#.##"), clr
				);
			}

			if ( drwAngls && indx > 0 )
			{
				Vector3 vToPrev = Vector3.Normalize(vPrev - transform.position);

				Vector3 vToNext = Vector3.zero;

				if (indx < (notchCount - 1) )
				{
					vToNext = Vector3.Normalize(
						vNext - transform.position
					);
				}
				/*else if ( RulerGrabbers[0].transform.position == RulerGrabbers[i].transform.position)
				{
					vToNext = Vector3.Normalize(
						RulerGrabbers[1].transform.position - RulerGrabbers[0].transform.position
					);
				}*/

				Vector3 vLblPos = Vector3.Normalize((vToPrev + vToNext) / 2f);
				if (vLblPos == Vector3.zero)
				{
					vLblPos = Vector3.up * 0.01f;
				}

				LNX_DrawingUtils.DrawLabeledPoint(transform.position,
					transform.position + (vLblPos * handleSize * 0.9f),
					$"ang\n'{Vector3.Angle(vToPrev, vToNext).ToString("#.##")}'", clr
				);
			}

		}
	}
}
