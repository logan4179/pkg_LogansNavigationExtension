using System;
using UnityEngine;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_Edge
	{
		public LNX_ComponentCoordinate MyCoordinate;
		
		//[Header("CACHED")]
		public Vector3 StartPosition;
		public LNX_ComponentCoordinate StartVertCoordinate;
		public Vector3 EndPosition;
		public LNX_ComponentCoordinate EndVertCoordinate;
		/// <summary> Currently set in the Triangle relationship constructor</summary>
		public LNX_ComponentCoordinate SharedEdgeCoordinate;
		/// <summary>Cached center vector for the owning triangle. This is for exposed property calculation </summary>
		[SerializeField, HideInInspector] private Vector3 v_triCenter_cached;

		/// <summary>Vector perpendicular to this edge, and to the side 
		///  that points inside of the owning triangle</summary>
		public Vector3 v_Cross;

		/// <summary>A corrected cross vector for use with flat operations where v_Cross won't be quite accurate enough.</summary>
		public Vector3 v_Cross_flat; //note, I decided to cache this value instead of calculating on the fly because it gets used potentially alot/continuously in projection operations

		//[Header("PROPERTIES")]
		public Vector3 MidPosition => (StartPosition + EndPosition) / 2f;

		public Vector3 V_StartToEnd => Vector3.Normalize(EndPosition - StartPosition);

		public Vector3 V_EndToStart => Vector3.Normalize(StartPosition - EndPosition);

		public Vector3 v_toCenter => Vector3.Normalize( v_triCenter_cached - MidPosition );
		public float EdgeLength => Vector3.Distance(StartPosition, EndPosition);
		public bool AmTerminal => SharedEdgeCoordinate == LNX_ComponentCoordinate.None;

		public int TriangleIndex => MyCoordinate.TrianglesIndex;
		public int ComponentIndex => MyCoordinate.ComponentIndex;

		public LNX_Edge( LNX_Triangle ownerTri, LNX_Vertex strtVrt, LNX_Vertex endVrt, int triIndx, int cmptIndx )
		{
			Debug.Log($"ctor. edge: '{ownerTri.Index_inCollection},{cmptIndx}', passed tri ctr: '{ownerTri.V_Center}'");
			//StartPosition = strtVrt.V_Position;
			//EndPosition = endVrt.V_Position;

			MyCoordinate = new LNX_ComponentCoordinate( triIndx, cmptIndx );

			StartVertCoordinate = strtVrt.MyCoordinate;
			EndVertCoordinate = endVrt.MyCoordinate;

			v_triCenter_cached = ownerTri.V_Center;

			SharedEdgeCoordinate = LNX_ComponentCoordinate.None;
		}

		public LNX_Edge( LNX_Edge edge )
		{
			StartPosition = edge.StartPosition;
			StartVertCoordinate = edge.StartVertCoordinate;
			EndPosition = edge.EndPosition;
			EndVertCoordinate = edge.EndVertCoordinate;

			v_Cross = edge.v_Cross;

			MyCoordinate = edge.MyCoordinate;

			SharedEdgeCoordinate = edge.SharedEdgeCoordinate;
		}

		public void AdoptValues(LNX_Edge edge)
		{
			StartPosition = edge.StartPosition;
			StartVertCoordinate = edge.StartVertCoordinate;
			EndPosition = edge.EndPosition;
			EndVertCoordinate = edge.EndVertCoordinate;

			v_Cross = edge.v_Cross;

			MyCoordinate = edge.MyCoordinate;

			SharedEdgeCoordinate = edge.SharedEdgeCoordinate;
		}

		public void CreateRelationships( LNX_NavMesh nvmsh ) //todo: unit test
		{
			for ( int i = 0; i < nvmsh.Triangles.Length; i++ )
			{
				if ( i == MyCoordinate.TrianglesIndex )
				{
					continue;
				}

				if ( AmOnSharedEdgeSpace(nvmsh.Triangles[i].Edges[0]) )
				{
					SharedEdgeCoordinate = nvmsh.Triangles[i].Edges[0].MyCoordinate;
					break;
				}
				else if ( AmOnSharedEdgeSpace(nvmsh.Triangles[i].Edges[1]) )
				{
					SharedEdgeCoordinate = nvmsh.Triangles[i].Edges[1].MyCoordinate;
					break;
				}
				else if ( AmOnSharedEdgeSpace(nvmsh.Triangles[i].Edges[2]) )
				{
					SharedEdgeCoordinate = nvmsh.Triangles[i].Edges[2].MyCoordinate;
					break;
				}
			}
		}

		public string DBG_vcross;
		public void CalculateDerivedInfo( LNX_Triangle tri )
		{
			StartPosition = tri.Verts[StartVertCoordinate.ComponentIndex].V_Position;
			EndPosition = tri.Verts[EndVertCoordinate.ComponentIndex].V_Position;

			v_Cross = Vector3.Cross(V_StartToEnd, tri.V_PlaneFaceNormal).normalized;

			if ( Vector3.Dot(v_Cross, v_toCenter) < 0 )
			{
				v_Cross = -v_Cross;
			}

			/*
			Note: My testing has found that the following calculation results in a subtly different value than just flattening the
			above v_Cross value. If I simply flattened that value, edge projecting didn't work at very acute angles for triangles
			with slanted surfaces.
			*/
			v_Cross_flat = Vector3.Cross 
			( 
				LNX_Utils.FlatVector(V_StartToEnd, tri.v_SurfaceNormal_cached).normalized, tri.v_SurfaceNormal_cached
			).normalized;
		}

		public void TriIndexChanged( int newIndex )
		{
			MyCoordinate = new LNX_ComponentCoordinate( newIndex, MyCoordinate.ComponentIndex );

			StartVertCoordinate = new LNX_ComponentCoordinate( newIndex, StartVertCoordinate.ComponentIndex);

			EndVertCoordinate = new LNX_ComponentCoordinate( newIndex, EndVertCoordinate.ComponentIndex);
		}

		/// <summary>
		/// Returns the index of the vertex whose angle is opposite this edge
		/// </summary>
		/// <returns></returns>
		public int GetIndexOfSineVert()
		{
			int opposingVertIndex = 0;

			if ( StartVertCoordinate.ComponentIndex != 1 && EndVertCoordinate.ComponentIndex != 1)
			{
				opposingVertIndex = 1;
			}
			else if (StartVertCoordinate.ComponentIndex != 2 && EndVertCoordinate.ComponentIndex != 2)
			{
				opposingVertIndex = 2;
			}

			return opposingVertIndex;
		}

		#region API METHODS-----------------------------
		public Vector3 ClosestPointOnEdge(Vector3 pos)
		{
			Vector3 v_vrtToPos = pos - StartPosition;

			Vector3 v_result = StartPosition + Vector3.Project(v_vrtToPos, V_StartToEnd);

			float dist_startToRslt = Vector3.Distance(v_result, StartPosition);
			float dist_endToRslt = Vector3.Distance(v_result, EndPosition);

			//Debug.Log($"dist_startToRslt: '{dist_startToRslt}', dist_endToRslt: '{dist_endToRslt}', len: '{EdgeLength}'");
			if (dist_startToRslt > EdgeLength || dist_endToRslt > EdgeLength)
			{
				//Debug.Log("if");
				v_result = dist_startToRslt < dist_endToRslt ? StartPosition : EndPosition;
			}

			return v_result;
		}

		public bool DoesPositionLieOnEdge( Vector3 pos, Vector3 flattenDir )
		{
			if
			( 
				pos == StartPosition || pos == EndPosition ||
				(
					LNX_Utils.FlatVector(pos - StartPosition, flattenDir).normalized == LNX_Utils.FlatVector(V_StartToEnd, flattenDir).normalized && 
					Vector3.Distance(StartPosition,pos) <= EdgeLength
				) ||
				(
					LNX_Utils.FlatVector(pos - EndPosition, flattenDir).normalized == -LNX_Utils.FlatVector(V_StartToEnd, flattenDir).normalized && 
					Vector3.Distance(EndPosition,pos) <= EdgeLength
				)
			)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		[NonSerialized] public string dbg_doesProjectionIntersectEdge;
		/// <summary>
		/// Returns whether a projection from origin to direction will intersect this edge.
		/// </summary>
		/// <param name="origin">Start of projection</param>
		/// <param name="destination">End of projection</param>
		/// <param name="flattenDir">Which direction (axis) to exclude. This shold be the direction that the navmesh surface is oriented (facing up)</param>
		/// <param name="dbgString"></param>
		/// <param name="outPos"></param>
		/// <returns></returns>
		public bool DoesProjectionIntersectEdge( 
			Vector3 origin, Vector3 destination, Vector3 flattenDir, out Vector3 outPos 
		)
		{
			Vector3 v_projection = LNX_Utils.FlatVector( destination - origin, flattenDir ).normalized;
			Vector3 v_originToStart = LNX_Utils.FlatVector( StartPosition - origin, flattenDir ).normalized;
			Vector3 v_originToEnd = LNX_Utils.FlatVector( EndPosition - origin ).normalized;

			#region DIRECTIONAL SHORT-CIRCUIT TEST-------------------------------------------------
			//The following tests if the origin and projection direction allow for the possibilty of edge intersection...
			Vector3 v_edgeMid_toOriginPt = LNX_Utils.FlatVector( origin - MidPosition ).normalized;

			dbg_doesProjectionIntersectEdge += $"directional short circuit test\n" +
				$"{nameof(v_Cross_flat)}: '{v_Cross_flat}', {nameof(v_edgeMid_toOriginPt)}: '{v_edgeMid_toOriginPt}'\n" +
				$"side check rslt: '{Vector3.Dot(v_Cross_flat, v_edgeMid_toOriginPt)}'...\n";

			if ( Vector3.Dot(v_Cross_flat, v_edgeMid_toOriginPt) >= 0f ) //origin is towards "inside" direction of edge...
			{
				dbg_doesProjectionIntersectEdge += $"origin is towards 'inside' direction of edge. now checking that " +
					$"the projection is in correct dir:...\n" +
					$"comparing '{v_projection}' with '{-v_Cross_flat}'. rslt: '{Vector3.Dot(v_projection, -v_Cross_flat)}'\n";

				if ( Vector3.Dot(v_projection, -v_Cross_flat) < 0f )
				{
					dbg_doesProjectionIntersectEdge += $"!!! Operation short-circuited bc of containment check! Returning false...\n" +
						$"info dump: {nameof(origin)}: '{LNX_UnitTestUtilities.LongVectorString(origin)}'\n" +
						$"dest: '{LNX_UnitTestUtilities.LongVectorString(destination)}'\n" +
						$"{nameof(v_Cross)}: '{LNX_UnitTestUtilities.LongVectorString(v_Cross)}\n" +
						$"{nameof(v_Cross_flat)}: '{LNX_UnitTestUtilities.LongVectorString(v_Cross_flat)}\n" +
						$"{nameof(v_edgeMid_toOriginPt)}: '{LNX_UnitTestUtilities.LongVectorString(v_edgeMid_toOriginPt)}\n" +
						$"";
					outPos = Vector3.zero;
					return false; //short-circuit
				}
			}
			else //origin is towards "outside" direction of edge...
			{
				dbg_doesProjectionIntersectEdge += $"origin is towards 'outside' direction of edge. now checking that " +
					$"the projection is in correct dir:...\n" +
					$"comparing '{v_projection}' with '{v_Cross_flat}'. rslt: '{Vector3.Dot(v_projection, v_Cross_flat)}'\n";

				if ( Vector3.Dot(v_projection, v_Cross_flat) < 0f )
				{
					dbg_doesProjectionIntersectEdge += $"!!! Operation short-circuited bc of containment check! Returning false...\n" +
						$"info dump: {nameof(origin)}: '{LNX_UnitTestUtilities.LongVectorString(origin)}'\n" +
						$"dest: '{LNX_UnitTestUtilities.LongVectorString(destination)}'\n" +
						$"{nameof(v_Cross)}: '{LNX_UnitTestUtilities.LongVectorString(v_Cross)}\n" +
						$"{nameof(v_Cross_flat)}: '{LNX_UnitTestUtilities.LongVectorString(v_Cross_flat)}\n" +
						$"{nameof(v_edgeMid_toOriginPt)}: '{LNX_UnitTestUtilities.LongVectorString(v_edgeMid_toOriginPt)}\n" +
						$""; 
					outPos = Vector3.zero;
					return false; //short-circuit
				}
			}
			#endregion

			#region ANGULAR SHORT-CIRCUIT TEST-------------------------------------------------------
			float ang_prjctTo_orgnToStrt = Vector3.Angle(v_projection, v_originToStart);
			float ang_prjctTo_orgnToEnd = Vector3.Angle(v_projection, v_originToEnd);
			//float ang_chevron = ang_prjctTo_orgnToStrt + ang_prjctTo_orgnToEnd; //this is cheap, but is it right?
			float ang_chevron = Vector3.Angle(v_originToStart, v_originToEnd);

			float lrgst = Mathf.Max
			(
				ang_prjctTo_orgnToStrt,
				ang_prjctTo_orgnToEnd
			);

			dbg_doesProjectionIntersectEdge += $"\n" +
				$"Angular short-circuit test\n" +
				$"trying angle short-circuit with rslts. 1: '{ang_prjctTo_orgnToStrt}', " +
				$"2: '{ang_prjctTo_orgnToEnd}'...\n" +
				$"chev: '{ang_chevron}', lrgst: '{lrgst}'...";

			if (
				(ang_prjctTo_orgnToStrt > 90f && ang_prjctTo_orgnToEnd > 90f) ||
				lrgst > ang_chevron
			)
			{
				dbg_doesProjectionIntersectEdge += $"\nOperation short-circuited bc of dot-prdct check! Returning false...\n";
				outPos = Vector3.zero;
				return false; //short-circuit
			}
			else
			{
				dbg_doesProjectionIntersectEdge += $"no short-circuit. Method will continue...\n";
			}

			#endregion

			#region CALCULATE OUT POS -----------------------------------------------------------
			outPos = StartPosition + 
			(
				V_StartToEnd * LNX_Utils.CalculateTriangleEdgeLength
				(
					Vector3.Angle(v_projection, v_originToStart),
					Vector3.Angle(-v_projection, -V_StartToEnd),
					Vector3.Distance(origin, StartPosition)
				)
			); //Todo: This length isn't actually accurate at this point because we're using flattened positions in here (as well as mixing with unflattened)
			#endregion

			return true;
		}

		/*
		public bool DoesProjectionIntersectEdge //Use this for experimentation
		(
			Vector3 origin, Vector3 destination, Vector3 flattenDir, out Vector3 outPos
		)
		{
			dbg_doesProjectionIntersectEdge = $"{nameof(DoesProjectionIntersectEdge)}{MyCoordinate.ComponentIndex}()-----\n" +
				$"{nameof(origin)}: '{origin}', {nameof(destination)}: '{destination}', {nameof(flattenDir)}: '{flattenDir}'\n";
			Vector3 v_projection = LNX_Utils.FlatVector(destination - origin, flattenDir).normalized;
			Vector3 v_originToStart = LNX_Utils.FlatVector(StartPosition - origin, flattenDir).normalized;
			Vector3 v_originToEnd = LNX_Utils.FlatVector(EndPosition - origin).normalized;
			dbg_doesProjectionIntersectEdge += $"{nameof(v_projection)}: '{v_projection}'\n";

			#region DIRECTIONAL SHORT-CIRCUIT TEST-------------------------------------------------
			//The following tests if the origin and projection direction allow for the possibilty of edge intersection...
			Vector3 v_cross_flat = LNX_Utils.FlatVector(v_Cross, flattenDir).normalized;
			Vector3 v_edgeMid_toOriginPt = LNX_Utils.FlatVector(origin - MidPosition).normalized;

			float checkThis = Vector3.Dot(LNX_Utils.FlatVector(V_StartToEnd).normalized, v_cross_flat);
			float checkThisB = Vector3.Dot(V_StartToEnd.normalized, v_Cross.normalized);

			dbg_doesProjectionIntersectEdge += $"directional short circuit test\n" +
				$"{nameof(v_cross_flat)}: '{v_cross_flat}', {nameof(v_edgeMid_toOriginPt)}: '{v_edgeMid_toOriginPt}'\n" +
				$"check: '{checkThis}', check2: '{checkThisB}'\n" +
				$"side check rslt: '{Vector3.Dot(v_cross_flat, v_edgeMid_toOriginPt)}'...\n";

			if (Vector3.Dot(v_cross_flat, v_edgeMid_toOriginPt) >= 0f) //origin is towards "inside" direction of edge...
			{
				dbg_doesProjectionIntersectEdge += $"origin is towards 'inside' direction of edge. now checking that " +
					$"the projection is in correct dir:...\n" +
					$"comparing '{v_projection}' with '{-v_cross_flat}'. rslt: '{Vector3.Dot(v_projection, -v_cross_flat)}'\n";

				if (Vector3.Dot(v_projection, -v_cross_flat) < 0f)
				{
					dbg_doesProjectionIntersectEdge += $"!!! Operation short-circuited bc of containment check! Returning false...\n" +
						$"info dump: {nameof(origin)}: '{LNX_UnitTestUtilities.LongVectorString(origin)}'\n" +
						$"dest: '{LNX_UnitTestUtilities.LongVectorString(destination)}'\n" +
						$"{nameof(v_Cross)}: '{LNX_UnitTestUtilities.LongVectorString(v_Cross)}\n" +
						$"{nameof(v_cross_flat)}: '{LNX_UnitTestUtilities.LongVectorString(v_cross_flat)}\n" +
						$"{nameof(v_edgeMid_toOriginPt)}: '{LNX_UnitTestUtilities.LongVectorString(v_edgeMid_toOriginPt)}\n" +
						$"";
					outPos = Vector3.zero;
					return false; //short-circuit
				}
			}
			else //origin is towards "outside" direction of edge...
			{
				dbg_doesProjectionIntersectEdge += $"origin is towards 'outside' direction of edge. now checking that " +
					$"the projection is in correct dir:...\n" +
					$"comparing '{v_projection}' with '{v_cross_flat}'. rslt: '{Vector3.Dot(v_projection, v_cross_flat)}'\n";

				if (Vector3.Dot(v_projection, v_cross_flat) < 0f)
				{
					dbg_doesProjectionIntersectEdge += $"!!! Operation short-circuited bc of containment check! Returning false...\n" +
						$"info dump: {nameof(origin)}: '{LNX_UnitTestUtilities.LongVectorString(origin)}'\n" +
						$"dest: '{LNX_UnitTestUtilities.LongVectorString(destination)}'\n" +
						$"{nameof(v_Cross)}: '{LNX_UnitTestUtilities.LongVectorString(v_Cross)}\n" +
						$"{nameof(v_cross_flat)}: '{LNX_UnitTestUtilities.LongVectorString(v_cross_flat)}\n" +
						$"{nameof(v_edgeMid_toOriginPt)}: '{LNX_UnitTestUtilities.LongVectorString(v_edgeMid_toOriginPt)}\n" +
						$"";
					outPos = Vector3.zero;
					return false; //short-circuit
				}
			}
			#endregion

			#region ANGULAR SHORT-CIRCUIT TEST-------------------------------------------------------
			float ang_prjctTo_orgnToStrt = Vector3.Angle(v_projection, v_originToStart);
			float ang_prjctTo_orgnToEnd = Vector3.Angle(v_projection, v_originToEnd);
			//float ang_chevron = ang_prjctTo_orgnToStrt + ang_prjctTo_orgnToEnd; //this is cheap, but is it right?
			float ang_chevron = Vector3.Angle(v_originToStart, v_originToEnd);

			float lrgst = Mathf.Max
			(
				ang_prjctTo_orgnToStrt,
				ang_prjctTo_orgnToEnd
			);

			dbg_doesProjectionIntersectEdge += $"\n" +
				$"Angular short-circuit test\n" +
				$"trying angle short-circuit with rslts. 1: '{ang_prjctTo_orgnToStrt}', " +
				$"2: '{ang_prjctTo_orgnToEnd}'...\n" +
				$"chev: '{ang_chevron}', lrgst: '{lrgst}'...";

			if (
				(ang_prjctTo_orgnToStrt > 90f && ang_prjctTo_orgnToEnd > 90f) ||
				lrgst > ang_chevron
			)
			{
				dbg_doesProjectionIntersectEdge += $"\nOperation short-circuited bc of dot-prdct check! Returning false...\n";
				outPos = Vector3.zero;
				return false; //short-circuit
			}
			else
			{
				dbg_doesProjectionIntersectEdge += $"no short-circuit. Method will continue...\n";
			}

			#endregion

			#region CALCULATE OUT POS -----------------------------------------------------------
			outPos = StartPosition +
			(
				V_StartToEnd * LNX_Utils.CalculateTriangleEdgeLength
				(
					Vector3.Angle(v_projection, v_originToStart),
					Vector3.Angle(-v_projection, -V_StartToEnd),
					Vector3.Distance(origin, StartPosition)
				)
			); //Todo: This length isn't actually accurate at this point because we're using flattened positions in here (as well as mixing with unflattened)
			#endregion

			return true;
		}
		*/

		/// <summary>
		/// Checks whether the supplied edge is touching this edge at both the start 
		/// and end position.
		/// </summary>
		/// <param name="edge"></param>
		/// <returns></returns>
		public bool AmTouching( LNX_Edge edge )
		{
			if( edge.StartPosition == StartPosition || edge.EndPosition == StartPosition )
			{
				return true;
			}

			if( edge.EndPosition == EndPosition || edge.EndPosition == StartPosition )
			{
				return true;
			}

			return false;
		}
		#endregion

		public bool AmOnSharedEdgeSpace( LNX_Edge edj )
		{
			if (
				(edj.StartPosition == StartPosition || edj.EndPosition == StartPosition) &&
				(edj.StartPosition == EndPosition || edj.EndPosition == EndPosition)
			)
			{
				return true;
			}

			return false;
		}

		#region HELPERS --------------------------------------------------
		public void SayCurrentInfo()
		{
			Debug.Log($"Edge.{nameof(SayCurrentInfo)}()\n" +
				$"{nameof(MyCoordinate)}: '{MyCoordinate}'\n" +
				$"{nameof(StartPosition)}: '{StartPosition}'\n" +
				$"{nameof(v_Cross)}: '{v_Cross}'\n" +
				$"");
		}

		public string GetAnomolyString()
		{
			string returnString = string.Empty;

			if ( MyCoordinate.TrianglesIndex < 0 || MyCoordinate.ComponentIndex < 0 )
			{
				returnString += $"{nameof(MyCoordinate)}: '{MyCoordinate}'\n";
			}

			if ( StartPosition == Vector3.zero )
			{
				returnString += $"{nameof(StartPosition)}: '{StartPosition}'\n";
			}

			if ( EndPosition == Vector3.zero)
			{
				returnString += $"{nameof(EndPosition)}: '{EndPosition}'\n";
			}

			if ( v_triCenter_cached == Vector3.zero )
			{
				returnString += $"{nameof(v_triCenter_cached)}: '{v_triCenter_cached}'\n";
			}

			if (v_Cross == Vector3.zero)
			{
				returnString += $"{nameof(v_Cross)}: '{v_Cross}'\n";
			}

			if (v_Cross_flat == Vector3.zero)
			{
				returnString += $"{nameof(v_Cross_flat)}: '{v_Cross_flat}'\n";
			}

			return returnString;
		}
		#endregion
	}
}