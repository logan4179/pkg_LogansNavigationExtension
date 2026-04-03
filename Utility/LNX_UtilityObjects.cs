using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LogansNavigationExtension
{
    public static class LNX_UtilityObjects
    {

	}


	#region ENUMS-----------------------------------------
	[System.Serializable]
	public enum LNX_Component
	{
		None = 0,
		Vertex = 1,
		Edge = 2,
		Triangle = 3
	}

	[System.Serializable]
	public enum LNX_OperationMode
	{
		Pointing = 0,
		Translating = 1,
	}

	[System.Serializable]
	public enum LNX_Direction
	{
		PositiveY = 0,
		NegativeY = 1,
		PositiveX = 2,
		NegativeX = 3,
		PositiveZ = 4,
		NegativeZ = 5,
	}
	#endregion

	[System.Serializable]
	public struct LNX_ComponentCoordinate
	{
		public int TrianglesIndex;
		public int ComponentIndex;
		/*
		public int TriangulationAreasIndex;
		/// <summary>
		/// Keeps track of the index inside of the NavMesh.CalculateTriangulation().vertices array where this component
		/// originated. This value is only relevant if this coordinate is pointing to a vertex. If it's an edge, this 
		/// value should be -1.
		/// </summary>
		public int TriangulationVerticesIndex;
		*/

		private static LNX_ComponentCoordinate none = new LNX_ComponentCoordinate()
		{
			TrianglesIndex = -1,
			ComponentIndex = -1,
		};

		public static LNX_ComponentCoordinate None
		{
			get
			{
				return none;
			}
		}

		public LNX_ComponentCoordinate(int triIndx, int cmptIndx)
		{
			TrianglesIndex = triIndx;
			ComponentIndex = cmptIndx;
		}

		public static bool operator ==(LNX_ComponentCoordinate a, LNX_ComponentCoordinate b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(LNX_ComponentCoordinate a, LNX_ComponentCoordinate b)
		{
			return !a.Equals(b);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is LNX_ComponentCoordinate))
				return false;

			LNX_ComponentCoordinate coord = (LNX_ComponentCoordinate)obj;
			if (coord.TrianglesIndex != TrianglesIndex || coord.ComponentIndex != ComponentIndex)
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		public override int GetHashCode()
		{
			return HashCode.Combine( TrianglesIndex, ComponentIndex );
		}

		public override string ToString()
		{
			return $"[{TrianglesIndex}][{ComponentIndex}]";
		}
	}

	/// <summary>
	/// A more basic representation of an LNX_Triangle. This is useful in situations where you need an 
	/// object that will have a much cheaper serialization cost than a full LNX_Triangle.
	/// </summary>
	[System.Serializable]
	public struct LNX_AtomicTriangle
	{
		public Vector3 VertPos0_current, VertPos0_orig;

		public Vector3 VertPos1_current, VertPos1_orig;

		public Vector3 VertPos2_current, VertPos2_orig;

		public Vector3 Center => (VertPos0_current + VertPos1_current + VertPos2_current) / 3f;

		public LNX_AtomicTriangle(LNX_Triangle tri)
		{
			VertPos0_current = tri.Verts[0].V_Position;
			VertPos0_orig = tri.Verts[0].OriginalPosition;

			VertPos1_current = tri.Verts[1].V_Position;
			VertPos1_orig = tri.Verts[1].OriginalPosition;

			VertPos2_current = tri.Verts[2].V_Position;
			VertPos2_orig = tri.Verts[2].OriginalPosition;

		}

		public LNX_AtomicTriangle(Vector3 v0pos, Vector3 v1pos, Vector3 v2pos)
		{
			VertPos0_current = v0pos;
			VertPos0_orig = v0pos;

			VertPos1_current = v1pos;
			VertPos1_orig = v1pos;

			VertPos2_current = v2pos;
			VertPos2_orig = v2pos;
		}

		/// <summary>
		/// Returns any vertices owned by this triangle that originally existed at the supplied position before 
		/// being modified.
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public int GetVertIndextAtOriginalPosition(Vector3 pos)
		{
			if (VertPos0_orig == pos)
			{
				return 0;
			}
			else if (VertPos1_orig == pos)
			{
				return 1;
			}
			else if (VertPos2_orig == pos)
			{
				return 2;
			}

			return -1;
		}

		public bool HasVertAtOriginalPosition(Vector3 pos)
		{
			if (GetVertIndextAtOriginalPosition(pos) > -1)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool OriginalPositionallyMatches(LNX_AtomicTriangle tri)
		{
			if
			(
				tri.VertPos0_orig == VertPos0_orig &&
				tri.VertPos1_orig == VertPos1_orig &&
				tri.VertPos2_orig == VertPos2_orig
			)
			{
				return true;
			}

			return false;
		}

		public bool OriginalPositionallyMatches(LNX_Triangle tri)
		{
			if
			(
				tri.Verts[0].OriginalPosition == VertPos0_orig &&
				tri.Verts[1].OriginalPosition == VertPos1_orig &&
				tri.Verts[2].OriginalPosition == VertPos2_orig
			)
			{
				return true;
			}

			return false;
		}
	}

	[System.Serializable]
	public struct LNX_NavmeshHit
	{
		[SerializeField] private Vector3 hitPosition;
		/// <summary>Index of the Triangle or component that was hit, depending on the context.</summary>
		public Vector3 Position => hitPosition;

		//private Vector3 startPosition;
		//public Vector3 StartPosition => startPosition;

		[SerializeField] private Vector3 normal;
		public Vector3 Normal => normal;

		//public float DistanceAway => Vector3.Distance(startPosition, hitPosition);
		[SerializeField] private LNX_ComponentCoordinate coordinate;
		public LNX_ComponentCoordinate Coordinate => coordinate;

		//PROPERTIES---------------
		public int TriIndex => coordinate.TrianglesIndex;
		public int ComponentIndex => coordinate.ComponentIndex;

		private static LNX_NavmeshHit none = new LNX_NavmeshHit(-1, Vector3.zero);

		#region CONSTRUCTORS =============================================================
		public LNX_NavmeshHit(int triIndx, Vector3 pos) //todo; i need to check where this is being called and make sure there aren't any more problems
		{
			hitPosition = pos;

			//startPosition = Vector3.zero;
			normal = Vector3.zero;

			coordinate = new LNX_ComponentCoordinate(triIndx, -1);
		}
		
		/* //Unfortunately can't do the following bc default vectors aren't compile-time constants...
		public LNX_NavmeshHit( Vector3 pos, int triIndx, int cmpntIndx = -1, Vector3 strtPos = Vector3.zero, Vector3 nrml = Vector3.zero )
		{
			hitPosition = pos;
			startPosition = strtPos;
			normal = nrml;

			Coordinate = new LNX_ComponentCoordinate( triIndx, cmpntIndx );
		}
		*/

		public LNX_NavmeshHit(LNX_Triangle hitTriangle, Vector3 hitpos, Vector3 startPos )
		{
			hitPosition = hitpos;
			//startPosition = startPos;
			normal = hitTriangle.v_SurfaceNormal_cached;

			coordinate = new LNX_ComponentCoordinate(hitTriangle.Index_inCollection, -1);
		}

		public LNX_NavmeshHit ( LNX_Vertex vert )
		{
			hitPosition = vert.V_Position;
			//startPosition = Vector3.zero;
			normal = vert.CachedSurfaceNormal;
			coordinate = vert.MyCoordinate;
		}

		public LNX_NavmeshHit(Vector3 pos, LNX_ComponentCoordinate coord)
		{
			hitPosition = pos;
			//startPosition = Vector3.zero;
			normal = Vector3.zero;

			coordinate = coord;
		}

		public LNX_NavmeshHit ( Vector3 pos, int triIndx, int cmpntIndx )
		{
			hitPosition = pos;
			coordinate = new LNX_ComponentCoordinate( triIndx, cmpntIndx );

			//startPosition = Vector3.zero;
			normal = Vector3.zero;
		}

		public LNX_NavmeshHit(Vector3 pos, Vector3 nrml, int triIndx, int cmpntIndx)
		{
			hitPosition = pos;
			coordinate = new LNX_ComponentCoordinate(triIndx, cmpntIndx);
			normal = nrml;

			//startPosition = Vector3.zero;
		}

		public LNX_NavmeshHit( Vector3 pos, Vector3 nrml )
		{
			hitPosition = pos;
			coordinate = new LNX_ComponentCoordinate(-1, -1);
			normal = nrml;
		}

		public LNX_NavmeshHit(Vector3 pos, Vector3 nrml, Vector3 strtPos, LNX_ComponentCoordinate coord )
		{
			hitPosition = pos;
			coordinate = coord;
			normal = nrml;
			//startPosition = strtPos;
		}

		public LNX_NavmeshHit(int indx, Vector3 hitpos, Vector3 originpos)
		{
			hitPosition = hitpos;
			//startPosition = originpos;
			normal = Vector3.zero;

			coordinate = new LNX_ComponentCoordinate(indx, -1);
		}
		#endregion

		public static LNX_NavmeshHit None
		{
			get
			{
				return none;
			}
		}

		#region OPERATORS ===============================================
		public override bool Equals(object obj)
		{
			if (!(obj is LNX_NavmeshHit))
				return false;

			LNX_NavmeshHit hit = (LNX_NavmeshHit)obj;
			if ( hit.coordinate != coordinate || hit.hitPosition != hitPosition /*|| startPosition != hit.startPosition*/ )
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		public override int GetHashCode()
		{
			return HashCode.Combine( hitPosition, normal, coordinate );
		}

		public static bool operator ==(LNX_NavmeshHit a, LNX_NavmeshHit b)
		{
			return a.Equals(b);
		}
		public static bool operator !=(LNX_NavmeshHit a, LNX_NavmeshHit b)
		{
			return !a.Equals(b);
		}
		
		public override string ToString()
		{
			return $"Hit[{coordinate} ({Position})]'";
		}
		#endregion
	}

	[System.Serializable]
	public struct LNX_Quad
	{
		public Vector3 crnrA;
		public Vector3 crnrB;
		public Vector3 crnrC;
		public Vector3 crnrD;

		private static LNX_Quad none = new LNX_Quad()
		{
			crnrA = Vector3.zero,
			crnrB = Vector3.zero,
			crnrC = Vector3.zero,
			crnrD = Vector3.zero
		};

		public static LNX_Quad None
		{
			get
			{
				return none;
			}
		}

		public LNX_Quad( Vector3 cA, Vector3 cB, Vector3 cC, Vector3 cD )
		{
			crnrA = cA;
			crnrB = cB;
			crnrC = cC;
			crnrD = cD;
		}

		public override string ToString()
		{
			return $"{crnrA}, {crnrB}, {crnrC}, {crnrD}";
		}
	}

	#region RELATIONSHIPS------------------------------------------------------------------------
	[System.Serializable]
	public struct LNX_VertexRelationship //todo: If i make RelatedVertCoordinate into a property, I think I can just do away with this struct and just use the LNX_Path instead...
	{
		#region COORDINATE ==========================================================
		public LNX_ComponentCoordinate RelatedVertCoordinate; //todo: I think this can be made into a property returning PathTo.EndCoordinate, in which case, I won't even really need this struct anymore because I could just use the LNX_Path struct instead

		public Vector3 RelatedVertPosition => PathTo.EndPosition;
		public Vector3 OwnerVertPosition => PathTo.StartPoint;
		/// <summary>
		/// Relates this relationship to it's position in the containing collection in the 'owner' LNX_Vertex
		/// </summary>
		public int Index_InCollection => RelatedVertCoordinate.TrianglesIndex * 3 + RelatedVertCoordinate.ComponentIndex;
		public int RelatedTriIndex => RelatedVertCoordinate.TrianglesIndex;
		public int RelatedComponentIndex => RelatedVertCoordinate.ComponentIndex;
		#endregion

		#region PATH ====================================================================
		/// <summary>The most direct path from the perspective vert to the related vert </summary>
		public LNX_Path PathTo;
		public bool CanSee => PathTo.AmStraight;

		/// <summary>The shortest possible distance to the destination vertex via traveling over the surface of the navmesh</summary>
		public float PathDistance => PathTo.TotalDistance;

		public Vector3 V_to => PathTo.V_CrowFlies;

		#endregion

		public bool AmValid //Started using this bc for some reason, having a static LNX_VertexRelationship.None was causing problems in in LNX_Vertex.CalculateDerivedInfo().
		{
			get
			{
				return RelatedVertCoordinate != LNX_ComponentCoordinate.None && PathTo != LNX_Path.None;
			}
		}

		//private static LNX_VertexRelationship none = new LNX_VertexRelationship( null, null, null, false); //todo: dws unless I figure out why this causes problems in LNX_Vertex.CalculateDerivedInfo()

		#region CONSTRUCTORS ======================================================================

		public LNX_VertexRelationship(LNX_Vertex myVert, LNX_Vertex relatedVert, LNX_NavMesh nvMsh, bool allowBorrowing = false)
		{
			StringBuilder sb_rprt = new StringBuilder();
			sb_rprt.AppendLine($"LNX_VertexRelationship ctor. myVert: '{myVert}'({myVert.V_Position}), " +
				$"relatedVert: '{relatedVert}'({relatedVert.V_Position})");

			DateTime dt_start = DateTime.Now;

			RelatedVertCoordinate = relatedVert.MyCoordinate;
			PathTo = LNX_Path.None;

			if (myVert == null && relatedVert == null)
			{
				RelatedVertCoordinate = LNX_ComponentCoordinate.None;
				return;
			}

			if (myVert.V_Position != relatedVert.V_Position)
			{
				if (myVert.MyCoordinate.TrianglesIndex == relatedVert.MyCoordinate.TrianglesIndex || 
					relatedVert.SharesVertSpace(nvMsh.Triangles[myVert.Coordinate_FirstSibling.TrianglesIndex].Verts[myVert.Coordinate_FirstSibling.ComponentIndex]) ||
					relatedVert.SharesVertSpace(nvMsh.Triangles[myVert.Coordinate_SecondSibling.TrianglesIndex].Verts[myVert.Coordinate_SecondSibling.ComponentIndex])
				) //"If we're siblings". More performant than using the AreSiblings() method
				{
					//Debug.LogWarning($"same spot or siblings");
					sb_rprt.AppendLine($"same spot or siblings, or in same spot as siblings");
					PathTo = new LNX_Path
					(
						new List<Vector3>() { myVert.V_Position, relatedVert.V_Position },
						new List<Vector3>() { nvMsh.Triangles[myVert.MyCoordinate.TrianglesIndex].V_PathingNormal, nvMsh.Triangles[myVert.MyCoordinate.TrianglesIndex].V_PathingNormal },
						true
					);
				}
				else if
				(
					//The following will derive the pathing instead of calculating it by "borrowing" from an existing relationship
					//with the related vert. This can greatly speed up this method, though I need to figure out exactly when it's okay
					//to do this...

					allowBorrowing && relatedVert.Relationships != null && relatedVert.Relationships.Length > 0 &&
					relatedVert.Relationships[myVert.Index_Relational].PathTo.AmValid
				)
				{
					sb_rprt.AppendLine($"Getting reversed relational pathing...");
					List<Vector3> pthPts = new List<Vector3>();
					List<Vector3> pthNrmls = new List<Vector3>();

					for (int i = relatedVert.Relationships[myVert.Index_Relational].PathTo.PathPoints.Count - 1; i > 0; i--)
					{
						pthPts.Add(relatedVert.Relationships[myVert.Index_Relational].PathTo.PathPoints[i].Position);
						pthNrmls.Add(relatedVert.Relationships[myVert.Index_Relational].PathTo.PathPoints[i].Normal);
					}

					PathTo = new LNX_Path
					(
						pthPts,
						pthNrmls,
						nvMsh.GetSurfaceNormalVector()
					);
				}
				else
				{
					sb_rprt.AppendLine($"Actually calculating the path...");

					/*
					StringBuilder sb_calculatePath = new StringBuilder();
					nvMsh.CalculatePath( myVert, relatedVert, out PathTo, ref sb_calculatePath); //this is by far where most of the time is being spent
					Debug.Log(sb_calculatePath);
					sb_rprt.AppendLine($"created path: '{PathTo}' with '{PathTo.PathPoints.Count}' points. Here's the report...\n" +
						$"=============================================================\n");
					sb_rprt.AppendLine($"{sb_calculatePath.ToString()}\n" +
						$"=============================================================\n");
					*/

					
					//Just to make things run smoother for now...
					PathTo = new LNX_Path
					(
						new List<Vector3>() { myVert.V_Position, relatedVert.V_Position },
						new List<Vector3>() { nvMsh.Triangles[myVert.MyCoordinate.TrianglesIndex].V_PathingNormal, nvMsh.Triangles[myVert.MyCoordinate.TrianglesIndex].V_PathingNormal },
						true
					);
					
				}
			}
			else
			{
				sb_rprt.AppendLine("verts are in same position...");
			}

				sb_rprt.AppendLine($"end of vertrelationship ctor. total time: '{DateTime.Now.Subtract(dt_start)}' " +
					$"total ms: '{DateTime.Now.Subtract(dt_start).TotalMilliseconds}'");

			Debug.Log(sb_rprt);
		}

		/// <summary>
		/// Use this overload only for siblings
		/// </summary>
		/// <param name="myVert"></param>
		/// <param name="owerTri"></param>
		/// <param name="reltdVrtIndx"></param>
		/// <param name="nvMsh"></param>
		public LNX_VertexRelationship(LNX_Vertex myVert, LNX_Triangle sharedTri, int siblingVertIndex)
		{
			StringBuilder sb_rprt = new StringBuilder();
			sb_rprt.AppendLine($"LNX_VertexRelationship sibling ctor. myVert: '{myVert}', sharedTri: '{sharedTri}'"); RelatedVertCoordinate = new LNX_ComponentCoordinate( myVert.TriangleIndex, siblingVertIndex );

			PathTo = new LNX_Path
			(
				new List<Vector3>() { myVert.V_Position, sharedTri.Verts[siblingVertIndex].V_Position },
				new List<Vector3>() { sharedTri.V_PathingNormal, sharedTri.V_PathingNormal },
				sharedTri.v_SurfaceNormal_cached
			);
		}
		#endregion

		/*
		public static LNX_VertexRelationship None  //todo: dws unless I figure out why this causes problems in in LNX_Vertex.CalculateDerivedInfo()
		{
			get
			{
				return none;
			}
		}
		*/

		#region OPERATORS ==================================================
		public override bool Equals(object obj)
		{
			if (!(obj is LNX_VertexRelationship))
				return false;

			LNX_VertexRelationship other = (LNX_VertexRelationship)obj;
			if ( other.RelatedVertCoordinate != RelatedVertCoordinate || other.PathTo != PathTo )
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		public override int GetHashCode()
		{
			return HashCode.Combine( RelatedVertCoordinate, PathTo );
		}

		public static bool operator ==(LNX_VertexRelationship a, LNX_VertexRelationship b)
		{
			return a.Equals(b);
		}
		public static bool operator !=(LNX_VertexRelationship a, LNX_VertexRelationship b)
		{
			return !a.Equals(b);
		}
		#endregion

		public override string ToString()
		{
			string s = $"Related: '{RelatedVertCoordinate}'\n" +
				$"{nameof(CanSee)}: '{CanSee}'\n" +
				$"";

			if( PathTo.PathPoints == null )
			{
				s += "PathPoints collection is null...";
			}
			else if ( PathTo.PathPoints.Count <= 0 )
			{
				s += $"PathPoints collection count is '{PathTo.PathPoints.Count}'";
			}
			else
			{
				s += $"PathPoints collection count is '{PathTo.PathPoints.Count}'\n" +
				$"path distance: '{PathDistance}'\n" +
				$"vTo: '{PathTo.V_CrowFlies}'" +
				$"";
			}

			return s;
		}
	}
	#endregion
}
