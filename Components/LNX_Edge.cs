using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace LogansNavigationExtension
{
	/// <summary>
	/// Class that creates an edge object composing a triangle.
	/// Note: On an LNX_Triangle, Edge0 will start at vert1 and end at vert2.
	/// Edge1 will start on vert0 and end at vert2. Edge2 will start at 
	/// vert0 and end at vert1
	/// </summary>
	[System.Serializable]
	public class LNX_Edge
	{
		public LNX_ComponentCoordinate MyCoordinate;
		
		//[Header("CACHED")]
		public Vector3 StartPosition;
		public Vector3 StartPosition_flat => LNX_Utils.FlatVector(StartPosition, v_SurfaceNormal_cached);
		//public LNX_ComponentCoordinate StartVertCoordinate; //Trying turning this into property so it's no longer serialized, dws
		public LNX_ComponentCoordinate StartVertCoordinate => 
			new LNX_ComponentCoordinate( MyCoordinate.TrianglesIndex, MyCoordinate.ComponentIndex == 0 ? 1 : 0 ); //Check out the LNX_Triangle ctor where the edges are made to understand this
		public int StartVertIndex => MyCoordinate.ComponentIndex == 0 ? 1 : 0;

		public Vector3 EndPosition;
		public Vector3 EndPosition_flat => LNX_Utils.FlatVector(EndPosition, v_SurfaceNormal_cached);
		//public LNX_ComponentCoordinate EndVertCoordinate; //Trying turning this into property so it's no longer serialized, dws
		public LNX_ComponentCoordinate EndVertCoordinate => 
			new LNX_ComponentCoordinate(MyCoordinate.TrianglesIndex, MyCoordinate.ComponentIndex == 2 ? 1 : 2); //Check out the LNX_Triangle ctor where the edges are made to understand this
		public int EndVertIndex => MyCoordinate.ComponentIndex == 2 ? 1 : 2;

		/// <summary> Currently set in the Triangle relationship constructor</summary>
		public LNX_ComponentCoordinate SharedEdgeCoordinate;

		/// <summary>Cached center vector for the owning triangle. This is for exposed property calculation </summary>
		[SerializeField, HideInInspector] private Vector3 v_triCenter_cached;

		[SerializeField, HideInInspector] private Vector3 v_SurfaceNormal_cached;
		public Vector3 V_SurfaceNormal_cached => v_SurfaceNormal_cached;

		/// <summary>Vector perpendicular to this edge, and to the side 
		///  that points inside of the owning triangle</summary>
		public Vector3 v_Cross;

		/// <summary>A corrected cross vector for use with flat operations where v_Cross won't be quite accurate enough.</summary>
		public Vector3 v_Cross_flat; //note, I decided to cache this value instead of calculating on the fly because it gets used potentially alot/continuously in projection operations

		//[Header("PROPERTIES")]
		public Vector3 MidPosition => (StartPosition + EndPosition) / 2f;
		public Vector3 MidPosition_flat => LNX_Utils.FlatVector((StartPosition + EndPosition) / 2f, v_SurfaceNormal_cached);

		public Vector3 V_StartToEnd => Vector3.Normalize(EndPosition - StartPosition);

		public Vector3 V_StartToEnd_flattened => Vector3.Normalize( EndPosition_flat - StartPosition_flat );

		public Vector3 V_EndToStart => Vector3.Normalize(StartPosition - EndPosition);
		public Vector3 V_EndToStart_flattened => Vector3.Normalize( StartPosition_flat - EndPosition_flat );

		public Vector3 v_toCenter => Vector3.Normalize( v_triCenter_cached - MidPosition );
		public float EdgeLength => Vector3.Distance(StartPosition, EndPosition);
		public bool AmTerminal => SharedEdgeCoordinate == LNX_ComponentCoordinate.None;

		public int TriangleIndex => MyCoordinate.TrianglesIndex;
		public int ComponentIndex => MyCoordinate.ComponentIndex;

		public LNX_Edge( List<LNX_AtomicTriangle> atomicTris, LNX_Triangle ownerTri, LNX_Vertex strtVrt, LNX_Vertex endVrt, int triIndx, int cmptIndx )
		{
			Debug.Log($"ctor. edge: '{ownerTri.Index_inCollection},{cmptIndx}', passed tri ctr: '{ownerTri.V_Center}'");
			//StartPosition = strtVrt.V_Position;
			//EndPosition = endVrt.V_Position;

			MyCoordinate = new LNX_ComponentCoordinate( triIndx, cmptIndx );

			//StartVertCoordinate = strtVrt.MyCoordinate;
			//EndVertCoordinate = endVrt.MyCoordinate;

			v_triCenter_cached = ownerTri.V_Center;
			v_SurfaceNormal_cached = ownerTri.v_SurfaceNormal_cached;

			SharedEdgeCoordinate = LNX_ComponentCoordinate.None;
		}

		public LNX_Edge( LNX_Edge edge )
		{
			StartPosition = edge.StartPosition;
			//StartVertCoordinate = edge.StartVertCoordinate;
			EndPosition = edge.EndPosition;
			//EndVertCoordinate = edge.EndVertCoordinate;

			v_Cross = edge.v_Cross;

			MyCoordinate = edge.MyCoordinate;

			SharedEdgeCoordinate = edge.SharedEdgeCoordinate;
		}

		public void AdoptValues(LNX_Edge edge)
		{
			StartPosition = edge.StartPosition;
			//StartVertCoordinate = edge.StartVertCoordinate;
			EndPosition = edge.EndPosition;
			//EndVertCoordinate = edge.EndVertCoordinate;

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
		public void CalculateDerivedInfo( LNX_Triangle tri, LNX_NavMesh nm )
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
			with slanted surfaces, so now I'm doing this, which works better...
			*/
			v_Cross_flat = LNX_Utils.FlatVector
			( 
				v_Cross, tri.v_SurfaceNormal_cached
			).normalized;
		}

		public void TriIndexChanged( int newIndex )
		{
			MyCoordinate = new LNX_ComponentCoordinate( newIndex, MyCoordinate.ComponentIndex );

			//StartVertCoordinate = new LNX_ComponentCoordinate( newIndex, StartVertCoordinate.ComponentIndex);

			//EndVertCoordinate = new LNX_ComponentCoordinate( newIndex, EndVertCoordinate.ComponentIndex);
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
			Vector3 v_edge = EndPosition - StartPosition;

			#region SHORT-CIRCUIT ====================================
			//TODO: efficiency test this method with and without this check to determine how much this check costs. Note: this check 
			// WILL be triggered in LNX_Triangle.ProjectThroughToPerimeter() in the overload that takes in LNX_Hits as parameters 
			// when called by LNX_Utils.TryProjectPathThrough()
			if ( v_vrtToPos.normalized == v_edge.normalized ) //this works bc both of these vectors are calcualted from 'StartPosition'
			{
				if ( v_vrtToPos.magnitude <= v_edge.magnitude )
				{
					return pos;
				}
				else
				{
					return EndPosition;
				}
			}
			#endregion

			Vector3 v_result = StartPosition + Vector3.Project( v_vrtToPos, v_edge.normalized );

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

		/// <summary>
		/// Whether a supplied position lies on this edge in a 'flattened' manor. IE: whether the position 
		/// lies on the edge from the perspective of the surface normal direction. Note: returns false if 
		/// the position lies on the direction of the edge, but beyond the start or end points.
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public bool DoesPositionLieOnEdge( Vector3 pos )
		{
			if( pos == StartPosition || pos == EndPosition || pos == MidPosition )
			{
				return true;
			}

			Vector3 pos_fltnd = LNX_Utils.FlatVector(pos, v_SurfaceNormal_cached);
			if( pos_fltnd == StartPosition_flat || pos_fltnd == EndPosition_flat || pos_fltnd == MidPosition_flat )
			{
				return true;
			}

			if
			( 
				(
					LNX_Utils.FlatVector(pos - StartPosition, v_SurfaceNormal_cached).normalized == LNX_Utils.FlatVector(V_StartToEnd, v_SurfaceNormal_cached).normalized && 
					Vector3.Distance(StartPosition,pos) <= EdgeLength
				) ||
				(
					LNX_Utils.FlatVector(pos - EndPosition, v_SurfaceNormal_cached).normalized == -LNX_Utils.FlatVector(V_StartToEnd, v_SurfaceNormal_cached).normalized && 
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

		//TODO for following method: considering this method is supposed to find a position that's definitely 
		//on the surface, and that it's being used by the LNX_Triangle.ProjectThroughToPerimeter() method to
		//ultimately create an LNX_Hit object, I believe that instead of an out vector ("outPos"), it should
		//take a reference to an LNX_Hit object, or at least have an overload that does
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
			Vector3 origin, Vector3 destination, out LNX_NavmeshHit outHit, bool endPointInclusive = true
		)
		{
			Vector3 v_projection = LNX_Utils.FlatVector( destination - origin, v_SurfaceNormal_cached ).normalized;
			Vector3 v_originToStart = LNX_Utils.FlatVector( StartPosition - origin, v_SurfaceNormal_cached).normalized;
			Vector3 v_originToEnd = LNX_Utils.FlatVector( EndPosition - origin ).normalized;

			#region ALIGNMENT SHORT-CIRCUIT TEST-------------------------------------------------
			//The following tests if the origin and projection direction allow for the possibilty of edge intersection...
			Vector3 v_edgeMid_toOriginPt = LNX_Utils.FlatVector( origin - MidPosition ).normalized;
			float dot_vCross_with_edgeMidPtToOriginPt = Vector3.Dot(v_Cross_flat, v_edgeMid_toOriginPt);

			if ( dot_vCross_with_edgeMidPtToOriginPt > 0f ) //origin is towards "inside" direction of edge...
			{
				if ( Vector3.Dot(v_projection, v_Cross_flat) > 0f ) //...and the projection is also pointed inside the triangle...
				{
					outHit = LNX_NavmeshHit.None;
					return false; //short-circuit
				}
			}
			else if ( dot_vCross_with_edgeMidPtToOriginPt < 0 ) //origin is towards "OUTSIDE" direction of edge...
			{
				if ( Vector3.Dot(v_projection, v_Cross_flat) < 0f ) //...and the projection is also towards the outside direction of the triangle
				{
					outHit = LNX_NavmeshHit.None;
					return false; //short-circuit
				}
			}
			#endregion

			#region ANGULAR SHORT-CIRCUIT TEST-------------------------------------------------------
			float ang_vprjctTo_orgnToStrt = Vector3.Angle(v_projection, v_originToStart);
			float ang_prjctTo_orgnToEnd = Vector3.Angle(v_projection, v_originToEnd);
			//float ang_chevron = ang_prjctTo_orgnToStrt + ang_prjctTo_orgnToEnd; //this is cheap, but is it right?
			float ang_chevron = Vector3.Angle(v_originToStart, v_originToEnd);

			float lrgst = Mathf.Max
			(
				ang_vprjctTo_orgnToStrt,
				ang_prjctTo_orgnToEnd
			);

			if (
				(ang_vprjctTo_orgnToStrt > 90f && ang_prjctTo_orgnToEnd > 90f) ||
				lrgst > ang_chevron
			)
			{
				//dbg_doesProjectionIntersectEdge += $"\nOperation short-circuited bc of dot-prdct check! Returning false...\n";
				outHit = LNX_NavmeshHit.None;
				return false; //short-circuit
			}
			#endregion

			if ( endPointInclusive )
			{
				if( DoesPositionLieOnEdge(origin) )
				{
					outHit = new LNX_NavmeshHit(
						StartPosition +
						(V_StartToEnd * LNX_Utils.CalculateTriangleEdgeLength(
							90f,
							180f - 90f - Vector3.Angle(V_StartToEnd_flattened, V_StartToEnd),
							Vector3.Distance(StartPosition_flat, LNX_Utils.FlatVector(origin, v_SurfaceNormal_cached))
							)
						), 
						MyCoordinate, v_SurfaceNormal_cached
					);
				}
			}

			#region CALCULATE OUT POS -----------------------------------------------------------
			outHit = new LNX_NavmeshHit(
				StartPosition + 
				(
					V_StartToEnd * LNX_Utils.CalculateTriangleEdgeLength
					(
						Vector3.Angle(v_projection, v_originToStart),
						Vector3.Angle(-v_projection, -V_StartToEnd),
						Vector3.Distance(origin, StartPosition)
					)
				),
				MyCoordinate, v_SurfaceNormal_cached
			); //Todo: This length isn't actually accurate at this point because we're using flattened positions in here (as well as mixing with unflattened)
			//thought: I think I could do this if I first solve for a theoretical triangle that is flattened to the surface normal, then, using that, I could
			//get a flattened position right below the edge. I could then use that position by solving for another theoretical triangle with that position 
			//being used to create an assumed 90 degree angle and a length from the edge startposition, to this position, etc...
			#endregion

			return true;
		}
		public bool DoesProjectionIntersectEdge_dbg(
			Vector3 origin, Vector3 destination, out LNX_NavmeshHit outHit, ref LNX_MethodDebugReport rprt, 
			bool endPointInclusive = true
		)
		{
			rprt.StartMethod($"DoesProjectionIntersectEdge_dbg(start: '{origin}', dest: '{destination}', endPtInclsv: '{endPointInclusive}')");
			Vector3 v_projection = LNX_Utils.FlatVector(destination - origin, v_SurfaceNormal_cached).normalized;
			Vector3 v_originToStart = LNX_Utils.FlatVector(StartPosition - origin, v_SurfaceNormal_cached).normalized;
			Vector3 v_originToEnd = LNX_Utils.FlatVector(EndPosition - origin).normalized;
			//todo: for efficiency testing, try caching the values of StartPosition, EndPosition, StartPosition_flat, EndPosition_flat in local
			//variables (and possibly others that I'm not thinking of) to see if this is better than continually calling these values, because these
			//values are all properties with their own overhead every time they're called.

			if (endPointInclusive)
			{
				rprt.Log("endpoint is inclusive. Checking if origin is on edge...");
				if (v_projection == V_StartToEnd_flattened) //if the vectors are pointed in the same direction...
				{
					rprt.Log($"v_projection equals V_StartToEnd_flattened. Investigating further...");

					if (v_originToEnd == V_StartToEnd_flattened) //this means the projection and the edge are definitely in alignment in 3d space...
					{
						rprt.Log("v_originToEnd equals V_StartToEnd_flattened. This means projection and edge are in 3d alignment.");
						rprt.Log("Creating outHit on end point of edge...");
						outHit = new LNX_NavmeshHit(EndPosition, MyCoordinate, v_SurfaceNormal_cached);
						rprt.Log_And_End_Method($"created projection of: '{outHit}' returning true...",
							"DoesProjectionIntersectEdge_dbg()");
						return true;
					}
				}
				else if (v_projection == V_EndToStart_flattened) //if the vectors are aligned in exactly opposite directions...
				{
					rprt.Log($"v_projection equals V_EndToStart_flattened. Investigating further...");

					if (v_originToStart == V_EndToStart_flattened) //this means the projection and the edge are definitely in alignment in 3d space...
					{
						rprt.Log("v_originToStart equals V_EndToStart_flattened. This means projection and edge are in 3d alignment.");
						rprt.Log("Creating outHit on start point of edge...");
						outHit = new LNX_NavmeshHit(StartPosition, MyCoordinate, v_SurfaceNormal_cached);
						rprt.Log_And_End_Method($"created projection of: '{outHit}' returning true...",
							"DoesProjectionIntersectEdge_dbg()");
						return true;
					}
				}
				else if (DoesPositionLieOnEdge(origin)) //Note: this needs to be checked after the alignment checks
				{
					rprt.Log("origin is on edge. Projecting...");
					outHit = new LNX_NavmeshHit(
						StartPosition +
						(V_StartToEnd * LNX_Utils.CalculateTriangleEdgeLength(
							90f,
							180f - 90f - Vector3.Angle(V_StartToEnd_flattened, V_StartToEnd),
							Vector3.Distance(StartPosition_flat, LNX_Utils.FlatVector(origin, v_SurfaceNormal_cached))
							)
						),
						MyCoordinate, v_SurfaceNormal_cached
					);
					rprt.Log_And_End_Method($"created projection of: '{outHit}' returning true...",
						"DoesProjectionIntersectEdge_dbg()");
					return true;
				}
				else
				{
					rprt.Log($"None of the endpointinclusive checks worked. Continuing...");
				}
			}


			#region ALIGNMENT SHORT-CIRCUIT TEST-------------------------------------------------
			/*
			//The following tests if the origin and projection direction allow for the possibilty of edge intersection...
			Vector3 v_edgeMid_toOriginPt = LNX_Utils.FlatVector(origin - MidPosition_flat).normalized;
			float dot_vCross_with_edgeMidPtToOriginPt = Vector3.Dot(v_Cross_flat, v_edgeMid_toOriginPt);

			rprt.Log($"edge v_cross_flat: '{v_Cross_flat}', vcross: '{v_Cross}'");
			rprt.Log($"Trying alignment short-circuit test using dot prod: '{dot_vCross_with_edgeMidPtToOriginPt}'...");
			if (dot_vCross_with_edgeMidPtToOriginPt > 0f) //origin is towards "inside" direction of edge...
			{
				rprt.Log("origin is towards 'inside' direction of triangle...");
				if (Vector3.Dot(v_projection, v_Cross_flat) > 0f) //...and the projection is also pointed inside the triangle...
				{
					rprt.Log("projection is also towards 'inside' direction of triangle...");

					outHit = LNX_NavmeshHit.None;
					rprt.Log_And_End_Method("this means projection CANNOt intersect edge. Short-circuiting by returning false...",
						"DoesProjectionIntersectEdge_dbg()");
					return false; //short-circuit
				}
			}
			else if (dot_vCross_with_edgeMidPtToOriginPt < 0) //origin is towards "OUTSIDE" direction of edge...
			{
				rprt.Log("origin is towards 'outside' direction of triangle...");

				if (Vector3.Dot(v_projection, v_Cross_flat) < 0f) //...and the projection is also towards the outside direction of the triangle
				{
					rprt.Log("projection is also towards 'outside' direction of triangle...");

					outHit = LNX_NavmeshHit.None;
					rprt.Log_And_End_Method("this means projection CANNOt intersect edge. Short-circuiting by returning false...",
						"DoesProjectionIntersectEdge_dbg()");
					return false; //short-circuit
				}
			}
			*/
			#endregion

			#region ANGULAR SHORT-CIRCUIT TEST-------------------------------------------------------
			float ang_vprjctTo_orgnToStrt = Vector3.Angle(v_projection, v_originToStart);
			float ang_prjctTo_orgnToEnd = Vector3.Angle(v_projection, v_originToEnd);
			//float ang_chevron = ang_prjctTo_orgnToStrt + ang_prjctTo_orgnToEnd; //this is cheap, but is it right?
			float ang_chevron = Vector3.Angle(v_originToStart, v_originToEnd);
			rprt.Log("Trying angular short-circuit test...");

			float lrgst = Mathf.Max
			(
				ang_vprjctTo_orgnToStrt,
				ang_prjctTo_orgnToEnd
			);

			if (
				(ang_vprjctTo_orgnToStrt > 90f && ang_prjctTo_orgnToEnd > 90f) ||
				lrgst > ang_chevron
			)
			{
				//dbg_doesProjectionIntersectEdge += $"\nOperation short-circuited bc of dot-prdct check! Returning false...\n";
				outHit = LNX_NavmeshHit.None;
				rprt.Log_And_End_Method("angular short-circuit test succeeded. Returning false...", 
					"DoesProjectionIntersectEdge_dbg()");
				return false; //short-circuit
			}
			#endregion

			rprt.Log("No short-circuits. Continuing...");

			#region CALCULATE OUT HIT -----------------------------------------------------------
			outHit = new LNX_NavmeshHit(
				StartPosition +
				(
					V_StartToEnd * LNX_Utils.CalculateTriangleEdgeLength
					(
						Vector3.Angle(v_projection, v_originToStart),
						Vector3.Angle(-v_projection, -V_StartToEnd),
						Vector3.Distance(origin, StartPosition)
					)
				), 
				MyCoordinate, v_SurfaceNormal_cached
			);
			//Todo: This length isn't actually accurate at this point because we're using flattened positions in here (as well as mixing with unflattened)
			   //thought: I think I could do this if I first solve for a theoretical triangle that is flattened to the surface normal, then, using that, I could
			   //get a flattened position right below the edge. I could then use that position by solving for another theoretical triangle with that position 
			   //being used to create an assumed 90 degree angle and a length from the edge startposition, to this position, etc...
			#endregion

			rprt.Log_And_End_Method($"End of method. calculated outpos: '{outHit}'. Now returning true...",
				"DoesProjectionIntersectEdge_dbg()");
			return true;
		}

		//public string DBG_GetSharedAngle;
		/// <summary>
		/// Returns both the angles, added together, on either side of a shared edge, either at the start or end point.
		/// </summary>
		/// <param name="nm"></param>
		/// <param name="atStart"></param>
		/// <returns></returns>
		public float GetCombinedSharedEdgeAngle( LNX_NavMesh nm, bool atStart )
		{
			//DBG_GetSharedAngle = $"{ToString()}.{nameof(GetSharedAngle)}({nameof(atStart)}: '{atStart}') sharedEdgeCoord: '{SharedEdgeCoordinate}'\n";
			if ( SharedEdgeCoordinate == LNX_ComponentCoordinate.None )
			{
				//DBG_GetSharedAngle += $"shared edge coord was none. Returning early...";
				return -1f;
			}

			LNX_Edge sharedEdge = nm.Triangles[SharedEdgeCoordinate.TrianglesIndex].Edges[SharedEdgeCoordinate.ComponentIndex];
			//DBG_GetSharedAngle += $"got shared edge: '{sharedEdge.ToString()}'...\n";

			if( atStart )
			{
				return nm.Triangles[MyCoordinate.TrianglesIndex].Verts[StartVertCoordinate.ComponentIndex].AngleAtBend_flattened +
				(
					StartPosition == sharedEdge.StartPosition ? nm.GetVertexAtCoordinate(sharedEdge.StartVertCoordinate).AngleAtBend_flattened :
					nm.GetVertexAtCoordinate(sharedEdge.EndVertCoordinate).AngleAtBend_flattened
				);
			}
			else
			{
				return nm.Triangles[MyCoordinate.TrianglesIndex].Verts[EndVertCoordinate.ComponentIndex].AngleAtBend_flattened +
				(
					EndPosition == sharedEdge.StartPosition ? nm.GetVertexAtCoordinate(sharedEdge.StartVertCoordinate).AngleAtBend_flattened :
					nm.GetVertexAtCoordinate(sharedEdge.EndVertCoordinate).AngleAtBend_flattened
				);
			}
		}

		public string DBG_GetContinuousAngleBetween;
		/// <summary>
		/// Calculates the angle from this edge to the edge at the passed coordinate as long as the two share a vert and 
		/// don't have broken space between.
		/// </summary>
		/// <param name="nm"></param>
		/// <param name="otherEdgeCoord"></param>
		/// <param name="prspctvVrtPos"></param>
		/// <returns></returns>
		public float GetContinuousAngleBetween( LNX_NavMesh nm, LNX_ComponentCoordinate otherEdgeCoord, Vector3 prspctvVrtPos )
		{
			DBG_GetContinuousAngleBetween = $"{ToString()}.{nameof(GetContinuousAngleBetween)}('{otherEdgeCoord}', '{prspctvVrtPos}')\n";

			#region SHORT-CIRCUITING ---------------------------
			if( otherEdgeCoord == MyCoordinate )
			{
				Debug.LogError($"{nameof(GetContinuousAngleBetween)}() edges are the same. Can't continue.");
				return -1f;
			}

			if ( !AmTouching(nm.Triangles[otherEdgeCoord.TrianglesIndex].Edges[otherEdgeCoord.ComponentIndex]) )
			{
				Debug.LogError($"{nameof(GetContinuousAngleBetween)}() this edge doesn't touch the other edge, so it's unable to " +
					$"resolve a shared angle");
				return -1f;
			}

			if
			( 
				!AmTouching(prspctvVrtPos) ||
				nm.Triangles[otherEdgeCoord.TrianglesIndex].Edges[otherEdgeCoord.ComponentIndex].AmTouching(prspctvVrtPos)
			)
			{
				Debug.LogError($"{nameof(GetContinuousAngleBetween)}() supplied position was not start or end position of either the owning edge, or other edge.");
				return -1f;
			}
			#endregion

			float runningAngle = 0f;
			int runningWhileCount = 0;

			LNX_ComponentCoordinate runningEdgeCoord = MyCoordinate;

			bool amFinished = false;
			while ( !amFinished )
			{
				DBG_GetContinuousAngleBetween += $"while{runningWhileCount}. Current edge: '{runningEdgeCoord}'\n" +
					$"getting vert...\n";
				LNX_Vertex vrt = nm.Triangles[runningEdgeCoord.TrianglesIndex].GetVertexAtCurrentPosition(prspctvVrtPos);
				if( vrt == null )
				{
					Debug.LogError($"getvertatcrntpos returned null");
					return -1f;
				}
				DBG_GetContinuousAngleBetween += $"got {vrt.ToString()} with angle: '{vrt.AngleAtBend_flattened}'...\n";

				runningAngle += vrt.AngleAtBend_flattened;

				DBG_GetContinuousAngleBetween += $"angle added, now '{runningAngle}'...\n";



				if( vrt.TriangleIndex == otherEdgeCoord.TrianglesIndex )
				{
					amFinished = true;
				}
				else
				{
					//runningEdgeCoord = nm.Triangles[runningEdgeCoord.TrianglesIndex].Edges[runningEdgeCoord.ComponentIndex].SharedEdgeCoordinate;

					for (int i = 0; i < 3; i++) 
					{
						if 
						( 
							i != runningEdgeCoord.ComponentIndex && 
							nm.Triangles[runningEdgeCoord.TrianglesIndex].Edges[runningEdgeCoord.ComponentIndex].AmTouching
							(
								nm.Triangles[runningEdgeCoord.TrianglesIndex].Edges[runningEdgeCoord.ComponentIndex]
							) 
						)
						{
							runningEdgeCoord = new LNX_ComponentCoordinate( runningEdgeCoord.TrianglesIndex, i );
							DBG_GetContinuousAngleBetween += ($"Decided next edge coordinate will be '{runningEdgeCoord}'\n");

							if( nm.Triangles[runningEdgeCoord.TrianglesIndex].Edges[runningEdgeCoord.ComponentIndex].AmTerminal )
							{
								Debug.Log($"Edge '{runningEdgeCoord}' along angle path was terminal. Can't get shared angle");
								return -1f;
							}

							break;
						}
					}
				}



				runningWhileCount++;
				if( runningWhileCount > 10 )
				{
					Debug.LogError($"while seems to be in infinte loop");
					return -1;
				}
			}

			return runningAngle;
		}

		/// <summary>
		/// Checks whether the supplied edge is touching this edge at either the start 
		/// or end position.
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
		/// <summary>
		/// Checks whether the supplied position is touching this edge at either the start or end position
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public bool AmTouching( Vector3 pos )
		{
			if ( pos == StartPosition || pos == StartPosition )
			{
				return true;
			}

			if ( pos == EndPosition || pos == StartPosition )
			{
				return true;
			}

			return false;
		}

		private bool AmOnSharedEdgeSpace( Vector3 endA, Vector3 endB )
		{
			if (
				endA != endB &&
				(endA == StartPosition || endB == StartPosition) &&
				(endA == EndPosition || endB == EndPosition)
			)
			{
				return true;
			}

			return false;
		}

		public bool AmOnSharedEdgeSpace( LNX_Edge edj )
		{
			return AmOnSharedEdgeSpace( edj.StartPosition, edj.EndPosition );
		}

		public bool AmBoundsEdge(LNX_NavMesh nm) //OTOD: get rid of this now that I have this in the LNX_NavMesh class
		{
			//note: It's possible to have a navmesh that isn't mostly square shaped. This won't help for that...
			//Debug.Log($"{nameof(AmBoundsEdge)}(), {nm.SurfaceOrientation}");
			#region short-circuit check ----------------------
			if ( SharedEdgeCoordinate != LNX_ComponentCoordinate.None ) 
			{
				return false;
			}

			if ( nm.SurfaceOrientation == LNX_Direction.PositiveY || nm.SurfaceOrientation == LNX_Direction.NegativeY) 
			{
				if
				(
					(StartPosition.x == nm.Bounds_HighestX && EndPosition.x == nm.Bounds_HighestX) ||
					(StartPosition.x == nm.Bounds_LowestX && EndPosition.x == nm.Bounds_LowestX) ||
					(StartPosition.z == nm.Bounds_HighestZ && EndPosition.z == nm.Bounds_HighestZ) ||
					(StartPosition.z == nm.Bounds_LowestZ && EndPosition.z == nm.Bounds_LowestZ)
				)
				{
					return true;
				}
			}

			if (nm.SurfaceOrientation == LNX_Direction.PositiveX || nm.SurfaceOrientation == LNX_Direction.NegativeX)
			{
				if
				(
					(StartPosition.y == nm.Bounds_HighestY && EndPosition.y == nm.Bounds_HighestY) ||
					(StartPosition.y == nm.Bounds_LowestY && EndPosition.y == nm.Bounds_LowestY) ||
					(StartPosition.z == nm.Bounds_HighestZ && EndPosition.z == nm.Bounds_HighestZ) ||
					(StartPosition.z == nm.Bounds_LowestZ && EndPosition.z == nm.Bounds_LowestZ)
				)
				{
					return true;
				}
			}
			
			if ( nm.SurfaceOrientation == LNX_Direction.PositiveZ || nm.SurfaceOrientation == LNX_Direction.NegativeZ )
			{
				if
				(
					(StartPosition.y == nm.Bounds_HighestY && EndPosition.y == nm.Bounds_HighestY) ||
					(StartPosition.y == nm.Bounds_LowestY && EndPosition.y == nm.Bounds_LowestY) ||
					(StartPosition.x == nm.Bounds_HighestX && EndPosition.x == nm.Bounds_HighestX) ||
					(StartPosition.x == nm.Bounds_LowestX && EndPosition.x == nm.Bounds_LowestX)
				)
				{
					return true;
				}
			}
			#endregion

			/* //todo: Maybe in the future I could also add triangles that weren't caught above
			Debug.Log("now checking projections...");
			//Now check projections...
			for ( int i = 0; i < nm.Triangles.Length; i++ )
			{
				
			}
			*/

			return false;
		}

		public bool HaveObtuseAngle(LNX_NavMesh nm)
		{
			if 
			(
				nm.Triangles[StartVertCoordinate.TrianglesIndex].Verts[StartVertCoordinate.ComponentIndex].AngleAtBend_flattened > 90f ||
				nm.Triangles[StartVertCoordinate.TrianglesIndex].Verts[EndVertCoordinate.ComponentIndex].AngleAtBend_flattened > 90f
			)
			{
				return true;
			}

			return false;
		} //todo: I think I can use this method for determining tri visibility

		#endregion


		#region HELPERS --------------------------------------------------
		public string GetCurrentInfoString(LNX_NavMesh nm)
		{
			return $"Edge.{nameof(SayCurrentInfo)}()\n" +
				$"{nameof(MyCoordinate)}: '{MyCoordinate}'\n" +
				$"{nameof(StartPosition)}: '{StartPosition}'\n" +
				$"{nameof(v_Cross)}: '{v_Cross}'\n" +
				$"{nameof(SharedEdgeCoordinate)}: '{SharedEdgeCoordinate}'\n" +
				$"{nameof(AmBoundsEdge)}: '{AmBoundsEdge(nm)}'\n" +
				$"";
		}

		public void SayCurrentInfo(LNX_NavMesh nm)
		{
			Debug.Log( GetCurrentInfoString(nm) );
		}

		public string GetAnomolyString( LNX_NavMesh nm )
		{
			string returnString = string.Empty;

			if ( 
				MyCoordinate.TrianglesIndex < 0 || 
				MyCoordinate.TrianglesIndex > nm.Triangles.Length - 1 || 
				MyCoordinate.ComponentIndex < 0 ||
				MyCoordinate.ComponentIndex > 2
			)
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

			if ( v_SurfaceNormal_cached == Vector3.zero)
			{
				returnString += $"{nameof(v_SurfaceNormal_cached)}: '{v_SurfaceNormal_cached}'\n";
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

		public string GetRelationalString()
		{
			return $"Edge[{ComponentIndex}].{nameof(GetRelationalString)}()\n" +
				$"{nameof(SharedEdgeCoordinate)}: '{SharedEdgeCoordinate}'\n" +
				$" == none: '{SharedEdgeCoordinate == LNX_ComponentCoordinate.None}'\n" +
				$"{nameof(StartVertCoordinate)}: '{StartVertCoordinate}'\n" +

				$"";
		}
		#endregion

		public override string ToString()
		{
			return $"Edge{MyCoordinate.ToString()}";
		}
	}
}