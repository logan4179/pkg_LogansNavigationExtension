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

		public LNX_PathPoint(Vector3 pt, Vector3 nrml)
		{
			V_Position = pt;
			V_normal = nrml;
		}

		public LNX_PathPoint( LNX_NavmeshHit hit )
		{
			V_Position = hit.Position;
			V_normal = hit.Normal;
		}

		#region OPERATORS ======================================================
		public static bool operator ==(LNX_PathPoint a, LNX_PathPoint b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(LNX_PathPoint a, LNX_PathPoint b)
		{
			return !a.Equals(b);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is LNX_PathPoint))
				return false;

			LNX_PathPoint otherPoint = (LNX_PathPoint)obj;
			if
			(
				otherPoint.V_Position != V_Position || 
				otherPoint.V_normal != V_normal
			)
			{
				return false;
			}
			else
			{
				return true;
			}
		}
		#endregion ---------------------------------------
	}
}