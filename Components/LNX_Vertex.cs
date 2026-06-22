using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_Vertex
	{
		#region IDENTITY/LOCATING ============================================================
		/// <summary>Current position of this vertex in 3d space. Potentially modified after initial
		/// construction of the tri this vertex belongs to.</summary>
		public Vector3 V_Position;
		public Vector3 V_flattenedPosition => LNX_Utils.FlatVector( V_Position, v_navmeshProjectionDirection_cached );

		[SerializeField, HideInInspector] private Vector3 originalPosition;
		/// <summary>Initial position, in 3d space, of this vertex upon creation of it's owning triangle, 
		/// before any modifications </summary>
		public Vector3 OriginalPosition => originalPosition;

		public LNX_ComponentCoordinate MyCoordinate;
		public int TriangleIndex => MyCoordinate.TrianglesIndex;
		public int ComponentIndex => MyCoordinate.ComponentIndex;

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
		#endregion--------------------------------------------------------

		//[Header("CALCULATED/DERIVED")] //---------------------------------------------------------------
		/// <summary>Aangle at the inner corner of the triangle at this vertex.</summary>
		public float AngleAtBend => Vector3.Angle(V_ToFirstSiblingVert.normalized, V_ToSecondSiblingVert.normalized); //~~

		/// <summary>Aangle at the inner corner of the triangle at this vertex assuming all verts are flattneed.</summary>
		public float AngleAtBend_flattened
		{
			get
			{
				return Vector3.Angle(
					LNX_Utils.FlatVector(V_ToFirstSiblingVert.normalized, v_navmeshProjectionDirection_cached), //~~
					LNX_Utils.FlatVector(V_ToSecondSiblingVert.normalized, v_navmeshProjectionDirection_cached) //~~
				);
			}
		}

		public float FloorAngle_toFirstSiblingVert => Vector3.Angle( V_ToFirstSiblingVert, V_ToFirstSiblingVert_flat );
		public float FloorAngle_toSecondSiblingVert => Vector3.Angle(V_ToSecondSiblingVert, V_ToSecondSiblingVert_flat);


		/// <summary>
		/// Signed angle going from V_ToFirstSiblingVert to V_ToSecondSiblingVert. You can use -SignedAngle (negative) to 
		/// get the signed angle from V_ToSecondSiblingVert to V_ToFirstSiblingVert.
		/// </summary>
		public float SignedAngle => Vector3.SignedAngle( V_ToFirstSiblingVert_flat, V_ToSecondSiblingVert_flat, v_navmeshProjectionDirection_cached );

		/// <summary>Cached center vector for the owning triangle. This is for exposed property calculation </summary>
		[SerializeField, HideInInspector] private Vector3 v_triCenter_cached;

		/// <summary>Normalized directional vector pointing from this vertex to the center of it's triangle </summary>
		[HideInInspector] public Vector3 v_toCenter => Vector3.Normalize( v_triCenter_cached - V_Position );

		[HideInInspector] public float DistanceToCenter => Vector3.Distance( V_Position, v_triCenter_cached );

		/// <summary>Should be the same as the Surface Orientation setting for the navmesh that this vert's triangle belongs to.</summary>
		[SerializeField, HideInInspector] private Vector3 v_navmeshProjectionDirection_cached;
		public Vector3 CachedSurfaceNormal => v_navmeshProjectionDirection_cached;


		// TRUTH...........
		public bool AmModified
		{
			get {  return V_Position != originalPosition; }
		}

		//public bool AmOnTerminalEdge; //todo: Implement

		#region RELATIONAL ======================================================================
		[HideInInspector] public LNX_VertexRelationship[] Relationships;

		/// <summary>Index where you can find this vertex from the perspective of other Vertices.</summary>
		public int Index_Relational => (MyCoordinate.TrianglesIndex * 3) + MyCoordinate.ComponentIndex;

		//todo: all these index properties need to be unit tested for accuracy
		public int Index_FirstSiblingVert => MyCoordinate.ComponentIndex == 0 ? 1 : 0;
		public LNX_ComponentCoordinate Coordinate_FirstSibling => new LNX_ComponentCoordinate(MyCoordinate.TrianglesIndex, Index_FirstSiblingVert);
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
		public LNX_ComponentCoordinate Coordinate_SecondSibling => new LNX_ComponentCoordinate(MyCoordinate.TrianglesIndex, Index_SecondSiblingVert);

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

		#region EDGE =======================================================================
		/// <summary>Index of 'first' edge (based on index in the edges array) on the containing triangle, that forms this vertex. Note: This index will be the same as the first sibling vertex index </summary>
		public int Index_FirstFormingEdge => MyCoordinate.ComponentIndex == 0 ? 1 : 0;
		/// <summary>Index of 'second' edge (based on index in the edges array) on the containing triangle, that forms this vertex. Note: This index will be the same as the second sibling vertex index </summary>
		public int Index_SecondFormingEdge => MyCoordinate.ComponentIndex == 2 ? 1 : 2;
		#endregion

		/// <summary>Collection of vertices sharing the same space as this one.</summary>
		public LNX_ComponentCoordinate[] SharedVertexCoordinates;
		#endregion --------------------------------------------------------------------------------

		public LNX_Vertex ( LNX_Triangle tri, List<LNX_AtomicTriangle> atomicTris, int triIndx, int cmpntIndx )
        {
			//Debug.Log($"vert[{triIndx}][{cmpntIndx}] ctor...");

			MyCoordinate = new LNX_ComponentCoordinate( triIndx, cmpntIndx );
			Vector3 firstSiblingPos = Vector3.zero;
			Vector3 secondSiblingPos = Vector3.zero;

			if ( cmpntIndx == 0 )
			{
				V_Position = atomicTris[triIndx].VertPos0_current;
				originalPosition = atomicTris[triIndx].VertPos0_orig;

				firstSiblingPos = atomicTris[triIndx].VertPos1_current;
				secondSiblingPos = atomicTris[triIndx].VertPos2_current;
			}
			else if ( cmpntIndx == 1 )
			{
				V_Position = atomicTris[triIndx].VertPos1_current;
				originalPosition = atomicTris[triIndx].VertPos1_orig;

				firstSiblingPos = atomicTris[triIndx].VertPos0_current;
				secondSiblingPos = atomicTris[triIndx].VertPos2_current;
			}
			else //( cmpntIndx == 2 )
			{
				V_Position = atomicTris[triIndx].VertPos2_current;
				originalPosition = atomicTris[triIndx].VertPos2_orig;

				firstSiblingPos = atomicTris[triIndx].VertPos0_current;
				secondSiblingPos = atomicTris[triIndx].VertPos1_current;
			}

			v_navmeshProjectionDirection_cached = tri.V_NavmeshProjectionDirection_cached;

			v_triCenter_cached = atomicTris[triIndx].Center;

			if( v_triCenter_cached == Vector3.zero )
			{
				Debug.LogError($"{nameof(v_triCenter_cached)}: '{v_triCenter_cached}', from atomic: '{atomicTris[triIndx].Center}'");
			}

			Index_VisMesh_Vertices = -1;

			Relationships = new LNX_VertexRelationship[atomicTris.Count * 3];

			Relationships[firstSiblingRelationshipIndex] = new LNX_VertexRelationship(
				new LNX_Path(
					v_navmeshProjectionDirection_cached,
					new LNX_NavmeshHit(this, tri.V_PathingNormal),
					new LNX_NavmeshHit(
						firstSiblingPos, tri.V_PathingNormal, 
						MyCoordinate.TrianglesIndex, Coordinate_FirstSibling.ComponentIndex, -1
					)
				)
			);

			Relationships[secondSiblingRelationshipIndex] = new LNX_VertexRelationship(
				new LNX_Path(
					v_navmeshProjectionDirection_cached,
					new LNX_NavmeshHit(this, tri.V_PathingNormal),
					new LNX_NavmeshHit(
						secondSiblingPos, tri.V_PathingNormal, 
						MyCoordinate.TrianglesIndex, Coordinate_SecondSibling.ComponentIndex, -1
					)
				)
			);
		}

		public void CalculateDerivedInfo(LNX_Triangle tri, LNX_NavMesh nvmsh )
		{
			//todo: take out the tri parameter, because it can be derived from the navmesh parameter along with the 
			// cached triangle coordinate, then efficiency test to see what difference it makes.

			#region ESTABLISH SIBLING RELATIONSHIPS FIRST --------------------------------------------------			
			/*
			if( Relationships == null || Relationships.Length == 0 || 
				Relationships.Length != (nvmsh.Triangles.Length * 3) )
			{
				Relationships = new LNX_VertexRelationship[nvmsh.Triangles.Length * 3];
			}
			*/

			//First establish initial relationships sibling relationships. This is important to do 
			//now so that the rest can raycast without error...
			
			/* //todo: dws
			Relationships[firstSiblingRelationshipIndex] = new LNX_VertexRelationship(
				this, tri, Index_FirstSiblingVert
			);
			Relationships[secondSiblingRelationshipIndex] = new LNX_VertexRelationship(
				this, tri, Index_SecondSiblingVert
			);
			*/

			/*
			Relationships[firstSiblingRelationshipIndex] = new LNX_VertexRelationship(
				new LNX_Path(
					v_surfaceNormal_cached,
					new LNX_NavmeshHit(this),
					new LNX_NavmeshHit(tri.Verts[Index_FirstSiblingVert])
				)
			);

			Relationships[secondSiblingRelationshipIndex] = new LNX_VertexRelationship(
				new LNX_Path(
					v_surfaceNormal_cached,
					new LNX_NavmeshHit(this),
					new LNX_NavmeshHit(tri.Verts[Index_SecondSiblingVert])
				)
			);
			*/
			#endregion
		}

		public void CreateRelationships( LNX_NavMesh nvmsh ) //todo: unit test
		{
			//Debug.Log( $"vert[{MyCoordinate}].{nameof(CreateRelationships)}()---" );

			//DateTime dt_start = DateTime.Now;
			//why does this take so long?
			Relationships = new LNX_VertexRelationship[nvmsh.Triangles.Length * 3];
			Vector3 clcltdPthngNrml = nvmsh.Triangles[TriangleIndex].V_PathingNormal;

			#region ESTABLISH SIBLING RELATIONSHIPS FIRST --------------------------------------------------			
			//Note: Even though I've already done this earlier, I need to do this again here, because I'm
			//making this method so that it can be used to completely re-recreate the relationships, so
			//it makes the collection new, and so those relationships are gone now that I've re-initialized the
			//Relationships collection above...

			Relationships[firstSiblingRelationshipIndex] = new LNX_VertexRelationship(
				new LNX_Path(
					v_navmeshProjectionDirection_cached,
					new LNX_NavmeshHit(this, clcltdPthngNrml),
					new LNX_NavmeshHit(nvmsh.Triangles[TriangleIndex].Verts[Index_FirstSiblingVert], clcltdPthngNrml)
				)
			);

			Relationships[secondSiblingRelationshipIndex] = new LNX_VertexRelationship(
				new LNX_Path(
					v_navmeshProjectionDirection_cached,
					new LNX_NavmeshHit(this, clcltdPthngNrml),
					new LNX_NavmeshHit(nvmsh.Triangles[TriangleIndex].Verts[Index_SecondSiblingVert], clcltdPthngNrml)
				)
			);
			#endregion
			//Debug.Log($"creating sibling relationships took: '{DateTime.Now.Subtract(dt_start)}'");

			#region NEXT, CALCULATE 'NEIGHBOR' VERT RELATIONSHIPS
			//Note: This needs to be done before the rest of the relationships so that raycasting using a vert 
			//as a start point will work.
			List<LNX_ComponentCoordinate> temp_sharedVrtCoords = new List<LNX_ComponentCoordinate>();
			for (int i = 0; i < nvmsh.Triangles.Length; i++)
			{
				if (i == MyCoordinate.TrianglesIndex)
				{
					continue;
				}

				for (int i_vrts = 0; i_vrts < 3; i_vrts++)
				{
					if (nvmsh.Triangles[i].Verts[i_vrts].V_Position == V_Position)
					{
						temp_sharedVrtCoords.Add(nvmsh.Triangles[i].Verts[i_vrts].MyCoordinate);
						//Debug.LogWarning("it happened a!");
						//Go ahead and make the other relationships...
						Relationships[(i * 3) + 0] = new LNX_VertexRelationship(
							new LNX_Path(
								CachedSurfaceNormal, new LNX_NavmeshHit(this, clcltdPthngNrml),
								new LNX_NavmeshHit(nvmsh.Triangles[i].Verts[0], nvmsh.Triangles[i].V_PathingNormal)
							)
						);
						Relationships[(i * 3) + 1] = new LNX_VertexRelationship(
							new LNX_Path(
								CachedSurfaceNormal, new LNX_NavmeshHit(this, clcltdPthngNrml),
								new LNX_NavmeshHit(nvmsh.Triangles[i].Verts[1], nvmsh.Triangles[i].V_PathingNormal)
							)
						);
						Relationships[(i * 3) + 2] = new LNX_VertexRelationship(
							new LNX_Path(
								CachedSurfaceNormal, new LNX_NavmeshHit(this, clcltdPthngNrml),
								new LNX_NavmeshHit(nvmsh.Triangles[i].Verts[2], nvmsh.Triangles[i].V_PathingNormal)
							)
						);

						break;
					}
					else if (nvmsh.Triangles[i].Verts[i_vrts].V_Position ==
						nvmsh.Triangles[MyCoordinate.TrianglesIndex].Verts[Index_FirstSiblingVert].V_Position
					)
					{//In this case, we have a vert that shares space with a sibling vert...
						
						//Debug.LogWarning("it happened b!");

						Relationships[(i * 3) + i_vrts] = new LNX_VertexRelationship(
							new LNX_Path(
								CachedSurfaceNormal, 
								new LNX_NavmeshHit(this, clcltdPthngNrml),
								new LNX_NavmeshHit(nvmsh.Triangles[i].Verts[i_vrts], nvmsh.Triangles[i].V_PathingNormal)
							)
						);
					}
					else if (nvmsh.Triangles[i].Verts[i_vrts].V_Position ==
						nvmsh.Triangles[MyCoordinate.TrianglesIndex].Verts[Index_SecondSiblingVert].V_Position
					)
					{
						//Debug.LogWarning("it happened c!");

						Relationships[(i * 3) + i_vrts] = new LNX_VertexRelationship(
							new LNX_Path(
								CachedSurfaceNormal, 
								new LNX_NavmeshHit(this, clcltdPthngNrml),
								new LNX_NavmeshHit(nvmsh.Triangles[i].Verts[i_vrts], nvmsh.Triangles[i].V_PathingNormal)
							)
						);
					}
				}
			}
			#endregion

			for ( int i = 0; i < nvmsh.Triangles.Length; i++ ) //Note: Before optimization this look took about 1.6 seconds
			{
				if( i == MyCoordinate.TrianglesIndex )
				{
					continue;
				}

				for (int i_vrts = 0; i_vrts < 3; i_vrts++)
				{
					if (nvmsh.Triangles[i].Verts[i_vrts].V_Position == V_Position ||
						nvmsh.Triangles[i].Verts[i_vrts].V_Position == nvmsh.Triangles[MyCoordinate.TrianglesIndex].Verts[Index_FirstSiblingVert].V_Position ||
						nvmsh.Triangles[i].Verts[i_vrts].V_Position == nvmsh.Triangles[MyCoordinate.TrianglesIndex].Verts[Index_SecondSiblingVert].V_Position
					)
					{
						continue; //because these are already logged above
					}

					Relationships[(i * 3) + i_vrts] = new LNX_VertexRelationship(
						this, nvmsh.Triangles[i].Verts[i_vrts], nvmsh, true
					);
				}
			}

			//Debug.Log($"creating the rest took: '{DateTime.Now.Subtract(dt_start)}'");

			SharedVertexCoordinates = temp_sharedVrtCoords.ToArray();
		}
		
		public Vector3 CalculatePathingNormal()
		{
			Debug.Log($"CalculatePathingNormal() relationships count: '{Relationships.Length}'");
			Vector3 nrml = Vector3.Cross(
				Vector3.Normalize(V_ToFirstSiblingVert),
				Vector3.Normalize(V_ToSecondSiblingVert)
			).normalized;
			if (Vector3.Dot(v_navmeshProjectionDirection_cached, nrml) > Vector3.Dot(v_navmeshProjectionDirection_cached, -nrml))
			{
				nrml = -nrml;
			}

			return nrml;
		}

		public void TriIndexChanged(int newIndex)
		{
			MyCoordinate = new LNX_ComponentCoordinate(newIndex, MyCoordinate.ComponentIndex);
		}

		#region API METHODS ------------------------------------------------------------
		public bool ProjectionIsInCenterSweep( Vector3 projection, bool includeOnPerim = false )
		{
			return LNX_Utils.AmInVectorCone(
				projection, V_ToFirstSiblingVert_flat, V_ToSecondSiblingVert_flat, 
				v_navmeshProjectionDirection_cached, includeOnPerim );
		}
		public bool ProjectionIsInCenterSweep_dbg(Vector3 projection, ref LNX_MethodDebugReport rprt, bool includeOnPerim = false)
		{
			rprt.StartMethod( $"v{ComponentIndex}.ProjectionIsInCenterSweep_dbg('{projection}', incldOnPrm: '{includeOnPerim}')");

			rprt.Log($"Passing off to util method...");
			bool rslt = LNX_Utils.AmInVectorCone_dbg(
				projection, V_ToFirstSiblingVert_flat, V_ToSecondSiblingVert_flat,
				v_navmeshProjectionDirection_cached, ref rprt, includeOnPerim);

			rprt.Log_And_End_Method($"returning: '{rslt}'...", "ProjectionIsInCenterSweep_dbg()");

			return rslt;
		}

		/// <summary>
		/// Returns a path to the supplied LNX_Vertex by fetching it from the relationships collection. This 
		/// will NOT work if called before the relationships collection has been properly set up.
		/// </summary>
		/// <param name="otherVert"></param>
		/// <returns></returns>
		public LNX_Path GetPathTo(LNX_Vertex otherVert)
		{
			return GetRelationship(otherVert).PathTo;
		}
		#endregion

		#region RELATIONAL METHODS----------------------------------------------
		/// <summary>
		/// Checks if a supplied triangle has a vert that shares space with this vert.
		/// </summary>
		/// <param name="tri"></param>
		/// <returns></returns>
		public bool SharesVertSpaceWithTri( LNX_Triangle tri )
		{
			if ( tri.Index_inCollection == MyCoordinate.TrianglesIndex )
			{
				return true;
			}

			if ( SharedVertexCoordinates != null && SharedVertexCoordinates.Length > 0 )
			{
				for ( int i = 0; i < SharedVertexCoordinates.Length; i++ )
				{
					if ( SharedVertexCoordinates[i].TrianglesIndex == tri.Index_inCollection )
					{
						return true;
					}
				}
			}
			else //fallback for when relational data isn't loaded...
			{
				if
				(
					tri.Verts[0].V_Position == V_Position ||
					tri.Verts[1].V_Position == V_Position ||
					tri.Verts[2].V_Position == V_Position
				)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks whether this vert has a shared vert with the triangle with the supplied index.
		/// <para>Note: Only use this method when you know that relational information has been 
		/// calculated, otherwise it won't likely work.</para>
		/// </summary>
		/// <param name="triIndx"></param>
		/// <returns></returns>
		public bool HasSharedVertViaTriIndex( int triIndx )
		{
			if ( triIndx == MyCoordinate.TrianglesIndex )
			{
				return true;
			}

			if (SharedVertexCoordinates != null && SharedVertexCoordinates.Length > 0)
			{
				for (int i = 0; i < SharedVertexCoordinates.Length; i++)
				{
					if (SharedVertexCoordinates[i].TrianglesIndex == triIndx )
					{
						return true;
					}
				}
			}

			return false;
		}

		public bool SharesVertSpace( LNX_Vertex vert ) //todo: this method won't be necessary if we unify the verts
		{
			if( SharedVertexCoordinates != null && SharedVertexCoordinates.Length > 0 )
			{
				for ( int i = 0; i < SharedVertexCoordinates.Length; i++ )
				{
					if ( SharedVertexCoordinates[i] == vert.MyCoordinate )
					{
						return true;
					}
				}
			}

			return V_Position == vert.V_Position;
		}

		public bool SharesVertSpace_ViaRelational( int triIndx, int vrtIndx )
		{
			if ( Relationships == null || Relationships.Length <= 0 ||
				SharedVertexCoordinates == null || SharedVertexCoordinates.Length <= 0
			)
			{
				return false;
			}

			for ( int i = 0; i < SharedVertexCoordinates.Length; i++ )
			{
				if( SharedVertexCoordinates[i].TrianglesIndex == triIndx &&
					SharedVertexCoordinates[i].ComponentIndex == vrtIndx
				)
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

		public LNX_VertexRelationship GetRelationship( LNX_Vertex otherVert )
		{
			return Relationships[otherVert.Index_Relational];
		}

		public LNX_VertexRelationship GetRelationship( LNX_ComponentCoordinate vertCoord )
		{
			return Relationships[vertCoord.TrianglesIndex * 3 + (vertCoord.ComponentIndex)];
		}


		public LNX_ComponentCoordinate GetVertCoord_viaProjectionSweep(
			Vector3 vProject, bool checkSelf )
		{
			vProject = LNX_Utils.FlatVector(vProject, v_navmeshProjectionDirection_cached);

			#region SHORT-CIRCUIT ================================================
			if (checkSelf && ProjectionIsInCenterSweep(vProject, true))
			{
				return MyCoordinate;
			}

			if (SharedVertexCoordinates == null || SharedVertexCoordinates.Length <= 0)
			{
				return LNX_ComponentCoordinate.None;
			}
			#endregion

			for (int i = 0; i < SharedVertexCoordinates.Length; i++)
			{
				Vector3 vLegA_flat = Relationships[
					SharedVertexCoordinates[i].TrianglesIndex * 3 +
					(SharedVertexCoordinates[i].ComponentIndex == 0 ? 1 : 0)
				].V_to_flat.normalized;
				Vector3 vLegB_flat = Relationships[
					SharedVertexCoordinates[i].TrianglesIndex * 3 +
					(SharedVertexCoordinates[i].ComponentIndex == 2 ? 1 : 2)
				].V_to_flat.normalized;

				if (LNX_Utils.AmInVectorCone(vProject, vLegA_flat, vLegB_flat, v_navmeshProjectionDirection_cached, true))
				{
					return SharedVertexCoordinates[i];
				}
			}

			return LNX_ComponentCoordinate.None;
		}
		public LNX_ComponentCoordinate GetVertCoord_viaProjectionSweep_dbg(
			Vector3 vProject, bool checkSelf, ref LNX_MethodDebugReport rprt )
		{
			rprt.StartMethod($"{this}.GetVertCoord_viaProjectionSweep_dbg(vPrjct: '{vProject}', chckSlf: '{checkSelf}')");

			vProject = LNX_Utils.FlatVector(vProject, v_navmeshProjectionDirection_cached);
			rprt.Log($"made flat projeciton: '{vProject}'...");
			#region SHORT-CIRCUIT ================================================
			if (checkSelf && ProjectionIsInCenterSweep(vProject, true))
			{
				rprt.Log_And_End_Method($"projection is in vert's own center sweep. Returning true...");
				return MyCoordinate;
			}

			if (SharedVertexCoordinates == null || SharedVertexCoordinates.Length <= 0)
			{
				rprt.Log_And_End_Method($"This vert has no sharedvertcoords. Returning false...");
				return LNX_ComponentCoordinate.None;
			}
			#endregion

			rprt.Log($"no short-circuits. Checking all shared coordinates...");

			for (int i = 0; i < SharedVertexCoordinates.Length; i++)
			{
				rprt.Log($"for '{i}', (coord: '{SharedVertexCoordinates[i]}')...",
					"calculating 'leg' projections...");

				Vector3 vLegA_flat = Relationships[
					SharedVertexCoordinates[i].TrianglesIndex * 3 + 
					(SharedVertexCoordinates[i].ComponentIndex == 0 ? 1 : 0)
				].V_to_flat.normalized;
				Vector3 vLegB_flat = Relationships[
					SharedVertexCoordinates[i].TrianglesIndex * 3 + 
					(SharedVertexCoordinates[i].ComponentIndex == 2 ? 1 : 2)
				].V_to_flat.normalized;

				rprt.Log($"using legA: '{vLegA_flat}', legB: '{vLegB_flat}'...");

				if ( LNX_Utils.AmInVectorCone(vProject, vLegA_flat, vLegB_flat, v_navmeshProjectionDirection_cached, true) )
				{
					rprt.Log_And_End_Method( $"Decided projection IS in vector cone. Returning coord: '{SharedVertexCoordinates[i]}'..." );
					return SharedVertexCoordinates[i];
				}
				else
				{
					rprt.Log($"decided projection is NOT in vector cone...");
				}
			}

			rprt.Log_And_End_Method($"end of method. Returning false with 'None' component coordinate...");

			return LNX_ComponentCoordinate.None;
		}

		public bool IsRelationshipCollectionValid()
		{
			if( Relationships == null || Relationships.Length <= 0 )
			{
				return false;
			}

			for( int i = 0; i < Relationships.Length; i++ )
			{
				//if( Relationships[i] == LNX_VertexRelationship.None )
				if ( !Relationships[i].AmValid )
				{
					return false;
				}
			}

			return true;
		}
		#endregion

		
		public LNX_Path Ping( LNX_NavmeshHit endPoint, LNX_NavMesh nm, float maxAllowableDist, LNX_Path runningPath,
			List<LNX_ComponentCoordinate> backstopverts = null
		)
		{
			#region SHORT-CIRCUITING ========================================
			if (maxAllowableDist > 0f)
			{
				if (runningPath.TotalDistance + Vector3.Distance(V_Position, endPoint.Position) > maxAllowableDist)
				{
					return LNX_Path.None;
				}
			}

			LNX_Path rcPath = new LNX_Path();

			bool rcastRslt = nm.Raycast( new LNX_NavmeshHit(this, nm.Triangles[TriangleIndex].V_PathingNormal), endPoint, out rcPath );

			if (!rcastRslt)
			{
				return new LNX_Path(runningPath, rcPath);
			}

			#endregion ---------------------------------------

			#region ASSEMBLE NEW (FORWARD) BACKSTOP ============================================
			List<LNX_ComponentCoordinate> fwdBackstopVerts = new List<LNX_ComponentCoordinate>();
			if (backstopverts != null && backstopverts.Count > 0)
			{
				for (int i = 0; i < backstopverts.Count; i++)
				{
					fwdBackstopVerts.Add(backstopverts[i]);
				}
			}

			if (!fwdBackstopVerts.Contains(MyCoordinate))
			{
				fwdBackstopVerts.Add(MyCoordinate);
			}

			List<LNX_Path> vsblVrtPths = nm.GetVisibleVertsFromVert(
				this, false, fwdBackstopVerts, maxAllowableDist - runningPath.TotalDistance
			);

			if (vsblVrtPths.Count <= 0)
			{
				return LNX_Path.None;
			}
			else
			{
				for (int i = 0; i < vsblVrtPths.Count; i++)
				{
					fwdBackstopVerts.Add(vsblVrtPths[i].EndCoordinate_vert);
				}
			}
			#endregion

			LNX_Path runningBestPath = LNX_Path.None;
			float runningBestDistance = maxAllowableDist;

			for (int i = 0; i < vsblVrtPths.Count; i++)
			{
				LNX_Path path_continuationToVsblVrt = new LNX_Path(runningPath, vsblVrtPths[i]);

				//todo: note: should I pass runingBestDist here instead of maxallowabledist considering maxallowabledist is now -1 at beginning?
				LNX_Path p = nm.Triangles[vsblVrtPths[i].EndTriIndex].Verts[vsblVrtPths[i].EndHit.VertIndex].Ping(
					endPoint, nm, runningBestDistance, path_continuationToVsblVrt, fwdBackstopVerts
				);


				if (p != LNX_Path.None)
				{
					if (runningBestDistance > 0 && p.TotalDistance < runningBestDistance)
					{
						runningBestPath = p;
						runningBestDistance = p.TotalDistance;
					}
				}
			}

			return runningBestPath;
		}

		public LNX_Path Ping_dbg(LNX_NavmeshHit endPoint, LNX_NavMesh nm, float maxAllowableDist,
			LNX_Path runningPath, ref LNX_MethodDebugReport rprt, List<LNX_ComponentCoordinate> backstopverts = null
		)
		{
			rprt.StartMethod($"{this}.Ping_dbg('{endPoint}', max: '{maxAllowableDist}', bkstps: " +
				$"'{(backstopverts == null ? "null" : backstopverts.Count)}')", $"{TriangleIndex}.{ComponentIndex}");

			if( runningPath != LNX_Path.None )
			{
				rprt.Log($"Note: runningPath: '{runningPath}', pts count: '{runningPath.PointCount}'...",
					$"runningpath dist: '{runningPath.TotalDistance}'");
			}

			//LNX_Path rtrnPath = LNX_Path.None;

			#region SHORT-CIRCUITING ========================================
			if (maxAllowableDist > 0f)
			{
				rprt.Log($"first, checking distance to see if we're already too far based on maxAllowableDist...");

				if (runningPath.TotalDistance + Vector3.Distance(V_Position, endPoint.Position) > maxAllowableDist)
				{
					rprt.Log_And_End_Method($"runningpath dist plus straight line distance too far. Short-circuiting...");
					Debug.Log($"runningpath dist plus straight line distance too far. Short-circuiting...");
					return LNX_Path.None;
				}
				else
				{
					rprt.Log($"decided am NOT too far yet. Continuing with ping operation...");
				}
			}

			rprt.Log($"Now raycasting to see if endPoint is visible from this vert...");
			LNX_Path rcPath = new LNX_Path();

			rprt.StartAbbreviatedMethod($"Raycast({this}, {endPoint})");
			bool rcastRslt = nm.Raycast_dbg(new LNX_NavmeshHit(this, nm.Triangles[TriangleIndex].V_PathingNormal), endPoint, out rcPath, ref rprt);
			rprt.EndAbbreviatedMethod($"Raycast({this}, {endPoint})");

			if (!rcastRslt)
			{
				/*
				LNX_Path p = new LNX_Path(runningPath, rcPath);
				rprt.Log_And_End_Method($"endpoint WAS visible. Returning path with dist: '{p.TotalDistance}' made from appending raycast path to running path...");
				return p;
				*/
				rprt.Log_And_End_Method($"endpoint WAS visible. Returning path made from appending raycast path to running path...");

				return new LNX_Path(runningPath, rcPath);
			}

			rprt.Log($"endpoint NOT visible. Continuing...");
			#endregion ---------------------------------------

			#region ASSEMBLE NEW (FORWARD) BACKSTOP ============================================
			rprt.Log($"Now assembling a list for forward backstop, which will include this vertex...");
			List<LNX_ComponentCoordinate> fwdBackstopVerts = new List<LNX_ComponentCoordinate>();
			if (backstopverts != null && backstopverts.Count > 0)
			{
				for (int i = 0; i < backstopverts.Count; i++)
				{
					fwdBackstopVerts.Add(backstopverts[i]);
				}
			}

			if (!fwdBackstopVerts.Contains(MyCoordinate))
			{
				fwdBackstopVerts.Add(MyCoordinate);
			}

			rprt.Log($"backstop initialized with: '{fwdBackstopVerts.Count}' verts from previous list...");

			rprt.Log($"Now getting visible verts from This vert, avoiding backstop verts...");

			rprt.StartAbbreviatedMethod($"GetVisibleVertsFromVert_dbg({this}, maxDist: '{maxAllowableDist - runningPath.TotalDistance}')");
			List<LNX_Path> vsblVrtPths = nm.GetVisibleVertsFromVert_dbg(
				this, ref rprt, false, fwdBackstopVerts, maxAllowableDist - runningPath.TotalDistance
			);
			rprt.EndAbbreviatedMethod($"GetVisibleVertsFromVert_dbg({this})");

			if (vsblVrtPths.Count <= 0)
			{
				rprt.Log_And_End_Method($"Ping() method tried to get visible verts from '{ToString()}', but failed to get any " +
					$"that weren't part of backstop collection. Returning 'None' path...");

				return LNX_Path.None;
			}
			else
			{
				rprt.Log($"Got '{vsblVrtPths.Count}' verts visible from this vert that were NOT already in the backstop.",
					$"Now adding these to forward backstop list...");

				for (int i = 0; i < vsblVrtPths.Count; i++)
				{
					fwdBackstopVerts.Add(vsblVrtPths[i].EndCoordinate_vert);
				}

				rprt.Log($"Finished creating fwd bckstop list. final list count: '{fwdBackstopVerts.Count}'...");
			}
			#endregion

			LNX_Path runningBestPath = LNX_Path.None;
			float runningBestDistance = maxAllowableDist;

			rprt.Log($"now calling ping() for all visible verts with starting runningbestdist: '{runningBestDistance}'...");
			for (int i = 0; i < vsblVrtPths.Count; i++)
			{
				rprt.Log($"for{i} ({vsblVrtPths[i].EndCoordinate_vert})...");

				/*
				if( (runningPath.TotalDistance + vsblVrtPths[i].TotalDistance) > maxAllowableDist )
				{
					rprt.Log($"path distances are already too long. Ignoring this one and continuing...");
					continue;
				}
				*/

				rprt.Log($"first, generating continuation path...");

				LNX_Path path_continuationToVsblVrt = new LNX_Path(runningPath, vsblVrtPths[i]);

				rprt.Log($"pinging from visible vert: '{vsblVrtPths[i].EndCoordinate_vert}'...");

				//todo: note: should I pass runingBestDist here instead of maxallowabledist considering maxallowabledist is now -1 at beginning?
				LNX_Path p = nm.Triangles[vsblVrtPths[i].EndTriIndex].Verts[vsblVrtPths[i].EndHit.VertIndex].Ping_dbg(
					endPoint, nm, runningBestDistance, path_continuationToVsblVrt, ref rprt, fwdBackstopVerts
				);


				if (p == LNX_Path.None)
				{
					rprt.Log($"ping returned 'None' path...");
				}
				else
				{
					rprt.Log($"got path with distance: '{p.TotalDistance}'. Checking against runningbest: '{runningBestDistance}'...");

					if (runningBestDistance > 0 && p.TotalDistance < runningBestDistance)
					{
						rprt.Log($"decided this is the new best path...");
						runningBestPath = p;
						runningBestDistance = p.TotalDistance;
					}
					else
					{
						rprt.Log($"decided NOT new best path based on distance...");
					}
				}
			}

			rprt.Log_And_End_Method($"end of ping for: '{this}'. Returning path: '{runningBestPath}'...");

			return runningBestPath;
		}


		#region HELPERS --------------------------------------------------
		public string GetCurrentInfoString()
		{
			return $"Vert.{nameof(SayCurrentInfo)}()\n" +
				$"{nameof(MyCoordinate)}: '{MyCoordinate}'\n" +
				$"{nameof(V_Position)}: '{V_Position}'\n" +
				$"{nameof(originalPosition)}: '{originalPosition}'\n" +

				$"{nameof(v_triCenter_cached)}: '{v_triCenter_cached}'\n" +
				$"{nameof(v_navmeshProjectionDirection_cached)}: '{v_navmeshProjectionDirection_cached}'\n" +
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
			//return $"{MyCoordinate.ToString()} {V_Position}";
			return $"{MyCoordinate.ToString()}";

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

			if (v_navmeshProjectionDirection_cached == Vector3.zero)
			{
				returnString += $"{nameof(v_navmeshProjectionDirection_cached)}: '{v_navmeshProjectionDirection_cached}'\n";
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

			if (SharedVertexCoordinates.Length <= 0 )
			{
				returnString += $"{nameof(SharedVertexCoordinates)} length: '{SharedVertexCoordinates.Length}'\n";
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
			string s = $"Vert[{ComponentIndex}].{nameof(GetRelationalString)}()\n";

			try
			{
				if( Relationships != null )
				{
					s += $"{nameof(Relationships)} count: '{Relationships.Length}'\n";
					s += $"{nameof(FirstSiblingRelationship)}: '{FirstSiblingRelationship}'\n" +
					$"{nameof(SecondSiblingRelationship)}: '{SecondSiblingRelationship}'\n\n" +
					$"{nameof(SharedVertexCoordinates)} count: '{SharedVertexCoordinates.Length}'\n" +
					$"";
				}
				else
				{
					s += "relationships collection was null...\n";
				}

				if (SharedVertexCoordinates == null)
				{
					s += $"{nameof(SharedVertexCoordinates)} collection is null\n";
				}
				else
				{
					s += $"{nameof(SharedVertexCoordinates)} length: '{SharedVertexCoordinates.Length}'\n";
				}
			}
			catch (Exception e )
			{
				Debug.LogError($"Got exception during GetRelationalString() for vert: '{ComponentIndex}'");
				//throw;
			}

			return s;
		}		

		public void SayAllRelationships()
		{
			string s = $"{this}.{nameof(SayAllRelationships)}()\n";
			int canSeeCount = 0;
			int cannotSeeCount = 0;
			int amValidCount = 0;

			if( Relationships == null )
			{
				s += $"relationships collection is null. Returning early...";
			}
			else if( Relationships.Length == 0 )
			{
				s += $"relationships collection count is only 0. Returning early...";
			}
			else
			{
				s += $"relationships collection count is '{Relationships.Length}'. Iterating through all...\n\n";


				for( int i = 0; i < Relationships.Length; i++ )
				{
					s += $"({i}) : {Relationships[i]}\n\n";
					if(Relationships[i].CanSee )
					{
						canSeeCount++;
					}
					else
					{
						cannotSeeCount++;
					}

					if(Relationships[i].AmValid )
					{
						amValidCount++;
					}
				}
			}

			s += $"\nREPORT==============================\n" +
				$"can see count: '{canSeeCount}'\n" +
				$"can NOT see count: '{cannotSeeCount}'\n" +
				$"amValid count: '{amValidCount}'";

			Debug.Log( s );

			Debug.Log($"can see count: '{canSeeCount}'\n" +
				$"can NOT see count: '{cannotSeeCount}'\n" +
				$"amValidCount: '{amValidCount}'"
			);
		}
		#endregion
	}
}