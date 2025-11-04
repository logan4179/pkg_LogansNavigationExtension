using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Data.Common;
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

		public static bool AmInVectorCone(Vector3 vToPos, Vector3 vLegA, Vector3 vLegB, Vector3 nrml, ref string dbgString, bool includeOnPerim = false )
		{
			dbgString = $"AmInVectorCone({vToPos})\n"; //todo: need to implement this in probably several methods that are currently doing their own thing

			#region SHORT-CIRCUITING ==========================================
			if ( includeOnPerim && (vToPos == vLegA || vToPos == vLegB) )
			{
				dbgString += $"was told to include perim, and I found that pos is on perim. Returning true...\n";
				return true;
			}

			if( vLegA == -vLegB )
			{
				return true; //because the "sweep cone" in this case would be a full 180 degrees, and it wouldn't matter which side.
				//todo: Maybe I should actually log a warning here?
			}

			if( vLegA == vLegB )
			{
				return false;
			}
			#endregion
			float ang_crnr = Vector3.SignedAngle( vLegA, vLegB, nrml );
			//float ang_crnr = Vector3.Angle(vLegA, vLegB);

			float ang_legAToPos = Vector3.SignedAngle( vToPos, vLegA, nrml );
			float ang_legBToPos = Vector3.SignedAngle( vToPos, vLegB, nrml );

			dbgString += $"corner angle: '{ang_crnr}'\n";

			dbgString += $"calc preview...\n" +
				$"c1: '{ang_legAToPos}'\n" +
				$"c2: '{ang_legBToPos}'\n";

			Vector3 v_btwn = FlatVector( (vLegA + vLegB) / 2f ).normalized;
			/*dbgString += $"\nDot prod check...\n" +
				$"d1: '{}'\n" +
				$"";*/
			if
			( 
				Mathf.Sign(ang_crnr) != Mathf.Sign(ang_legAToPos) &&
				Mathf.Sign(ang_crnr) == Mathf.Sign(ang_legBToPos)
			)
			{
				return true;
			}

			/*
			if( Mathf.Abs(ang_legAToPos) > ang_crnr || Mathf.Abs(ang_legBToPos) > ang_crnr )
			{
				dbgString += $"return false...\n";
				return false;
			}


			dbgString += $"return true...\n";
			return true;
			*/
			return false;
		}

		/// <summary>
		/// Used to check whether any part of an edge is in the cone formed by a vertex
		/// </summary>
		/// <param name="edge"></param>
		/// <param name="vert"></param>
		/// <param name="nrml"></param>
		/// <param name="dbgString"></param>
		/// <param name="includeOnPerim"></param>
		/// <returns></returns>
		public static bool AmInVertexCone(LNX_Edge edge, LNX_Vertex vert, Vector3 nrml, ref string dbgString, bool includeOnPerim = false)
		{
			dbgString = $"AmInVectorCone({edge})\n" +
				$"";

			dbgString += $"condition preview...\n" +
				$"c1: '{vert.IsInCenterSweep(edge.StartPosition_flat, vert.CachedSurfaceNormal)}'\n" +
				$"c2: '{vert.IsInCenterSweep(edge.EndPosition_flat, vert.CachedSurfaceNormal)}'\n";

			if
			(
				vert.IsInCenterSweep(edge.MidPosition_flat, nrml) ||
				vert.IsInCenterSweep(edge.StartPosition_flat, nrml) ||
				vert.IsInCenterSweep(edge.EndPosition_flat, nrml)
			)
			{
				return true;
			}

			#region Now check if obstructEdge is big enough to encompass entire Cone path===================================	
			dbgString += $"None of the discrete edge points are in center sweep.\n\n" +
				$"Checking if edge encompases sweep...\n";

			string rprtC1 = "";
			string rprtC2 = "";

			if
			(
				AmInVectorCone
				(
					FlatVector(vert.V_ToFirstSiblingVert),
					FlatVector(edge.StartPosition_flat - vert.V_flattenedPosition).normalized,
					FlatVector(edge.EndPosition_flat - vert.V_flattenedPosition).normalized,
					nrml, ref rprtC1
				) &&
				AmInVectorCone
				(
					FlatVector(vert.V_ToSecondSiblingVert),
					FlatVector(edge.StartPosition_flat - vert.V_flattenedPosition).normalized,
					FlatVector(edge.EndPosition_flat - vert.V_flattenedPosition).normalized,
					nrml, ref rprtC1
				)
			)
			{
				dbgString += $"decided edge DOES encompas entire sweep! rprt:\n" +
				$"c1: '{rprtC1}'\n" +
				$"c2: '{rprtC2}'\n";

				return true;
			}
			#endregion
			dbgString += $"decided edge does NOT encompas entire sweep! Rprt: \n" +
				$"c1: '{rprtC1}'\n" +
				$"c2: '{rprtC2}'\n";


			return false;
		}

		public static bool AmCompletelyInVertexCone(LNX_Edge edge, LNX_Vertex vert, Vector3 nrml, ref string dbgString, bool includeOnPerim = false)
		{
			if
			(
				vert.IsInCenterSweep(edge.StartPosition_flat, nrml) &&
				vert.IsInCenterSweep(edge.EndPosition_flat, nrml)
			)
			{
				return true;
			}

			return false;
		}

		public static bool EdgeEncompassesVertSweep(LNX_Edge edge, LNX_Vertex vert, Vector3 nrml, ref string dbgString)
		{
			dbgString = $"{nameof(EdgeEncompassesVertSweep)}()\n";

			Vector3 vpos = vert.V_flattenedPosition; //caching it so I don't have to keep calling the property logic...
			//float vrtSignedAngle = vert.SignedAngle; //caching it so I don't have to keep calling the property logic...

			Vector3 v_vrtToEdgeStart = FlatVector( edge.StartPosition_flat - vpos).normalized;
			Vector3 v_vrtToEdgeEnd = FlatVector( edge.EndPosition_flat - vpos).normalized;

			dbgString += $"vert signed angle: '{vert.AngleAtBend}'\n";

			#region SHORT-CIRCUITING ====================================
			if( edge.TriangleIndex == vert.TriangleIndex && edge.StartVertCoordinate != vert.MyCoordinate && edge.EndVertCoordinate != vert.MyCoordinate )
			{
				dbgString += $"ss1\n";
				return true;
			}

			//todo: might want a short-circuit that detects if the edge is on verts sharing space with the other verts on the triangle that vert is part of

			if ( Vector3.Angle(v_vrtToEdgeStart, v_vrtToEdgeEnd) < vert.AngleAtBend) //todo: bring this back when I'm satisfied testing the rest. Commented this out so that the following stuff could be tested...
			{
				return false; //can return false right away
			}

			if
			( 
				(v_vrtToEdgeStart == vert.V_ToFirstSiblingVert_flat && v_vrtToEdgeEnd == vert.V_ToSecondSiblingVert_flat) ||
				(v_vrtToEdgeStart == vert.V_ToSecondSiblingVert_flat && v_vrtToEdgeEnd == vert.V_ToFirstSiblingVert_flat)
			)
			{
				dbgString += $"ss2\n";

				return true;
			}

			if
			( 
				Vector3.Dot
				(
					(vert.V_ToFirstSiblingVert_flat + vert.V_ToSecondSiblingVert_flat) / 2f, FlatVector(edge.MidPosition_flat - vpos).normalized
				) < 0f 
			)
			{
				dbgString += $"ss3\n";

				return false; //this would mean the vert legs aren't facing the edge enough...
			}
			#endregion

			string dbgAVEEPA = "";

			if
			( 
				(
					Mathf.Sign(Vector3.SignedAngle(v_vrtToEdgeStart, vert.V_ToFirstSiblingVert_flat, nrml)) ==
					Mathf.Sign(Vector3.SignedAngle(v_vrtToEdgeStart, vert.V_ToSecondSiblingVert_flat, nrml))
				) && 
				(
					Mathf.Sign(Vector3.SignedAngle(v_vrtToEdgeEnd, vert.V_ToFirstSiblingVert_flat, nrml)) ==
					Mathf.Sign(Vector3.SignedAngle(v_vrtToEdgeEnd, vert.V_ToSecondSiblingVert_flat, nrml))
				)
				&&
				(
					Mathf.Sign(Vector3.SignedAngle(vert.V_ToFirstSiblingVert_flat, v_vrtToEdgeStart, nrml)) !=
					Mathf.Sign(Vector3.SignedAngle(vert.V_ToFirstSiblingVert_flat, v_vrtToEdgeEnd, nrml))
				) &&
				(
					Mathf.Sign(Vector3.SignedAngle(v_vrtToEdgeStart, vert.V_ToFirstSiblingVert_flat, nrml)) !=
					Mathf.Sign(Vector3.SignedAngle(v_vrtToEdgeEnd, vert.V_ToFirstSiblingVert_flat, nrml))
				)
			)
			{
				return true;
			}

				return false;
		}

		public static bool PositionIsBetweenTriPoints( Vector3 pos, Vector3 triPointA, Vector3 triPointB, Vector3 triPointC, Vector3 nrml/*, ref string dbgString*/ )
		{
			/*dbgString = $"{nameof(PositionIsBetweenTriPoints)}({pos}', \n" +
				$"'{triPointA}', '{triPointB}', '{triPointC}')\n" +
				$"'{nrml}'\n";*/

			Vector3 v_a_to_b = Vector3.Normalize( FlatVector(triPointB,nrml) - FlatVector(triPointA,nrml) );
			Vector3 v_a_to_c = Vector3.Normalize( FlatVector(triPointC, nrml) - FlatVector(triPointA, nrml) );
			Vector3 v_ptA_to_pos = Vector3.Normalize( FlatVector(pos,nrml) - FlatVector(triPointA,nrml) );

			float crnrAngle = Vector3.Angle( v_a_to_b, v_a_to_c ) /*+ 0.001f*/;
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
				crnrAngle = Vector3.Angle(v_b_toA, v_b_to_c) /*+ 0.001f*/;
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
			dbgRprt += $"{nameof(AmBetweenConcurrentLines)}({pos}, {lineAStart}, {lineAEnd}, {lineBStart}, {lineBEnd}) rprt...\n";

			if( lineAStart == lineAEnd && lineBStart == lineBEnd )
			{
				dbgRprt += $"LNX ERROR! You supplied '{nameof(AmBetweenConcurrentLines)}'() with two lines with the same start and end points.\n";
				return false;
			}

			if ( lineAStart == lineAEnd )
			{
				dbgRprt += "chose lineA start and end equal to block. Now turning to tri point check...\n" +
					$"{nameof(PositionIsBetweenTriPoints)}()\n" +
					$"returning: '{PositionIsBetweenTriPoints(pos, lineAStart, lineBStart, lineBEnd, nrml/*, ref dbgit*/ )}'\n";

				return PositionIsBetweenTriPoints( pos, lineAStart, lineBStart, lineBEnd, nrml/*, ref dbgit*/ );

			}
			if ( lineBStart == lineBEnd )
			{
				dbgRprt += "chose lineB start and end equal to block. Now turning to tri point check...\n" +
					$"{nameof(PositionIsBetweenTriPoints)}() rslt: '{PositionIsBetweenTriPoints(pos, lineAStart, lineAEnd, lineBStart, nrml/*, ref dbgit*/)}'\n";

				return PositionIsBetweenTriPoints( pos, lineAStart, lineAEnd, lineBStart, nrml/*, ref dbgit*/);
			}

			//This would be expected most of the time because usually you're not going to pass in points that are equal...
			Vector3 v_lnAStart_to_lnAEnd = Vector3.Normalize(FlatVector(lineAEnd, nrml) - FlatVector(lineAStart, nrml));
			Vector3 v_lnBStart_to_lnBEnd = Vector3.Normalize(FlatVector(lineBEnd, nrml) - FlatVector(lineBStart, nrml));
			Vector3 v_lineAStart_to_pos = Vector3.Normalize(FlatVector(pos, nrml) - FlatVector(lineAStart, nrml));
			Vector3 v_lineBStart_to_pos = Vector3.Normalize(FlatVector(pos, nrml) - FlatVector(lineBStart, nrml));

			dbgRprt += $"vA: '{v_lnAStart_to_lnAEnd}', vB: '{v_lnBStart_to_lnBEnd}', " +
				$"angA: '{Vector3.SignedAngle(v_lnAStart_to_lnAEnd, v_lineAStart_to_pos, nrml)}', angB: '{Vector3.SignedAngle(v_lnBStart_to_lnBEnd, v_lineBStart_to_pos, nrml)}'\n";

			//Vector3 v_offset = Vector3.up * 0.2f;
			//Debug.DrawLine(lineAStart + v_offset, lineAEnd + v_offset);
			//Debug.DrawLine(lineBStart + v_offset, lineBEnd + v_offset);

			dbgRprt += $"Angle1: '{Vector3.SignedAngle(v_lnAStart_to_lnAEnd, v_lineAStart_to_pos, nrml)}'\n" +
			$"Angle2: '{Vector3.SignedAngle(v_lnBStart_to_lnBEnd, v_lineBStart_to_pos, nrml)}'\n";

			if
			(
				Mathf.Sign(Vector3.SignedAngle(v_lnAStart_to_lnAEnd, v_lineAStart_to_pos, nrml)) !=
				Mathf.Sign(Vector3.SignedAngle(v_lnBStart_to_lnBEnd, v_lineBStart_to_pos, nrml))
			)
			{
				dbgRprt += "returning true...\n";
				return true;
			}
			else
			{
				dbgRprt += "returning false...\n";

				return false;
			}
		}

		/// <summary>
		/// Determines if a supplied position is in an area defined by 4 points. NOTE: The 4 points MUST run "clockwise" with respect to each other.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="crnrA"></param>
		/// <param name="crnrB"></param>
		/// <param name="crnrC"></param>
		/// <param name="crnrD"></param>
		/// <param name="nrml"></param>
		/// <param name="dbgRprt"></param>
		/// <returns></returns>
		public static bool AmInQuadArea( Vector3 pos, Vector3 crnrA, Vector3 crnrB, Vector3 crnrC, Vector3 crnrD, Vector3 nrml, bool includeOnBorder, ref string dbgRprt )
		{
			dbgRprt = $"{nameof(AmInQuadArea)}('{pos}'\n" +
				$"cA: '{crnrA}', cB: '{crnrB}', cC: '{crnrC}', cD: '{crnrD}')\n";

			Vector3 pos_fltnd = FlatVector(pos, nrml);

			Vector3 v_aToB = Vector3.Normalize( FlatVector(crnrB,nrml) - FlatVector(crnrA,nrml) );
			Vector3 v_aToD = Vector3.Normalize( FlatVector(crnrD, nrml) - FlatVector(crnrA, nrml) );
			float ang_crnrA = Vector3.Angle( v_aToB, v_aToD );

			Vector3 v_cToB = Vector3.Normalize(FlatVector(crnrB, nrml) - FlatVector(crnrC, nrml));
			Vector3 v_cToD = Vector3.Normalize(FlatVector(crnrD, nrml) - FlatVector(crnrC, nrml));
			float ang_crnrC = Vector3.Angle(v_cToB, v_cToD);

			Vector3 v_aToPos = Vector3.Normalize( pos_fltnd - FlatVector(crnrA,nrml) );
			Vector3 v_cToPos = Vector3.Normalize( pos_fltnd - FlatVector(crnrC, nrml) );

			//Vector3 v_aToMid = Vector3.Normalize( FlatVector((crnrB + crnrD)/2f) - crnrA );
			//Vector3 v_cToMid = Vector3.Normalize(FlatVector((crnrB + crnrD) / 2f) - crnrC);
			dbgRprt += $"using...\n" +
				$"{nameof(ang_crnrA)}: '{ang_crnrA}'. v_aToB: '{v_aToB}', v_aToD: '{v_aToD}'\n" +
				$"{nameof(ang_crnrC)}: '{ang_crnrC}', v_cToB: '{v_cToB}', v_cToD: '{v_cToD}'\n" +
				$"v_aToPos: '{v_aToPos}', v_cToPos: '{v_cToPos}'\n";

			/*
			dbgRprt += $"\n---\n" +
				$"condition preview...\n" +
				$"cndtn1: {ang_crnrA} > '{Vector3.Angle(v_aToB, v_aToPos)}' && \n" +
				$"cndtn2: {ang_crnrA} > '{Vector3.Angle(v_aToD, v_aToPos)}' && \n" +
				$"cndtn3: {ang_crnrC} > '{Vector3.Angle(v_cToB, v_cToPos)}' && \n" +
				$"cndtn4: {ang_crnrC} > '{Vector3.Angle(v_cToD, v_cToPos)}' \n" +
				$"pos: '{Vector3.SignedAngle(v_aToB, v_aToPos, nrml)}'\n" +
				$"pos: '{Vector3.SignedAngle(v_aToD, v_aToPos, nrml)}'\n" +
				$"pos: '{Vector3.SignedAngle(v_cToB, v_cToPos, nrml)}'\n" +
				$"pos: '{Vector3.SignedAngle(v_cToD, v_cToPos, nrml)}'\n" +
				$"";
			*/

			dbgRprt += $"Now running angle check...\n";
			if (includeOnBorder)
			{
				dbgRprt += $"using includeOnBorder block...\n";

				if
				(
					pos == crnrA || pos == crnrB || pos == crnrC || pos == crnrD ||
					v_aToPos == v_aToB || v_aToPos == v_aToD ||
					v_cToPos == v_cToB || v_cToPos == v_cToD
				)
				{
					dbgRprt += $"decided pos was on the border. Returning true...\n";
					return true;
				}
				/*
				if
				(
					Vector3.Angle(v_aToB, v_aToPos) > ang_crnrA ||
					Vector3.Angle(v_aToD, v_aToPos) > ang_crnrA ||

					Vector3.Angle(v_cToB, v_cToPos) > ang_crnrC ||
					Vector3.Angle(v_cToD, v_cToPos) > ang_crnrC
				)
				{
					dbgRprt += $"Returning false...";
					return false;
				}				
				*/

				/*
				if //Note: This didn't work in specific circumstances/angles...
				(
					ang_crnrA >= Vector3.Angle(v_aToB, v_aToPos) &&
					ang_crnrA >= Vector3.Angle(v_aToD, v_aToPos) &&

					ang_crnrC >= Vector3.Angle(v_cToB, v_cToPos) &&
					ang_crnrC >= Vector3.Angle(v_cToD, v_cToPos)
				)
				{
					dbgRprt += $"Returning true...";
					return true;
				}
				*/


				if
				( 
					Vector3.SignedAngle(v_aToB, v_aToPos, nrml) > 0f &&
					Vector3.SignedAngle(v_aToD, v_aToPos, nrml) < 0f &&
					Vector3.SignedAngle(v_cToB, v_cToPos, nrml) < 0f &&
					Vector3.SignedAngle(v_cToD, v_cToPos, nrml) > 0f
				)
				{
					dbgRprt += $"Returning true...";
					return true;
				}

			}
			else
			{
				/*
				if //Note: This didn't work in specific circumstances/angles...
				(
					Vector3.Angle(v_aToB, v_aToPos) < ang_crnrA &&
					Vector3.Angle(v_aToD, v_aToPos) < ang_crnrA &&

					Vector3.Angle(v_cToB, v_cToPos) < ang_crnrC &&
					Vector3.Angle(v_cToD, v_cToPos) < ang_crnrC
				)
				{
					dbgRprt += $"Returning false...";
					return true;
				}
				*/
				if
				(
					Vector3.SignedAngle(v_aToB, v_aToPos, nrml) > 0f &&
					Vector3.SignedAngle(v_aToD, v_aToPos, nrml) < 0f &&
					Vector3.SignedAngle(v_cToB, v_cToPos, nrml) < 0f &&
					Vector3.SignedAngle(v_cToD, v_cToPos, nrml) > 0f
				)
				{
					dbgRprt += $"Returning true...";
					return true;
				}
			}

			dbgRprt += $"Returning true...";

			return false;
		}

		public static bool AmInQuadArea( Vector3 pos, LNX_Edge edgeA, LNX_Edge edgeB, bool includeOnBorder, ref string dbgString )
		{
			if( AreEdgesAlignedFromTheirPerspectives(edgeA,edgeB) )
			{
				return AmInQuadArea(pos, edgeA.StartPosition_flat, edgeA.EndPosition_flat, edgeB.EndPosition_flat, edgeB.StartPosition_flat, edgeA.V_SurfaceNormal_cached, includeOnBorder, ref dbgString );
			}
			else
			{
				return AmInQuadArea(pos, edgeA.StartPosition_flat, edgeA.EndPosition_flat, edgeB.StartPosition_flat, edgeB.EndPosition_flat, edgeA.V_SurfaceNormal_cached, includeOnBorder, ref dbgString);
			}
		}

		public static bool AreVertAndEdgeEndPointsAligned(LNX_Vertex vert, LNX_Edge edge, ref string dbgRprt )
		{
			dbgRprt = $"{nameof(AreVertAndEdgeEndPointsAligned)}()";

			float ang_firstToEnd = Vector3.Angle( vert.V_ToFirstSiblingVert_flat, Vector3.Normalize(edge.EndPosition_flat - vert.V_flattenedPosition) );
			float ang_secondToStart = Vector3.Angle( vert.V_ToSecondSiblingVert_flat, Vector3.Normalize(edge.StartPosition_flat - vert.V_flattenedPosition) );

			float ang_firstToStart = Vector3.Angle( vert.V_ToFirstSiblingVert_flat, Vector3.Normalize(edge.StartPosition_flat - vert.V_flattenedPosition) );
			float ang_secondToEnd = Vector3.Angle( vert.V_ToSecondSiblingVert_flat, Vector3.Normalize(edge.EndPosition_flat - vert.V_flattenedPosition) );

			float runningGreatestAngle = ang_firstToEnd;
			int greatest = 0;
			if(ang_secondToStart > runningGreatestAngle)
			{
				runningGreatestAngle = ang_secondToStart;
				greatest = 1;
			}
			if (ang_firstToStart > runningGreatestAngle)
			{
				runningGreatestAngle = ang_firstToStart;
				greatest = 2;
			}
			if (ang_secondToEnd > runningGreatestAngle)
			{
				runningGreatestAngle = ang_secondToEnd;
				greatest = 3;
			}

			if
			(
				greatest < 2
			)
			{
				dbgRprt += $"returning true...";
				return true;
			}

			dbgRprt += $"returning false...";
			return false;
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
		//todo: could put methods in here to shorten constructing the list of vertices grabbed by various components... idk if it's worth it...
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
		public static LNX_Edge GetWidestEdgeFromPerspective( Vector3 vPerspective, LNX_Triangle triangle, ref string dbgString )
		{
			dbgString = $"GetWidestEdgeFromPerspective(vprsp: '{vPerspective}', tri: '{triangle}')...\n";

			float ang_perspToE0 = Vector3.Angle(
				triangle.Verts[triangle.Edges[0].StartVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.v_SurfaceNormal_cached),
				triangle.Verts[triangle.Edges[0].EndVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.v_SurfaceNormal_cached)
			);
			float runningWidestAngle = ang_perspToE0;
			int runningWidestEdge = 0;

			float ang_perspToE1 = Vector3.Angle(
				triangle.Verts[triangle.Edges[1].StartVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.v_SurfaceNormal_cached),
				triangle.Verts[triangle.Edges[1].EndVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.v_SurfaceNormal_cached)
			);
			float ang_perspToE2 = Vector3.Angle(
				triangle.Verts[triangle.Edges[2].StartVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.v_SurfaceNormal_cached),
				triangle.Verts[triangle.Edges[2].EndVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.v_SurfaceNormal_cached)
			);

			dbgString += $"angle from persp to edge0: '{ang_perspToE0}'\n";
			dbgString += $"angle from persp to edge1: '{ang_perspToE1}'\n";
			dbgString += $"angle from persp to edge2: '{ang_perspToE2}'\n";

			if ( ang_perspToE1 > runningWidestAngle )
			{
				runningWidestAngle = ang_perspToE1;
				runningWidestEdge = 1;
			}

			if ( ang_perspToE2 > runningWidestAngle )
			{
				runningWidestAngle = ang_perspToE2;
				runningWidestEdge = 2;
			}

			//Finally, we need to check that there aren't two angles that are the same that are 
			//actually, I'm going to put this on hold for a sec while I try something else...

			return triangle.Edges[runningWidestEdge];
		}

		public static LNX_Edge GetWidestEdgeFromPerspective( LNX_Edge prspctvEdge, LNX_Triangle otherTri, ref string dbgString)
		{
			dbgString = $"GetWidestEdgeFromPerspective(edge{prspctvEdge.MyCoordinate}, '{otherTri}')\n";// <<<< ERRORTRACE

			dbgString += $"first, checking the 'bridge angle' from prspctvEdge to each edge on otherTri...\n";
			float runningWidestBridgeAngle = 0f;
			int indx_widestEdge = -1;
			for (int i = 0; i < 3; i++)
			{
				dbgString += $"for i: '{i}'...\n";
				float angl = GetEdgeBridgeAngleAmount(prspctvEdge, otherTri.Edges[i]);
				dbgString += $"bridge angle was: '{angl}'...\n";

				if (angl > runningWidestBridgeAngle)
				{
					dbgString += $"new widest angle!\n";
					runningWidestBridgeAngle = angl;
					indx_widestEdge = i;
				}
			}

			dbgString += $"end of loop. Widest edge was edge{indx_widestEdge} with angle: '{runningWidestBridgeAngle}'. Returning '{otherTri.Edges[indx_widestEdge]}'...\n";
			
			return otherTri.Edges[indx_widestEdge];
		}

		public static LNX_Edge GetWidestEdgeFromPerspective( LNX_Triangle perspectiveTri, LNX_Triangle otherTri, ref string dbgString )
		{
			dbgString = $"GetWidestEdgeFromPerspective(prspctvTri: '{perspectiveTri}', othrTri: '{otherTri}')\n";

			LNX_Edge rtrnEdge = null;

			int[] nmbrTms = new int[3] { 0, 0, 0 };
			LNX_ComponentCoordinate[] coords_widestEdges = new LNX_ComponentCoordinate[3];

			dbgString += $"iterating through edges to decide widest perspective edges...\n";
			for( int i_prspctvTriEdges = 0; i_prspctvTriEdges < 3; i_prspctvTriEdges++ )
			{
				dbgString += $"for prspctvTri edge{i_prspctvTriEdges}...\n";
				string s = "";
				LNX_Edge wdstEdgeOnOtherTri = GetWidestEdgeFromPerspective( perspectiveTri.Edges[i_prspctvTriEdges], otherTri, ref s ); //note: this is apparently returning null at a certain case...
				//dbgString += $"\n{s}\n";

				if (wdstEdgeOnOtherTri == null)
				{
					dbgString += $"widest edge from prspctvTri edge{i_prspctvTriEdges} was Null...\n";
					return null;
				}
				else
				{
					dbgString += $"widest edge on otherTri was: '{wdstEdgeOnOtherTri}'. Checking for 2-way agreement...\n";
				}

				LNX_Edge wdstEdge_otherWay = GetWidestEdgeFromPerspective(wdstEdgeOnOtherTri, perspectiveTri, ref s); // <<<< ERRORTRACE
				//dbgString += $"\n{s}\n";

				if ( wdstEdge_otherWay == null )
				{
					dbgString += $"widest edge (other way) was Null...\n";
				}
				else
				{
					dbgString += $"widest edge (other way) was: '{wdstEdge_otherWay}'...\n";
				}

				if (wdstEdge_otherWay == perspectiveTri.Edges[i_prspctvTriEdges] )
				{
					nmbrTms[wdstEdgeOnOtherTri.ComponentIndex]++; //todo: I think that maybe if both edges agree, I can return here...

					dbgString += $"They agree. Incremented to '{nmbrTms[wdstEdgeOnOtherTri.ComponentIndex]}'...\n";

					if(nmbrTms[wdstEdgeOnOtherTri.ComponentIndex] >1 )
					{
						dbgString += $"hit 2 times. Returning this edge...\n";
						return wdstEdgeOnOtherTri;
					}
				}
				else
				{
					dbgString += $"Decided they do NOT agree...\n";
				}
			}

			dbgString += $"finally choosing any edge that was found widest and agreed...\n";
			for( int i = 0; i < 3; i++ )
			{
				if(nmbrTms[i] > 0 )
				{
					dbgString += $"returning {otherTri.Edges[i]}...\n";
					return otherTri.Edges[i];
				}
			}

			dbgString += $"apparently none agreed. returning null...\n";
			///////////////////////////////////////////////////////////////////////////////////
			/*
			dbgString += $"\nNow finding widest edge from 3 verts...\n";

			Vector3 v_midToMid = Vector3.Normalize( otherTri.V_FlattenedCenter - perspectiveTri.V_FlattenedCenter );

			for ( int i = 0; i < 3; i++ )
			{
				dbgString += $"for vert{i}...\n";

				string s = "";
				LNX_Edge wdstEdge = GetWidestEdgeFromPerspective(perspectiveTri.Verts[i].V_flattenedPosition, otherTri, ref s);
				
				if ( wdstEdge == null )
				{
					dbgString += $"return edge was null?...\n";
				}
				else
				{
					dbgString += $"widest edge was: '{wdstEdge}'\n";
				}
				dbgString += $"rprt...\n" +
					$"{s}\n\n";

				nmbrTms[wdstEdge.ComponentIndex]++;

				if(nmbrTms[wdstEdge.ComponentIndex] >= 2 )
				{
					return wdstEdge;
				}
			}

			#region IF WIDEST EDGE WASN'T AGREED ON----------------------------------------------
			dbgString += $"\nCouldn't decide which edge. Checking further...\n";


			#endregion
			*/

			return rtrnEdge;
		}

		/// <summary>
		/// Checks if any point on a given edge lies in the path between two triangles.
		/// </summary>
		/// <param name="nm"></param>
		/// <param name="obstructEdge"></param>
		/// <param name="triIndxA"></param>
		/// <param name="triIndxB"></param>
		/// <returns></returns>
		public static bool DoesEdgeObstructTriPath( LNX_Edge obstructEdge, LNX_Triangle triA, LNX_Triangle triB, ref string dbgRprt )
		{
			dbgRprt = $"{nameof(DoesEdgeObstructTriPath)}() Report---------------\n" +
				$"params--------\n" +
				$"{nameof(obstructEdge)}: '{obstructEdge.ToString()}', midPos: '{obstructEdge.MidPosition}' (flt: '{obstructEdge.MidPosition_flat}')\n" +
				$"{nameof(triA)}: '{triA}', {nameof(triB)}: '{triB}'\n" +
				$"nm surface orientation: '{triA.v_SurfaceNormal_cached}'\n" +
				$"--\n\n";

			#region SHORT-CIRCUITING==========================================
			if (triA == triB)
			{
				dbgRprt += $"LNX Error! supplied triangles were the same. Returning early...\n";
				Debug.LogError($"LNX Error! supplied triangle indices were the same. Returning early...");
				return false;
			}

			if ( obstructEdge.TriangleIndex == triA.Index_inCollection || obstructEdge.TriangleIndex == triB.Index_inCollection )
			{
				dbgRprt += $"LNX Error! ObstructEdge seems to be on one of the supplied triangles. Returning early...\n";
				Debug.LogError($"LNX Error! ObstructEdge seems to be on one of the supplied triangles. Returning early...");
				return false;
			}
			#endregion


			dbgRprt += $"Now getting widest edge from each triangle's perspective...\n";

			string strA = "";
			LNX_Edge edgeA = GetWidestEdgeFromPerspective( triA, triB, ref strA ); //<<<<<
			dbgRprt += $"\n----\n" +
				$"{strA}" +
				$"----\n";

			string strB = "";
			LNX_Edge edgeB = GetWidestEdgeFromPerspective( triB, triA, ref strB );
			dbgRprt += $"\n----\n" +
				$"{strB}" +
				$"----\n\n";

			if (edgeA == null || edgeB == null)
			{
				dbgRprt += $"one of the widest edges were null. Returning early...\n";
				return false;
			}

			dbgRprt += $"Chose widestEdgeA: '{edgeA}', strt: '{edgeA.StartPosition}', end: '{edgeA.EndPosition}', mid: '{edgeA.MidPosition}'\n" +
				$"Chose widestEdgeB: '{edgeB}, strt: '{edgeB.StartPosition}', end: '{edgeB.EndPosition}', mid: '{edgeB.MidPosition}'\n" +
				$"Obstructedge strt: '{obstructEdge.StartPosition}, end: '{obstructEdge.EndPosition}'...\n\n";

			string str = "";
			DoesEdgeObstructEdgePath(obstructEdge, edgeA, edgeB, ref str );

			/*
			#region FIGURE EDGE ALIGNMENTS---------------------------------------
			dbgRprt += $"figuring Dot-check edge alignments for 'edge bridge' vectors...\n";

			bool EdgesAreInalignment = AreEdgesAlignedFromTheirPerspectives(edgeA, edgeB);

			Vector3 bridgeStartA_calculated = edgeA.StartPosition_flat;
			Vector3 bridgeEndA_calculated = edgeB.StartPosition_flat;
			Vector3 bridgeStartB_calculated = edgeA.EndPosition_flat;
			Vector3 bridgeEndB_calculated = edgeB.EndPosition_flat;

			if ( !EdgesAreInalignment )
			{
				dbgRprt += $"using second alignment because the Dot product is greater (more in-line)...\n" +
					$"This means one edge's start and end needs to be flipped for the theoretical 'bridge'...\n";
				bridgeEndA_calculated = edgeB.EndPosition_flat;
				bridgeEndB_calculated = edgeB.StartPosition_flat;
			}
			else
			{
				dbgRprt += $"using first alignment because the Dot product greater (more in-line)...\n" +
						$"This means the two edge's start and ends already line up for the theoretical 'bridge'...\n";
			}

			dbgRprt += $"bridgeStartA: '{bridgeStartA_calculated}', bridgeEndA: '{bridgeEndA_calculated}'\n" +
				$"bridgeStartB: '{bridgeStartB_calculated}', bridgeEndB: '{bridgeEndB_calculated}'\n";
			#endregion

			#region CHECK IF ANY OF THE EDGE POINTS ARE DISCRETELY WITHIN THE PROJECTION---------------------------------
			//The following if-check determines which vector-set is in better alignment. This set will be the vector-set 
			//that will be on the "outside", and therefore the correct one to use...
			dbgRprt += "\nNow checking if obstruct edge points are between edge path...\n";
			string s = "";
			
			dbgRprt += $"previewing obstructEdge midpoint at: '{obstructEdge.MidPosition}'\n" +
				$"cndtn1 (btwnEjs): '{AmBetweenConcurrentLines(obstructEdge.MidPosition, bridgeStartA_calculated, bridgeStartB_calculated, bridgeEndA_calculated, bridgeEndB_calculated, nm.V_SurfaceOrientation, ref s)}'\n" +
				$"cndtn2 (bridge ): '{AmBetweenConcurrentLines(obstructEdge.MidPosition, bridgeStartA_calculated, bridgeEndA_calculated, bridgeStartB_calculated, bridgeEndB_calculated, nm.V_SurfaceOrientation, ref s)}'\n" +
				$"";

			dbgRprt += $"previewing obstructedge start at: '{obstructEdge.StartPosition_flat}'\n" +
				$"cndtn1 (btwnEjs): '{AmBetweenConcurrentLines(obstructEdge.StartPosition_flat, bridgeStartA_calculated, bridgeStartB_calculated, bridgeEndA_calculated, bridgeEndB_calculated,nm.V_SurfaceOrientation, ref s)}'\n" +
				$"cndtn2 (bridge ): '{AmBetweenConcurrentLines(obstructEdge.StartPosition_flat, bridgeStartA_calculated, bridgeEndA_calculated, bridgeStartB_calculated, bridgeEndB_calculated, nm.V_SurfaceOrientation, ref s)}'\n" +
				$"obstrctEdgeStrt is on edgeA: '{edgeA.DoesPositionLieOnEdge(obstructEdge.StartPosition, nm.V_SurfaceOrientation)}'. B: '{edgeB.DoesPositionLieOnEdge(obstructEdge.StartPosition, nm.V_SurfaceOrientation)}'\n";

			dbgRprt += $"previewing obstruct edge end at: '{obstructEdge.EndPosition}'\n" +
				$"cndtn1 (btwnEjs): '{AmBetweenConcurrentLines(obstructEdge.EndPosition, bridgeStartA_calculated, bridgeStartB_calculated, bridgeEndA_calculated, bridgeEndB_calculated, nm.V_SurfaceOrientation, ref s)}'\n" +
				$"cndtn2 (bridge ): '{AmBetweenConcurrentLines(obstructEdge.EndPosition, bridgeStartA_calculated, bridgeEndA_calculated, bridgeStartB_calculated, bridgeEndB_calculated, nm.V_SurfaceOrientation, ref s)}'\n" +
				$"obstrctEdgeEnd is on edge: '{edgeA.DoesPositionLieOnEdge(obstructEdge.EndPosition, nm.V_SurfaceOrientation)}', B: '{edgeB.DoesPositionLieOnEdge(obstructEdge.EndPosition, nm.V_SurfaceOrientation)}'\n";

			dbgRprt += ("\nnow actually doing it----------------//////////////\n");
			if //check mid point...
			(
				//!edgeA.DoesPositionLieOnEdge(obstructEdge.MidPosition, nm.V_SurfaceOrientation) && //Note: this does NOT need to be checked for Mid position...
				AmBetweenConcurrentLines //checks that it's between the two edges
				(
					obstructEdge.MidPosition,
					bridgeStartA_calculated, bridgeStartB_calculated,
					bridgeEndA_calculated, bridgeEndB_calculated,
					nm.V_SurfaceOrientation, ref dbgRprt
				) &&
				AmBetweenConcurrentLines //checks that it's between the edge bridges
				(
					obstructEdge.MidPosition,
					bridgeStartA_calculated, bridgeEndA_calculated,
					bridgeStartB_calculated, bridgeEndB_calculated,
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
				!edgeA.DoesPositionLieOnEdge(obstructEdge.StartPosition, nm.V_SurfaceOrientation) &&
				!edgeB.DoesPositionLieOnEdge(obstructEdge.StartPosition, nm.V_SurfaceOrientation) &&
				AmBetweenConcurrentLines //checks that it's between vectors going from edge to edge
				(
					obstructEdge.StartPosition_flat,
					bridgeStartA_calculated, bridgeStartB_calculated,
					bridgeEndA_calculated, bridgeEndB_calculated,
					nm.V_SurfaceOrientation, ref dbgRprt
				) &&
				AmBetweenConcurrentLines //checks that it's between the edges
				(
					obstructEdge.StartPosition_flat,
					bridgeStartA_calculated, bridgeEndA_calculated,
					bridgeStartB_calculated, bridgeEndB_calculated,
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
				!edgeA.DoesPositionLieOnEdge(obstructEdge.EndPosition, nm.V_SurfaceOrientation) &&
				!edgeB.DoesPositionLieOnEdge(obstructEdge.EndPosition, nm.V_SurfaceOrientation) &&
				AmBetweenConcurrentLines //checks that it's between vectors going from edge to edge
				(
					obstructEdge.EndPosition,
					bridgeStartA_calculated, bridgeStartB_calculated,
					bridgeEndA_calculated, edgeB.EndPosition_flat,
					nm.V_SurfaceOrientation, ref dbgRprt
				) &&
				AmBetweenConcurrentLines //checks that it's between the edges
				(
					obstructEdge.EndPosition,
					bridgeStartA_calculated, bridgeEndA_calculated,
					bridgeStartB_calculated, edgeB.EndPosition_flat,
					nm.V_SurfaceOrientation, ref dbgRprt
				)
			)
			{
				dbgRprt += $"end point check passed. Returning true...";
				Debug.Log(dbgRprt);
				return true;
			}
			else
			{
				dbgRprt += "None returned true. Continuing...\n";
			}

			#endregion
			*/

			return false;
		}

		public static LNX_Quad GetTriPath( LNX_Triangle triA, LNX_Triangle triB, ref string dbgString )
		{
			dbgString = $"GetTriPath({triA}, {triB})\n\n";
			LNX_Quad rtrnQuad = new LNX_Quad( Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero );

			dbgString += $"First, seeing if a single vert on triA is the answer...\n";

			for ( int i_vrts_triA = 0; i_vrts_triA < 3; i_vrts_triA++ )
			{
				dbgString += $"for triA.Vert{i_vrts_triA}...\n";

				bool sweepEncompassesAllOtherVerts = true;
				string s = "";

				LNX_Edge wdstEdge = GetWidestEdgeFromPerspective( triA.Verts[i_vrts_triA].V_flattenedPosition, triB, ref s );
				dbgString += $"widest edge from triA.Vert{i_vrts_triA} perspective was: '{wdstEdge}'...\n" +
					$"checking if edge is in vector cone...\n";

				if
				(
					EdgeEncompassesVertSweep(wdstEdge, triA.Verts[i_vrts_triA], triA.v_SurfaceNormal_cached, ref s )
					//AmCompletelyInVertexCone(wdstEdge, triA.Verts[i_vrts_triA], triA.v_SurfaceNormal_cached, ref s, true) //todo: this isn't it. It's so close though...
					//I neeed to check that the widest edge goes beyond the vert angle sweep
				/*
					AmInVertexCone
					(
						wdstEdge,
						triA.Verts[i_vrts_triA],
						triA.v_SurfaceNormal_cached, ref s, true
					)
				*/
				)
				{
					dbgString += $"Found that widest edge from vert perspective WAS in vector cone. Figuring return quad...\n";

					Vector3 v_v_toWdstEdgeStart = FlatVector(wdstEdge.StartPosition_flat - triA.Verts[i_vrts_triA].V_flattenedPosition).normalized;
					Vector3 v_v_toWdstEdgeEnd = FlatVector(wdstEdge.EndPosition_flat - triA.Verts[i_vrts_triA].V_flattenedPosition).normalized;
					Vector3 v_v_toWdstEdgeMid = FlatVector(wdstEdge.MidPosition_flat - triA.Verts[i_vrts_triA].V_flattenedPosition).normalized;

					if
					(
						Vector3.SignedAngle(v_v_toWdstEdgeMid, v_v_toWdstEdgeStart, triA.v_SurfaceNormal_cached) < 0
					)
					{
						return new LNX_Quad
						(
							triA.Verts[i_vrts_triA].V_flattenedPosition, triA.Verts[i_vrts_triA].V_flattenedPosition,
							wdstEdge.StartPosition_flat, wdstEdge.EndPosition_flat
						);
					}
					else
					{
						return new LNX_Quad
						(
							triA.Verts[i_vrts_triA].V_flattenedPosition, triA.Verts[i_vrts_triA].V_flattenedPosition,
							wdstEdge.EndPosition_flat, wdstEdge.StartPosition_flat
						);
					}
				}
				else
				{
					dbgString += $"Found that widest edge from vert perspective was NOT in vector cone...\n";
				}
			}

			dbgString += $"End of vert cone check...\n\n" +
				$"now finding widest edge from tri perspectives...\n";

			string sA = "";
			LNX_Edge edgeA = GetWidestEdgeFromPerspective( triA, triB, ref sA );
			if( edgeA == null )
			{
				dbgString += $"Couldn't get widest edge for triA...\n" +
					$"rprt:\n" +
					$"{sA}\n";
			}
			else
			{
				dbgString += $"widest edge for triA: '{edgeA}'...\n";
			}

			string sB = "";
			LNX_Edge edgeB = GetWidestEdgeFromPerspective( triB, triA, ref sB );
			if (edgeB == null)
			{
				dbgString += $"Couldn't get widest edge for triB...\n" +
					$"rprt:\n" +
					$"{sB}\n";
			}
			else
			{
				dbgString += $"widest edge for triB: '{edgeB}'...\n";
			}

			if ( AreEdgesAlignedFromTheirPerspectives(edgeA, edgeB) )
			{
				dbgString += $"edges ARE aligned...\n";
				rtrnQuad = new LNX_Quad( edgeA.StartPosition_flat, edgeA.EndPosition_flat, edgeB.EndPosition_flat, edgeB.StartPosition_flat );
			}
			else
			{
				dbgString += $"edges are NOT aligned...\n";

				rtrnQuad = new LNX_Quad(edgeA.StartPosition_flat, edgeA.EndPosition_flat, edgeB.StartPosition_flat, edgeB.EndPosition_flat );
			}

			return rtrnQuad;
		}
		#endregion

		#region SPECIAL EDGE METHODS --------------------
		public static bool AreStartAndEndPointsAlignedFromTheirPerspectives( Vector3 lineStrtA, Vector3 lineEndA, Vector3 lineStrtB, Vector3 lineEndB, Vector3 nrml )
		{
			if (lineStrtA == lineStrtB || lineEndA == lineEndB)
			{
				return true;
			}
			else if( lineStrtA == lineEndB || lineStrtB == lineEndA )
			{
				return false;
			}

			return Vector3.Dot(
				Vector3.Normalize(FlatVector(lineStrtB, nrml) - FlatVector(lineStrtA, nrml)), //start to start
				Vector3.Normalize(FlatVector(lineEndB, nrml) - FlatVector(lineEndA, nrml)) //end to end
			) >
			Vector3.Dot(
				Vector3.Normalize(FlatVector(lineEndB, nrml) - FlatVector(lineStrtA, nrml)), //start to end
				Vector3.Normalize(FlatVector(lineStrtB, nrml) - FlatVector(lineEndA, nrml)) //end to start
			);
		}

		/// <summary>
		/// Checks whether two edges have starts and end points that are more aligned with each others starts and end points than 
		/// "crisscrossed", where one's start point is more aligned with the other's end point.
		/// </summary>
		/// <param name="edgeA"></param>
		/// <param name="edgeB"></param>
		/// <returns></returns>
		public static bool AreEdgesAlignedFromTheirPerspectives( LNX_Edge edgeA, LNX_Edge edgeB )
		{
			return AreStartAndEndPointsAlignedFromTheirPerspectives(edgeA.StartPosition_flat, edgeA.EndPosition_flat, edgeB.StartPosition_flat, edgeB.EndPosition_flat, edgeA.V_SurfaceNormal_cached );
		}

		/// <summary>
		/// Determines if a supplied position lies between two edge lines, that is; if the position is at 
		/// different signed angles compared to both edges.
		/// Note: this method does NOT take into account edge length. It instead treats the edges as if they 
		/// go on for infinity.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="edgeA"></param>
		/// <param name="edgeB"></param>
		/// <returns></returns>
		public static bool IsPositionBetweenEdges( Vector3 pos, LNX_Edge edgeA, LNX_Edge edgeB )
		{
			//todo: make a tdg...
			string s = "";
			if( AreEdgesAlignedFromTheirPerspectives(edgeA, edgeB) )
			{
				return AmBetweenConcurrentLines( pos, edgeA.StartPosition_flat, edgeA.EndPosition_flat, edgeB.StartPosition_flat, edgeB.EndPosition_flat, edgeA.V_SurfaceNormal_cached, ref s );
			}
			else
			{
				return AmBetweenConcurrentLines(pos, edgeA.StartPosition_flat, edgeA.EndPosition_flat, edgeB.EndPosition_flat, edgeB.StartPosition_flat, edgeA.V_SurfaceNormal_cached, ref s);
			}
		}

		public static bool DoesEdgeObstructQuadArea( LNX_Edge obstructEdge, Vector3 crnrA, Vector3 crnrB, Vector3 crnrC, Vector3 crnrD, bool includeOnBorder, ref string dbgReport )
		{
			dbgReport = $"DoesEdgeObstructQuadArea({obstructEdge})\n";

			string strA = "";
			string strB = "";
			string strC = "";

			if
			(
				AmInQuadArea(obstructEdge.MidPosition_flat, crnrA, crnrB, crnrC, crnrD, obstructEdge.V_SurfaceNormal_cached, includeOnBorder, ref strA) ||
				AmInQuadArea(obstructEdge.StartPosition_flat, crnrA, crnrB, crnrC, crnrD, obstructEdge.V_SurfaceNormal_cached, includeOnBorder, ref strB) ||
				AmInQuadArea(obstructEdge.EndPosition_flat, crnrA, crnrB, crnrC, crnrD, obstructEdge.V_SurfaceNormal_cached, includeOnBorder, ref strC)
			)
			{
				dbgReport += $"AmInQuadArea(obstruct mid) rprt...\n" +
					$"{strA}\n" +
					$"AmInQuadArea(obstruct start) rprt...\n" +
					$"{strB}\n" +
					$"AmInQuadArea(obstruct end) rprt...\n" +
					$"{strC}\n";

				return true;
			}

			dbgReport += $"decided none of the obstruct edge points are in quad area\n" +
				$"Now checking if obstruct edge encomapses entire bridge path...";
			#region Now check if obstructEdge is big enough to encompass entire bridge path===================================			
			//Vector3 mid2mid = Vector3.Normalize(FlatVector((crnrA + crnrB) / 2f) - obstructEdge.MidPosition_flat);

			Vector3 v_bridge_pthToObstruct_sideA = Vector3.Normalize( obstructEdge.StartPosition_flat - crnrA );
			Vector3 v_bridge_pthToObstruct_sideB = Vector3.Normalize( obstructEdge.EndPosition_flat - crnrB );

			Vector3 v_bridge_pathToPath_sideA = Vector3.Normalize( crnrD - crnrA );
			Vector3 v_bridge_pathToPath_sideB = Vector3.Normalize( crnrC - crnrB );

			bool strtPthEdgeAndObstructStartAreInAlignment = AreStartAndEndPointsAlignedFromTheirPerspectives(
				crnrA, crnrB, obstructEdge.StartPosition_flat, obstructEdge.EndPosition_flat, obstructEdge.V_SurfaceNormal_cached );

			// CORRECT, IF NECESSARY===============================================================================
			if (!strtPthEdgeAndObstructStartAreInAlignment)
			{
				v_bridge_pthToObstruct_sideA = Vector3.Normalize( obstructEdge.EndPosition_flat - crnrA );
				v_bridge_pthToObstruct_sideB = Vector3.Normalize( obstructEdge.StartPosition_flat - crnrB );
			}
			// --------------------

			Vector3 v_startEdge_startToEnd = Vector3.Normalize( FlatVector(crnrB - crnrA) );

			if
			(
				(
					Vector3.Angle(v_startEdge_startToEnd, v_bridge_pthToObstruct_sideA) >
					Vector3.Angle(v_startEdge_startToEnd, v_bridge_pathToPath_sideA)
				) &&
				(
					Vector3.Angle(-v_startEdge_startToEnd, v_bridge_pthToObstruct_sideB) >
					Vector3.Angle(-v_startEdge_startToEnd, v_bridge_pathToPath_sideB)
				)
			)
			{
				dbgReport += "obstruct edge encompases bridge path. returning true";
				return true;
			}
			#endregion

			return false;
		}
		public static bool DoesEdgeObstructEdgePath(LNX_Edge obstructEdge, LNX_Edge pthStrtEdge, LNX_Edge pthEndEdge, ref string dbgString )
		{
			dbgString = $"{nameof(DoesEdgeObstructEdgePath)}()\n";
			bool pthEdgesAreInAlignment = AreEdgesAlignedFromTheirPerspectives(pthStrtEdge, pthEndEdge);

			/*
			string dbgAmBtwnCcrnt1 = "";
			string dbgAmBtwnCcrnt2 = "";
			string dbgAmBtwnCcrnt3 = "";

			if //note: If you're confused why I'm not using 'pthEdgesAreInAlignment' here, it's because the following methods calculate this in their bodies...
			(
				AmInQuadArea(obstructEdge.MidPosition_flat, pthStrtEdge, pthEndEdge, false, ref dbgAmBtwnCcrnt1) ||
				AmInQuadArea(obstructEdge.StartPosition_flat, pthStrtEdge, pthEndEdge, false, ref dbgAmBtwnCcrnt2) ||
				AmInQuadArea(obstructEdge.EndPosition_flat, pthStrtEdge, pthEndEdge, false, ref dbgAmBtwnCcrnt3)
			)*/

			dbgString += $"pthEdgesAreInAlignment: '{pthEdgesAreInAlignment}'\n\n" +
				$"now checking if obstruct edge obstructs the quad area formed by the path start and end edges...\n";
			string dbgObstructEdgeMethod = "";
			if( pthEdgesAreInAlignment )
			{
				if ( DoesEdgeObstructQuadArea(
					obstructEdge, 
					pthStrtEdge.StartPosition_flat, pthStrtEdge.EndPosition_flat, 
					pthEndEdge.EndPosition_flat, pthEndEdge.StartPosition_flat, false, ref dbgObstructEdgeMethod) )
				{
					dbgString += $"Decided one of the obstructEdge points are in quad area.\n" +
					$"\nreport=====\n" +
					$"{dbgObstructEdgeMethod}" +
					$"----\n" +
					$"\nReturning true...\n";
					return true;
				}
			}
			else
			{
				if ( DoesEdgeObstructQuadArea(obstructEdge, pthStrtEdge.StartPosition_flat, pthStrtEdge.EndPosition_flat, pthEndEdge.StartPosition_flat, pthEndEdge.EndPosition_flat, false, ref dbgObstructEdgeMethod) )
				{
					dbgString += $"Decided one of the obstructEdge points are in quad area.\n" +
					$"\nreport=====\n" +
					$"{dbgObstructEdgeMethod}" +
					$"----\n" +
					$"\nReturning true...\n";
					return true;
				}
			}

			dbgString += $"Apparently no obstruction...\n";
			/*
			dbgString += $"Decided obstruct points are NOT in between 'bridge'\n";

			dbgString += $"\n=================================\n" +
				$"Now Checking if obstruct edge encompasses entire path...\n";

			#region Now check if obstructEdge is big enough to encompass entire edge path===================================			
			Vector3 mid2mid = Vector3.Normalize( pthStrtEdge.MidPosition_flat - obstructEdge.MidPosition_flat );

			Vector3 v_bridge_pthToObstruct_sideA = Vector3.Normalize(obstructEdge.StartPosition_flat - pthStrtEdge.StartPosition_flat );
			Vector3 v_bridge_pthToObstruct_sideB = Vector3.Normalize(obstructEdge.EndPosition_flat - pthStrtEdge.EndPosition_flat);

			Vector3 v_bridge_pathToPath_sideA = Vector3.Normalize( pthEndEdge.StartPosition_flat - pthStrtEdge.StartPosition_flat );
			Vector3 v_bridge_pathToPath_sideB = Vector3.Normalize( pthEndEdge.EndPosition_flat - pthStrtEdge.EndPosition_flat );

			bool strtPthEdgeAndObstructStartAreInAlignment = AreEdgesAlignedFromTheirPerspectives(pthStrtEdge, obstructEdge);

			#region CORRECT, IF NECESSARY===============================================================================
			if ( !strtPthEdgeAndObstructStartAreInAlignment )
			{
				v_bridge_pthToObstruct_sideA = Vector3.Normalize( obstructEdge.EndPosition_flat - pthStrtEdge.StartPosition_flat );
				v_bridge_pthToObstruct_sideB = Vector3.Normalize( obstructEdge.StartPosition_flat - pthStrtEdge.EndPosition_flat );
			}

			if ( !pthEdgesAreInAlignment )
			{
				v_bridge_pathToPath_sideA = Vector3.Normalize(pthEndEdge.EndPosition_flat - pthStrtEdge.StartPosition_flat);
				v_bridge_pathToPath_sideB = Vector3.Normalize(pthEndEdge.StartPosition_flat - pthStrtEdge.EndPosition_flat);
			}
			#endregion--------------------

			if
			(
				(
					Vector3.Angle(pthStrtEdge.V_StartToEnd_flattened, v_bridge_pthToObstruct_sideA) >
					Vector3.Angle(pthStrtEdge.V_StartToEnd_flattened, v_bridge_pathToPath_sideA)
				) &&
				(
					Vector3.Angle(pthStrtEdge.V_EndToStart_flattened, v_bridge_pthToObstruct_sideB) >
					Vector3.Angle(pthStrtEdge.V_EndToStart_flattened, v_bridge_pathToPath_sideB)
				)
			)
			{
				dbgString += "obstruct edge encompases start edge path. returning true";
				return true;
			}

			#endregion
			*/

			return false;
		}

		public static LNX_Vertex GetWidestVertOnOtherTriangleFromEdgeStartPerspective( LNX_Edge perspectiveEdge, LNX_Triangle otherTriangle )
		{
			float runningWidestAngle = 0f;
			int runningWidestIndex = -1;

			for ( int i = 0; i < 3; i++ )
			{
				float ang = Vector3.Angle( perspectiveEdge.V_StartToEnd_flattened, Vector3.Normalize(otherTriangle.Verts[i].V_flattenedPosition - perspectiveEdge.StartPosition_flat) );
				if( ang > runningWidestAngle )
				{
					runningWidestAngle = ang;
					runningWidestIndex = i;
				}
			}

			return otherTriangle.Verts[runningWidestIndex];
		}

		public static LNX_ComponentCoordinate GetWidestVertOnOtherEdgeFromEdgeStartPerspective( LNX_Edge perspectiveEdge, LNX_Edge otherEdge)
		{
			float runningWidestAngle = Vector3.Angle( perspectiveEdge.V_StartToEnd_flattened, Vector3.Normalize(otherEdge.StartPosition_flat - perspectiveEdge.StartPosition_flat) );

			if( Vector3.Angle(perspectiveEdge.V_StartToEnd_flattened, Vector3.Normalize(otherEdge.EndPosition_flat - perspectiveEdge.StartPosition_flat)) > runningWidestAngle )
			{
				return otherEdge.EndVertCoordinate;
			}
			else
			{
				return otherEdge.StartVertCoordinate;

			}
		}

		public static LNX_Vertex GetWidestVertOnOtherTriangleFromEdgeEndPerspective(LNX_Edge perspectiveEdge, LNX_Triangle otherTriangle)
		{
			float runningWidestAngle = 0f;
			int runningWidestIndex = -1;

			for (int i = 0; i < 3; i++)
			{
				float ang = Vector3.Angle( perspectiveEdge.V_EndToStart_flattened, Vector3.Normalize(otherTriangle.Verts[i].V_flattenedPosition - perspectiveEdge.EndPosition_flat) );
				if (ang > runningWidestAngle)
				{
					runningWidestAngle = ang;
					runningWidestIndex = i;
				}
			}

			return otherTriangle.Verts[runningWidestIndex];
		}

		public static LNX_ComponentCoordinate GetWidestVertOnOtherEdgeFromEdgeEndPerspective(LNX_Edge perspectiveEdge, LNX_Edge otherEdge )
		{
			float runningWidestAngle = Vector3.Angle(perspectiveEdge.V_EndToStart_flattened, Vector3.Normalize(otherEdge.StartPosition_flat - perspectiveEdge.EndPosition_flat));

			if ( Vector3.Angle(perspectiveEdge.V_EndToStart_flattened, Vector3.Normalize(otherEdge.EndPosition_flat - perspectiveEdge.EndPosition_flat)) > runningWidestAngle)
			{
				return otherEdge.EndVertCoordinate;
			}
			else
			{
				return otherEdge.StartVertCoordinate;

			}
		}

		public static float GetEdgeBridgeAngleAmount(LNX_Edge prspctvEdge, LNX_Edge otherEdge)
		{
			if (AreEdgesAlignedFromTheirPerspectives(prspctvEdge, otherEdge))
			{
				return
					(prspctvEdge.StartPosition_flat == otherEdge.StartPosition_flat ? 90f : //If it's shared, just consider it 90 degrees...
						Vector3.Angle(prspctvEdge.V_StartToEnd_flattened, Vector3.Normalize(otherEdge.StartPosition_flat - prspctvEdge.StartPosition_flat))) +
					(otherEdge.EndPosition_flat == prspctvEdge.EndPosition_flat ? 90f : //If it's shared, just consider it 90 degrees...
						Vector3.Angle(prspctvEdge.V_EndToStart_flattened, Vector3.Normalize(otherEdge.EndPosition_flat - prspctvEdge.EndPosition_flat)));
			}
			else
			{
				return
					(prspctvEdge.StartPosition_flat == otherEdge.EndPosition_flat ? 90f : //If it's shared, just consider it 90 degrees...
						Vector3.Angle(prspctvEdge.V_StartToEnd_flattened, Vector3.Normalize(otherEdge.EndPosition_flat - prspctvEdge.StartPosition_flat))) +
					(otherEdge.StartPosition_flat == prspctvEdge.EndPosition_flat ? 90f :  //If it's shared, just consider it 90 degrees...
					Vector3.Angle(prspctvEdge.V_EndToStart_flattened, Vector3.Normalize(otherEdge.StartPosition_flat - prspctvEdge.EndPosition_flat)));
			}
			
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
}