using JetBrains.Annotations;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;


namespace LogansNavigationExtension
{
	public static class LNX_Utils
	{
		/// <summary>
		/// Casts multiple times in a cross formation around origin. If any of the casts finds a hit, it stops the operation immediately and returns true.
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="radius"></param>
		/// <param name="layerMask"></param>
		/// <returns></returns>
		public static bool CrossCast(Vector3 origin, float radius, out RaycastHit hitInfo, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
		{
			if (Physics.Linecast(origin + (Vector3.up * radius), origin + (Vector3.down * radius), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}
			else if (Physics.Linecast(origin + (Vector3.down * radius), origin + (Vector3.up * radius), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}

			if (Physics.Linecast(origin + (Vector3.right * radius), origin + (Vector3.left * radius), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}
			else if (Physics.Linecast(origin + (Vector3.left * radius), origin + (Vector3.right * radius), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}

			if (Physics.Linecast(origin + (Vector3.forward * radius), origin + (Vector3.back * radius), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}
			else if (Physics.Linecast(origin + (Vector3.back * radius), origin + (Vector3.forward * radius), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}
			return false;
		}

		public static bool CrossCast(Vector3 origin, Vector3 end, float extendCastDist, out RaycastHit hitInfo, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
		{
			Vector3 vExtend = Vector3.Normalize(end - origin) * extendCastDist;
			Debug.Log($"vExtend: '{vExtend}'");

			if (Physics.Linecast(origin - vExtend, end + vExtend, out hitInfo, layerMask, queryTriggerInteraction))
			{
				Debug.Log("crosscast immediately made hit");
				return true;
			}
			else if (Physics.Linecast(end + vExtend, origin - vExtend, out hitInfo, layerMask, queryTriggerInteraction))
			{
				Debug.Log("crosscast immediately made reverse hit");

				return true;
			}

			Debug.Log($"crosscast wasn't immediately succesful for pt: '{origin}'");


			float dist = Vector3.Distance(origin, end);

			if (Physics.Linecast(origin + (Vector3.up * dist), origin + (Vector3.down * dist), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}
			else if (Physics.Linecast(origin + (Vector3.down * dist), origin + (Vector3.up * dist), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}

			if (Physics.Linecast(origin + (Vector3.right * dist), origin + (Vector3.left * dist), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}
			else if (Physics.Linecast(origin + (Vector3.left * dist), origin + (Vector3.right * dist), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}

			if (Physics.Linecast(origin + (Vector3.forward * dist), origin + (Vector3.back * dist), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}
			else if (Physics.Linecast(origin + (Vector3.back * dist), origin + (Vector3.forward * dist), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}

			Debug.LogWarning($"crosscast ultimately failed for pt: '{origin}'");
			return false;
		}

		public static Vector3 CreateCornerPathPoint(LNX_PathPoint startPt, LNX_PathPoint endPt)
		{
			Vector3 resultPt = Vector3.zero;

			//https://math.libretexts.org/Bookshelves/Algebra/Algebra_and_Trigonometry_1e_(OpenStax)/10%3A_Further_Applications_of_Trigonometry/10.01%3A_Non-right_Triangles_-_Law_of_Sines

			Vector3 v_starPtTToEndPt = (endPt.V_Point - startPt.V_Point);
			Vector3 v_endPtToStartPt = -v_starPtTToEndPt;
			float dist_hypotenuse = v_endPtToStartPt.magnitude;
			float angleA = 90f - Vector3.Angle(startPt.V_normal, v_starPtTToEndPt.normalized);
			float angleB = 90f - Vector3.Angle(endPt.V_normal, v_endPtToStartPt.normalized);
			float angle_opposingHypotenuse = 180f - angleA - angleB;

			//note: need to convert to radians in the following, as opposed to degrees...
			float distA = Mathf.Sin(Mathf.Deg2Rad * angleB) * (dist_hypotenuse / Mathf.Sin(Mathf.Deg2Rad * angle_opposingHypotenuse)); //This is a re-ordered algebraic equation based on trigonometry

			resultPt = startPt.V_Point + Vector3.ProjectOnPlane(v_starPtTToEndPt, startPt.V_normal).normalized * distA;

			return resultPt;
		}

		public static Vector3 GetCenterVector(Vector3[] corners)
		{
			Vector3 vCenter = Vector3.zero;

			for (int i = 0; i < corners.Length; i++)
			{
				vCenter += corners[i];
			}

			return vCenter / corners.Length;
		}

		public static Vector3 FlatVector( Vector3 vector, LNX_Direction flattenDir = LNX_Direction.PositiveY )
		{
			Vector3 vNormal = Vector3.zero;
			if ( flattenDir == LNX_Direction.PositiveY || flattenDir == LNX_Direction.NegativeY )
			{
				vNormal = Vector3.up;
			}
			else if ( flattenDir == LNX_Direction.PositiveX || flattenDir == LNX_Direction.NegativeX )
			{
				vNormal = Vector3.right;
			}
			else if ( flattenDir == LNX_Direction.PositiveZ || flattenDir == LNX_Direction.NegativeZ )
			{
				vNormal = Vector3.forward;
			}

			return FlatVector( vector, vNormal );
		}

		public static Vector3 FlatVector( Vector3 vector, Vector3 nrml )
		{
			if (nrml == Vector3.up || nrml == Vector3.down)
			{
				return new Vector3(vector.x, 0f, vector.z);
			}
			else if (nrml == Vector3.right || nrml == Vector3.left)
			{
				return new Vector3(0f, vector.y, vector.z);
			}
			else if (nrml == Vector3.forward || nrml == Vector3.back)
			{
				return new Vector3(vector.x, vector.y, 0f);
			}
			else if ( nrml != Vector3.zero )
			{
				return Vector3.ProjectOnPlane( vector, nrml );
			}

			return Vector3.zero;
		}

		public static Vector3 FlooredVector( Vector3 vector, Vector3 floorBase, Vector3 nrml)
		{
			if ( nrml == Vector3.up || nrml == Vector3.down )
			{
				return new Vector3( vector.x, floorBase.y, vector.z );
			}
			else if ( nrml == Vector3.right || nrml == Vector3.left )
			{
				return new Vector3( floorBase.x, vector.y, vector.z );
			}
			else //if ( nrml == Vector3.forward || nrml == Vector3.back )
			{
				return new Vector3( vector.x, vector.y, floorBase.z );
			}
		}

		#region MATH OPERATIONS --------------------------------------
		public static float CalculateTriangleEdgeLength( float angA, float angB, float lenB )
		{
			return Mathf.Sin(angA * Mathf.Deg2Rad) * lenB / Mathf.Sin(angB * Mathf.Deg2Rad);
		}
		#endregion

		#region FOR COMPONENT SELECTION ("GRABBING")-------------------------
		//could put methods in here to shorten constructing the list of vertices grabbed by various components... idk if it's worth it...
		#endregion

		#region FOR MESH MANIPULATION-------------------------
		public static bool AmPointingAt(Vector3 vOrigin, Vector3 vProjection, Vector3 vCenter, Vector3[] corners)
		{
			Vector3 v_originToCenter = Vector3.Normalize(vCenter - vOrigin);
			float dot_projectionToCenter = Vector3.Dot(v_originToCenter, vProjection);

			for (int i = 0; i < corners.Length; i++)
			{
				Vector3 v_originToCorner = Vector3.Normalize(corners[i] - vOrigin);

				if (dot_projectionToCenter < Vector3.Dot(v_originToCorner, v_originToCenter))
				{
					return false;
				}
			}

			return true;
		}
		/*
		public static bool AmPointingAt( Vector3 vOrigin, Vector3 vProjection, Vector3[] corners, out string dbgString )
		{
			dbgString = $"perspective : '{vOrigin}' vProj: '{vProjection}' \n";
			Vector3 v_center = GetCenterVector( corners );
			
			dbgString += $"ctr of mesh: '{v_center}' \n";

			Vector3 v_originToCenter = Vector3.Normalize( v_center - vOrigin );
			Vector3 v_originToProjection = Vector3.Normalize( (vOrigin + vProjection) - vOrigin );
			float dot_mouseProjAlignedWithCtr = Vector3.Dot( v_originToCenter, v_originToProjection );
			dbgString += $"v_orgnToCtr: '{v_originToCenter}' \n";
			dbgString += $"v_originToProjection: '{v_originToProjection}'\n";
			dbgString += $" dot_prjToCtr: '{dot_mouseProjAlignedWithCtr}'\n\n";

			for (int i = 0; i < corners.Length; i++)
			{
				Vector3 v_originToCrnr = Vector3.Normalize(corners[i] - vOrigin);
				dbgString += $"v_originToCrnr: '{v_originToCrnr}' dot '{Vector3.Dot(v_originToCrnr, v_originToCenter)}'\n";

				if (dot_mouseProjAlignedWithCtr < Vector3.Dot(v_originToCrnr, v_originToCenter) )
				{
					dbgString += $"failed at corner: '{i}'";
					return false;
				}
			}

			return true;
		}
		*/

		// The idea: every point on the triangle will have at least one other point where the dot product of the vToPtA is more aligned with vToProjection 
		// than vToPointB...
		public static bool AmPointingAt(Vector3 vOrigin, Vector3 vProjection, LNX_Triangle tri)
		{
			Vector3 v_toProjection = Vector3.Normalize((vOrigin + vProjection) - vOrigin);

			Vector3[] vToVerts = new Vector3[3]
			{
				Vector3.Normalize(tri.Verts[0].V_Position - vOrigin),
				Vector3.Normalize(tri.Verts[1].V_Position - vOrigin),
				Vector3.Normalize(tri.Verts[2].V_Position - vOrigin)
			};

			float alignment_projWithV0 = Vector3.Dot(v_toProjection, vToVerts[0]);
			float alignment_projWithV1 = Vector3.Dot(v_toProjection, vToVerts[1]);
			float alignment_projWithV2 = Vector3.Dot(v_toProjection, vToVerts[2]);

			float[] alignments_projectionWithVerts = new float[3]
			{
				Vector3.Dot(v_toProjection, vToVerts[0]),
				Vector3.Dot(v_toProjection, vToVerts[1]),
				Vector3.Dot(v_toProjection, vToVerts[2])
			};

			float alignment_v0toV1 = Vector3.Dot(vToVerts[0], vToVerts[1]);
			float alignment_v0toV2 = Vector3.Dot(vToVerts[0], vToVerts[2]);
			float alignment_v1toV2 = Vector3.Dot(vToVerts[1], vToVerts[2]);


			//0...
			if (alignment_projWithV1 < alignment_v0toV1 &&
				alignment_projWithV2 < alignment_v0toV2
			)
			{
				return false;
			}

			//1...
			if (alignment_projWithV0 < alignment_v0toV1 &&
				alignment_projWithV2 < alignment_v1toV2
			)
			{
				return false;
			}

			//2...
			if (alignment_projWithV0 < alignment_v0toV2 &&
				alignment_projWithV1 < alignment_v1toV2
			)
			{
				return false;
			}

			return true;
		}

		public static bool AmPointingAt(Vector3 vOrigin, Vector3 vProjection, Vector3[] corners, out string dbgString)
		{
			dbgString = $"perspective : '{vOrigin}' vProj: '{vProjection}' \n";
			Vector3 v_center = GetCenterVector(corners);
			Vector3 v_originToProjection = Vector3.Normalize((vOrigin + vProjection) - vOrigin);

			Vector3[] originToCrnrVectors = new Vector3[corners.Length];
			int mostAlignedCrnr = 0;
			float runningClosestDot = -1f;
			for (int i = 0; i < corners.Length; i++)
			{
				originToCrnrVectors[i] = Vector3.Normalize(corners[i] - vOrigin);

				float d = Vector3.Dot(v_originToProjection, originToCrnrVectors[i]);
				if (d > runningClosestDot)
				{
					mostAlignedCrnr = i;
				}
			}

			/*
			Run through all corners and check that v_originToProjection has a closer dot product than...
			*/
			for (int i = 0; i < corners.Length; i++)
			{
				if (i != mostAlignedCrnr)
				{

				}
			}


			/*

				Vector3 v_originToCenter = Vector3.Normalize(v_center - vOrigin);
			float dot_mouseProjAlignedWithCtr = Vector3.Dot(v_originToCenter, v_originToProjection);

			for ( int i = 0; i < corners.Length; i++ )
			{
				Vector3 v_crnrToOrigin = Vector3.Normalize( vOrigin - corners[i] );
				Vector3 v_originToCorner = Vector3.Normalize(corners[i] - vOrigin);

				if (dot_mouseProjAlignedWithCtr < Vector3.Dot(v_originToCrnr, v_originToCenter))
				{
					return false;
				}
			}
			*/

			return true;
		}

		/// <summary>
		/// Gets the verts that should be moved during a cut to form the 
		/// </summary>
		/// <param name="nm"></param>
		/// <param name="primaryEdge"></param>
		/// <param name="secondaryEdge"></param>
		/// <param name="pt"></param>
		/// <returns></returns>
		public static List<LNX_Vertex> GetMoveVerts_forInsertLoop(LNX_NavMesh nm, LNX_Edge primaryEdge, LNX_Edge secondaryEdge)
		{
			List<LNX_Vertex> returnVerts = new List<LNX_Vertex>();

			Vector3 avgdMidPt = (primaryEdge.MidPosition + secondaryEdge.MidPosition) * 0.5f;
			//Debug.DrawLine(avgdMidPt, avgdMidPt + (Vector3.up * 3f), Color.red, 3f);

			LNX_Triangle primaryTri = nm.GetTriangle( primaryEdge.MyCoordinate );
			float runningfurthestdist = 0f;
			int edgIndx = -1;
			for (int i = 0; i < 3; i++) //find the edge with the furthest away mid position
			{
				float dst = Vector3.Distance(primaryEdge.MidPosition, avgdMidPt);
				if (primaryTri.Edges[i] != primaryEdge && dst > runningfurthestdist)
				{
					runningfurthestdist = dst;
					edgIndx = i;
				}
			}

			LNX_Edge moveEdge = primaryTri.Edges[edgIndx];

			//find verts...
			returnVerts.Add(primaryTri.Verts[moveEdge.StartVertCoordinate.ComponentIndex]);
			returnVerts.Add(primaryTri.Verts[moveEdge.EndVertCoordinate.ComponentIndex]);

			if (nm.GetVertexAtCoordinate(secondaryEdge.StartVertCoordinate).V_Position == moveEdge.StartPosition ||
				nm.GetVertexAtCoordinate(secondaryEdge.StartVertCoordinate).V_Position == moveEdge.EndPosition
			)
			{
				returnVerts.Add(nm.GetVertexAtCoordinate(secondaryEdge.StartVertCoordinate));
			}
			else if (nm.GetVertexAtCoordinate(secondaryEdge.EndVertCoordinate).V_Position == moveEdge.StartPosition ||
				nm.GetVertexAtCoordinate(secondaryEdge.EndVertCoordinate).V_Position == moveEdge.EndPosition
			)
			{
				returnVerts.Add(nm.GetVertexAtCoordinate(secondaryEdge.EndVertCoordinate));
			}

			return returnVerts;
		}
		#endregion

#if UNITY_EDITOR
		#region GIZMO DRAWING-------------------------------------
		public static void DrawTriGizmos( LNX_Triangle tri )
		{
			Gizmos.DrawLine(tri.Verts[0].V_Position, tri.Verts[1].V_Position);
			Gizmos.DrawLine(tri.Verts[1].V_Position, tri.Verts[2].V_Position);
			Gizmos.DrawLine(tri.Verts[2].V_Position, tri.Verts[0].V_Position);
		}

		public static void DrawTriHandles( LNX_Triangle tri, float thickness )
		{
			Handles.DrawLine(tri.Verts[0].V_Position, tri.Verts[1].V_Position, thickness );
			Handles.DrawLine(tri.Verts[1].V_Position, tri.Verts[2].V_Position, thickness );
			Handles.DrawLine(tri.Verts[2].V_Position, tri.Verts[0].V_Position, thickness );
		}
		#endregion
#endif
	}

	#region ENUMS-----------------------------------------
	[System.Serializable]
	public enum LNX_SelectMode
	{
		None = 0,
		Vertices = 1,
		Edges = 2,
		Faces = 3
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
		NegativeZ = 0,
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

		public LNX_ComponentCoordinate( int triIndx, int cmptIndx )
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

	public struct LNX_ProjectionHit
	{
		/// <summary>Index of the Triangle or component that was hit, depending on the context.</summary>
		public int Index_Hit;
		public Vector3 HitPosition;

		private static LNX_ProjectionHit none = new LNX_ProjectionHit(-1, Vector3.zero);

		public LNX_ProjectionHit(int indx, Vector3 pos)
		{
			Index_Hit = indx;
			HitPosition = pos;
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
			if ( !(obj is LNX_ProjectionHit) )
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

	#region RELATIONSHIPS------------------------------------------------------------------------
	[System.Serializable]
	public struct LNX_VertexRelationship
	{
		public LNX_ComponentCoordinate RelatedVertCoordinate;

		public Vector3 RelatedVertPosition;

		public bool CanSee;

		/// <summary>The shortest possible distance to the destination vertex via traveling over the surface of the navmesh</summary>
		public float FlatDistance => PathTo.Distance;

		/// <summary>The most direct path from the perspective vert to the related vert </summary>
		public LNX_Path PathTo;

		public LNX_VertexRelationship( LNX_Vertex myVert, LNX_Vertex relatedVert, LNX_NavMesh nvMsh )
		{
			RelatedVertCoordinate = relatedVert.MyCoordinate;

			RelatedVertPosition = relatedVert.V_Position;

			PathTo = LNX_Path.None;
			CanSee = false;

			if ( myVert.AreSiblings(relatedVert) )
			{
				PathTo = new LNX_Path
				( 
					new List<Vector3>(){ myVert.V_Position, relatedVert.V_Position }, 
					new List<Vector3>() { nvMsh.Triangles[myVert.MyCoordinate.TrianglesIndex].V_PathingNormal,
					nvMsh.Triangles[relatedVert.MyCoordinate.TrianglesIndex].V_PathingNormal}
				);
				CanSee = true;
			}
			else
			{
				//CanSee = !nvMsh.Raycast(myVert.V_Position, relatedVert.V_Position, 1f);

				if ( CanSee )
				{
					/*try
					{
						nvMsh.CalculatePath( myVert.V_Position, relatedVert.V_Position, 0.3f, out PathTo );
					}
					catch (System.Exception e)
					{
						Debug.Log($"caught exception. dumping report...");
						Debug.Log( nvMsh.dbgCalculatePath );
						throw;
					}*/
				}
			}
		}

		public override string ToString()
		{
			return $"Related: '{RelatedVertCoordinate}'\n" +
				$"{nameof(CanSee)}: '{CanSee}'\n" +
				$"flatdist: '{FlatDistance}'\n" +
				$"";
		}
	}
	#endregion
}