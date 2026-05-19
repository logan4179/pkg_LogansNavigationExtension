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
		public LNX_ComponentGrabber _Grabber;
		public LNX_Triangle _SampledTriangle => _Grabber.CurrentlyGrabbedTriangle;

		[Header("For data")]
		public bool OperationResult;
		LNX_NavmeshHit lnxHit = new LNX_NavmeshHit();

		[Header("DEBUG")]
		public Color color_success;
		public Color color_fail;
		public Color Color_sampleObject;
		[Range(0f,0.2f)] public float size_sampleObject = 0.15f;


		protected override void OnDrawGizmos()
		{
			if ( AmInUnitTest || Selection.activeGameObject != gameObject)
			{
				return;
			}

			base.OnDrawGizmos();

			if( _Grabber.RecalculatedLastFrame )
			{
				lnxHit = new LNX_NavmeshHit();

				DBG_Operation = $"{DateTime.Now}\n" +
					$"Now sampling position...\n";

				OperationResult = _navmesh.SamplePosition( _Grabber.transform.position, out lnxHit, 10f );

				if ( OperationResult )
				{
					DBG_Operation += $"samplePosition returned true with: '{lnxHit}'\n";

					LNX_Triangle sampledTri = _navmesh.Triangles[lnxHit.TriangleIndex];
				}
				else
				{
					DBG_Operation += $"samplePosition() returned false\n";
				}
			}

			#region	GIZMO DRAWING ===============================
			if ( OperationResult )
			{
				if( _SampledTriangle != null )
				{
					LNX_DrawingUtils.DrawStandardFocusTriGizmos(
						_SampledTriangle,
						1f,
						_SampledTriangle.Index_inCollection.ToString(), Color.magenta
					);
				}


				//Gizmos.color = color_success;
				Gizmos.color = Color_sampleObject;
				Gizmos.DrawLine(_Grabber.transform.position, lnxHit.Position );
				Gizmos.DrawCube( lnxHit.Position, Vector3.one * size_sampleObject );
				Handles.Label( lnxHit.Position + (Vector3.up * 0.015f), lnxHit.Position.ToString() );
			}

			#endregion

			//Gizmos.DrawSphere(_Grabber.transform.position, Radius_ObjectDebugSpheres );
		}

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			_dataCapture.CaptureDataPoint(
				_Grabber.transform.position, lnxHit.Position, _navmesh.Triangles[lnxHit.TriangleIndex].V_Center
			);

			Debug.Log($"Captured ");
		}

		[ContextMenu("z call GenerateHItResultCollections()")]
		public void GenerateHItResultCollections()
		{
			//Debug.Log($"{nameof(GenerateHItResultCollections)}()...");
			/*
			LNX_NavmeshHit lnxHit = new LNX_NavmeshHit();
			samplePositions = new List<Vector3>();
			capturedHitPositions = new List<Vector3>();
			capturedTriCenters = new List<Vector3>();
			
			for ( int i = 0; i < problemPositions.Count; i++ )
			{
				samplePositions.Add( problemPositions[i] );

				lnxHit = new LNX_NavmeshHit();
				if ( _navmesh.SamplePosition(problemPositions[i], out lnxHit, 10f) )
				{
					capturedHitPositions.Add( lnxHit.HitPosition );
					capturedTriCenters.Add( _navmesh.Triangles[lnxHit.TriIndex].V_Center );
				}
				else
				{
					DBG_Class += $"found nothing...";
				}
			}


			Debug.Log($"generated '{capturedHitPositions.Count}' {nameof(capturedHitPositions)}, and " +
				$"'{capturedTriCenters.Count}' {nameof(capturedTriCenters)}. " +
				$"this method does NOT write the test data to json.");
			*/
		}


		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			/*
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
			*/

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

		#endregion
	}
}
