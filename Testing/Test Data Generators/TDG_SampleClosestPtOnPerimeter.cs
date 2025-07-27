using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LogansNavigationExtension
{
    public class TDG_SampleClosestPtOnPerimeter : TDG_base
    {

		[SerializeField] private Vector3 v_result;
		LNX_ProjectionHit lnxHit = new LNX_ProjectionHit();


		[SerializeField] private float radius_drawSphere = 0.1f;

		public List<Vector3> SampleFromPositions = new List<Vector3>();
		public List<Vector3> capturedPerimeterPositions = new List<Vector3>();
		public List<Vector3> capturedTriCenters = new List<Vector3>();

		[Header("DEBUG")]
		[Range(0,0.3f)] public float radius_testObject;
		[Range(0, 0.05f)] public float size_sampleObject;

		public Color Color_IFSuccess = Color.white;
		public Color Color_IfFailure = Color.white;

		public Color Color_sampleObject = Color.white;

		[SerializeField] private string DBG_Class;


		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();

			if ( Selection.activeGameObject != gameObject )
			{
				return;
			}

			DBG_Class = $"Searching through '{_navmesh.Triangles.Length}' tris...\n";

			lnxHit = new LNX_ProjectionHit();

			if( _navmesh.SamplePosition(transform.position, out lnxHit, 10f) ) //It needs to do this in order to decide which triangle to use...
			{
				DrawStandardFocusTriGizmos( _navmesh.Triangles[lnxHit.Index_Hit], 0.3f, lnxHit.Index_Hit.ToString() );


				v_result = _navmesh.Triangles[lnxHit.Index_Hit].ClosestPointOnPerimeter(transform.position);

				Gizmos.color = Color_sampleObject;

				Gizmos.DrawCube( v_result, Vector3.one * size_sampleObject );
				Gizmos.DrawLine(transform.position, v_result);

				Gizmos.color = Color_IFSuccess;
			}
			else
			{
				DBG_Class += $"Couldn't sample position...\n";

				Gizmos.color = Color_IfFailure;
			}

			Gizmos.DrawSphere( transform.position, radius_testObject );
		}

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			SampleFromPositions.Add( transform.position );
			capturedPerimeterPositions.Add( v_result );
			capturedTriCenters.Add( _navmesh.Triangles[lnxHit.Index_Hit].V_Center );

			Debug.Log($"captured...");
		}

		[ContextMenu("z call GenerateHItResultCollections()")]
		public void GenerateHItResultCollections()
		{
			Debug.Log($"{nameof(GenerateHItResultCollections)}()...");

			LNX_ProjectionHit lnxHit = new LNX_ProjectionHit();
			capturedPerimeterPositions = new List<Vector3>();
			capturedTriCenters = new List<Vector3>();

			for ( int i = 0; i < SampleFromPositions.Count; i++ )
			{
				lnxHit = new LNX_ProjectionHit();

				if ( _navmesh.SamplePosition(SampleFromPositions[i], out lnxHit, 10f) )
				{
					Vector3 v = _navmesh.Triangles[lnxHit.Index_Hit].ClosestPointOnPerimeter( SampleFromPositions[i] );

					capturedPerimeterPositions[i] = v;
					capturedTriCenters[i] = _navmesh.Triangles[lnxHit.Index_Hit].V_Center;
				}
			}

			Debug.Log($"generated '{capturedPerimeterPositions.Count}' {nameof(capturedPerimeterPositions)}, and " +
				$"'{capturedTriCenters.Count}' {nameof(capturedTriCenters)}. " +
				$"this method does NOT write the test data to json.");
		}

		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			if ( SampleFromPositions == null || SampleFromPositions.Count == 0 )
			{
				Debug.LogWarning($"WARNING! problem positions was null or 0 count.");
			}
			else
			{
				if ( capturedPerimeterPositions == null || capturedPerimeterPositions.Count == 0 )
				{
					Debug.LogWarning($"WARNING! {nameof(capturedPerimeterPositions)} was null or 0 count.");
				}

				if (capturedTriCenters == null || capturedTriCenters.Count == 0)
				{
					Debug.LogWarning($"WARNING! {nameof(capturedTriCenters)} was null or 0 count.");
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