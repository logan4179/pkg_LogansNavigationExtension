using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;


namespace LogansNavigationExtension
{
    public class TDG_pointingAndGrabbing : MonoBehaviour
    {
		public string LastWriteTime;

		public LNX_MeshManipulator _Lnx_MeshManipulator;

		public Ray _Ray;

		public List<Vector3> TestMousePositions_vert;
		public List<Vector3> TestMouseDirections_vert;
		public List<Vector3> CapturedPointedAtVertPositions;
		public List<Vector3> CapturedGrabbedPositions_vert;
		public List<Vector3> CapturedGrabbedManipulatorPos_vert;
		[Space(5f)]

		public List<Vector3> TestMousePositions_edge;
		public List<Vector3> TestMouseDirections_edge;
		public List<Vector3> CapturedPointedAtEdgeMidPositions;
		public List<Vector3> CapturedGrabbedPositions_edge;
		public List<Vector3> CapturedGrabbedManipulatorPos_edge;
		[Space(5f)]

		public List<Vector3> TestMousePositions_face;
		public List<Vector3> TestMouseDirections_face;
		public List<Vector3> CapturedPointedAtFaceCenterPositions;
		public List<Vector3> CapturedGrabbedPositions_face;
		public List<Vector3> CapturedGrabbedManipulatorPos_face;

		[ContextMenu("z call ClearCollections()")]
		public void ClearCollections()
		{
			TestMousePositions_vert = new List<Vector3>();
			TestMouseDirections_vert = new List<Vector3>();
			CapturedPointedAtVertPositions = new List<Vector3>();
			CapturedGrabbedPositions_vert = new List<Vector3>();
			CapturedGrabbedManipulatorPos_vert = new List<Vector3>();

			TestMousePositions_edge = new List<Vector3>();
			TestMouseDirections_edge = new List<Vector3>();
			CapturedPointedAtEdgeMidPositions = new List<Vector3>();
			CapturedGrabbedPositions_edge = new List<Vector3>();
			CapturedGrabbedManipulatorPos_edge = new List<Vector3>();

			TestMousePositions_face = new List<Vector3>();
			TestMouseDirections_face = new List<Vector3>();
			CapturedPointedAtFaceCenterPositions = new List<Vector3>();
			CapturedGrabbedPositions_face = new List<Vector3>();
			CapturedGrabbedManipulatorPos_face = new List<Vector3>();
		}

		[ContextMenu("z call GenerateDerivedCollectionData()")]
		public void GenerateDerivedCollectionData()
		{
			_Lnx_MeshManipulator.ClearSelection();

			List<Vector3> tempTestPositions_vert = TestMousePositions_vert;
			TestMousePositions_vert = new List<Vector3>();
			List<Vector3> tempTestDirections_vert = TestMouseDirections_vert;
			TestMouseDirections_vert = new List<Vector3>();

			CapturedPointedAtVertPositions = new List<Vector3>();
			//PointedAtNumberOfSharedVerts = new List<int>();
			CapturedGrabbedPositions_vert = new List<Vector3>();
			CapturedGrabbedManipulatorPos_vert = new List<Vector3>();
			_Lnx_MeshManipulator.SelectMode = LNX_SelectMode.Vertices;
			for ( int i = 0; i < tempTestPositions_vert.Count; i++ )
			{
				_Lnx_MeshManipulator.TryPointAtComponentViaDirection( tempTestPositions_vert[i], tempTestDirections_vert[i] );

				CaptureMouseInfo( tempTestPositions_vert[i], tempTestDirections_vert[i] );
			}

			List<Vector3> tempTestPositions_edge = TestMousePositions_edge;
			TestMousePositions_edge = new List<Vector3>();
			List<Vector3> tempTestDirections_edge = TestMouseDirections_edge;
			TestMouseDirections_edge = new List<Vector3>();

			CapturedPointedAtEdgeMidPositions = new List<Vector3>();
			CapturedGrabbedPositions_edge = new List<Vector3>();
			CapturedGrabbedManipulatorPos_edge = new List<Vector3>();
			_Lnx_MeshManipulator.SelectMode = LNX_SelectMode.Edges;
			
			for ( int i = 0; i < tempTestPositions_edge.Count; i++ )
			{
				_Lnx_MeshManipulator.TryPointAtComponentViaDirection(tempTestPositions_edge[i], tempTestDirections_edge[i] );

				CaptureMouseInfo( tempTestPositions_edge[i], tempTestDirections_edge[i] );
			}

			List<Vector3> tempTestPositions_face = TestMousePositions_face;
			TestMousePositions_face = new List<Vector3>();
			List<Vector3> tempTestDirections_face = TestMouseDirections_face;
			TestMouseDirections_face = new List<Vector3>();

			CapturedPointedAtFaceCenterPositions = new List<Vector3>();
			CapturedGrabbedPositions_face = new List<Vector3>();
			CapturedGrabbedManipulatorPos_face = new List<Vector3>();
			_Lnx_MeshManipulator.SelectMode = LNX_SelectMode.Faces;
			
			for ( int i = 0; i < tempTestPositions_face.Count; i++ )
			{
				_Lnx_MeshManipulator.TryPointAtComponentViaDirection(tempTestPositions_face[i], tempTestDirections_face[i] );

				CaptureMouseInfo( tempTestPositions_face[i], tempTestDirections_face[i] );
			}
			
		}

		public void CaptureMouseInfo( Vector3 pos, Vector3 dir )
        {
			Debug.Log($"capturing pos '{pos}', and dir: '{dir}'. mode: '{_Lnx_MeshManipulator.SelectMode}'...");

			if( _Lnx_MeshManipulator.SelectMode == LNX_SelectMode.None )
			{
				Debug.LogError($"Error! change mesh manipulator select mode to something other than 'none'. Returning early...");
				return;
			}

			if( _Lnx_MeshManipulator.SelectMode == LNX_SelectMode.Vertices )
			{
				TestMousePositions_vert.Add( pos );
				TestMouseDirections_vert.Add( dir );

				if ( _Lnx_MeshManipulator.Vert_CurrentlyPointingAt == null )
				{
					CapturedPointedAtVertPositions.Add( Vector3.zero );
					//PointedAtNumberOfSharedVerts.Add( 0 );

					CapturedGrabbedPositions_vert.Add( Vector3.zero );
					CapturedGrabbedManipulatorPos_vert.Add( Vector3.zero );
					Debug.Log("captured null...");
				}
				else
				{
					CapturedPointedAtVertPositions.Add( _Lnx_MeshManipulator.Vert_CurrentlyPointingAt.Position );

					_Lnx_MeshManipulator.TryGrab();
					//ointedAtNumberOfSharedVerts.Add( _Lnx_MeshManipulator.Verts_currentlySelected.Count );
					CapturedGrabbedPositions_vert.Add( _Lnx_MeshManipulator.Vert_LastSelected.Position );
					CapturedGrabbedManipulatorPos_vert.Add( _Lnx_MeshManipulator.manipulatorPos );
					Debug.Log($"{_Lnx_MeshManipulator.Verts_currentlySelected.Count}");
				}
			}
			else if( _Lnx_MeshManipulator.SelectMode == LNX_SelectMode.Edges )
			{
				TestMousePositions_edge.Add( pos );
				TestMouseDirections_edge.Add( dir );

				if ( _Lnx_MeshManipulator.Edge_CurrentlyPointingAt == null )
				{
					CapturedPointedAtEdgeMidPositions.Add( Vector3.zero );

					CapturedGrabbedPositions_edge.Add( Vector3.zero );
					CapturedGrabbedManipulatorPos_edge.Add( Vector3.zero );

					Debug.Log("captured null...");
				}
				else
				{
					CapturedPointedAtEdgeMidPositions.Add( _Lnx_MeshManipulator.Edge_CurrentlyPointingAt.MidPosition );

					_Lnx_MeshManipulator.TryGrab();
					CapturedGrabbedPositions_edge.Add( _Lnx_MeshManipulator.Edge_LastSelected.MidPosition );
					CapturedGrabbedManipulatorPos_edge.Add( _Lnx_MeshManipulator.manipulatorPos );

					Debug.Log($"{_Lnx_MeshManipulator.Edges_currentlySelected.Count}");

				}
			}
			else if( _Lnx_MeshManipulator.SelectMode == LNX_SelectMode.Faces )
			{
				TestMousePositions_face.Add( pos );
				TestMouseDirections_face.Add( dir );

				Debug.Log(_Lnx_MeshManipulator.Index_TriPointingAt);

				if ( _Lnx_MeshManipulator.Index_TriPointingAt < 0 )
				{
					CapturedPointedAtFaceCenterPositions.Add( Vector3.zero );

					CapturedGrabbedPositions_face.Add( Vector3.zero );
					CapturedGrabbedManipulatorPos_face.Add( Vector3.zero );
					Debug.Log("captured null...");
				}
				else
				{
					Debug.Log($"index of pointing at tri: '{_Lnx_MeshManipulator.Index_TriPointingAt}'");

					CapturedPointedAtFaceCenterPositions.Add( 
						_Lnx_MeshManipulator._LNX_NavMesh.Triangles[_Lnx_MeshManipulator.Index_TriPointingAt].V_center 
					);

					_Lnx_MeshManipulator.TryGrab();


					CapturedGrabbedPositions_face.Add( 
						_Lnx_MeshManipulator._LNX_NavMesh.Triangles[_Lnx_MeshManipulator.Index_TriLastSelected].V_center );

					CapturedGrabbedManipulatorPos_face.Add( _Lnx_MeshManipulator.manipulatorPos );
				}
			}
		}


		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			if ( !Directory.Exists(TDG_Manager.dirPath_testDataFolder) )
			{
				Debug.LogWarning($"directory: '{TDG_Manager.filePath_testData_pointingAndGrabbing}' wasn't found.");
				return false;
			}

			if ( File.Exists(TDG_Manager.filePath_testData_pointingAndGrabbing) )
			{
				Debug.LogWarning($"overwriting existing file at: '{TDG_Manager.filePath_testData_pointingAndGrabbing}'");
			}
			else
			{
				Debug.Log($"writing new file at: '{TDG_Manager.filePath_testData_pointingAndGrabbing}'");

			}

			File.WriteAllText( TDG_Manager.filePath_testData_pointingAndGrabbing, JsonUtility.ToJson(this, true) );

			LastWriteTime = System.DateTime.Now.ToString();

			return true;
		}

		[ContextMenu("z call RecreateMeFromJson()")]
		public void RecreateMeFromJson()
		{
			if ( !File.Exists(TDG_Manager.filePath_testData_pointingAndGrabbing) )
			{
				Debug.LogError($"path '{TDG_Manager.filePath_testData_pointingAndGrabbing}' didn't exist. returning early...");
				return;
			}

			string myJsonString = File.ReadAllText( TDG_Manager.filePath_testData_pointingAndGrabbing );

			JsonUtility.FromJsonOverwrite( myJsonString, this );

			EditorUtility.SetDirty( this );
		}
	}
}
