using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
	/// <summary>
	/// Base class for efficiency tests
	/// </summary>
    public class ET_Base : MonoBehaviour
    {
		public LNX_NavMesh _navmesh;

		public virtual void RunTests()
		{
			Debug.LogWarningFormat($"NOTE: Don't run tests with debug logging or debug string interpolations enabled in method chains!!!");

		}

		public void DrawTriGizmo(LNX_Triangle tri, Color col)
		{
			Gizmos.color = col;
			Handles.color = col;

			GUIStyle gstl_vertLines = new GUIStyle();
			gstl_vertLines.normal.textColor = col;

			//Draw borders...
			Handles.DrawLine(tri.Verts[0].V_Position, tri.Verts[1].V_Position);
			Handles.DrawLine(tri.Verts[1].V_Position, tri.Verts[2].V_Position);
			Handles.DrawLine(tri.Verts[2].V_Position, tri.Verts[0].V_Position);
		}

		public void DrawStandardFocusTriGizmos(LNX_Triangle tri, float raiseAmount, string lblString)
		{
			Color oldColor = Gizmos.color;

			Gizmos.color = Color.magenta;
			//Handles.color = Color.magenta;
			Vector3 vRaise = Vector3.up * raiseAmount;

			Gizmos.DrawLine(tri.Verts[0].V_Position, tri.V_Center + vRaise);
			Gizmos.DrawLine(tri.Verts[1].V_Position, tri.V_Center + vRaise);
			Gizmos.DrawLine(tri.Verts[2].V_Position, tri.V_Center + vRaise);

			Handles.Label(tri.V_Center + vRaise, lblString);

			Gizmos.color = oldColor;
		}

		public void DrawStandardEdgeFocusGizmos(LNX_Edge edge, float raiseAmount, string lblString, Color clr)
		{
			Color oldColor = Gizmos.color;

			Gizmos.color = clr;
			Vector3 vRaise = Vector3.up * raiseAmount;

			Gizmos.DrawLine(edge.StartPosition, edge.StartPosition + vRaise);
			Handles.Label(edge.StartPosition + vRaise, "edgeStart");

			Gizmos.DrawLine(edge.StartPosition + vRaise, edge.EndPosition + vRaise);
			Gizmos.DrawLine(edge.EndPosition, edge.EndPosition + vRaise);
			Handles.Label(edge.EndPosition + vRaise, "edgeEnd");


			Gizmos.color = oldColor;
		}

		public void SayLoopReportString( string hdr, DateTime dt_loopStart, DateTime dt_loopEnd, int loopIterationCount,
			float expectedTotalLoopSeconds, float expectedAvgOpTime )
		{
			TimeSpan ts_loop = dt_loopEnd.Subtract(dt_loopStart);
			double avgOpTime = dt_loopEnd.Subtract(dt_loopStart).TotalMilliseconds / loopIterationCount;

			string s = $"{hdr}\n";

			double newLoopPercentage = (ts_loop.TotalSeconds / expectedTotalLoopSeconds) * 100f;
			
			double newOperationPercentage = (avgOpTime / expectedAvgOpTime) * 100f;

			s += $"Total time: '{ts_loop}', which was {(newLoopPercentage < 100f ? $"{100f - newLoopPercentage}% faster " : $"{100f - newLoopPercentage}% slower")} " +
				$"than most recent ({expectedTotalLoopSeconds})\n" +
				$". Avg Operation time: '{avgOpTime}' ms, which was {(newOperationPercentage < 100f ? $"{100f - newOperationPercentage}% faster " : $"{100f - newOperationPercentage}% slower")} " +
				$"than most recent ({expectedAvgOpTime})";

			if ( newLoopPercentage > 115f || newLoopPercentage < 75f || newOperationPercentage > 115f || newOperationPercentage < 75f )
			{
				Debug.LogWarning( s );
			}
			else
			{
				Debug.Log(s);
			}
		}
	}
}
