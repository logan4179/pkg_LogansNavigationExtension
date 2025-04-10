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

		/// <summary> Currently set in the Triangle relationship constructor</summary>
		public LNX_ComponentCoordinate SharedEdge;

		public bool AmTerminal => SharedEdge == LNX_ComponentCoordinate.None;

		// TRUTH...........
		/// <summary>If true, it means that this edge has no shared edge with another triangle, 
		/// and therefore forms part of the boundary of walkable space.</summary>
		//public bool AmTerminal;

		public LNX_Edge( LNX_Triangle tri, LNX_Vertex strtVrt, LNX_Vertex endVrt, int indx )
		{
			CalculateInfo( tri, strtVrt, endVrt );

			MyCoordinate = new LNX_ComponentCoordinate( tri, indx );

			StartVertCoordinate = strtVrt.MyCoordinate;
			EndVertCoordinate = endVrt.MyCoordinate;

			SharedEdge = LNX_ComponentCoordinate.None;
		}

		public LNX_Edge( LNX_Edge edge )
		{
			EdgeLength = edge.EdgeLength;

			StartPosition = edge.StartPosition;
			StartVertCoordinate = edge.StartVertCoordinate;
			MidPosition = edge.MidPosition;
			EndPosition = edge.EndPosition;
			EndVertCoordinate = edge.EndVertCoordinate;

			v_startToEnd = edge.v_startToEnd;
			v_endToStart = edge.v_endToStart;

			v_toCenter = edge.v_toCenter;
			v_cross = edge.v_cross;

			MyCoordinate = edge.MyCoordinate;

			SharedEdge = edge.SharedEdge;
		}

		public void AdoptValues(LNX_Edge edge)
		{
			EdgeLength = edge.EdgeLength;

			StartPosition = edge.StartPosition;
			StartVertCoordinate = edge.StartVertCoordinate;
			MidPosition = edge.MidPosition;
			EndPosition = edge.EndPosition;
			EndVertCoordinate = edge.EndVertCoordinate;

			v_startToEnd = edge.v_startToEnd;
			v_endToStart = edge.v_endToStart;

			v_toCenter = edge.v_toCenter;
			v_cross = edge.v_cross;

			MyCoordinate = edge.MyCoordinate;

			SharedEdge = edge.SharedEdge;
		}

		public void CalculateInfo( LNX_Triangle tri, LNX_Vertex strtVrt, LNX_Vertex endVrt )
		{
			StartPosition = strtVrt.Position;
			EndPosition = endVrt.Position;
			MidPosition = (StartPosition + EndPosition) / 2f;

			v_startToEnd = Vector3.Normalize( StartPosition - EndPosition );
			v_endToStart = Vector3.Normalize( EndPosition - StartPosition );

			EdgeLength = Vector3.Distance( StartPosition, EndPosition );

			v_toCenter = Vector3.Normalize(tri.V_center - MidPosition);
			v_cross = Vector3.Cross(v_startToEnd, tri.v_normal).normalized;

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

		/// <summary>
		/// Checks whether the supplied edge is touching this edge at both the start 
		/// and end position.
		/// </summary>
		/// <param name="edge"></param>
		/// <returns></returns>
		public bool AmTouching( LNX_Edge edge )
		{
			if( edge.StartPosition == StartPosition || edge.EndPosition == StartPosition )
			{
				return true;
			}

			if( edge.EndPosition == EndPosition || edge.EndPosition == StartPosition )
			{
				return true;
			}

			return false;
		}
	}
}