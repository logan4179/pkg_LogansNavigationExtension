using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_Triangle
	{
		public string name;
		[SerializeField] private string DbgCalculateTriInfo;

		/// <summary>Describes this triangle's position inside the containing manager's Triangles array </summary>
		[SerializeField, HideInInspector] private int index_parallelWithParentArray;
		public int Index_parallelWithParentArray => index_parallelWithParentArray; 

		[HideInInspector, Tooltip("Corresponds to the areas set up in the Navigation window.")] 
		public float Area;

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


		[Header("RELATIONAL")]
		public LNX_TriangleRelationship_exp[] Relationships;
		/// <summary>
		/// Array of indices of triangles that share at least one vertex with this triangle.
		/// </summary>
		public int[] AdjacentTriIndices;

		//[Header("FLAGS")]
		/// <summary>Marks a vert dirty after a re-position of vert so that it's containing triangle knows to 
		/// re-calculate it's derived info when the user stops moving the vert.</summary>
		[SerializeField, HideInInspector] private bool dirtyFlag_repositionedVert = false;

		[SerializeField, HideInInspector] private bool wasAddedViaMod = false;
		/// <summary>Whether this triangle was added by a mesh modification, as opposed to being created 
		/// as part of the original navmesh triangulation.</summary>
		public bool WasAddedViaMod => wasAddedViaMod;


		[Header("OTHER")]
		[HideInInspector] public Vector3 v_normal;


		public LNX_Triangle( int triIndx, NavMeshTriangulation nmTriangulation, int lrMask )
		{
			DbgCalculateTriInfo = string.Empty;

			index_parallelWithParentArray = triIndx;

			Area = nmTriangulation.areas[triIndx];

			Vector3 vrtPos0 = nmTriangulation.vertices[ nmTriangulation.indices[(triIndx * 3)] ];
			Vector3 vrtPos1 = nmTriangulation.vertices[ nmTriangulation.indices[(triIndx * 3) + 1] ];
			Vector3 vrtPos2 = nmTriangulation.vertices[ nmTriangulation.indices[(triIndx * 3) + 2] ];

			Verts = new LNX_Vertex[3];
			Verts[0] = new LNX_Vertex( this, vrtPos0, 0, nmTriangulation );
			Verts[1] = new LNX_Vertex( this, vrtPos1, 1, nmTriangulation );
			Verts[2] = new LNX_Vertex( this, vrtPos2, 2, nmTriangulation );

			Edges = new LNX_Edge[3];
			Edges[0] = new LNX_Edge( Verts[1], Verts[2], V_center, v_normal, 
				new LNX_ComponentCoordinate(index_parallelWithParentArray, 0) );
			Edges[1] = new LNX_Edge( Verts[0], Verts[2], V_center, v_normal,
				new LNX_ComponentCoordinate(index_parallelWithParentArray, 1) );
			Edges[2] = new LNX_Edge( Verts[1], Verts[0], V_center, v_normal,
				new LNX_ComponentCoordinate(index_parallelWithParentArray, 2) );

			CalculateDerivedInfo();

			TrySampleNormal( lrMask, true );
		}

		public LNX_Triangle( LNX_Triangle baseTri, int triIndx )
		{
			index_parallelWithParentArray = triIndx;

			DbgCalculateTriInfo = baseTri.DbgCalculateTriInfo;

			V_center = baseTri.V_center;
			v_normal = baseTri.v_normal;
			Perimeter = baseTri.Perimeter;
			Area = baseTri.Area;
			LongestEdgeLength = baseTri.LongestEdgeLength;
			ShortestEdgeLength = baseTri.ShortestEdgeLength;
			Verts = new LNX_Vertex[3];
			Verts[0] = new LNX_Vertex( baseTri.Verts[0] );
			Verts[1] = new LNX_Vertex( baseTri.Verts[1] );
			Verts[2] = new LNX_Vertex( baseTri.Verts[2] );

			Edges = new LNX_Edge[3];
			Edges[0] = new LNX_Edge( baseTri.Edges[0] );
			Edges[1] = new LNX_Edge( baseTri.Edges[1] );
			Edges[2] = new LNX_Edge( baseTri.Edges[2] );

			Relationships = baseTri.Relationships;
			AdjacentTriIndices = baseTri.AdjacentTriIndices;
			dirtyFlag_repositionedVert = false;

			name = $"ind: '{index_parallelWithParentArray}', ctr: '{V_center}'";
		}

		public void AdoptValues( LNX_Triangle baseTri )
		{
			index_parallelWithParentArray = baseTri.index_parallelWithParentArray;

			DbgCalculateTriInfo = baseTri.DbgCalculateTriInfo;

			V_center = baseTri.V_center;
			v_normal = baseTri.v_normal;
			Perimeter = baseTri.Perimeter;
			Area = baseTri.Area;
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

			name = $"ind: '{index_parallelWithParentArray}', ctr: '{V_center}'";
		}

		public void ChangeIndex( int indx )
		{
			index_parallelWithParentArray = indx;

			Verts[0].MyCoordinate.TriIndex = index_parallelWithParentArray;
			Verts[1].MyCoordinate.TriIndex = index_parallelWithParentArray;
			Verts[2].MyCoordinate.TriIndex = index_parallelWithParentArray;

			Edges[0].MyCoordinate.TriIndex = index_parallelWithParentArray;
			Edges[1].MyCoordinate.TriIndex = index_parallelWithParentArray;
			Edges[2].MyCoordinate.TriIndex = index_parallelWithParentArray;

			//todo: in the future when I start caching relational info, I might need to refresh it here...
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
				otherTri.GetVerIndextAtPosition(Verts[0].Position) == -1 ||
				otherTri.GetVerIndextAtPosition(Verts[1].Position) == -1 ||
				otherTri.GetVerIndextAtPosition(Verts[2].Position) == -1
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
		public bool OriginallyMatches( LNX_Triangle otherTri )
		{
			if (
				otherTri.Verts == null || otherTri.Verts.Length != 3 || Verts == null || Verts.Length != 3
			)
			{
				return false;
			}

			if (
				otherTri.GetVerIndextAtPosition(Verts[0].OriginalPosition) == -1 ||
				otherTri.GetVerIndextAtPosition(Verts[1].OriginalPosition) == -1 ||
				otherTri.GetVerIndextAtPosition(Verts[2].OriginalPosition) == -1
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
				Area != tri.Area || 
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
			DbgCalculateTriInfo = string.Empty;

			V_center = (Verts[0].Position + Verts[1].Position + Verts[2].Position) / 3f;

			Edges[0].CalculateInfo( Verts[1], Verts[2], V_center, v_normal );
			Edges[1].CalculateInfo( Verts[0], Verts[2], V_center, v_normal );
			Edges[2].CalculateInfo( Verts[1], Verts[0], V_center, v_normal );

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

			name = $"ind: '{index_parallelWithParentArray}', ctr: '{V_center}'";

			DbgCalculateTriInfo += $"nrml: '{v_normal}'\n" +
				$"edge lengths: '{Edges[0].EdgeLength}', '{Edges[1].EdgeLength}', '{Edges[2].EdgeLength}'\n" +
				$"Prmtr: '{Perimeter}', Area: '{Area}'\n";
		}

		public void RefreshTriangle( LNX_NavMesh nm, bool logMessages = true)
		{
			if ( dirtyFlag_repositionedVert )
			{
				CalculateDerivedInfo( logMessages );

				CreateRelationships( nm.Triangles );

				dirtyFlag_repositionedVert = false;
			}
		}

		public void CreateRelationships( LNX_Triangle[] Tris, bool amThorough = true )
		{
			Relationships = new LNX_TriangleRelationship_exp[Tris.Length];
			List<int> foundAdjacentTriIndices_temp = new List<int>();
			List<LNX_ComponentCoordinate> sharedVertCoords0_temp = new List<LNX_ComponentCoordinate>();
			List<LNX_ComponentCoordinate> sharedVertCoords1_temp = new List<LNX_ComponentCoordinate>();
			List<LNX_ComponentCoordinate> sharedVertCoords2_temp = new List<LNX_ComponentCoordinate>();

			for ( int i_otherTri = 0; i_otherTri < Tris.Length; i_otherTri++ )
			{
				Relationships[i_otherTri] = new LNX_TriangleRelationship_exp( this, Tris[i_otherTri] );

				if ( i_otherTri == index_parallelWithParentArray ) //IF we've iterated to this triangle's self, just continue...
				{
					continue;
				}

				if ( Relationships[i_otherTri].NumberofSharedVerts > 0 )
				{
					foundAdjacentTriIndices_temp.Add( i_otherTri );
				}

				if ( !amThorough )
				{
					continue;
				}

				for ( int i_myVerts = 0; i_myVerts < 3; i_myVerts++ ) //Construct sharedvertexcoordinates collection--------------------
				{
					for ( int i_otherVerts = 0; i_otherVerts < 3; i_otherVerts++ )
					{
						if ( Verts[i_myVerts].Position == Tris[i_otherTri].Verts[i_otherVerts].Position )
						{
							if( i_myVerts == 0 )
							{
								sharedVertCoords0_temp.Add(
									new LNX_ComponentCoordinate(Tris[i_otherTri].index_parallelWithParentArray, i_otherVerts)
								);
							}
							else if ( i_myVerts == 1 )
							{
								sharedVertCoords1_temp.Add(
									new LNX_ComponentCoordinate(Tris[i_otherTri].index_parallelWithParentArray, i_otherVerts)
								);
							}
							else if ( i_myVerts == 2 )
							{
								sharedVertCoords2_temp.Add(
									new LNX_ComponentCoordinate(Tris[i_otherTri].index_parallelWithParentArray, i_otherVerts)
								);
							}
						}
					}
				}
			}

			AdjacentTriIndices = foundAdjacentTriIndices_temp.ToArray();

			Verts[0].SharedVertexCoordinates = sharedVertCoords0_temp.ToArray();
			Verts[1].SharedVertexCoordinates = sharedVertCoords1_temp.ToArray();
			Verts[2].SharedVertexCoordinates = sharedVertCoords2_temp.ToArray();
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
				Debug.LogWarning($"Not able to resolve normal for tri: '{index_parallelWithParentArray}'.");
			}
		}

		#region SAMPLING METHODS----------------------------------------------------------------------
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
		/// Overload that does the exact same thing, but sets an out Vector to show where the projection hits.
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

		public Vector3 ProjectThroughToPerimeter( Vector3 innerPos, Vector3 outerPos )
		{
			Vector3 v_dir = Vector3.Normalize( outerPos - innerPos );
			string dbgPerim = string.Empty;

			#region Find opposing edge...........................
			//note: the dot product of edge 0 isn't necessary as we can assume it's this one for sure if the other two don't work...
			float dotProd_edge1 = Vector3.Dot( -Edges[1].v_cross, v_dir );
			float dotProd_edge2 = Vector3.Dot( -Edges[2].v_cross, v_dir );

			int opposingEdge = 0;

			if( dotProd_edge1 > 0 && Edges[1].IsProjectedPointOnEdge(innerPos, outerPos - innerPos) )
			{
				dbgPerim += $"if-chose 1\n";
				opposingEdge = 1;
			}
			else if ( dotProd_edge2 > 0 && Edges[2].IsProjectedPointOnEdge(innerPos, outerPos - innerPos) )
			{
				dbgPerim += $"if-chose 2\n";

				opposingEdge = 2;
			}

			dbgPerim += $"d1: '{dotProd_edge1}' ({Edges[1].IsProjectedPointOnEdge(innerPos, outerPos - innerPos)}), " +
				$"d2: '{dotProd_edge2}' ({Edges[2].IsProjectedPointOnEdge(innerPos, outerPos - innerPos)}), \n" +
				$"chose edge: '{opposingEdge}'\n";
			#endregion

			float lengthA = Vector3.Distance( innerPos, Edges[opposingEdge].StartPosition );
			float angA = Vector3.Angle( -v_dir, Edges[opposingEdge].v_endToStart );

			float angX = Vector3.Angle( 
				Vector3.Normalize(innerPos - Edges[opposingEdge].StartPosition),
				Edges[opposingEdge].v_startToEnd
			);

			float lengthX = Mathf.Sin(Mathf.Deg2Rad * angX) * ( lengthA / Mathf.Sin(Mathf.Deg2Rad * angA) );

			dbgPerim += $"lengthA: '{lengthA}', angA: '{angA}'\n" +
				$"lengthx: '{lengthX}', angX: '{angX}'";

			//Debug.Log( dbgPerim );

			/*rawLine( innerPos, outerPos );

			Gizmos.color = Color.yellow;
			Gizmos.DrawLine( Edges[opposingEdge].StartPosition, Edges[opposingEdge].EndPosition );
			*/
			return innerPos + (v_dir * lengthX);
		}
		#endregion

		/// <summary>
		/// Movies a vertex belonging to this triangle in a managed fashion. Sets appropriate flags and 
		/// does what's necessary after a movement has been made.
		/// </summary>
		/// <param name="vertIndex"></param>
		/// <param name="pos"></param>
		/// <param name="positionIsAbsolute"></param>

		#region MODIFICATION ----------------------------------------------------
		public void AdoptModifiedValues(LNX_Triangle baseTri)
		{
			v_normal = baseTri.v_normal;

			Verts[0].AdoptValues(baseTri.Verts[0]);
			Verts[1].AdoptValues(baseTri.Verts[1]);
			Verts[2].AdoptValues(baseTri.Verts[2]);

			CalculateDerivedInfo();
		}

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

		public bool HasBeenModified()
		{
			if( 
				Verts[0].AmModified || Verts[1].AmModified || Verts[2].AmModified
			)
			{
				return true;
			}

			return false;
		}

		public void ClearModifications()
		{
			if( Verts[0].AmModified )
			{
				Verts[0].Position = Verts[0].OriginalPosition;
			}
			if ( Verts[1].AmModified )
			{
				Verts[1].Position = Verts[1].OriginalPosition;
			}
			if ( Verts[2].AmModified )
			{
				Verts[2].Position = Verts[2].OriginalPosition;
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
					if (AdjacentTriIndices[i] == tri.index_parallelWithParentArray )
					{
						return true;
					}
				}
			}

			return false;
		}

		


		#region GETTERS/IDENTIFIERS -----------------------------------------------------
		public LNX_Vertex[] GetVertsOnEdge( int edgeIndex )
		{
			LNX_Vertex[] returnCollection = new LNX_Vertex[2];
			int count = 0;
			for (int i = 0; i < 3; i++)
			{
				if( Edges[edgeIndex].StartPosition == Verts[i].Position || Edges[edgeIndex].EndPosition == Verts[i].Position )
				{
					returnCollection[count] = Verts[i];
					count++;
				}
			}

			return returnCollection;
		}

		public int GetVerIndextAtPosition( Vector3 pos )
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

		public int GetOriginalVerIndextAtPosition(Vector3 pos)
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

		public string DBGcenterSweeps( Vector3 pos )
		{
			return
				$"isWithin()\n" +
				$"v0: '{Verts[0].IsInCenterSweep(pos)}, nmrSweep: '{Verts[0].IsInNormalizedCenterSweep(pos, this)}'\n" +
				$"v1: '{Verts[1].IsInCenterSweep(pos)}, nmrSweep: '{Verts[1].IsInNormalizedCenterSweep(pos, this)}'\n" +
				$"v2: '{Verts[2].IsInCenterSweep(pos)}, nmrSweep: '{Verts[2].IsInNormalizedCenterSweep(pos, this)}'\n" +

				$"";
		}

		public void Ping( LNX_Triangle[] tris )
		{
			Verts[0].Ping( tris );
			Verts[1].Ping( tris );
			Verts[2].Ping( tris );
		}
	}
}