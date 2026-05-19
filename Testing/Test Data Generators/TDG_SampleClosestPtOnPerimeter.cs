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
		LNX_NavmeshHit lnxHit = new LNX_NavmeshHit();

		[Header("DEBUG")]
		[Range(0,0.3f)] public float radius_testObject;
		[Range(0, 0.05f)] public float size_sampleObject;

		public Color Color_IFSuccess = Color.white;
		public Color Color_IfFailure = Color.white;

		public Color Color_sampleObject = Color.white;


		protected override void OnDrawGizmos()
		{
			if ( AmInUnitTest || Selection.activeGameObject != gameObject )
			{
				return;
			}

			base.OnDrawGizmos();

			DBG_Operation = $"Searching through '{_navmesh.Triangles.Length}' tris...\n";

			lnxHit = new LNX_NavmeshHit();

			if( _navmesh.SamplePosition(transform.position, out lnxHit, 10f) ) //It needs to do this in order to decide which triangle to use...
			{
				LNX_Triangle chosenTri = _navmesh.Triangles[lnxHit.TriangleIndex];

				DrawStandardFocusTriGizmos( chosenTri, 0.3f, lnxHit.TriangleIndex.ToString(), Color.magenta );

				v_result = chosenTri.ClosestPointOnPerimeter(transform.position);

				Gizmos.color = Color_sampleObject;

				Gizmos.DrawCube( v_result, Vector3.one * size_sampleObject );
				Gizmos.DrawLine(transform.position, v_result);

				Gizmos.color = Color_IFSuccess;
			}
			else
			{
				DBG_Operation += $"Couldn't sample position...\n";

				Gizmos.color = Color_IfFailure;
			}

			Gizmos.DrawSphere( transform.position, radius_testObject );
		}

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{


			Debug.Log($"captured...");
		}

		[ContextMenu("z call GenerateHItResultCollections()")]
		public void GenerateHItResultCollections()
		{
			//Debug.Log($"{nameof(GenerateHItResultCollections)}()...");

		}

		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
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