using LogansNavigationExtension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

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

		public static int HowManyAreTheSame( params Vector3[] vectors )
		{
			List<Vector3> discoveredValues = new List<Vector3>();
			List<int> discoveredValueAppearences = new List<int>();
			bool foundDuplicate = false;

			for ( int i = 0; i < vectors.Length; i++ )
			{
				if( !discoveredValues.Contains( vectors[i] ) )
				{
					discoveredValues.Add( vectors[i] );
					discoveredValueAppearences.Add(1);

					for ( int j = i + 1; j < vectors.Length; j++ )
					{
						if(vectors[i] == vectors[j] )
						{
							discoveredValueAppearences[discoveredValueAppearences.Count - 1]++;

							foundDuplicate = true;
						}
					}
				}
			}

			if( foundDuplicate )
			{
				int amt = 0;
				for( int i = 0; i < discoveredValueAppearences.Count; i++ )
				{
					if(discoveredValueAppearences[i] > 1 )
					{
						amt += discoveredValueAppearences[i];
					}
				}

				return amt;
			}

			return 0;
		} //todo: dws?

		#region MATH OPERATIONS --------------------------------------
		/// <summary>
		/// Solves for lenA
		/// </summary>
		/// <param name="angA"></param>
		/// <param name="angB"></param>
		/// <param name="lenB"></param>
		/// <returns></returns>
		public static float CalculateTriangleEdgeLength( float angA, float angB, float lenB ) //todo: use the following CalculateTriangleSideLength() instead and DWS
		{
			return Mathf.Sin(angA * Mathf.Deg2Rad) * lenB / Mathf.Sin(angB * Mathf.Deg2Rad);
		}

		/// <summary>
		/// Solves triangle side length. Which side is solved will depend on which parameters were supplied.
		/// Input -1f For the paramaters you don't know.
		/// </summary>
		/// <param name="lenA"></param>
		/// <param name="angA"></param>
		/// <param name="lenB"></param>
		/// <param name="angB"></param>
		/// <param name="lenC"></param>
		/// <param name="angC"></param>
		/// <returns></returns>
		public static float CalculateTriangleEdgeLength( float lenA, float angA, float lenB, float angB, float lenC, float angC )
		{
			if( lenA < 0f && lenB < 0f && lenC < 0f )
			{
				Debug.LogError($"You supplied this method with no valid lengths. You MUST have at least one known length to solve a triangle.");
				return -1f; //You MUST know a length...
			}

			if( lenA == -1f && angA > 0) //solve for lenA...
			{
				if( lenB > 0 && angB > 0 )
				{
					return Mathf.Sin(angA * Mathf.Deg2Rad) * lenB / Mathf.Sin(angB * Mathf.Deg2Rad);
				}
				if ( lenC > 0 && angC > 0)
				{
					return Mathf.Sin(angA * Mathf.Deg2Rad) * lenC / Mathf.Sin(angC * Mathf.Deg2Rad);
				}
			}
			else if ( lenB == -1f && angB > 0 ) //solve for lenB...
			{
				if (lenA > 0 && angA > 0)
				{
					return Mathf.Sin(angB * Mathf.Deg2Rad) * lenA / Mathf.Sin(angA * Mathf.Deg2Rad);
				}
				if (lenC > 0 && angC > 0)
				{
					return Mathf.Sin(angB * Mathf.Deg2Rad) * lenC / Mathf.Sin(angC * Mathf.Deg2Rad);
				}
			}
			else if (lenC == -1f && angC > 0) //solve for lenC...
			{
				if (lenA > 0 && angA > 0)
				{
					return Mathf.Sin(angC * Mathf.Deg2Rad) * lenA / Mathf.Sin(angA * Mathf.Deg2Rad);
				}
				if (lenB > 0 && angB > 0)
				{
					return Mathf.Sin(angC * Mathf.Deg2Rad) * lenB / Mathf.Sin(angB * Mathf.Deg2Rad);
				}
			}
			else
			{
				Debug.LogError($"You supplied this method with an invalid combination. You MUST have at least one known length to solve a triangle.");
			}

			return -1f;
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

		#region VECTOR OPERATIONS ==========================================================================
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

		public static Vector3 FlatVector( Vector3 vector, Vector3 flattenDir )
		{
			if( flattenDir == Vector3.zero )
			{
				Debug.LogError( $"LNX ERROR! You supplied a normal parameter of 0. Need a normal direction in order to flatten!" );
				return Vector3.zero;
			}

			if ( (flattenDir == Vector3.up || flattenDir == Vector3.down) )
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
			else if (flattenDir == Vector3.right || flattenDir == Vector3.left)
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
			else if (flattenDir == Vector3.forward || flattenDir == Vector3.back)
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
			else if ( flattenDir != Vector3.zero )
			{
				return Vector3.ProjectOnPlane( vector, flattenDir );
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

		public static bool AmInVectorCone(Vector3 vToPos, Vector3 vLegA, Vector3 vLegB, Vector3 nrml, bool includeOnPerim = false )
		{
			#region SHORT-CIRCUITING ==========================================
			if ( vToPos == vLegA || vToPos == vLegB)
			{
				if (includeOnPerim)
				{
					return true;
				}
				else
				{
					return false;
				}
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

			if
			( 
				Mathf.Sign(ang_crnr) != Mathf.Sign(ang_legAToPos) &&
				Mathf.Sign(ang_crnr) == Mathf.Sign(ang_legBToPos)
			)
			{
				return true;
			}

			return false;
		}
		public static bool AmInVectorCone_dbg(Vector3 vToPos, Vector3 vLegA, Vector3 vLegB, Vector3 nrml, ref LNX_MethodDebugReport rprt, bool includeOnPerim = false)
		{
			rprt.StartMethod($"AmInVectorCone(vToPos: '{vToPos}', incldOnPerim: '{includeOnPerim}')");
			rprt.Log($"vLegA: '{vLegA}', vLegB: '{vLegB}', nrml: '{nrml}'");

			vToPos = Vector3.Normalize(vToPos);

			#region SHORT-CIRCUITING ==========================================
			if (vToPos == vLegA || vToPos == vLegB)
			{
				if (includeOnPerim)
				{
					rprt.Log_And_End_Method( $"was told to include perim, and I found that pos is on perim, short-circuit returning true...",
						$"AmInVectorCone");

					return true;
				}
				else
				{
					rprt.Log_And_End_Method( $"was told NOT to include perim, and I found that pos is on perim, short-circuit returning false...",
						$"AmInVectorCone");

					return false;
				}
			}

			if (vLegA == -vLegB)
			{
				rprt.Log_And_End_Method($"vLegA equals -vLegB, this would make a 180 degree sweep. Short-circuit returning true...",
					"AmInVectorCone");
				return true; //because the "sweep cone" in this case would be a full 180 degrees, and it wouldn't matter which side.
							 //todo: Maybe I should actually log a warning here?
			}

			if (vLegA == vLegB)
			{
				rprt.Log_And_End_Method($"vLegA equals vLegB, this would make a 0 degree sweep. Short-circuit returning false...",
					"AmInVectorCone");
				return false;
			}
			#endregion

			rprt.Log($"no short-circuits applied. Continuing...");

			float ang_crnr = Vector3.SignedAngle(vLegA, vLegB, nrml);
			//float ang_crnr = Vector3.Angle(vLegA, vLegB);

			float ang_legAToPos = Vector3.SignedAngle(vToPos, vLegA, nrml);
			float ang_legBToPos = Vector3.SignedAngle(vToPos, vLegB, nrml);

			rprt.Log($" using ang_crnr: '{ang_crnr}', ang_legAToPos: '{ang_legAToPos}', and ang_legBToPos: '{ang_legBToPos}'...");
			rprt.Log($"now performing sign check...");

			if
			(
				Mathf.Sign(ang_crnr) != Mathf.Sign(ang_legAToPos) &&
				Mathf.Sign(ang_crnr) == Mathf.Sign(ang_legBToPos)
			)
			{
				rprt.Log_And_End_Method($"sign check succeeded. Returning true...",
					"AmInVectorCone");
				return true;
			}
			else
			{
				rprt.Log($"sign check failed....");
			}

			rprt.Log_And_End_Method("Returning false...", "AmInVectorCone");

			return false;
		}

		/// <summary>
		/// Determines whether two supplied lines cross each other. Note: these lines are treated as having discrete start and end points. They are NOT 
		/// treated as "directional vectors", running infinitely with no theoretical starts or ends.
		/// </summary>
		/// <param name="lineaStart"></param>
		/// <param name="lineaEnd"></param>
		/// <param name="linebStart"></param>
		/// <param name="linebEnd"></param>
		/// <param name="nrml"></param>
		/// <param name="includeTouchEnds"></param>
		/// <returns></returns>
		public static bool VectorsCrossEachOther( Vector3 lineaStart, Vector3 lineaEnd, Vector3 linebStart, Vector3 linebEnd, Vector3 nrml, bool includeTouchEnds )
		{
			//todo: implement this in the edge DoesProjectionIntersectEdge() method
			if 
			( 
				!includeTouchEnds && 
				lineaStart == linebStart || lineaStart == linebEnd ||
				lineaEnd == linebStart || lineaEnd == linebEnd
			)
			{
				return false;
			}

			lineaStart = FlatVector(lineaStart);
			lineaEnd = FlatVector(lineaEnd);
			linebStart = FlatVector(linebStart);
			linebEnd = FlatVector(linebEnd);

			if ( !AreStartAndEndPointsAlignedFromTheirPerspectives(lineaStart, lineaEnd, linebStart, linebEnd, nrml) )
			{
				Vector3 savedStart = linebStart;
				linebStart = linebEnd;
				linebEnd = savedStart;
			}

			Vector3 v_lineA_startToEnd = Vector3.Normalize( lineaEnd - lineaStart );
			Vector3 v_lineB_startToEnd = Vector3.Normalize( linebEnd - linebStart );

			Vector3 v_lineAStart_to_linebStart = Vector3.Normalize( linebStart - lineaStart );
			Vector3 v_lineAStart_to_linebEnd = Vector3.Normalize( linebEnd - lineaStart );
			Vector3 v_lineAend_to_lineBStart = Vector3.Normalize( linebStart - lineaEnd );
			Vector3 v_lineAend_to_lineBend = Vector3.Normalize( linebEnd - lineaEnd );

			Vector3 v_lineBStart_to_lineAstart = -v_lineAStart_to_linebStart;
			Vector3 v_lineBStart_to_lineAEnd = -v_lineAend_to_lineBStart;
			Vector3 lineBEnd_to_lineAstart = -v_lineAStart_to_linebEnd;
			Vector3 lineBEnd_to_lineAend = -v_lineAend_to_lineBend;
			if
			(
				(
					Mathf.Sign(Vector3.SignedAngle(-v_lineA_startToEnd, v_lineAStart_to_linebStart, nrml)) !=
					Mathf.Sign(Vector3.SignedAngle(-v_lineA_startToEnd, v_lineAStart_to_linebEnd, nrml)) 
					||
					Mathf.Sign(Vector3.SignedAngle(v_lineA_startToEnd, v_lineAend_to_lineBStart, nrml)) !=
					Mathf.Sign(Vector3.SignedAngle(v_lineA_startToEnd, v_lineAend_to_lineBend, nrml))
				)
				&&
				(
					Mathf.Sign(Vector3.SignedAngle(-v_lineB_startToEnd, v_lineBStart_to_lineAstart, nrml)) !=
					Mathf.Sign(Vector3.SignedAngle(-v_lineB_startToEnd, v_lineBStart_to_lineAEnd, nrml))
					||
					Mathf.Sign(Vector3.SignedAngle(v_lineB_startToEnd, lineBEnd_to_lineAstart, nrml)) !=
					Mathf.Sign(Vector3.SignedAngle(v_lineB_startToEnd, lineBEnd_to_lineAend, nrml))
				)
			)
			{
				return true;
			}

			return false;
		}
		#endregion


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
		public static bool AmInArea( Vector3 pos, Vector3 crnrA, Vector3 crnrB, Vector3 crnrC, Vector3 crnrD, Vector3 nrml, bool includeOnPerim, ref string dbgRprt )
		{
			dbgRprt = $"4) AmInArea('{pos}'\n" +
				$"cA: '{crnrA}', cB: '{crnrB}', cC: '{crnrC}', cD: '{crnrD}', includeOnBorder: '{includeOnPerim}')\n";

			pos = FlatVector(pos, nrml);
			crnrA = FlatVector(crnrA, nrml);
			crnrB = FlatVector(crnrB, nrml);
			crnrC = FlatVector(crnrC, nrml);
			crnrD = FlatVector(crnrD, nrml);

			#region SHORT-CIRCUITING ==========================
			if (includeOnPerim &&
				(
					pos == crnrA || pos == crnrB || pos == crnrC || pos == crnrD
				)
			)
			{
				return true;
			}
			#endregion

			bool usingCrnrAToCrnrC = true;

			#region SHORT-CIRCUITING ================================
			//================================
			int sameCount = 0;
			if( crnrA == crnrB ) //A...
			{
				sameCount++;
			}
			if (crnrA == crnrC)
			{
				sameCount++;

				usingCrnrAToCrnrC = false;
			}
			if (crnrA == crnrD)
			{
				sameCount++;
			}
			if (crnrB == crnrC) //B...
			{
				sameCount++;
			}
			if (crnrB == crnrD)
			{
				sameCount++;
			}
			if (crnrC == crnrD) //C...
			{
				sameCount++;
			}
			dbgRprt += $"check found '{sameCount}' same corners...\n";
			//================================

			if ( sameCount > 0 )
			{
				if ( sameCount > 1 )
				{
					dbgRprt += $"too many crnrs are in the same position. Returning false...\n";
					return false;
				}

				if ( crnrA == crnrC || crnrB == crnrD )
				{
					dbgRprt += $"opposing corners were the same. This creates an invalid area.";
					return false;
				}

				dbgRprt += $"samecount is above 0. Checking if I can use PositionIsOnTriangleArea()...\n";

				if ( crnrB == crnrA || crnrB == crnrC )
				{
					dbgRprt += $"cornerB shares space with another corner. Using triangular area check short-circuit...\n";
					return AmInArea(pos, crnrA, crnrC, crnrD, nrml, includeOnPerim);
				}

				if ( crnrD == crnrA || crnrD == crnrC )
				{
					dbgRprt += $"cornerD shares space with another corner. Using triangular area check short-circuit...\n";
					return AmInArea( pos, crnrA, crnrB, crnrC, nrml, includeOnPerim );
				}
			}
			#endregion

			Vector3 v_aToPos = Vector3.Normalize( pos - crnrA );
			Vector3 v_aToB = Vector3.Normalize( crnrB - crnrA );
			Vector3 v_aToD = Vector3.Normalize( crnrD - crnrA );

			Vector3 v_cToPos = Vector3.Normalize( pos - crnrC );
			Vector3 v_cToB = Vector3.Normalize( crnrB - crnrC );
			Vector3 v_cToD = Vector3.Normalize( crnrD - crnrC );

			#region SHORT-CIRCUITING -----------------------
			if( v_aToB == -v_aToD) //CrnrA
			{
				dbgRprt += $"crnrA doesn't actually make a bend. Short-circuiting to triangular AmInArea() method instead...";
				return AmInArea(pos, crnrB, crnrC, crnrD, nrml, includeOnPerim);
			}
			else if (v_aToB == -v_cToB) //CrnrB
			{
				dbgRprt += $"crnrB doesn't actually make a bend. Short-circuiting to triangular AmInArea() method instead...";
				return AmInArea(pos, crnrA, crnrB, crnrC, nrml, includeOnPerim);
			}
			else if ( v_cToB == -v_cToD ) //CrnrC
			{
				dbgRprt += $"crnrC doesn't actually make a bend. Short-circuiting to triangular AmInArea() method instead...";
				return AmInArea(pos, crnrA, crnrB, crnrD, nrml, includeOnPerim);
			}
			else if (v_aToD == -v_aToB) //CrnrD
			{
				dbgRprt += $"crnrC doesn't actually make a bend. Short-circuiting to triangular AmInArea() method instead...";
				return AmInArea(pos, crnrA, crnrB, crnrD, nrml, includeOnPerim);
			}
			#endregion

			dbgRprt += $"using...\n" +
				$"v_aToB: '{v_aToB}', v_aToD: '{v_aToD}'\n" +
				$"v_cToB: '{v_cToB}', v_cToD: '{v_cToD}'\n" +
				$"v_aToPos: '{v_aToPos}', v_cToPos: '{v_cToPos}'\n" +
				$"Now running angle check...\n";

			if ( usingCrnrAToCrnrC )
			{
				dbgRprt += $"using conventional corner-to-corner check (a-to-c)...\n";
				if
				( 
					AmInVectorCone(v_aToPos, v_aToB, v_aToD, nrml, includeOnPerim) &&
					AmInVectorCone(v_cToPos, v_cToB, v_cToD, nrml, includeOnPerim)
				)
				{
					dbgRprt += $"both vectorcone methods returned true\n" +
						$"4) Returning true...\n";
					return true;
				}
			}
			else //using crnrB to crnr D...
			{
				dbgRprt += $"using UN-conventional corner-to-corner check (a-to-c)...\n";

				Vector3 v_bToPos = Vector3.Normalize( pos - crnrB );
				Vector3 v_dToPos = Vector3.Normalize( pos - crnrD );
				Vector3 v_bToA = -v_aToB;
				Vector3 v_bToC = -v_cToB;
				Vector3 v_dToC = -v_cToD;
				Vector3 v_dToA = -v_aToD;

				if
				(
					AmInVectorCone(v_bToPos, v_bToA, v_bToC, nrml, includeOnPerim) &&
					AmInVectorCone(v_dToPos, v_dToC, v_dToA, nrml, includeOnPerim)
				)
				{
					dbgRprt += $"both vectorcone methods returned true\n" +
						$"4) Returning true...\n"; 
					return true;
				}
			}

			dbgRprt += $"Returning false...";

			return false;
		}

		public static bool AmInArea( Vector3 pos, Vector3 crnrA, Vector3 crnrB, Vector3 crnrC, Vector3 nrml, bool includeOnPerim/*, ref string dbgString*/ )
		{
			//todo: could probably use this in the Triangle classes method that determines if a point is on it's surface.

			pos = FlatVector( pos, nrml );
			crnrA = FlatVector( crnrA, nrml );
			crnrB = FlatVector( crnrB, nrml );
			crnrC = FlatVector( crnrC, nrml );

			#region SHORT-CIRCUITING ==========================
			if (includeOnPerim &&
				(
					pos == crnrA || pos == crnrB || pos == crnrC
				)
			)
			{
				return true;
			}
			#endregion

			Vector3 v_a_to_b = Vector3.Normalize( crnrB - crnrA );
			Vector3 v_a_to_c = Vector3.Normalize( crnrC - crnrA );
			Vector3 v_ptA_to_pos = Vector3.Normalize( pos - crnrA );

			Vector3 v_ptB_to_pos = Vector3.Normalize(pos - crnrB);
			Vector3 v_b_toA = -v_a_to_b;
			Vector3 v_b_to_c = Vector3.Normalize(crnrC - crnrB);

			if
			(
				AmInVectorCone(v_ptA_to_pos, v_a_to_b, v_a_to_c, nrml, includeOnPerim) &&
				AmInVectorCone(v_ptB_to_pos, v_b_toA, v_b_to_c, nrml, includeOnPerim)
			)
			{
				return true;
			}

			return false;
		}
		public static bool AmInArea_dbg(Vector3 pos, Vector3 crnrA, Vector3 crnrB, Vector3 crnrC, Vector3 nrml, bool includeOnPerim, ref LNX_MethodDebugReport rprt )
		{
			rprt.StartMethod($"AmInArea_dbg(pos: '{pos}', )");

			pos = FlatVector(pos, nrml);
			crnrA = FlatVector(crnrA, nrml);
			crnrB = FlatVector(crnrB, nrml);
			crnrC = FlatVector(crnrC, nrml);

			#region SHORT-CIRCUITING ==========================
			if( includeOnPerim &&
				(
					pos == crnrA || pos == crnrB || pos == crnrC
				)
			)
			{
				rprt.Log_And_End_Method($"includeOnPerim == true, and position was on one of the coners. Returning true early...", 
					"AmInArea_dbg");
				return true;
			}
			#endregion

			Vector3 v_a_to_b = Vector3.Normalize(crnrB - crnrA);
			Vector3 v_a_to_c = Vector3.Normalize(crnrC - crnrA);
			Vector3 v_cnrA_to_pos = Vector3.Normalize(pos - crnrA);

			Vector3 v_cnrB_to_pos = Vector3.Normalize(pos - crnrB);
			Vector3 v_b_toA = -v_a_to_b;
			Vector3 v_b_to_c = Vector3.Normalize(crnrC - crnrB);

			rprt.Log($"have created necessary values. Now running AmInVectorCone() checks...");
			if
			(
				AmInVectorCone_dbg(v_cnrA_to_pos, v_a_to_b, v_a_to_c, nrml, ref rprt, includeOnPerim) &&
				AmInVectorCone_dbg(v_cnrB_to_pos, v_b_toA, v_b_to_c, nrml, ref rprt, includeOnPerim)
			)
			{
				rprt.Log_And_End_Method($"checks passed. Returning true...", 
					"AmInArea_dbg");
				return true;
			}

			rprt.Log_And_End_Method($"checks did NOT pass. Returning false...",
				"AmInArea_dbg");
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
					$"{nameof(AmInArea)}()\n" +
					$"returning: '{AmInArea(pos, lineAStart, lineBStart, lineBEnd, nrml, false/*, ref dbgit*/ )}'\n";

				return AmInArea( pos, lineAStart, lineBStart, lineBEnd, nrml, false/*, ref dbgit*/ );

			}
			if ( lineBStart == lineBEnd )
			{
				dbgRprt += "chose lineB start and end equal to block. Now turning to tri point check...\n" +
					$"{nameof(AmInArea)}() rslt: '{AmInArea(pos, lineAStart, lineAEnd, lineBStart, nrml, false/*, ref dbgit*/)}'\n";

				return AmInArea( pos, lineAStart, lineAEnd, lineBStart, nrml, false/*, ref dbgit*/);
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

		#region QUAD OPERATIONS ==================================================================

		public static bool AmInQuadArea( Vector3 pos, LNX_Edge edgeA, LNX_Edge edgeB, bool includeOnBorder, ref string dbgString )
		{
			if( AreEdgesAlignedFromTheirPerspectives(edgeA,edgeB) )
			{
				return AmInArea(pos, edgeA.StartPosition_flat, edgeA.EndPosition_flat, edgeB.EndPosition_flat, edgeB.StartPosition_flat, edgeA.V_NavmeshProjectionDirection_cached, includeOnBorder, ref dbgString );
			}
			else
			{
				return AmInArea(pos, edgeA.StartPosition_flat, edgeA.EndPosition_flat, edgeB.StartPosition_flat, edgeB.EndPosition_flat, edgeA.V_NavmeshProjectionDirection_cached, includeOnBorder, ref dbgString);
			}
		}

		public static bool AmInQuadArea( Vector3 pos, LNX_Quad quad, Vector3 nrml, bool includeOnBorder )
		{
			string s = "";
			return AmInArea( pos, quad.crnrA, quad.crnrB, quad.crnrC, quad.crnrD, nrml, includeOnBorder, ref s );
		}
		#endregion

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
				triangle.Verts[triangle.Edges[0].StartVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.V_NavmeshProjectionDirection_cached),
				triangle.Verts[triangle.Edges[0].EndVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.V_NavmeshProjectionDirection_cached)
			);
			float runningWidestAngle = ang_perspToE0;
			int runningWidestEdge = 0;

			float ang_perspToE1 = Vector3.Angle(
				triangle.Verts[triangle.Edges[1].StartVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.V_NavmeshProjectionDirection_cached),
				triangle.Verts[triangle.Edges[1].EndVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.V_NavmeshProjectionDirection_cached)
			);
			float ang_perspToE2 = Vector3.Angle(
				triangle.Verts[triangle.Edges[2].StartVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.V_NavmeshProjectionDirection_cached),
				triangle.Verts[triangle.Edges[2].EndVertIndex].V_flattenedPosition - FlatVector(vPerspective, triangle.V_NavmeshProjectionDirection_cached)
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
			//dbgString = $"GetWidestEdgeFromPerspective(edge{prspctvEdge.MyCoordinate}, '{otherTri}')\n";

			dbgString += $"first, checking the 'bridge angle' from prspctvEdge to each edge on otherTri...\n";
			float runningWidestBridgeAngle = 0f;
			int indx_widestEdge = -1;
			for (int i = 0; i < 3; i++)
			{
				dbgString += $"for i: '{i}'...\n";
				float angl = GetEdgeBridgeAngleAmount(prspctvEdge, otherTri.Edges[i]);
				dbgString += $"bridge angle was: '{angl}'...\n";

				if( angl == runningWidestBridgeAngle )
				{
					dbgString += $"FOUND EQUAL!!!\n";
				}

				if (angl > runningWidestBridgeAngle)
				{
					//dbgString += $"new widest angle!\n";
					runningWidestBridgeAngle = angl;
					indx_widestEdge = i;
				}


			}

			dbgString += $"end of loop. Widest edge was edge{indx_widestEdge} with angle: '{runningWidestBridgeAngle}'. Returning '{otherTri.Edges[indx_widestEdge]}'...\n";
			
			return otherTri.Edges[indx_widestEdge];
		}

		public static LNX_Edge GetWidestEdgeFromPerspective( LNX_Triangle perspectiveTri, LNX_Triangle otherTri, ref string dbgString ) //<<<< problem
		{
			dbgString = $"GetWidestEdgeFromPerspective(prspctvTri: '{perspectiveTri}', othrTri: '{otherTri}')\n";

			LNX_Edge rtrnEdge = null;

			int[] nmbrTms = new int[3] { 0, 0, 0 };
			LNX_ComponentCoordinate[] coords_widestEdges = new LNX_ComponentCoordinate[3];

			dbgString += $"\n===============================================\n" +
				$"First, iterating through edges to decide widest perspective edges...\n";
			for( int i_prspctvTriEdges = 0; i_prspctvTriEdges < 3; i_prspctvTriEdges++ )
			{
				dbgString += $"\nfor prspctvTri edge{i_prspctvTriEdges}...\n";
				string s = "";
				LNX_Edge wdstEdgeOnOtherTri = GetWidestEdgeFromPerspective( perspectiveTri.Edges[i_prspctvTriEdges], otherTri, ref s );
				//dbgString += $"\n{s}\n";

				dbgString += $"widest edge on otherTri was: '{wdstEdgeOnOtherTri}'. rprt:\n" +
					$"{s}\n" +
					$".Now Checking for 2-way agreement...\n";

				s = "";
				LNX_Edge wdstEdge_otherWay = GetWidestEdgeFromPerspective(wdstEdgeOnOtherTri, perspectiveTri, ref s);
				//dbgString += $"\n{s}\n";

				dbgString += $"widest edge (other way) was: '{wdstEdge_otherWay}'. rprt:\n" +
					$"{s}\n";
				
				if ( wdstEdge_otherWay == perspectiveTri.Edges[i_prspctvTriEdges] )
				{
					nmbrTms[wdstEdgeOnOtherTri.ComponentIndex]++; //todo: I think that maybe if both edges agree, I can return here...

					dbgString += $"They agree. Incremented to '{nmbrTms[wdstEdgeOnOtherTri.ComponentIndex]}'...\n";

					if(nmbrTms[wdstEdgeOnOtherTri.ComponentIndex] > 1 )
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

			dbgString += $"\n======================================\n" +
				$"finally choosing any edge that was found widest with 2-way agreement...\n";
			for( int i = 0; i < 3; i++ )
			{
				if(nmbrTms[i] > 0 )
				{
					dbgString += $"returning {otherTri.Edges[i]}...\n";
					return otherTri.Edges[i];
				}
			}

			dbgString += $"\napparently none agreed. returning null...\n";
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
			return AreStartAndEndPointsAlignedFromTheirPerspectives(edgeA.StartPosition_flat, edgeA.EndPosition_flat, edgeB.StartPosition_flat, edgeB.EndPosition_flat, edgeA.V_NavmeshProjectionDirection_cached );
		}

		/// <summary>
		/// Checks if any point on a given edge lies in the path between two triangles.
		/// </summary>
		/// <param name="nm"></param>
		/// <param name="obstructEdge"></param>
		/// <param name="triIndxA"></param>
		/// <param name="triIndxB"></param>
		/// <returns></returns>
		/// 
		public static bool DoesEdgeObstructTriPath(LNX_Edge obstructEdge, LNX_Triangle triA, LNX_Triangle triB, ref string dbgRprt)
		{
			dbgRprt = $"1) {nameof(DoesEdgeObstructTriPath)}( obstrctEdg: '{obstructEdge}', triA: '{triA}', triB: '{triB}')\n";

			#region SHORT-CIRCUITING==========================================
			if (triA == triB)
			{
				dbgRprt += $"Both supplied triangles were the same. Returning early...\n";
				return false;
			}
			/* //todo: figure this one out...what do I do instead here?
			if ( obstructEdge.TriangleIndex == triA.Index_inCollection || obstructEdge.TriangleIndex == triB.Index_inCollection )
			{
				dbgRprt += $"LNX Error! ObstructEdge seems to be on one of the supplied triangles. Returning early...\n";
				Debug.LogError($"LNX Error! ObstructEdge seems to be on one of the supplied triangles. Returning early...");
				return false;
			}
			*/
			#endregion

			dbgRprt += $"\nChecking for edge obstruction in edge-to-edge areas...\n";

			string s = "";

			for( int i = 0; i < 3; i++ )
			{
				dbgRprt += $"for i: '{i}'...\n";
				dbgRprt += $"Checking for obstruction between triA.Edge[{i}] and all of triB's edges...\n";
				if
				( 
					DoesEdgeObstructEdgePath(obstructEdge, triA.Edges[i], triB.Edges[0], ref s) || //<<<<<<<<<<<<<1
					DoesEdgeObstructEdgePath(obstructEdge, triA.Edges[i], triB.Edges[1], ref s) ||
					DoesEdgeObstructEdgePath(obstructEdge, triA.Edges[i], triB.Edges[2], ref s)
				)
				{
					dbgRprt += $"found obstruction between triA and triB\n" +
						$"rprt:------------------------------------------------------------\n" +
						$"{s}\n" +
						$"1)---------------------------------------------------------------\n\n" +
						$"returning true...";
					return true;
				}
			}
			return false;
		}
		public static bool DoesEdgeObstructQuadArea( //3
			LNX_Edge obstructEdge, Vector3 crnrA, Vector3 crnrB, Vector3 crnrC, Vector3 crnrD, 
			bool includeOnBorder, ref string dbgReport 
		)
		{
			dbgReport = $"3) DoesEdgeObstructQuadArea({obstructEdge}, includeOnBorder: '{includeOnBorder}')\n";

			string tryMid = "";
			string tryStrt = "";
			string tryEnd = "";

			dbgReport += $"first, checking if start, mid, or end edge positions are in area...\n";
			if //<<<<<<<<<<<<<3
			(
				AmInArea(obstructEdge.MidPosition_flat, crnrA, crnrB, crnrC, crnrD, obstructEdge.V_NavmeshProjectionDirection_cached, includeOnBorder, ref tryMid) ||
				AmInArea(obstructEdge.StartPosition_flat, crnrA, crnrB, crnrC, crnrD, obstructEdge.V_NavmeshProjectionDirection_cached, includeOnBorder, ref tryStrt) ||
				AmInArea(obstructEdge.EndPosition_flat, crnrA, crnrB, crnrC, crnrD, obstructEdge.V_NavmeshProjectionDirection_cached, includeOnBorder, ref tryEnd)
			)
			{
				if( !string.IsNullOrEmpty(tryMid) )
				{
					dbgReport += $"found that the obstruct edge mid point was in quad area\n" +
						$"rprt----------\n" +
						$"{tryMid}\n" +
						$"3) -----------\n" +
						$"returning true...";
				}

				return true;
			}

			dbgReport += $"decided none of the obstruct edge points are in quad area\n" +
				$"Now checking if obstruct edge encomapses entire bridge path...";
			//Todo: note: something's wrong with this check...
			#region Now check if obstructEdge encompasses entire bridge path===================================			
			//Vector3 mid2mid = Vector3.Normalize(FlatVector((crnrA + crnrB) / 2f) - obstructEdge.MidPosition_flat);

			if( 
				VectorsCrossEachOther(
					obstructEdge.StartPosition_flat, obstructEdge.EndPosition_flat, crnrA, crnrD, obstructEdge.V_NavmeshProjectionDirection_cached, false) ||
				VectorsCrossEachOther(
					obstructEdge.StartPosition_flat, obstructEdge.EndPosition_flat, crnrB, crnrC, obstructEdge.V_NavmeshProjectionDirection_cached, false)
			)
			{
				return true;
			}
			#endregion

			return false;
		}

		public static bool DoesEdgeObstructEdgePath(LNX_Edge obstructEdge, LNX_Edge pthStrtEdge, LNX_Edge pthEndEdge, ref string dbgString ) //2
		{
			dbgString = $"2) DoesEdgeObstructEdgePath(obstrctEdg: '{obstructEdge}', strtEdge: '{pthStrtEdge}', endEdge: '{pthEndEdge}')\n" +
				$"\nchecking start/end edge alignment...\n";
			bool pthEdgesAreInAlignment = AreEdgesAlignedFromTheirPerspectives(pthStrtEdge, pthEndEdge);

			dbgString += $"now checking if obstruct edge obstructs the quad area formed between \n" +
				$"'{pthStrtEdge}' and '{pthEndEdge}'...\n";
			string dbgObstructEdgeMethod = "";
			if( pthEdgesAreInAlignment )
			{
				dbgString += $"edges ARE in alignment. using strtEdge.strt, strtEdge.end, endEdge.end, endEdge.Strt...\n";
				if ( DoesEdgeObstructQuadArea( //<<<<<<<<<<<<<2
					obstructEdge, 
					pthStrtEdge.StartPosition_flat, pthStrtEdge.EndPosition_flat, 
					pthEndEdge.EndPosition_flat, pthEndEdge.StartPosition_flat, false, ref dbgObstructEdgeMethod) )
				{
					dbgString += $"Decided one of the obstructEdge points are in quad area.\n" +
					$"\nreport--------------\n" +
					$"{dbgObstructEdgeMethod}" +
					$"2) -------------------\n" +
					$"Returning true...\n";
					return true;
				}
			}
			else
			{
				dbgString += $"edges are NOT in alignment. using strtEdge.strt, strtEdge.end, endEdge.strt, endEdge.end...\n";

				if ( DoesEdgeObstructQuadArea(  //<<<<<<<<<<<<<2
					obstructEdge, 
					pthStrtEdge.StartPosition_flat, pthStrtEdge.EndPosition_flat, 
					pthEndEdge.StartPosition_flat, pthEndEdge.EndPosition_flat, false, ref dbgObstructEdgeMethod) )
				{
					dbgString += $"Decided one of the obstructEdge points are in quad area.\n" +
					$"\nreport------\n" +
					$"{dbgObstructEdgeMethod}" +
					$"--------------\n" +
					$"\nReturning true...\n";
					return true;
				}
			}

			dbgString += $"Apparently no obstruction...\n";

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

		#region SPECIAL NAVMESH METHODS =======================

		/// <summary>
		/// Attempts to project through the LNX_NavMesh, from the startHit position to the endHit position, creating a path 
		/// as it does.
		/// </summary>
		/// <param name="nm"></param>
		/// <param name="startHit"></param>
		/// <param name="endHit"></param>
		/// <param name="outPath"></param>
		/// <returns>True if it's able to project through from start to end, false if it hits an obstructing edge and cannot complete the path.</returns>
		/*public static bool TryProjectPathThrough(LNX_NavMesh nm, LNX_NavmeshHit startHit, LNX_NavmeshHit endHit, out LNX_Path outPath )
		{ //note: started making this as cleaner/faster version. Use this when ready to efficiency test vs the overload after...
			outPath = new LNX_Path();
			outPath.AddPoint( startHit );

			#region SHORT-CIRCUITING =======================================
			if(startHit.TriIndex == endHit.TriIndex )
			{
				outPath.AddPoint( endHit );
				return true;
			}
			#endregion

			int runningTriIndex = startHit.TriIndex;
			Vector3 runningStartPos = startHit.Position;
			int currentEdgeIndex = -1;

			int runningWhileIterations = 0;

			bool amStillProjecting = true;
			while (amStillProjecting)
			{
				string dbgprjct = "";

				LNX_NavmeshHit edgePerimHit = LNX_NavmeshHit.None;

				if( !nm.Triangles[runningTriIndex].ProjectThroughToPerimeter(
					runningStartPos, endHit.Position, out edgePerimHit, ref dbgprjct, currentEdgeIndex, true))
				{
					return false;
				}

				//outPath.AddPoint( new LNX_NavmeshHit(runningTriIndex, edgePerimHit.HitPosition), nm ); //dws
				outPath.AddPoint( edgePerimHit );

				//LNX_Edge hitEdge = nm.Triangles[runningTriIndex].Edges[edgePerimHit.ComponentIndex]; //dws
				currentEdgeIndex = edgePerimHit.ComponentIndex;
				runningStartPos = edgePerimHit.Position;

				if( Vector3.Distance(edgePerimHit.Position, endHit.Position) < 0.001f )
				{
					amStillProjecting = false;
					runningTriIndex = endHit.TriIndex;
				}
				else if ( nm.Triangles[runningTriIndex].Edges[edgePerimHit.ComponentIndex].AmTerminal )
				{
					amStillProjecting = false;
				}
				else
				{
					if
					(
						nm.Triangles[runningTriIndex].Edges[edgePerimHit.ComponentIndex].SharedEdgeCoordinate.TrianglesIndex == endHit.TriIndex ||
						(nm.Triangles[runningTriIndex].AmAdjacentToTri(nm.Triangles[endHit.TriIndex]) && nm.Triangles[runningTriIndex].IsPositionOnAnyEdge(endHit.Position))
					)
					{
						runningTriIndex = endHit.TriIndex;

						if (endHit.Position != edgePerimHit.Position) //In case the end position is on the perimeter of the destination tri...
						{
							outPath.AddPoint( endHit );
						}

						amStillProjecting = false;
					}
					else
					{
						runningTriIndex = nm.Triangles[runningTriIndex].Edges[edgePerimHit.ComponentIndex].SharedEdgeCoordinate.TrianglesIndex;
						currentEdgeIndex = nm.Triangles[runningTriIndex].Edges[edgePerimHit.ComponentIndex].SharedEdgeCoordinate.ComponentIndex;
						runningStartPos = edgePerimHit.Position;

					}
				}

				runningWhileIterations++;
				if (runningWhileIterations > nm.Triangles.Length)
				{
					Debug.LogError($"while loop went for more than '{nm.Triangles.Length}' iterations. Breaking early...");
					amStillProjecting = false;

					return false;
				}
			}

			return runningTriIndex == endHit.TriIndex;
		}
		*/

		public static bool TryProjectThrough(LNX_NavMesh nm, LNX_NavmeshHit startHit, LNX_NavmeshHit endHit, out LNX_Path outPath) //<<<<<
		{
			outPath = new LNX_Path( nm );
			outPath.AddPoint(startHit);

			#region SHORT-CIRCUITING =======================================
			if (startHit.TriangleIndex == endHit.TriangleIndex)
			{
				outPath.AddPoint(endHit);
				return true;
			}
			#endregion

			LNX_NavmeshHit currentStartHit = startHit;
			int safetyTimeout = nm.Triangles.Length;
			int runningWhileIterations = 0;

			bool amStillProjecting = true;
			while (amStillProjecting)
			{
				LNX_NavmeshHit edgePerimHit = LNX_NavmeshHit.None;

				if (
					!nm.Triangles[currentStartHit.TriangleIndex].ProjectThroughToPerimeter(
					currentStartHit, endHit, out edgePerimHit, currentStartHit.EdgeIndex, true)
				)
				{
					return false;
				}

				outPath.AddPoint(edgePerimHit);

				LNX_Edge hitEdge = nm.Triangles[edgePerimHit.TriangleIndex].Edges[edgePerimHit.EdgeIndex];
				currentStartHit = edgePerimHit;

				if (hitEdge.AmTerminal) //if we've hit a wall...
				{
					return false;
				}
				else if (Vector3.Distance(edgePerimHit.Position, endHit.Position) < 0.001f) //if the projection is close enough...
				{
					return true;
				}
				else if (
					hitEdge.TriangleIndex == endHit.TriangleIndex ||
					(
						nm.Triangles[edgePerimHit.TriangleIndex].AmAdjacentToTri(nm.Triangles[endHit.TriangleIndex]) && //this is called first for short-circuiting efficiency
						nm.Triangles[edgePerimHit.TriangleIndex].IsPositionOnAnyEdge(endHit.Position)
					)
				)
				{
					if (endHit.Position != edgePerimHit.Position) //In case the end position is on the perimeter of the destination tri...
					{
						outPath.AddPoint(endHit);
					}

					amStillProjecting = false;
					return true;
				}

				runningWhileIterations++;
				if (runningWhileIterations > safetyTimeout)
				{
					Debug.LogError($"while loop went for more than '{safetyTimeout}' iterations. Breaking early...");
					amStillProjecting = false;

					return false;
				}
				//rprt.Log_Untabbed($"~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
			}

			return false;
		}

		public static bool TryProjectThrough( LNX_NavMesh nm, LNX_Vertex startVert, LNX_Vertex endVert, out LNX_Path outPath, ref string dbgRprt)
		{
			return TryProjectThrough( 
				nm, new LNX_NavmeshHit(startVert, nm.Triangles[startVert.TriangleIndex].V_PathingNormal), 
				new LNX_NavmeshHit(endVert, nm.Triangles[endVert.TriangleIndex].V_PathingNormal), 
				out outPath 
			);
		}

		public static bool TryProjectThrough( LNX_NavMesh nm, LNX_NavmeshHit startHit, LNX_NavmeshHit endHit, ref string dbgRprt )
		{
			dbgRprt = $"ProjectPathThrough({startHit}, {endHit})\n\n";

			#region SHORT-CIRCUITING =======================================
			if (startHit.TriangleIndex == endHit.TriangleIndex)
			{
				dbgRprt += $"tri indices the same for hit objects. Short-circuting by returning true...\n";
				return true;
			}
			#endregion

			LNX_Triangle currentTri = nm.Triangles[startHit.TriangleIndex];
			Vector3 currentStartPos = startHit.Position;
			int currentEdgeIndex = -1;

			int safetyTimeout = nm.Triangles.Length;
			int runningWhileIterations = 0;

			dbgRprt += $"starting with tri{currentTri}...\n\n" +
				$"while-looping to build path==============\n";

			bool amStillProjecting = true;
			while (amStillProjecting)
			{
				dbgRprt += $"\nwhile{runningWhileIterations}...\n" +
					$"\n(currentTri: '{currentTri.Index_inCollection}', startPt: '{LNX_UnitTestUtilities.LongVectorString(currentStartPos)}')\n" +
					$"projecting through triangle...\n";

				string dbgprjct = "";
				LNX_NavmeshHit edgePerimHit = LNX_NavmeshHit.None;
				if ( !currentTri.ProjectThroughToPerimeter(currentStartPos, endHit.Position, out edgePerimHit, ref dbgprjct, currentEdgeIndex) )
				{
					dbgRprt += $"Couldn't project through to perimeter of current tri. now returning...";
					return false;
				}
				dbgRprt += $"Projected through to perim with hit: '{edgePerimHit}'...\n";

				/*DBGRaycast += $"tri.prjctThrToPerim report----------------\n" +
					$"{currentTri.dbg_prjctThrhToPerim}" +
					$"end rprt------------\n";*/

				dbgRprt += $"hit object is okay. Continuing...\n";

				LNX_Edge hitEdge = currentTri.Edges[edgePerimHit.EdgeIndex];
				currentEdgeIndex = edgePerimHit.EdgeIndex;
				currentStartPos = edgePerimHit.Position;

				dbgRprt += $"projected to edge: '{hitEdge.MyCoordinate}' at '{LNX_UnitTestUtilities.LongVectorString(edgePerimHit.Position)}'. " +
				$"Shared edge is: '{hitEdge.SharedEdgeCoordinate}'\n";

				if (hitEdge.AmTerminal)
				{
					dbgRprt += $"hit edge is terminal. Stoping loop...\n";
					amStillProjecting = false;
				}
				else
				{
					dbgRprt += $"edge is NOT terminal. Checking to see if we're at the end...\n";
					dbgRprt += $"test1, Triangles[{currentTri.Index_inCollection}].AmAdjacentToTri({endHit.TriangleIndex}): " +
						$"'{currentTri.AmAdjacentToTri(nm.Triangles[endHit.TriangleIndex])}'\n" +
						$"";

					if
					(
						hitEdge.SharedEdgeCoordinate.TrianglesIndex == endHit.TriangleIndex ||
						(currentTri.AmAdjacentToTri(nm.Triangles[endHit.TriangleIndex]) && currentTri.IsPositionOnAnyEdge(endHit.Position))
					)
					{
						currentTri = nm.Triangles[endHit.TriangleIndex];

						amStillProjecting = false;
						dbgRprt += $"Decided AM at the end. Stopping...\n";
					}
					else
					{
						currentTri = nm.Triangles[hitEdge.SharedEdgeCoordinate.TrianglesIndex];
						currentEdgeIndex = hitEdge.SharedEdgeCoordinate.ComponentIndex;
						currentStartPos = edgePerimHit.Position;

						dbgRprt += $"Decided NOT at the end. Set new current tri to: '{currentTri.Index_inCollection}'...\n";
					}
				}

				runningWhileIterations++;
				if (runningWhileIterations > safetyTimeout)
				{
					Debug.LogError($"while loop went for more than '{safetyTimeout}' iterations. Breaking early...");
					amStillProjecting = false;

					return false;
				}
			}

			return currentTri.Index_inCollection == endHit.TriangleIndex;
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

			LNX_Triangle primaryTri = nm.GetTriangle(primaryEdge.MyCoordinate);
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

		#region FILE/DATA ===========================================
		public static string MakePathFromString(string str, string splitterString, int endStop = 0 )
		{
			string rtrnString = "";

			string[] lines = str.Split( splitterString ); //at this moment, this string will have forward-slash separators. After re-combining using Path.Combine(), the string will have back-slash separators...
			for ( int i = 0; i < lines.Length - endStop; i++ )
			{
				rtrnString = Path.Combine(rtrnString, lines[i]);
			}

			return rtrnString;
		}

		public static string AppendDigitTilUniqueFileName( string fileNameString, string extensionString, string dirPathString )
		{
			string cmbnd = Path.Combine( dirPathString, fileNameString );

			if ( !File.Exists($"{cmbnd}{extensionString}") )
			{
				return cmbnd + extensionString;
			}
			else
			{
				for ( int i = 0; i < 100; i++ )
				{
					string s = $"{cmbnd}{i}{extensionString}";
					if ( !File.Exists(s) )
					{
						return s;
					}
				}
			}

			return string.Empty;
		}
		#endregion

	}
}