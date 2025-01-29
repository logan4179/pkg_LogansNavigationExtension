using UnityEngine;
using UnityEngine.AI;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_Edge
	{
		public int MyIndex = -1;

		public float EdgeLength;

		public Vector3 StartPosition; //todo: see how hard it would be to do without this and use the start vert index to reference the vert...
		public Vector3 MidPosition;
		public Vector3 EndPosition; //todo: see how hard it would be to do without this and use the end vert index to reference the vert...

		public Vector3 v_startToEnd;
		public Vector3 v_endToStart;

		// TRUTH...........
		/// <summary>If true, it means that this edge has no shared edge with another triangle, 
		/// and therefore forms part of the boundary of walkable space.</summary>
		public bool Flag_AmTerminal
		{
			get
			{
				return Index_CompositeTriB == -1;
			}
		}

		// RELATIONAL...........
		public int Index_StartingVert = -1;
		public int Index_EndingVert = -1;
		/// <summary>Index of the first/primary triangle that is partially formed by this edge. This will always be a number >= 0</summary>
		public int Index_CompositeTriA = -1;
		/// <summary>
		/// Index of the second triangle that is partially formed by this edge, if any. If this edge only forms one triangle, then 
		/// this value will stay at -1, indicating that this edge forms a boundary in walkable space.
		/// </summary>
		public int Index_CompositeTriB = -1;
		public Vector3 v_cross_CompositeTriA, v_cross_CompositeTriB;
		public Vector3 v_toCenter_CompositeTriA, v_toCenter_CompositeTriB;

		public LNX_Edge( 
			int indx, LNX_Vertex strtVrt, LNX_Vertex endVrt, NavMeshTriangulation nmTriangultn
		)
		{
			CalculateInfo( strtVrt, endVrt, nmTriangultn );

			// RELATIONAL...........
			MyIndex = indx;
			Index_StartingVert = strtVrt.MyIndex;
			Index_EndingVert = endVrt.MyIndex;
			Index_CompositeTriA = -1;
			Index_CompositeTriB = -1;
		}

		public void CalculateInfo( LNX_Vertex strtVrt, LNX_Vertex endVrt, NavMeshTriangulation nmTriangultn )
		{
			StartPosition = strtVrt.Position;
			EndPosition = endVrt.Position;
			MidPosition = (StartPosition + EndPosition) / 2f;

			v_startToEnd = Vector3.Normalize( StartPosition - EndPosition );
			v_endToStart = Vector3.Normalize( EndPosition - StartPosition );

			EdgeLength = Vector3.Distance( StartPosition, EndPosition );

			/*
			v_toCenter = Vector3.Normalize(triCtrPos - MidPosition);
			v_cross = Vector3.Cross(v_startToEnd, triNrml).normalized;

			if (Vector3.Dot(v_cross, v_toCenter) < 0)
			{
				v_cross = -v_cross;
			}*/

			for ( int i = 0; i < nmTriangultn.areas.Length; i++ ) 
			{
				if ( nmTriangultn.vertices[i * 3] == StartPosition )
				{
					Index_CompositeTriA = i; //got here...
				}
			}
			Index_CompositeTriB = ;

			v_cross_CompositeTriA = 
			v_cross_CompositeTriB;
			v_toCenter_CompositeTriA
			v_toCenter_CompositeTriB;
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