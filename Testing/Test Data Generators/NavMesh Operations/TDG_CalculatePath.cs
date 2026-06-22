using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_CalculatePath : TDG_base
	{
		[Space(10)]

		[Header("REFERENCE")]
		public LNX_ComponentGrabber Grabber_StartPos;
		public LNX_ComponentGrabber Grabber_EndPos;

		public LNX_Triangle StartTriangle => Grabber_StartPos.CurrentlyGrabbedTriangle;
		public LNX_Triangle EndTriangle => Grabber_EndPos.CurrentlyGrabbedTriangle;

		public LNX_Vertex StartVert => Grabber_StartPos.CurrentlyGrabbedVert;
		public LNX_Vertex EndVert => Grabber_EndPos?.CurrentlyGrabbedVert;

		public TextAsset DataAsset;

		[Header("DATA")]
		public bool CurrentOperationResult;
		public LNX_Path CurrentResultPath;
		public LNX_NavMeshData _data;

		[Header("DEBUG PATH")]
		public Color Color_PathPoints;
		[Range(0f, 0.05f)] public float Size_PathPoints;
		[Range(0f, 0.25f)] public float Height_PathPtLabels;

		[Header("DEBUG")]
		public bool AllowEffiencyLoading;
		public Color Color_IfTrue;
		public Color Color_IfFalse;

		public int Index_drawVertPath = -1;

		public List<LNX_Path> BestVertPaths;

		[ContextMenu("z call TryIt()")]
		public void TryIt()
		{
			//List<LNX_ComponentCoordinate> backstopverts = new List<LNX_ComponentCoordinate>();
			//List<LNX_ComponentCoordinate> backstopverts = null;

			//List<LNX_ComponentCoordinate> fwdBackstopVerts = new List<LNX_ComponentCoordinate>();
			//List<LNX_ComponentCoordinate> fwdBackstopVerts = new List<LNX_ComponentCoordinate>(backstopverts); //todo: can I do this instead of the following loop?

			/*
			if (backstopverts != null && backstopverts.Count > 0)
			{
				for (int i = 0; i < backstopverts.Count; i++)
				{
					fwdBackstopVerts.Add(backstopverts[i]);
				}
			}
			
			Debug.Log($"fwdbstpvrts null: '{fwdBackstopVerts == null}'");
			*/

			float totalDist = 0f;
			for ( int i = 0; i < BestVertPaths[Index_drawVertPath].PointCount - 1; i++ )
			{
				float dist = Vector3.Distance(
					BestVertPaths[Index_drawVertPath].PathPoints[i].Position,
					BestVertPaths[Index_drawVertPath].PathPoints[i + 1].Position
				);

				totalDist += dist;
			}

			Debug.Log($"calculated dist: '{totalDist}', dist on path: '{BestVertPaths[Index_drawVertPath].TotalDistance}'");
		}

		[ContextMenu("z call CastTextAssetToData()")]
		public void CastTextAssetToData()
		{
			_data = JsonUtility.FromJson<LNX_NavMeshData>( DataAsset.ToString() );
		}

		[ContextMenu("z call CalculatePath()")]
		public void CalculatePath() //putting all the logic here so I can call as desired rather than automatically in ondrawgizmos
		{
			DBG_Operation = $"Recalculating at: '{DateTime.Now}'...\n";
			CurrentOperationResult = false;
			mthdDbg_Report.Clear();
			CurrentResultPath = LNX_Path.None;
			/*
			if( AllowEffiencyLoading )
			{
				DBG_Operation += $"am allowing efficiency loading. attempting loading...\n";
				DateTime dt_efficiencyLoadStart = DateTime.Now;
				if( !_data.MatchesNavmesh(_navmesh) )
				{
					Debug.LogError($"LNX ERROR! {nameof(AllowEffiencyLoading)} is turned on, but saved navmesh data seems to be invalid. Returning early...");
					return;
				}
				else
				{
					_navmesh.TryLoadEfficiencyData(_data);
				}
				DBG_Operation += $"efficiency load took '{DateTime.Now.Subtract(dt_efficiencyLoadStart).TotalSeconds}' seconds...\n";

				if (_data == null || !_data.MatchesNavmesh(_navmesh))
				{
					DBG_Operation += ($"LNX ERROR! Saved navmesh data seems to be invalid. You should probably " +
						$"call {nameof(CastTextAssetToData)} Returning early...");
					return;
				}
			}
			*/


			long totalMs = 0;
			long totalTicks = 0;
			long totalSeconds = 0;
			string s = "";
			if ( !UseDebugVersion )
			{
				DBG_Operation += $"regular version.\n" + 
					$"using startHit: '{Grabber_StartPos.CurrentHit}', and endHit: '{Grabber_EndPos.CurrentHit}'...\n" +
					$"Commencing operation...\n";

				System.Diagnostics.Stopwatch stpWtch = System.Diagnostics.Stopwatch.StartNew();
				CurrentOperationResult = _navmesh.CalculatePath(
					Grabber_StartPos.CurrentHit, Grabber_EndPos.CurrentHit,
					out CurrentResultPath
				);
				stpWtch.Stop();
				totalMs = stpWtch.ElapsedMilliseconds;
				totalTicks = stpWtch.ElapsedTicks;
				totalSeconds = stpWtch.Elapsed.Seconds;
			}
			else
			{
				DBG_Operation += $"debug version.\n" +
					$"using startHit: '{Grabber_StartPos.CurrentHit}', and endHit: '{Grabber_EndPos.CurrentHit}'...\n" +
					$"Commencing operation...\n";

				mthdDbg_Report.StartReport();
				//try
				//{
				System.Diagnostics.Stopwatch stpWtch = System.Diagnostics.Stopwatch.StartNew();
				CurrentOperationResult = _navmesh.CalculatePath_dbg(
						Grabber_StartPos.CurrentHit, Grabber_EndPos.CurrentHit,
						out CurrentResultPath, ref mthdDbg_Report
					);
					stpWtch.Stop();
					totalMs = stpWtch.ElapsedMilliseconds;
					totalTicks = stpWtch.ElapsedTicks;
					totalSeconds = stpWtch.Elapsed.Seconds;
				//}
				//catch (Exception)
				//{
				//throw;
				//}

				mthdDbg_Report.EndReport();
			}

			DBG_Operation += $"calculatepath took\n" +
				$"'{totalSeconds}' seconds\n" +
				$"'{totalMs}' ms,\n" +
				$"'{totalTicks}' ticks\n" +
				$"Result: '{CurrentOperationResult}'\n";
		}

		protected override void OnDrawGizmos()
		{
			if 
			( 
				AmInUnitTest || 
				!SelectionIsOneOfTheFollowing(
					gameObject, 
					Grabber_StartPos.gameObject,
					Grabber_EndPos.gameObject
				)
			)
			{
				return;
			}

			base.OnDrawGizmos();

			DrawStandardFocusTriGizmos(StartTriangle, 0.01f, "", Color.magenta, true, 0.01f, false, false);
			DrawStandardFocusTriGizmos(EndTriangle, 0.01f, "", Color.magenta, true, 0.01f, false, false);

			//DBG_Operation += $"Commencing operation...\n";

			if( 
				AutoRun && 
				(Grabber_StartPos.RecalculatedLastFrame ||
				Grabber_EndPos.RecalculatedLastFrame)
			)
			{
				CalculatePath();
			}


			if (CurrentOperationResult)
			{
				Gizmos.color = Color_IfTrue;
			}
			else
			{
				Gizmos.color = Color_IfFalse;
			}

			#region Draw Basic Gizmo Objects --------------------------------------------------------------------
			Grabber_StartPos.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_EndPos.DrawMyGizmos(Radius_ObjectDebugSpheres);
			//Debug.Log(System.DateTime.Now);
			#endregion

			#region Draw Path --------------------------------------------------
			Color oldclr = Gizmos.color;
			Color oldHandlesColor = Handles.color;
			Gizmos.color = Color_PathPoints;
			Handles.color = Color_PathPoints;

			if( Index_drawVertPath <= -1 )
			{
				CurrentResultPath.DrawMyGizmos(Size_PathPoints, Height_PathPtLabels);
			}
			else
			{
				BestVertPaths[Index_drawVertPath].DrawMyGizmos( Size_PathPoints, Height_PathPtLabels );
			}

				Gizmos.color = oldclr;
			Handles.color = oldHandlesColor;
			#endregion
		}
	}
}
