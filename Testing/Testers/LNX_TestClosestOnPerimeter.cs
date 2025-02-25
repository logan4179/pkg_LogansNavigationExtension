using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class LNX_TestClosestOnPerimeter : LNX_Test_base
    {
		[SerializeField] private string DBG_GetClosest;

		[SerializeField] private Vector3 v_result;

		[SerializeField] private float radius_drawSphere = 0.1f;

		public Vector3[] resultPositions;
		public Vector3[] triCenters;

		public string fileName;

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
				DrawTriGizmo( _mgr.Triangles[lnxHit.Index_intersectedTri], Color.yellow );

				Gizmos.color = Color.red;

				v_result = _mgr.Triangles[lnxHit.Index_intersectedTri].ClosestPointOnPerimeter(transform.position);

				Gizmos.DrawSphere(
					v_result, radius_drawSphere );

				Gizmos.DrawLine(transform.position, v_result);
			}
		}

		[ContextMenu("z call GenerateDataCollections()")]
		public void GenerateDataCollections()
		{
			LNX_ProjectionHit lnxHit = new LNX_ProjectionHit();
			resultPositions = new Vector3[testPositions.Count];
			triCenters = new Vector3[testPositions.Count];

			for ( int i = 0; i < testPositions.Count; i++ )
			{
				lnxHit = new LNX_ProjectionHit();

				if ( _mgr.SamplePosition(testPositions[i], out lnxHit, 10f) )
				{
					Vector3 v = _mgr.Triangles[lnxHit.Index_intersectedTri].ClosestPointOnPerimeter( testPositions[i] );

					resultPositions[i] = v;
					triCenters[i] = _mgr.Triangles[lnxHit.Index_intersectedTri].V_center;
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
				if ( resultPositions == null || resultPositions.Length == 0 )
				{
					Debug.LogWarning($"WARNING! {nameof(resultPositions)} was null or 0 count.");
				}

				if (triCenters == null || triCenters.Length == 0)
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

			File.WriteAllText(filePath, JsonUtility.ToJson(this, true));
		}

		[ContextMenu("z call RecreateMeFromJson()")]
		public void RecreateMeFromJson()
		{
			string filePath = Path.Combine($"{Directory.GetCurrentDirectory()}\\Packages\\LogansNavigationExtension\\Testing\\Unit Tests\\Test Data",
				$"{fileName}.json");

			if (!File.Exists(filePath))
			{
				Debug.LogError($"path '{filePath}' didn't exist. returning early...");
				return;
			}

			string myJsonString = File.ReadAllText(filePath);

			JsonUtility.FromJsonOverwrite(myJsonString, this);
		}
	}
}