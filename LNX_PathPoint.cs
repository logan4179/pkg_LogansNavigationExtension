using System;
using UnityEngine;

namespace LogansNavigationExtension
{
	[Serializable]
	public struct LNX_PathPoint
	{
		public Vector3 V_Point;

		public Vector3 V_toNext;

		/// <summary>
		/// Normalized vector pointing to the previous pathpoint.
		/// </summary>
		public Vector3 V_toPrev;

		/// <summary>
		/// Normalized vector describing the 'up' direction a crawling enemy would need to know based on the terrain 'relatively under' this point.
		/// </summary>
		public Vector3 V_normal;

		/// <summary>The distance to the next patrol point.</summary>
		public float Dist_toNext;

		public float Dist_toPrev;

		public bool Flag_amCorner;
		public bool flag_switchGravityOff; //todo: I don't think we need both of these flags...
		public bool flag_switchGravityOn; //todo: I don't think we need both of these flags...

		[TextArea(2, 10)] public string DebugClass;

		public LNX_PathPoint( Vector3 pt, Vector3 nrml )
		{
			V_Point = pt;
			V_normal = nrml;

			V_toPrev = Vector3.zero;
			V_toNext = Vector3.zero;
			Dist_toPrev = -1;
			Dist_toNext = -1;

			flag_switchGravityOn = false;
			flag_switchGravityOff = false;
			Flag_amCorner = false;

			DebugClass = string.Empty;
		}

		public LNX_PathPoint(Vector3 prevPos, Vector3 pt, Vector3 nextPos, Vector3 nrml)
		{
			V_Point = pt;
			V_normal = nrml;

			V_toPrev = Vector3.zero;
			V_toNext = Vector3.zero;
			Dist_toPrev = -1;
			Dist_toNext = -1;

			if ( prevPos != pt )
			{
				V_toPrev = Vector3.Normalize( prevPos - pt );
				Dist_toPrev = Vector3.Distance( prevPos, pt );
			}
			if ( nextPos != pt )
			{
				V_toNext = Vector3.Normalize(nextPos - pt);
				Dist_toNext = Vector3.Distance( pt, nextPos );
			}

			flag_switchGravityOn = false;
			flag_switchGravityOff = false;
			Flag_amCorner = false;

			DebugClass = string.Empty;
		}

		public void DetermineGravityRequirement(float slopeHeight_switchGravity)
		{
			if (Mathf.Abs(V_toNext.y) >= slopeHeight_switchGravity)
			{
				flag_switchGravityOff = true;
			}
			else if (Mathf.Abs(V_toPrev.y) >= slopeHeight_switchGravity && Mathf.Abs(V_toNext.y) < slopeHeight_switchGravity)
			{
				flag_switchGravityOn = true;
			}
		}
	}
}