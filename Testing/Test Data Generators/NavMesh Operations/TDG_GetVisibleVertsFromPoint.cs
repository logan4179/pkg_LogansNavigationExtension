using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_GetVisibleVertsFromPoint : TDG_base
    {
		public LNX_ComponentGrabber Grabber_Hit;

		[Header("RESULTS")]
		public List<LNX_ComponentCoordinate> ResultCoordinates;
		public List<LNX_Path> ResultPaths;
		public List<LNX_ComponentCoordinate> excludeCoords;

		[Header("DEBUG")]
		public Color Color_lines;
		public bool UseDbgVersion = true;

		#region HELPERS -------------------------------------
		[ContextMenu("z call GoToDataPoint")]
		public void GoToDataPoint()
		{

		}

		[ContextMenu("z call DoEet")]
		public void DoEet()
		{
			ResultCoordinates = new List<LNX_ComponentCoordinate>();
			ResultPaths = new List<LNX_Path>();

			DBG_Operation = $"{DateTime.Now}\n";
			mthdDbg_Report.Clear();

			if (Grabber_Hit.CurrentHit == LNX_NavmeshHit.None)
			{
				DBG_Operation += $"was NOT able to sample hit from this position. Returning early...\n";
				return;
			}

			DBG_Operation += $"\nGrabber_Hit.CurrentHit: '{Grabber_Hit.CurrentHit.Position}'. Commencing operation...\n";

			if (!UseDbgVersion)
			{
				ResultPaths = _navmesh.GetVisibleVertsFromPoint(Grabber_Hit.CurrentHit, false, excludeCoords);
			}
			else
			{
				DBG_Operation += $"using debug version...\n";
				mthdDbg_Report.StartReport();
				ResultPaths = _navmesh.GetVisibleVertsFromPoint_dbg(Grabber_Hit.CurrentHit, ref mthdDbg_Report, false, excludeCoords);
				mthdDbg_Report.EndReport();
			}


			DBG_Operation += $"{nameof(ResultCoordinates)} count: '{ResultCoordinates.Count}'\n" +
				$"{nameof(ResultPaths)} count: '{ResultPaths.Count}'\n" +
				$"";
		}

		#endregion

		protected override void OnDrawGizmos()
		{
			if 
			(
				AmInUnitTest || 
				(
					Selection.activeGameObject != gameObject && 
					Selection.activeGameObject != Grabber_Hit.gameObject
				)
			)
			{
				return;
			}

			base.OnDrawGizmos();

			Grabber_Hit.DrawMyGizmos( Radius_ObjectDebugSpheres );

			if ( Grabber_Hit.RecalculatedLastFrame )
			{
				ResultCoordinates = new List<LNX_ComponentCoordinate>();
				ResultPaths = new List<LNX_Path>();

				DBG_Operation = $"{DateTime.Now}\n";
				mthdDbg_Report.Clear();

				if( Grabber_Hit.CurrentHit == LNX_NavmeshHit.None )
				{
					DBG_Operation += $"was NOT able to sample hit from this position. Returning early...\n";
					return;
				}

				DBG_Operation += $"\nGrabber_Hit.CurrentHit: '{Grabber_Hit.CurrentHit.Position}'. Commencing operation...\n";

				if ( !UseDbgVersion )
				{
					ResultPaths = _navmesh.GetVisibleVertsFromPoint(Grabber_Hit.CurrentHit, false, excludeCoords);
				}
				else
				{
					DBG_Operation += $"using debug version...\n";
					mthdDbg_Report.StartReport();
					ResultPaths = _navmesh.GetVisibleVertsFromPoint_dbg(Grabber_Hit.CurrentHit, ref mthdDbg_Report, false, excludeCoords);
					mthdDbg_Report.EndReport();
				}


				DBG_Operation += $"{nameof(ResultCoordinates)} count: '{ResultCoordinates.Count}'\n" +
					$"{nameof(ResultPaths)} count: '{ResultPaths.Count}'\n" +
					$"";
			}


			Gizmos.color = Color_lines;
			float height = 0.5f;

			if( ResultCoordinates != null && ResultCoordinates.Count > 0 )
			{
				for( int i = 0; i < ResultCoordinates.Count; i++ )
				{
					Gizmos.DrawLine(
						Grabber_Hit.CurrentHit.Position,
						_navmesh.GetVertexAtCoordinate(ResultCoordinates[i]).V_Position
					);

					Gizmos.DrawLine( 
						_navmesh.GetVertexAtCoordinate(ResultCoordinates[i]).V_Position,
						_navmesh.GetVertexAtCoordinate(ResultCoordinates[i]).V_Position + (Vector3.up * height)
					);
				}
			}

			if ( ResultPaths != null && ResultPaths.Count > 0 )
			{
				for ( int i = 0; i < ResultPaths.Count; i++ )
				{
					Gizmos.DrawLine(
						Grabber_Hit.CurrentHit.Position,
						ResultPaths[i].EndPosition
					);

					Gizmos.DrawLine(
						ResultPaths[i].EndPosition,
						ResultPaths[i].EndPosition + (Vector3.up * height)
					);
				}
			}
		}

		#region WRITING-------------------------------------
		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			bool rslt = TDG_Manager.WriteTestObjectToJson(TDG_Manager.filePath_testData_Raycasting, this);

			if (rslt)
			{
				LastWriteTime = System.DateTime.Now.ToString();
				return true;

			}

			return false;
		}

		[ContextMenu("z call RecreateMeFromJson()")]
		public void RecreateMeFromJson()
		{
			if (!System.IO.File.Exists(TDG_Manager.filePath_testData_Raycasting))
			{
				Debug.LogError($"path '{TDG_Manager.filePath_testData_Raycasting}' didn't exist. returning early...");
				return;
			}

			string myJsonString = System.IO.File.ReadAllText(TDG_Manager.filePath_testData_Raycasting);

			JsonUtility.FromJsonOverwrite(myJsonString, this);

			EditorUtility.SetDirty(this);
		}
		#endregion
	}
}
