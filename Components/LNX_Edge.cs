using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
		public Vector3 v_Cross;


		//[Header("PROPERTIES")]
		public Vector3 MidPosition => (StartPosition + EndPosition) / 2f;

		public Vector3 V_StartToEnd => Vector3.Normalize(EndPosition - StartPosition);

		public Vector3 V_EndToStart => Vector3.Normalize(StartPosition - EndPosition);

		public Vector3 v_toCenter => Vector3.Normalize( v_triCenter_cached - MidPosition );
		public float EdgeLength => Vector3.Distance(StartPosition, EndPosition);
		public bool AmTerminal => SharedEdgeCoordinate == LNX_ComponentCoordinate.None;



		public LNX_Edge( LNX_Triangle ownerTri, LNX_Vertex strtVrt, LNX_Vertex endVrt, int triIndx, int cmptIndx )
		{
			//Debug.Log($"ctor. edge: '{tri.Index_inCollection},{indx}'");

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

		public void CalculateRelational( LNX_NavMesh nvmsh ) //todo: unit test
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


		public void CalculateDerivedInfo( LNX_Triangle tri, LNX_Vertex strtVrt, LNX_Vertex endVrt )
		{
			StartPosition = strtVrt.V_Position;
			EndPosition = endVrt.V_Position;

			v_Cross = Vector3.Cross(V_StartToEnd, tri.V_PlaneFaceNormal).normalized; //why is this value Vector3.zero during the test? Because the planeface normal isnt' ready yet!

			if ( Vector3.Dot(v_Cross, v_toCenter) < 0 )
			{
				v_Cross = -v_Cross;
			}
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

		public string dbg_doesPositionLieOnEdge;
		public bool DoesPositionLieOnEdge( Vector3 pos, Vector3 flattenDir )
		{
			dbg_doesPositionLieOnEdge = $"{nameof(DoesPositionLieOnEdge)}({pos}, {flattenDir})...\n" +
				$"condition1: '{pos == StartPosition || pos == EndPosition}\n" +
				$"condition2: '{LNX_Utils.FlatVector(pos - StartPosition, flattenDir).normalized == LNX_Utils.FlatVector(V_StartToEnd, flattenDir).normalized}\n" +
				$"condition3: '{Vector3.Distance(StartPosition, pos) <= EdgeLength}\n" +
				$"condition3: '{LNX_Utils.FlatVector(pos - EndPosition, flattenDir).normalized == -LNX_Utils.FlatVector(V_StartToEnd, flattenDir).normalized}\n" +
				$"condition3: '{Vector3.Distance(EndPosition, pos) <= EdgeLength}\n" +
				"";

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
				dbg_doesPositionLieOnEdge += "returning true...";
				return true;
			}
			else
			{
				dbg_doesPositionLieOnEdge += "returning false...";

				return false;
			}
		}

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
			Vector3 origin, Vector3 destination, Vector3 flattenDir, ref string dbgString, out Vector3 outPos 
		)
		{
			dbgString += $"{nameof(DoesProjectionIntersectEdge)}({MyCoordinate.ComponentIndex})-----\n";
			Vector3 v_prjct = LNX_Utils.FlatVector( destination - origin, flattenDir ).normalized;
			Vector3 v_originToStart = LNX_Utils.FlatVector( StartPosition - origin, flattenDir ).normalized;
			Vector3 v_originToEnd = LNX_Utils.FlatVector( EndPosition - origin ).normalized;
			////////////////////////////////////////////////////////////////////////////////////////////////////////
			float ang_prjctTo_orgnToStrt = Vector3.Angle( v_prjct, v_originToStart );
			float ang_prjctTo_orgnToEnd = Vector3.Angle( v_prjct, v_originToEnd );
			//float ang_chevron = ang_prjctTo_orgnToStrt + ang_prjctTo_orgnToEnd; //this is cheap, but is it right?
			float ang_chevron = Vector3.Angle( v_originToStart, v_originToEnd );

			float lrgst = Mathf.Max
			(
				ang_prjctTo_orgnToStrt,
				ang_prjctTo_orgnToEnd
			);

			#region DIRECTIONAL SHORT-CIRCUIT TEST-------------------------------------------------
			//The following tests if the origin and projection direction allow for the possibilty of edge intersection...
			Vector3 v_cross_flat = LNX_Utils.FlatVector(v_Cross, flattenDir).normalized;
			Vector3 v_midPt_toOrigin = LNX_Utils.FlatVector( origin - MidPosition ).normalized;

			dbgString += $"directional short circuit test\n" +
				$"vcsflt: '{v_cross_flat}', vmdpt2orgn: '{v_midPt_toOrigin}'. " +
				$"dot: '{Vector3.Dot(v_cross_flat, v_midPt_toOrigin)}'...\n";

			if( Vector3.Dot(v_cross_flat, v_midPt_toOrigin) >= 0f ) //origin should be inside triangle...
			{
				dbgString += $"origin is inside\n";

				if( Vector3.Dot(v_prjct, -v_cross_flat) < 0f )
				{
					dbgString += $"\nOperation short-circuited bc of containment check! Returning false...\n";
					outPos = Vector3.zero;
					return false; //short-circuit
				}
			}
			else //origin is OUTSIDE triangle...
			{
				dbgString += $"origin is outside\n";

				if ( Vector3.Dot(v_prjct, v_cross_flat) < 0f )
				{
					dbgString += $"\nOperation short-circuited bc of containment check! Returning false...\n";
					outPos = Vector3.zero;
					return false; //short-circuit
				}
			}
			#endregion

			#region ANGULAR SHORT-CIRCUIT TEST-------------------------------------------------------
			dbgString += $"\n" +
				$"Angular short-circuit test\n" +
				$"trying angle short-circuit with rslts. 1: '{ang_prjctTo_orgnToStrt}', " +
				$"2: '{ang_prjctTo_orgnToEnd}'...\n" +
				$"chev: '{ang_chevron}', lrgst: '{lrgst}'...";

			if (
				(ang_prjctTo_orgnToStrt > 90f && ang_prjctTo_orgnToEnd > 90f) ||
				lrgst > ang_chevron
			)
			{
				dbgString += $"\nOperation short-circuited bc of dot-prdct check! Returning false...\n";
				outPos = Vector3.zero;
				return false; //short-circuit
			}
			else
			{
				dbgString += $"no short-circuit. Method will continue...\n";
			}
			
			#endregion

			#region CALCULATE OUT POS -----------------------------------------------------------
			outPos = StartPosition + 
			(
				V_StartToEnd * LNX_Utils.CalculateTriangleEdgeLength
				(
					Vector3.Angle(v_prjct, v_originToStart),
					Vector3.Angle(-v_prjct, -V_StartToEnd),
					Vector3.Distance(origin, StartPosition)
				)
			); //Todo: This length isn't actually accurate at this point because we're using flattened positions in here (as well as mixing with unflattened)
			#endregion

			return true;
		}

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

		public void SayCurrentInfo()
		{
			Debug.Log($"Edge.{nameof(SayCurrentInfo)}()\n" +
				$"{nameof(MyCoordinate)}: '{MyCoordinate}'\n" +
				$"{nameof(StartPosition)}: '{StartPosition}'\n" +
				$"{nameof(v_Cross)}: '{v_Cross}'\n" +
				$"");
		}
	}
}