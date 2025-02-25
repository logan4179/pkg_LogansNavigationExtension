using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System;

namespace LogansNavigationExtension
{
    public class Test_SamplePosition : LNX_Test_base
    {
		[SerializeField] private string DBG_GetClosestTri;

		[SerializeField, Range(0f, 0.3f)] private float radius_drawSphere = 0.1f;

		public Vector3[] hitPositions;
		public Vector3[] triCenters;

		[SerializeField] string fileName;

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

		[ContextMenu("z call GenerateDataCollections()")]
		public void GenerateDataCollections()
		{
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
		}


		[ContextMenu("z call WiteMeToJson()")]
		public void WiteMeToJson()
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

			string filePath = $"{Directory.GetCurrentDirectory()}\\Packages\\LogansNavigationExtension\\Testing\\Unit Tests\\Test Data";

			if (!Directory.Exists(filePath))
			{
				Debug.LogWarning($"directory: '{filePath}' wasn't found.");
				return;
			}

			filePath = Path.Combine(filePath, $"{fileName}.json");

			if (File.Exists(filePath))
			{
				Debug.LogWarning($"overwriting existing file at: '{filePath}'");
			}
			else
			{
				Debug.Log($"writing new file at: '{filePath}'");

			}

			File.WriteAllText( filePath, JsonUtility.ToJson(this, true) );
		}

		[ContextMenu("z call RecreateMeFromJson()")]
		public void RecreateMeFromJson()
		{
			string filePath = Path.Combine($"{Directory.GetCurrentDirectory()}\\Packages\\LogansNavigationExtension\\Testing\\Unit Tests\\Test Data", 
				$"{fileName}.json");

			if ( !File.Exists(filePath) )
			{
				Debug.LogError($"path '{filePath}' didn't exist. returning early...");
				return;
			}

			string myJsonString = File.ReadAllText( filePath );

			JsonUtility.FromJsonOverwrite( myJsonString, this );
		}
	}
}
