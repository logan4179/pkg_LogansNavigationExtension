using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEditor.Experimental.GraphView;
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

		#region RELATIONAL ======================================================================
		[HideInInspector] public LNX_VertexRelationship[] Relationships;


		/// <summary>Index where you can find this vertex from the perspective of other Vertices.</summary>
		public int Index_Relational => (MyCoordinate.TrianglesIndex * 3) + MyCoordinate.ComponentIndex;

		//todo: all these index properties need to be unit tested for accuracy
		public int Index_FirstSiblingVert => MyCoordinate.ComponentIndex == 0 ? 1 : 0;
		public LNX_ComponentCoordinate Coordinate_FirstSibling => new LNX_ComponentCoordinate(MyCoordinate.TrianglesIndex, Index_FirstSiblingVert);
		//private int firstSiblingRelationshipIndex => MyCoordinate.ComponentIndex == 0 ? (MyCoordinate.TrianglesIndex * 3) + 1 : MyCoordinate.TrianglesIndex * 3;
		public int firstSiblingRelationshipIndex => (MyCoordinate.TrianglesIndex * 3) + Index_FirstSiblingVert;

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

		public int secondSiblingRelationshipIndex => (MyCoordinate.TrianglesIndex * 3) + Index_SecondSiblingVert;

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

		#region EDGE =======================================================================
		/// <summary>Index of 'first' edge (based on index in the edges array) on the containing triangle, that forms this vertex. Note: This index will be the same as the first sibling vertex index </summary>
		public int Index_FirstFormingEdge => MyCoordinate.ComponentIndex == 0 ? 1 : 0;
		/// <summary>Index of 'second' edge (based on index in the edges array) on the containing triangle, that forms this vertex. Note: This index will be the same as the second sibling vertex index </summary>
		public int Index_SecondFormingEdge => MyCoordinate.ComponentIndex == 2 ? 1 : 2;
		#endregion
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

			//todo: it doesn't look to me like I need to calculate the relationships here. Try not doing this 
			//and seeing if it causes problems. If not, I also don't need to pass in the atomicTris collection, 
			//but instead, just the position of the first and second sibling verts I believe
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

		public void CalculateDerivedInfo(LNX_Triangle tri, LNX_NavMeshSurface nvmsh ) //todo: dws
		{

		}

		public void CreateRelationships( LNX_NavMeshSurface nvmsh, bool createSiblingRelationships, 
			bool createProximalRelationships, bool createDistalRelationships, ref StringBuilder rprt, bool allowBorrowing = true ) //todo: unit test
		{
			//Debug.Log( $"vert[{MyCoordinate}].{nameof(CreateRelationships)}()------------------------//////" );
			rprt.AppendLine( $"vert[{MyCoordinate}].{nameof(CreateRelationships)}()------------------------//////" );

			DateTime dt_methodStart = DateTime.Now;
			//why does this take so long?

			if ( Relationships == null || Relationships.Length != nvmsh.Triangles.Length * 3 )
			{
				Relationships = new LNX_VertexRelationship[nvmsh.Triangles.Length * 3];

				if ( createDistalRelationships && (!createSiblingRelationships || !createProximalRelationships) )
				{
					Debug.LogWarning($"LNX WARNING! CreateRelationships() was called for ONLY distal relationships, yet collection length " +
						$"was NOT valid, meaning proximal relationships might not be valid. Cannot make distal relationships if proximal " +
						$"relationships aren't valid. Remaking entire collection...");
				}

				createSiblingRelationships = true;
				createProximalRelationships = true;
			}
			else if ( createSiblingRelationships && createProximalRelationships && createDistalRelationships )
			{
				//Debug.Log($"making collection anew...");

				Relationships = new LNX_VertexRelationship[nvmsh.Triangles.Length * 3];
			}

			Vector3 clcltdPthngNrml = nvmsh.Triangles[TriangleIndex].V_PathingNormal;

			if (createSiblingRelationships)
			{
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
			}

			if ( createProximalRelationships )
			{
				//Note: This needs to be done before the rest of the relationships so that raycasting using a vert 
				//as a start point will work.
				List<LNX_ComponentCoordinate> temp_sharedVrtCoords = new List<LNX_ComponentCoordinate>();
				for (int i = 0; i < nvmsh.Triangles.Length; i++)
				{
					if (i == MyCoordinate.TrianglesIndex)
					{
						//Debug.Log($"continuing because of tri index...");
						continue;
					}

					#region CHECK IF TRIANGLE SHARES A VERT WITH ME ====================================
					if ( nvmsh.Triangles[i].Verts[0].V_Position == V_Position )
					{
						temp_sharedVrtCoords.Add( nvmsh.Triangles[i].Verts[0].MyCoordinate );

						Relationships[(i * 3) + 0] = new LNX_VertexRelationship(
							new LNX_Path(
								CachedSurfaceNormal,
								new LNX_NavmeshHit(nvmsh.Triangles[i].Verts[0], nvmsh.Triangles[i].V_PathingNormal)
							)
						);
						Relationships[(i * 3) + 1] = new LNX_VertexRelationship(
							new LNX_Path(
								CachedSurfaceNormal, 
								new LNX_NavmeshHit(this, clcltdPthngNrml),
								new LNX_NavmeshHit(nvmsh.Triangles[i].Verts[1], nvmsh.Triangles[i].V_PathingNormal)
							)
						);
						Relationships[(i * 3) + 2] = new LNX_VertexRelationship(
							new LNX_Path(
								CachedSurfaceNormal, 
								new LNX_NavmeshHit(this, clcltdPthngNrml),
								new LNX_NavmeshHit(nvmsh.Triangles[i].Verts[2], nvmsh.Triangles[i].V_PathingNormal)
							)
						);
						continue;
					}
					else if (nvmsh.Triangles[i].Verts[1].V_Position == V_Position)
					{
						temp_sharedVrtCoords.Add(nvmsh.Triangles[i].Verts[1].MyCoordinate);

						Relationships[(i * 3) + 0] = new LNX_VertexRelationship(
							new LNX_Path(
								CachedSurfaceNormal, 
								new LNX_NavmeshHit(this, clcltdPthngNrml),
								new LNX_NavmeshHit(nvmsh.Triangles[i].Verts[0], nvmsh.Triangles[i].V_PathingNormal)
							)
						);
						Relationships[(i * 3) + 1] = new LNX_VertexRelationship(
							new LNX_Path(
								CachedSurfaceNormal,
								new LNX_NavmeshHit(nvmsh.Triangles[i].Verts[1], nvmsh.Triangles[i].V_PathingNormal)
							)
						);
						Relationships[(i * 3) + 2] = new LNX_VertexRelationship(
							new LNX_Path(
								CachedSurfaceNormal, 
								new LNX_NavmeshHit(this, clcltdPthngNrml),
								new LNX_NavmeshHit(nvmsh.Triangles[i].Verts[2], 
								nvmsh.Triangles[i].V_PathingNormal)
							)
						);
						continue;
					}
					else if (nvmsh.Triangles[i].Verts[2].V_Position == V_Position)
					{
						temp_sharedVrtCoords.Add(nvmsh.Triangles[i].Verts[2].MyCoordinate);

						Relationships[(i * 3) + 0] = new LNX_VertexRelationship(
							new LNX_Path(
								CachedSurfaceNormal, 
								new LNX_NavmeshHit(this, clcltdPthngNrml),
								new LNX_NavmeshHit(nvmsh.Triangles[i].Verts[0], nvmsh.Triangles[i].V_PathingNormal)
							)
						);
						Relationships[(i * 3) + 1] = new LNX_VertexRelationship(
							new LNX_Path(
								CachedSurfaceNormal, 
								new LNX_NavmeshHit(this, clcltdPthngNrml),
								new LNX_NavmeshHit(nvmsh.Triangles[i].Verts[1], nvmsh.Triangles[i].V_PathingNormal)
							)
						);
						Relationships[(i * 3) + 2] = new LNX_VertexRelationship(
							new LNX_Path(
								CachedSurfaceNormal,
								new LNX_NavmeshHit(nvmsh.Triangles[i].Verts[2], nvmsh.Triangles[i].V_PathingNormal)
							)
						);
						continue;
					}
					#endregion

					#region CHECK IF TRIANGLE SHARES A VERT WITH ONE OF MY SIBLINGS ============
					if 
					(
						nvmsh.Triangles[i].Verts[0].V_Position == nvmsh.Triangles[MyCoordinate.TrianglesIndex].Verts[Index_FirstSiblingVert].V_Position ||
						nvmsh.Triangles[i].Verts[0].V_Position == nvmsh.Triangles[MyCoordinate.TrianglesIndex].Verts[Index_SecondSiblingVert].V_Position
					)
					{
						Relationships[(i * 3) + 0] = new LNX_VertexRelationship(
							new LNX_Path(
								CachedSurfaceNormal, 
								new LNX_NavmeshHit(this, clcltdPthngNrml),
								new LNX_NavmeshHit(nvmsh.Triangles[i].Verts[0], nvmsh.Triangles[i].V_PathingNormal)
							)
						);
					}
					if
					(
						nvmsh.Triangles[i].Verts[1].V_Position == nvmsh.Triangles[MyCoordinate.TrianglesIndex].Verts[Index_FirstSiblingVert].V_Position ||
						nvmsh.Triangles[i].Verts[1].V_Position == nvmsh.Triangles[MyCoordinate.TrianglesIndex].Verts[Index_SecondSiblingVert].V_Position
					)
					{
						Relationships[(i * 3) + 1] = new LNX_VertexRelationship(
							new LNX_Path(
								CachedSurfaceNormal, 
								new LNX_NavmeshHit(this, clcltdPthngNrml),
								new LNX_NavmeshHit(nvmsh.Triangles[i].Verts[1], nvmsh.Triangles[i].V_PathingNormal)
							)
						);
					}
					if
					(
						nvmsh.Triangles[i].Verts[2].V_Position == nvmsh.Triangles[MyCoordinate.TrianglesIndex].Verts[Index_FirstSiblingVert].V_Position ||
						nvmsh.Triangles[i].Verts[2].V_Position == nvmsh.Triangles[MyCoordinate.TrianglesIndex].Verts[Index_SecondSiblingVert].V_Position
					)
					{
						Relationships[(i * 3) + 2] = new LNX_VertexRelationship(
							new LNX_Path(
								CachedSurfaceNormal, 
								new LNX_NavmeshHit(this, clcltdPthngNrml),
								new LNX_NavmeshHit(nvmsh.Triangles[i].Verts[2], nvmsh.Triangles[i].V_PathingNormal)
							)
						);
					}
					#endregion
				}

				SharedVertexCoordinates = temp_sharedVrtCoords.ToArray();
			}

			if ( createDistalRelationships )
			{
				Debug.Log($"vert[{MyCoordinate}].{nameof(CreateRelationships)}()------------------//////");

				//Debug.Log("now creating distal relationships...");
				DateTime dt_relStart;
				int aBorrowCount = 0;
				int bBorrowCount = 0;
				int cBorrowCount = 0;
				int dBorrowCount = 0;
				int eBorrowCount = 0;

				double totalRelTime = 0f;
				double totalBorrowRelTime = 0f;

				for ( int i = 0; i < nvmsh.Triangles.Length; i++ ) //Note: Before optimization this look took about 1.6 seconds
				{
					DateTime dt_triStart = DateTime.Now;

					//Debug.Log($"<b>for tri{i}...</b>");
					if( i == MyCoordinate.TrianglesIndex )
					{
						//Debug.Log($"bypassing because of tri index...");
						continue;
					}

					if 
					(
						nvmsh.Triangles[i].Verts[0].V_Position == V_Position ||
						nvmsh.Triangles[i].Verts[1].V_Position == V_Position ||
						nvmsh.Triangles[i].Verts[2].V_Position == V_Position
					)
					{
						//Debug.Log($"<b>this entire triangle should be proximal. Bypassing...</b>");
						continue; //because these are already logged above
					}

					for (int i_vrts = 0; i_vrts < 3; i_vrts++)
					{
						//Debug.Log($"for vert{i_vrts}...");
						//Debug.Log($"for vert [{i}][{i_vrts}]...");

						//Debug.Log("now creating relationship...");
						dt_relStart = DateTime.Now;

						bool foundShared = false;
						#region CHECK IF RELATIONSHIP CAN BE BORROWED =======================================================
						if( allowBorrowing)
						{
							//CHECK FOR IF 'FOR' VERT ALREADY HAS A PATH TO THIS VERT...
							if 
							(
								nvmsh.Triangles[i].Verts[i_vrts].Relationships[Index_Relational] != null &&
								nvmsh.Triangles[i].Verts[i_vrts].Relationships[Index_Relational].AmValid
							)
							{
								//Debug.LogError($"happened A - 'for' vert already has path to 'this' vert");
								//Debug.Log($"happened to: '{nvmsh.Triangles[i].Verts[i_vrts]}'.");

								foundShared = true;
								aBorrowCount++;
								Relationships[(i * 3) + i_vrts] = new LNX_VertexRelationship(
									nvmsh.Triangles[i].Verts[i_vrts].Relationships[Index_Relational].PathTo.Reversed()
								);
							}
							//CHECK IF 'FOR' VERT ALREADY HAS A PATH TO A VERT SHARING SPACE WITH THIS VERT...
							else if ( nvmsh.Triangles[i].Verts[i_vrts].IsRelationshipCollectionSuperficiallyValid(nvmsh.Triangles.Length) )
							{
								
								for ( int i_shrd = 0; i_shrd < SharedVertexCoordinates.Length; i_shrd++ )
								{
									LNX_VertexRelationship otherRel = nvmsh.Triangles[i].Verts[i_vrts].
										Relationships[SharedVertexCoordinates[i_shrd].AsRelationalVertIndex];
									if ( otherRel != null && otherRel.AmValid )
									{

										//Debug.LogError($"happened B. - 'for' vert already has path to a vert sharing space with 'this vert");
										//Debug.Log($"happened to: '{nvmsh.Triangles[i].Verts[i_vrts]}'.");

										foundShared = true;
										bBorrowCount++;

										Relationships[(i * 3) + i_vrts] = new LNX_VertexRelationship(
											this, nvmsh.Triangles[i].Verts[i_vrts], otherRel.PathTo.Reversed()
										);
										break;
									}
								}
							}
							
							// CHECK FOR IF THIS VERT ALREADY HAS A PATH TO A VERT SHARING SPACE WITH 'FOR' VERT...
							if( !foundShared )
							{
								for (int i_otherShrd = 0; i_otherShrd < nvmsh.Triangles[i].Verts[i_vrts].SharedVertexCoordinates.Length; i_otherShrd++ )
								{
									if
									(
										nvmsh.Triangles[i].Verts[i_vrts].SharedVertexCoordinates[i_otherShrd].AmValid &&
										(
											nvmsh.Triangles[i].Verts[i_vrts].SharedVertexCoordinates[i_otherShrd].TrianglesIndex < i ||
											(
												nvmsh.Triangles[i].Verts[i_vrts].SharedVertexCoordinates[i_otherShrd].TrianglesIndex == i &&
												nvmsh.Triangles[i].Verts[i_vrts].SharedVertexCoordinates[i_otherShrd].ComponentIndex < i_vrts
											)
										)
									)
									{
										LNX_ComponentCoordinate shrdSpaceVertCoord = nvmsh.Triangles[i].Verts[i_vrts].SharedVertexCoordinates[i_otherShrd];

										//Debug.LogError($"happened C. - 'this' vert already has a path to a vert sharing space with 'for' vert");
										//Debug.Log($"happened to: '{nvmsh.Triangles[i].Verts[i_vrts]}'. shrdSpaceVertCoord: '{shrdSpaceVertCoord}'.");

										int existingRelIndx = nvmsh.Triangles[shrdSpaceVertCoord.TrianglesIndex].Verts[shrdSpaceVertCoord.ComponentIndex].Index_Relational;
										//Debug.Log($"rel index: '{existingRelIndx}' (as: '{shrdSpaceVertCoord.AsRelationalVertIndex}')");
								
										foundShared = true;
										cBorrowCount++;

										Relationships[(i * 3) + i_vrts] = new LNX_VertexRelationship(
											this, nvmsh.Triangles[i].Verts[i_vrts], new LNX_Path( Relationships[existingRelIndx].PathTo )
										);
								
										break;
								
									}
								}
							}

							// CHECK IF ANY OF THE SHARED COORDINATE RELATIONSHIPS MATCH ANY OF THE SHARED COORDINATE RELATIONSHIPS
							if (!foundShared)
							{
								bool fin = false;

								for (int i_shrd = 0; i_shrd < SharedVertexCoordinates.Length; i_shrd++)
								{
									LNX_ComponentCoordinate shrdVrtCoord = SharedVertexCoordinates[i_shrd];
									for( int i_shrd_other = 0; i_shrd_other < nvmsh.Triangles[i].Verts[i_vrts].SharedVertexCoordinates.Length; i_shrd_other++ )
									{
										LNX_ComponentCoordinate otherShrdVrtCrd = nvmsh.Triangles[i].Verts[i_vrts].SharedVertexCoordinates[i_shrd_other];
										LNX_VertexRelationship rel_shrd_to_otherShrd = nvmsh.Triangles[shrdVrtCoord.TrianglesIndex].Verts[shrdVrtCoord.ComponentIndex].
											Relationships[otherShrdVrtCrd.AsRelationalVertIndex];

										if ( rel_shrd_to_otherShrd != null && rel_shrd_to_otherShrd.AmValid )
										{
											//Debug.LogError($"happened D - 'this' vert has shared vert with path to shared vert of 'for' vert");

											fin = true;
											foundShared = true;
											dBorrowCount++;
											Relationships[(i * 3) + i_vrts] = new LNX_VertexRelationship(
												this, nvmsh.Triangles[i].Verts[i_vrts], new LNX_Path( rel_shrd_to_otherShrd.PathTo )
											);
											break;
										}
										else
										{
											if 
											(
												nvmsh.Triangles[otherShrdVrtCrd.TrianglesIndex].Verts[otherShrdVrtCrd.ComponentIndex].
												Relationships[shrdVrtCoord.AsRelationalVertIndex] != null &&
												nvmsh.Triangles[otherShrdVrtCrd.TrianglesIndex].Verts[otherShrdVrtCrd.ComponentIndex].
												Relationships[shrdVrtCoord.AsRelationalVertIndex].AmValid
											)

											//if ( rel_otherShrd_to_shrd != null && rel_otherShrd_to_shrd.AmValid )
											{
												Debug.LogError($"happened E - 'for' vert has shared vert with path to shared vert of 'this' vert");

												fin = true;
												foundShared = true;
												eBorrowCount++;
												Relationships[(i * 3) + i_vrts] = new LNX_VertexRelationship(
													this, nvmsh.Triangles[i].Verts[i_vrts], 
													nvmsh.Triangles[otherShrdVrtCrd.TrianglesIndex].Verts[otherShrdVrtCrd.ComponentIndex].
												Relationships[shrdVrtCoord.AsRelationalVertIndex].PathTo.Reversed()
												);
												break;
											}
										}
									}

									if( fin )
									{
										break;
									}
								}
							}
						}

						#endregion

						if ( !foundShared )
						{
							Relationships[(i * 3) + i_vrts] = new LNX_VertexRelationship(
								this, nvmsh.Triangles[i].Verts[i_vrts], nvmsh
							);
						}


						#region	CHECK TIME =========================================================
						double t = DateTime.Now.Subtract(dt_relStart).TotalSeconds;
						totalRelTime += t;

						if ( foundShared )
						{
							totalBorrowRelTime += t;
						}

						if( t > 1f )
						{
							//Debug.LogWarning($"relationship ('{Relationships[(i * 3) + i_vrts]}') took: '{DateTime.Now.Subtract(dt_relStart).TotalSeconds}' s...");
						}
						else
						{
							//Debug.Log($"relationship ('{Relationships[(i * 3) + i_vrts]}') took: '{DateTime.Now.Subtract(dt_relStart).TotalSeconds}' s...");
						}

						if( DateTime.Now.Subtract(dt_methodStart).TotalSeconds > 20f )
						{
							//Debug.LogError($"timeout reached at vert: '{this}' making rel: '{i},{i_vrts}'");
							return;
						}
						#endregion
					}

					#region REPORT ================================================
					double t_tri = DateTime.Now.Subtract(dt_triStart).TotalMilliseconds;
					string rprtStr = $"tri{i} took: '{t_tri}' ms. " +
						$"BORROW total: '{aBorrowCount + bBorrowCount + cBorrowCount + dBorrowCount + eBorrowCount}'. " +
						$"a: '{aBorrowCount}', b: '{bBorrowCount}', c: '{cBorrowCount}', d: '{dBorrowCount}', e: '{eBorrowCount}', " +
						$"avgRelTime: '{totalRelTime / (84 * 3)}' avgBorrowRelTime: '{totalBorrowRelTime / (aBorrowCount + bBorrowCount + cBorrowCount + dBorrowCount + eBorrowCount)}'";

					if ( t_tri < 10f)
					{
						//Debug.Log($"<color=blue>{rprtStr}</color>");
					}
					else
					{
						//Debug.LogWarning($"{rprtStr}");
					}
					rprt.AppendLine(rprtStr);

					#endregion

				}
			}

			//Debug.Log($"creating the rest took: '{DateTime.Now.Subtract(dt_start)}'");
		}

		public Vector3 CalculatePathingNormal()
		{
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
			projection = Vector3.Normalize(projection);

			#region SHORT-CIRCUITING ==========================================
			if (projection == V_ToFirstSiblingVert_flat || projection == V_ToSecondSiblingVert_flat)
			{
				if (includeOnPerim)
				{
					return true;
				}
				else
				{
					return false;
				}
			}

			if (V_ToFirstSiblingVert_flat == -V_ToSecondSiblingVert_flat)
			{
				return true; //because the "sweep cone" in this case would be a full 180 degrees, and it wouldn't matter which side.
							 //todo: Maybe I should actually log a warning here?
			}

			if (V_ToFirstSiblingVert_flat == V_ToSecondSiblingVert_flat)
			{
				return false;
			}
			#endregion

			float ang_crnr = Vector3.SignedAngle(V_ToFirstSiblingVert_flat, V_ToSecondSiblingVert_flat, v_navmeshProjectionDirection_cached);
			//float ang_crnr = Vector3.Angle(vLegA, vLegB);

			float ang_legAToPos = Vector3.SignedAngle(projection, V_ToFirstSiblingVert_flat, v_navmeshProjectionDirection_cached);
			float ang_legBToPos = Vector3.SignedAngle(projection, V_ToSecondSiblingVert_flat, v_navmeshProjectionDirection_cached);

			if
			(
				Mathf.Sign(ang_crnr) != Mathf.Sign(ang_legAToPos) &&
				Mathf.Sign(ang_crnr) == Mathf.Sign(ang_legBToPos)
			)
			{
				return true;
			}

			return false;
		}
		public bool ProjectionIsInCenterSweep_dbg(Vector3 projection, ref LNX_MethodDebugReport rprt, bool includeOnPerim = false)
		{
			rprt.StartMethod( $"v{ComponentIndex}.ProjectionIsInCenterSweep_dbg('{projection}', incldOnPrm: '{includeOnPerim}')");

			projection = Vector3.Normalize(projection);

			#region SHORT-CIRCUITING ==========================================
			if (projection == V_ToFirstSiblingVert_flat || projection == V_ToSecondSiblingVert_flat)
			{
				if (includeOnPerim)
				{
					return true;
				}
				else
				{
					return false;
				}
			}

			if (V_ToFirstSiblingVert_flat == -V_ToSecondSiblingVert_flat)
			{
				return true; //because the "sweep cone" in this case would be a full 180 degrees, and it wouldn't matter which side.
							 //todo: Maybe I should actually log a warning here?
			}

			if (V_ToFirstSiblingVert_flat == V_ToSecondSiblingVert_flat)
			{
				return false;
			}
			#endregion

			float ang_crnr = Vector3.SignedAngle(V_ToFirstSiblingVert_flat, V_ToSecondSiblingVert_flat, v_navmeshProjectionDirection_cached);
			//float ang_crnr = Vector3.Angle(vLegA, vLegB);

			float ang_legAToPos = Vector3.SignedAngle(projection, V_ToFirstSiblingVert_flat, v_navmeshProjectionDirection_cached);
			float ang_legBToPos = Vector3.SignedAngle(projection, V_ToSecondSiblingVert_flat, v_navmeshProjectionDirection_cached);

			if
			(
				Mathf.Sign(ang_crnr) != Mathf.Sign(ang_legAToPos) &&
				Mathf.Sign(ang_crnr) == Mathf.Sign(ang_legBToPos)
			)
			{
				return true;
			}

			rprt.Log_And_End_Method($"returning false...", "ProjectionIsInCenterSweep_dbg()");

			return false;
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

		public LNX_VertexRelationship GetRelationship( int triIndx, int vrtIndx )
		{
			return Relationships[triIndx * 3 + vrtIndx];
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

		public List<LNX_ComponentCoordinate> GetAdjacentTriangles(List<int> avoidTriangles = null)
		{
			List<LNX_ComponentCoordinate> rtrnList = new List<LNX_ComponentCoordinate>();

			if( SharedVertexCoordinates == null || SharedVertexCoordinates.Length <= 0 )
			{
				return null;
			}

			for (int i = 0; i < SharedVertexCoordinates.Length; i++ )
			{
				if
				(
					!rtrnList.Contains(SharedVertexCoordinates[i]) &&
					!avoidTriangles.Contains(SharedVertexCoordinates[i].TrianglesIndex)
				)
				{
					rtrnList.Add( SharedVertexCoordinates[i] );
				}
			}

			return rtrnList;
		}

		public bool IsRelationshipCollectionValid( LNX_NavMeshSurface nm )
		{
			if( Relationships == null || Relationships.Length != (nm.Triangles.Length * 3) )
			{
				return false;
			}

			return true;
		} //todo: replace with below overload

		public bool IsRelationshipCollectionSuperficiallyValid( int triCount )
		{
			if (Relationships == null || Relationships.Length != (triCount * 3))
			{
				return false;
			}

			return true;
		}

		public bool IsVertexRelationallyValid( int triIndx, int vrtIndx )
		{
			return Relationships[(triIndx * 3) + vrtIndx] != null && Relationships[(triIndx * 3) + vrtIndx].AmValid;
		}

		public bool IsTriangleCompletelyRelationallyValid(int triIndx)
		{
			if ( Relationships[triIndx * 3] == null || !Relationships[triIndx * 3].AmValid )
			{
				return false;
			}
			if ( Relationships[(triIndx * 3) + 1] == null || !Relationships[(triIndx * 3) + 1].AmValid )
			{
				return false;
			}
			if (Relationships[(triIndx * 3) + 2] == null || !Relationships[(triIndx * 3) + 2].AmValid )
			{
				return false;
			}

			return true;
		}

		public float GetFurthestDistanceOnTriangle_viaRelational(int triIndx)
		{
			float runningBestDist = Relationships[triIndx * 3].PathDistance;
			if( Relationships[(triIndx * 3) + 1].PathDistance > runningBestDist )
			{
				runningBestDist = Relationships[(triIndx * 3) + 1].PathDistance;
			}
			if (Relationships[(triIndx * 3) + 2].PathDistance > runningBestDist)
			{
				runningBestDist = Relationships[(triIndx * 3) + 2].PathDistance;
			}

			return runningBestDist;
		}

		public LNX_VertexRelationship GetFurthestDistanceRelationshipOnTriangle(int triIndx)
		{
			int runningBestIndx = 0;
			float runningBestDist = Relationships[triIndx * 3].PathDistance;

			if (Relationships[(triIndx * 3) + 1].PathDistance > runningBestDist)
			{
				runningBestIndx = 1;
				runningBestDist = Relationships[(triIndx * 3) + 1].PathDistance;
			}
			if (Relationships[(triIndx * 3) + 2].PathDistance > runningBestDist)
			{
				runningBestIndx = 2;
				runningBestDist = Relationships[(triIndx * 3) + 2].PathDistance;
			}

			return Relationships[(triIndx * 3) + runningBestIndx];
		}
		#endregion


		public LNX_Path Ping(LNX_NavmeshHit endPoint, LNX_NavMeshSurface nm, float maxAllowableDist,
			LNX_Path runningPath, List<LNX_ComponentCoordinate> backstopverts = null
		)
		{
			#region SHORT-CIRCUITING ========================================
			if (maxAllowableDist > 0f)
			{
				if (runningPath.TotalDistance + Vector3.Distance(V_Position, endPoint.Position) > maxAllowableDist)
				{
					return null;
				}

			}

			LNX_Path rcPath = new LNX_Path();

			bool rcastRslt = nm.Raycast(
				new LNX_NavmeshHit(this, nm.Triangles[TriangleIndex].V_PathingNormal), endPoint, out rcPath
			); //use in order to shorten report

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
				this, false, fwdBackstopVerts, maxAllowableDist > 0 ? maxAllowableDist - runningPath.TotalDistance : maxAllowableDist
			);

			if (vsblVrtPths.Count <= 0)
			{
				if (!backstopverts.Contains(MyCoordinate))
				{
					backstopverts.Add(MyCoordinate);
				}
				return null;
			}
			else
			{
				for (int i = 0; i < vsblVrtPths.Count; i++)
				{
					//rprt.Log($"adding vert: '{vsblVrtPths[i].EndCoordinate_vert}'...");

					fwdBackstopVerts.Add(vsblVrtPths[i].EndCoordinate_vert);
				}
			}
			#endregion

			#region FIND BEST START VERT FOR PING ===============================================
			float runningBestDistance = maxAllowableDist;
			int indx_bestPingStart = -1;

			if (vsblVrtPths.Count > 1)
			{
				bool foundRelPrblm = false;
				for (int i_visblVrtPths = 0; i_visblVrtPths < vsblVrtPths.Count; i_visblVrtPths++)
				{
					if (!IsTriangleCompletelyRelationallyValid(endPoint.TriangleIndex))
					{
						foundRelPrblm = true;
						break;
					}

					for (int i_vrts = 0; i_vrts < 3; i_vrts++)
					{
						float dist = vsblVrtPths[i_visblVrtPths].TotalDistance +
						nm.Triangles[vsblVrtPths[i_visblVrtPths].EndTriIndex].Verts[vsblVrtPths[i_visblVrtPths].EndHit.VertIndex].
						GetRelationship(endPoint.TriangleIndex, i_vrts).PathDistance +
						Vector3.Distance(nm.Triangles[endPoint.TriangleIndex].Verts[i_vrts].V_Position, endPoint.Position);

						if (runningBestDistance == -1 || dist < runningBestDistance)
						{
							runningBestDistance = dist;
							indx_bestPingStart = i_visblVrtPths;
						}
					}
				}

				if (foundRelPrblm)
				{
					int bestAdjacency = -1;

					for (int i = 0; i < vsblVrtPths.Count; i++)
					{
						int adjcncy = nm.GetAdjacencyDepthToTriangle(vsblVrtPths[i].EndTriIndex, endPoint.TriangleIndex);
						if (adjcncy > -1 && (indx_bestPingStart == -1 || adjcncy < bestAdjacency))
						{
							indx_bestPingStart = i;
							bestAdjacency = adjcncy;
						}
					}
				}
			}
			#endregion

			LNX_Path runningBestPath = null;

			#region FIRST, TRY ADJACENT ====================================
			if (indx_bestPingStart > -1)
			{
				LNX_Path path_continuationToVsblVrt = new LNX_Path(runningPath, vsblVrtPths[indx_bestPingStart]);
				LNX_Path fwdPath = nm.Triangles[vsblVrtPths[indx_bestPingStart].EndTriIndex].
					Verts[vsblVrtPths[indx_bestPingStart].EndHit.VertIndex].
					Ping(
					endPoint, nm, runningBestDistance, path_continuationToVsblVrt, fwdBackstopVerts
				);

				if (fwdPath != null)
				{
					if (runningBestDistance == -1 || fwdPath.TotalDistance < runningBestDistance)
					{
						runningBestPath = new LNX_Path(fwdPath); //todo: <<<<<does this need to be a new instance?
						runningBestDistance = fwdPath.TotalDistance;
					}
				}
			}
			#endregion

			for (int i = 0; i < vsblVrtPths.Count; i++)
			{
				if (i == indx_bestPingStart || vsblVrtPths[i].EndHit == endPoint)
				{
					continue;
				}

				LNX_Path path_continuationToVsblVrt = new LNX_Path(runningPath, vsblVrtPths[i]);

				LNX_Path fwdPath = nm.Triangles[vsblVrtPths[i].EndTriIndex].Verts[vsblVrtPths[i].EndHit.VertIndex].Ping(
					endPoint, nm, runningBestDistance, path_continuationToVsblVrt, fwdBackstopVerts
				);


				if (fwdPath != null)
				{
					if (runningBestDistance == -1 || fwdPath.TotalDistance < runningBestDistance)
					{
						runningBestPath = new LNX_Path(fwdPath); //todo: <<<<<does this need to be a new instance?
						runningBestDistance = fwdPath.TotalDistance;
					}
				}
			}

			return runningBestPath;
		}

		public LNX_Path Ping_dbg(LNX_NavmeshHit endPoint, LNX_NavMeshSurface nm, float maxAllowableDist,
			LNX_Path runningPath, ref LNX_MethodDebugReport rprt, List<LNX_ComponentCoordinate> backstopverts = null
		)
		{
			rprt.StartMethod($"{this}.Ping_dbg('{endPoint}', maxAllowableDist: '{maxAllowableDist}', bkstps: " +
				$"'{(backstopverts == null ? "null" : backstopverts.Count)}')");

			if( runningPath != null )
			{
				rprt.Log($"Note: runningPath: '{runningPath}', pts count: '{runningPath.PointCount}'...",
					$"runningpath dist: '{runningPath.TotalDistance}'");
			}

			//LNX_Path rtrnPath = LNX_Path.None;

			#region SHORT-CIRCUITING ========================================
			if( DateTime.Now.Subtract(rprt.DT_Start).TotalSeconds > 2f )
			{
				rprt.Log_And_End_Method($"dt timeout!");
				Debug.LogError($"dt timeout!");
				return null;
			}

			if (maxAllowableDist > 0f)
			{
				rprt.Log($"first, checking distance to see if we're already too far based on maxAllowableDist...");

				if (runningPath.TotalDistance + Vector3.Distance(V_Position, endPoint.Position) > maxAllowableDist)
				{
					rprt.Log_And_End_Method($"runningpath dist ('{runningPath.TotalDistance}') plus straight line " +
						$"distance ('{Vector3.Distance(V_Position, endPoint.Position)}') is '{runningPath.TotalDistance + Vector3.Distance(V_Position, endPoint.Position)}', which is farther than " +
						$"maxAllowableDist: '{maxAllowableDist}'. Short-circuiting...");
					//Debug.Log($"runningpath dist ('{runningPath.TotalDistance}') plus straight line " +
						//$"distance ('{Vector3.Distance(V_Position, endPoint.Position)}') farther than " +
						//$"maxAllowableDist: '{maxAllowableDist}'. Short-circuiting...");
					return null;
				}
				else
				{
					rprt.Log($"decided am NOT too far yet. Continuing with ping operation...");
				}
			}

			rprt.Log($"Now raycasting to see if endPoint is visible from this vert...");
			LNX_Path rcPath = new LNX_Path();

			//rprt.StartAbbreviatedMethod($"Raycast({this}, {endPoint})");

			/*bool rcastRslt = nm.Raycast_dbg(
				new LNX_NavmeshHit(this, nm.Triangles[TriangleIndex].V_PathingNormal), endPoint, out rcPath, ref rprt
			);*/
			bool rcastRslt = nm.Raycast(
				new LNX_NavmeshHit(this, nm.Triangles[TriangleIndex].V_PathingNormal), endPoint, out rcPath
			); //use in order to shorten report

			//rprt.EndAbbreviatedMethod("");

			if (!rcastRslt)
			{
				rprt.Log_And_End_Method($"endpoint WAS visible. rcpath dist: '{rcPath.TotalDistance}'. Returning path made from appending " +
					$"raycast path to running path...");

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

			rprt.Log($"fwdbackstop initialized with: '{fwdBackstopVerts.Count}' verts from previous list...");

			rprt.Log($"Now getting visible verts from This vert, avoiding backstop verts...");

			rprt.StartAbbreviatedMethod($"GetVisibleVertsFromVert_dbg({this}, maxDist: '{(maxAllowableDist > 0 ? maxAllowableDist - runningPath.TotalDistance : maxAllowableDist)}')");
			List<LNX_Path> vsblVrtPths = nm.GetVisibleVertsFromVert_dbg(
				this, ref rprt, false, fwdBackstopVerts, maxAllowableDist > 0 ? maxAllowableDist - runningPath.TotalDistance : maxAllowableDist
			);
			rprt.EndAbbreviatedMethod("");

			if (vsblVrtPths.Count <= 0)
			{
				rprt.Log_And_End_Method($"Ping() method tried to get visible verts from '{ToString()}', but failed to get any " +
					$"that weren't part of backstop collection. Returning 'None' path...");
				//Debug.Log($"Ping() method tried to get visible verts from '{ToString()}', but failed to get any " +
				//$"that weren't part of backstop collection. Returning 'None' path...");

				if (!backstopverts.Contains(MyCoordinate))
				{
					backstopverts.Add(MyCoordinate);
					rprt.Log($"Added my coordinate to backstop verts collection.");
				}
				return null;
			}
			else
			{
				rprt.Log($"Got '{vsblVrtPths.Count}' verts visible from this vert that were NOT already in the backstop.",
					$"Now adding these to forward backstop list...");

				for (int i = 0; i < vsblVrtPths.Count; i++)
				{
					//rprt.Log($"adding vert: '{vsblVrtPths[i].EndCoordinate_vert}'...");

					fwdBackstopVerts.Add(vsblVrtPths[i].EndCoordinate_vert);
				}
				rprt.Log($"Finished creating fwd bckstop list. final list count: '{fwdBackstopVerts.Count}'...");
			}
			#endregion

			#region FIND BEST START VERT FOR PING ===============================================
			float runningBestDistance = maxAllowableDist;
			int indx_bestPingStart = -1;

			if (vsblVrtPths.Count > 1)
			{
				bool foundRelPrblm = false;
				for (int i_visblVrtPths = 0; i_visblVrtPths < vsblVrtPths.Count; i_visblVrtPths++)
				{
					rprt.Log($"for({i_visblVrtPths}), vert: '{vsblVrtPths[i_visblVrtPths].EndCoordinate_vert}'...");

					if ( !IsTriangleCompletelyRelationallyValid(endPoint.TriangleIndex) )
					{
						rprt.Log($"endHit tri is NOT completely relationally valid. breaking check...");
						foundRelPrblm = true;
						break;
					}

					for (int i_vrts = 0; i_vrts < 3; i_vrts++)
					{
						float dist = vsblVrtPths[i_visblVrtPths].TotalDistance +
						nm.Triangles[vsblVrtPths[i_visblVrtPths].EndTriIndex].Verts[vsblVrtPths[i_visblVrtPths].EndHit.VertIndex].
						GetRelationship(endPoint.TriangleIndex, i_vrts).PathDistance +
						Vector3.Distance(nm.Triangles[endPoint.TriangleIndex].Verts[i_vrts].V_Position, endPoint.Position);

						if (runningBestDistance == -1 || dist < runningBestDistance)
						{
							runningBestDistance = dist;
							indx_bestPingStart = i_visblVrtPths;
							rprt.Log($"decided path: '{vsblVrtPths[i_visblVrtPths]}' " +
								$" seems to be the new best.",
								$"runningBestDistance: '{runningBestDistance}', indx_runningBestPath: '{indx_bestPingStart}'");
						}
					}
				}

				if (foundRelPrblm)
				{
					rprt.Log($"checking best end-adjacency among '{vsblVrtPths.Count}' visible paths...");
					int bestAdjacency = -1;

					for (int i = 0; i < vsblVrtPths.Count; i++)
					{
						int adjcncy = nm.GetAdjacencyDepthToTriangle( vsblVrtPths[i].EndTriIndex, endPoint.TriangleIndex );
						rprt.Log($"for{i}, from visible vert: '{vsblVrtPths[i].EndCoordinate_vert}', got adjacency: '{adjcncy}'");
						if( adjcncy > -1 && (indx_bestPingStart == -1 || adjcncy < bestAdjacency) )
						{
							indx_bestPingStart = i;
							bestAdjacency = adjcncy;
							rprt.Log($"new best adjacenecy at element '{i}'...");
						}
					}
				}
			}



			rprt.Log($"After element check, indx_bestAdjacent: '{indx_bestPingStart}'");
			#endregion

			LNX_Path runningBestPath = null;

			rprt.Log($"now calling ping() for all visible verts with starting runningbestdist: '{runningBestDistance}'...");

			#region FIRST, TRY ADJACENT ====================================
			if(indx_bestPingStart > -1 )
			{
				rprt.Log($"trying best adjacent first...");
				LNX_Path path_continuationToVsblVrt = new LNX_Path(runningPath, vsblVrtPths[indx_bestPingStart]);
				rprt.Log($"runningpath dist: '{runningPath.TotalDistance}', vsblVrtPth dist: '{vsblVrtPths[indx_bestPingStart].TotalDistance}'...");
				rprt.Log($"continuation path (running path + vsblVrtPth) initialized with dist: '{path_continuationToVsblVrt.TotalDistance}'");

				rprt.Log($"pinging from visible vert: '{vsblVrtPths[indx_bestPingStart].EndCoordinate_vert}'...");

				LNX_Path fwdPath = nm.Triangles[vsblVrtPths[indx_bestPingStart].EndTriIndex].
					Verts[vsblVrtPths[indx_bestPingStart].EndHit.VertIndex].
					Ping_dbg(
					endPoint, nm, runningBestDistance, path_continuationToVsblVrt, ref rprt, fwdBackstopVerts
				);

				if (fwdPath == null)
				{
					rprt.Log($"ping returned 'None' path...");
				}
				else
				{
					rprt.Log($"got path with distance: '{fwdPath.TotalDistance}'...");

					if (runningBestDistance == -1 || fwdPath.TotalDistance < runningBestDistance)
					{
						rprt.Log($"decided this is the new best path...");
						runningBestPath = new LNX_Path(fwdPath); //todo: <<<<<does this need to be a new instance?
						runningBestDistance = fwdPath.TotalDistance;
					}
					else
					{
						rprt.Log($"decided NOT new best path based on distance...");
					}
				}
			}
			#endregion

			for (int i = 0; i < vsblVrtPths.Count; i++)
			{
				rprt.Log($"for{i} ({vsblVrtPths[i].EndCoordinate_vert})...");

				if ( i == indx_bestPingStart || vsblVrtPths[i].EndHit == endPoint)
				{
					rprt.Log($"this path was already found adjacent. Continuing...");
					continue;
				}

				rprt.Log($"first, generating continuation path...");

				LNX_Path path_continuationToVsblVrt = new LNX_Path(runningPath, vsblVrtPths[i]);
				rprt.Log($"runningpath dist: '{runningPath.TotalDistance}', vsblVrtPth dist: '{vsblVrtPths[i].TotalDistance}'...");
				rprt.Log($"continuation path (running path + vsblVrtPth) initialized with dist: '{path_continuationToVsblVrt.TotalDistance}'");

				rprt.Log($"pinging from visible vert: '{vsblVrtPths[i].EndCoordinate_vert}'...");


				LNX_Path fwdPath = nm.Triangles[vsblVrtPths[i].EndTriIndex].Verts[vsblVrtPths[i].EndHit.VertIndex].Ping_dbg(
					endPoint, nm, runningBestDistance, path_continuationToVsblVrt, ref rprt, fwdBackstopVerts
				);


				if (fwdPath == null)
				{
					rprt.Log($"ping returned 'None' path...");
				}
				else
				{
					rprt.Log($"got path with distance: '{fwdPath.TotalDistance}'. Checking against runningbest: '{runningBestDistance}'...");

					if (runningBestDistance == -1 || fwdPath.TotalDistance < runningBestDistance)
					{
						rprt.Log($"decided this is the new best path...");
						runningBestPath = new LNX_Path(fwdPath); //todo: <<<<<does this need to be a new instance?
						runningBestDistance = fwdPath.TotalDistance;
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
		public string GetCurrentInfoString(LNX_NavMeshSurface nm )
		{
			string rtrnString = $"{this}.GetCurrentInfoString()\n" +
				$"{nameof(MyCoordinate)}: '{MyCoordinate}'\n" +
				$"{nameof(V_Position)}: '{V_Position}'\n" +
				$"{nameof(originalPosition)}: '{originalPosition}'\n" +

				$"{nameof(v_triCenter_cached)}: '{v_triCenter_cached}'\n" +
				$"{nameof(v_navmeshProjectionDirection_cached)}: '{v_navmeshProjectionDirection_cached}'\n" +
				$"{nameof(Index_VisMesh_Vertices)}: '{Index_VisMesh_Vertices}'\n" +

				$"RELATIONAL---------\n" +

				$"{GetRelationalString(nm)}\n";

			if( IsRelationshipCollectionValid(nm) )
			{
				rtrnString += $"{nameof(AngleAtBend)}: '{AngleAtBend}'\n" +
				$"{nameof(AngleAtBend_flattened)}: '{AngleAtBend_flattened}'\n" +

				$"{nameof(V_ToFirstSiblingVert)}: '{V_ToFirstSiblingVert}'\n" +

				$"{nameof(V_ToSecondSiblingVert)}: '{V_ToSecondSiblingVert}'\n" +

				$"";
			}

			return rtrnString;
		}

		public void SayCurrentInfo(LNX_NavMeshSurface nm)
		{
			Debug.Log( GetCurrentInfoString(nm) );
		}

		public override string ToString()
		{
			//return $"{MyCoordinate.ToString()} {V_Position}";
			return $"{MyCoordinate.ToString()}";

		}

		public string GetAnomolyString(LNX_NavMeshSurface nm )
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

		public string GetRelationalString(LNX_NavMeshSurface nm)
		{
			string s = $"Vert[{ComponentIndex}].{nameof(GetRelationalString)}()\n";

			s += $"IsRelationshipCollectionValid(): " +
				$"'{(IsRelationshipCollectionValid(nm) ? "true" : "relationships NOT valid! <<<<<<<<<<<<<<<<<<<<<<<<<<<<<\n")}'\n";
			try
			{
				if( Relationships == null )
				{
					s += "relationships collection was null...\n";
				}
				else if ( Relationships.Length == 0 )
				{
					s += $"relationships length is 0...\n";
				}
				else
				{
					s += $"{nameof(Relationships)} count: '{Relationships.Length}'\n" +
					$"\n" +
					$"{nameof(Index_FirstSiblingVert)}: '{Index_FirstSiblingVert}'\n" +
					$"{nameof(FirstSiblingRelationship)}: '{FirstSiblingRelationship}'\n" +
					$"{nameof(firstSiblingRelationshipIndex)}: '{firstSiblingRelationshipIndex}'\n" +

					$"\n" +
					$"{nameof(Index_SecondSiblingVert)}: '{Index_SecondSiblingVert}'\n" +
					$"{nameof(SecondSiblingRelationship)}: '{SecondSiblingRelationship}'\n" +
					$"{nameof(secondSiblingRelationshipIndex)}: '{secondSiblingRelationshipIndex}'\n\n" +
					$"";
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
				s += $"relationships collection count is '{Relationships.Length}'.\n" +
					$"shared vert coord amt: '{SharedVertexCoordinates.Length}'\n" +
					$"Iterating through all...\n\n";


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