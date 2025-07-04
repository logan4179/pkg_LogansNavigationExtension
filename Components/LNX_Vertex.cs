using System;
using System.Collections.Generic;
using System.IO.Compression;
using UnityEngine;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_Vertex
	{
		/// <summary>Current position of this vertex in 3d space. Potentially modified after initial
		/// construction of the tri this vertex belongs to.</summary>
		public Vector3 V_Position;

		private Vector3 V_flattenedPosition;

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
		/// <summary>Aangle at the inner corner of the triangle at this vertex assuming all verts are flattneed.</summary>
		[HideInInspector] public float AngleAtBend_flattened;


		/// <summary>Vector pointing from this vertex to the center of it's triangle </summary>
		[HideInInspector] public Vector3 v_toCenter;

		[HideInInspector] public float DistanceToCenter;

		[HideInInspector] public Vector3 v_normal;

		// TRUTH...........
		public bool AmModified
		{
			get {  return V_Position != originalPosition; }
		}

		/// <summary> Returns a localized (0 origin) vector pointing from this vert to it's first sibling vert. </summary>
		public Vector3 V_ToFirstSiblingVert
		{
			get
			{
				return SiblingRelationships[0].v_to;
			}
		}
		/// <summary> Returns a localized (0 origin) vector pointing from this vert to it's first sibling vert. </summary>
		public Vector3 V_ToSecondSiblingVert
		{
			get
			{
				return SiblingRelationships[1].v_to;
			}
		}

		[Header("RELATIONAL")] //---------------------------------------------------------------
		public LNX_VertexRelationship[] SiblingRelationships;
		[HideInInspector] public LNX_VertexRelationship[] Relationships;

		public LNX_ComponentCoordinate[] SharedVertexCoordinates;

		[TextArea(1,10)] public string DBG_constructor;

		public LNX_Vertex( LNX_Triangle tri, Vector3 vrtPos, int cmpntIndx )
        {
			V_Position = vrtPos;

			V_flattenedPosition = tri.GetFlattenedPosition( V_Position );

			originalPosition = vrtPos;

			v_toCenter = Vector3.Normalize( tri.V_Center - vrtPos );
			v_normal = tri.v_sampledNormal;
			DistanceToCenter = Vector3.Distance(tri.V_Center, vrtPos);

			MyCoordinate = new LNX_ComponentCoordinate( tri.Index_inCollection, cmpntIndx );

			Relationships = new LNX_VertexRelationship[0];
			SiblingRelationships = new LNX_VertexRelationship[2];

			Index_VisMesh_triangles = tri.MeshIndex_trianglesStart + cmpntIndx;
			Index_VisMesh_Vertices = -1;

			DBG_constructor = $"Was passed pos: '{vrtPos}' indx: '{cmpntIndx}'\n\n" +
				$"at tri[{MyCoordinate.TrianglesIndex}], [{MyCoordinate.ComponentIndex}]\n" +
				$"Pos: '{V_Position}', orig: '{originalPosition}'\n" +
				$"fltndPos: '{V_flattenedPosition}'\n" +
				$"vToCtr: '{v_toCenter}'\n" +
				$"nml: '{v_normal}', dstToCtr: '{DistanceToCenter}'\n" +
				$"";
		}

		public void AdoptValues( LNX_Vertex vert )
		{
			V_Position = vert.V_Position;

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
			#region Find the angle -----------------------------------
			Vector3 v_toA = Vector3.Normalize(vA.V_Position - V_Position);
			Vector3 v_toB = Vector3.Normalize(vB.V_Position - V_Position);

			AngleAtBend = Vector3.Angle(v_toA, v_toB);

			AngleAtBend_flattened = Vector3.Angle( GetFlattenedPosition(v_toA), GetFlattenedPosition(v_toB) );
			#endregion

			SiblingRelationships = new LNX_VertexRelationship[2];
			SiblingRelationships[0] = new LNX_VertexRelationship( this, vA );
			SiblingRelationships[1] = new LNX_VertexRelationship( this, vB );

			DBG_constructor += $"\n{nameof(SetSiblingRelationships)}() report...\n" +
				$"AngAtBnd: '{AngleAtBend}', fltnd: '{AngleAtBend_flattened}' \n" +
				$"A: '{SiblingRelationships[0].Angle_centerToDestinationVertex}', " +
				$"B: '{SiblingRelationships[1].Angle_centerToDestinationVertex}'";
		}

		#region API METHODS ------------------------------------------------------------
		public string DBG_IsInCenterSweep;
		/// <summary>
		/// Determines if the line from this vertex to the supplied position 
		/// is within the theoretical "cone" created by the angle of the  sides emenating out from this vertex.
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public bool IsInCenterSweep( Vector3 pos )
		{
			DBG_IsInCenterSweep = "";
			
			Vector3 vToPos = Vector3.Normalize( pos - V_Position );

			DBG_IsInCenterSweep += $"{nameof(AngleAtBend)}: '{AngleAtBend}'\n" +
				$"ang0: '{Vector3.Angle(V_ToFirstSiblingVert, vToPos)}', " +
				$"ang1: '{Vector3.Angle(V_ToSecondSiblingVert, vToPos)}'\n" +
				$"diff0: '{AngleAtBend - Vector3.Angle(V_ToFirstSiblingVert, vToPos)}'\n" +
				$"diff1: '{AngleAtBend - Vector3.Angle(V_ToSecondSiblingVert, vToPos)}'\n";


			if (Vector3.Angle(V_ToFirstSiblingVert, vToPos) > (AngleAtBend + 0.01f) ||
				Vector3.Angle(V_ToSecondSiblingVert, vToPos) > (AngleAtBend + 0.01f)
				)
			{
				DBG_IsInCenterSweep += "returning false";
				return false;
			}
			/*
			if ( Vector3.Angle(SiblingRelationships[0].v_to, vToPos) > AngleAtBend ||
				Vector3.Angle(SiblingRelationships[1].v_to, vToPos) > AngleAtBend
				)
			{
				DBG_IsInCenterSweep += "returning false";
				return false;
			}*/

			DBG_IsInCenterSweep += "returning true";


			return true;
		}

		public bool IsInFlatCenterSweep( Vector3 pos )
		{
			DBG_IsInCenterSweep = $"{nameof(IsInFlatCenterSweep)}() report..\n";

			Vector3 vToPos = Vector3.Normalize( GetFlattenedPosition(pos) - V_flattenedPosition );
			Vector3 v_to0_flat = GetFlattenedPosition(V_ToFirstSiblingVert).normalized;
			Vector3 v_to1_flat = GetFlattenedPosition(V_ToSecondSiblingVert).normalized;

			DBG_IsInCenterSweep += $"using vector: '{vToPos}'\n" +
				$"{nameof(AngleAtBend_flattened)}: '{AngleAtBend_flattened}'\n" +
				$"ang0: '{Vector3.Angle(v_to0_flat, vToPos)}', " +
				$"ang1: '{Vector3.Angle(v_to1_flat, vToPos)}'\n" +
				$"diff0: '{AngleAtBend_flattened - Vector3.Angle(v_to0_flat, vToPos)}'\n" +
				$"diff1: '{AngleAtBend_flattened - Vector3.Angle(v_to1_flat, vToPos)}'\n";


			if ( 
				Vector3.Angle(v_to0_flat, vToPos) > (AngleAtBend_flattened + 0.001f) ||
				Vector3.Angle(v_to1_flat, vToPos) > (AngleAtBend_flattened + 0.001f)
				)
			{
				DBG_IsInCenterSweep += "returning false";
				return false;
			}

			DBG_IsInCenterSweep += "returning true";

			return true;
		}

		/// <summary>
		/// Determines if a supplied position is towards the normalized center from this vertex. Doing this 
		/// on the 3 vertices of a triangle will determine if a position is in the "normal sweep" of a triangle 
		/// (IE: within the normalized plane of a triangle).
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public bool IsInCenterSweep_Projected( Vector3 pos, LNX_Triangle tri )
		{
			return IsInCenterSweep( tri.V_Center + Vector3.ProjectOnPlane(pos, v_normal) );
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

		private Vector3 GetFlattenedPosition( Vector3 pos )
		{
			if ( V_flattenedPosition.y == 0f )
			{
				return new Vector3(pos.x, 0f, pos.z);
			}
			else if ( V_flattenedPosition.x == 0f )
			{
				return new Vector3(0f, pos.y, pos.z);
			}
			else if ( V_flattenedPosition.z == 0f )
			{
				return new Vector3(pos.x, pos.y, 0f);
			}

			return Vector3.zero;
		}
	}
}