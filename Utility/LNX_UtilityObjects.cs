using System.Collections;
using System.Collections.Generic;
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
	public struct LNX_ProjectionHit
	{
		/// <summary>Index of the Triangle or component that was hit, depending on the context.</summary>
		public int Index_Hit;
		public Vector3 HitPosition;
		public float DistanceAway;
		LNX_ComponentCoordinate Coordinate;

		private static LNX_ProjectionHit none = new LNX_ProjectionHit(-1, Vector3.zero);

		public LNX_ProjectionHit(int indx, Vector3 pos)
		{
			Index_Hit = indx;
			HitPosition = pos;
			DistanceAway = 0f;

			Coordinate = LNX_ComponentCoordinate.None;
		}

		public LNX_ProjectionHit(Vector3 pos, LNX_ComponentCoordinate coord)
		{
			Index_Hit = -1;
			HitPosition = pos;
			DistanceAway = 0f;

			Coordinate = coord;
		}

		public LNX_ProjectionHit(int indx, Vector3 hitpos, Vector3 originpos)
		{
			Index_Hit = indx;
			HitPosition = hitpos;
			DistanceAway = Vector3.Distance(originpos, hitpos);

			Coordinate = LNX_ComponentCoordinate.None;
		}

		public static LNX_ProjectionHit None
		{
			get
			{
				return none;
			}
		}

		public override bool Equals(object obj)
		{
			if (!(obj is LNX_ProjectionHit))
				return false;

			LNX_ProjectionHit hit = (LNX_ProjectionHit)obj;
			if (hit.Index_Hit != Index_Hit || hit.HitPosition != HitPosition)
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		public override string ToString()
		{
			return $"Indx '{Index_Hit}', at '{HitPosition}'";
		}
	}

	[System.Serializable]
	public struct LNX_Quad
	{
		public Vector3 crnrA;
		public Vector3 crnrB;
		public Vector3 crnrC;
		public Vector3 crnrD;

		public LNX_Quad( Vector3 cA, Vector3 cB, Vector3 cC, Vector3 cD )
		{
			crnrA = cA;
			crnrB = cB;
			crnrC = cC;
			crnrD = cD;
		}
	}

	#region RELATIONSHIPS------------------------------------------------------------------------
	[System.Serializable]
	public struct LNX_VertexRelationship
	{
		public LNX_ComponentCoordinate RelatedVertCoordinate;

		public Vector3 RelatedVertPosition => PathTo.EndPoint;
		public Vector3 OwnerVertPosition => PathTo.StartPoint;

		public bool CanSee => PathTo.AmStraight;

		/// <summary>The shortest possible distance to the destination vertex via traveling over the surface of the navmesh</summary>
		public float PathDistance => PathTo.TotalDistance;

		/// <summary>The most direct path from the perspective vert to the related vert </summary>
		public LNX_Path PathTo;

		public Vector3 V_to => PathTo.V_CrowFlies;

		/// <summary>If true, this vert and it's related vert are part of the same terminal cutout 
		/// (obstacle) in the navmesh. This efficiency boolean is useful for efficient pathfinding 
		/// around obstacles</summary>
		//public bool AmPartOfSharedTerminalCutout; //todo: implement

		public LNX_VertexRelationship(LNX_Vertex myVert, LNX_Vertex relatedVert, LNX_NavMesh nvMsh, bool allowBorrowing = false)
		{
			//DateTime dt_start = DateTime.Now;

			RelatedVertCoordinate = relatedVert.MyCoordinate;
			PathTo = LNX_Path.None;

			if (myVert.V_Position != relatedVert.V_Position)
			{
				if (myVert.MyCoordinate.TrianglesIndex == relatedVert.MyCoordinate.TrianglesIndex) //"If we're siblings". More performant than using the AreSiblings() method
				{
					//Debug.LogWarning($"same spot or siblings");
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
					//Debug.LogWarning($"Getting reversed relational pathing...");
					List<Vector3> pthPts = new List<Vector3>();
					List<Vector3> pthNrmls = new List<Vector3>();

					for (int i = relatedVert.Relationships[myVert.Index_Relational].PathTo.PathPoints.Count - 1; i > 0; i--)
					{
						pthPts.Add(relatedVert.Relationships[myVert.Index_Relational].PathTo.PathPoints[i].V_Position);
						pthNrmls.Add(relatedVert.Relationships[myVert.Index_Relational].PathTo.PathPoints[i].V_normal);
					}

					PathTo = new LNX_Path
					(
						pthPts,
						pthNrmls,
						nvMsh.V_SurfaceOrientation
					);
				}
				else
				{
					//nvMsh.CalculatePath(myVert.V_Position, relatedVert.V_Position, 0f, out PathTo, false); //this is by far where most of the time is being spent

					/*
					#region determine if can see is true..
					CanSee = true;
					Vector3 v_inLineCheck = LNX_Utils.FlatVector( relatedVert.V_Position - myVert.V_Position, nvMsh.GetSurfaceNormal() ).normalized;
					for ( int i = 0; i < PathTo.PathPoints.Count-1; i++ )
					{
						if( LNX_Utils.FlatVector(PathTo.PathPoints[i].V_ToNext, nvMsh.GetSurfaceNormal().normalized) != v_inLineCheck )
						{
							CanSee = false;
							break;
						}
					}
					#endregion
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

			//Debug.Log($"end of vertrelationship ctor. total time: '{DateTime.Now.Subtract(dt_start)}' " +
			//$"total ms: '{DateTime.Now.Subtract(dt_start).TotalMilliseconds}'");
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
			RelatedVertCoordinate = new LNX_ComponentCoordinate(myVert.TriangleIndex, siblingVertIndex);

			PathTo = new LNX_Path
			(
				new List<Vector3>() { myVert.V_Position, sharedTri.Verts[siblingVertIndex].V_Position },
				new List<Vector3>() { sharedTri.V_PathingNormal, sharedTri.V_PathingNormal },
				sharedTri.v_SurfaceNormal_cached
			);
		}

		public override string ToString()
		{
			return $"Related: '{RelatedVertCoordinate}'\n" +
				$"{nameof(CanSee)}: '{CanSee}'\n" +
				$"PathPoints: '{PathTo.PathPoints.Count}'\n" +
				$"path distance: '{PathDistance}'\n" +
				$"vTo: '{PathTo.V_CrowFlies}'\n" +
				$"";
		}
	}
	#endregion
}
