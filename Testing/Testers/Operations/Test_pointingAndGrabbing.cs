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
    public class Test_pointingAndGrabbing : MonoBehaviour
    {
		[SerializeField] string fileName;
		public LNX_MeshManipulator _Lnx_MeshManipulator;

		public Ray _Ray;

		public List<Vector3> ReconstructPositions;
		public List<Vector3> ReconstructDirections;
		public List<LNX_SelectMode> ReconstructModes;
		[Space(15f)]

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

		[ContextMenu("z call ReconstructCollections()")]
		public void ReconstructCollections()
		{
			Debug.Log($"ReconstructCollections() reconstructing from '{ReconstructPositions.Count}' reconstruct perspectives...");
			ClearCollections();

			try
			{
				System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
				sw.Start();

				for ( int i = 0; i < ReconstructPositions.Count; i++ )
				{
					Debug.Log($"{i}...");

					_Lnx_MeshManipulator.SelectMode = ReconstructModes[i];
					_Lnx_MeshManipulator.TryPointAtComponentViaDirection( ReconstructPositions[i], ReconstructDirections[i] );

					CaptureMouseInfo( ReconstructPositions[i], ReconstructDirections[i], true );

					if( sw.Elapsed.TotalMinutes > 1 )
					{
						Debug.LogError($"decided took too long at '{i}'. Breaking...");
						break;
					}
				}
			}
			catch (System.Exception e)
			{

				throw;
			}

		}

		public void CaptureMouseInfo( Vector3 pos, Vector3 dir, bool fromReconstruct = false )
        {
			Debug.Log($"capturing pos '{pos}', and dir: '{dir}'. mode: '{_Lnx_MeshManipulator.SelectMode}'...");

			/*
			if( CapturedPositions == null || CapturedDirections == null || CapturedModes == null )
			{
				Debug.Log($"a collection was null, reinitializing lists...");

				CapturedModes = new List<int>();
				CapturedPositions = new List<Vector3>();
				CapturedDirections = new List<Vector3>();
			}
			*/

			if( !fromReconstruct )
			{
				ReconstructPositions.Add( pos );
				ReconstructDirections.Add( dir );
				ReconstructModes.Add( _Lnx_MeshManipulator.SelectMode );
			}


			if( _Lnx_MeshManipulator.SelectMode == LNX_SelectMode.None )
			{
				Debug.LogError($"Error! change mesh manipulator select mode to something other than 'none'. Returning early...");
				return;
			}

			if( _Lnx_MeshManipulator.SelectMode == LNX_SelectMode.Vertices )
			{
				TestPositions_vert.Add( pos );
				TestDirections_vert.Add( dir );

				if( _Lnx_MeshManipulator.Vert_CurrentlyPointingAt == null )
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

		[ContextMenu("z call WiteMeToJson()")]
		public void WiteMeToJson()
		{
			string filePath = $"{Directory.GetCurrentDirectory()}\\Packages\\LogansNavigationExtension\\Testing\\Unit Tests\\Test Data";

			if ( !Directory.Exists(filePath) )
			{
				Debug.LogWarning($"directory: '{filePath}' wasn't found.");
				return;
			}

			filePath = Path.Combine(filePath, $"{fileName}.json");

			if ( File.Exists(filePath) )
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

			string myJsonString = File.ReadAllText(filePath);

			JsonUtility.FromJsonOverwrite(myJsonString, this);

			EditorUtility.SetDirty(this);
		}
	}
}
