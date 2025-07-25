using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;


namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_Triangle
	{
		public string name;

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

		/// <summary>The center of the triangle "flattened" with respect to the surface orientation of the navmesh.</summary>
		public Vector3 V_FlattenedCenter => LNX_Utils.FlatVector( V_Center, v_projectionNormal );
		/// <summary>Distance around the triangle</summary>
		public float Perimeter
		{
			get
			{
				return Edges[0].EdgeLength + Edges[1].EdgeLength + Edges[2].EdgeLength;
			}
		}
		/// <summary>The longest edge of this triangle. This can be used for effecient decision-making. 
		/// IE: IF a position's distance from this triangle's center is greater than half of this value, it can't possibly 
		/// be on this triangle</summary>
		public float LongestEdgeLength
		{
			get
			{
				return Mathf.Max( Edges[0].EdgeLength, Edges[1].EdgeLength, Edges[2].EdgeLength );
			}
		}
		public float ShortestEdgeLength
		{
			get
			{
				return Mathf.Min( Edges[0].EdgeLength, Edges[1].EdgeLength, Edges[2].EdgeLength );
			}
		}
		public float Area
		{
			get //"Heron's formula"
			{
				return Mathf.Sqrt(
				Perimeter * 0.5f *
				((Perimeter * 0.5f) - Edges[0].EdgeLength) *
				((Perimeter * 0.5f) - Edges[1].EdgeLength) *
				((Perimeter * 0.5f) - Edges[2].EdgeLength)
				);
			}
		}

		public float Slope //todo: this needs to account for when triangles are completely facing the wrong direction. IE: when they're "kinked"
		{
			get
			{
				return Vector3.Angle( v_projectionNormal, v_derivedNormal );
			}
		}

		/// <summary>The quaternion describing the angle that this triangle is "facing" compared to the projection of the navmesh</summary>
		public Quaternion Rotation
		{
			get
			{
				return Quaternion.FromToRotation(v_projectionNormal, v_derivedNormal);
			}
		}

		[Header("RELATIONAL")]
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
		/// <summary>Gets the best normal this triangle is able to provide for pathing over it's surface. </summary>
		public Vector3 V_PathingNormal
		{
			get
			{
				return v_derivedNormal != Vector3.zero ? v_derivedNormal : v_projectionNormal;
			}
		}

		[Header("DEBUG")]
		[TextArea(0,10)] public string DBG_Class;
		[SerializeField] private string DbgCalculateTriInfo;
		/*[TextArea(0, 10)]*/
		[HideInInspector] public string DBG_Relationships;

		public LNX_Triangle( int parallelIndex, int areaIndx, Vector3 vrtPos0, Vector3 vrtPos1, Vector3 vrtPos2, LNX_NavMesh navMesh )
		{
			//Debug.Log($"tri ctor. {nameof(parallelIndex)}: '{parallelIndex}' (x3: '{parallelIndex * 3}'). verts start: '{nmTriangulation.indices[(parallelIndex * 3)]}'");

			DBG_Class = $"ctor '({DateTime.Now.ToString()})'...\n";

			index_inCollection = parallelIndex;

			AreaIndex = areaIndx;
			v_projectionNormal = navMesh.GetSurfaceNormal();

			Verts = new LNX_Vertex[3];
			Verts[0] = new LNX_Vertex( navMesh, vrtPos0, index_inCollection, 0 );
			Verts[1] = new LNX_Vertex( navMesh, vrtPos1, index_inCollection, 1 );
			Verts[2] = new LNX_Vertex( navMesh, vrtPos2, index_inCollection, 2 );

			V_Center = (Verts[0].V_Position + Verts[1].V_Position + Verts[2].V_Position) / 3f;

			//v_flattenedCenter = GetFlattenedPosition( V_Center ); //dws

			Edges = new LNX_Edge[3];
			Edges[0] = new LNX_Edge( navMesh, Verts[1], Verts[2], index_inCollection, 0 );
			Edges[1] = new LNX_Edge( navMesh, Verts[0], Verts[2], index_inCollection, 1 );
			Edges[2] = new LNX_Edge( navMesh, Verts[1], Verts[0], index_inCollection, 2 );

			CalculateNormals( navMesh, true ); //needs to happen before calculating derived info

			CalculateDerivedInfo();

			DBG_Class += $"\nEnd of ctor(). Report:\n" +
				$"{nameof(V_Center)}: '{V_Center}'\n" +
				$"{nameof(V_FlattenedCenter)}: '{V_FlattenedCenter}'\n" +
				$"nrml (smpld): '{v_sampledNormal}', prjctd: '{v_projectionNormal}'\n" +
				$"derivdNrml: '{v_derivedNormal}'\n" +
				$"edge lengths: '{Edges[0].EdgeLength}', '{Edges[1].EdgeLength}', '{Edges[2].EdgeLength}'\n" +
				$"Prmtr: '{Perimeter}', Area: '{AreaIndex}'\n\n";

			DbgCalculateTriInfo = $"{nameof(V_Center)}: '{V_Center}'\n" +
				$"{nameof(V_FlattenedCenter)}: '{V_FlattenedCenter}'\n" +
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
			AreaIndex = baseTri.AreaIndex;

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

			name = $"ind: '{index_inCollection}', ctr: '{V_Center}'";
		}

		public void RefreshTriangle( LNX_NavMesh nm, bool logMessages = true )
		{
			if ( dirtyFlag_repositionedVert )
			{
				CalculateDerivedInfo( logMessages );

				CreateRelationships( nm );

				dirtyFlag_repositionedVert = false;
			}
		}

		/// <summary>
		/// Creates the relationship structures relating this triangle to all other triangles.
		/// </summary>
		/// <param name="Tris"></param>
		/// <param name="amThorough">If false, bypasses re-processing the sharedvertexcoordinates collections, 
		/// which is not necessary in some cases.</param>
		public void CreateRelationships( LNX_NavMesh navmsh )
		{
			DBG_Relationships = $"Triangle[{index_inCollection}].{nameof(CreateRelationships)}(): '{DateTime.Now.ToString()}'...\n";
			Debug.Log( DBG_Relationships );
			
			DBG_Relationships += $"amThrough true. Creating vertex relationships...\n";

			#region Create Vertex relationships...
			DBG_Relationships += $"Creating first vertex relationship...\n";
			Verts[0].CreateRelationships( navmsh ); //ERRORTRACE 4: This fails for Triangle 0 (the first in the list)...

			DBG_Relationships += $"Creating second vertex relationship...\n";
			Verts[1].CreateRelationships( navmsh );

			DBG_Relationships += $"Creating third vertex relationship...\n";
			Verts[2].CreateRelationships( navmsh );
			#endregion

			List<int> foundAdjacentTriIndices_temp = new List<int>();
			List<LNX_ComponentCoordinate> sharedVertCoords0_temp = new List<LNX_ComponentCoordinate>();
			List<LNX_ComponentCoordinate> sharedVertCoords1_temp = new List<LNX_ComponentCoordinate>();
			List<LNX_ComponentCoordinate> sharedVertCoords2_temp = new List<LNX_ComponentCoordinate>();

			for ( int i_otherTri = 0; i_otherTri < navmsh.Triangles.Length; i_otherTri++ )
			{
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
							navmsh.Triangles[i_otherTri].Verts[Relationships[i_otherTri].IndexMap_OwnedVerts_toShared[0]].MyCoordinate
						);
					}
					if ( Relationships[i_otherTri].IndexMap_OwnedVerts_toShared[1] != -1 )
					{
						sharedVertCoords1_temp.Add(
							navmsh.Triangles[i_otherTri].Verts[Relationships[i_otherTri].IndexMap_OwnedVerts_toShared[1]].MyCoordinate
						);
					}
					if ( Relationships[i_otherTri].IndexMap_OwnedVerts_toShared[2] != -1 )
					{
						sharedVertCoords2_temp.Add(
							navmsh.Triangles[i_otherTri].Verts[Relationships[i_otherTri].IndexMap_OwnedVerts_toShared[2]].MyCoordinate
						);
					}
				}
			}

			AdjacentTriIndices = foundAdjacentTriIndices_temp.ToArray();

			Verts[0].SharedVertexCoordinates = sharedVertCoords0_temp.ToArray(); //todo: can I do this now in the vertex setRelationships method?
			Verts[1].SharedVertexCoordinates = sharedVertCoords1_temp.ToArray();
			Verts[2].SharedVertexCoordinates = sharedVertCoords2_temp.ToArray();
			
			DBG_Relationships += $"end of CreateRelationships()\n";
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

		public string DBG_IsInShapeProject;

		/// <summary>
		/// Determines if a supplied position is within a theoretical sweep 
		/// (or cast) of the triangle's shape along the direction of the navmesh orientation.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="projectedPos">supplied position projected onto the surface of this triangle</param>
		/// <returns></returns>
		public bool IsInShapeProject( Vector3 pos, out Vector3 projectedPos ) //todo: this one doesn't return correct if triangle surface is slanted
		{
			//todo: currently, it doesn't set projectedPos to the correct "out" value
			DBG_IsInShapeProject = $"tri[{index_inCollection}].IsInShapeProject({pos})\n";

			if ( !Verts[0].IsInFlatCenterSweep(pos) )
			{
				DBG_IsInShapeProject += $"vrt0: \n" +
					$"{Verts[0].DBG_IsInCenterSweep}\n";

				DBG_IsInShapeProject += $"vert0 center sweep failed. Returning false...";

				projectedPos = Vector3.zero;
				return false;
			}

			DBG_IsInShapeProject += $"vrt0: \n" +
				$"{Verts[0].DBG_IsInCenterSweep}\n";

			if ( !Verts[1].IsInFlatCenterSweep(pos) ) //ERRORTRACE 9
			{
				DBG_IsInShapeProject += $"vrt1: \n" +
					$"{Verts[1].DBG_IsInCenterSweep}\n";

				DBG_IsInShapeProject += $"vert1 center sweep failed. Returning false...";

				projectedPos = Vector3.zero;
				return false;
			}
			DBG_IsInShapeProject += $"vrt1: \n" +
				$"{Verts[1].DBG_IsInCenterSweep}\n";

			if ( !Verts[2].IsInFlatCenterSweep(pos) )
			{
				DBG_IsInShapeProject += $"vrt2: \n" +
					$"{Verts[2].DBG_IsInCenterSweep}\n";

				DBG_IsInShapeProject += $"vert2 center sweep failed. Returning false...";

				projectedPos = Vector3.zero;
				return false;
			}
			DBG_IsInShapeProject += $"vrt2: \n" +
				$"{Verts[2].DBG_IsInCenterSweep}\n";

			#region DETERMINE THE PROJECTED POSITION--------------------------------------
			Vector3 flatPos = LNX_Utils.FlatVector(pos, v_projectionNormal);
			DBG_IsInShapeProject += $"flatpos: '{flatPos}'\n";

			if (Slope == 0f)
			{
				projectedPos = flatPos;
			}
			else
			{

				//Trying quaternion multiplication----------------------------------------
				/*
				//I still feel like this is worth investigating further because it reads so much better. I believe the reason the 
				result is a little bent looking is because I'm rotating a directional vector around an axis, which is going to 
				affect the x and z, when I actually need the x and z to remain the same.
				Vector3 v_fltCtr_to_fltPos = flatPos - V_FlattenedCenter;
				Vector3 v_ctrToPos = pos - V_Center;

				//projectedPos = V_Center + (Rotation * v_fltCtr_to_fltPos); //bent
				projectedPos = V_Center + (Rotation * LNX_Utils.FlatVector(v_ctrToPos, v_projectionNormal) ); //also bent...why? looks like the exact same result

				Vector3 vtry = Vector3.Project(  );
				*/
				//-------------------------------------------------------

				
				//Use the law of sines...
				// Note: This isn't currently perfect. It seems to create a point slightly below the surface of the triangle.
				Vector3 edgePrjct = Edges[1].StartPosition + 
					Vector3.Project(pos - Edges[1].StartPosition, Edges[1].v_startToEnd);//this gets a point on the edge closest to the pos
				DBG_IsInShapeProject += $"edjprjct: '{edgePrjct.y}'\n";
				//float lenA = Vector3.Distance(edgePrjct, flatPos); //orig
				float lenA = Vector3.Distance( LNX_Utils.FlatVector(edgePrjct, v_projectionNormal), flatPos );

				float angA = 90f * Mathf.Deg2Rad;
				float angC = Slope * Mathf.Deg2Rad;
				float lenC = (Mathf.Sin(angC) * lenA) / MathF.Sin(angA);
				//projectedPos = flatPos + (v_projectionNormal * lenC);
				projectedPos = LNX_Utils.FlooredVector(flatPos, edgePrjct, v_projectionNormal) + (v_projectionNormal * lenC);
			}
			#endregion

			DBG_IsInShapeProject += $"projectedPos: '{projectedPos.y}'\n";

			return true;
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

		public bool IsPositionProjectedOnAnyEdge(Vector3 pos, bool flatten = false)
		{
			if (flatten)
			{
				pos = LNX_Utils.FlatVector( pos, v_projectionNormal );
			}

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

		public string DBG_IsPositionOnGivenEdge;
		public bool IsPositionOnGivenEdge( Vector3 pos, int edgeIndex )
		{
			Vector3 fltndPos = LNX_Utils.FlatVector( pos, v_projectionNormal );

			//Vector3 v_startToPos_fltnd = LNX_Utils.FlatVector( //oldway, maybe this is actually better...
				//pos - Edges[edgeIndex].StartPosition, v_projectionNormal
			//).normalized;

			float ang = Vector3.Angle(
				LNX_Utils.FlatVector(fltndPos - Edges[edgeIndex].StartPosition).normalized,
				LNX_Utils.FlatVector(Edges[edgeIndex].v_startToEnd, v_projectionNormal).normalized
			);

			DBG_IsPositionOnGivenEdge = $"{nameof(IsPositionOnGivenEdge)}({pos}, {edgeIndex})\n" +
				$"using flattenedPos: '{fltndPos}\n" +
				$"angle: '{ang}'\n";

			if ( 
				fltndPos == LNX_Utils.FlatVector(Edges[edgeIndex].StartPosition, v_projectionNormal) ||
				//v_startToPos_fltnd == Edges[edgeIndex].v_startToEnd //oldway, maybe better
				ang < 0.08f
			)
			{
				DBG_IsPositionOnGivenEdge += $"found true...";
				return true;
			}
			else
			{
				DBG_IsPositionOnGivenEdge += $"found false...";

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
		public bool DoesProjectionIntersectGivenEdge(Vector3 origin, Vector3 destination, int edgeIndex, out Vector3 outPos )
		{
			string dbg = "";
			int opposingVertIndex = Edges[edgeIndex].GetOpposingVertIndex();
			dbg += $"oppVrt: '{opposingVertIndex}'. vrtA: '{Edges[edgeIndex].StartVertCoordinate.ComponentIndex}', " +
				$"vrtB: '{Edges[edgeIndex].EndVertCoordinate.ComponentIndex}'\n";

			/* //old way. dws
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
			*/

			Vector3 v_project = LNX_Utils.FlatVector( destination - origin, v_projectionNormal ).normalized;

			Vector3 v_origin_to_StartVert = LNX_Utils.FlatVector(
				Verts[Edges[edgeIndex].StartVertCoordinate.ComponentIndex].V_Position - origin,
				v_projectionNormal
			).normalized;

			Vector3 v_origin_to_endVert = LNX_Utils.FlatVector(
				Verts[Edges[edgeIndex].EndVertCoordinate.ComponentIndex].V_Position - origin,
				v_projectionNormal
			).normalized;

			float chevronAngle = Vector3.Angle( v_origin_to_StartVert, v_origin_to_endVert );

			//Note: Currently this method uses a bit of a hack, but it seems to me like it will always work. The following two angle
			//calculations don't work in all instances, because they count up to 180, then back down after the threshold is crossed.
			//They would ideally go from 0 to 360. Later on, a magic number is used to check that the sum of both of these plus 
			//the magic number are above the chevron angle. This is because, due to rounding, the two angles can add up to slightly 
			//beyond what they actually are...
			//float ang_prjctToStartVrt = Vector3.Angle( LNX_Utils.FlatVector(v_project).normalized, v_origin_to_StartVert.normalized);
			//float ang_prjctToEndVrt = Vector3.Angle( LNX_Utils.FlatVector(v_project).normalized, v_origin_to_endVert.normalized );
			float ang_prjctToStartVrt = Vector3.Angle( v_project, v_origin_to_StartVert );
			float ang_prjctToEndVrt = Vector3.Angle( v_project, v_origin_to_endVert );

			dbg += $"chev: '{chevronAngle}', 1: '{ang_prjctToStartVrt}', 2: '{ang_prjctToEndVrt}'. " +
				$"sum: '{ang_prjctToStartVrt+ang_prjctToEndVrt}'\n" +
				$"diff: '{(ang_prjctToStartVrt + ang_prjctToEndVrt) - chevronAngle}'\n" +
				$"";

			// note: the following has a magic number. This is a hack for now. I do this because if I just use
			// '(angle1 + angle2) > chevronAngle', there will sometimes be a rounding error that will make the
			// added number a tiny amount larger than chevronAngle when the destination is truly projected on
			// the edge
			if ( ang_prjctToStartVrt > chevronAngle || 
				ang_prjctToEndVrt > chevronAngle || 
				((ang_prjctToStartVrt + ang_prjctToEndVrt) > (chevronAngle + 0.01f)) 
			)
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

			outPos = origin + (v_project * lenX);
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
		public LNX_ProjectionHit ProjectThroughToPerimeter( Vector3 innerPos, Vector3 outerPos )
		{
			dbg_prjctThrhToPerim = "";

			Vector3 projectedEdgePosition = Vector3.zero;

			Vector3 ftndInrPos = LNX_Utils.FlatVector( innerPos, v_projectionNormal );

			dbg_prjctThrhToPerim += $"\n{nameof(IsPositionOnGivenEdge)}(0): '{IsPositionOnGivenEdge(ftndInrPos, 0)}'\n" +
				$"{DBG_IsPositionOnGivenEdge}\n" +
				$"\n{nameof(IsPositionOnGivenEdge)}(1): '{IsPositionOnGivenEdge(ftndInrPos, 1)}'\n" +
				$"{DBG_IsPositionOnGivenEdge}\n" +
				$"\n{nameof(IsPositionOnGivenEdge)}(2): '{IsPositionOnGivenEdge(ftndInrPos, 2)}'\n" +
				$"{DBG_IsPositionOnGivenEdge}\n" +
				"";

			if ( !IsPositionOnGivenEdge(ftndInrPos, 0) && DoesProjectionIntersectGivenEdge(innerPos, outerPos, 0, out projectedEdgePosition) ) //todo: why am I calling isPositionOnGivenEdge?
			{
				dbg_prjctThrhToPerim += $"edge 0 succeeded";
				return new LNX_ProjectionHit( 0, projectedEdgePosition );
			}
			else if( !IsPositionOnGivenEdge(ftndInrPos, 1) && DoesProjectionIntersectGivenEdge(innerPos,outerPos, 1, out projectedEdgePosition) )
			{
				dbg_prjctThrhToPerim += $"edge 1 succeeded";
				return new LNX_ProjectionHit( 1, projectedEdgePosition );
			}
			else if( !IsPositionOnGivenEdge(ftndInrPos, 2) && DoesProjectionIntersectGivenEdge(innerPos, outerPos, 2, out projectedEdgePosition) )
			{
				dbg_prjctThrhToPerim += $"edge 2 succeeded";
				return new LNX_ProjectionHit( 2, projectedEdgePosition );
			}
			else
			{
				dbg_prjctThrhToPerim += $"NONE succeeded. returning null...";
				return LNX_ProjectionHit.None;
			}
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

		public void ChangeIndex_action(int newIndex)
		{
			index_inCollection = newIndex;

			Verts[0].TriIndexChanged(newIndex);
			Verts[1].TriIndexChanged(newIndex);
			Verts[2].TriIndexChanged(newIndex);

			Edges[0].TriIndexChanged(newIndex);
			Edges[1].TriIndexChanged(newIndex);
			Edges[2].TriIndexChanged(newIndex);

			name = $"ind: '{index_inCollection}', ctr: '{V_Center}'";
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
		public bool AmAdjacentToTri( int indx ) //todo: unit test
		{
			if ( AdjacentTriIndices.Length > 0 )
			{
				for ( int i = 0; i < AdjacentTriIndices.Length; i++ )
				{
					if (AdjacentTriIndices[i] == indx )
					{
						return true;
					}
				}
			}

			return false;
		}
		public bool AmAdjacentToTri(LNX_Triangle tri) //todo: unit test
		{
			return AmAdjacentToTri( tri.index_inCollection );
		}

		public int GetNumberOfSharedVerts( int triIndex )
		{
			if( triIndex == index_inCollection )
			{
				return -1;
			}

			return Verts[0].GetNumberOfSharedVerts(triIndex) + 
				Verts[1].GetNumberOfSharedVerts(triIndex) + 
				Verts[2].GetNumberOfSharedVerts(triIndex);
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

		public bool HasVertAtSamePosition( LNX_Vertex vert )
		{
			if( GetVertIndextAtPosition(vert.V_Position) > -1 )
			{
				return true;
			}
			else
			{
				return false;
			}
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

		/// <summary>
		/// Returns a point that describes the "lowest" point on the triangle with respect to the SurfaceOrientation of the navmesh.
		/// </summary>
		/// <returns></returns>
		private Vector3 GetProjectionBase()
		{
			float genrtdX = V_Center.x;
			float genrtdY = V_Center.y;
			float genrtdZ = V_Center.z;

			if( v_projectionNormal == Vector3.up || v_projectionNormal == Vector3.down )
			{
				genrtdY = Mathf.Min(Verts[0].V_Position.y, Mathf.Min(Verts[1].V_Position.y, Verts[2].V_Position.y) );
			}
			else if( v_projectionNormal == Vector3.forward || v_projectionNormal == Vector3.back )
			{
				genrtdZ = Mathf.Min( Verts[0].V_Position.z, Mathf.Min(Verts[1].V_Position.z, Verts[2].V_Position.z) );
			}
			else if ( v_projectionNormal == Vector3.right || v_projectionNormal == Vector3.left )
			{
				genrtdX = Mathf.Min(Verts[0].V_Position.x, Mathf.Min(Verts[1].V_Position.x, Verts[2].V_Position.x) );
			}

			return new Vector3 ( genrtdX, genrtdY, genrtdZ );
		}
		#endregion

		public void Ping( LNX_Triangle[] tris )
		{
			Verts[0].Ping( tris );
			Verts[1].Ping( tris );
			Verts[2].Ping( tris );
		}

		public string GetCurrentInfoString()
		{
			return $"Triangle.{nameof(SayCurrentInfo)}()...\n" +
				$"{nameof(index_inCollection)}: '{index_inCollection}'\n" +
				$"{nameof(V_Center)}: '{V_Center}'\n" +
				$"{nameof(MeshIndex_trianglesStart)}: '{MeshIndex_trianglesStart}'\n\n" +
				$"NORMALS-----------------------\n" +
				$"{nameof(v_sampledNormal)}: '{v_sampledNormal}'\n" +
				$"{nameof(v_derivedNormal)}: '{v_derivedNormal}'\n\n" +
				$"PROPERTIES-------------\n" +
				$"{nameof(V_FlattenedCenter)}: '{V_FlattenedCenter}'\n" +
				$"{nameof(Rotation)}: '{Rotation}'\n" +
				$"{nameof(Slope)}: '{Slope}'\n" +
				$"projection base: '{GetProjectionBase()}'\n\n" +
				$"Vertices---------------------\n\n" +
				$"{Verts[0].GetCurrentInfoString()}\n" +
				$"{Verts[1].GetCurrentInfoString()}\n" +
				$"{Verts[2].GetCurrentInfoString()}\n" +
				$"";
		}

		public void SayCurrentInfo()
		{
			Debug.Log( GetCurrentInfoString() );

			Verts[0].SayCurrentInfo();
			Verts[1].SayCurrentInfo();
			Verts[2].SayCurrentInfo();

			Edges[0].SayCurrentInfo();
			Edges[1].SayCurrentInfo();
			Edges[2].SayCurrentInfo();
		}
	}
}