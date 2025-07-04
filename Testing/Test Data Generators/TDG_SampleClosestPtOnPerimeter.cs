using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_SampleClosestPtOnPerimeter : TDG_base
    {
		[SerializeField] private string DBG_GetClosest;

		[SerializeField] private Vector3 v_result;

		[SerializeField] private float radius_drawSphere = 0.1f;

		public Vector3[] hitPositions;
		public Vector3[] triCenters;

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();

			if ( Selection.activeGameObject != gameObject )
			{
				return;
			}


			DBG_GetClosest = $"Searching through '{_mgr.Triangles.Length}' tris...\n";

			LNX_ProjectionHit lnxHit = new LNX_ProjectionHit();

			if( _mgr.SamplePosition(transform.position, out lnxHit, 10f) ) //It needs to do this in order to decide which triangle to use...
			{
				DrawTriGizmo( _mgr.Triangles[lnxHit.Index_hitTriangle], Color.yellow );

				Gizmos.color = Color.red;

				v_result = _mgr.Triangles[lnxHit.Index_hitTriangle].ClosestPointOnPerimeter(transform.position);

				Gizmos.DrawSphere(
					v_result, radius_drawSphere );

				Gizmos.DrawLine(transform.position, v_result);
			}
		}

		[ContextMenu("z call GenerateHItResultCollections()")]
		public void GenerateHItResultCollections()
		{
			Debug.Log($"{nameof(GenerateHItResultCollections)}()...");

			LNX_ProjectionHit lnxHit = new LNX_ProjectionHit();
			hitPositions = new Vector3[testPositions.Count];
			triCenters = new Vector3[testPositions.Count];

			for ( int i = 0; i < testPositions.Count; i++ )
			{
				lnxHit = new LNX_ProjectionHit();

				if ( _mgr.SamplePosition(testPositions[i], out lnxHit, 10f) )
				{
					Vector3 v = _mgr.Triangles[lnxHit.Index_hitTriangle].ClosestPointOnPerimeter( testPositions[i] );

					hitPositions[i] = v;
					triCenters[i] = _mgr.Triangles[lnxHit.Index_hitTriangle].V_Center;
				}
			}

			Debug.Log($"generated '{hitPositions.Length}' {nameof(hitPositions)}, and '{triCenters.Length}' {nameof(triCenters)}. " +
				$"this method does NOT write the test data to json.");
		}

		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			if ( testPositions == null || testPositions.Count == 0 )
			{
				Debug.LogWarning($"WARNING! problem positions was null or 0 count.");
			}
			else
			{
				if ( hitPositions == null || hitPositions.Length == 0 )
				{
					Debug.LogWarning($"WARNING! {nameof(hitPositions)} was null or 0 count.");
				}

				if (triCenters == null || triCenters.Length == 0)
				{
					Debug.LogWarning($"WARNING! {nameof(triCenters)} was null or 0 count.");
				}
			}


			if ( !Directory.Exists(TDG_Manager.dirPath_testDataFolder) )
			{
				Debug.LogWarning($"directory: '{TDG_Manager.dirPath_testDataFolder}' wasn't found.");
				return false;
			}


			if ( File.Exists(TDG_Manager.filePath_testData_sampleClosestPtOnPerim) )
			{
				Debug.LogWarning($"overwriting existing file at: '{TDG_Manager.filePath_testData_sampleClosestPtOnPerim}'");
			}
			else
			{
				Debug.Log($"writing new file at: '{TDG_Manager.filePath_testData_sampleClosestPtOnPerim}'");

			}

			File.WriteAllText( TDG_Manager.filePath_testData_sampleClosestPtOnPerim, JsonUtility.ToJson(this, true) );

			LastWriteTime = System.DateTime.Now.ToString();

			return true;
		}

		[ContextMenu("z call RecreateMeFromJson()")]
		public void RecreateMeFromJson()
		{
			if ( !File.Exists(TDG_Manager.filePath_testData_sampleClosestPtOnPerim) )
			{
				Debug.LogError($"path '{TDG_Manager.filePath_testData_sampleClosestPtOnPerim}' didn't exist. returning early...");
				return;
			}

			string myJsonString = File.ReadAllText( TDG_Manager.filePath_testData_sampleClosestPtOnPerim );

			JsonUtility.FromJsonOverwrite( myJsonString, this );
		}
	}
}