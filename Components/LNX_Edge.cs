using UnityEngine;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_Edge
	{
		public float EdgeLength;

		public Vector3 StartPosition;
		public LNX_ComponentCoordinate StartVertCoordinate;
		public Vector3 MidPosition;
		public Vector3 EndPosition;
		public LNX_ComponentCoordinate EndVertCoordinate;

		public Vector3 v_startToEnd;
		public Vector3 v_endToStart;

		public Vector3 v_toCenter;
		public Vector3 v_cross;

		public LNX_ComponentCoordinate MyCoordinate;

		public LNX_ComponentCoordinate SharedEdge;

		// TRUTH...........
		/// <summary>If true, it means that this edge has no shared edge with another triangle, 
		/// and therefore forms part of the boundary of walkable space.</summary>
		//public bool AmTerminal;

		public LNX_Edge( LNX_Vertex strtVrt, LNX_Vertex endVrt, Vector3 triCtrPos, Vector3 triNrml, LNX_ComponentCoordinate myCoordinate )
		{
			CalculateInfo( strtVrt, endVrt, triCtrPos, triNrml );

			MyCoordinate = myCoordinate;

			StartVertCoordinate = strtVrt.MyCoordinate;
			EndVertCoordinate = endVrt.MyCoordinate;

			SharedEdge = LNX_ComponentCoordinate.None;
		}

		public void CalculateInfo( LNX_Vertex strtVrt, LNX_Vertex endVrt, Vector3 triCtrPos, Vector3 triNrml )
		{
			StartPosition = strtVrt.Position;
			EndPosition = endVrt.Position;
			MidPosition = (StartPosition + EndPosition) / 2f;

			v_startToEnd = Vector3.Normalize( StartPosition - EndPosition );
			v_endToStart = Vector3.Normalize( EndPosition - StartPosition );

			EdgeLength = Vector3.Distance( StartPosition, EndPosition );

			v_toCenter = Vector3.Normalize(triCtrPos - MidPosition);
			v_cross = Vector3.Cross(v_startToEnd, triNrml).normalized;

			if (Vector3.Dot(v_cross, v_toCenter) < 0)
			{
				v_cross = -v_cross;
			}
		}

		public Vector3 ClosestPointOnEdge(Vector3 pos)
		{
			Vector3 v_vrtToPos = pos - StartPosition;

			Vector3 v_result = StartPosition + Vector3.Project(v_vrtToPos, v_startToEnd);

			float dist_startToRslt = Vector3.Distance(v_result, StartPosition);
			float dist_endToRslt = Vector3.Distance(v_result, EndPosition);

			//Debug.Log($"dist_startToRslt: '{dist_startToRslt}', dist_endToRslt: '{dist_endToRslt}', len: '{EdgeLength}'");
			if (dist_startToRslt > EdgeLength || dist_endToRslt > EdgeLength)
			{
				//Debug.Log("if");
				v_result = dist_startToRslt < dist_endToRslt ? StartPosition : EndPosition;
			}

			return v_result;
		}

		public bool IsProjectedPointOnEdge(Vector3 origin, Vector3 direction)
		{
			float angBetweenVerts = Vector3.Angle(
				Vector3.Normalize(StartPosition - origin),
				Vector3.Normalize(EndPosition - origin)
			);

			float angToStart = Vector3.Angle(direction, StartPosition - origin);
			float angToEnd = Vector3.Angle(direction, EndPosition - origin);

			if (angToStart > angBetweenVerts || angToEnd > angBetweenVerts)
			{
				return false;
			}

			return true;
		}
	}
}