using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_Vertex
	{
		public Vector3 Position;

		[SerializeField] public LNX_ComponentCoordinate MyCoordinate;

		[HideInInspector] public float Angle;

		/// <summary>Vector pointing from this vertex to the center of it's triangle </summary>
		[HideInInspector] public Vector3 v_toCenter;

		[HideInInspector] public float DistanceToCenter;

		[HideInInspector] public Vector3 v_normal;

		// TRUTH...........
		/// <summary>How many verts share the exact position of this one. Can be used to quickly tell if this 
		/// vert is mid-navmesh, or on a spot where the navmesh terminates.</summary>
		//public int Valency;

		public LNX_VertexRelationship_exp[] SiblingRelationships;
		[HideInInspector] public LNX_VertexRelationship_exp[] Relationships;

		//public List<LNX_ComponentCoordinate> SharedVertexCoordinates; //dws
		public LNX_ComponentCoordinate[] SharedVertexCoordinates;

		[TextArea(1,5)] public string DBG_constructor;

		/// <summary>
		/// Returns the index where this vertex originated in the original NavmeshTriangulation's vertices array.
		/// </summary>
		public int PositionInOriginalTriangulation;
		/*{
			get
			{
				return (MyCoordinate.TriIndex * 3) + MyCoordinate.ComponentIndex;
			}
		}*/


		public LNX_Vertex( LNX_Triangle tri, Vector3 vrtPos, int indx, NavMeshTriangulation nmTriangulation )
        {
			Position = vrtPos;
			//Position = nmTriangulation.vertices[ nmTriangulation.indices[tri.Index_parallelWithParentArray * 3] + indx ];

			v_toCenter = Vector3.Normalize( tri.V_center - vrtPos );
			v_normal = tri.v_normal;
			DistanceToCenter = Vector3.Distance(tri.V_center, vrtPos);

			MyCoordinate = new LNX_ComponentCoordinate( tri.Index_parallelWithParentArray, indx );

			Relationships = new LNX_VertexRelationship_exp[0];
			SiblingRelationships = new LNX_VertexRelationship_exp[2];
			//SharedVertexCoordinates = new List<LNX_ComponentCoordinate>(); //dws

			Angle = -1f;

			PositionInOriginalTriangulation = nmTriangulation.indices[(MyCoordinate.TriIndex * 3) + MyCoordinate.ComponentIndex];

			DBG_constructor = $"at tri[{MyCoordinate.TriIndex}], [{MyCoordinate.ComponentIndex}]\n" +
				$"Pos: '{Position}', vToCtr: '{v_toCenter}'\n" +
				$"nml: '{v_normal}', dstToCtr: '{DistanceToCenter}'\n" +
				$"";
		}

		public LNX_Vertex( LNX_Vertex vert )
		{
			Position = vert.Position;

			v_toCenter = vert.v_toCenter;
			v_normal = vert.v_normal;
			DistanceToCenter = vert.DistanceToCenter;
			MyCoordinate = vert.MyCoordinate;

			Relationships = vert.Relationships;
			SiblingRelationships = vert.SiblingRelationships;
			SharedVertexCoordinates = vert.SharedVertexCoordinates;

			Angle = vert.Angle;

			PositionInOriginalTriangulation = vert.PositionInOriginalTriangulation;

			DBG_constructor = vert.DBG_constructor;
		}

		public void AdoptValues(LNX_Vertex vert)
		{
			Position = vert.Position;

			v_toCenter = vert.v_toCenter;
			v_normal = vert.v_normal;
			DistanceToCenter = vert.DistanceToCenter;
			MyCoordinate = vert.MyCoordinate;

			Relationships = vert.Relationships;
			SiblingRelationships = vert.SiblingRelationships;
			SharedVertexCoordinates = vert.SharedVertexCoordinates;

			Angle = vert.Angle;

			DBG_constructor = vert.DBG_constructor;
		}

		/// <summary>
		/// Creates SiblingRelationship objects for other 2 sibling vertices. Calculates and 
		/// caches convenience variables for relating this vertex to it's sibling vertices.
		/// </summary>
		/// <param name="vA"></param>
		/// <param name="vB"></param>
		public void SetSiblingRelationships( LNX_Vertex vA, LNX_Vertex vB )
		{
			SiblingRelationships = new LNX_VertexRelationship_exp[2];
			SiblingRelationships[0] = new LNX_VertexRelationship_exp( this, vA );
			SiblingRelationships[1] = new LNX_VertexRelationship_exp( this, vB );

			Vector3 v_toA = Vector3.Normalize( vA.Position - Position );
			Vector3 v_toB = Vector3.Normalize( vB.Position - Position );
			Angle = Vector3.Angle( v_toA, v_toB );

			float semiA = Vector3.Angle(v_toCenter, v_toA);
			float semiB = Vector3.Angle(v_toCenter, v_toB);

			DBG_constructor += $"\tAngle: '{Angle}'. A: " +
				$"'{SiblingRelationships[0].Angle_centerToDestinationVertex}', " +
				$"B: '{SiblingRelationships[1].Angle_centerToDestinationVertex}'";
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

			/* //note: this was the old way of doing this. I've replaced it with what's above. I haven't thoroughly tested to make sure it hasn't broken something so I'm leaving this here for now...
			float lrgstAng = Mathf.Max(
				Vector3.Angle(SiblingRelationships[0].v_to, vToPos),
				Vector3.Angle(SiblingRelationships[1].v_to, vToPos)
			);

			if( lrgstAng < Angle )
			{
				return true;
			}

			return false;
			*/
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
				if( i == MyCoordinate.TriIndex )
				{

				}
			}
		}
	}
}