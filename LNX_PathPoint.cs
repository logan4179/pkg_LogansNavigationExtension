using System;
using UnityEngine;

namespace LogansNavigationExtension
{
	[Serializable]
	public struct LNX_PathPoint
	{
		public Vector3 V_Position;

		/// <summary>
		/// Normalized vector describing the 'up' direction a crawling enemy would need to know based on the terrain 'relatively under' this point.
		/// </summary>
		public Vector3 V_normal;

		public Vector3 V_PreviousPoint;
		public Vector3 V_NextPoint;

		/// <summary>
		/// Vector pointing to the next pathpoint.
		/// </summary>
		public Vector3 V_ToNext => V_NextPoint - V_Position;

		/// <summary>
		/// Vector pointing to the previous pathpoint.
		/// </summary>
		public Vector3 V_ToPrev => V_PreviousPoint - V_Position;

		/// <summary>The distance to the next path point.</summary>
		public float Dist_toNext => Vector3.Distance(V_Position, V_NextPoint);
		/// <summary>The distance to the previous path point.</summary>
		public float Dist_toPrev => Vector3.Distance(V_Position, V_PreviousPoint);

		public LNX_PathPoint(Vector3 pt, Vector3 prevPos, Vector3 nextPos, Vector3 nrml)
		{
			V_Position = pt;
			V_normal = nrml;
			V_PreviousPoint = prevPos;
			V_NextPoint = nextPos;
		}
	}
}