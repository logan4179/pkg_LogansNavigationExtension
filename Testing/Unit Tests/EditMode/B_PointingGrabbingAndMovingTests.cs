using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LogansNavigationExtension;
using UnityEngine.AI;
using JetBrains.Annotations;
using System.IO;
using System.Configuration;

namespace LoganLand.LogansNavmeshExtension.Tests
{
    public class B_PointingGrabbingAndMovingTests
    {
        LNX_NavMesh _serializedLNXNavmesh;

		LNX_MeshManipulator _lnx_meshManipulator;

		[Header("TEST OBJECTS")]
		TDG_pointingAndGrabbing _tdg_pointingAndGrabbing;
		TDG_MoveComponents _tdg_MoveComponents;


		#region A - Setup --------------------------------------------------------------------------------
		[Test]
		public void a1_SetupObjects()
		{
			GameObject go = GameObject.Find( LNX_UnitTestUtilities.Name_SerializedNavmeshGameobject );

			_serializedLNXNavmesh = go.GetComponent<LNX_NavMesh>();
			Assert.NotNull(_serializedLNXNavmesh);


			_lnx_meshManipulator = go.GetComponent<LNX_MeshManipulator>();
			Assert.NotNull(_serializedLNXNavmesh);

		}

		[Test]
		public void a2_CreateTestObjectsFromJson()
		{
			Debug.Log($"{nameof(a2_CreateTestObjectsFromJson)}()...");

			#region pointing and grabbing -------------------------------------------------------
			if ( !File.Exists(TDG_Manager.filePath_testData_pointingAndGrabbing) )
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}

			//CREATE TEST OBJECT -----------------------------
			_tdg_pointingAndGrabbing = _serializedLNXNavmesh.gameObject.AddComponent<TDG_pointingAndGrabbing>();
			string jsonString = File.ReadAllText( TDG_Manager.filePath_testData_pointingAndGrabbing );
			JsonUtility.FromJsonOverwrite(jsonString, _tdg_pointingAndGrabbing);
			Assert.NotNull(_tdg_pointingAndGrabbing);
			#endregion

			#region mesh manipulation -------------------------------------------------------
			if ( !File.Exists(TDG_Manager.filePath_testData_moveComponents) )
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}

			//CREATE TEST OBJECT -----------------------------
			_tdg_MoveComponents = _serializedLNXNavmesh.gameObject.AddComponent<TDG_MoveComponents>();
			jsonString = File.ReadAllText( TDG_Manager.filePath_testData_moveComponents );
			JsonUtility.FromJsonOverwrite( jsonString, _tdg_MoveComponents );
			Assert.NotNull( _tdg_MoveComponents );

			if (_tdg_MoveComponents.TestMousePositions_vert == null || _tdg_MoveComponents.TestMousePositions_vert.Count == 0)
			{
				Debug.LogError($"couldn't do test because {nameof(_tdg_MoveComponents.TestMousePositions_vert)} was either null or 0 count. Returning early...");
				return;
			}
			else
			{
				Debug.Log($"{nameof(_tdg_MoveComponents.TestMousePositions_vert)} count is '{_tdg_MoveComponents.TestMousePositions_vert.Count}'. Proceeding...");
			}
			#endregion
		}
		#endregion

		#region B - Pointing, grabbing, and changing select mode for Verts ----------------------------------------------------
		[Test]
		public void b1_Change_LNXMeshManipulator_SelectMode_To_Vertices()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b1_Change_LNXMeshManipulator_SelectMode_To_Vertices)));

			#region make sure it starts completely cleared ----------------------------------
			Debug.Log("Making sure mesh manipulator starts out completely cleared...");
			_lnx_meshManipulator.ClearSelection();

			Assert.AreEqual(_lnx_meshManipulator.Verts_currentlySelected.Count, 0);
			Assert.IsNull(_lnx_meshManipulator.Vert_LastSelected);
			Assert.IsNull(_lnx_meshManipulator.Vert_CurrentlyPointingAt);

			Assert.AreEqual(_lnx_meshManipulator.Index_TriPointingAt, -1);
			Assert.AreEqual(_lnx_meshManipulator.Index_TriLastSelected, -1);
			Assert.AreEqual(_lnx_meshManipulator.indices_selectedTris.Count, 0);

			Assert.AreEqual(_lnx_meshManipulator.Edges_currentlySelected.Count, 0);
			Assert.IsNull(_lnx_meshManipulator.Edge_LastSelected);
			Assert.IsNull(_lnx_meshManipulator.Edge_CurrentlyPointingAt);

			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestSectionEndString, "clear at start"));
			#endregion

			Debug.Log("Changing selectmode to vertices...");
			_lnx_meshManipulator.ChangeSelectMode(LNX_SelectMode.Vertices);
			Assert.AreEqual(_lnx_meshManipulator.SelectMode, LNX_SelectMode.Vertices);
		}

		[Test]
		public void b2_Pointing_At_Vert_Results_In_Non_Null_Property_Value()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b2_Pointing_At_Vert_Results_In_Non_Null_Property_Value)));

			Debug.Log($"Selecting first grab vert...");
			_lnx_meshManipulator.TryPointAtComponentViaDirection(
				_tdg_MoveComponents.TestMousePositions_vert[0],
				_tdg_MoveComponents.TestMouseDirections_vert[0]
			);

			Assert.NotNull( _lnx_meshManipulator.Vert_CurrentlyPointingAt );
		}

		[Test]
		public void b3_MeshManipulator_Properties_Are_Correct_After_Grabbing_A_Vertex()
		{
			Debug.Log( string.Format( LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b3_MeshManipulator_Properties_Are_Correct_After_Grabbing_A_Vertex)) );

			_lnx_meshManipulator.TryGrab();

			Assert.Greater( _lnx_meshManipulator.Verts_currentlySelected.Count, 0 );
			Assert.NotNull( _lnx_meshManipulator.Vert_LastSelected );

			Assert.AreEqual(_lnx_meshManipulator.Index_TriPointingAt, -1);
			Assert.AreEqual(_lnx_meshManipulator.Index_TriLastSelected, -1);
			Assert.AreEqual(_lnx_meshManipulator.indices_selectedTris.Count, 0);

			Assert.AreEqual(_lnx_meshManipulator.Edges_currentlySelected.Count, 0);
			Assert.IsNull(_lnx_meshManipulator.Edge_LastSelected);
			Assert.IsNull(_lnx_meshManipulator.Edge_CurrentlyPointingAt);

			#region make sure it ends completely cleared ----------------------------------
			Debug.Log("Making sure mesh manipulator ends cleared out for next test...");

			_lnx_meshManipulator.ClearSelection();

			Assert.AreEqual(_lnx_meshManipulator.Verts_currentlySelected.Count, 0);
			Assert.IsNull(_lnx_meshManipulator.Vert_LastSelected);
			Assert.IsNull(_lnx_meshManipulator.Vert_CurrentlyPointingAt);

			Assert.AreEqual(_lnx_meshManipulator.Index_TriPointingAt, -1);
			Assert.AreEqual(_lnx_meshManipulator.Index_TriLastSelected, -1);
			Assert.AreEqual(_lnx_meshManipulator.indices_selectedTris.Count, 0);

			Assert.AreEqual(_lnx_meshManipulator.Edges_currentlySelected.Count, 0);
			Assert.IsNull(_lnx_meshManipulator.Edge_LastSelected);
			Assert.IsNull(_lnx_meshManipulator.Edge_CurrentlyPointingAt);
			#endregion

			Debug.Log($"End of test");
		}

		[Test]
		public void b4_Point_At_And_Grabbing_Verts_Results_In_Expected_Positional_Values()
		{
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b4_Point_At_And_Grabbing_Verts_Results_In_Expected_Positional_Values)) );

			//Debug.Log(_lnx_meshManipulator.SelectMode);
			_lnx_meshManipulator.ChangeSelectMode( LNX_SelectMode.Vertices );
			for (int i = 0; i < _tdg_pointingAndGrabbing.TestPositions_vert.Count; i++)
			{
				Debug.Log($"{i}...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestPositions_vert[i],
					_tdg_pointingAndGrabbing.TestDirections_vert[i]
				);

				if (_lnx_meshManipulator.Vert_CurrentlyPointingAt == null)
				{
					Assert.AreEqual(_tdg_pointingAndGrabbing.CapturedVertPositions[i], Vector3.zero);
				}
				else
				{
					//Debug.Log($"{_test_pointingAndGrabbing.CapturedVertPositions[i]} || {_lnx_meshManipulator.Vert_CurrentlyPointingAt.Position}");

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedVertPositions[i].x, _lnx_meshManipulator.Vert_CurrentlyPointingAt.Position.x);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedVertPositions[i].y, _lnx_meshManipulator.Vert_CurrentlyPointingAt.Position.y);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedVertPositions[i].z, _lnx_meshManipulator.Vert_CurrentlyPointingAt.Position.z);

					//Debug.Log("trying grab stuff...");
					_lnx_meshManipulator.TryGrab();

					//-------------------------------------------------------------------------------------------------------
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedPositions_vert[i].x, _lnx_meshManipulator.Vert_LastSelected.Position.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedPositions_vert[i].y, _lnx_meshManipulator.Vert_LastSelected.Position.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedPositions_vert[i].z, _lnx_meshManipulator.Vert_LastSelected.Position.z
					);


					//-------------------------------------------------------------------------------------------------------
					// Debug.Log($"trying count. expecting '{_test_pointingAndGrabbing.CapturedNumberOfSharedVerts[i]}'...");
					UnityEngine.Assertions.Assert.AreEqual(_tdg_pointingAndGrabbing.CapturedNumberOfSharedVerts[i], _lnx_meshManipulator.Verts_currentlySelected.Count);

					//-------------------------------------------------------------------------------------------------------
					//Debug.Log($"trying manipulator pos. expecting '{_test_pointingAndGrabbing.GrabbedManipulatorPos_vert[i]}'...");
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedManipulatorPos_vert[i].x, _lnx_meshManipulator.manipulatorPos.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedManipulatorPos_vert[i].y, _lnx_meshManipulator.manipulatorPos.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedManipulatorPos_vert[i].z, _lnx_meshManipulator.manipulatorPos.z
					);
				}
			}
		}

		[Test]
		public void b5_Point_At_And_Grabbing_Verts_Results_In_Expected_Corresponding_VisMesh_Vert_Position()
		{
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b5_Point_At_And_Grabbing_Verts_Results_In_Expected_Corresponding_VisMesh_Vert_Position)) );

			Debug.Log("This test will check to see that the pointed at verts are in the same position as their corresponding visualization mesh vertices...");

			_lnx_meshManipulator.ChangeSelectMode(LNX_SelectMode.Vertices);
			for (int i = 0; i < _tdg_pointingAndGrabbing.TestPositions_vert.Count; i++)
			{
				Debug.Log($"{i}...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestPositions_vert[i],
					_tdg_pointingAndGrabbing.TestDirections_vert[i]
				);

				Debug.Log($"Tried to point, now checking...");

				if (_lnx_meshManipulator.Vert_CurrentlyPointingAt == null)
				{
					Assert.AreEqual(_tdg_pointingAndGrabbing.CapturedVertPositions[i], Vector3.zero);
				}
				else
				{
					//Debug.Log($"{_test_pointingAndGrabbing.CapturedVertPositions[i]} || {_lnx_meshManipulator.Vert_CurrentlyPointingAt.Position}");

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedVertPositions[i].x, 
						_lnx_meshManipulator._LNX_NavMesh._Mesh.vertices[_lnx_meshManipulator.Vert_CurrentlyPointingAt.Index_VisMesh_Vertices].x );

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedVertPositions[i].y,
						_lnx_meshManipulator._LNX_NavMesh._Mesh.vertices[_lnx_meshManipulator.Vert_CurrentlyPointingAt.Index_VisMesh_Vertices].y);

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedVertPositions[i].z,
						_lnx_meshManipulator._LNX_NavMesh._Mesh.vertices[_lnx_meshManipulator.Vert_CurrentlyPointingAt.Index_VisMesh_Vertices].z);
					//Debug.Log("trying grab stuff...");
					_lnx_meshManipulator.TryGrab();

				}
			}
		}
		#endregion

		#region C - Pointing, grabbing, and changing select mode for Edges ----------------------------------------------------
		[Test]
		public void c1_Change_LNXMeshManipulator_SelectMode_To_Edges()
		{
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(c1_Change_LNXMeshManipulator_SelectMode_To_Edges)) );

			#region make sure it starts completely cleared ----------------------------------
			Debug.Log("Making sure mesh manipulator starts out cleared...");
			_lnx_meshManipulator.ClearSelection();

			Assert.AreEqual(_lnx_meshManipulator.Verts_currentlySelected.Count, 0);
			Assert.IsNull(_lnx_meshManipulator.Vert_LastSelected);
			Assert.IsNull(_lnx_meshManipulator.Vert_CurrentlyPointingAt);

			Assert.AreEqual(_lnx_meshManipulator.Index_TriPointingAt, -1);
			Assert.AreEqual(_lnx_meshManipulator.Index_TriLastSelected, -1);
			Assert.AreEqual(_lnx_meshManipulator.indices_selectedTris.Count, 0);

			Assert.AreEqual(_lnx_meshManipulator.Edges_currentlySelected.Count, 0);
			Assert.IsNull(_lnx_meshManipulator.Edge_LastSelected);
			Assert.IsNull(_lnx_meshManipulator.Edge_CurrentlyPointingAt);

			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestSectionEndString, "clear at start"));
			#endregion

			Debug.Log("Changing selectmode to edges...");
			_lnx_meshManipulator.ChangeSelectMode(LNX_SelectMode.Edges);
			Assert.AreEqual(_lnx_meshManipulator.SelectMode, LNX_SelectMode.Edges);
		}

		[Test]
		public void c2_Pointing_At_Edge_Results_In_Non_Null_Property_Value()
		{
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(c2_Pointing_At_Edge_Results_In_Non_Null_Property_Value)) );

			Debug.Log($"Selecting first grab edge...");
			_lnx_meshManipulator.TryPointAtComponentViaDirection(
				_tdg_MoveComponents.TestMousePositions_edge[0],
				_tdg_MoveComponents.TestMouseDirections_edge[0]
			);
			Assert.NotNull( _lnx_meshManipulator.Edge_CurrentlyPointingAt );
		}

		[Test]
		public void c3_MeshManipulator_Properties_Are_Correct_After_Grabbing_An_Edge()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(c3_MeshManipulator_Properties_Are_Correct_After_Grabbing_An_Edge)));

			_lnx_meshManipulator.TryGrab();

			Assert.Greater(_lnx_meshManipulator.Edges_currentlySelected.Count, 0);
			Assert.NotNull(_lnx_meshManipulator.Edge_LastSelected);

			Assert.Greater(_lnx_meshManipulator.Verts_currentlySelected.Count, 0); //even though the select mode is edges, this should still be greater than 0...
			Assert.IsNull(_lnx_meshManipulator.Vert_LastSelected);
			Assert.IsNull(_lnx_meshManipulator.Vert_CurrentlyPointingAt);

			Assert.AreEqual(_lnx_meshManipulator.Index_TriPointingAt, -1);
			Assert.AreEqual(_lnx_meshManipulator.Index_TriLastSelected, -1);
			Assert.AreEqual(_lnx_meshManipulator.indices_selectedTris.Count, 0);

			#region make sure it ends completely cleared ----------------------------------
			Debug.Log("Making sure mesh manipulator ends cleared...");
			_lnx_meshManipulator.ClearSelection();

			Assert.AreEqual(_lnx_meshManipulator.Verts_currentlySelected.Count, 0);
			Assert.IsNull(_lnx_meshManipulator.Vert_LastSelected);
			Assert.IsNull(_lnx_meshManipulator.Vert_CurrentlyPointingAt);

			Assert.AreEqual(_lnx_meshManipulator.Index_TriPointingAt, -1);
			Assert.AreEqual(_lnx_meshManipulator.Index_TriLastSelected, -1);
			Assert.AreEqual(_lnx_meshManipulator.indices_selectedTris.Count, 0);

			Assert.AreEqual(_lnx_meshManipulator.Edges_currentlySelected.Count, 0);
			Assert.IsNull(_lnx_meshManipulator.Edge_LastSelected);
			Assert.IsNull(_lnx_meshManipulator.Edge_CurrentlyPointingAt);
			#endregion

			Debug.Log("End of test");
		}

		[Test]
		public void c4_Point_At_And_Grabbing_Edges_Results_In_Expected_Positional_Values()
		{
			Debug.Log($"{nameof(c4_Point_At_And_Grabbing_Edges_Results_In_Expected_Positional_Values)}--------------------------------");

			Debug.Log($"iterating through '{_tdg_pointingAndGrabbing.TestPositions_edge.Count}' test positions " +
				$"and '{_tdg_pointingAndGrabbing.CapturedEdgeCenterPositions.Count}' captured positions...");
			_lnx_meshManipulator.SelectMode = LNX_SelectMode.Edges;
			for (int i = 0; i < _tdg_pointingAndGrabbing.TestPositions_edge.Count; i++)
			{
				Debug.Log($"{i}...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestPositions_edge[i],
					_tdg_pointingAndGrabbing.TestDirections_edge[i]
				);

				Debug.Log($"flag after pointing: '{_lnx_meshManipulator.Flag_AComponentIsCurrentlyHighlighted}'");

				if (_lnx_meshManipulator.Edge_CurrentlyPointingAt == null)
				{
					Debug.Log($"{nameof(_lnx_meshManipulator.Edge_CurrentlyPointingAt)} was null...");
					Assert.AreEqual(_tdg_pointingAndGrabbing.CapturedEdgeCenterPositions[i], Vector3.zero);
				}
				else
				{
					Debug.Log($"{nameof(_lnx_meshManipulator.Edge_CurrentlyPointingAt)} was NOT null...");

					//Debug.Log($"{_test_pointingAndGrabbing.CapturedEdgeCenterPositions[i]} || {_lnx_meshManipulator.Edge_CurrentlyPointingAt.Position}");

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedEdgeCenterPositions[i].x, _lnx_meshManipulator.Edge_CurrentlyPointingAt.MidPosition.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedEdgeCenterPositions[i].y, _lnx_meshManipulator.Edge_CurrentlyPointingAt.MidPosition.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedEdgeCenterPositions[i].z, _lnx_meshManipulator.Edge_CurrentlyPointingAt.MidPosition.z
					);

					//-------------------------------------------------------------------------------------------------------
					Debug.Log("now trying grab...");
					_lnx_meshManipulator.TryGrab();

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedPositions_edge[i].x, _lnx_meshManipulator.Edge_LastSelected.MidPosition.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedPositions_edge[i].y, _lnx_meshManipulator.Edge_LastSelected.MidPosition.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedPositions_edge[i].z, _lnx_meshManipulator.Edge_LastSelected.MidPosition.z
					);

					//-------------------------------------------------------------------------------------------------------
					// Debug.Log($"trying count. expecting '{_test_pointingAndGrabbing.CapturedNumberOfSharedVerts[i]}'...");
					UnityEngine.Assertions.Assert.AreEqual(_tdg_pointingAndGrabbing.CapturedNumberOfSharedVerts_edge[i], _lnx_meshManipulator.Verts_currentlySelected.Count);

					//-------------------------------------------------------------------------------------------------------
					//Debug.Log($"trying manipulator pos. expecting '{_test_pointingAndGrabbing.GrabbedManipulatorPos_edge[i]}'...");
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedManipulatorPos_edge[i].x, _lnx_meshManipulator.manipulatorPos.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedManipulatorPos_edge[i].y, _lnx_meshManipulator.manipulatorPos.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedManipulatorPos_edge[i].z, _lnx_meshManipulator.manipulatorPos.z
					);
				}
			}
		}
		#endregion

		#region D - Pointing, grabbing, and changing select mode for Edges ----------------------------------------------------
		[Test]
		public void d1_Change_LNXMeshManipulator_SelectMode_To_Faces()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(d1_Change_LNXMeshManipulator_SelectMode_To_Faces)));

			#region make sure it starts completely cleared ----------------------------------
			Debug.Log("Making sure mesh manipulator starts out cleared...");
			_lnx_meshManipulator.ClearSelection();

			Assert.AreEqual(_lnx_meshManipulator.Verts_currentlySelected.Count, 0);
			Assert.IsNull(_lnx_meshManipulator.Vert_LastSelected);
			Assert.IsNull(_lnx_meshManipulator.Vert_CurrentlyPointingAt);

			Assert.AreEqual(_lnx_meshManipulator.Index_TriPointingAt, -1);
			Assert.AreEqual(_lnx_meshManipulator.Index_TriLastSelected, -1);
			Assert.AreEqual(_lnx_meshManipulator.indices_selectedTris.Count, 0);

			Assert.AreEqual(_lnx_meshManipulator.Edges_currentlySelected.Count, 0);
			Assert.IsNull(_lnx_meshManipulator.Edge_LastSelected);
			Assert.IsNull(_lnx_meshManipulator.Edge_CurrentlyPointingAt);

			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestSectionEndString, "clear at start"));
			#endregion

			Debug.Log("Changing selectmode to Faces...");
			_lnx_meshManipulator.ChangeSelectMode(LNX_SelectMode.Faces);
			Assert.AreEqual(_lnx_meshManipulator.SelectMode, LNX_SelectMode.Faces);
		}

		[Test]
		public void d2_Pointing_At_Face_Results_In_Non_Null_Property_Value()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(d2_Pointing_At_Face_Results_In_Non_Null_Property_Value)));

			Debug.Log($"pointing at first grab face...");
			_lnx_meshManipulator.TryPointAtComponentViaDirection(
				_tdg_pointingAndGrabbing.TestPositions_face[1],
				_tdg_pointingAndGrabbing.TestDirections_face[1]
			);

			Assert.Greater(_lnx_meshManipulator.Index_TriPointingAt, -1);
		}

		[Test]
		public void d3_MeshManipulator_Properties_Are_Correct_After_Grabbing_A_Face()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(d3_MeshManipulator_Properties_Are_Correct_After_Grabbing_A_Face)));

			Debug.Log($"now grabbing first grab face...");
			_lnx_meshManipulator.TryGrab();

			Assert.Greater( _lnx_meshManipulator.Verts_currentlySelected.Count, 0 ); //even though the select mode is faces, this should still be greater than 0...
			Assert.IsNull( _lnx_meshManipulator.Vert_LastSelected );
			Assert.IsNull( _lnx_meshManipulator.Vert_CurrentlyPointingAt );

			Assert.Greater( _lnx_meshManipulator.Index_TriLastSelected, -1 );
			Assert.AreEqual( _lnx_meshManipulator.indices_selectedTris.Count, 1 );

			Assert.AreEqual(_lnx_meshManipulator.Edges_currentlySelected.Count, 0);
			Assert.IsNull(_lnx_meshManipulator.Edge_LastSelected);
			Assert.IsNull(_lnx_meshManipulator.Edge_CurrentlyPointingAt);

			#region make sure it ends completely cleared ----------------------------------
			Debug.Log("Making sure mesh manipulator ends cleared...");
			_lnx_meshManipulator.ClearSelection();

			Assert.AreEqual(_lnx_meshManipulator.Verts_currentlySelected.Count, 0);
			Assert.IsNull(_lnx_meshManipulator.Vert_LastSelected);
			Assert.IsNull(_lnx_meshManipulator.Vert_CurrentlyPointingAt);

			Assert.AreEqual(_lnx_meshManipulator.Index_TriPointingAt, -1);
			Assert.AreEqual(_lnx_meshManipulator.Index_TriLastSelected, -1);
			Assert.AreEqual(_lnx_meshManipulator.indices_selectedTris.Count, 0);

			Assert.AreEqual(_lnx_meshManipulator.Edges_currentlySelected.Count, 0);
			Assert.IsNull(_lnx_meshManipulator.Edge_LastSelected);
			Assert.IsNull(_lnx_meshManipulator.Edge_CurrentlyPointingAt);
			#endregion
		}

		[Test]
		public void d4_Point_At_And_Grabbing_Faces_Results_In_Expected_Positional_Values()
		{
			Debug.Log($"{nameof(d4_Point_At_And_Grabbing_Faces_Results_In_Expected_Positional_Values)}--------------------------------");

			_lnx_meshManipulator.SelectMode = LNX_SelectMode.Faces;
			Debug.Log($"running '{_tdg_pointingAndGrabbing.TestPositions_face.Count}' test positions...");
			for (int i = 0; i < _tdg_pointingAndGrabbing.TestPositions_face.Count; i++)
			{
				Debug.Log($"{i}...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestPositions_face[i],
					_tdg_pointingAndGrabbing.TestDirections_face[i]
				);

				if (_lnx_meshManipulator.Index_TriPointingAt < 0)
				{
					Assert.AreEqual(_tdg_pointingAndGrabbing.CapturedFaceCenterPositions[i], Vector3.zero);
				}
				else
				{
					//Debug.Log($"{_test_pointingAndGrabbing.CapturedEdgeCenterPositions[i]} || {_lnx_meshManipulator.Edge_CurrentlyPointingAt.Position}");

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedFaceCenterPositions[i].x, _lnx_meshManipulator.PointingAtTri.V_center.x);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedFaceCenterPositions[i].y, _lnx_meshManipulator.PointingAtTri.V_center.y);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedFaceCenterPositions[i].z, _lnx_meshManipulator.PointingAtTri.V_center.z);

					//Debug.Log("trying grab stuff...");
					_lnx_meshManipulator.TryGrab();

					//-------------------------------------------------------------------------------------------------------
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedPositions_face[i].x, _lnx_meshManipulator.LastSelectedTri.V_center.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedPositions_face[i].y, _lnx_meshManipulator.LastSelectedTri.V_center.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedPositions_face[i].z, _lnx_meshManipulator.LastSelectedTri.V_center.z
					);


					//-------------------------------------------------------------------------------------------------------
					// Debug.Log($"trying count. expecting '{_test_pointingAndGrabbing.CapturedNumberOfSharedVerts[i]}'...");
					UnityEngine.Assertions.Assert.AreEqual(_tdg_pointingAndGrabbing.CapturedNumberOfSharedVerts_face[i], _lnx_meshManipulator.Verts_currentlySelected.Count);

					//-------------------------------------------------------------------------------------------------------
					//Debug.Log($"trying manipulator pos. expecting '{_test_pointingAndGrabbing.GrabbedManipulatorPos_edge[i]}'...");
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedManipulatorPos_face[i].x, _lnx_meshManipulator.manipulatorPos.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedManipulatorPos_face[i].y, _lnx_meshManipulator.manipulatorPos.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.GrabbedManipulatorPos_face[i].z, _lnx_meshManipulator.manipulatorPos.z
					);
				}
			}
		}
		#endregion


		#region E - Moving Components -----------------------------------------------------------------------
		[Test]
		public void e1_MovingVerts()
		{
			Debug.Log($"{nameof(e1_MovingVerts)}--------------------------------");

			Debug.Log($"now selecting and moving {_tdg_MoveComponents.TestMousePositions_vert.Count} verts...");
			//Debug.Log(_lnx_meshManipulator.SelectMode);
			_lnx_meshManipulator.SelectMode = LNX_SelectMode.Vertices;
			for ( int i = 0; i < _tdg_MoveComponents.TestMousePositions_vert.Count; i++ )
			{
				Debug.Log($"{i}...........................................................................");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_MoveComponents.TestMousePositions_vert[i],
					_tdg_MoveComponents.TestMouseDirections_vert[i]
				);

				if ( _lnx_meshManipulator.Vert_CurrentlyPointingAt == null )
				{
					Debug.LogError($"tried pointing at vert, but got null. None of these entries are supposed to be null. Returning early...");
					return;
				}
				else
				{
					Debug.Log($"succesfully pointed at vert '{_lnx_meshManipulator.Vert_CurrentlyPointingAt.MyCoordinate}', at " +
						$"vert pos: '{_lnx_meshManipulator.Vert_CurrentlyPointingAt.Position}'...");

					//Debug.Log("trying grab stuff...");
					_lnx_meshManipulator.TryGrab();

					Debug.Log($"Grab operation executed. mesh manipulator vert last selected coordinate: " +
						$"'{_lnx_meshManipulator.Vert_LastSelected.MyCoordinate}'. current pos: '{_lnx_meshManipulator.Vert_LastSelected.Position}'. " +
						$"expected position: '{_tdg_MoveComponents.GrabbedPositions_vert[i]}'");

					Vector3 vOffset = new Vector3( 1.5f, 1.5f, 1.5f );
					Vector3 v_moveTo = _tdg_MoveComponents.GrabbedPositions_vert[i] + vOffset;

					Debug.Log($"now moving to '{v_moveTo}'...");

					_lnx_meshManipulator.MoveSelectedVerts( v_moveTo );

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						v_moveTo.x, _lnx_meshManipulator.Vert_LastSelected.Position.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						v_moveTo.y, _lnx_meshManipulator.Vert_LastSelected.Position.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						v_moveTo.z, _lnx_meshManipulator.Vert_LastSelected.Position.z
					);

					Debug.Log($"pos now: '{_lnx_meshManipulator.Vert_LastSelected.Position}'");

					#region move back-------------------------------------------------
					Debug.Log($"now moving '{_lnx_meshManipulator.Vert_LastSelected.Position}' back to '{_tdg_MoveComponents.GrabbedPositions_vert[i]}'...");

					_lnx_meshManipulator.MoveSelectedVerts( _tdg_MoveComponents.GrabbedPositions_vert[i] );

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_MoveComponents.GrabbedPositions_vert[i].x, _lnx_meshManipulator.Vert_LastSelected.Position.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_MoveComponents.GrabbedPositions_vert[i].y, _lnx_meshManipulator.Vert_LastSelected.Position.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_MoveComponents.GrabbedPositions_vert[i].z, _lnx_meshManipulator.Vert_LastSelected.Position.z
					);

					Debug.Log($"pos now: '{_lnx_meshManipulator.Vert_LastSelected.Position}'");
					#endregion
				}
			}
		}

		[Test]
		public void e2_Checking_VisMesh_Vert_Positioning_After_Moving_LnxVertex()
		{
			Debug.Log($"{nameof(e2_Checking_VisMesh_Vert_Positioning_After_Moving_LnxVertex)}--------------------------------");

			Debug.Log($"now selecting and moving {_tdg_MoveComponents.TestMousePositions_vert.Count} verts...");
			//Debug.Log(_lnx_meshManipulator.SelectMode);
			_lnx_meshManipulator.SelectMode = LNX_SelectMode.Vertices;
			for (int i = 0; i < _tdg_MoveComponents.TestMousePositions_vert.Count; i++)
			{
				Debug.Log($"{i}...........................................................................");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_MoveComponents.TestMousePositions_vert[i],
					_tdg_MoveComponents.TestMouseDirections_vert[i]
				);

				if (_lnx_meshManipulator.Vert_CurrentlyPointingAt == null)
				{
					Debug.LogError($"tried pointing at vert, but got null. None of these entries are supposed to be null. Returning early...");
					return;
				}
				else
				{
					Debug.Log($"succesfully pointed at vert '{_lnx_meshManipulator.Vert_CurrentlyPointingAt.MyCoordinate}', at " +
						$"vert pos: '{_lnx_meshManipulator.Vert_CurrentlyPointingAt.Position}'...");

					//Debug.Log("trying grab stuff...");
					_lnx_meshManipulator.TryGrab();

					Debug.Log($"Grab operation executed. mesh manipulator vert last selected coordinate: " +
						$"'{_lnx_meshManipulator.Vert_LastSelected.MyCoordinate}'. current pos: '{_lnx_meshManipulator.Vert_LastSelected.Position}'. " +
						$"expected position: '{_tdg_MoveComponents.GrabbedPositions_vert[i]}'");

					Vector3 vOffset = new Vector3(1.5f, 1.5f, 1.5f);
					Vector3 v_moveTo = _tdg_MoveComponents.GrabbedPositions_vert[i] + vOffset;

					Debug.Log($"now moving to '{v_moveTo}'...");

					_lnx_meshManipulator.MoveSelectedVerts(v_moveTo);

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						v_moveTo.x, _lnx_meshManipulator._LNX_NavMesh._Mesh.vertices[_lnx_meshManipulator.Vert_LastSelected.Index_VisMesh_Vertices].x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						v_moveTo.y, _lnx_meshManipulator._LNX_NavMesh._Mesh.vertices[_lnx_meshManipulator.Vert_LastSelected.Index_VisMesh_Vertices].y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						v_moveTo.z, _lnx_meshManipulator._LNX_NavMesh._Mesh.vertices[_lnx_meshManipulator.Vert_LastSelected.Index_VisMesh_Vertices].z
					);

					Debug.Log($"pos now: '{_lnx_meshManipulator.Vert_LastSelected.Position}'");

					#region move back-------------------------------------------------
					Debug.Log($"now moving '{_lnx_meshManipulator.Vert_LastSelected.Position}' back to '{_tdg_MoveComponents.GrabbedPositions_vert[i]}'...");

					_lnx_meshManipulator.MoveSelectedVerts(_tdg_MoveComponents.GrabbedPositions_vert[i]);

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_MoveComponents.GrabbedPositions_vert[i].x, _lnx_meshManipulator._LNX_NavMesh._Mesh.vertices[_lnx_meshManipulator.Vert_LastSelected.Index_VisMesh_Vertices].x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_MoveComponents.GrabbedPositions_vert[i].y, _lnx_meshManipulator._LNX_NavMesh._Mesh.vertices[_lnx_meshManipulator.Vert_LastSelected.Index_VisMesh_Vertices].y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_MoveComponents.GrabbedPositions_vert[i].z, _lnx_meshManipulator._LNX_NavMesh._Mesh.vertices[_lnx_meshManipulator.Vert_LastSelected.Index_VisMesh_Vertices].z
					);

					Debug.Log($"pos now: '{_lnx_meshManipulator.Vert_LastSelected.Position}'");
					#endregion
				}
			}
		}

			[Test]
		public void e3_MovingEdges()
		{
			Debug.Log($"{nameof(e3_MovingEdges)}--------------------------------");
			Debug.Log($"Creating test object from Json...");

			if (_tdg_MoveComponents.TestMousePositions_edge == null || _tdg_MoveComponents.TestMousePositions_edge.Count == 0)
			{
				Debug.LogError($"couldn't do test because {nameof(_tdg_MoveComponents.TestMousePositions_edge)} was either null or 0 count. Returning early...");
				return;
			}
			else
			{
				Debug.Log($"{nameof(_tdg_MoveComponents.TestMousePositions_edge)} count is '{_tdg_MoveComponents.TestMousePositions_edge.Count}'. Proceeding...");
			}
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestSectionEndString, "setup"));

			_lnx_meshManipulator.SelectMode = LNX_SelectMode.Edges;
			for ( int i = 0; i < _tdg_MoveComponents.TestMousePositions_edge.Count; i++ )
			{
				Debug.Log($"{i}...........................................................................");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_MoveComponents.TestMousePositions_edge[i],
					_tdg_MoveComponents.TestMouseDirections_edge[i]
				);

				if ( _lnx_meshManipulator.Edge_CurrentlyPointingAt == null )
				{
					Debug.LogError($"tried pointing at edge, but got null. Returning early...");
					return;
				}
				else
				{
					Debug.Log($"succesfully pointed at edge. Attempting grab..."); //the flag being false is the culprit!...

					_lnx_meshManipulator.TryGrab();

					if( _lnx_meshManipulator.Edge_LastSelected == null )
					{
						Debug.LogError($"after grab attempt, apparently edgelastselected is null. flag is '{_lnx_meshManipulator.Flag_AComponentIsCurrentlyHighlighted}'. Returning early...");
						return;
					}

					Debug.Log($"Grab operation executed. mesh manipulator edge last selected coordinate: " +
						$"'{_lnx_meshManipulator.Edge_LastSelected.MyCoordinate}'. midpos: '{_lnx_meshManipulator.Edge_LastSelected.MidPosition}', " +
						$"logged midpos: '{_tdg_MoveComponents.GrabbedMidPositions_edge[i]}'");

					Vector3 vOffset = new Vector3(1.5f, 1.5f, 1.5f);
					Vector3 v_moveTo = _tdg_MoveComponents.GrabbedMidPositions_edge[i] + vOffset;

					Debug.Log($"now moving to '{v_moveTo}'...");

					_lnx_meshManipulator.MoveSelectedVerts( v_moveTo );
					_lnx_meshManipulator._LNX_NavMesh.RefreshAfterMove(); //important so that the midpos will get re-calculated.

					Debug.Log($"after move and refresh, midpos: '{_lnx_meshManipulator.Edge_LastSelected.MidPosition}'.");

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						v_moveTo.x, _lnx_meshManipulator.Edge_LastSelected.MidPosition.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						v_moveTo.y, _lnx_meshManipulator.Edge_LastSelected.MidPosition.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						v_moveTo.z, _lnx_meshManipulator.Edge_LastSelected.MidPosition.z
					);

					#region move back-------------------------------------------------
					Debug.Log($"now moving '{_lnx_meshManipulator.Edge_LastSelected.MidPosition}' back to '{_tdg_MoveComponents.GrabbedMidPositions_edge[i]}'...");

					_lnx_meshManipulator.MoveSelectedVerts( _tdg_MoveComponents.GrabbedMidPositions_edge[i] );
					_lnx_meshManipulator._LNX_NavMesh.RefreshAfterMove(); //important so that the midpos will get re-calculated.

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_MoveComponents.GrabbedMidPositions_edge[i].x, _lnx_meshManipulator.Edge_LastSelected.MidPosition.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_MoveComponents.GrabbedMidPositions_edge[i].y, _lnx_meshManipulator.Edge_LastSelected.MidPosition.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_MoveComponents.GrabbedMidPositions_edge[i].z, _lnx_meshManipulator.Edge_LastSelected.MidPosition.z
					);

					Debug.Log($"pos now: '{_lnx_meshManipulator.Edge_LastSelected.MidPosition}'");
					#endregion
				}
			}
		}
		#endregion

	}
}

