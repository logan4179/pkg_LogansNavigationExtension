using System;
using System.Text;
using UnityEngine;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_VertexRelationship //todo: If i make RelatedVertCoordinate into a property, I think I can just do away with this struct and just use the LNX_Path instead...
	{
		#region COORDINATE ==========================================================
		public LNX_ComponentCoordinate RelatedVertCoordinate; //todo: this is redundant bc it's contained in the path. Take this away, but should wait until I have unit tests. Once I take this away, it might not even be necessary to have this class

		public Vector3 RelatedVertPosition => PathTo.EndPosition;
		public Vector3 OwnerVertPosition => PathTo.StartPosition;
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
		public Vector3 V_to_flat => PathTo.V_CrowFiles_flat;

		#endregion

		public bool AmValid //Started using this bc for some reason, having a static LNX_VertexRelationship.None was causing problems in in LNX_Vertex.CalculateDerivedInfo().
		{
			get
			{
				return RelatedVertCoordinate.AmValid && PathTo.AmValid;
			}
		}

		//private static LNX_VertexRelationship none = new LNX_VertexRelationship( LNX_Path.None ); //todo: dws unless I figure out why this causes problems in LNX_Vertex.CalculateDerivedInfo()

		public string DBG_History;
		public int SpecialInt = -1;

		#region CONSTRUCTORS ======================================================================
		public LNX_VertexRelationship()
		{
			//Debug.Log($"first ctor");
			PathTo = new LNX_Path();
			RelatedVertCoordinate = LNX_ComponentCoordinate.None;
		}

		public LNX_VertexRelationship(LNX_Vertex myVert, LNX_Vertex relatedVert, LNX_NavMesh nvMsh, bool allowBorrowing = false)
		{
			DBG_History = $"ctorA (used for distals)\n";

			StringBuilder rprt = new StringBuilder($"LNX_VertexRelationship ctor()");
			Debug.Log($"LNX_VertexRelationship ctor('{myVert}', '{relatedVert}')--------------//");

			DateTime dt_start = DateTime.Now;

			RelatedVertCoordinate = relatedVert.MyCoordinate;
			//PathTo = LNX_Path.None;
			PathTo = new LNX_Path();

			if (myVert == null || relatedVert == null)
			{
				Debug.LogError($"LNX ERROR! One of the supplied verts in vertex relationship constructor was null. " +
					$"myVert null: '{myVert == null}', relatedVert null: '{relatedVert == null}'");
				RelatedVertCoordinate = LNX_ComponentCoordinate.None;
				return;
			}

			if (myVert.V_Position == relatedVert.V_Position)
			{
				Debug.Log($"happened A, positions are the same...");
				DBG_History += $"point A: positions are the same...\n";
				PathTo = new LNX_Path
				(
					nvMsh.GetSurfaceProjectionVector(),
					new LNX_NavmeshHit(relatedVert, nvMsh.Triangles[myVert.MyCoordinate.TrianglesIndex].V_PathingNormal)
				);
				return;
			}

			if
			(
				allowBorrowing &&
				relatedVert.IsRelationshipCollectionSuperficiallyValid(nvMsh.Triangles.Length)
			)
			{
				Debug.Log($"Immediate criteria met to investigate if relationship can be reversed...");
				DBG_History += $"point B: investigating if this can construct via reversed existing relationship...\n";

				LNX_VertexRelationship borrowRel = relatedVert.GetRelationship(myVert.MyCoordinate);
				Debug.Log($"borrowed rel valid: '{borrowRel.AmValid}', path null: '{borrowRel.PathTo == null}'");

				if (borrowRel.AmValid)
				{
					Debug.LogWarning($"borrowed rel '{borrowRel}' was valid with path count: '{borrowRel.PathTo.PointCount}'...");
					DBG_History += $"borrowed rel was valid with point count: '{borrowRel.PathTo.PointCount}'. Reversing...\n";
					PathTo = borrowRel.PathTo.Reversed();
					DBG_History += $"after borrow. pathto: '{PathTo}', count: '{PathTo.PointCount}'...\n";
					return;
				}

				if (borrowRel.PathTo != null)
				{
					Debug.Log($"borrowed rel path count: '{borrowRel.PathTo.PointCount}'");

				}

				Debug.Log($"borrowed rel '{borrowRel}' was apparently NOT valid. Continuing further...");
				DBG_History += $"borrowed rel '{borrowRel}' was apparently NOT valid. Continuing further...\n";
			}

			if (myVert.MyCoordinate.TrianglesIndex == relatedVert.MyCoordinate.TrianglesIndex ||
				relatedVert.SharesVertSpace(nvMsh.Triangles[myVert.Coordinate_FirstSibling.TrianglesIndex].Verts[myVert.Coordinate_FirstSibling.ComponentIndex]) ||
				relatedVert.SharesVertSpace(nvMsh.Triangles[myVert.Coordinate_SecondSibling.TrianglesIndex].Verts[myVert.Coordinate_SecondSibling.ComponentIndex])
			) //"If we're siblings". More performant than using the AreSiblings() method
			{
				DBG_History += $"point C: siblings, or in the same position as a sibling...\n";

				rprt.AppendLine($"Siblings, or in same spot as siblings");
				Debug.Log($"Siblings, or in same spot as siblings");
				PathTo = new LNX_Path
				(
					nvMsh.GetSurfaceProjectionVector(),
					new LNX_NavmeshHit(myVert, nvMsh.Triangles[myVert.MyCoordinate.TrianglesIndex].V_PathingNormal),
					new LNX_NavmeshHit(relatedVert, nvMsh.Triangles[myVert.MyCoordinate.TrianglesIndex].V_PathingNormal)
				);
			}
			else
			{
				DBG_History += $"point D: NOT siblings, Can't addume path. Actually calculating the path...\n";

				rprt.AppendLine($"Not siblings. Can't assume path. Actually calculating the path...");
				Debug.Log($"Not siblings. Can't assume path. Actually calculating the path...");
				/*
				//Just to make things run smoother for now...
				PathTo = new LNX_Path
				(
					nvMsh.GetSurfaceProjectionVector(),
					new LNX_NavmeshHit(myVert, nvMsh.Triangles[myVert.MyCoordinate.TrianglesIndex].V_PathingNormal),
					new LNX_NavmeshHit(relatedVert, nvMsh.Triangles[myVert.MyCoordinate.TrianglesIndex].V_PathingNormal)
				);
				*/

				Debug.Log($"Now calculating path from '{myVert}' to '{relatedVert}'...");
				
				//if( myVert.ComponentIndex == 1 && relatedVert.TriangleIndex == 76)
				if (myVert.ComponentIndex == 0 && relatedVert.TriangleIndex == 22 && relatedVert.ComponentIndex == 1)
				{
					Debug.LogWarning($"got to relationship in question...");
					LNX_MethodDebugReport dbgRprt = new LNX_MethodDebugReport();
					dbgRprt.StartReport();
					nvMsh.CalculatePath_dbg(
						myVert, relatedVert, out PathTo, ref dbgRprt
					); //this is by far where most of the time is being spent
					dbgRprt.EndReport();

					Debug.Log($"end of calculatepath. Report:");
					Debug.LogWarning(dbgRprt.ReportString);
					LNX_VertexRelationship vrel = nvMsh.Triangles[myVert.TriangleIndex].Verts[0].GetFurthestDistanceRelationshipOnTriangle(relatedVert.TriangleIndex);
					Debug.Log($"dbg for getfurthestdist got vrel: '{vrel}', pathdist: '{vrel.PathDistance}', pt count: '{vrel.PathTo.PointCount}'");
					for (int i = 0; i < vrel.PathTo.PointCount; i++)
					{
						//Debug.Log($"i: '{i}', {vrel.PathTo.PathPoints[i]}...");
					}
				}
				else
				{
					nvMsh.CalculatePath(
						myVert, relatedVert, out PathTo
					); //this is by far where most of the time is being spent
				}
				

				/*
				nvMsh.CalculatePath(
					myVert, relatedVert, out PathTo
				); //this is by far where most of the time is being spent
				*/

				Debug.Log($"created path: '{PathTo}' with '{PathTo.PointCount}' pathpoints\n" +
					$"amstraight: '{PathTo.AmStraight}'");

				DBG_History += $"created path: '{PathTo}' with '{PathTo.PointCount}' pathpoints...\n";
				rprt.AppendLine($"created path: '{PathTo}'");

				rprt.AppendLine($"path has '{PathTo.PathPoints.Count}' points...\n"); //<<< todo: why is this causing probs?
			}

			rprt.AppendLine($"end of vertrelationship ctor. total time: '{DateTime.Now.Subtract(dt_start)}' " +
				$"total ms: '{DateTime.Now.Subtract(dt_start).TotalMilliseconds}'");
		}

		public LNX_VertexRelationship(LNX_Vertex myVert, LNX_Vertex relatedVert, LNX_NavMesh nvMsh, bool allowBorrowing,
			ref LNX_MethodDebugReport rprt)
		{
			DBG_History = $"ctorB (used for tdg)\n";

			rprt.Log($"LNX_VertexRelationship ctor('{myVert}', '{relatedVert}')");

			DateTime dt_start = DateTime.Now;

			RelatedVertCoordinate = relatedVert.MyCoordinate;
			PathTo = null;

			if (myVert == null || relatedVert == null)
			{
				Debug.LogError($"LNX ERROR! One of the supplied verts in vertex relationship constructor was null. " +
					$"myVert null: '{myVert == null}', relatedVert null: '{relatedVert == null}'");
				rprt.Log($"LNX ERROR! One of the supplied verts in vertex relationship constructor was null. " +
					$"myVert null: '{myVert == null}', relatedVert null: '{relatedVert == null}'");
				RelatedVertCoordinate = LNX_ComponentCoordinate.None;
				return;
			}

			if (myVert.V_Position == relatedVert.V_Position)
			{
				rprt.Log($"happened A, positions are the same...");
				PathTo = new LNX_Path
				(
					nvMsh.GetSurfaceProjectionVector(),
					new LNX_NavmeshHit(relatedVert, nvMsh.Triangles[myVert.MyCoordinate.TrianglesIndex].V_PathingNormal)
				);
				return;
			}

			if
			(
				allowBorrowing &&
				relatedVert.IsRelationshipCollectionSuperficiallyValid(nvMsh.Triangles.Length)
			)
			{
				rprt.Log($"Investigating if relationship can be reversed...");

				LNX_VertexRelationship borrowRel = relatedVert.GetRelationship(myVert.MyCoordinate);
				rprt.Log($"borrowed rel valid: '{borrowRel.AmValid}', path null: '{borrowRel.PathTo == null}'");

				if (borrowRel.AmValid)
				{
					rprt.Log($"borrowed rel '{borrowRel}' was valid with path count: '{borrowRel.PathTo.PointCount}'...");
					PathTo = borrowRel.PathTo.Reversed();
					return;
				}

				if (borrowRel.PathTo != null)
				{
					rprt.Log($"borrowed rel path count: '{borrowRel.PathTo.PointCount}'");

				}

				rprt.Log($"borrowed rel '{borrowRel}' was apparently NOT valid. Continuing further...");
			}
			else
			{
				rprt.Log($"conditions not correct for borrowing. Proceding further...");
			}

			if (myVert.MyCoordinate.TrianglesIndex == relatedVert.MyCoordinate.TrianglesIndex ||
				relatedVert.SharesVertSpace(nvMsh.Triangles[myVert.Coordinate_FirstSibling.TrianglesIndex].Verts[myVert.Coordinate_FirstSibling.ComponentIndex]) ||
				relatedVert.SharesVertSpace(nvMsh.Triangles[myVert.Coordinate_SecondSibling.TrianglesIndex].Verts[myVert.Coordinate_SecondSibling.ComponentIndex])
			) //"If we're siblings". More performant than using the AreSiblings() method
			{
				rprt.Log($"Siblings, or in same spot as siblings");
				
				PathTo = new LNX_Path
				(
					nvMsh.GetSurfaceProjectionVector(),
					new LNX_NavmeshHit(myVert, nvMsh.Triangles[myVert.MyCoordinate.TrianglesIndex].V_PathingNormal),
					new LNX_NavmeshHit(relatedVert, nvMsh.Triangles[myVert.MyCoordinate.TrianglesIndex].V_PathingNormal)
				);
				
			}
			else
			{
				rprt.Log($"Not siblings. Can't derive path. Actually calculating the path...");
				/*
				//Just to make things run smoother for now...
				PathTo = new LNX_Path
				(
					nvMsh.GetSurfaceProjectionVector(),
					new LNX_NavmeshHit(myVert, nvMsh.Triangles[myVert.MyCoordinate.TrianglesIndex].V_PathingNormal),
					new LNX_NavmeshHit(relatedVert, nvMsh.Triangles[myVert.MyCoordinate.TrianglesIndex].V_PathingNormal)
				);
				*/

				
				nvMsh.CalculatePath_dbg(
					myVert, relatedVert, out PathTo, ref rprt
				); //this is by far where most of the time is being spent
				

				/*
				nvMsh.CalculatePath(
					myVert, relatedVert, out PathTo
				); //this is by far where most of the time is being spent
				*/

				rprt.Log($"created path: '{PathTo}'");

				rprt.Log($"path has '{PathTo.PathPoints.Count}' points...\n");
			}

			rprt.Log($"end of vertrelationship ctor. total time: '{DateTime.Now.Subtract(dt_start)}' " +
				$"total ms: '{DateTime.Now.Subtract(dt_start).TotalMilliseconds}'");
		}

		public LNX_VertexRelationship(LNX_Path path)
		{
			DBG_History = $"ctorC (passed path)\n";

			StringBuilder sb_rprt = new StringBuilder();
			sb_rprt.AppendLine($"LNX_VertexRelationship ctor (fastpath version)");

			DateTime dt_start = DateTime.Now;

			RelatedVertCoordinate = new LNX_ComponentCoordinate(path.EndHit.TriangleIndex, path.EndHit.VertIndex);
			PathTo = new LNX_Path(path);

			sb_rprt.AppendLine($"end of vertrelationship ctor. total time: '{DateTime.Now.Subtract(dt_start)}' " +
				$"total ms: '{DateTime.Now.Subtract(dt_start).TotalMilliseconds}'");

			//Debug.LogWarning(sb_rprt);
			/*
			if (DateTime.Now.Subtract(dt_start).TotalMilliseconds > 1.5)
			{
				Debug.LogWarning(sb_rprt);
			}
			else
			{
				Debug.Log(sb_rprt);
			}
			*/
		}

		public LNX_VertexRelationship(int triIndex, int vertIndex, LNX_Path path)
		{
			DBG_History = $"ctorD\n";

			RelatedVertCoordinate = new LNX_ComponentCoordinate(triIndex, vertIndex);
			PathTo = path;
		}
		#endregion

		public bool ValueEquals( LNX_VertexRelationship otherRelationship)
		{
			if( otherRelationship == null || RelatedVertCoordinate != otherRelationship.RelatedVertCoordinate )
			{
				return false;
			}

			if( PathTo == null )
			{
				if( otherRelationship.PathTo != null )
				{
					return false;
				}
				else
				{
					return true;
				}
			}
			else if( otherRelationship.PathTo == null ) 
			{
				if ( PathTo != null)
				{
					return false;
				}
				else
				{
					return true;
				}
			}

			return PathTo.ValueEquals(otherRelationship.PathTo);
		}

		#region OPERATORS ==================================================
		/*
		public override bool Equals(object obj)
		{
			if (!(obj is LNX_VertexRelationship))
				return false;

			LNX_VertexRelationship other = (LNX_VertexRelationship)obj;
			if (other.RelatedVertCoordinate != RelatedVertCoordinate )
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
			return HashCode.Combine(RelatedVertCoordinate, PathTo);
		}

		
		public static bool operator ==(LNX_VertexRelationship a, LNX_VertexRelationship b)
		{
			return a.Equals(b);
		}
		public static bool operator !=(LNX_VertexRelationship a, LNX_VertexRelationship b)
		{
			return !a.Equals(b);
		}
		*/
		#endregion


		public override string ToString()
		{
			if (this == null)
			{
				return "NULL";
			}

			//return this == none ? "LNX_VertexRelationship.None" : $"{(PathTo.PathPoints.Count > 0 ? $"([{PathTo.StartHit.TriangleIndex}][{PathTo.StartHit.VertIndex}]" : "[?]")}->" +
			//$"{RelatedVertCoordinate})";

			return $"{PathTo}";

		}

		public string GetInfoString()
		{
			string s = $"Related: '{RelatedVertCoordinate}'\n" +
				$"{nameof(CanSee)}: '{CanSee}'\n" +
				$"";

			if (PathTo.PathPoints == null)
			{
				s += "PathPoints collection is null...";
			}
			else if (PathTo.PathPoints.Count <= 0)
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

}
