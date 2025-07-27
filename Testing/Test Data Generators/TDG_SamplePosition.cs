using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.Profiling;

namespace LogansNavigationExtension
{
    public class TDG_SamplePosition : TDG_base
    {
		public List<Vector3> samplePositions;
		public List<Vector3> capturedHitPositions;
		public List<Vector3> capturedTriCenters;

		[Header("For data")]
		public bool SamplePositionResult;
		LNX_ProjectionHit lnxHit = new LNX_ProjectionHit();

		[Header("DEBUG")]
		[SerializeField, Range(0f, 0.3f)] private float radius_drawSphere = 0.1f;
		public Color color_success;
		public Color color_fail;
		public Color Color_sampleObject;
		[Range(0f,0.2f)] public float size_sampleObject = 0.15f;
		[SerializeField] private string DBG_Class;


		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();

			if (Selection.activeGameObject != gameObject)
			{
				return;
			}

			DBG_Class = $"";
			SamplePositionResult = false;
			
			lnxHit = new LNX_ProjectionHit();

			if ( _navmesh.SamplePosition(transform.position, out lnxHit, 10f) )
			{
				DBG_Class += $"samplePosition returned true with: '{lnxHit.HitPosition}', on tri: '{lnxHit.Index_Hit}'\n" +
					$"report: \n" +
					$"{_navmesh.DBG_SamplePosition}\n";

				
				DrawStandardFocusTriGizmos(
					_navmesh.Triangles[lnxHit.Index_Hit],
					1f,
					_navmesh.Triangles[lnxHit.Index_Hit].Index_inCollection.ToString()
				);

				Gizmos.color = Color_sampleObject;
				Gizmos.DrawLine( transform.position, lnxHit.HitPosition );
				Gizmos.DrawCube( lnxHit.HitPosition, Vector3.one * size_sampleObject );
				Handles.Label( lnxHit.HitPosition + (Vector3.up * 0.015f), lnxHit.HitPosition.ToString() );

				Gizmos.color = color_success;
			}
			else
			{
				DBG_Class += $"samplePosition() returned false. Report:...\n" +
					$"{_navmesh.DBG_SamplePosition}\n";
				Gizmos.color = color_fail;
			}

			Gizmos.DrawSphere( transform.position, radius_drawSphere );
		}

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			samplePositions.Add( transform.position );
			capturedHitPositions.Add( lnxHit.HitPosition );
			capturedTriCenters.Add( _navmesh.Triangles[lnxHit.Index_Hit].V_Center );

			Debug.Log($"Captured ");
		}

		[ContextMenu("z call GenerateHItResultCollections()")]
		public void GenerateHItResultCollections()
		{
			Debug.Log($"{nameof(GenerateHItResultCollections)}()...");

			LNX_ProjectionHit lnxHit = new LNX_ProjectionHit();
			samplePositions = new List<Vector3>();
			capturedHitPositions = new List<Vector3>();
			capturedTriCenters = new List<Vector3>();

			for ( int i = 0; i < problemPositions.Count; i++ )
			{
				samplePositions.Add( problemPositions[i] );

				lnxHit = new LNX_ProjectionHit();
				if ( _navmesh.SamplePosition(problemPositions[i], out lnxHit, 10f) )
				{
					capturedHitPositions.Add( lnxHit.HitPosition );
					capturedTriCenters.Add( _navmesh.Triangles[lnxHit.Index_Hit].V_Center );
				}
				else
				{
					DBG_Class += $"found nothing...";
				}
			}

			Debug.Log($"generated '{capturedHitPositions.Count}' {nameof(capturedHitPositions)}, and " +
				$"'{capturedTriCenters.Count}' {nameof(capturedTriCenters)}. " +
				$"this method does NOT write the test data to json.");
		}


		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			if ( problemPositions == null || problemPositions.Count == 0 )
			{
				Debug.LogWarning($"WARNING! problem positions was null or 0 count.");
			}
			else
			{
				if ( capturedHitPositions == null || capturedHitPositions.Count == 0 )
				{
					Debug.LogWarning($"WARNING! {nameof(capturedHitPositions)} was null or 0 count.");
				}

				if ( capturedTriCenters == null || capturedTriCenters.Count == 0 )
				{
					Debug.LogWarning($"WARNING! {nameof(capturedTriCenters)} was null or 0 count.");
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
			if ( _navmesh.SamplePosition(transform.position, out lnxHit, 10f) )
			{
				Debug.Log($"hit pos: '{lnxHit.HitPosition}'");

			}
			else
			{
				DBG_Class += $"found nothing...";
			}
		}
		#endregion
	}
}
