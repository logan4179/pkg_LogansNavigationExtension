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

		public List<Vector3> TestPositions_vert;
		public List<Vector3> TestDirections_vert;
		public List<Vector3> CapturedVertPositions;
		public List<int> CapturedNumberOfSharedVerts;
		public List<Vector3> GrabbedPositions_vert;
		public List<Vector3> GrabbedManipulatorPos_vert;
		[Space(5f)]

		public List<Vector3> TestPositions_edge;
		public List<Vector3> TestDirections_edge;
		public List<Vector3> CapturedEdgeCenterPositions;
		public List<int> CapturedNumberOfSharedEdges;
		public List<int> CapturedNumberOfSharedVerts_edge;
		public List<Vector3> GrabbedPositions_edge;
		public List<Vector3> GrabbedManipulatorPos_edge;
		[Space(5f)]

		public List<Vector3> TestPositions_face;
		public List<Vector3> TestDirections_face;
		public List<Vector3> CapturedFaceCenterPositions;
		public List<Vector3> GrabbedPositions_face;
		public List<Vector3> GrabbedManipulatorPos_face;
		public List<int> CapturedNumberOfSharedVerts_face;

		[ContextMenu("z call ClearCollections()")]
		public void ClearCollections()
		{
			TestPositions_vert = new List<Vector3>();
			TestDirections_vert = new List<Vector3>();
			CapturedVertPositions = new List<Vector3>();
			CapturedNumberOfSharedVerts = new List<int>();
			GrabbedPositions_vert = new List<Vector3>();
			GrabbedManipulatorPos_vert = new List<Vector3>();

			TestPositions_edge = new List<Vector3>();
			TestDirections_edge = new List<Vector3>();
			CapturedEdgeCenterPositions = new List<Vector3>();
			CapturedNumberOfSharedEdges = new List<int>();
			GrabbedPositions_edge = new List<Vector3>();
			GrabbedManipulatorPos_edge = new List<Vector3>();
			CapturedNumberOfSharedVerts_edge = new List<int>();

			TestPositions_face = new List<Vector3>();
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

			List<Vector3> tempTestPositions_vert = TestPositions_vert;
			TestPositions_vert = new List<Vector3>();
			List<Vector3> tempTestDirections_vert = TestDirections_vert;
			TestDirections_vert = new List<Vector3>();

			CapturedVertPositions = new List<Vector3>();
			CapturedNumberOfSharedVerts = new List<int>();
			GrabbedPositions_vert = new List<Vector3>();
			GrabbedManipulatorPos_vert = new List<Vector3>();
			_Lnx_MeshManipulator.SelectMode = LNX_SelectMode.Vertices;
			for ( int i = 0; i < tempTestPositions_vert.Count; i++ )
			{
				_Lnx_MeshManipulator.TryPointAtComponentViaDirection( tempTestPositions_vert[i], tempTestDirections_vert[i] );

				CaptureMouseInfo( tempTestPositions_vert[i], tempTestDirections_vert[i] );
			}

			List<Vector3> tempTestPositions_edge = TestPositions_edge;
			TestPositions_edge = new List<Vector3>();
			List<Vector3> tempTestDirections_edge = TestDirections_edge;
			TestDirections_edge = new List<Vector3>();

			CapturedEdgeCenterPositions = new List<Vector3>();
			CapturedNumberOfSharedEdges = new List<int>();
			GrabbedPositions_edge = new List<Vector3>();
			GrabbedManipulatorPos_edge = new List<Vector3>();
			CapturedNumberOfSharedVerts_edge = new List<int>();
			_Lnx_MeshManipulator.SelectMode = LNX_SelectMode.Edges;
			
			for ( int i = 0; i < tempTestPositions_edge.Count; i++ )
			{
				_Lnx_MeshManipulator.TryPointAtComponentViaDirection(tempTestPositions_edge[i], tempTestDirections_edge[i] );

				CaptureMouseInfo( tempTestPositions_edge[i], tempTestDirections_edge[i] );
			}

			List<Vector3> tempTestPositions_face = TestPositions_face;
			TestPositions_face = new List<Vector3>();
			List<Vector3> tempTestDirections_face = TestDirections_face;
			TestDirections_face = new List<Vector3>();

			CapturedFaceCenterPositions = new List<Vector3>();
			GrabbedPositions_face = new List<Vector3>();
			GrabbedManipulatorPos_face = new List<Vector3>();
			CapturedNumberOfSharedVerts_face = new List<int>();
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
				TestPositions_vert.Add( pos );
				TestDirections_vert.Add( dir );

				if ( _Lnx_MeshManipulator.Vert_CurrentlyPointingAt == null )
				{
					CapturedVertPositions.Add( Vector3.zero );
					CapturedNumberOfSharedVerts.Add( 0 );
					GrabbedPositions_vert.Add( Vector3.zero );
					GrabbedManipulatorPos_vert.Add( Vector3.zero );
					Debug.Log("captured null...");
				}
				else
				{
					CapturedVertPositions.Add( _Lnx_MeshManipulator.Vert_CurrentlyPointingAt.Position );

					_Lnx_MeshManipulator.TryGrab();
					CapturedNumberOfSharedVerts.Add( _Lnx_MeshManipulator.Verts_currentlySelected.Count );
					GrabbedPositions_vert.Add( _Lnx_MeshManipulator.Vert_LastSelected.Position );
					GrabbedManipulatorPos_vert.Add( _Lnx_MeshManipulator.manipulatorPos );
					Debug.Log($"{_Lnx_MeshManipulator.Verts_currentlySelected.Count}");
				}
			}
			else if( _Lnx_MeshManipulator.SelectMode == LNX_SelectMode.Edges )
			{
				TestPositions_edge.Add( pos );
				TestDirections_edge.Add( dir );

				if ( _Lnx_MeshManipulator.Edge_CurrentlyPointingAt == null )
				{
					CapturedEdgeCenterPositions.Add( Vector3.zero );

					CapturedNumberOfSharedEdges.Add(0);
					GrabbedPositions_edge.Add( Vector3.zero );
					GrabbedManipulatorPos_edge.Add( Vector3.zero );
					CapturedNumberOfSharedVerts_edge.Add( 0 );

					Debug.Log("captured null...");
				}
				else
				{
					CapturedEdgeCenterPositions.Add( _Lnx_MeshManipulator.Edge_CurrentlyPointingAt.MidPosition );

					_Lnx_MeshManipulator.TryGrab();
					CapturedNumberOfSharedEdges.Add( _Lnx_MeshManipulator.Edges_currentlySelected.Count );
					GrabbedPositions_edge.Add( _Lnx_MeshManipulator.Edge_LastSelected.MidPosition );
					GrabbedManipulatorPos_edge.Add( _Lnx_MeshManipulator.manipulatorPos );
					CapturedNumberOfSharedVerts_edge.Add( _Lnx_MeshManipulator.Verts_currentlySelected.Count );

					Debug.Log($"{_Lnx_MeshManipulator.Edges_currentlySelected.Count}");

				}
			}
			else if( _Lnx_MeshManipulator.SelectMode == LNX_SelectMode.Faces )
			{
				TestPositions_face.Add( pos );
				TestDirections_face.Add( dir );

				Debug.Log(_Lnx_MeshManipulator.Index_TriPointingAt);

				if ( _Lnx_MeshManipulator.Index_TriPointingAt < 0 )
				{
					CapturedFaceCenterPositions.Add( Vector3.zero );

					GrabbedPositions_face.Add( Vector3.zero );
					GrabbedManipulatorPos_face.Add( Vector3.zero );
					CapturedNumberOfSharedVerts_face.Add( 0 );
					Debug.Log("captured null...");
				}
				else
				{
					Debug.Log($"index of pointing at tri: '{_Lnx_MeshManipulator.Index_TriPointingAt}'");

					CapturedFaceCenterPositions.Add( 
						_Lnx_MeshManipulator._LNX_NavMesh.Triangles[_Lnx_MeshManipulator.Index_TriPointingAt].V_center 
					);

					_Lnx_MeshManipulator.TryGrab();


					GrabbedPositions_face.Add( 
						_Lnx_MeshManipulator._LNX_NavMesh.Triangles[_Lnx_MeshManipulator.Index_TriLastSelected].V_center );

					GrabbedManipulatorPos_face.Add( _Lnx_MeshManipulator.manipulatorPos );
					CapturedNumberOfSharedVerts_face.Add( _Lnx_MeshManipulator.Verts_currentlySelected.Count );
				}
			}
		}


		public int tryIndex = 0;
		public LNX_SelectMode TrySelectMode;
		[ContextMenu("z call TryPoint()")]
		public void TryPoint()
		{
			_Lnx_MeshManipulator.SelectMode = TrySelectMode;

			if( TrySelectMode == LNX_SelectMode.Vertices )
			{
				_Lnx_MeshManipulator.TryPointAtComponentViaDirection(
					TestPositions_vert[tryIndex],
					TestDirections_vert[tryIndex]
				);

				Debug.Log($"{_Lnx_MeshManipulator.Vert_CurrentlyPointingAt} is null: '{_Lnx_MeshManipulator.Vert_CurrentlyPointingAt == null}'");

				if ( _Lnx_MeshManipulator.Vert_CurrentlyPointingAt != null )
				{
					if( CapturedVertPositions[tryIndex] != _Lnx_MeshManipulator.Vert_CurrentlyPointingAt.Position )
					{
						Debug.Log("not what I was expecting...");
					}
				}
			}
			else if ( TrySelectMode == LNX_SelectMode.Edges )
			{
				_Lnx_MeshManipulator.TryPointAtComponentViaDirection(
					TestPositions_edge[tryIndex],
					TestDirections_edge[tryIndex]
				);

				Debug.Log($"{_Lnx_MeshManipulator.Edge_CurrentlyPointingAt} is null: '{_Lnx_MeshManipulator.Edge_CurrentlyPointingAt == null}'");

				if ( _Lnx_MeshManipulator.Edge_CurrentlyPointingAt != null )
				{
					if ( CapturedEdgeCenterPositions[tryIndex] != _Lnx_MeshManipulator.Edge_CurrentlyPointingAt.MidPosition )
					{
						Debug.Log("not what I was expecting...");
					}
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
