using System;
using System.Collections.Generic;
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
		public Vector3 v_cross;


		//[Header("PROPERTIES")]
		public Vector3 MidPosition => (StartPosition + EndPosition) / 2f;

		public Vector3 v_startToEnd => Vector3.Normalize(EndPosition - StartPosition);

		public Vector3 v_endToStart => Vector3.Normalize(StartPosition - EndPosition);

		public Vector3 v_toCenter => Vector3.Normalize( v_triCenter_cached - MidPosition );
		public float EdgeLength => Vector3.Distance(StartPosition, EndPosition);
		public bool AmTerminal => SharedEdgeCoordinate == LNX_ComponentCoordinate.None;



		public LNX_Edge( LNX_NavMesh nm, LNX_Vertex strtVrt, LNX_Vertex endVrt, int triIndx, int cmptIndx )
		{
			//Debug.Log($"ctor. edge: '{tri.Index_inCollection},{indx}'");

			MyCoordinate = new LNX_ComponentCoordinate( triIndx, cmptIndx );

			StartVertCoordinate = strtVrt.MyCoordinate;
			EndVertCoordinate = endVrt.MyCoordinate;

			v_triCenter_cached = nm.Triangles[triIndx].V_Center;

			SharedEdgeCoordinate = LNX_ComponentCoordinate.None;
		}

		public LNX_Edge( LNX_Edge edge )
		{
			StartPosition = edge.StartPosition;
			StartVertCoordinate = edge.StartVertCoordinate;
			EndPosition = edge.EndPosition;
			EndVertCoordinate = edge.EndVertCoordinate;

			v_cross = edge.v_cross;

			MyCoordinate = edge.MyCoordinate;

			SharedEdgeCoordinate = edge.SharedEdgeCoordinate;
		}

		public void AdoptValues(LNX_Edge edge)
		{
			StartPosition = edge.StartPosition;
			StartVertCoordinate = edge.StartVertCoordinate;
			EndPosition = edge.EndPosition;
			EndVertCoordinate = edge.EndVertCoordinate;

			v_cross = edge.v_cross;

			MyCoordinate = edge.MyCoordinate;

			SharedEdgeCoordinate = edge.SharedEdgeCoordinate;
		}

		public void CalculateInfo( LNX_Triangle tri, LNX_Vertex strtVrt, LNX_Vertex endVrt )
		{
			StartPosition = strtVrt.V_Position;
			EndPosition = endVrt.V_Position;

			v_cross = Vector3.Cross(v_startToEnd, tri.v_sampledNormal).normalized;

			if ( Vector3.Dot(v_cross, v_toCenter) < 0 )
			{
				v_cross = -v_cross;
			}
		}

		public void TriIndexChanged( int newIndex )
		{
			MyCoordinate = new LNX_ComponentCoordinate( newIndex, MyCoordinate.ComponentIndex );

			StartVertCoordinate = new LNX_ComponentCoordinate( newIndex, StartVertCoordinate.ComponentIndex);

			EndVertCoordinate = new LNX_ComponentCoordinate( newIndex, EndVertCoordinate.ComponentIndex);
		}

		public int GetOpposingVertIndex()
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

			Vector3 v_result = StartPosition + Vector3.Project(v_vrtToPos, v_startToEnd);

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

		public bool IsProjectedPointOnEdge( Vector3 origin, Vector3 destination )
		{
			string dbg = "";
			Vector3 direction = LNX_Utils.FlatVector( destination - origin);

			float dotOf_dir_and_edgeStart = Vector3.Dot( 
				direction, LNX_Utils.FlatVector(StartPosition - origin) );
			dbg += $"dot(start): '{dotOf_dir_and_edgeStart}'. ";

			if( dotOf_dir_and_edgeStart < 0f )
			{
				Debug.Log(dbg);
				return false;
			}

			float dotOf_dir_and_edgeEnd = Vector3.Dot(
				direction, LNX_Utils.FlatVector(EndPosition - origin) );
			dbg += $"dot(end): '{dotOf_dir_and_edgeEnd}'\n";

			if( dotOf_dir_and_edgeEnd < 0f )
			{
				Debug.Log(dbg);

				return false;
			}

			Debug.Log(dbg);


			return true;

			/////////////////////////////////////////////////////////////////////////////////////
			/*
			float ang_originToProjectEdge = Vector3.Angle(
				Vector3.Normalize(StartPosition - origin),
				Vector3.Normalize(EndPosition - origin)
			);
			dbg += $"{nameof(ang_originToProjectEdge)}: '{ang_originToProjectEdge}'\n";


			float angA = Vector3.Angle(
				direction, LNX_Utils.FlatVector(StartPosition - origin)
			);
			float angB = Vector3.Angle(
				direction, LNX_Utils.FlatVector(EndPosition - origin)
			);

			dbg += $"angA: '{angA}', angB: '{angB}'\n";

			if (angA > ang_originToProjectEdge || angB > ang_originToProjectEdge)
			{
				dbg += "greather than...";
				Debug.Log(dbg );
				return false;
			}
			Debug.Log(dbg);

			return true;
			*/
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

		public void SayCurrentInfo()
		{
			Debug.Log($"Edge.{nameof(SayCurrentInfo)}()\n" +
				$"{nameof(MyCoordinate)}: '{MyCoordinate}'\n" +
				$"{nameof(StartPosition)}: '{StartPosition}'\n" +
				$"{nameof(v_cross)}: '{v_cross}'\n" +
				$"");
		}
	}
}