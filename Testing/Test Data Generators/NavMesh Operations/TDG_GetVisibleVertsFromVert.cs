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
		public bool AutoGenerate = true;

		[Header("RESULTS")]
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

		[ContextMenu("z call SayVertRelational()")]
		public void SayVertRelational()
		{
			CurrentVert.SayAllRelationships();
		}

		[ContextMenu("z call GetVisible()")]
		public void GetVisible()
		{
			ResultPaths = new List<LNX_Path>();
			DBG_Operation = $"starting operation at: '{System.DateTime.Now}'...\n";

			if( Grabber_Vert.CurrentHit == LNX_NavmeshHit.None )
			{
				DBG_Operation += "currently grabber has 'None' hit. Something's wrong...\n";
				return;
			}

			if (CurrentVert == null)
			{
				DBG_Operation += $"CurrentVert is null. Something's wrong...\n";
				return;
			}

			DBG_Operation += $"using currentvert: '{CurrentVert}'...\n" +
				$"Commencing operation...\n";
			DateTime dtStart;
			double totalMs = 0;
			if (UseDebugVersionOfMethod)
			{
				DBG_Operation += $"using debug version...\n";
				mthdDbg_Report.StartReport();
				dtStart = DateTime.Now; ;
				ResultPaths = _navmesh.GetVisibleVertsFromVert_dbg(
					CurrentVert, ref mthdDbg_Report, true, ExcludeVerts
				);
				totalMs = DateTime.Now.Subtract(dtStart).TotalMilliseconds;
				mthdDbg_Report.EndReport();
			}
			else
			{
				DBG_Operation += $"using regular version (as opposed to debug version)...\n";
				dtStart = DateTime.Now; ;
				ResultPaths = _navmesh.GetVisibleVertsFromVert(CurrentVert, true, ExcludeVerts);
				totalMs = DateTime.Now.Subtract(dtStart).TotalMilliseconds;
			}

			DBG_Operation += $"{nameof(ResultPaths)} count: '{ResultPaths.Count}'\n" +
				$"operation took: '{totalMs}' ms...";
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

				LNX_DrawingUtils.DrawTriGizmos( _navmesh.Triangles[Grabber_Vert.CurrentHit.TriangleIndex], Color.yellow, 
					false, false, true, 0.02f, true, 0.1f, false, -1f
				);
			}

			if (Grabber_Vert.RecalculatedLastFrame && AutoGenerate )
			{
				GetVisible();
			}

			Gizmos.color = Color_lines;
			float height = 0.5f;
			for (int i = 0; i < ResultPaths.Count; i++)
			{
				Gizmos.DrawLine(
					CurrentVert.V_Position,
					_navmesh.Triangles[ResultPaths[i].EndTriIndex].Verts[ResultPaths[i].EndHit.VertIndex].V_Position
				);

				Gizmos.DrawLine(
					_navmesh.Triangles[ResultPaths[i].EndTriIndex].Verts[ResultPaths[i].EndHit.VertIndex].V_Position,
					_navmesh.Triangles[ResultPaths[i].EndTriIndex].Verts[ResultPaths[i].EndHit.VertIndex].V_Position + 
					(Vector3.up * height)
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
