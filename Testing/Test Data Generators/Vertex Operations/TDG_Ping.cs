using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_Ping : TDG_base
    {
		public LNX_ComponentGrabber _Grabber_Vert;
		public LNX_ComponentGrabber _grabber_endHit;

		public LNX_Vertex CallingVert => _Grabber_Vert.CurrentlyGrabbedVert;
		public LNX_NavmeshHit EndHit => _grabber_endHit.CurrentHit;

		public List<LNX_ComponentCoordinate> backstopCoords;

		[Header("RESULTS")]
		public LNX_Path ResultPath;

		[Header("DEBUG PATH")]
		public Color Color_PathPoints;
		[Range(0f, 0.05f)] public float Size_PathPoints;
		[Range(0f, 0.25f)] public float Height_PathPtLabels;

		[ContextMenu("z call RunOperation()")]
		public void RunOperation() 
		{
			DBG_Operation = $"Recalculating at: '{DateTime.Now}'...\n";
			ResultPath = LNX_Path.None;
			mthdDbg_Report.Clear();

			if( CallingVert == null )
			{
				DBG_Operation += $"CallingVert was null. Returning early...\n";
				Debug.LogError($"CallingVert was null. Returning early...");
				return;
			}

			if (EndHit == LNX_NavmeshHit.None)
			{
				DBG_Operation += $"EndHit was None. Returning early...\n";
				Debug.LogError($"EndHit was None. Returning early...");
				return;
			}
			float maxDist = float.MaxValue;

			long totalMs = 0;
			long totalTicks = 0;
			long totalSeconds = 0;
			string s = "";
			if (!UseDebugVersion)
			{
				DBG_Operation += $"regular version.\n" +
					$"using CallingVert: '{CallingVert}', and endHit: '{EndHit}'...\n" +
					$"Commencing operation...\n";

				System.Diagnostics.Stopwatch stpWtch = System.Diagnostics.Stopwatch.StartNew();
				ResultPath = CallingVert.Ping(EndHit, _navmesh, maxDist, LNX_Path.None, backstopCoords);
				stpWtch.Stop();
				totalMs = stpWtch.ElapsedMilliseconds;
				totalTicks = stpWtch.ElapsedTicks;
				totalSeconds = stpWtch.Elapsed.Seconds;
			}
			else
			{
				DBG_Operation += $"regular version.\n" +
					$"using CallingVert: '{CallingVert}', and endHit: '{EndHit}'...\n" +
					$"Commencing operation...\n";

				mthdDbg_Report.StartReport();
				System.Diagnostics.Stopwatch stpWtch = System.Diagnostics.Stopwatch.StartNew();
				ResultPath = CallingVert.Ping_dbg(EndHit, _navmesh, maxDist, LNX_Path.None, ref mthdDbg_Report, backstopCoords);
				stpWtch.Stop();
				totalMs = stpWtch.ElapsedMilliseconds;
				totalTicks = stpWtch.ElapsedTicks;
				totalSeconds = stpWtch.Elapsed.Seconds;

				mthdDbg_Report.EndReport();
			}

			DBG_Operation += $"calculatepath took\n" +
				$"'{totalSeconds}' seconds\n" +
				$"'{totalMs}' ms,\n" +
				$"'{totalTicks}' ticks\n" +
				$"ResultPath length: '{ResultPath.PointCount}'\n";
		}


		protected override void OnDrawGizmos()
		{
			if
			(
				AmInUnitTest ||
				!SelectionIsOneOfTheFollowing(
					gameObject,
					_Grabber_Vert.gameObject,
					_grabber_endHit.gameObject
				)
			)
			{
				return;
			}

			base.OnDrawGizmos();


			if (
				AutoRun &&
				(_Grabber_Vert.RecalculatedLastFrame ||
				_grabber_endHit.RecalculatedLastFrame)
			)
			{
				RunOperation();
			}


			if ( ResultPath != LNX_Path.None)
			{
				Gizmos.color = Color.green;
			}
			else
			{
				Gizmos.color = Color.red;
			}

			#region Draw Basic Gizmo Objects --------------------------------------------------------------------
			_Grabber_Vert.DrawMyGizmos(Radius_ObjectDebugSpheres);
			_grabber_endHit.DrawMyGizmos(Radius_ObjectDebugSpheres);
			//Debug.Log(System.DateTime.Now);
			#endregion

			Gizmos.DrawLine( _Grabber_Vert.transform.position, _grabber_endHit.transform.position );

			#region Draw Path --------------------------------------------------
			Color oldclr = Gizmos.color;
			Color oldHandlesColor = Handles.color;
			Gizmos.color = Color_PathPoints;
			Handles.color = Color_PathPoints;

			ResultPath.DrawMyGizmos(Size_PathPoints, Height_PathPtLabels);

			Gizmos.color = oldclr;
			Handles.color = oldHandlesColor;
			#endregion
		}
	}
}
