using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;


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
		[HideInInspector] public Vector3 V_center;
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

		[Header("OTHER")]
		[HideInInspector] public Vector3 v_normal;

		public LNX_Triangle( int parallelIndex, int areaIndx, Vector3 vrtPos0, Vector3 vrtPos1, Vector3 vrtPos2, LNX_NavMesh navMesh )
		{
			//Debug.Log($"tri ctor. {nameof(parallelIndex)}: '{parallelIndex}' (x3: '{parallelIndex * 3}'). verts start: '{nmTriangulation.indices[(parallelIndex * 3)]}'");

			DbgCalculateTriInfo = string.Empty;

			index_inCollection = parallelIndex;

			AreaIndex = areaIndx;

			Verts = new LNX_Vertex[3];
			Verts[0] = new LNX_Vertex( this, vrtPos0, 0 );
			Verts[1] = new LNX_Vertex( this, vrtPos1, 1 );
			Verts[2] = new LNX_Vertex( this, vrtPos2, 2 );

			Edges = new LNX_Edge[3];
			Edges[0] = new LNX_Edge( this, Verts[1], Verts[2], 0);
			Edges[1] = new LNX_Edge( this, Verts[0], Verts[2], 1);
			Edges[2] = new LNX_Edge( this, Verts[1], Verts[0], 2);

			CalculateDerivedInfo();

			TrySampleNormal( navMesh.CachedLayerMask, true );

			DbgCalculateTriInfo += $"nrml: '{v_normal}'\n" +
				$"edge lengths: '{Edges[0].EdgeLength}', '{Edges[1].EdgeLength}', '{Edges[2].EdgeLength}'\n" +
				$"Prmtr: '{Perimeter}', Area: '{AreaIndex}'\n";
		}

		public void AdoptValues( LNX_Triangle baseTri )
		{
			index_inCollection = baseTri.index_inCollection;

			DbgCalculateTriInfo = baseTri.DbgCalculateTriInfo;

			V_center = baseTri.V_center;
			v_normal = baseTri.v_normal;
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

			name = $"ind: '{index_inCollection}', ctr: '{V_center}'";
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

			name = $"ind: '{index_inCollection}', ctr: '{V_center}'";
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
				otherTri.GetVertIndextAtPosition(Verts[0].Position) == -1 ||
				otherTri.GetVertIndextAtPosition(Verts[1].Position) == -1 ||
				otherTri.GetVertIndextAtPosition(Verts[2].Position) == -1
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
				V_center != tri.V_center || 
				v_normal != tri.v_normal ||
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
			V_center = (Verts[0].Position + Verts[1].Position + Verts[2].Position) / 3f;

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

			name = $"ind: '{index_inCollection}', ctr: '{V_center}'";
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

		public void TrySampleNormal( int lrMsk, bool logMessages = true)
		{
			if (v_normal == Vector3.zero)
			{
				RaycastHit rcHit = new RaycastHit();

				Vector3 castDir = Vector3.Cross(
					Vector3.Normalize(Verts[0].Position - Verts[1].Position),
					Vector3.Normalize(Verts[2].Position - Verts[1].Position)
				);

				if (
					Physics.Linecast(V_center - (castDir.normalized * 0.15f),
					V_center + (castDir.normalized * 0.15f),
					out rcHit, lrMsk))
				{
					//Debug.Log($"rc1 success");

				}
				else if (
					Physics.Linecast(V_center + (castDir.normalized * 0.15f),
					V_center - (castDir.normalized * 0.15f),
					out rcHit, lrMsk))
				{
					//Debug.Log($"rc2 success");

				}

				v_normal = rcHit.normal;
			}
			else if( logMessages )
			{
				Debug.LogWarning($"Not able to resolve normal for tri: '{index_inCollection}'.");
			}
		}

		#region MAIN API METHODS----------------------------------------------------------------------
		/// <summary>
		/// This version determines if an object is within the "diamond-like" 3 dimensional shape a triangle could theoretically make
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public bool IsWithinTriangleDiamond( Vector3 pos )
		{
			if( (pos - V_center).magnitude > (LongestEdgeLength * 0.5f) )
			{
				return false;
			}

			if( !Verts[0].IsInCenterSweep(pos) )
			{
				return false;
			}

			if ( !Verts[1].IsInCenterSweep(pos) )
			{
				return false;
			}

			if ( !Verts[2].IsInCenterSweep(pos) )
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Determines if a supplied position is within a theoretical sweep 
		/// (or cast) of the triangle's shape along it's normal direction.
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public bool IsInShapeProjectAlongNormal( Vector3 pos )
		{
		    Vector3 projectedPos = V_center + Vector3.ProjectOnPlane(pos - V_center, v_normal);

			if ( !Verts[0].IsInCenterSweep(projectedPos) )
			{
				return false;
			}

			if ( !Verts[1].IsInCenterSweep(projectedPos) )
			{
				return false;
			}

			if ( !Verts[2].IsInCenterSweep(projectedPos) )
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Determines if a supplied position is within a theoretical sweep (or cast) of the triangle's shape 
		/// along it's normal direction. 
		/// This Overload also sets an out Vector to show where the projection hits.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="projectedPos"></param>
		/// <returns></returns>
		public bool IsInShapeProjectAlongNormal( Vector3 pos, out Vector3 projectedPos )
		{
			Vector3 v_ctrToPos = pos - V_center;

			projectedPos = V_center + Vector3.ProjectOnPlane( v_ctrToPos, v_normal );

			if ( !Verts[0].IsInCenterSweep(projectedPos) )
			{
				return false;
			}

			if ( !Verts[1].IsInCenterSweep(projectedPos) )
			{
				return false;
			}

			if ( !Verts[2].IsInCenterSweep(projectedPos) )
			{
				return false;
			}

			return true;
		}

		public float DistanceToNearestVert( Vector3 pos )
		{
			return Mathf.Min(
				Vector3.Distance(pos, V_center),
				Vector3.Distance(pos, Verts[0].Position),
				Vector3.Distance(pos, Verts[1].Position),
				Vector3.Distance(pos, Verts[2].Position)
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

		public string dbgPerim;

		/// <summary>
		/// Checks if a point (destination) is 
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="destination"></param>
		/// <param name="edgeIndex"></param>
		/// <returns></returns>
		public bool IsProjectedPointOnEdge(Vector3 origin, Vector3 destination, int edgeIndex)
		{
			string dbg = "";
			int opposingVertIndex = Edges[edgeIndex].GetOpposingVertIndex();
			dbg += $"oppVrt: '{opposingVertIndex}'. vrtA: '{Edges[edgeIndex].StartVertCoordinate.ComponentIndex}', " +
				$"vrtB: '{Edges[edgeIndex].EndVertCoordinate.ComponentIndex}'\n";

			Vector3 v_project = LNX_Utils.FlatVector( destination - origin );

			Vector3 v_origin_to_VrtA = LNX_Utils.FlatVector(
				Verts[Edges[edgeIndex].StartVertCoordinate.ComponentIndex].Position - origin
			);
			Vector3 v_origin_to_VrtB = LNX_Utils.FlatVector(
				Verts[Edges[edgeIndex].EndVertCoordinate.ComponentIndex].Position - origin
			);

			float chevronAngle = Vector3.Angle( v_origin_to_VrtA, v_origin_to_VrtB );

			//Note: Currently this method uses a bit of a hack, but it seems to me like it will always work. The following two angle
			//calculations don't work in all instances, because they count up to 180, then back down after the threshold is crossed.
			//They would ideally go from 0 to 360. Later on, a magic number is used to check that the sum of both of these plus 
			//the magic number are above the chevron angle. This is because, due to rounding, the two angles can add up to slightly 
			//beyond what they actually are...
			float angle1 = Vector3.Angle(v_project, v_origin_to_VrtA);
			float angle2 = Vector3.Angle(v_project, v_origin_to_VrtB);

			// same thing ---------------------------------------------------------------------------
			/*
			float angle1 = Quaternion.Angle(
				Quaternion.FromToRotation(Vector3.forward, v_project),
				Quaternion.FromToRotation(Vector3.forward, v_origin_to_VrtA)
			);
			float angle2 = Quaternion.Angle(
				Quaternion.FromToRotation(Vector3.forward, v_project),
				Quaternion.FromToRotation(Vector3.forward, v_origin_to_VrtB)
			);
			*/


			dbg += $"chev: '{chevronAngle}', 1: '{angle1}', 2: '{angle2}'. added: '{angle1+angle2}'";

			//Debug.Log( dbg );

			// note: the following has a magic number. This is a hack for now. I do this because if I just use '(angle1 + angle2) > chevronAngle', there 
			// will sometimes be a rounding error that will make the added number a tiny amount larger than chevronAngle when the destination is truly 
			// projected on the edge
			if ( angle1 > chevronAngle || angle2 > chevronAngle || ((angle1 + angle2) > (chevronAngle + 0.0001f)) )
			{
				return false;
			}

			return true;
		}

		public Vector3 ProjectThroughToPerimeter( Vector3 innerPos, Vector3 outerPos, out LNX_Edge outedge, LNX_Direction meshProjectionDir = LNX_Direction.PositiveY )
		{
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

			/*dbgPerim += $"d1: '{dotProd_edge1}' ({Edges[1].IsProjectedPointOnEdge(innerPos, outerPos - innerPos)}), " +
				$"d2: '{dotProd_edge2}' ({Edges[2].IsProjectedPointOnEdge(innerPos, outerPos - innerPos)}), \n" +
				$"chose edge: '{opposingEdge}'\n";*/
			#endregion

			float lengthA = Vector3.Distance( innerPos, Edges[opposingEdge].StartPosition );
			float angA = Vector3.Angle( -v_dir, Edges[opposingEdge].v_endToStart );

			float angX = Vector3.Angle( 
				Vector3.Normalize(innerPos - Edges[opposingEdge].StartPosition),
				Edges[opposingEdge].v_startToEnd
			);

			float lengthX = Mathf.Sin(Mathf.Deg2Rad * angX) * ( lengthA / Mathf.Sin(Mathf.Deg2Rad * angA) );

			/*dbgPerim += $"lengthA: '{lengthA}', angA: '{angA}'\n" +
				$"lengthx: '{lengthX}', angX: '{angX}'";*/

			//Debug.Log( dbgPerim );

			/*DrawLine( innerPos, outerPos );

			Gizmos.color = Color.yellow;
			Gizmos.DrawLine( Edges[opposingEdge].StartPosition, Edges[opposingEdge].EndPosition );
			*/
			return innerPos + (v_dir * lengthX);
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
			v_normal = baseTri.v_normal;

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
			Verts[vertIndex].Position = (positionIsAbsolute ? pos : Verts[vertIndex].Position + pos);

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
				Verts[0].Position = Verts[0].OriginalPosition;
				Debug.LogWarning($"vert 0 was modified");
			}
			if ( Verts[1].AmModified )
			{
				Verts[1].Position = Verts[1].OriginalPosition;
				Debug.LogWarning($"vert 1 was modified");

			}
			if ( Verts[2].AmModified )
			{
				Verts[2].Position = Verts[2].OriginalPosition;
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
			if ( Verts[0].Position == pos )
			{
				return 0;
			}
			else if( Verts[1].Position == pos )
			{
				return 1;
			}
			else if ( Verts[2].Position == pos )
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
				$"{nameof(v_normal)}: '{v_normal}'\n" +
				$"");

			Edges[0].SayCurrentInfo();
			Edges[1].SayCurrentInfo();
			Edges[2].SayCurrentInfo();
		}
	}
}