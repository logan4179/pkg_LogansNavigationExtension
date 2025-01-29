using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_Vertex
	{
		public Vector3 Position;

		public int MyIndex = -1;

		/// <summary>Collection of indices pointing to triangles that this vertex helps make up. </summary>
		public int[] CompositeTriangleIndices;
		public float[] Dist_vertToCompositeTriangleCenters;
		public float[] Angles_AtCompositeTrianglePoint;
		public int[] ValentEdgeIndices;

		// TRUTH...........
		[HideInInspector] public LNX_VertexRelationship_exp[] Relationships;

		public string DBG_constructor;

        public LNX_Vertex( Vector3 vrtPos, int indx, NavMeshTriangulation nmTriangulation )
        {
			MyIndex = indx;
			Position = vrtPos;

			CalculateRelationships( indx, nmTriangulation );
			DBG_constructor = $"My index: '{MyIndex}', Pos: '{Position}'";
		}

		public void CalculateRelationships( int indx, NavMeshTriangulation nmTriangulation )
		{
			Relationships = new LNX_VertexRelationship_exp[0];

			List<int> triIndices = new List<int>();
			List<float> distsVrtToCmpstTriCtrs = new List<float>();
			List<float> angsAtCmpstTriPt = new List<float>();
			for ( int i = 0; i < nmTriangulation.areas.Length; i++ )
			{
				bool foundVrtPosIndx = false;
				if ( nmTriangulation.vertices[i * 3] == Position )
				{
					foundVrtPosIndx = true;
					angsAtCmpstTriPt.Add(
						Vector3.Angle(nmTriangulation.vertices[(i * 3) + 1], nmTriangulation.vertices[(i * 3) + 2])
					);

				}
				else if ( nmTriangulation.vertices[(i * 3) + 1] == Position )
				{
					foundVrtPosIndx = true;
					angsAtCmpstTriPt.Add(
						Vector3.Angle(nmTriangulation.vertices[(i * 3)], nmTriangulation.vertices[(i * 3) + 2])
					);
				}
				else if (nmTriangulation.vertices[(i * 3) + 2] == Position )
				{
					foundVrtPosIndx = true;
					angsAtCmpstTriPt.Add(
						Vector3.Angle(nmTriangulation.vertices[(i * 3) + 1], nmTriangulation.vertices[(i * 3)])
					);
				}

				if( foundVrtPosIndx )
				{
					triIndices.Add( i );

					Vector3 vCtr = (nmTriangulation.vertices[i * 3] + nmTriangulation.vertices[(i * 3) + 1] + nmTriangulation.vertices[(i * 3) + 2]) / 3f;
					distsVrtToCmpstTriCtrs.Add( Vector3.Distance(Position, vCtr) );
				}
			}

			CompositeTriangleIndices = triIndices.ToArray();
			Dist_vertToCompositeTriangleCenters = distsVrtToCmpstTriCtrs.ToArray();
			Angles_AtCompositeTrianglePoint = angsAtCmpstTriPt.ToArray();

			DBG_constructor += $"\tcomposite tri amount: '{CompositeTriangleIndices.Length}'\n" +
				$"";
		}

		/// <summary>
		/// Use this overload for when the mesh has been changed only with positioning as opposed to cutting.
		/// </summary>
		/// <param name="indx"></param>
		/// <param name="nmTriangulation"></param>
		public void CalculateRelationships_shallow(int indx, LNX_Triangle[] tris )
		{
			Relationships = new LNX_VertexRelationship_exp[0];

			List<int> cmpTriIndices = new List<int>();
			List<float> distsVrtToCmpstTriCtrs = new List<float>();
			List<float> angsAtCmptstTriPt = new List<float>();
			/*for (int i = 0; i < nmTriangulation.areas.Length; i++)
			{
				bool foundVrtPosIndx = false;
				if (nmTriangulation.vertices[i * 3] == Position)
				{
					foundVrtPosIndx = true;
					angsAtCmpstTriPt.Add(
						Vector3.Angle(nmTriangulation.vertices[(i * 3) + 1], nmTriangulation.vertices[(i * 3) + 2])
					);

				}
				else if (nmTriangulation.vertices[(i * 3) + 1] == Position)
				{
					foundVrtPosIndx = true;
					angsAtCmpstTriPt.Add(
						Vector3.Angle(nmTriangulation.vertices[(i * 3)], nmTriangulation.vertices[(i * 3) + 2])
					);
				}
				else if (nmTriangulation.vertices[(i * 3) + 2] == Position)
				{
					foundVrtPosIndx = true;
					angsAtCmpstTriPt.Add(
						Vector3.Angle(nmTriangulation.vertices[(i * 3) + 1], nmTriangulation.vertices[(i * 3)])
					);
				}

				if (foundVrtPosIndx)
				{
					triIndices.Add(i);

					Vector3 vCtr = (nmTriangulation.vertices[i * 3] + nmTriangulation.vertices[(i * 3) + 1] + nmTriangulation.vertices[(i * 3) + 2]) / 3f;
					distsVrtToCmpstTriCtrs.Add(Vector3.Distance(Position, vCtr));
				}
			}*/

			CompositeTriangleIndices = cmpTriIndices.ToArray();
			Dist_vertToCompositeTriangleCenters = distsVrtToCmpstTriCtrs.ToArray();
			Angles_AtCompositeTrianglePoint = angsAtCmptstTriPt.ToArray();

			DBG_constructor += $"\tcomposite tri amount: '{CompositeTriangleIndices.Length}'\n" +
				$"";
		}

		/// <summary>
		/// Determines if the line from this vertex to the supplied position 
		/// is within the theoretical "cone" created by the angle of the  sides emenating out from this vertex.
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public bool IsInCenterSweep( Vector3 pos )
		{
			Vector3 vToPos = Vector3.Normalize( pos - Position );

			if( Vector3.Angle(SiblingRelationships[0].v_to, vToPos) > Angle )
			{
				return false;
			}

			if ( Vector3.Angle(SiblingRelationships[1].v_to, vToPos) > Angle )
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Determines if a supplied position is towards the normalized center from this vertex. Doing this 
		/// on the 3 vertices of a triangle will determine if a position is in the "normal sweep" of a triangle 
		/// (IE: within the normalized plane of a triangle).
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public bool IsInNormalizedCenterSweep( Vector3 pos, LNX_Triangle tri )
		{
			return IsInCenterSweep( tri.V_center + Vector3.ProjectOnPlane(pos, v_normal) );

		}

		public void Ping( LNX_Triangle[] tris )
		{
			Relationships = new LNX_VertexRelationship_exp[(tris.Length * 3)-1]; //minus one to account for not needing a relationship to itself...

			for ( int i = 0; i < tris.Length; i++ )
			{
				/*if( i == MyCoordinate.TriIndex )
				{

				}*/
			}
		}
	}
}