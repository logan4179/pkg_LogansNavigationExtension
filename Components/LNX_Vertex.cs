using System.Collections.Generic;
using UnityEngine;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_Vertex
	{
		/// <summary>Current position of this vertex in 3d space. Potentially modified after initial
		/// construction of the tri this vertex belongs to.</summary>
		public Vector3 Position;

		[SerializeField, HideInInspector] Vector3 originalPosition;
		/// <summary>Initial position, in 3d space, of this vertex upon creation of it's owning triangle, 
		/// before any modifications </summary>
		public Vector3 OriginalPosition => originalPosition;

		[Header("LOCATION")] //---------------------------------------------------------------
		public LNX_ComponentCoordinate MyCoordinate;

		/// <summary>Index corresponding to the visualization mesh's triangles array that this vertex 
		/// corresponds to.</summary>
		public int Index_VisMesh_triangles;

		/// <summary>Index corresponding to the visualization mesh's vertices array that this vertex 
		/// corresponds to.</summary>
		public int Index_VisMesh_Vertices = -1;

		//[Header("CALCULATED/DERIVED")] //---------------------------------------------------------------
		/// <summary>Aangle at the inner corner of the triangle at this vertex.</summary>
		[HideInInspector] public float AngleAtBend;

		/// <summary>Vector pointing from this vertex to the center of it's triangle </summary>
		[HideInInspector] public Vector3 v_toCenter;

		[HideInInspector] public float DistanceToCenter;

		[HideInInspector] public Vector3 v_normal;

		// TRUTH...........
		public bool AmModified
		{
			get {  return Position != originalPosition; }
		}
		/// <summary>How many verts share the exact position of this one. Can be used to quickly tell if this 
		/// vert is mid-navmesh, or on a spot where the navmesh terminates.</summary>
		//public int Valency;

		[Header("RELATIONAL")] //---------------------------------------------------------------
		public LNX_VertexRelationship[] SiblingRelationships;
		[HideInInspector] public LNX_VertexRelationship[] Relationships;

		public LNX_ComponentCoordinate[] SharedVertexCoordinates;

		[TextArea(1,5)] public string DBG_constructor;

		public LNX_Vertex( LNX_Triangle tri, Vector3 vrtPos, int cmpntIndx )
        {
			Position = vrtPos;

			originalPosition = vrtPos;

			v_toCenter = Vector3.Normalize( tri.V_center - vrtPos );
			v_normal = tri.v_normal;
			DistanceToCenter = Vector3.Distance(tri.V_center, vrtPos);

			MyCoordinate = new LNX_ComponentCoordinate( tri.Index_inCollection, cmpntIndx );

			Relationships = new LNX_VertexRelationship[0];
			SiblingRelationships = new LNX_VertexRelationship[2];

			#region Find the angle -----------------------------------
			Vector3 v_toA = Vector3.zero;
			Vector3 v_toB = Vector3.zero;
			if (cmpntIndx == 0)
			{
				v_toA = Vector3.Normalize(tri.Verts[1].Position - Position);
				v_toB = Vector3.Normalize(tri.Verts[2].Position - Position);
			}
			else if ( cmpntIndx == 1 )
			{
				v_toA = Vector3.Normalize(tri.Verts[0].Position - Position);
				v_toB = Vector3.Normalize(tri.Verts[2].Position - Position);
			}
			else if ( cmpntIndx == 2 )
			{
				v_toA = Vector3.Normalize(tri.Verts[0].Position - Position);
				v_toB = Vector3.Normalize(tri.Verts[1].Position - Position);
			}

			AngleAtBend = Vector3.Angle(v_toA, v_toB);
			#endregion

			Index_VisMesh_triangles = tri.MeshIndex_trianglesStart + cmpntIndx;
			Index_VisMesh_Vertices = -1;

			DBG_constructor = $"at tri[{MyCoordinate.TrianglesIndex}], [{MyCoordinate.ComponentIndex}]\n" +
				$"Pos: '{Position}', orig: '{originalPosition}'\n" +
				$"vToCtr: '{v_toCenter}'\n" +
				$"nml: '{v_normal}', dstToCtr: '{DistanceToCenter}'\n" +
				$"";
		}

		public void AdoptValues( LNX_Vertex vert )
		{
			Position = vert.Position;

			v_toCenter = vert.v_toCenter;
			v_normal = vert.v_normal;
			DistanceToCenter = vert.DistanceToCenter;
			MyCoordinate = vert.MyCoordinate;

			Relationships = vert.Relationships;
			SiblingRelationships = vert.SiblingRelationships;
			SharedVertexCoordinates = vert.SharedVertexCoordinates;

			AngleAtBend = vert.AngleAtBend;

			DBG_constructor = vert.DBG_constructor;
		}

		public void TriIndexChanged( int newIndex )
		{
			MyCoordinate = new LNX_ComponentCoordinate( newIndex, MyCoordinate.ComponentIndex );
		}

		/// <summary>
		/// Creates SiblingRelationship objects for other 2 sibling vertices. Calculates and 
		/// caches convenience variables for relating this vertex to it's sibling vertices.
		/// </summary>
		/// <param name="vA"></param>
		/// <param name="vB"></param>
		public void SetSiblingRelationships( LNX_Vertex vA, LNX_Vertex vB ) //todo: unit test
		{
			SiblingRelationships = new LNX_VertexRelationship[2];
			SiblingRelationships[0] = new LNX_VertexRelationship( this, vA );
			SiblingRelationships[1] = new LNX_VertexRelationship( this, vB );

			DBG_constructor += $"\tAngle: '{AngleAtBend}'. A: " +
				$"'{SiblingRelationships[0].Angle_centerToDestinationVertex}', " +
				$"B: '{SiblingRelationships[1].Angle_centerToDestinationVertex}'";
		}

		#region API METHODS ------------------------------------------------------------
		/// <summary>
		/// Determines if the line from this vertex to the supplied position 
		/// is within the theoretical "cone" created by the angle of the  sides emenating out from this vertex.
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public bool IsInCenterSweep( Vector3 pos )
		{
			Vector3 vToPos = Vector3.Normalize( pos - Position );

			if( Vector3.Angle(SiblingRelationships[0].v_to, vToPos) > AngleAtBend ||
				Vector3.Angle(SiblingRelationships[1].v_to, vToPos) > AngleAtBend
				)
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
		#endregion

		public void Ping( LNX_Triangle[] tris )
		{
			Relationships = new LNX_VertexRelationship[(tris.Length * 3)-1]; //minus one to account for not needing a relationship to itself...

			for ( int i = 0; i < tris.Length; i++ )
			{
				if( i == MyCoordinate.TrianglesIndex )
				{

				}
			}
		}
	}
}