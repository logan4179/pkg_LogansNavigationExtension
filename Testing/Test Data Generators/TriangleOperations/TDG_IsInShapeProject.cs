using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_IsInShapeProject : TDG_base
    {
		public LNX_Triangle CurrentTriangle;
		public Vector3 CurrentProjectedPos;
		public bool CurrentResult;

		[Header("DATA CAPTURE")]
		public List<Vector3> CapturedStartPositions = new List<Vector3>();
		public List<bool> CapturedResults = new List<bool>();
		public List<Vector3> CapturedHitPositions = new List<Vector3>();
		public List<Vector3> CapturedTriCenterPositions = new List<Vector3>();

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			CapturedStartPositions.Add( transform.position );
			CapturedResults.Add( CurrentResult );
			CapturedHitPositions.Add( CurrentProjectedPos );
			CapturedTriCenterPositions.Add( CurrentTriangle.V_Center );

			DrawDataPointCapture(CapturedStartPositions[CapturedStartPositions.Count - 1],
				CapturedResults[CapturedResults.Count-1] ? Color.green : Color.red	
			);

			if(CapturedResults[CapturedResults.Count-1] )
			{
				DrawDataPointCapture(CapturedStartPositions[CapturedStartPositions.Count - 1],
					Color.green
				);

				DrawDataPointCapture(CapturedHitPositions[CapturedHitPositions.Count - 1],
					Color.green
				);
			}


			Debug.Log($"captured '{CapturedResults[CapturedResults.Count-1]}', " +
				$"hit '{CapturedHitPositions[CapturedHitPositions.Count - 1]}' at start: " +
				$"'{CapturedStartPositions[CapturedStartPositions.Count - 1]}'");
		}

		protected override void OnDrawGizmos()
		{
			DBG_Operation = "";

			if ( Selection.activeGameObject != gameObject )
			{
				DBG_Operation += $"OnDrawGizmos short-circuit. Something wrong with selection...";
				return;
			}

			if ( _navmesh == null )
			{
				DBG_Operation += $"OnDrawGizmos short-circuit. Need to assign a navmesh...";
				Debug.LogWarning($"Need to sample a navmesh...");
				return;
			}

			if (CurrentTriangle == null)
			{
				DBG_Operation += $"OnDrawGizmos short-circuit. Need to sample a focus triangle...";
				Debug.LogWarning($"Need to sample a focus triangle...");
				return;
			}

			base.OnDrawGizmos();

			DBG_Operation += $"Commencing operation using triangle '{CurrentTriangle.Index_inCollection}'...\n";

			DrawStandardFocusTriGizmos(CurrentTriangle, 1f, $"tri{CurrentTriangle.Index_inCollection}");

			CurrentProjectedPos = Vector3.zero;
			CurrentResult = false;

			CurrentResult = CurrentTriangle.IsInShapeProject( transform.position, out CurrentProjectedPos );

			Gizmos.color = CurrentResult ? Color.green : Color.red;
			Gizmos.DrawSphere( transform.position, Radius_ObjectDebugSpheres );

			if ( CurrentResult )
			{
				Gizmos.DrawCube( CurrentProjectedPos, Vector3.one * Radius_ProjectPos );
				Gizmos.DrawLine( transform.position, CurrentProjectedPos );
			}


			DBG_Operation += $"Operation complete. rslt: '{CurrentResult}'. \n\n" +
				$"triangle report----------\n" +
				$"{CurrentTriangle.DBG_IsInShapeProject}\n";
		}


		#region HELPERS ---------------------------------------------------
		[ContextMenu("z call SampleFocusTri()")]
		public void SampleFocusTri()
		{
			Debug.Log($"{nameof(SampleFocusTri)}()...");

			LNX_ProjectionHit hit = LNX_ProjectionHit.None;

			if (_navmesh.SamplePosition(transform.position, out hit, 2f, false))
			{
				CurrentTriangle = _navmesh.Triangles[hit.Index_Hit];
				Debug.Log($"Succesful sample! Set new triangle to: '{hit.Index_Hit}'");
			}
			else
			{
				Debug.Log($"sample unsuccesful...");
			}
		}
		#endregion

		#region WRITING-------------------------------------
		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			bool rslt = TDG_Manager.WriteTestObjectToJson(TDG_Manager.filePath_testData_isInShapeProject, this);

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
			if ( !File.Exists(TDG_Manager.filePath_testData_isInShapeProject) )
			{
				Debug.LogError($"path '{TDG_Manager.filePath_testData_isInShapeProject}' didn't exist. returning early...");
				return;
			}

			string myJsonString = File.ReadAllText(TDG_Manager.filePath_testData_isInShapeProject);

			JsonUtility.FromJsonOverwrite(myJsonString, this);

			EditorUtility.SetDirty(this);
		}
		#endregion
	}
}
