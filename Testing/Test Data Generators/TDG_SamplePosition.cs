using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System;

namespace LogansNavigationExtension
{
    public class TDG_SamplePosition : TDG_base
    {
		[SerializeField] private string DBG_GetClosestTri;

		[SerializeField, Range(0f, 0.3f)] private float radius_drawSphere = 0.1f;

		public Vector3[] hitPositions;
		public Vector3[] triCenters;

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();

			if (Selection.activeGameObject != gameObject)
			{
				return;
			}

			DBG_GetClosestTri = $"Searching through '{_mgr.Triangles.Length}' tris...\n";

			LNX_ProjectionHit lnxHit = new LNX_ProjectionHit();

			if (_mgr.SamplePosition(transform.position, out lnxHit, 10f))
			{
				DBG_GetClosestTri += $"found point: '{lnxHit.HitPosition}', on tri: '{lnxHit.Index_intersectedTri}'";
				_debugger.DrawTriGizmos(_mgr.Triangles[lnxHit.Index_intersectedTri], true);

				Vector3 v_to = transform.position - _mgr.Triangles[lnxHit.Index_intersectedTri].V_center;
				Gizmos.color = Color.red;
				Gizmos.DrawSphere(lnxHit.HitPosition, radius_drawSphere);
			}
			else
			{
				DBG_GetClosestTri += $"found nothing...";
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
					hitPositions[i] = lnxHit.HitPosition;
					triCenters[i] = _mgr.Triangles[lnxHit.Index_intersectedTri].V_center;
				}
				else
				{
					DBG_GetClosestTri += $"found nothing...";
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

				if ( triCenters == null || triCenters.Length == 0 )
				{
					Debug.LogWarning($"WARNING! {nameof(triCenters)} was null or 0 count.");
				}
			}

			if ( !Directory.Exists(TDG_Manager.dirPath_testDataFolder) )
			{
				Debug.LogWarning($"directory: '{TDG_Manager.dirPath_testDataFolder}' wasn't found.");
				return false;
			}


			if ( File.Exists(TDG_Manager.filePath_testData_SamplePosition) )
			{
				Debug.LogWarning( $"overwriting existing file at: '{TDG_Manager.filePath_testData_SamplePosition}'" );
			}
			else
			{
				Debug.Log( $"writing new file at: '{TDG_Manager.filePath_testData_SamplePosition}'" );

			}

			File.WriteAllText( TDG_Manager.filePath_testData_SamplePosition, JsonUtility.ToJson(this, true) );

			LastWriteTime = System.DateTime.Now.ToString();

			return true;
		}

		[ContextMenu("z call RecreateMeFromJson()")]
		public void RecreateMeFromJson()
		{
			if ( !File.Exists(TDG_Manager.filePath_testData_SamplePosition) )
			{
				Debug.LogError($"path '{TDG_Manager.filePath_testData_SamplePosition}' didn't exist. returning early...");
				return;
			}

			string myJsonString = File.ReadAllText( TDG_Manager.filePath_testData_SamplePosition );

			JsonUtility.FromJsonOverwrite( myJsonString, this );
		}

		#region Helpers ------------------------------
		[ContextMenu("z SayCurrentResult()")]
		public void SayCurrentResult()
		{
			LNX_ProjectionHit lnxHit = new LNX_ProjectionHit();
			if ( _mgr.SamplePosition(transform.position, out lnxHit, 10f) )
			{
				Debug.Log($"hit pos: '{lnxHit.HitPosition}'");

			}
			else
			{
				DBG_GetClosestTri += $"found nothing...";
			}
		}
		#endregion
	}
}
