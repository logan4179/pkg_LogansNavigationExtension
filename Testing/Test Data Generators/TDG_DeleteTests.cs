using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_DeleteTests : MonoBehaviour
    {
		public string LastWriteTime;

		public LNX_MeshManipulator _Lnx_MeshManipulator;

		public Ray _Ray;

		public List<Vector3> TestMousePositions_face;
		public List<Vector3> TestDirections_face;
		public List<Vector3> CapturedFaceCenterPositions;
		public List<Vector3> GrabbedPositions_face;
		public List<Vector3> GrabbedManipulatorPos_face;
		public List<int> CapturedNumberOfSharedVerts_face;

		[ContextMenu("z call ClearCollections()")]
		public void ClearCollections()
		{
			TestMousePositions_face = new List<Vector3>();
			TestDirections_face = new List<Vector3>();
			CapturedFaceCenterPositions = new List<Vector3>();
			GrabbedPositions_face = new List<Vector3>();
			GrabbedManipulatorPos_face = new List<Vector3>();
			CapturedNumberOfSharedVerts_face = new List<int>();
		}

		[ContextMenu("z call GenerateDerivedCollectionData()")]
		public void GenerateDerivedCollectionData()
		{
			_Lnx_MeshManipulator.ClearSelection();

			List<Vector3> tempTestPositions_face = TestMousePositions_face;
			TestMousePositions_face = new List<Vector3>();
			List<Vector3> tempTestDirections_face = TestDirections_face;
			TestDirections_face = new List<Vector3>();

			CapturedFaceCenterPositions = new List<Vector3>();
			GrabbedPositions_face = new List<Vector3>();
			GrabbedManipulatorPos_face = new List<Vector3>();
			CapturedNumberOfSharedVerts_face = new List<int>();
			_Lnx_MeshManipulator.SelectMode = LNX_SelectMode.Faces;

			for ( int i = 0; i < tempTestPositions_face.Count; i++ )
			{
				_Lnx_MeshManipulator.TryPointAtComponentViaDirection( tempTestPositions_face[i], tempTestDirections_face[i] );

				CaptureMouseInfo( tempTestPositions_face[i], tempTestDirections_face[i] );
			}

		}

		public void CaptureMouseInfo(Vector3 pos, Vector3 dir)
		{
			Debug.Log($"capturing pos '{pos}', and dir: '{dir}'. mode: '{_Lnx_MeshManipulator.SelectMode}'...");

			if ( _Lnx_MeshManipulator.SelectMode != LNX_SelectMode.Faces )
			{
				Debug.LogError($"Error! change mesh manipulator select mode to faces. Returning early...");
				return;
			}

			TestMousePositions_face.Add(pos);
			TestDirections_face.Add(dir);

			Debug.Log( _Lnx_MeshManipulator.Index_TriPointingAt );

			if ( _Lnx_MeshManipulator.Index_TriPointingAt < 0 )
			{
				CapturedFaceCenterPositions.Add( Vector3.zero );

				GrabbedPositions_face.Add(Vector3.zero);
				GrabbedManipulatorPos_face.Add(Vector3.zero);
				CapturedNumberOfSharedVerts_face.Add(0);
				Debug.Log("captured null...");
			}
			else
			{
				Debug.Log($"index of pointing at tri: '{_Lnx_MeshManipulator.Index_TriPointingAt}'");

				CapturedFaceCenterPositions.Add(
					_Lnx_MeshManipulator._LNX_NavMesh.Triangles[_Lnx_MeshManipulator.Index_TriPointingAt].V_Center
				);

				_Lnx_MeshManipulator.TryGrab();


				GrabbedPositions_face.Add(
					_Lnx_MeshManipulator._LNX_NavMesh.Triangles[_Lnx_MeshManipulator.Index_TriLastSelected].V_Center);

				GrabbedManipulatorPos_face.Add(_Lnx_MeshManipulator.manipulatorPos);
				CapturedNumberOfSharedVerts_face.Add(_Lnx_MeshManipulator.Verts_currentlySelected.Count);
			}
		}

		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			if ( !Directory.Exists(TDG_Manager.dirPath_testDataFolder) )
			{
				Debug.LogWarning($"directory: '{TDG_Manager.dirPath_testDataFolder}' wasn't found.");
				return false;
			}

			if ( File.Exists(TDG_Manager.filePath_testData_deleteTests) )
			{
				Debug.LogWarning($"overwriting existing file at: '{TDG_Manager.filePath_testData_deleteTests}'");
			}
			else
			{
				Debug.Log($"writing new file at: '{TDG_Manager.filePath_testData_deleteTests}'");
			}

			File.WriteAllText( TDG_Manager.filePath_testData_deleteTests, JsonUtility.ToJson(this, true) );

			LastWriteTime = System.DateTime.Now.ToString();

			return true;
		}

		[ContextMenu("z call RecreateMeFromJson()")]
		public void RecreateMeFromJson()
		{
			if ( !File.Exists(TDG_Manager.filePath_testData_deleteTests) )
			{
				Debug.LogError($"path '{TDG_Manager.filePath_testData_deleteTests}' didn't exist. returning early...");
				return;
			}

			string myJsonString = File.ReadAllText( TDG_Manager.filePath_testData_deleteTests );

			JsonUtility.FromJsonOverwrite( myJsonString, this );

			EditorUtility.SetDirty(this);
		}
	}
}
