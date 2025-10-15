using JetBrains.Annotations;
using System;
using System.Collections.Generic;
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

			Vector3 v_starPtTToEndPt = (endPt.V_Position - startPt.V_Position);
			Vector3 v_endPtToStartPt = -v_starPtTToEndPt;
			float dist_hypotenuse = v_endPtToStartPt.magnitude;
			float angleA = 90f - Vector3.Angle(startPt.V_normal, v_starPtTToEndPt.normalized);
			float angleB = 90f - Vector3.Angle(endPt.V_normal, v_endPtToStartPt.normalized);
			float angle_opposingHypotenuse = 180f - angleA - angleB;

			//note: need to convert to radians in the following, as opposed to degrees...
			float distA = Mathf.Sin(Mathf.Deg2Rad * angleB) * (dist_hypotenuse / Mathf.Sin(Mathf.Deg2Rad * angle_opposingHypotenuse)); //This is a re-ordered algebraic equation based on trigonometry

			resultPt = startPt.V_Position + Vector3.ProjectOnPlane(v_starPtTToEndPt, startPt.V_normal).normalized * distA;

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
			if( nrml == Vector3.zero )
			{
				Debug.LogError( $"LNX ERROR! You supplied a normal parameter of 0. Need a normal direction in order to flatten!" );
				return Vector3.zero;
			}

			if ( (nrml == Vector3.up || nrml == Vector3.down) )
			{
				if( vector.y != 0f )
				{
					return new Vector3(vector.x, 0f, vector.z);
				}
				else
				{
					return vector;
				}
			}
			else if (nrml == Vector3.right || nrml == Vector3.left)
			{
				if( vector.x != 0f )
				{
					return new Vector3(0f, vector.y, vector.z);
				}
				else
				{
					return vector;
				}
			}
			else if (nrml == Vector3.forward || nrml == Vector3.back)
			{
				if( vector.z != 0f )
				{
					return new Vector3(vector.x, vector.y, 0f);
				}
				else
				{
					return vector;
				}
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

		public static bool PositionIsBetweenTriPoints( Vector3 pos, Vector3 triPointA,  Vector3 triPointB, Vector3 triPointC, Vector3 nrml/*, ref string dbgString*/ )
		{
			/*dbgString = $"{nameof(PositionIsBetweenTriPoints)}({pos}', \n" +
				$"'{triPointA}', '{triPointB}', '{triPointC}')\n" +
				$"'{nrml}'\n";*/

			Vector3 v_a_to_b = Vector3.Normalize( FlatVector(triPointB,nrml) - FlatVector(triPointA,nrml) );
			Vector3 v_a_to_c = Vector3.Normalize( FlatVector(triPointC, nrml) - FlatVector(triPointA, nrml) );
			Vector3 v_ptA_to_pos = Vector3.Normalize( FlatVector(pos,nrml) - FlatVector(triPointA,nrml) );

			float crnrAngle = Vector3.Angle( v_a_to_b, v_a_to_c ) + 0.001f;
			/*dbgString += $"{nameof(crnrAngle)}: '{crnrAngle}'\n" +
				$"{nameof(v_a_to_b)}: '{v_a_to_b}'\n" +
				$"{nameof(v_a_to_c)}: '{v_a_to_c}'\n";*/

			if
			(  
				Vector3.Angle(v_a_to_b, v_ptA_to_pos) < crnrAngle &&
				Vector3.Angle(v_a_to_c, v_ptA_to_pos) < crnrAngle
			)
			{
				//dbgString += $"cndtn1 passed...\n";
				Vector3 v_b_toA = -v_a_to_b;
				Vector3 v_b_to_c = Vector3.Normalize(FlatVector(triPointC, nrml) - FlatVector(triPointB, nrml));
				Vector3 v_ptB_to_pos = Vector3.Normalize( FlatVector(pos, nrml) - FlatVector(triPointB, nrml) );
				crnrAngle = Vector3.Angle(v_b_toA, v_b_to_c) + 0.001f;
				/*dbgString += $"new crnr angle: '{crnrAngle}'\n" +
					$"";*/

				if
				(
					Vector3.Angle(v_b_toA, v_ptB_to_pos) < crnrAngle &&
					Vector3.Angle(v_b_to_c, v_ptB_to_pos) < crnrAngle
				)
				{
					//dbgString += $"cndtn2 passed...\n";

					return true;
				}
			}

			return false;
		}

		public static bool AmBetweenConcurrentLines( Vector3 pos, Vector3 lineAStart, Vector3 lineAEnd, Vector3 lineBStart, Vector3 lineBEnd, Vector3 nrml, ref string dbgRprt )
		{
			bool dbgIt = false;

			if( lineAStart == lineAEnd && lineBStart == lineBEnd )
			{
				if(dbgIt) Debug.LogError($"LNX ERROR! You supplied '{nameof(AmBetweenConcurrentLines)}'() with two lines with the same start and end points.");
				dbgRprt += $"LNX ERROR! You supplied '{nameof(AmBetweenConcurrentLines)}'() with two lines with the same start and end points.\n";
				return false;
			}

			if ( lineAStart == lineAEnd )
			{
				if (dbgIt) Debug.Log("chose lineA equal to block...");
				dbgRprt += "chose lineA equal to block...\n";

				return PositionIsBetweenTriPoints( pos, lineAStart, lineBStart, lineBEnd, nrml/*, ref dbgit*/ );

			}
			if ( lineBStart == lineBEnd )
			{
				if (dbgIt) Debug.Log("chose lineB equal to block...");
				dbgRprt += "chose lineB equal to block...\n";

				return PositionIsBetweenTriPoints( pos, lineAStart, lineAEnd, lineBStart, nrml/*, ref dbgit*/);
			}
			else //This would be expected most of the time...
			{
				Vector3 v_lnAStart_to_lnAEnd = Vector3.Normalize(FlatVector(lineAEnd, nrml) - FlatVector(lineAStart, nrml));
				Vector3 v_lnBStart_to_lnBEnd = Vector3.Normalize(FlatVector(lineBEnd, nrml) - FlatVector(lineBStart, nrml));
				Vector3 v_lineAStart_to_pos = Vector3.Normalize(FlatVector(pos, nrml) - FlatVector(lineAStart, nrml));
				Vector3 v_lineBStart_to_pos = Vector3.Normalize(FlatVector(pos, nrml) - FlatVector(lineBStart, nrml));

				if (dbgIt)
				{
					Debug.Log($"vA: '{v_lnAStart_to_lnAEnd}', vB: '{v_lnBStart_to_lnBEnd}', " +
					$"angA: '{Vector3.SignedAngle(v_lnAStart_to_lnAEnd, v_lineAStart_to_pos, nrml)}', angB: '{Vector3.SignedAngle(v_lnBStart_to_lnBEnd, v_lineBStart_to_pos, nrml)}'");
				}
				dbgRprt += $"vA: '{v_lnAStart_to_lnAEnd}', vB: '{v_lnBStart_to_lnBEnd}', " +
					$"angA: '{Vector3.SignedAngle(v_lnAStart_to_lnAEnd, v_lineAStart_to_pos, nrml)}', angB: '{Vector3.SignedAngle(v_lnBStart_to_lnBEnd, v_lineBStart_to_pos, nrml)}'\n";

				//Vector3 v_offset = Vector3.up * 0.2f;
				//Debug.DrawLine(lineAStart + v_offset, lineAEnd + v_offset);
				//Debug.DrawLine(lineBStart + v_offset, lineBEnd + v_offset);

				dbgRprt = $"Angle1: '{Vector3.SignedAngle(v_lnAStart_to_lnAEnd, v_lineAStart_to_pos, nrml)}'\n" +
				$"Angle2: '{Vector3.SignedAngle(v_lnBStart_to_lnBEnd, v_lineBStart_to_pos, nrml)}'\n";

				if(dbgIt)
				{
					Debug.Log($"Angle1: '{Vector3.SignedAngle(v_lnAStart_to_lnAEnd, v_lineAStart_to_pos, nrml)}'\n" +
						$"Angle2: '{Vector3.SignedAngle(v_lnBStart_to_lnBEnd, v_lineBStart_to_pos, nrml)}'");
				}

				if
				(
					Mathf.Sign(Vector3.SignedAngle(v_lnAStart_to_lnAEnd, v_lineAStart_to_pos, nrml)) !=
					Mathf.Sign(Vector3.SignedAngle(v_lnBStart_to_lnBEnd, v_lineBStart_to_pos, nrml))
				)
				{
					if(dbgIt) Debug.Log("returning true...");
					dbgRprt += "returning true...\n";
					return true;
				}
				else
				{
					if (dbgIt) Debug.Log("returning false...");
					dbgRprt += "returning false...\n";

					return false;
				}
			}
		}

		#region MATH OPERATIONS --------------------------------------
		public static float CalculateTriangleEdgeLength( float angA, float angB, float lenB )
		{
			return Mathf.Sin(angA * Mathf.Deg2Rad) * lenB / Mathf.Sin(angB * Mathf.Deg2Rad);
		}

		public static float SignedAngleToFullDegrees( float angleAmount )
		{
			if( angleAmount < 0f )
			{
				return 180f + (180f - Mathf.Abs(angleAmount));
			}

			return angleAmount;
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

		//NOte: The following "special" methods are methods that I don't really want to put in the object classes
		// because it would feel like I'm cluttering/enlarging them with methods I don't commonly need in those classes
		#region SPECIAL TRIANGLE METHODS ------------------------
		/// <summary>
		/// Gets the edge on a triangle that appears the widest from the supplied perspective.
		/// </summary>
		/// <returns></returns>
		public static LNX_Edge GetWidestEdgeFromPerspective( Vector3 vPerspective, LNX_Triangle triangle )
		{
			float runningWidestAngle = Vector3.Angle(
				triangle.Verts[triangle.Edges[0].StartVertIndex].V_flattenedPosition - FlatVector(vPerspective,triangle.v_SurfaceNormal_cached),
				triangle.Verts[triangle.Edges[0].EndVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.v_SurfaceNormal_cached)
			);
			int runningWidestEdge = 0;

			if
			(
				Vector3.Angle(
				triangle.Verts[triangle.Edges[1].StartVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.v_SurfaceNormal_cached),
				triangle.Verts[triangle.Edges[1].EndVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.v_SurfaceNormal_cached)
				) > runningWidestAngle
			)
			{
				runningWidestAngle = Vector3.Angle(
				triangle.Verts[triangle.Edges[1].StartVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.v_SurfaceNormal_cached),
				triangle.Verts[triangle.Edges[1].EndVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.v_SurfaceNormal_cached)
				);
				runningWidestEdge = 1;
			}

			if
			(
				Vector3.Angle(
				triangle.Verts[triangle.Edges[2].StartVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.v_SurfaceNormal_cached),
				triangle.Verts[triangle.Edges[2].EndVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.v_SurfaceNormal_cached)
				) > runningWidestAngle
			)
			{
				runningWidestAngle = Vector3.Angle(
				triangle.Verts[triangle.Edges[2].StartVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.v_SurfaceNormal_cached),
				triangle.Verts[triangle.Edges[2].EndVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.v_SurfaceNormal_cached)
				);
				runningWidestEdge = 2;
			}

			return triangle.Edges[runningWidestEdge];
		}

		/// <summary>
		/// Checks if any point on a given edge lies in the path between two triangles.
		/// </summary>
		/// <param name="nm"></param>
		/// <param name="obstructEdge"></param>
		/// <param name="triIndxA"></param>
		/// <param name="triIndxB"></param>
		/// <returns></returns>
		public static bool DoesEdgeObstructTriPath(LNX_NavMesh nm, LNX_Edge obstructEdge, int triIndxA, int triIndxB, ref string dbgRprt )
		{
			dbgRprt = $"{nameof(DoesEdgeObstructTriPath)}() Report---------------\n" +
				$"params--------\n" +
				$"{nameof(obstructEdge)}: '{obstructEdge.ToString()}', midPos: '{obstructEdge.MidPosition}' (flt: '{obstructEdge.MidPosition_flat}')\n" +
				$"{nameof(triIndxA)}: '{triIndxA}' (ctr: {nm.Triangles[triIndxA].V_Center}), {nameof(triIndxB)}: '{triIndxB}' (ctr: {nm.Triangles[triIndxB].V_Center})\n" +
				$"nm surface orientation: '{nm.V_SurfaceOrientation}'\n" +
				$"--\n\n";

			if ( triIndxA == triIndxB )
			{
				dbgRprt += $"LNX Error! supplied triangle indices were the same. Returning early...\n";
				Debug.LogError($"LNX Error! supplied triangle indices were the same. Returning early...");
				return false;
			}

			Vector3 v_midPt = (nm.Triangles[triIndxA].V_Center + nm.Triangles[triIndxB].V_Center) / 2f; //necessary to obtain the correct edges...
			//dbgRprt += $"midpoint between triangles calculated to be: '{v_midPt}'...\n";
			//Debug.DrawLine(v_midPt, v_midPt + (Vector3.up * 5f));
			//Debug.DrawLine(obstructEdge.MidPosition, obstructEdge.MidPosition + (Vector3.up * 2f));

			LNX_Edge edgeA = GetWidestEdgeFromPerspective(v_midPt, nm.Triangles[triIndxA]);
			LNX_Edge edgeB = GetWidestEdgeFromPerspective(v_midPt, nm.Triangles[triIndxB]);

			//Debug.DrawLine(edgeA.StartPosition, edgeA.StartPosition + (Vector3.up * 1f));
			//Debug.DrawLine(edgeA.EndPosition, edgeA.EndPosition + (Vector3.up * 1f));
			//Debug.DrawLine(edgeB.StartPosition, edgeB.StartPosition + (Vector3.up * 1f));
			//Debug.DrawLine(edgeB.EndPosition, edgeB.EndPosition + (Vector3.up * 1f));

			dbgRprt += $"Chose widestEdgeA: '{edgeA}', strt: '{edgeA.StartPosition}', end: '{edgeA.EndPosition}', mid: '{edgeA.MidPosition}'\n" +
				$"Chose widestEdgeB: '{edgeB}, strt: '{edgeB.StartPosition}', end: '{edgeB.EndPosition}', mid: '{edgeB.MidPosition}'\n" +
				$"Obstructedge strt: '{obstructEdge.StartPosition}, end: '{obstructEdge.EndPosition}'...\n\n";

			Vector3 v_startToStart = Vector3.Normalize(FlatVector(edgeB.StartPosition, nm.V_SurfaceOrientation) - FlatVector(edgeA.StartPosition, nm.V_SurfaceOrientation));
			Vector3 v_startToEnd = Vector3.Normalize(FlatVector(edgeB.EndPosition, nm.V_SurfaceOrientation) - FlatVector(edgeA.StartPosition, nm.V_SurfaceOrientation));
			Vector3 v_endToStart = Vector3.Normalize(FlatVector(edgeB.StartPosition, nm.V_SurfaceOrientation) - FlatVector(edgeA.EndPosition, nm.V_SurfaceOrientation));
			Vector3 v_endToEnd = Vector3.Normalize(FlatVector(edgeB.EndPosition, nm.V_SurfaceOrientation) - FlatVector(edgeA.EndPosition, nm.V_SurfaceOrientation));

			float alignmentA = Vector3.Dot(v_startToStart, v_endToEnd);
			float alignmentB = Vector3.Dot(v_startToEnd, v_endToStart);

			//Vector3 v_startToPos = Vector3.Normalize(FlatVector(pos, nm.V_SurfaceOrientation) - FlatVector(edgeA.StartPosition, nm.V_SurfaceOrientation)); //why did I make this?
			//Vector3 v_endToPos = Vector3.Normalize(FlatVector(pos, nm.V_SurfaceOrientation) - FlatVector(edgeA.EndPosition, nm.V_SurfaceOrientation)); //why did I make this?

			#region CHECK IF ANY OF THE EDGE POINTS ARE DISCRETELY WITHIN THE PROJECTION---------------------------------
			//The following if-check determines which vector-set is in better alignment. This set will be the vector-set 
			//that will be on the "outside", and therefore the correct one to use...

			if ( alignmentA > alignmentB ) //this means the start and end positions of the edges "line up", so to speak.
			{
				dbgRprt += $"using first alignment\n";

				string s = "";
				dbgRprt += $"previewing obstructEdge midpoint at: '{obstructEdge.MidPosition}'\n" +
					$"cndtn1: '{AmBetweenConcurrentLines(obstructEdge.MidPosition,		  edgeA.StartPosition_flat, edgeB.StartPosition_flat, edgeA.EndPosition_flat, edgeB.EndPosition_flat, nm.V_SurfaceOrientation, ref s)}'\n" +
					$"cndtn2: '{AmBetweenConcurrentLines(obstructEdge.MidPosition,		  edgeA.StartPosition_flat, edgeA.EndPosition_flat, edgeB.StartPosition_flat, edgeB.EndPosition_flat, nm.V_SurfaceOrientation, ref s)}'\n" +
					$"";																																															 
																																																					 
				dbgRprt += $"previewing obstructedge start at: '{obstructEdge.StartPosition_flat}'\n" +																														 
					$"cndtn1: '{AmBetweenConcurrentLines(obstructEdge.StartPosition_flat, edgeA.StartPosition_flat, edgeB.StartPosition_flat, edgeA.EndPosition_flat, edgeB.EndPosition_flat, nm.V_SurfaceOrientation, ref s)}'\n" +
					$"cndtn2: '{AmBetweenConcurrentLines(obstructEdge.StartPosition_flat, edgeA.StartPosition_flat, edgeA.EndPosition_flat, edgeB.StartPosition_flat, edgeB.EndPosition_flat, nm.V_SurfaceOrientation, ref s)}'\n" +
					$"";																																															 
																																																					 
				dbgRprt += $"previewing obstruct edge end at: '{obstructEdge.EndPosition}'\n" +																																 
					$"cndtn1: '{AmBetweenConcurrentLines(obstructEdge.EndPosition,		  edgeA.StartPosition_flat, edgeB.StartPosition_flat, edgeA.EndPosition_flat, edgeB.EndPosition_flat, nm.V_SurfaceOrientation, ref s)}'\n" +
					$"cndtn2: '{AmBetweenConcurrentLines(obstructEdge.EndPosition,		  edgeA.StartPosition_flat, edgeA.EndPosition_flat, edgeB.StartPosition_flat, edgeB.EndPosition_flat, nm.V_SurfaceOrientation, ref s)}'\n" +
					$"";

				dbgRprt += ("\nnow actually doing it----------------//////////////\n");
				if //check mid point...
				(
					AmBetweenConcurrentLines //checks that it's between vectors going from edge to edge
					(
						obstructEdge.MidPosition, 
						edgeA.StartPosition_flat, edgeB.StartPosition_flat, 
						edgeA.EndPosition_flat, edgeB.EndPosition_flat, 
						nm.V_SurfaceOrientation, ref dbgRprt
					) &&
					AmBetweenConcurrentLines //checks that it's between the edges
					(
						obstructEdge.MidPosition, 
						edgeA.StartPosition_flat, edgeA.EndPosition_flat, 
						edgeB.StartPosition_flat, edgeB.EndPosition_flat, 
						nm.V_SurfaceOrientation, ref dbgRprt
					)
				)
				{
					dbgRprt += $"midpoint check passed. Returning true...";
					Debug.Log(dbgRprt);
					return true;
				}
				else if //check start point...
				(
					AmBetweenConcurrentLines //checks that it's between vectors going from edge to edge
					(
						obstructEdge.StartPosition_flat, 
						edgeA.StartPosition_flat, edgeB.StartPosition_flat, 
						edgeA.EndPosition_flat, edgeB.EndPosition_flat, 
						nm.V_SurfaceOrientation, ref dbgRprt
					) &&
					AmBetweenConcurrentLines //checks that it's between the edges
					(
						obstructEdge.StartPosition_flat, 
						edgeA.StartPosition_flat, edgeA.EndPosition_flat, 
						edgeB.StartPosition_flat, edgeB.EndPosition_flat, 
						nm.V_SurfaceOrientation, ref dbgRprt
					)
				)
				{
					dbgRprt += $"start point check passed. Returning true...";
					Debug.Log(dbgRprt);
					return true;
				}
				else if //check end point...
				(
					AmBetweenConcurrentLines //checks that it's between vectors going from edge to edge
					(
						obstructEdge.EndPosition, 
						edgeA.StartPosition_flat, edgeB.StartPosition_flat, 
						edgeA.EndPosition_flat, edgeB.EndPosition_flat, 
						nm.V_SurfaceOrientation, ref dbgRprt
					) &&
					AmBetweenConcurrentLines //checks that it's between the edges
					(
						obstructEdge.EndPosition, 
						edgeA.StartPosition_flat, edgeA.EndPosition_flat, 
						edgeB.StartPosition_flat, edgeB.EndPosition_flat, 
						nm.V_SurfaceOrientation, ref dbgRprt
					)
				)
				{
					dbgRprt += $"end point check passed. Returning true...";
					Debug.Log(dbgRprt);
					return true;
				}
			}
			else //This means that the starts and ends do NOT line up...
			{
				dbgRprt += $"using second alignment\n";
				string s = "";

				dbgRprt +=	$"checking midpoint at: '{obstructEdge.MidPosition}'\n" +
					$"cndtn1: '{AmBetweenConcurrentLines(obstructEdge.MidPosition,			edgeA.StartPosition_flat, edgeB.EndPosition_flat, edgeA.EndPosition_flat, edgeB.StartPosition_flat, nm.V_SurfaceOrientation, ref s)}'\n" +
					$"cndtn2: '{AmBetweenConcurrentLines(obstructEdge.MidPosition,			edgeA.StartPosition_flat, edgeA.EndPosition_flat, edgeB.EndPosition_flat, edgeB.StartPosition_flat, nm.V_SurfaceOrientation, ref s)}'\n" +
					$"";																																									   						   		 
																																															   						   		 
				dbgRprt += $"checking obstruct edge start at: '{obstructEdge.StartPosition_flat}'\n" +																						   						   		 
					$"cndtn1: '{AmBetweenConcurrentLines(obstructEdge.StartPosition_flat,	edgeA.StartPosition_flat, edgeB.EndPosition_flat, edgeA.EndPosition_flat, edgeB.StartPosition_flat, nm.V_SurfaceOrientation, ref s)}'\n" +
					$"cndtn2: '{AmBetweenConcurrentLines(obstructEdge.StartPosition_flat,	edgeA.StartPosition_flat, edgeA.EndPosition_flat, edgeB.EndPosition_flat, edgeB.StartPosition_flat, nm.V_SurfaceOrientation, ref s)}'\n" +
					$"";																																															   		
																																																					   		
				dbgRprt += $"checking obstruct edge end at: '{obstructEdge.EndPosition}'\n" +																														   		
					$"cndtn1: '{AmBetweenConcurrentLines(obstructEdge.EndPosition,			edgeA.StartPosition_flat, edgeB.EndPosition_flat, edgeA.EndPosition_flat, edgeB.StartPosition_flat, nm.V_SurfaceOrientation, ref s)}'\n" +
					$"cndtn2: '{AmBetweenConcurrentLines(obstructEdge.EndPosition,			edgeA.StartPosition_flat, edgeA.EndPosition_flat, edgeB.EndPosition_flat, edgeB.StartPosition_flat, nm.V_SurfaceOrientation, ref s)}'\n" +
					$"";

				if //check mid point...
				(
					AmBetweenConcurrentLines //checks that it's between vectors going from edge to edge
					(
						obstructEdge.MidPosition,
						edgeA.StartPosition_flat, edgeB.EndPosition_flat,
						edgeA.EndPosition_flat, edgeB.StartPosition_flat,
						nm.V_SurfaceOrientation, ref dbgRprt
					) &&
					AmBetweenConcurrentLines //checks that it's between the edges
					(
						obstructEdge.MidPosition,
						edgeA.StartPosition_flat, edgeA.EndPosition_flat,
						edgeB.EndPosition_flat, edgeB.StartPosition_flat,
						nm.V_SurfaceOrientation, ref dbgRprt
					)
				)
				{
					dbgRprt += $"midpoint check passed. Returning true...";
					Debug.Log(dbgRprt);

					return true;
				}
				else if //check start point...
				(
					AmBetweenConcurrentLines //checks that it's between vectors going from edge to edge
					(
						obstructEdge.StartPosition_flat,
						edgeA.StartPosition_flat, edgeB.EndPosition_flat,
						edgeA.EndPosition_flat, edgeB.StartPosition_flat,
						nm.V_SurfaceOrientation, ref dbgRprt
					) &&
					AmBetweenConcurrentLines //checks that it's between the edges
					(
						obstructEdge.StartPosition_flat,
						edgeA.StartPosition_flat, edgeA.EndPosition_flat,
						edgeB.EndPosition_flat, edgeB.StartPosition_flat,
						nm.V_SurfaceOrientation, ref dbgRprt
					)
				)
				{
					dbgRprt += $"start point check passed. Returning true...";
					Debug.Log(dbgRprt);

					return true;
				}
				else if //check end point...
				(
					AmBetweenConcurrentLines //checks that it's between vectors going from edge to edge
					(
						obstructEdge.EndPosition,
						edgeA.StartPosition_flat, edgeB.EndPosition_flat,
						edgeA.EndPosition_flat, edgeB.StartPosition_flat,
						nm.V_SurfaceOrientation, ref dbgRprt
					) &&
					AmBetweenConcurrentLines //checks that it's between the edges
					(
						obstructEdge.EndPosition,
						edgeA.StartPosition_flat, edgeA.EndPosition_flat,
						edgeB.EndPosition_flat, edgeB.StartPosition_flat,
						nm.V_SurfaceOrientation, ref dbgRprt
					)
				)
				{
					dbgRprt += $"end point check passed. Returning true...";
					Debug.Log(dbgRprt);

					return true;
				}
			}

			#endregion

			dbgRprt += $"\n\nNow checking if the obstructing edge is wide enough to encompass...\n";
			#region CHECK IF OBSTRUCT EDGE IS SO WIDE THAT IT ENCOMPASSES THE ENTIRE PROJECTION-----------
			Vector3 v_edgeAStart_to_obstrctEdgeStrt = Vector3.Normalize( obstructEdge.StartPosition_flat - edgeA.StartPosition_flat );
			Vector3 v_edgeAEnd_to_obstrctEdgeEnd = Vector3.Normalize( obstructEdge.EndPosition_flat - edgeA.EndPosition_flat );
			Vector3 v_edgeAMid_toObstructEdgeMid = Vector3.Normalize( obstructEdge.MidPosition_flat - edgeA.MidPosition_flat );
			dbgRprt += $"v_edgeAStart_to_obstrctEdgeStrt: '{LNX_UnitTestUtilities.LongVectorString(v_edgeAStart_to_obstrctEdgeStrt)}'\n" +
				$"v_edgeAEnd_to_obstrctEdgeEnd: '{LNX_UnitTestUtilities.LongVectorString(v_edgeAEnd_to_obstrctEdgeEnd)}'\n" +
				$"v_edgeAMid_toObstructEdgeMid: '{LNX_UnitTestUtilities.LongVectorString(v_edgeAMid_toObstructEdgeMid)}'\n" +
				$"";

			dbgRprt += $"previewing...\n" +
				$"cndtn1: '{Vector3.SignedAngle(v_edgeAMid_toObstructEdgeMid, v_edgeAStart_to_obstrctEdgeStrt, nm.V_SurfaceOrientation)}'\n" + //TODO: this is returning 0...
				$"cndtn2: '{Vector3.SignedAngle(v_edgeAMid_toObstructEdgeMid, v_edgeAEnd_to_obstrctEdgeEnd, nm.V_SurfaceOrientation)}'\n";

			if( 
				Mathf.Sign(Vector3.SignedAngle(v_edgeAMid_toObstructEdgeMid, v_edgeAStart_to_obstrctEdgeStrt, nm.V_SurfaceOrientation)) !=
				Mathf.Sign(Vector3.SignedAngle(v_edgeAMid_toObstructEdgeMid, v_edgeAEnd_to_obstrctEdgeEnd, nm.V_SurfaceOrientation))
			)
			{
				dbgRprt += $"Decided obstruct edge encompassed entire projection. Returning true...";
				if (triIndxB == 39) Debug.Log(dbgRprt);

				return true;
			}
			#endregion

			if (triIndxB == 39) Debug.Log(dbgRprt);
			return false;
		}
		#endregion

		#region SPECIAL EDGE METHODS --------------------

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
}