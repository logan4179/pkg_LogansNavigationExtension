using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_Vertex
	{
		/// <summary>Current position of this vertex in 3d space. Potentially modified after initial
		/// construction of the tri this vertex belongs to.</summary>
		public Vector3 V_Position;

		//[SerializeField, HideInInspector] private Vector3 V_flattenedPosition;

		[SerializeField, HideInInspector] private Vector3 originalPosition;
		/// <summary>Initial position, in 3d space, of this vertex upon creation of it's owning triangle, 
		/// before any modifications </summary>
		public Vector3 OriginalPosition => originalPosition;

		[Header("LOCATING")] //---------------------------------------------------------------
		public LNX_ComponentCoordinate MyCoordinate;

		/// <summary>Index corresponding to the visualization mesh's triangles array that this vertex 
		/// corresponds to.</summary>
		public int Index_VisMesh_triangles
		{
			get
			{
				return (MyCoordinate.TrianglesIndex * 3) + MyCoordinate.ComponentIndex;
			}
		}

		/// <summary>Index corresponding to the visualization mesh's vertices array that this vertex 
		/// corresponds to.</summary>
		public int Index_VisMesh_Vertices = -1;


		//[Header("CALCULATED/DERIVED")] //---------------------------------------------------------------
		/// <summary>Aangle at the inner corner of the triangle at this vertex.</summary>
		public float AngleAtBend => Vector3.Angle(V_ToFirstSiblingVert, V_ToSecondSiblingVert);

		/// <summary>Aangle at the inner corner of the triangle at this vertex assuming all verts are flattneed.</summary>
		[HideInInspector] public float AngleAtBend_flattened
		{
			get
			{
				return Vector3.Angle(
					LNX_Utils.FlatVector(V_ToFirstSiblingVert, v_projectionNormal_cached),
					LNX_Utils.FlatVector(V_ToSecondSiblingVert, v_projectionNormal_cached)
				);
			}
		}

		/// <summary>Cached center vector for the owning triangle. This is for exposed property calculation </summary>
		[SerializeField, HideInInspector] private Vector3 v_triCenter_cached;

		/// <summary>Normalized directional vector pointing from this vertex to the center of it's triangle </summary>
		[HideInInspector] public Vector3 v_toCenter => Vector3.Normalize( v_triCenter_cached - V_Position );

		[HideInInspector] public float DistanceToCenter => Vector3.Distance( V_Position, v_triCenter_cached );

		/// <summary>Should be the same as the Surface Orientation setting for the navmesh that this vert's triangle belongs to.</summary>
		[SerializeField, HideInInspector] private Vector3 v_projectionNormal_cached;

		public Vector3 V_flattenedPosition
		{
			get
			{
				return LNX_Utils.FlatVector( V_Position, v_projectionNormal_cached );
			}
		}

		// TRUTH...........
		public bool AmModified
		{
			get {  return V_Position != originalPosition; }
		}

		/// <summary> Returns a localized (0 origin) vector pointing from this vert to it's first sibling vert. </summary>
		public Vector3 V_ToFirstSiblingVert //ERRORTRACE 11
		{
			get
			{
				return Vector3.Normalize( FirstSiblingRelationship.RelatedVertPosition - V_Position );
			}
		}
		/// <summary> Returns a localized (0 origin) vector pointing from this vert to it's first sibling vert. </summary>
		public Vector3 V_ToSecondSiblingVert
		{
			get
			{
				return Vector3.Normalize( SecondSiblingRelationship.RelatedVertPosition - V_Position );
			}
		}

		[Header("RELATIONAL")] //---------------------------------------------------------------
		[HideInInspector] public LNX_VertexRelationship[] Relationships;
		
		public LNX_VertexRelationship FirstSiblingRelationship
		{
			get
			{
				return MyCoordinate.ComponentIndex == 0 ?
					Relationships[(MyCoordinate.TrianglesIndex * 3) + 1] : Relationships[MyCoordinate.TrianglesIndex * 3];
			}
		}
		public LNX_VertexRelationship SecondSiblingRelationship
		{
			get
			{
				return MyCoordinate.ComponentIndex == 2 ?
					Relationships[(MyCoordinate.TrianglesIndex * 3) + 1] : Relationships[(MyCoordinate.TrianglesIndex * 3) + 2];
			}
		}

		public LNX_ComponentCoordinate[] SharedVertexCoordinates;

		/*[TextArea(1,10)]*/ [HideInInspector] public string DBG_constructor;

		public LNX_Vertex( LNX_NavMesh nm, Vector3 vrtPos, int triIndx, int cmpntIndx )
        {
			DBG_constructor = "Ctor start...\n";

			V_Position = vrtPos;
			originalPosition = vrtPos;

			v_projectionNormal_cached = nm.GetSurfaceNormal();

			v_triCenter_cached = nm.Triangles[triIndx].V_Center;

			MyCoordinate = new LNX_ComponentCoordinate( triIndx, cmpntIndx );

			Index_VisMesh_Vertices = -1;

			Relationships = new LNX_VertexRelationship[ nm.Triangles.Length * 3 ];

			DBG_constructor = $"Was passed pos: '{vrtPos}' indx: '{cmpntIndx}'\n\n" +
				$"at tri[{MyCoordinate.TrianglesIndex}], [{MyCoordinate.ComponentIndex}]\n" +
				$"Pos: '{V_Position}', orig: '{originalPosition}'\n" +
				$"fltndPos: '{V_flattenedPosition}'\n" +
				$"vToCtr: '{v_toCenter}'\n" +
				$"nml: '{v_projectionNormal_cached}', dstToCtr: '{DistanceToCenter}'\n" +
				$"";
		}

		public void AdoptValues( LNX_Vertex vert )
		{
			V_Position = vert.V_Position;
			originalPosition = vert.originalPosition;
			v_projectionNormal_cached = vert.v_projectionNormal_cached;
			v_triCenter_cached = vert.v_triCenter_cached;

			MyCoordinate = vert.MyCoordinate;

			Relationships = vert.Relationships;
			SharedVertexCoordinates = vert.SharedVertexCoordinates;

			DBG_constructor = vert.DBG_constructor;
		}

		public void TriIndexChanged( int newIndex )
		{
			MyCoordinate = new LNX_ComponentCoordinate( newIndex, MyCoordinate.ComponentIndex );
		}

		public void CreateRelationships( LNX_NavMesh nvmsh ) //todo: unit test
		{
			DBG_constructor += $"{nameof(CreateRelationships)}() start...\n";
			Debug.Log( $"{nameof(CreateRelationships)}() for vert: '{MyCoordinate}'..." );

			Relationships = new LNX_VertexRelationship[nvmsh.Triangles.Length * 3];

			DBG_constructor += $"Initialized relationships list with '{Relationships.Length}'" +
				$" entries. Iterating through...\n";

			//First establish initial relationships sibling relationships. This is important to do 
			//now so that the rest can raycast without error...
			#region ESTABLISH SIBLING RELATIONSHIPS FIRST --------------------------------------------------
			Relationships[MyCoordinate.TrianglesIndex*3] = new LNX_VertexRelationship(
				this, nvmsh.Triangles[MyCoordinate.TrianglesIndex].Verts[0], nvmsh
			);
			Relationships[(MyCoordinate.TrianglesIndex * 3)+1] = new LNX_VertexRelationship(
				this, nvmsh.Triangles[MyCoordinate.TrianglesIndex].Verts[1], nvmsh
			);
			Relationships[(MyCoordinate.TrianglesIndex * 3) + 2] = new LNX_VertexRelationship(
				this, nvmsh.Triangles[MyCoordinate.TrianglesIndex].Verts[2], nvmsh
			);
			#endregion
			
			for ( int i = 0; i < nvmsh.Triangles.Length; i++ )
			{
				if( i == MyCoordinate.TrianglesIndex )
				{
					continue;
				}

				DBG_constructor += $"making relationships for verts belonging to tri: '{i}'...\n";
				//Debug.Log($"iterated to verts belonging to tri: '{i}'...");

				Relationships[(i*3)] = new LNX_VertexRelationship( this, nvmsh.Triangles[i].Verts[0], nvmsh ); //ERRORTRACE 5:
				//Debug.Log($"created vert rel {i*3}\n{Relationships[i*3]}...");

				Relationships[(i*3)+1] = new LNX_VertexRelationship( this, nvmsh.Triangles[i].Verts[1], nvmsh );
				//Debug.Log($"created vert rel {(i * 3)+1}\n{Relationships[(i * 3)+1]}...");

				Relationships[(i*3)+2] = new LNX_VertexRelationship( this, nvmsh.Triangles[i].Verts[2], nvmsh );
				//Debug.Log($"created vert rel {(i * 3) + 2}\n{Relationships[(i * 3)+2]}...");

			}

			DBG_constructor += $"\n{nameof(CreateRelationships)}() report...\n" +
				$"AngAtBnd: '{AngleAtBend}', flatnd: '{AngleAtBend_flattened}' \n" +
				$"vToFirst: '{V_ToFirstSiblingVert}', vToSecond: '{V_ToSecondSiblingVert}'\n" +
				//$"'{}' - '{}'\n" +
				$"{nameof(Index_VisMesh_Vertices)}: '{Index_VisMesh_Vertices}'\n" +
				$"created '{Relationships.Length}' relationships...";
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

			DBG_IsInCenterSweep += "returning true";

			return true;
		}

		public bool IsInFlatCenterSweep( Vector3 pos )
		{
			DBG_IsInCenterSweep = $"Vert{MyCoordinate.ComponentIndex}.{nameof(IsInFlatCenterSweep)}({pos}) " +
				$"report...\n";

			Vector3 vToPos = Vector3.Normalize( LNX_Utils.FlatVector(pos, v_projectionNormal_cached) - V_flattenedPosition );

			//Debug.Log($"ERRORSPOT. coord: '{MyCoordinate}'. relLength: '{Relationships.Length}'. 1stSibINdx should be: " +
				//$"'{(MyCoordinate.ComponentIndex == 0 ?	(MyCoordinate.TrianglesIndex * 3) + 1 : MyCoordinate.TrianglesIndex * 3)}'...");
			
			Vector3 v_to0_flat = LNX_Utils.FlatVector(V_ToFirstSiblingVert, v_projectionNormal_cached).normalized; //ERRORTRACE 10 (FINAL)
			Vector3 v_to1_flat = LNX_Utils.FlatVector(V_ToSecondSiblingVert, v_projectionNormal_cached).normalized;

			DBG_IsInCenterSweep += $"using vto vector: '{vToPos}' and nrml: '{v_projectionNormal_cached}'\n" +
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
			return IsInCenterSweep( tri.V_Center + Vector3.ProjectOnPlane(pos, v_projectionNormal_cached) );
		}
		#endregion

		public int GetNumberOfSharedVerts( int triIndex )
		{
			if( triIndex == MyCoordinate.TrianglesIndex )
			{
				return 0;
			}

			int sharedCount = 0;
			for ( int i = 0; i < SharedVertexCoordinates.Length; i++ )
			{
				if ( SharedVertexCoordinates[i].TrianglesIndex == triIndex )
				{
					sharedCount++;
				}
			}

			return sharedCount;
		}

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

		public string GetCurrentInfoString()
		{
			return $"Vert.{nameof(SayCurrentInfo)}()\n" +
				$"{nameof(MyCoordinate)}: '{MyCoordinate}'\n" +
				$"{nameof(V_Position)}: '{V_Position}'\n" +
				$"{nameof(originalPosition)}: '{originalPosition}'\n" +
				$"{nameof(v_projectionNormal_cached)}: '{v_projectionNormal_cached}'\n" +
				$"{nameof(Relationships)} count: '{Relationships.Length}\n" +
				$"{nameof(Index_VisMesh_Vertices)}: '{Index_VisMesh_Vertices}'\n" +
				$"{nameof(AngleAtBend)}: '{AngleAtBend}'\n" +
				$"{nameof(AngleAtBend_flattened)}: '{AngleAtBend_flattened}'\n" +
				$"";
		}

		public void SayCurrentInfo()
		{
			Debug.Log( GetCurrentInfoString() );
		}

		public override string ToString()
		{
			return $"{MyCoordinate.ToString()} {V_Position}";
		}
	}
}