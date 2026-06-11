using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace LogansNavigationExtension
{
    public class TDG_IsInShapeProjectB : TDG_base
    {
		public LNX_ComponentGrabber _grabber_triangle;
		public LNX_ComponentGrabber _grabber_hitPosition;

		public LNX_Triangle CurrentTriangle => _grabber_triangle.CurrentlyGrabbedTriangle;
		public LNX_NavmeshHit CurrentProjectedHit;
		public bool CurrentResult;

		[Header("DATA CAPTURE")]
		public List<Vector3> CapturedStartPositions = new List<Vector3>();
		public List<bool> CapturedResults = new List<bool>();
		public List<LNX_NavmeshHit> CapturedHits = new List<LNX_NavmeshHit>();
		public List<Vector3> CapturedTriCenterPositions = new List<Vector3>();



		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			CapturedStartPositions.Add(transform.position);
			CapturedResults.Add(CurrentResult);
			CapturedHits.Add( CurrentProjectedHit );
			CapturedTriCenterPositions.Add(CurrentTriangle.V_Center);

			DrawDataPointCapture(CapturedStartPositions[CapturedStartPositions.Count - 1],
				CapturedResults[CapturedResults.Count - 1] ? Color.green : Color.red
			);

			if (CapturedResults[CapturedResults.Count - 1])
			{
				DrawDataPointCapture(CapturedStartPositions[CapturedStartPositions.Count - 1],
					Color.green
				);

				DrawDataPointCapture(CapturedHits[CapturedHits.Count - 1],
					Color.green
				);
			}


			Debug.Log($"captured '{CapturedResults[CapturedResults.Count - 1]}', " +
				$"hit '{CapturedHits[CapturedHits.Count - 1]}' at start: " +
				$"'{CapturedStartPositions[CapturedStartPositions.Count - 1]}'");
		}

		protected override void OnDrawGizmos()
		{

			if (Selection.activeGameObject != gameObject &&
				Selection.activeGameObject != _grabber_triangle.gameObject &&
				Selection.activeGameObject != _grabber_hitPosition.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Something wrong with selection...";
				return;
			}

			if (_navmesh == null)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Need to assign a navmesh...";
				Debug.LogWarning($"Need to sample a navmesh...");
				return;
			}

			if (CurrentTriangle == null)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Need to sample a focus triangle...";
				Debug.LogWarning($"Need to sample a focus triangle...");
				return;
			}

			base.OnDrawGizmos();

			if (_grabber_hitPosition.RecalculatedLastFrame)
			{
				DBG_Operation = $"{DateTime.Now}\n" +
					$"Commencing operation using pos: '{_grabber_hitPosition.transform.position}', \n" +
					$"and triangle '{CurrentTriangle.Index_inCollection}'...\n";

				CurrentProjectedHit = LNX_NavmeshHit.None;
				CurrentResult = false;

				if (UseDebugVersion)
				{
					mthdDbg_Report.StartReport();
					CurrentResult = CurrentTriangle.IsInShapeProject_dbg(
						_grabber_hitPosition.transform.position, out CurrentProjectedHit, ref mthdDbg_Report
					);
					mthdDbg_Report.EndReport();
				}
				else
				{
					CurrentResult = CurrentTriangle.IsInShapeProject(
						_grabber_hitPosition.transform.position, out CurrentProjectedHit
					);
				}


				DBG_Operation += $"Operation complete.\n" +
					$"rslt: '{CurrentResult}', at hit: '{CurrentProjectedHit}'.";
			}

			LNX_DrawingUtils.DrawStandardFocusTriGizmos(
				CurrentTriangle, 1f, $"tri{CurrentTriangle.Index_inCollection}", Color.magenta
			);

			Gizmos.color = CurrentResult ? Color.green : Color.red;
			Gizmos.DrawSphere(_grabber_hitPosition.transform.position, Radius_ObjectDebugSpheres);

			if (CurrentResult)
			{
				Gizmos.DrawCube(CurrentProjectedHit.Position, Vector3.one * Radius_ProjectPos);
				Gizmos.DrawLine(_grabber_hitPosition.transform.position, CurrentProjectedHit.Position);
			}



		}


		#region HELPERS ---------------------------------------------------
		[ContextMenu("z call DoEet()")]
		public void DoEet()
		{
			_grabber_hitPosition.transform.position = CurrentTriangle.Edges[0].MidPosition;
		}
		#endregion

		#region WRITING-------------------------------------
		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			bool rslt = TDG_Manager.WriteTestObjectToJson(TDG_Manager.filePath_testData_isInShapeProjectB, this);

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
			if (!File.Exists(TDG_Manager.filePath_testData_isInShapeProjectB))
			{
				Debug.LogError($"path '{TDG_Manager.filePath_testData_isInShapeProjectB}' didn't exist. returning early...");
				return;
			}

			string myJsonString = File.ReadAllText(TDG_Manager.filePath_testData_isInShapeProjectB);

			JsonUtility.FromJsonOverwrite(myJsonString, this);

			EditorUtility.SetDirty(this);
		}
		#endregion
	}
}
