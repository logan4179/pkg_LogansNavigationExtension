using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.UIElements;


namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_Triangle
	{
		public string name;
		[SerializeField] private string DbgCalculateTriInfo;

		/// <summary>Describes this triangle's position inside the containing manager's Triangles array </summary>
		[SerializeField, HideInInspector] private int index_inCollection;
		public int Index_inCollection => index_inCollection;
		/// <summary>Index where this triangle's vertices start in the visualization mesh's .triangles array. Note: 
		/// this is just Index_inCollection * 3.</summary>
		public int MeshIndex_trianglesStart => index_inCollection * 3;

		[HideInInspector, Tooltip("Corresponds to the area indices set up in the Navigation window.")] 
		public int AreaIndex;

		[Header("COMPONENTS")]
		public LNX_Vertex[] Verts;
		public LNX_Edge[] Edges;

		[Header("CALCULATED/DERIVED")]
		[HideInInspector] public Vector3 V_Center;
		[SerializeField, HideInInspector] private Vector3 v_flattenedCenter;
		/// <summary>Distance around the triangle</summary>
		[HideInInspector] public float Perimeter;
		/// <summary>The longest edge of this triangle. This can be used for effecient decision-making. 
		/// IE: IF a position's distance from this triangle's center is greater than half of this value, it can't possibly 
		/// be on this triangle</summary>
		[HideInInspector] public float LongestEdgeLength;
		[HideInInspector] public float ShortestEdgeLength;
		[HideInInspector] public float Area;

		[Header("RELATIONAL")]
		public LNX_TriangleRelationship[] Relationships;
		/// <summary>
		/// Array of indices of triangles that share at least one vertex with this triangle.
		/// </summary>
		public int[] AdjacentTriIndices;

		//[Header("FLAGS")]
		/// <summary>Marks a vert dirty after a re-position of vert so that it's containing triangle knows to 
		/// re-calculate it's derived info when the user stops moving the vert.</summary>
		[SerializeField, HideInInspector] private bool dirtyFlag_repositionedVert = false;

		/// <summary>Whether this triangle was added by a mesh modification, as opposed to being created 
		/// as part of the original navmesh triangulation.</summary>
		public bool WasAddedViaMod;

		public bool HasBeenModifiedAfterCreation
		{
			get
			{
				return Verts[0].AmModified || Verts[1].AmModified || Verts[2].AmModified;
			}
		}

		public bool HasTerminalEdge
		{
			get
			{
				return Edges[0].AmTerminal || Edges[1].AmTerminal || Edges[2].AmTerminal;
			}
		}

		//[Header("OTHER")]
		/// <summary>Normal derived by sampling the terrain underfoot.</summary>
		[HideInInspector] public Vector3 v_sampledNormal;
		/// <summary>Normal derived from the layout of the triangle's vertices. IE: This normal will be perpendicular to the plane formed by the vertices.</summary>
		[HideInInspector] public Vector3 v_derivedNormal;
		/// <summary>This is the normal used for shape projecting. It should be the same as the SurfaceOrientation of the LNX_Navmesh this triangle belongs to. </summary>
		[HideInInspector] public Vector3 v_projectionNormal;

		public LNX_Triangle( int parallelIndex, int areaIndx, Vector3 vrtPos0, Vector3 vrtPos1, Vector3 vrtPos2, LNX_NavMesh navMesh )
		{
			//Debug.Log($"tri ctor. {nameof(parallelIndex)}: '{parallelIndex}' (x3: '{parallelIndex * 3}'). verts start: '{nmTriangulation.indices[(parallelIndex * 3)]}'");

			DbgCalculateTriInfo = string.Empty;

			index_inCollection = parallelIndex;

			AreaIndex = areaIndx;
			v_projectionNormal = navMesh.GetSurfaceNormal();

			Verts = new LNX_Vertex[3];
			Verts[0] = new LNX_Vertex( this, vrtPos0, 0 );
			Verts[1] = new LNX_Vertex( this, vrtPos1, 1 );
			Verts[2] = new LNX_Vertex( this, vrtPos2, 2 );

			V_Center = (Verts[0].V_Position + Verts[1].V_Position + Verts[2].V_Position) / 3f;

			v_flattenedCenter = GetFlattenedPosition( V_Center );

			Edges = new LNX_Edge[3];
			Edges[0] = new LNX_Edge( this, Verts[1], Verts[2], 0);
			Edges[1] = new LNX_Edge( this, Verts[0], Verts[2], 1);
			Edges[2] = new LNX_Edge( this, Verts[1], Verts[0], 2);

			CalculateNormals( navMesh, true ); //needs to happen before calculating derived info

			CalculateDerivedInfo();

			DbgCalculateTriInfo += $"\nEnd of ctor(). Report:\n" +
				$"{nameof(V_Center)}: '{V_Center}'\n" +
				$"{nameof(v_flattenedCenter)}: '{v_flattenedCenter}'\n" +
				$"nrml (smpld): '{v_sampledNormal}', prjctd: '{v_projectionNormal}'\n" +
				$"derivdNrml: '{v_derivedNormal}'\n" +
				$"edge lengths: '{Edges[0].EdgeLength}', '{Edges[1].EdgeLength}', '{Edges[2].EdgeLength}'\n" +
				$"Prmtr: '{Perimeter}', Area: '{AreaIndex}'\n";
		}

		public void AdoptValues( LNX_Triangle baseTri )
		{
			index_inCollection = baseTri.index_inCollection;

			DbgCalculateTriInfo = baseTri.DbgCalculateTriInfo;

			V_Center = baseTri.V_Center;
			v_sampledNormal = baseTri.v_sampledNormal;
			Perimeter = baseTri.Perimeter;
			AreaIndex = baseTri.AreaIndex;
			LongestEdgeLength = baseTri.LongestEdgeLength;
			ShortestEdgeLength = baseTri.ShortestEdgeLength;
			Verts[0].AdoptValues( baseTri.Verts[0] );
			Verts[1].AdoptValues( baseTri.Verts[1] );
			Verts[2].AdoptValues( baseTri.Verts[2] );

			Edges[0].AdoptValues( baseTri.Edges[0] );
			Edges[1].AdoptValues( baseTri.Edges[1] );
			Edges[2].AdoptValues( baseTri.Edges[2] );

			Relationships = baseTri.Relationships;
			AdjacentTriIndices = baseTri.AdjacentTriIndices;
			dirtyFlag_repositionedVert = false;

			name = $"ind: '{index_inCollection}', ctr: '{V_Center}'";
		}

		public void ChangeIndex_action( int newIndex )
		{
			index_inCollection = newIndex;

			Verts[0].TriIndexChanged( newIndex );
			Verts[1].TriIndexChanged( newIndex );
			Verts[2].TriIndexChanged( newIndex );

			Edges[0].TriIndexChanged( newIndex );
			Edges[1].TriIndexChanged( newIndex );
			Edges[2].TriIndexChanged( newIndex );

			name = $"ind: '{index_inCollection}', ctr: '{V_Center}'";
		}

		public bool VertsEqual( LNX_Triangle otherTri )
		{
			if (
				otherTri.Verts == null || otherTri.Verts.Length != 3 || Verts == null || Verts.Length != 3
			)
			{
				return false;
			}

			if (
				otherTri.GetVertIndextAtPosition(Verts[0].V_Position) == -1 ||
				otherTri.GetVertIndextAtPosition(Verts[1].V_Position) == -1 ||
				otherTri.GetVertIndextAtPosition(Verts[2].V_Position) == -1
			)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Tests if this triangle's original state is a match the supplied triangle's current state.
		/// </summary>
		/// <param name="otherTri"></param>
		/// <returns></returns>
		public bool PositionallyMatches( LNX_Triangle otherTri )
		{
			if (
				otherTri.Verts == null || otherTri.Verts.Length != 3 || Verts == null || Verts.Length != 3
			)
			{
				return false;
			}

			if (
				otherTri.GetVertIndextAtPosition(Verts[0].OriginalPosition) == -1 ||
				otherTri.GetVertIndextAtPosition(Verts[1].OriginalPosition) == -1 ||
				otherTri.GetVertIndextAtPosition(Verts[2].OriginalPosition) == -1
			)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// For checking if another triangle has equal values.
		/// </summary>
		/// <param name="tri"></param>
		/// <returns></returns>
		public bool ValueEquals( LNX_Triangle tri )
		{
			if( !VertsEqual(tri) )
			{
				return false;
			}

			if ( 
				V_Center != tri.V_Center || 
				v_sampledNormal != tri.v_sampledNormal ||
				Perimeter != tri.Perimeter || 
				AreaIndex != tri.AreaIndex || 
				LongestEdgeLength != tri.LongestEdgeLength || 
				ShortestEdgeLength != tri.ShortestEdgeLength
			)
			{
				return false;
			}



			return true;
		}

		/// <summary>
		/// Calculates/recalculates the information a tri derives about itself using the positions of it's vertices. 
		/// Use this after you edit a tri's components.
		/// </summary>
		public void CalculateDerivedInfo( bool logMessages = true )
		{
			V_Center = (Verts[0].V_Position + Verts[1].V_Position + Verts[2].V_Position) / 3f;

			Edges[0].CalculateInfo( this, Verts[1], Verts[2] );
			Edges[1].CalculateInfo(this, Verts[0], Verts[2]);
			Edges[2].CalculateInfo(this, Verts[1], Verts[0]);

			Perimeter = Edges[0].EdgeLength + Edges[1].EdgeLength + Edges[2].EdgeLength;

			//Use "Heron's Formula" to get the area..
			float semiPerimeter = Perimeter * 0.5f;
			Area = Mathf.Sqrt(semiPerimeter *
				(semiPerimeter - Edges[0].EdgeLength) *
				(semiPerimeter - Edges[1].EdgeLength) *
				(semiPerimeter - Edges[2].EdgeLength)
			);

			LongestEdgeLength = Mathf.Max(Edges[0].EdgeLength, Edges[1].EdgeLength, Edges[2].EdgeLength);
			ShortestEdgeLength = Mathf.Min(Edges[0].EdgeLength, Edges[1].EdgeLength, Edges[2].EdgeLength);

			name = $"ind: '{index_inCollection}', ctr: '{V_Center}'";
		}

		public void RefreshTriangle( LNX_NavMesh nm, bool logMessages = true) //todo: this needs to be renamed
		{
			if ( dirtyFlag_repositionedVert )
			{
				CalculateDerivedInfo( logMessages );

				CreateRelationships( nm.Triangles );

				dirtyFlag_repositionedVert = false;
			}
		}

		/// <summary>
		/// Creates the relationship structures relating this triangle to all other triangles.
		/// </summary>
		/// <param name="Tris"></param>
		/// <param name="amThorough">If false, bypasses re-processing the sharedvertexcoordinates collections, 
		/// which is not necessary in some cases.</param>
		public void CreateRelationships( LNX_Triangle[] Tris, bool amThorough = true )
		{
			Relationships = new LNX_TriangleRelationship[Tris.Length];
			for ( int i_otherTri = 0; i_otherTri < Tris.Length; i_otherTri++ )
			{
				Relationships[i_otherTri] = new LNX_TriangleRelationship( this, Tris[i_otherTri] );
			}
			
			if( amThorough )
			{
				#region Create Vertex sibling relationships....
				Verts[0].SetSiblingRelationships( Verts[1], Verts[2] );
				Verts[1].SetSiblingRelationships( Verts[0], Verts[2] );
				Verts[2].SetSiblingRelationships( Verts[0], Verts[1] );
				#endregion

				List<int> foundAdjacentTriIndices_temp = new List<int>();
				List<LNX_ComponentCoordinate> sharedVertCoords0_temp = new List<LNX_ComponentCoordinate>();
				List<LNX_ComponentCoordinate> sharedVertCoords1_temp = new List<LNX_ComponentCoordinate>();
				List<LNX_ComponentCoordinate> sharedVertCoords2_temp = new List<LNX_ComponentCoordinate>();

				for ( int i_otherTri = 0; i_otherTri < Tris.Length; i_otherTri++ )
				{
					Relationships[i_otherTri] = new LNX_TriangleRelationship( this, Tris[i_otherTri] );

					if ( i_otherTri == index_inCollection ) //IF we've iterated to this triangle's self, just continue...
					{
						continue;
					}

					if ( Relationships[i_otherTri].GetNumberOfSharedVerts() > 0 )
					{
						foundAdjacentTriIndices_temp.Add( i_otherTri );

						if ( Relationships[i_otherTri].IndexMap_OwnedVerts_toShared[0] != -1 )
						{
							sharedVertCoords0_temp.Add(
								Tris[i_otherTri].Verts[Relationships[i_otherTri].IndexMap_OwnedVerts_toShared[0]].MyCoordinate
							);
						}
						if ( Relationships[i_otherTri].IndexMap_OwnedVerts_toShared[1] != -1 )
						{
							sharedVertCoords1_temp.Add(
								Tris[i_otherTri].Verts[Relationships[i_otherTri].IndexMap_OwnedVerts_toShared[1]].MyCoordinate
							);
						}
						if ( Relationships[i_otherTri].IndexMap_OwnedVerts_toShared[2] != -1 )
						{
							sharedVertCoords2_temp.Add(
								Tris[i_otherTri].Verts[Relationships[i_otherTri].IndexMap_OwnedVerts_toShared[2]].MyCoordinate
							);
						}
					}
				}

				AdjacentTriIndices = foundAdjacentTriIndices_temp.ToArray();

				Verts[0].SharedVertexCoordinates = sharedVertCoords0_temp.ToArray();
				Verts[1].SharedVertexCoordinates = sharedVertCoords1_temp.ToArray();
				Verts[2].SharedVertexCoordinates = sharedVertCoords2_temp.ToArray();
			}
		}

		public void CalculateNormals( LNX_NavMesh nm, bool logMessages = true)
		{
			DbgCalculateTriInfo += $"{nameof(CalculateNormals)}() report\n";

			v_projectionNormal = nm.GetSurfaceNormal(); //yes, this is already done in the ctor, but I might want to call this again to regenerate normals at some point.

			RaycastHit rcHit = new RaycastHit();

			#region CALCULATE DERIVED NORMAL ----------------------------------
			Vector3 castDir = Vector3.Cross(
				Vector3.Normalize(Verts[0].V_Position - Verts[1].V_Position),
				Vector3.Normalize(Verts[2].V_Position - Verts[1].V_Position)
			);
			v_derivedNormal = -castDir.normalized; //just to set one by default because why not...

			DbgCalculateTriInfo += $"castdir decided to be: '{castDir}'\n";

			if (
				Physics.Linecast(V_Center - (castDir.normalized * 0.3f),
				V_Center + (castDir.normalized * 0.3f),
				out rcHit, nm.CachedLayerMask))
			{
				DbgCalculateTriInfo += $"rc1 success. hit at: '{rcHit.point}'\n";
				v_sampledNormal = rcHit.normal;
				v_derivedNormal = -castDir.normalized;
			}
			else if (
				Physics.Linecast(V_Center + (castDir.normalized * 0.3f),
				V_Center - (castDir.normalized * 0.3f),
				out rcHit, nm.CachedLayerMask))
			{
				DbgCalculateTriInfo += $"rc2 success\n";
				v_sampledNormal = rcHit.normal;
				v_derivedNormal = castDir.normalized;
			}
			#endregion

			#region CALCULATE SAMPLED NORMAL--------------------------------------------------
			if (
				v_projectionNormal != Vector3.zero &&
				Physics.Linecast(V_Center + (v_projectionNormal * 0.3f),
				V_Center + (-v_projectionNormal * 0.3f),
				out rcHit, nm.CachedLayerMask))
			{
				DbgCalculateTriInfo += $"linecast success. hit at: '{rcHit.point}'\n";
				v_sampledNormal = rcHit.normal;
			}
			else
			{
				v_sampledNormal = Vector3.zero;
				DbgCalculateTriInfo += $"";
			}
			#endregion
		}

		#region MAIN API METHODS----------------------------------------------------------------------

		public string DBG_IsInShapeProjectAlongNormal;

		/// <summary>
		/// Determines if a supplied position is within a theoretical sweep 
		/// (or cast) of the triangle's shape along it's normal direction.
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public bool IsInShapeProject( Vector3 pos, out Vector3 projectedPos )
		{
			//todo: currently, it doesn't set projectedPos to the correct "out" value
			DBG_IsInShapeProjectAlongNormal = "";

			Vector3 flatPos = GetFlattenedPosition( pos );
			Vector3 v_ctrToPos = pos - v_flattenedCenter;

			Vector3 flatProjectedPos = v_flattenedCenter + (flatPos - v_ctrToPos);

			projectedPos = V_Center + Vector3.ProjectOnPlane( v_ctrToPos, v_derivedNormal );

			//DBG_IsInShapeProjectAlongNormal += $"first, is it on edge: '{IsPositionOnAnyEdge(projectedPos)}'\n";

			if ( !Verts[0].IsInFlatCenterSweep(flatProjectedPos) )
			{
				DBG_IsInShapeProjectAlongNormal += $"vrt0: \n" +
					$"{Verts[0].DBG_IsInCenterSweep}\n";

				DBG_IsInShapeProjectAlongNormal += $"vert0 center sweep failed. Returning false...";

				return false;
			}

			DBG_IsInShapeProjectAlongNormal += $"vrt0: \n" +
				$"{Verts[0].DBG_IsInCenterSweep}\n";

			if ( !Verts[1].IsInFlatCenterSweep(flatProjectedPos) )
			{
				DBG_IsInShapeProjectAlongNormal += $"vrt1: \n" +
					$"{Verts[1].DBG_IsInCenterSweep}\n";

				DBG_IsInShapeProjectAlongNormal += $"vert1 center sweep failed. Returning false...";

				return false;
			}
			DBG_IsInShapeProjectAlongNormal += $"vrt1: \n" +
				$"{Verts[1].DBG_IsInCenterSweep}\n";

			if ( !Verts[2].IsInFlatCenterSweep(flatProjectedPos) )
			{
				DBG_IsInShapeProjectAlongNormal += $"vrt2: \n" +
					$"{Verts[2].DBG_IsInCenterSweep}\n";

				DBG_IsInShapeProjectAlongNormal += $"vert2 center sweep failed. Returning false...";

				return false;
			}
			DBG_IsInShapeProjectAlongNormal += $"vrt2: \n" +
				$"{Verts[2].DBG_IsInCenterSweep}\n";

			return true;
		}

		public float DistanceToNearestVert( Vector3 pos )
		{
			return Mathf.Min(
				Vector3.Distance(pos, V_Center),
				Vector3.Distance(pos, Verts[0].V_Position),
				Vector3.Distance(pos, Verts[1].V_Position),
				Vector3.Distance(pos, Verts[2].V_Position)
			);
		}

		public Vector3 ClosestPointOnPerimeter( Vector3 pos )
		{
			//Debug.Log($"Edges.length: '{Edges.Length}'");
			Vector3 vA = Edges[0].ClosestPointOnEdge( pos );
			Vector3 vB = Edges[1].ClosestPointOnEdge( pos );
			Vector3 vC = Edges[2].ClosestPointOnEdge( pos );

			float distToA = Vector3.Distance( pos, vA );
			float distToB = Vector3.Distance( pos, vB );
			float distToC = Vector3.Distance( pos, vC );

			if( distToA < distToB && distToA < distToC )
			{
				return vA;
			}
			else if( distToB < distToA && distToB < distToC )
			{
				return vB;
			}
			else
			{
				return vC;
			}
		}

		public bool IsPositionOnAnyEdge(Vector3 pos)
		{
			if( IsPositionOnGivenEdge(pos, 0) )
			{
				return true;
			}
			else if( IsPositionOnGivenEdge(pos, 1) )
			{
				return true;
			}
			else if ( IsPositionOnGivenEdge(pos, 2) )
			{
				return true;
			}

			return false;
		}

		public bool IsPositionOnGivenEdge( Vector3 pos, int edgeIndex )
		{
			if( pos == Edges[edgeIndex].StartPosition || pos == Edges[edgeIndex].EndPosition ||
				Vector3.Normalize(pos - Edges[edgeIndex].StartPosition) == Edges[edgeIndex].v_startToEnd )
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Checks if a line drawn from origin to destination runs through one of this triangle's edges
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="destination"></param>
		/// <param name="edgeIndex"></param>
		/// <returns></returns>
		public bool DoesProjectionIntersectEdge(Vector3 origin, Vector3 destination, int edgeIndex, out Vector3 outPos )
		{
			string dbg = "";
			int opposingVertIndex = Edges[edgeIndex].GetOpposingVertIndex();
			dbg += $"oppVrt: '{opposingVertIndex}'. vrtA: '{Edges[edgeIndex].StartVertCoordinate.ComponentIndex}', " +
				$"vrtB: '{Edges[edgeIndex].EndVertCoordinate.ComponentIndex}'\n";

			Vector3 v_project = Vector3.zero;
			Vector3 v_origin_to_StartVert = Vector3.zero;
			Vector3 v_origin_to_endVert = Vector3.zero;

			if ( v_sampledNormal == Vector3.zero)
			{
				dbg += "using flat vector...";
				v_project = LNX_Utils.FlatVector( destination - origin );
				//Vector3 v_project = ( destination - origin ); //Note: don't normalize this here. Apparently it can cause this to return false when it should return true at a very specific angle

				v_origin_to_StartVert = LNX_Utils.FlatVector(
					Verts[Edges[edgeIndex].StartVertCoordinate.ComponentIndex].V_Position - origin
				);
				v_origin_to_endVert = LNX_Utils.FlatVector(
					Verts[Edges[edgeIndex].EndVertCoordinate.ComponentIndex].V_Position - origin
				);
			}
			else
			{
				dbg += "using plane project...";

				v_project = Vector3.ProjectOnPlane( destination - origin, v_sampledNormal );

				v_origin_to_StartVert = Vector3.ProjectOnPlane(Verts[Edges[edgeIndex].StartVertCoordinate.ComponentIndex].V_Position - origin, v_sampledNormal );
				v_origin_to_endVert = Vector3.ProjectOnPlane( Verts[Edges[edgeIndex].EndVertCoordinate.ComponentIndex].V_Position - origin, v_sampledNormal );
			}


			float chevronAngle = Vector3.Angle( v_origin_to_StartVert.normalized, v_origin_to_endVert.normalized );

			//Note: Currently this method uses a bit of a hack, but it seems to me like it will always work. The following two angle
			//calculations don't work in all instances, because they count up to 180, then back down after the threshold is crossed.
			//They would ideally go from 0 to 360. Later on, a magic number is used to check that the sum of both of these plus 
			//the magic number are above the chevron angle. This is because, due to rounding, the two angles can add up to slightly 
			//beyond what they actually are...
			//float ang_prjctToStartVrt = Vector3.Angle( LNX_Utils.FlatVector(v_project).normalized, v_origin_to_StartVert.normalized);
			//float ang_prjctToEndVrt = Vector3.Angle( LNX_Utils.FlatVector(v_project).normalized, v_origin_to_endVert.normalized );
			float ang_prjctToStartVrt = Vector3.Angle( v_project.normalized, v_origin_to_StartVert.normalized );
			float ang_prjctToEndVrt = Vector3.Angle( v_project.normalized, v_origin_to_endVert.normalized );

			dbg += $"chev: '{chevronAngle}', 1: '{ang_prjctToStartVrt}', 2: '{ang_prjctToEndVrt}'. sum: '{ang_prjctToStartVrt+ang_prjctToEndVrt}'\n";

			//Debug.Log( dbg );

			float diff = (ang_prjctToStartVrt + ang_prjctToEndVrt) - chevronAngle;
			dbg += $"diff: '{diff}'\n" +
				$"";

			// note: the following has a magic number. This is a hack for now. I do this because if I just use '(angle1 + angle2) > chevronAngle', there 
			// will sometimes be a rounding error that will make the added number a tiny amount larger than chevronAngle when the destination is truly 
			// projected on the edge
			if ( ang_prjctToStartVrt > chevronAngle || ang_prjctToEndVrt > chevronAngle || ((ang_prjctToStartVrt + ang_prjctToEndVrt) > (chevronAngle + 0.01f)) )
			{
				outPos = Vector3.zero;
				//Debug.Log(dbg);
				return false;
			}

			#region calculate projection position using the law of sines ------------------------------------------
			float len_orgnToStrtPos = Vector3.Distance( origin, Edges[edgeIndex].StartPosition );
			float ang_atEdgeStart = Vector3.Angle( Edges[edgeIndex].v_startToEnd, origin - Edges[edgeIndex].StartPosition );
			float ang_atOutPos = 180f - (Vector3.Angle(v_project, v_origin_to_StartVert) + ang_atEdgeStart);

			//float lenX = (Mathf.Sin(ang_atEdgeStart) * len_orgnToStrtPos) / Mathf.Sin(ang_atOutPos);
			float lenX = (len_orgnToStrtPos / Mathf.Sin(ang_atOutPos * Mathf.Deg2Rad)) * Mathf.Sin(ang_atEdgeStart * Mathf.Deg2Rad);

			outPos = origin + (v_project.normalized * lenX);
			#endregion

			dbg += $"\n{nameof(ang_atEdgeStart)}: '{ang_atEdgeStart}\n" +
				$"{nameof(ang_prjctToStartVrt)}: '{ang_prjctToStartVrt}'\n" +
				$"{nameof(ang_atOutPos)}: '{ang_atOutPos}'\n" +
				$"{nameof(len_orgnToStrtPos)}: '{len_orgnToStrtPos}'\n" +
				$"{nameof(lenX)}: '{lenX}'\n" +
				$"";

			//Debug.Log(dbg );

			return true;
		}

		[TextArea(1,5)] public string dbg_prjctThrhToPerim;
		public Vector3 ProjectThroughToPerimeter( Vector3 innerPos, Vector3 outerPos, out LNX_Edge outedge, LNX_Direction meshProjectionDir = LNX_Direction.PositiveY )
		{
			dbg_prjctThrhToPerim = "";
			outedge = null;

			Vector3 projectedEdgePosition = Vector3.zero;

			dbg_prjctThrhToPerim += $"{nameof(IsPositionOnGivenEdge)}(0): '{IsPositionOnGivenEdge(innerPos, 0)}'\n" +
				$"{nameof(IsPositionOnGivenEdge)}(1): '{IsPositionOnGivenEdge(innerPos, 1)}'\n" +
				$"{nameof(IsPositionOnGivenEdge)}(2): '{IsPositionOnGivenEdge(innerPos, 2)}'\n";

			if ( !IsPositionOnGivenEdge(innerPos, 0) && DoesProjectionIntersectEdge(innerPos, outerPos, 0, out projectedEdgePosition) )
			{
				outedge = Edges[0];
				dbg_prjctThrhToPerim += $"edge 0 succeeded";
			}
			else if( !IsPositionOnGivenEdge(innerPos, 1) && DoesProjectionIntersectEdge(innerPos,outerPos, 1, out projectedEdgePosition) )
			{
				outedge = Edges[1];
				dbg_prjctThrhToPerim += $"edge 1 succeeded";

			}
			else if( !IsPositionOnGivenEdge(innerPos, 2) && DoesProjectionIntersectEdge(innerPos, outerPos, 2, out projectedEdgePosition) )
			{
				outedge = Edges[2];
				dbg_prjctThrhToPerim += $"edge 2 succeeded";

			}
			else
			{
				outedge = null;
				dbg_prjctThrhToPerim += $"NONE succeeded. returning null...";

			}

			return projectedEdgePosition;

			//oldway..............................................................
			/*
			innerPos = LNX_Utils.FlatVector( innerPos, meshProjectionDir );
			outerPos = LNX_Utils.FlatVector( outerPos, meshProjectionDir );

			Vector3 v_dir = Vector3.Normalize( outerPos - innerPos );
			dbgPerim = $"vc0: '{Edges[0].v_cross}', vc1: '{Edges[1].v_cross}', vc2: '{Edges[2].v_cross}'\n";

			#region Find opposing edge...........................
			//note: the dot product of edge 0 isn't necessary as we can assume it's this one for sure if the other two don't work...
			float dotProd_edge1 = Vector3.Dot( -Edges[1].v_cross, v_dir );
			float dotProd_edge2 = Vector3.Dot( -Edges[2].v_cross, v_dir );

			int opposingEdge = 0;

			dbgPerim += $"{nameof(dotProd_edge1)}: '{dotProd_edge1}'\n" +
				$"{nameof(dotProd_edge2)}: '{dotProd_edge2}'\n";

			if( dotProd_edge1 > 0 && Edges[1].IsProjectedPointOnEdge(innerPos, outerPos) )
			{
				dbgPerim += $"if-chose 1\n";
				opposingEdge = 1;
			}
			else if ( dotProd_edge2 > 0 && Edges[2].IsProjectedPointOnEdge(innerPos, outerPos) )
			{
				dbgPerim += $"if-chose 2\n";
				opposingEdge = 2;
			}
			else
			{
				dbgPerim += "defaulted to 0\n";
			}

				outedge = Edges[opposingEdge];


			#endregion

			float lengthA = Vector3.Distance( innerPos, Edges[opposingEdge].StartPosition );
			float angA = Vector3.Angle( -v_dir, Edges[opposingEdge].v_endToStart );

			float angX = Vector3.Angle( 
				Vector3.Normalize(innerPos - Edges[opposingEdge].StartPosition),
				Edges[opposingEdge].v_startToEnd
			);

			float lengthX = Mathf.Sin(Mathf.Deg2Rad * angX) * ( lengthA / Mathf.Sin(Mathf.Deg2Rad * angA) );

			//Debug.Log( dbgPerim );

			return innerPos + (v_dir * lengthX);
			*/
		}
		#endregion

		#region MODIFICATION ----------------------------------------------------
		/// <summary>
		/// Takes in a previously-modified triangle, and gives this triangle the same values. This is 
		/// used when re-making a mesh.
		/// </summary>
		/// <param name="baseTri"></param>
		public void AdoptModifiedValues(LNX_Triangle baseTri)
		{
			v_sampledNormal = baseTri.v_sampledNormal;

			Verts[0].AdoptValues(baseTri.Verts[0]);
			Verts[1].AdoptValues(baseTri.Verts[1]);
			Verts[2].AdoptValues(baseTri.Verts[2]);

			CalculateDerivedInfo();
		}

		/// <summary>
		/// Movies a vertex belonging to this triangle in a managed fashion. Sets appropriate flags and 
		/// does what's necessary after a movement has been made.
		/// </summary>
		/// <param name="vertIndex"></param>
		/// <param name="pos"></param>
		/// <param name="positionIsAbsolute"></param>
		public void MoveVert_managed( LNX_NavMesh nm, int vertIndex, Vector3 pos, bool positionIsAbsolute = false )
		{
			Verts[vertIndex].V_Position = (positionIsAbsolute ? pos : Verts[vertIndex].V_Position + pos);

			dirtyFlag_repositionedVert = true;

			for ( int i = 0; i < AdjacentTriIndices.Length; i++ )
			{
				nm.Triangles[AdjacentTriIndices[i]].ForceMarkDirty();
			}
		}

		public void ForceMarkDirty()
		{
			dirtyFlag_repositionedVert = true;
		}

		public void ClearModifications()
		{
			//Debug.Log($"tri[{index_inCollection}].ClearModifications()");

			if( Verts[0].AmModified )
			{
				Verts[0].V_Position = Verts[0].OriginalPosition;
				Debug.LogWarning($"vert 0 was modified");
			}
			if ( Verts[1].AmModified )
			{
				Verts[1].V_Position = Verts[1].OriginalPosition;
				Debug.LogWarning($"vert 1 was modified");

			}
			if ( Verts[2].AmModified )
			{
				Verts[2].V_Position = Verts[2].OriginalPosition;
				Debug.LogWarning($"vert 2 was modified");

			}
		}
		#endregion

		/// <summary>
		/// Returns true if the tri with the supplied index is touching this triangle at any vertex.
		/// </summary>
		/// <param name="indx"></param>
		/// <returns></returns>
		public bool AmAdjacentToTri( int indx ) //todo: unit test
		{
			if( AdjacentTriIndices.Length > 0 )
			{
				for( int i = 0; i < AdjacentTriIndices.Length; i++ )
				{
					if( AdjacentTriIndices[i] == indx )
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Returns true if the tri with the supplied index is touching this triangle at any vertex.
		/// </summary>
		/// <param name="indx"></param>
		/// <returns></returns>
		public bool AmAdjacentToTri( LNX_Triangle tri ) //todo: unit test
		{
			if ( AdjacentTriIndices.Length > 0 )
			{
				for ( int i = 0; i < AdjacentTriIndices.Length; i++ )
				{
					if (AdjacentTriIndices[i] == tri.index_inCollection )
					{
						return true;
					}
				}
			}

			return false;
		}

		#region GETTERS/IDENTIFIERS -----------------------------------------------------
		/// <summary>
		/// Returns any vertices owned by this triangle that exist at the supplied position.
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public int GetVertIndextAtPosition( Vector3 pos )
		{
			if ( Verts[0].V_Position == pos )
			{
				return 0;
			}
			else if( Verts[1].V_Position == pos )
			{
				return 1;
			}
			else if ( Verts[2].V_Position == pos )
			{
				return 2;
			}

			return -1;
		}
		/// <summary>
		/// Returns any vertices owned by this triangle that originally existed at the supplied position before 
		/// being modified.
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public int GetVertIndextAtOriginalPosition(Vector3 pos)
		{
			if (Verts[0].OriginalPosition == pos)
			{
				return 0;
			}
			else if (Verts[1].OriginalPosition == pos)
			{
				return 1;
			}
			else if (Verts[2].OriginalPosition == pos)
			{
				return 2;
			}

			return -1;
		}

		public Vector3 GetFlattenedPosition(Vector3 pos)
		{
			if ( v_projectionNormal == Vector3.up || v_projectionNormal == Vector3.down )
			{
				return new Vector3(pos.x, 0f, pos.z);
			}
			else if ( v_projectionNormal == Vector3.right || v_projectionNormal == Vector3.left )
			{
				return new Vector3(0f, pos.y, pos.z);
			}
			else if (v_projectionNormal == Vector3.forward || v_projectionNormal == Vector3.back)
			{
				return new Vector3(pos.x, pos.y, 0f);
			}

			return Vector3.zero;
		}
		#endregion

		public void Ping( LNX_Triangle[] tris )
		{
			Verts[0].Ping( tris );
			Verts[1].Ping( tris );
			Verts[2].Ping( tris );
		}

		public void SayCurrentInfo()
		{
			Debug.Log($"Triangle.{nameof(SayCurrentInfo)}()...\n" +
				$"{nameof(index_inCollection)}: '{index_inCollection}'\n" +
				$"{nameof(MeshIndex_trianglesStart)}: '{MeshIndex_trianglesStart}'\n" +
				$"{nameof(v_sampledNormal)}: '{v_sampledNormal}'\n" +
				$"");

			Edges[0].SayCurrentInfo();
			Edges[1].SayCurrentInfo();
			Edges[2].SayCurrentInfo();
		}
	}
}