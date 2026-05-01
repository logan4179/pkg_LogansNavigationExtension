using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace LogansNavigationExtension
{
    public class TDG_GetVisibleVertsFromVert : TDG_base
    {
		public LNX_ComponentGrabber Grabber_Vert;
		public LNX_Vertex CurrentVert => Grabber_Vert.CurrentlyGrabbedVert;

		public List<LNX_ComponentCoordinate> ExcludeVerts;

		[Header("OPTIONS")]
		public bool UseDebugVersionOfMethod = true;

		[Header("RESULTS")]
		public LNX_NavmeshHit ResultHit;
		public List<LNX_ComponentCoordinate> ResultCoordinates;
		List<LNX_Path> ResultPaths = new List<LNX_Path>();

		[Header("DEBUG")]
		public Color Color_lines;


		#region HELPERS -------------------------------------
		[ContextMenu("z call GoToDataPoint")]
		public void GoToDataPoint()
		{

		}

		[ContextMenu("z call CaptureProblemPosition")]
		public override void CaptureProblemPosition()
		{
			_dataCapture_problems.CaptureDataPoint(CurrentVert.V_Position);
		}

		[ContextMenu("z call DoEet")]
		public void DoEet()
		{
			List<int> tryListA = new List<int>() { 0, 1, 2, 3, 4 };
			List<int> tryListB = tryListA.GetRange(0, tryListA.Count);
			tryListA[0] = 123;
			Debug.Log($"A: '{tryListA[0]}', B: '{tryListB[0]}' bcount: '{tryListB.Count}'");
		}
		#endregion

		protected override void OnDrawGizmos()
		{
			if
			(
				AmInUnitTest ||
				(
					Selection.activeGameObject != gameObject &&
					Selection.activeGameObject != Grabber_Vert.gameObject
				)
			)
			{
				return;
			}

			base.OnDrawGizmos();

			Grabber_Vert.DrawMyGizmos(Radius_ObjectDebugSpheres);

			if( CurrentVert != null )
			{
				Gizmos.DrawSphere( CurrentVert.V_Position, Radius_ObjectDebugSpheres );

				LNX_DrawingUtils.DrawTriGizmos( _navmesh.Triangles[Grabber_Vert.CurrentHit.TriIndex], Color.yellow, 
					false, false, true, 0.02f, true, 0.1f, false, -1f
				);
			}

			if (Grabber_Vert.RecalculatedLastFrame)
			{
				ResultHit = LNX_NavmeshHit.None;
				ResultCoordinates = new List<LNX_ComponentCoordinate>();
				DBG_Operation = $"starting operation at: '{System.DateTime.Now}'...\n";

				if( CurrentVert == null )
				{
					DBG_Operation += $"CurrentVert is null. Something's wrong...\n";
					return;
				}
				else
				{
					DBG_Operation += $"using currentvert: '{CurrentVert}'...\n";
				}

					DBG_Operation += "Attempting to sample hit on Navmesh surface...\n";
				if (!_navmesh.SamplePosition(Grabber_Vert.transform.position, out ResultHit, 3f, false, true))
				{
					DBG_Operation += $"was NOT able to sample hit from this position. Returning early...\n";
					return;
				}

				DBG_Operation += $"\nsampled hit at: '{ResultHit.Position}'.\n" +
					$"Commencing operation...\n";

				/*
				ResultPaths = _navmesh.GetVisibleVertsFromPoint( 
					CurrentVert, true, ExcludeVerts 
				);
				*/

				if( UseDebugVersionOfMethod )
				{
					DBG_Operation += $"using debug version...\n";
					mthdDbg_Report.StartReport();
					ResultPaths = _navmesh.GetVisibleVertsFromVert_dbg(
						CurrentVert, ref mthdDbg_Report, true, ExcludeVerts
					);
					mthdDbg_Report.EndReport();
				}
				else
				{
					DBG_Operation += $"using regular version (as opposed to debug version)...\n";

					ResultPaths = _navmesh.GetVisibleVertsFromVert(CurrentVert, true, ExcludeVerts);
				}

				DBG_Operation += $"{nameof(ResultCoordinates)} count: '{ResultCoordinates.Count}'\n";
			}


			Gizmos.color = Color_lines;
			float height = 0.5f;
			for (int i = 0; i < ResultCoordinates.Count; i++)
			{
				Gizmos.DrawLine(
					ResultHit.Position,
					_navmesh.GetVertexAtCoordinate(ResultCoordinates[i]).V_Position
				);

				Gizmos.DrawLine(
					_navmesh.GetVertexAtCoordinate(ResultCoordinates[i]).V_Position,
					_navmesh.GetVertexAtCoordinate(ResultCoordinates[i]).V_Position + (Vector3.up * height)
				);
			}

			Gizmos.color = Color.red;
			for(int i = 0; i < ExcludeVerts.Count; i++)
			{
				Gizmos.DrawLine(_navmesh.GetVertexAtCoordinate(ExcludeVerts[i]).V_Position,
					_navmesh.GetVertexAtCoordinate(ExcludeVerts[i]).V_Position + (Vector3.up * height));
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
