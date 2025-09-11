using System;
using System.Collections.Generic;
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
					LNX_Utils.FlatVector(V_ToFirstSiblingVert, v_surfaceNormal_cached),
					LNX_Utils.FlatVector(V_ToSecondSiblingVert, v_surfaceNormal_cached)
				);
			}
		}

		/// <summary>Cached center vector for the owning triangle. This is for exposed property calculation </summary>
		[SerializeField, HideInInspector] private Vector3 v_triCenter_cached;

		/// <summary>Normalized directional vector pointing from this vertex to the center of it's triangle </summary>
		[HideInInspector] public Vector3 v_toCenter => Vector3.Normalize( v_triCenter_cached - V_Position );

		[HideInInspector] public float DistanceToCenter => Vector3.Distance( V_Position, v_triCenter_cached );

		/// <summary>Should be the same as the Surface Orientation setting for the navmesh that this vert's triangle belongs to.</summary>
		[SerializeField, HideInInspector] private Vector3 v_surfaceNormal_cached;

		public Vector3 V_flattenedPosition
		{
			get
			{
				return LNX_Utils.FlatVector( V_Position, v_surfaceNormal_cached );
			}
		}

		// TRUTH...........
		public bool AmModified
		{
			get {  return V_Position != originalPosition; }
		}

		public int TriangleIndex => MyCoordinate.TrianglesIndex;
		public int ComponentIndex => MyCoordinate.ComponentIndex;

		public bool AmOnTerminalEdge; //todo: Implement

		[Header("RELATIONAL")] //---------------------------------------------------------------
		[HideInInspector] public LNX_VertexRelationship[] Relationships;

		private int firstSiblingRelationshipIndex => MyCoordinate.ComponentIndex == 0 ? (MyCoordinate.TrianglesIndex * 3) + 1 : MyCoordinate.TrianglesIndex * 3;
		public LNX_VertexRelationship FirstSiblingRelationship
		{
			get
			{
				/*return MyCoordinate.ComponentIndex == 0 ?
					Relationships[(MyCoordinate.TrianglesIndex * 3) + 1] : Relationships[MyCoordinate.TrianglesIndex * 3];*/

				return Relationships[firstSiblingRelationshipIndex];
			}
		}

		private int secondSiblingRelationshipIndex => MyCoordinate.ComponentIndex == 2 ? (MyCoordinate.TrianglesIndex * 3) + 1 : (MyCoordinate.TrianglesIndex * 3) + 2;
		public LNX_VertexRelationship SecondSiblingRelationship
		{
			get
			{
				/*return MyCoordinate.ComponentIndex == 2 ?
					Relationships[(MyCoordinate.TrianglesIndex * 3) + 1] : Relationships[(MyCoordinate.TrianglesIndex * 3) + 2];*/
				return Relationships[secondSiblingRelationshipIndex];
			}
		}

		/// <summary> Returns a localized (0 origin) vector pointing from this vert to it's first sibling vert. </summary>
		public Vector3 V_ToFirstSiblingVert
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

		public LNX_ComponentCoordinate[] SharedVertexCoordinates;

		public LNX_Vertex( LNX_Triangle ownerTri, Vector3 vrtPos, int triIndx, int cmpntIndx )
        {
			V_Position = vrtPos;
			originalPosition = vrtPos;

			v_surfaceNormal_cached = ownerTri.v_SurfaceNormal_cached;

			v_triCenter_cached = ownerTri.V_Center; //prob! triangles list not ready yet...

			MyCoordinate = new LNX_ComponentCoordinate( triIndx, cmpntIndx );

			Index_VisMesh_Vertices = -1;

			#region ESTABLISH SIBLING RELATIONSHIPS FIRST --------------------------------------------------
			//I really wish I could find a way to do the following right here...
			/*
			Relationships = new LNX_VertexRelationship[nvmsh.Triangles.Length * 3];
			//First establish initial relationships sibling relationships. This is important to do 
			//now so that the rest can raycast without error...
			Relationships[MyCoordinate.TrianglesIndex * 3] = new LNX_VertexRelationship(
				this, nvmsh.Triangles[MyCoordinate.TrianglesIndex].Verts[0], nvmsh
			);
			Relationships[(MyCoordinate.TrianglesIndex * 3) + 1] = new LNX_VertexRelationship(
				this, nvmsh.Triangles[MyCoordinate.TrianglesIndex].Verts[1], nvmsh
			);
			Relationships[(MyCoordinate.TrianglesIndex * 3) + 2] = new LNX_VertexRelationship(
				this, nvmsh.Triangles[MyCoordinate.TrianglesIndex].Verts[2], nvmsh
			);
			*/
			#endregion
		}

		public void AdoptValues( LNX_Vertex vert )
		{
			V_Position = vert.V_Position;
			originalPosition = vert.originalPosition;
			v_surfaceNormal_cached = vert.v_surfaceNormal_cached;
			v_triCenter_cached = vert.v_triCenter_cached;

			MyCoordinate = vert.MyCoordinate;

			Relationships = vert.Relationships;
			SharedVertexCoordinates = vert.SharedVertexCoordinates;

		}

		public void TriIndexChanged( int newIndex )
		{
			MyCoordinate = new LNX_ComponentCoordinate( newIndex, MyCoordinate.ComponentIndex );
		}

		public void CreateRelationships( LNX_NavMesh nvmsh ) //todo: unit test
		{
			Relationships = new LNX_VertexRelationship[nvmsh.Triangles.Length * 3];
			List<LNX_ComponentCoordinate> temp_sharedVrtCoords = new List<LNX_ComponentCoordinate>();

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

				Relationships[(i*3)] = new LNX_VertexRelationship( this, nvmsh.Triangles[i].Verts[0], nvmsh ); //Error trace
				//Debug.Log($"created vert rel {i*3}\n{Relationships[i*3]}...");
				if ( nvmsh.Triangles[i].Verts[0].V_Position == V_Position )
				{
					temp_sharedVrtCoords.Add( nvmsh.Triangles[i].Verts[0].MyCoordinate );
				}

				Relationships[(i*3)+1] = new LNX_VertexRelationship( this, nvmsh.Triangles[i].Verts[1], nvmsh );
				//Debug.Log($"created vert rel {(i * 3)+1}\n{Relationships[(i * 3)+1]}...");
				if ( nvmsh.Triangles[i].Verts[1].V_Position == V_Position )
				{
					temp_sharedVrtCoords.Add( nvmsh.Triangles[i].Verts[1].MyCoordinate );
				}

				Relationships[(i*3)+2] = new LNX_VertexRelationship( this, nvmsh.Triangles[i].Verts[2], nvmsh );
				//Debug.Log($"created vert rel {(i * 3) + 2}\n{Relationships[(i * 3)+2]}...");
				if ( nvmsh.Triangles[i].Verts[2].V_Position == V_Position )
				{
					temp_sharedVrtCoords.Add(nvmsh.Triangles[i].Verts[2].MyCoordinate);
				}
			}

			SharedVertexCoordinates = temp_sharedVrtCoords.ToArray();
		}

		public void CalculatePathing( LNX_NavMesh nm )
		{
			for( int i = 0; i < Relationships.Length; i++ )
			{
				Relationships[i].CalculatePathing( nm, this );
			}
		}

		#region API METHODS ------------------------------------------------------------
		[NonSerialized] public string DBG_IsInCenterSweep;
		public bool IsInCenterSweep( Vector3 pos )
		{
			DBG_IsInCenterSweep = $"Vert{MyCoordinate.ComponentIndex}.{nameof(IsInCenterSweep)}({pos}) " +
				$"report...\n";

			Vector3 vToPos = Vector3.Normalize( LNX_Utils.FlatVector(pos, v_surfaceNormal_cached) - V_flattenedPosition );

			//Debug.Log($"ERRORSPOT. coord: '{MyCoordinate}'. relLength: '{Relationships.Length}'. 1stSibINdx should be: " +
				//$"'{(MyCoordinate.ComponentIndex == 0 ?	(MyCoordinate.TrianglesIndex * 3) + 1 : MyCoordinate.TrianglesIndex * 3)}'...");
			
			Vector3 v_to0_flat = LNX_Utils.FlatVector(V_ToFirstSiblingVert, v_surfaceNormal_cached).normalized; //ERRORTRACE 10 (FINAL)
			Vector3 v_to1_flat = LNX_Utils.FlatVector(V_ToSecondSiblingVert, v_surfaceNormal_cached).normalized;

			DBG_IsInCenterSweep += $"using vto vector: '{vToPos}' and nrml: '{v_surfaceNormal_cached}'\n" +
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
		#endregion

		#region RELATIONAL METHODS----------------------------------------------
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

		public bool AreSiblings( LNX_ComponentCoordinate otherVertCoordinate )
		{
			return MyCoordinate.TrianglesIndex > -1 &&
				otherVertCoordinate.TrianglesIndex > -1 &&
				MyCoordinate.TrianglesIndex == otherVertCoordinate.TrianglesIndex;
		}

		public bool AreSiblings( LNX_Vertex otherVert )
		{
			return MyCoordinate.TrianglesIndex > -1 && 
				otherVert.MyCoordinate.TrianglesIndex > -1 && 
				MyCoordinate.TrianglesIndex == otherVert.MyCoordinate.TrianglesIndex;
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

		#region HELPERS --------------------------------------------------
		public string GetCurrentInfoString()
		{
			return $"Vert.{nameof(SayCurrentInfo)}()\n" +
				$"{nameof(MyCoordinate)}: '{MyCoordinate}'\n" +
				$"{nameof(V_Position)}: '{V_Position}'\n" +
				$"{nameof(originalPosition)}: '{originalPosition}'\n" +
				$"{nameof(v_surfaceNormal_cached)}: '{v_surfaceNormal_cached}'\n" +
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

		public string GetAnomolyString()
		{
			string returnString = string.Empty;

			if ( MyCoordinate.TrianglesIndex < 0 || MyCoordinate.ComponentIndex < 0 )
			{
				returnString += $"{nameof(MyCoordinate)}: '{MyCoordinate}'\n";
			}

			if (V_Position == Vector3.zero)
			{
				returnString += $"{nameof(V_Position)}: '{V_Position}'\n";
			}

			if (originalPosition == Vector3.zero)
			{
				returnString += $"{nameof(originalPosition)}: '{originalPosition}'\n";
			}

			if (Index_VisMesh_Vertices == -1 )
			{
				returnString += $"{nameof(Index_VisMesh_Vertices)}: '{Index_VisMesh_Vertices}'\n";
			}

			if (v_triCenter_cached == Vector3.zero)
			{
				returnString += $"{nameof(v_triCenter_cached)}: '{v_triCenter_cached}'\n";
			}

			if (v_surfaceNormal_cached == Vector3.zero)
			{
				returnString += $"{nameof(v_surfaceNormal_cached)}: '{v_surfaceNormal_cached}'\n";
			}

			if ( Relationships == null || Relationships.Length == 0 )
			{
				returnString += $"{nameof(Relationships)} collection not set\n";
			}

			return returnString;
		}

		public string GetRelationalString()
		{
			return $"Vert[{ComponentIndex}].{nameof(GetRelationalString)}()\n" +
				$"{nameof(Relationships)} count: '{Relationships.Length}'\n" +
				$"{nameof(FirstSiblingRelationship)}: '{FirstSiblingRelationship}'\n" +
				$"{nameof(SecondSiblingRelationship)}: '{SecondSiblingRelationship}'\n" +

				$"";
		}		
		#endregion
	}
}