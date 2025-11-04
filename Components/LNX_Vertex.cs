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
		public float AngleAtBend => Vector3.Angle(V_ToFirstSiblingVert.normalized, V_ToSecondSiblingVert.normalized); //~~

		/// <summary>Aangle at the inner corner of the triangle at this vertex assuming all verts are flattneed.</summary>
		public float AngleAtBend_flattened
		{
			get
			{
				return Vector3.Angle(
					LNX_Utils.FlatVector(V_ToFirstSiblingVert.normalized, v_surfaceNormal_cached), //~~
					LNX_Utils.FlatVector(V_ToSecondSiblingVert.normalized, v_surfaceNormal_cached) //~~
				);
			}
		}

		/// <summary>
		/// Signed angle going from V_ToFirstSiblingVert to V_ToSecondSiblingVert. You can use -SignedAngle (negative) to 
		/// get the signed angle from V_ToSecondSiblingVert to V_ToFirstSiblingVert.
		/// </summary>
		public float SignedAngle => Vector3.SignedAngle( V_ToFirstSiblingVert_flat, V_ToSecondSiblingVert_flat, v_surfaceNormal_cached );

		/// <summary>Cached center vector for the owning triangle. This is for exposed property calculation </summary>
		[SerializeField, HideInInspector] private Vector3 v_triCenter_cached;

		/// <summary>Normalized directional vector pointing from this vertex to the center of it's triangle </summary>
		[HideInInspector] public Vector3 v_toCenter => Vector3.Normalize( v_triCenter_cached - V_Position );

		[HideInInspector] public float DistanceToCenter => Vector3.Distance( V_Position, v_triCenter_cached );

		/// <summary>Should be the same as the Surface Orientation setting for the navmesh that this vert's triangle belongs to.</summary>
		[SerializeField, HideInInspector] private Vector3 v_surfaceNormal_cached;
		public Vector3 CachedSurfaceNormal => v_surfaceNormal_cached;

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

		/// <summary>Index where you can find this vertex from the perspective of other Vertices.</summary>
		public int Index_Relational => (MyCoordinate.TrianglesIndex * 3) + MyCoordinate.ComponentIndex;

		//todo: all these index properties need to be unit tested for accuracy
		public int Index_FirstSiblingVert => MyCoordinate.ComponentIndex == 0 ? 1 : 0;
		//private int firstSiblingRelationshipIndex => MyCoordinate.ComponentIndex == 0 ? (MyCoordinate.TrianglesIndex * 3) + 1 : MyCoordinate.TrianglesIndex * 3;
		private int firstSiblingRelationshipIndex => (MyCoordinate.TrianglesIndex * 3) + Index_FirstSiblingVert;

		public LNX_VertexRelationship FirstSiblingRelationship
		{
			get
			{
				/*return MyCoordinate.ComponentIndex == 0 ?
					Relationships[(MyCoordinate.TrianglesIndex * 3) + 1] : Relationships[MyCoordinate.TrianglesIndex * 3];*/

				return Relationships[firstSiblingRelationshipIndex];
			}
		}

		public int Index_SecondSiblingVert => MyCoordinate.ComponentIndex == 2 ? 1 : 2;
		private int secondSiblingRelationshipIndex => (MyCoordinate.TrianglesIndex * 3) + Index_SecondSiblingVert;

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
				return Relationships[firstSiblingRelationshipIndex].V_to;
			}
		}
		public Vector3 V_ToFirstSiblingVert_flat
		{
			get
			{
				return LNX_Utils.FlatVector(V_ToFirstSiblingVert).normalized;
			}
		}
		/// <summary> Returns a localized (0 origin) vector pointing from this vert to it's first sibling vert. </summary>
		public Vector3 V_ToSecondSiblingVert
		{
			get
			{
				return Relationships[secondSiblingRelationshipIndex].V_to;
			}
		}
		public Vector3 V_ToSecondSiblingVert_flat
		{
			get
			{
				return LNX_Utils.FlatVector(V_ToSecondSiblingVert).normalized;
			}
		}

		public float DistToFirstSiblingVert_path => FirstSiblingRelationship.PathDistance;
		public float DistToSecondSiblingVert_path => SecondSiblingRelationship.PathDistance;
		public float DistToFirstSiblingVert_straight => FirstSiblingRelationship.V_to.magnitude;
		public float DistToSecondSiblingVert_straight => SecondSiblingRelationship.V_to.magnitude;

		/// <summary>Collection of vertices sharing the same space as this one.</summary>
		public LNX_ComponentCoordinate[] SharedVertexCoordinates;

		public LNX_Vertex (List<LNX_AtomicTriangle> atomicTris, int triIndx, int cmpntIndx, LNX_NavMesh nvmsh )
        {
			//Debug.Log($"vert[{triIndx}][{cmpntIndx}] ctor...");

			MyCoordinate = new LNX_ComponentCoordinate( triIndx, cmpntIndx );

			if ( cmpntIndx == 0 )
			{
				V_Position = atomicTris[triIndx].VertPos0_current;
				originalPosition = atomicTris[triIndx].VertPos0_orig;
			}
			else if ( cmpntIndx == 1 )
			{
				V_Position = atomicTris[triIndx].VertPos1_current;
				originalPosition = atomicTris[triIndx].VertPos1_orig;
			}
			else
			{
				V_Position = atomicTris[triIndx].VertPos2_current;
				originalPosition = atomicTris[triIndx].VertPos2_orig;
			}

			v_surfaceNormal_cached = nvmsh.V_SurfaceOrientation;

			//v_triCenter_cached = atomicTris[triIndx].Center;
			v_triCenter_cached = (atomicTris[triIndx].VertPos0_current + atomicTris[triIndx].VertPos1_current + atomicTris[triIndx].VertPos2_current) / 3f;

			//Debug.Log($"{nameof(v_triCenter_cached)}: '{v_triCenter_cached}', from atomic: '{atomicTris[triIndx].Center}'");

			if( v_triCenter_cached == Vector3.zero )
			{
				Debug.LogError($"{nameof(v_triCenter_cached)}: '{v_triCenter_cached}', from atomic: '{atomicTris[triIndx].Center}'");
			}

			Index_VisMesh_Vertices = -1;
		}

		public void CalculateDerivedInfo(LNX_Triangle tri, LNX_NavMesh nvmsh )
		{
			#region ESTABLISH SIBLING RELATIONSHIPS FIRST --------------------------------------------------			
			Relationships = new LNX_VertexRelationship[nvmsh.Triangles.Length * 3];
			//First establish initial relationships sibling relationships. This is important to do 
			//now so that the rest can raycast without error...
			
			Relationships[firstSiblingRelationshipIndex] = new LNX_VertexRelationship(
				this, tri, Index_FirstSiblingVert
			);
			Relationships[secondSiblingRelationshipIndex] = new LNX_VertexRelationship(
				this, tri, Index_SecondSiblingVert
			);
			
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

		public void CreateRelationships( LNX_NavMesh nvmsh ) //todo: unit test
		{
			//Debug.Log( $"vert[{MyCoordinate}].{nameof(CreateRelationships)}()---" );

			//DateTime dt_start = DateTime.Now;
			//why does this take so long?
			Relationships = new LNX_VertexRelationship[nvmsh.Triangles.Length * 3];

			#region ESTABLISH SIBLING RELATIONSHIPS FIRST --------------------------------------------------			
			//Note: Even though I've already done this in the CalculateDerivedInfo() method, I need to do 
			//this again here, because those relationships are gone now that I've re-initialized the
			//Relationships collection above...
			Relationships[firstSiblingRelationshipIndex] = new LNX_VertexRelationship(
				this, nvmsh.GetTriangle(this), Index_FirstSiblingVert
			);
			Relationships[secondSiblingRelationshipIndex] = new LNX_VertexRelationship(
				this, nvmsh.GetTriangle(this), Index_SecondSiblingVert
			);
			#endregion
			//Debug.Log($"creating sibling relationships took: '{DateTime.Now.Subtract(dt_start)}'");

			List<LNX_ComponentCoordinate> temp_sharedVrtCoords = new List<LNX_ComponentCoordinate>();

			for ( int i = 0; i < nvmsh.Triangles.Length; i++ ) //Note: Before optimization this look took about 1.6 seconds
			{
				if( i == MyCoordinate.TrianglesIndex )
				{
					continue;
				}

				Relationships[(i*3)] = new LNX_VertexRelationship( this, nvmsh.Triangles[i].Verts[0], nvmsh, true ); 
				Relationships[(i*3)+1] = new LNX_VertexRelationship( this, nvmsh.Triangles[i].Verts[1], nvmsh, true);
				Relationships[(i*3)+2] = new LNX_VertexRelationship( this, nvmsh.Triangles[i].Verts[2], nvmsh, true);

				if ( nvmsh.Triangles[i].Verts[0].V_Position == V_Position )
				{
					temp_sharedVrtCoords.Add( nvmsh.Triangles[i].Verts[0].MyCoordinate );
				}
				else if ( nvmsh.Triangles[i].Verts[1].V_Position == V_Position )
				{
					temp_sharedVrtCoords.Add( nvmsh.Triangles[i].Verts[1].MyCoordinate );
				}
				else if ( nvmsh.Triangles[i].Verts[2].V_Position == V_Position )
				{
					temp_sharedVrtCoords.Add(nvmsh.Triangles[i].Verts[2].MyCoordinate);
				}
			}

			//Debug.Log($"creating the rest took: '{DateTime.Now.Subtract(dt_start)}'");

			SharedVertexCoordinates = temp_sharedVrtCoords.ToArray();
		}
		public void TriIndexChanged(int newIndex)
		{
			MyCoordinate = new LNX_ComponentCoordinate(newIndex, MyCoordinate.ComponentIndex);
		}

		#region API METHODS ------------------------------------------------------------
		[NonSerialized] public string DBG_IsInCenterSweep;
		public bool IsInCenterSweep( Vector3 pos, Vector3 nrml )
		{
			DBG_IsInCenterSweep = $"Vert{MyCoordinate.ComponentIndex}.{nameof(IsInCenterSweep)}({pos}) " +
				$"report...\n";

			Vector3 vToPos_flat = Vector3.Normalize( LNX_Utils.FlatVector(pos, nrml) - V_flattenedPosition );
			
			Vector3 v_toFirstSibling_flat = LNX_Utils.FlatVector(V_ToFirstSiblingVert, nrml).normalized;
			Vector3 v_toSecondSibling_flat = LNX_Utils.FlatVector(V_ToSecondSiblingVert, nrml).normalized;

			DBG_IsInCenterSweep += $"using vto vector: '{vToPos_flat}' and nrml: '{nrml}'\n" +
				$"{nameof(AngleAtBend_flattened)}: '{AngleAtBend_flattened}'\n" +
				$"first ang: '{Vector3.Angle(v_toFirstSibling_flat, vToPos_flat)}', " +
				$"second ang: '{Vector3.Angle(v_toSecondSibling_flat, vToPos_flat)}'\n" +
				$"diff0: '{AngleAtBend_flattened - Vector3.Angle(v_toFirstSibling_flat, vToPos_flat)}'\n" +
				$"diff1: '{AngleAtBend_flattened - Vector3.Angle(v_toSecondSibling_flat, vToPos_flat)}'\n";
			
			if ( 
				Vector3.Angle(v_toFirstSibling_flat, vToPos_flat) > (AngleAtBend_flattened + 0.001f) ||
				Vector3.Angle(v_toSecondSibling_flat, vToPos_flat) > (AngleAtBend_flattened + 0.001f)
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
		public bool SharesVertSpaceWithTri( int triIndex )
		{
			if( triIndex == MyCoordinate.TrianglesIndex || SharedVertexCoordinates == null || SharedVertexCoordinates.Length == 0)
			{
				return false;
			}

			for ( int i = 0; i < SharedVertexCoordinates.Length; i++ )
			{
				if ( SharedVertexCoordinates[i].TrianglesIndex == triIndex )
				{
					return true;
				}
			}

			return false;
		}

		public bool SharesVertSpace( LNX_Vertex vert )
		{
			if( SharedVertexCoordinates == null || SharedVertexCoordinates.Length == 0 )
			{
				Debug.LogWarning($"LNX WARNING! {nameof(SharedVertexCoordinates)} collection " +
					$"not set up.");
				return false;
			}

			for ( int i = 0; i < SharedVertexCoordinates.Length; i++ )
			{
				if (SharedVertexCoordinates[i] == vert.MyCoordinate)
				{
					return true;
				}
			}

			return false;
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

		/*
		public List<LNX_Vertex> GetVisibleVerts(LNX_NavMesh nm )
		{

		}
		*/

		public LNX_Path GetPathTo( LNX_ProjectionHit hit, LNX_NavMesh nm )
		{
			List<LNX_ProjectionHit> pathHits = new List<LNX_ProjectionHit>() { new LNX_ProjectionHit(V_Position, MyCoordinate) };

			bool amPinging = true;
			while ( amPinging )
			{

			}

			return new LNX_Path( pathHits, nm );
		}

		#region HELPERS --------------------------------------------------
		public string GetCurrentInfoString()
		{
			return $"Vert.{nameof(SayCurrentInfo)}()\n" +
				$"{nameof(MyCoordinate)}: '{MyCoordinate}'\n" +
				$"{nameof(V_Position)}: '{V_Position}'\n" +
				$"{nameof(originalPosition)}: '{originalPosition}'\n" +

				$"{nameof(v_triCenter_cached)}: '{v_triCenter_cached}'\n" +
				$"{nameof(v_surfaceNormal_cached)}: '{v_surfaceNormal_cached}'\n" +
				$"{nameof(Relationships)} count: '{Relationships.Length}\n" +
				$"{nameof(Index_VisMesh_Vertices)}: '{Index_VisMesh_Vertices}'\n" +
				$"{nameof(AngleAtBend)}: '{AngleAtBend}'\n" +
				$"{nameof(AngleAtBend_flattened)}: '{AngleAtBend_flattened}'\n" +

				$"{nameof(Index_FirstSiblingVert)}: '{Index_FirstSiblingVert}'\n" +
				$"{nameof(firstSiblingRelationshipIndex)}: '{firstSiblingRelationshipIndex}'\n" +
				$"{nameof(V_ToFirstSiblingVert)}: '{V_ToFirstSiblingVert}'\n" +

				$"{nameof(Index_SecondSiblingVert)}: '{Index_SecondSiblingVert}'\n" +
				$"{nameof(secondSiblingRelationshipIndex)}: '{secondSiblingRelationshipIndex}'\n" +
				$"{nameof(V_ToSecondSiblingVert)}: '{V_ToSecondSiblingVert}'\n" +

				$"{nameof(SharedVertexCoordinates)} length: '{SharedVertexCoordinates.Length}'\n" +

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

		public string GetAnomolyString(LNX_NavMesh nm )
		{
			string returnString = string.Empty;

			if (
				MyCoordinate.TrianglesIndex < 0 ||
				MyCoordinate.TrianglesIndex > nm.Triangles.Length - 1 ||
				MyCoordinate.ComponentIndex < 0 ||
				MyCoordinate.ComponentIndex > 2
			)
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

			if (CachedSurfaceNormal == Vector3.zero)
			{
				returnString += $"{nameof(CachedSurfaceNormal)}: '{CachedSurfaceNormal}'\n";
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

			if( AngleAtBend > 180 || AngleAtBend < float.MinValue )
			{
				returnString += $"{nameof(AngleAtBend)}: '{AngleAtBend}'\n";
			}

			if ( DistanceToCenter <= 0 )
			{
				returnString += $"{nameof(DistanceToCenter)} was '{DistanceToCenter}'\n";
			}

			#region RElATIONAL------------------------------------------------
			if ( Relationships == null || Relationships.Length == 0 )
			{
				returnString += $"{nameof(Relationships)} collection not set\n";
			}

			

			if ( FirstSiblingRelationship.V_to == Vector3.zero )
			{
				returnString += $"{nameof(FirstSiblingRelationship)}.{nameof(FirstSiblingRelationship.V_to)} was '{FirstSiblingRelationship.V_to}'\n";
			}

			if( FirstSiblingRelationship.V_to != V_ToFirstSiblingVert )
			{
				returnString += $"{nameof(FirstSiblingRelationship)}.{nameof(FirstSiblingRelationship.V_to)} at '{FirstSiblingRelationship.V_to}' was NOT equal to " +
					$"{nameof(V_ToFirstSiblingVert)} at '{V_ToFirstSiblingVert}'\n";
			}

			if (SecondSiblingRelationship.V_to == Vector3.zero)
			{
				returnString += $"{nameof(SecondSiblingRelationship)}.{nameof(SecondSiblingRelationship.V_to)} was '{SecondSiblingRelationship.V_to}'\n";
			}

			if ( FirstSiblingRelationship.V_to == SecondSiblingRelationship.V_to )
			{
				returnString += $"{nameof(FirstSiblingRelationship)}.{nameof(FirstSiblingRelationship.V_to)} was Equal to {nameof(SecondSiblingRelationship.V_to)}\n";
			}

			if ( V_ToFirstSiblingVert == V_ToSecondSiblingVert )
			{
				returnString += $"{nameof(V_ToFirstSiblingVert)} was Equal to {nameof(V_ToSecondSiblingVert)}\n";
			}
			#endregion

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