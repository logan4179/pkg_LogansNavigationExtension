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
		}

		[Test]
		public void A3_Ensure_Test_Objects_Are_Valid()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(A3_Ensure_Test_Objects_Are_Valid),
				"Ensures that the objects created for testing have adequate/valid values"
			);

			Assert.Greater( _tdg_pointingAndGrabbing.TestMousePositions_vert.Count, 0 );
			Assert.Greater( _tdg_pointingAndGrabbing.TestMouseDirections_vert.Count, 0 );
			Assert.Greater(_tdg_pointingAndGrabbing.CapturedPointedAtVertPositions.Count, 0);
			Assert.Greater(_tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert.Count, 0);
			Assert.Greater(_tdg_pointingAndGrabbing.CapturedGrabbedManipulatorPos_vert.Count, 0);

			Assert.Greater(_tdg_pointingAndGrabbing.TestMousePositions_edge.Count, 0);
			Assert.Greater(_tdg_pointingAndGrabbing.TestMouseDirections_edge.Count, 0);
			Assert.Greater(_tdg_pointingAndGrabbing.CapturedPointedAtEdgeMidPositions.Count, 0);
			Assert.Greater(_tdg_pointingAndGrabbing.CapturedGrabbedPositions_edge.Count, 0);
			Assert.Greater(_tdg_pointingAndGrabbing.CapturedGrabbedManipulatorPos_edge.Count, 0);

			Assert.Greater(_tdg_pointingAndGrabbing.TestMousePositions_face.Count, 0);
			Assert.Greater(_tdg_pointingAndGrabbing.TestMouseDirections_face.Count, 0);
			Assert.Greater(_tdg_pointingAndGrabbing.CapturedPointedAtFaceCenterPositions.Count, 0);
			Assert.Greater(_tdg_pointingAndGrabbing.CapturedGrabbedPositions_face.Count, 0);
			Assert.Greater(_tdg_pointingAndGrabbing.CapturedGrabbedManipulatorPos_face.Count, 0);
		}
		#endregion

		#region B - Pointing, grabbing, and changing select mode for Verts ----------------------------------------------------
		[Test]
		public void b1_Change_LNXMeshManipulator_SelectMode_To_Vertices()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(b1_Change_LNXMeshManipulator_SelectMode_To_Vertices),
				$"Makes sure the {nameof(LNX_MeshManipulator)}'s {nameof(_lnx_meshManipulator.ChangeSelectMode)}() method works " +
				$"as intended for vertices."
			);

			Debug.Log("Changing selectmode to Vertices...");
			_lnx_meshManipulator.ChangeSelectMode( LNX_SelectMode.Vertices );
			Assert.AreEqual( _lnx_meshManipulator.SelectMode, LNX_SelectMode.Vertices );

			#region make sure it ends correctly cleared ----------------------------------
			Debug.Log("Making sure mesh manipulator starts out completely cleared...");
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
		public void b2_MeshManipulator_Nullness_Is_Correct_After_Pointing_At_Vertices()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(b2_MeshManipulator_Nullness_Is_Correct_After_Pointing_At_Vertices),
				$"This test points at various captured positions/directions, then checks that \nthe mesh " +
				$"manipulator's {nameof(_lnx_meshManipulator.Vert_CurrentlyPointingAt)} value is either null or not-null, " +
				$"as appropriate."
			);


			for ( int i = 0; i < _tdg_pointingAndGrabbing.TestMousePositions_vert.Count; i++ )
			{
				Debug.Log($"{i}...");
				_lnx_meshManipulator.ClearSelection();

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestMousePositions_vert[i],
					_tdg_pointingAndGrabbing.TestMouseDirections_vert[i]
				);

				if (_tdg_pointingAndGrabbing.CapturedPointedAtVertPositions[i] == Vector3.zero) 
				{
					Debug.Log($"the tdg says this one SHOULD be null...");
					Assert.IsNull(_lnx_meshManipulator.Vert_CurrentlyPointingAt);
				}
				else
				{
					Debug.Log($"the tdg says this one should NOT be null...");
					Assert.NotNull(_lnx_meshManipulator.Vert_CurrentlyPointingAt);
				}
			}
		}

		[Test]
		public void b3_Properties_Are_Correct_After_Pointing_At_Vertices()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(b3_Properties_Are_Correct_After_Pointing_At_Vertices),
				$"This test checks that the {nameof(LNX_MeshManipulator)}.{nameof(LNX_MeshManipulator.Vert_CurrentlyPointingAt)} " +
				$"\nproperty's position is correct after attempting to point at an array of positions/directions"
			);

			_lnx_meshManipulator.ChangeSelectMode(LNX_SelectMode.Vertices);
			for ( int i = 0; i < _tdg_pointingAndGrabbing.TestMousePositions_vert.Count; i++ )
			{
				Debug.Log($"{i}...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestMousePositions_vert[i],
					_tdg_pointingAndGrabbing.TestMouseDirections_vert[i]
				);

				if ( _tdg_pointingAndGrabbing.CapturedPointedAtVertPositions[i] == Vector3.zero )
				{
					Debug.Log("The TDG says this one should be null. This is irrelevant for the test. Skipping...");
				}
				else
				{
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedPointedAtVertPositions[i].x, _lnx_meshManipulator.Vert_CurrentlyPointingAt.Position.x);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedPointedAtVertPositions[i].y, _lnx_meshManipulator.Vert_CurrentlyPointingAt.Position.y);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedPointedAtVertPositions[i].z, _lnx_meshManipulator.Vert_CurrentlyPointingAt.Position.z);
				}
			}
		}

		[Test]
		public void b4_MeshManipulator_Properties_Correct_After_Grabbing_For_Vertices()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(b4_MeshManipulator_Properties_Correct_After_Grabbing_For_Vertices),
				"This test makes sure that only vertex-based properties are showing active after \n" +
				"selecting verts, and face and edge properties are signaling inactive values. Then \n" +
				$"it checks that everything is signaling inactive when the {nameof(_lnx_meshManipulator.ClearSelection)} \n" +
				$"method is invoked."
			);

			_lnx_meshManipulator.ChangeSelectMode(LNX_SelectMode.Vertices);

			for ( int i = 0; i < _tdg_pointingAndGrabbing.TestMousePositions_vert.Count; i++ )
			{
				Debug.Log($"{i}...");
				//_lnx_meshManipulator.ClearSelection(); //I'm deciding NOT to enable this because clearing from the grab method is part of what I want to test...

				if (_tdg_pointingAndGrabbing.CapturedPointedAtVertPositions[i] == Vector3.zero)
				{
					Debug.Log("this one should be null. bypassing...");
				}
				else
				{
					Debug.Log("this one should NOT be null. Pointing...");

					_lnx_meshManipulator.TryPointAtComponentViaDirection(
						_tdg_pointingAndGrabbing.TestMousePositions_vert[i],
						_tdg_pointingAndGrabbing.TestMouseDirections_vert[i]
					);

					if( _lnx_meshManipulator.Vert_CurrentlyPointingAt == null )
					{
						Debug.LogError($"Something went wrong. Point attempt failed when it should have worked. Prior tests may need to " +
							$"be checked for validity. Returning early...");
						return;
					}

					Debug.Log("now grabing...");
					_lnx_meshManipulator.TryGrab();

					//-------------------------------------------------------------------------------------------------------
					Assert.NotNull(_lnx_meshManipulator.Vert_LastSelected);

					Assert.Greater(_lnx_meshManipulator.Verts_currentlySelected.Count, 0);

					UnityEngine.Assertions.Assert.AreEqual(
						_lnx_meshManipulator.Vert_LastSelected.SharedVertexCoordinates.Length + 1, 
						_lnx_meshManipulator.Verts_currentlySelected.Count 
					);

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i].x, _lnx_meshManipulator.Vert_LastSelected.Position.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i].y, _lnx_meshManipulator.Vert_LastSelected.Position.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i].z, _lnx_meshManipulator.Vert_LastSelected.Position.z
					);

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedManipulatorPos_vert[i].x, _lnx_meshManipulator.manipulatorPos.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedManipulatorPos_vert[i].y, _lnx_meshManipulator.manipulatorPos.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedManipulatorPos_vert[i].z, _lnx_meshManipulator.manipulatorPos.z
					);

					//-------------------------------------------------------------------------------------------------------
					Debug.Log("Making sure none of the triangle or edge properties are indicating a selection...");
					Assert.AreEqual(_lnx_meshManipulator.Index_TriPointingAt, -1);
					Assert.AreEqual(_lnx_meshManipulator.Index_TriLastSelected, -1);
					Assert.AreEqual(_lnx_meshManipulator.indices_selectedTris.Count, 0);

					Assert.AreEqual(_lnx_meshManipulator.Edges_currentlySelected.Count, 0);
					Assert.IsNull(_lnx_meshManipulator.Edge_LastSelected);
					Assert.IsNull(_lnx_meshManipulator.Edge_CurrentlyPointingAt);
				}
			}

			Debug.Log($"End of test");
		}

		[Test]
		public void b5_MeshManipulator_Properties_Correct_After_Clearing_For_Vertices()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(b5_MeshManipulator_Properties_Correct_After_Clearing_For_Vertices),
				"This test makes sure that only vertex-based properties are showing inactive after \n" +
				$"the {nameof(_lnx_meshManipulator.ClearSelection)} method is invoked."
			);

			_lnx_meshManipulator.ChangeSelectMode( LNX_SelectMode.Vertices );			

			for ( int i = 0; i < _tdg_pointingAndGrabbing.TestMousePositions_vert.Count; i++ )
			{
				Debug.Log($"{i}...");

				if (_tdg_pointingAndGrabbing.CapturedPointedAtVertPositions[i] == Vector3.zero)
				{
					Debug.Log("this one should be null. bypassing...");
					continue;
				}

				Debug.Log("this one should NOT be null. Pointing...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestMousePositions_vert[i],
					_tdg_pointingAndGrabbing.TestMouseDirections_vert[i]
				);

				if (_lnx_meshManipulator.Vert_CurrentlyPointingAt == null)
				{
					Debug.LogError($"Something went wrong. Point attempt failed when it should have worked. Prior tests may need to " +
						$"be checked for validity. Returning early...");
					return;
				}

				Debug.Log("now grabing...");
				_lnx_meshManipulator.TryGrab();

				if (_lnx_meshManipulator.Vert_LastSelected == null)
				{
					Debug.LogError($"Something went wrong. Grab attempt failed when it should have worked so test can't continue.\n" +
						$"Prior tests may need to be checked for validity. Returning early...");
					return;
				}

				//-------------------------------------------------------------------------------------------------------
				#region make sure it ends completely cleared ----------------------------------
				Debug.Log("Clearing...");
				_lnx_meshManipulator.ClearSelection();
				Debug.Log("Manipulator clear method called, now asserting mesh manipulator properties indicate clear...");

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

			Debug.Log($"End of test");
		}

		[Test]
		public void B6_Point_At_And_Grabbing_Verts_Results_In_Expected_Corresponding_VisMesh_Vert_Position()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(B6_Point_At_And_Grabbing_Verts_Results_In_Expected_Corresponding_VisMesh_Vert_Position),
				"This test will check to see that the pointed-at verts are in the same position as their corresponding " +
				"visualization mesh vertices..."
			);

			_lnx_meshManipulator.ChangeSelectMode(LNX_SelectMode.Vertices);
			for (int i = 0; i < _tdg_pointingAndGrabbing.TestMousePositions_vert.Count; i++)
			{
				Debug.Log($"{i}...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestMousePositions_vert[i],
					_tdg_pointingAndGrabbing.TestMouseDirections_vert[i]
				);

				Debug.Log($"Tried to point, now checking...");

				if (_lnx_meshManipulator.Vert_CurrentlyPointingAt == null)
				{
					Assert.AreEqual(_tdg_pointingAndGrabbing.CapturedPointedAtVertPositions[i], Vector3.zero);
				}
				else
				{
					//Debug.Log($"{_test_pointingAndGrabbing.CapturedVertPositions[i]} || {_lnx_meshManipulator.Vert_CurrentlyPointingAt.Position}");

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedPointedAtVertPositions[i].x, 
						_lnx_meshManipulator._LNX_NavMesh._Mesh.vertices[_lnx_meshManipulator.Vert_CurrentlyPointingAt.Index_VisMesh_Vertices].x );

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedPointedAtVertPositions[i].y,
						_lnx_meshManipulator._LNX_NavMesh._Mesh.vertices[_lnx_meshManipulator.Vert_CurrentlyPointingAt.Index_VisMesh_Vertices].y);

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedPointedAtVertPositions[i].z,
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
			LNX_UnitTestUtilities.LogTestStart(nameof(c1_Change_LNXMeshManipulator_SelectMode_To_Edges),
				$"Makes sure the {nameof(LNX_MeshManipulator)}'s {nameof(_lnx_meshManipulator.ChangeSelectMode)}() method works " +
				$"as intended for edges."
			);

			Debug.Log("Changing selectmode to Edges...");
			_lnx_meshManipulator.ChangeSelectMode( LNX_SelectMode.Edges );
			Assert.AreEqual( _lnx_meshManipulator.SelectMode, LNX_SelectMode.Edges );

			#region make sure it ends correctly cleared ----------------------------------
			Debug.Log("Making sure mesh manipulator starts out completely cleared...");
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
		public void c2_MeshManipulator_Nullness_Is_Correct_After_Pointing_At_Edges()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(c2_MeshManipulator_Nullness_Is_Correct_After_Pointing_At_Edges),
				$"This test points at various captured positions/directions, then checks that \nthe mesh " +
				$"manipulator's {nameof(_lnx_meshManipulator.Vert_CurrentlyPointingAt)} value is either null or not-null, " +
				$"as appropriate."
			);

			for ( int i = 0; i < _tdg_pointingAndGrabbing.TestMousePositions_edge.Count; i++ )
			{
				Debug.Log($"{i}...");
				_lnx_meshManipulator.ClearSelection();

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestMousePositions_edge[i],
					_tdg_pointingAndGrabbing.TestMouseDirections_edge[i]
				);

				if ( _tdg_pointingAndGrabbing.CapturedPointedAtEdgeMidPositions[i] != Vector3.zero ) 
				{
					Debug.Log($"the tdg says this one should NOT be null. Trying point...");
					Assert.NotNull( _lnx_meshManipulator.Edge_CurrentlyPointingAt );
				}
				else
				{
					Debug.Log($"the tdg says this one SHOULD be null...");
					Assert.IsNull( _lnx_meshManipulator.Edge_CurrentlyPointingAt );
				}
			}
		}

		[Test]
		public void c3_Properties_Are_Correct_After_Pointing_At_Edges()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(b3_Properties_Are_Correct_After_Pointing_At_Vertices),
				$"This test checks that the {nameof(LNX_MeshManipulator)}'s Edge properties are correct after attempting \n" +
				$"to point at an array of positions/directions"
			);

			_lnx_meshManipulator.ChangeSelectMode( LNX_SelectMode.Edges );
			for ( int i = 0; i < _tdg_pointingAndGrabbing.TestMousePositions_edge.Count; i++ )
			{
				Debug.Log($"{i}...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestMousePositions_edge[i],
					_tdg_pointingAndGrabbing.TestMouseDirections_edge[i]
				);

				if (_tdg_pointingAndGrabbing.CapturedPointedAtEdgeMidPositions[i] == Vector3.zero )
				{
					Debug.Log("The TDG says this one should be null. This is irrelevant for the test. Skipping...");
				}
				else
				{
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedPointedAtEdgeMidPositions[i].x, 
						_lnx_meshManipulator.Edge_CurrentlyPointingAt.MidPosition.x);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedPointedAtEdgeMidPositions[i].y, 
						_lnx_meshManipulator.Edge_CurrentlyPointingAt.MidPosition.y);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedPointedAtEdgeMidPositions[i].z, 
						_lnx_meshManipulator.Edge_CurrentlyPointingAt.MidPosition.z);
				}
			}
		}

		[Test]
		public void C4_MeshManipulator_Properties_Correct_After_Grabbing_For_Edges()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(C4_MeshManipulator_Properties_Correct_After_Grabbing_For_Edges),
				"This test makes sure that only edge-related properties are showing active after \n" +
				"selecting edges, and face and Vertex properties are signaling inactive values. Then \n" +
				$"it checks that everything is signaling inactive when the {nameof(_lnx_meshManipulator.ClearSelection)} \n" +
				$"method is invoked."
			);

			_lnx_meshManipulator.ChangeSelectMode (LNX_SelectMode.Edges );

			for ( int i = 0; i < _tdg_pointingAndGrabbing.TestMousePositions_edge.Count; i++ )
			{
				Debug.Log($"{i}...");
				//_lnx_meshManipulator.ClearSelection(); //I'm deciding NOT to enable this because clearing from the grab method is part of what I want to test...

				if (_tdg_pointingAndGrabbing.CapturedPointedAtEdgeMidPositions[i] == Vector3.zero)
				{
					Debug.Log("this one should be null. bypassing...");
				}
				else
				{
					Debug.Log("this one should NOT be null. Pointing...");

					_lnx_meshManipulator.TryPointAtComponentViaDirection(
						_tdg_pointingAndGrabbing.TestMousePositions_edge[i],
						_tdg_pointingAndGrabbing.TestMouseDirections_edge[i]
					);

					if ( _lnx_meshManipulator.Edge_CurrentlyPointingAt == null )
					{
						Debug.LogError($"Something went wrong. Point attempt failed when it should have worked. Prior tests may need to " +
							$"be checked for validity. Returning early...");
						return;
					}

					Debug.Log("now grabing...");
					_lnx_meshManipulator.TryGrab();

					//-------------------------------------------------------------------------------------------------------
					Assert.NotNull(_lnx_meshManipulator.Edge_LastSelected);

					Assert.Greater(_lnx_meshManipulator.Verts_currentlySelected.Count, 0);

					int numberOfSelectedVerts = 2 +
						_lnx_meshManipulator._LNX_NavMesh.GetVertexAtCoordinate(_lnx_meshManipulator.Edge_LastSelected.StartVertCoordinate).SharedVertexCoordinates.Length +
						_lnx_meshManipulator._LNX_NavMesh.GetVertexAtCoordinate(_lnx_meshManipulator.Edge_LastSelected.EndVertCoordinate).SharedVertexCoordinates.Length;

					Debug.Log($"calculated '{numberOfSelectedVerts}' vertices that should be selected right now. Asserting this is the case...");

					UnityEngine.Assertions.Assert.AreEqual( numberOfSelectedVerts, _lnx_meshManipulator.Verts_currentlySelected.Count );

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_edge[i].x, _lnx_meshManipulator.Edge_LastSelected.MidPosition.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_edge[i].y, _lnx_meshManipulator.Edge_LastSelected.MidPosition.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_edge[i].z, _lnx_meshManipulator.Edge_LastSelected.MidPosition.z
					);

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_edge[i].x, _lnx_meshManipulator.manipulatorPos.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_edge[i].y, _lnx_meshManipulator.manipulatorPos.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_edge[i].z, _lnx_meshManipulator.manipulatorPos.z
					);

					//-------------------------------------------------------------------------------------------------------
					Debug.Log("Making sure none of the triangle or vert properties are indicating a selection...");
					Assert.AreEqual(_lnx_meshManipulator.Index_TriPointingAt, -1);
					Assert.AreEqual(_lnx_meshManipulator.Index_TriLastSelected, -1);
					Assert.AreEqual(_lnx_meshManipulator.indices_selectedTris.Count, 0);

					Assert.IsNull(_lnx_meshManipulator.Vert_LastSelected);
					Assert.IsNull(_lnx_meshManipulator.Vert_CurrentlyPointingAt);
				}
			}

			Debug.Log($"End of test");
		}

		[Test]
		public void C5_MeshManipulator_Properties_Correct_After_Clearing_For_Edges()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(C5_MeshManipulator_Properties_Correct_After_Clearing_For_Edges),
				"This test makes sure that only edge-based properties are showing inactive after \n" +
				$"the {nameof(_lnx_meshManipulator.ClearSelection)} method is invoked."
			);

			_lnx_meshManipulator.ChangeSelectMode( LNX_SelectMode.Edges );

			for ( int i = 0; i < _tdg_pointingAndGrabbing.TestMousePositions_edge.Count; i++ )
			{
				Debug.Log($"{i}...");

				if ( _tdg_pointingAndGrabbing.CapturedPointedAtEdgeMidPositions[i] == Vector3.zero )
				{
					Debug.Log("this one should be null. bypassing...");
					continue;
				}

				Debug.Log("this one should NOT be null. Pointing...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestMousePositions_edge[i],
					_tdg_pointingAndGrabbing.TestMouseDirections_edge[i]
				);

				if ( _lnx_meshManipulator.Edge_CurrentlyPointingAt == null )
				{
					Debug.LogError($"Something went wrong. Point attempt failed when it should have worked. Prior tests may need to " +
						$"be checked for validity. Returning early...");
					return;
				}

				Debug.Log("now grabing...");
				_lnx_meshManipulator.TryGrab();

				if ( _lnx_meshManipulator.Edge_LastSelected == null )
				{
					Debug.LogError($"Something went wrong. Grab attempt failed when it should have worked so test can't continue.\n" +
						$"Prior tests may need to be checked for validity. Returning early...");
					return;
				}

				//-------------------------------------------------------------------------------------------------------
				#region make sure it ends completely cleared ----------------------------------
				Debug.Log("Clearing...");
				_lnx_meshManipulator.ClearSelection();
				Debug.Log("Manipulator clear method called, now asserting mesh manipulator properties indicate clear...");

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

			Debug.Log($"End of test");
		}

		/* //todo: implement
		[Test]
		public void C6_Point_At_And_Grabbing_Edges_Results_In_Expected_Corresponding_VisMesh_Vert_Position()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(B6_Point_At_And_Grabbing_Verts_Results_In_Expected_Corresponding_VisMesh_Vert_Position),
				"This test will check to see that the pointed-at verts are in the same position as their corresponding " +
				"visualization mesh vertices..."
			);

			_lnx_meshManipulator.ChangeSelectMode(LNX_SelectMode.Vertices);
			for (int i = 0; i < _tdg_pointingAndGrabbing.TestMousePositions_vert.Count; i++)
			{
				Debug.Log($"{i}...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestMousePositions_vert[i],
					_tdg_pointingAndGrabbing.TestMouseDirections_vert[i]
				);

				Debug.Log($"Tried to point, now checking...");

				if (_lnx_meshManipulator.Vert_CurrentlyPointingAt == null)
				{
					Assert.AreEqual(_tdg_pointingAndGrabbing.CapturedPointedAtVertPositions[i], Vector3.zero);
				}
				else
				{
					//Debug.Log($"{_test_pointingAndGrabbing.CapturedVertPositions[i]} || {_lnx_meshManipulator.Vert_CurrentlyPointingAt.Position}");

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedPointedAtVertPositions[i].x,
						_lnx_meshManipulator._LNX_NavMesh._Mesh.vertices[_lnx_meshManipulator.Vert_CurrentlyPointingAt.Index_VisMesh_Vertices].x);

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedPointedAtVertPositions[i].y,
						_lnx_meshManipulator._LNX_NavMesh._Mesh.vertices[_lnx_meshManipulator.Vert_CurrentlyPointingAt.Index_VisMesh_Vertices].y);

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedPointedAtVertPositions[i].z,
						_lnx_meshManipulator._LNX_NavMesh._Mesh.vertices[_lnx_meshManipulator.Vert_CurrentlyPointingAt.Index_VisMesh_Vertices].z);
					//Debug.Log("trying grab stuff...");
					_lnx_meshManipulator.TryGrab();

				}
			}
		}*/
		#endregion

		#region D - Pointing, grabbing, and changing select mode for Faces ----------------------------------------------------
		[Test]
		public void D1_Change_LNXMeshManipulator_SelectMode_To_Faces()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(D1_Change_LNXMeshManipulator_SelectMode_To_Faces),
				$"Makes sure the {nameof(LNX_MeshManipulator)}'s {nameof(_lnx_meshManipulator.ChangeSelectMode)}() method works " +
				$"as intended for faces."
			);

			Debug.Log("Changing selectmode to Faces...");
			_lnx_meshManipulator.ChangeSelectMode( LNX_SelectMode.Faces );
			Assert.AreEqual( _lnx_meshManipulator.SelectMode, LNX_SelectMode.Faces );

			#region make sure it ends correctly cleared ----------------------------------
			Debug.Log("Making sure mesh manipulator starts out completely cleared...");
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
		public void D2_MeshManipulator_Nullness_Is_Correct_After_Pointing_At_Faces()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(D2_MeshManipulator_Nullness_Is_Correct_After_Pointing_At_Faces),
				$"This test points at various captured positions/directions, then checks that \nthe mesh " +
				$"manipulator's '{nameof(_lnx_meshManipulator.Edge_CurrentlyPointingAt)}' value is either null or not-null, " +
				$"as appropriate."
			);

			for ( int i = 0; i < _tdg_pointingAndGrabbing.TestMousePositions_face.Count; i++ )
			{
				Debug.Log($"{i}...");
				_lnx_meshManipulator.ClearSelection();

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestMousePositions_face[i],
					_tdg_pointingAndGrabbing.TestMouseDirections_face[i]
				);

				if ( _tdg_pointingAndGrabbing.CapturedPointedAtFaceCenterPositions[i] != Vector3.zero )
				{
					Debug.Log($"the tdg says this one should NOT be null. Trying point...");
					Assert.Greater(_lnx_meshManipulator.Index_TriPointingAt, -1);
					Assert.NotNull( _lnx_meshManipulator.PointingAtTri );
				}
				else
				{
					Debug.Log($"the tdg says this one SHOULD be null...");
					Assert.AreEqual(_lnx_meshManipulator.Index_TriPointingAt, -1);
					Assert.IsNull(_lnx_meshManipulator.PointingAtTri);
				}
			}
		}

		[Test]
		public void D3_Properties_Are_Correct_After_Pointing_At_Faces()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(D3_Properties_Are_Correct_After_Pointing_At_Faces),
				$"This test checks that the {nameof(LNX_MeshManipulator)}'s Face-related properties are correct after attempting \n" +
				$"to point at an array of positions/directions"
			);

			_lnx_meshManipulator.ChangeSelectMode( LNX_SelectMode.Faces );
			for (int i = 0; i < _tdg_pointingAndGrabbing.TestMousePositions_face.Count; i++)
			{
				Debug.Log($"{i}...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestMousePositions_face[i],
					_tdg_pointingAndGrabbing.TestMouseDirections_face[i]
				);

				if ( _tdg_pointingAndGrabbing.CapturedPointedAtFaceCenterPositions[i] == Vector3.zero )
				{
					Debug.Log("The TDG says this one should be null. This is irrelevant for the test. Skipping...");
				}
				else
				{
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedPointedAtFaceCenterPositions[i].x,
						_lnx_meshManipulator.PointingAtTri.V_center.x);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedPointedAtFaceCenterPositions[i].y,
						_lnx_meshManipulator.PointingAtTri.V_center.y);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_pointingAndGrabbing.CapturedPointedAtFaceCenterPositions[i].z,
						_lnx_meshManipulator.PointingAtTri.V_center.z);
				}
			}
		}

		[Test]
		public void D4_MeshManipulator_Properties_Correct_After_Grabbing_For_Faces()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(D4_MeshManipulator_Properties_Correct_After_Grabbing_For_Faces),
				"This test makes sure that only face-related properties are showing active after \n" +
				"selecting faces, and face and Vertex properties are signaling inactive values. Then \n" +
				$"it checks that everything is signaling inactive when the {nameof(_lnx_meshManipulator.ClearSelection)} \n" +
				$"method is invoked."
			);

			_lnx_meshManipulator.ChangeSelectMode( LNX_SelectMode.Faces );

			for ( int i = 0; i < _tdg_pointingAndGrabbing.TestMousePositions_face.Count; i++ )
			{
				Debug.Log($"{i}...");
				//_lnx_meshManipulator.ClearSelection(); //I'm deciding NOT to enable this because clearing from the grab method is part of what I want to test...

				if ( _tdg_pointingAndGrabbing.CapturedPointedAtFaceCenterPositions[i] == Vector3.zero )
				{
					Debug.Log("this one should be null. bypassing...");
				}
				else
				{
					Debug.Log("this one should NOT be null. Pointing...");

					_lnx_meshManipulator.TryPointAtComponentViaDirection(
						_tdg_pointingAndGrabbing.TestMousePositions_face[i],
						_tdg_pointingAndGrabbing.TestMouseDirections_face[i]
					);

					if ( _lnx_meshManipulator.PointingAtTri == null )
					{
						Debug.LogError($"Something went wrong. Point attempt failed when it should have worked. Prior tests may need to " +
							$"be checked for validity. Returning early...");
						return;
					}

					Debug.Log("now grabing...");
					_lnx_meshManipulator.TryGrab();

					//-------------------------------------------------------------------------------------------------------
					Assert.Greater( _lnx_meshManipulator.Index_TriLastSelected, -1 );
					Assert.NotNull( _lnx_meshManipulator.LastSelectedTri );

					Assert.Greater( _lnx_meshManipulator.Verts_currentlySelected.Count, 0 );

					#region CALCULATE TOTAL AMOUNT OF SELECTED VERTS ------------------------------
					int numberOfSelectedVerts = 3 +
						_lnx_meshManipulator._LNX_NavMesh.GetVertexAtCoordinate(_lnx_meshManipulator.Index_TriLastSelected, 0).SharedVertexCoordinates.Length +
						_lnx_meshManipulator._LNX_NavMesh.GetVertexAtCoordinate(_lnx_meshManipulator.Index_TriLastSelected, 1).SharedVertexCoordinates.Length +
						_lnx_meshManipulator._LNX_NavMesh.GetVertexAtCoordinate(_lnx_meshManipulator.Index_TriLastSelected, 2).SharedVertexCoordinates.Length;

					Debug.Log($"calculated '{numberOfSelectedVerts}' vertices that should be selected right now. Asserting this is the case...");

					UnityEngine.Assertions.Assert.AreEqual(numberOfSelectedVerts, _lnx_meshManipulator.Verts_currentlySelected.Count);
					#endregion

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_face[i].x, _lnx_meshManipulator.LastSelectedTri.V_center.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_face[i].y, _lnx_meshManipulator.LastSelectedTri.V_center.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_face[i].z, _lnx_meshManipulator.LastSelectedTri.V_center.z
					);

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_face[i].x, _lnx_meshManipulator.manipulatorPos.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_face[i].y, _lnx_meshManipulator.manipulatorPos.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_face[i].z, _lnx_meshManipulator.manipulatorPos.z
					);

					//-------------------------------------------------------------------------------------------------------
					Debug.Log("Making sure none of the edge or vert properties are indicating a selection...");
					Assert.AreEqual(_lnx_meshManipulator.Edges_currentlySelected.Count, 0);
					Assert.IsNull(_lnx_meshManipulator.Edge_LastSelected);
					Assert.IsNull(_lnx_meshManipulator.Edge_CurrentlyPointingAt);

					Assert.IsNull(_lnx_meshManipulator.Vert_LastSelected);
					Assert.IsNull(_lnx_meshManipulator.Vert_CurrentlyPointingAt);
				}
			}

			Debug.Log($"End of test");
		}

		[Test]
		public void D5_MeshManipulator_Properties_Correct_After_Clearing_For_Faces()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(D5_MeshManipulator_Properties_Correct_After_Clearing_For_Faces),
				"This test makes sure that only face-based properties are showing inactive after \n" +
				$"the {nameof(_lnx_meshManipulator.ClearSelection)} method is invoked."
			);

			_lnx_meshManipulator.ChangeSelectMode( LNX_SelectMode.Faces );

			for ( int i = 0; i < _tdg_pointingAndGrabbing.TestMousePositions_face.Count; i++ )
			{
				Debug.Log($"{i}...");

				if ( _tdg_pointingAndGrabbing.CapturedPointedAtFaceCenterPositions[i] == Vector3.zero )
				{
					Debug.Log("this one should be null. bypassing...");
					continue;
				}

				Debug.Log("this one should NOT be null. Pointing...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestMousePositions_face[i],
					_tdg_pointingAndGrabbing.TestMouseDirections_face[i]
				);

				if (_lnx_meshManipulator.PointingAtTri == null)
				{
					Debug.LogError($"Something went wrong. Point attempt failed when it should have worked. Prior tests may need to " +
						$"be checked for validity. Returning early...");
					return;
				}

				Debug.Log("now grabing...");
				_lnx_meshManipulator.TryGrab();

				if (_lnx_meshManipulator.LastSelectedTri == null)
				{
					Debug.LogError($"Something went wrong. Grab attempt failed when it should have worked so test can't continue.\n" +
						$"Prior tests may need to be checked for validity. Returning early...");
					return;
				}

				//-------------------------------------------------------------------------------------------------------
				#region make sure it ends completely cleared ----------------------------------
				Debug.Log("Clearing...");
				_lnx_meshManipulator.ClearSelection();
				Debug.Log("Manipulator clear method called, now asserting mesh manipulator properties indicate clear...");

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

			Debug.Log($"End of test");
		}
		#endregion


		#region E - Moving Components -----------------------------------------------------------------------
		[Test]
		public void e1_MovingVerts()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(e1_MovingVerts),
				"This test makes sure that..."
			);

			Debug.Log($"now selecting and moving {_tdg_pointingAndGrabbing.TestMousePositions_vert.Count} verts...");
			//Debug.Log(_lnx_meshManipulator.SelectMode);
			_lnx_meshManipulator.SelectMode = LNX_SelectMode.Vertices;
			for ( int i = 0; i < _tdg_pointingAndGrabbing.TestMousePositions_vert.Count; i++ )
			{
				Debug.Log($"{i}...........................................................................");

				Debug.Log($"at start of iteration, how many verts currently selected: '{_lnx_meshManipulator.Verts_currentlySelected.Count}'");

				if( _tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i] == Vector3.zero )
				{
					Debug.Log($"This entry doesn't select a vert. Bypassing...");
					continue;
				}

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestMousePositions_vert[i],
					_tdg_pointingAndGrabbing.TestMouseDirections_vert[i]
				);

				Debug.Log($"pointed at vert '{_lnx_meshManipulator.Vert_CurrentlyPointingAt.MyCoordinate}', at " +
					$"vert pos: '{_lnx_meshManipulator.Vert_CurrentlyPointingAt.Position}'...");

				//Debug.Log("trying grab stuff...");
				_lnx_meshManipulator.TryGrab();

				Debug.Log($"Grab operation executed. mesh manipulator vert last selected coordinate: " +
					$"'{_lnx_meshManipulator.Vert_LastSelected.MyCoordinate}'. current pos: '{_lnx_meshManipulator.Vert_LastSelected.Position}'. " +
					$"expected position: '{_tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i]}'.");

				Vector3 vOffset = new Vector3( 1.5f, 1.5f, 1.5f );
				Vector3 v_moveTo = _tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i] + vOffset;

				Debug.Log($"last selected (before): '{_lnx_meshManipulator.Vert_LastSelected.MyCoordinate}', pos now: '{_lnx_meshManipulator.Vert_LastSelected.Position}'");

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

				Debug.Log($"last selected: '{_lnx_meshManipulator.Vert_LastSelected.MyCoordinate}', pos now: '{_lnx_meshManipulator.Vert_LastSelected.Position}'");

				#region move back-------------------------------------------------
				Debug.Log($"now moving '{_lnx_meshManipulator.Vert_LastSelected.Position}' back to '{_tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i]}'...");

				_lnx_meshManipulator.MoveSelectedVerts(_tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i] );

				Debug.Log($"last selected (after): '{_lnx_meshManipulator.Vert_LastSelected.MyCoordinate}', pos now: '{_lnx_meshManipulator.Vert_LastSelected.Position}'");


				UnityEngine.Assertions.Assert.AreApproximatelyEqual(
					_tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i].x, _lnx_meshManipulator.Vert_LastSelected.Position.x
				);
				UnityEngine.Assertions.Assert.AreApproximatelyEqual(
					_tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i].y, _lnx_meshManipulator.Vert_LastSelected.Position.y
				);
				UnityEngine.Assertions.Assert.AreApproximatelyEqual(
					_tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i].z, _lnx_meshManipulator.Vert_LastSelected.Position.z
				);

				Debug.Log($"pos now: '{_lnx_meshManipulator.Vert_LastSelected.Position}'");
				#endregion
				
			}
		}

		[Test]
		public void e2_Checking_VisMesh_Vert_Positioning_After_Moving_LnxVertex()
		{
			Debug.Log($"{nameof(e2_Checking_VisMesh_Vert_Positioning_After_Moving_LnxVertex)}--------------------------------");

			Debug.Log($"now selecting and moving {_tdg_pointingAndGrabbing.TestMousePositions_vert.Count} verts...");
			//Debug.Log(_lnx_meshManipulator.SelectMode);
			_lnx_meshManipulator.SelectMode = LNX_SelectMode.Vertices;
			for (int i = 0; i < _tdg_pointingAndGrabbing.TestMousePositions_vert.Count; i++)
			{
				Debug.Log($"{i}...........................................................................");

				if ( _tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i] == Vector3.zero )
				{
					Debug.Log($"This entry doesn't select a vert. Bypassing...");
					continue;
				}

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestMousePositions_vert[i],
					_tdg_pointingAndGrabbing.TestMouseDirections_vert[i]
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
						$"expected position: '{_tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i]}'");

					Vector3 vOffset = new Vector3(1.5f, 1.5f, 1.5f);
					Vector3 v_moveTo = _tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i] + vOffset;

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
					Debug.Log($"now moving '{_lnx_meshManipulator.Vert_LastSelected.Position}' back to '{_tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i]}'...");

					_lnx_meshManipulator.MoveSelectedVerts(_tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i]);

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i].x, _lnx_meshManipulator._LNX_NavMesh._Mesh.vertices[_lnx_meshManipulator.Vert_LastSelected.Index_VisMesh_Vertices].x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i].y, _lnx_meshManipulator._LNX_NavMesh._Mesh.vertices[_lnx_meshManipulator.Vert_LastSelected.Index_VisMesh_Vertices].y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_tdg_pointingAndGrabbing.CapturedGrabbedPositions_vert[i].z, _lnx_meshManipulator._LNX_NavMesh._Mesh.vertices[_lnx_meshManipulator.Vert_LastSelected.Index_VisMesh_Vertices].z
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

			if (_tdg_pointingAndGrabbing.TestMousePositions_edge == null || _tdg_pointingAndGrabbing.TestMousePositions_edge.Count == 0)
			{
				Debug.LogError($"couldn't do test because {nameof(_tdg_pointingAndGrabbing.TestMousePositions_edge)} was either null or 0 count. Returning early...");
				return;
			}
			else
			{
				Debug.Log($"{nameof(_tdg_pointingAndGrabbing.TestMousePositions_edge)} count is '{_tdg_pointingAndGrabbing.TestMousePositions_edge.Count}'. Proceeding...");
			}
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestSectionEndString, "setup"));

			_lnx_meshManipulator.SelectMode = LNX_SelectMode.Edges;
			for ( int i = 0; i < _tdg_pointingAndGrabbing.TestMousePositions_edge.Count; i++ )
			{
				Debug.Log($"{i}...........................................................................");
				if ( _tdg_pointingAndGrabbing.CapturedGrabbedPositions_edge[i] == Vector3.zero )
				{
					Debug.Log($"This entry doesn't select a vert. Bypassing...");
					continue;
				}

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_tdg_pointingAndGrabbing.TestMousePositions_edge[i],
					_tdg_pointingAndGrabbing.TestMouseDirections_edge[i]
				);

				Debug.Log($"succesfully pointed at edge. Attempting grab..."); //the flag being false is the culprit!...

				_lnx_meshManipulator.TryGrab();

				if( _lnx_meshManipulator.Edge_LastSelected == null )
				{
					Debug.LogError($"after grab attempt, apparently edgelastselected is null. flag is '{_lnx_meshManipulator.Flag_AComponentIsCurrentlyHighlighted}'. Returning early...");
					return;
				}

				Debug.Log($"Grab operation executed. mesh manipulator edge last selected coordinate: " +
					$"'{_lnx_meshManipulator.Edge_LastSelected.MyCoordinate}'. midpos: '{_lnx_meshManipulator.Edge_LastSelected.MidPosition}', " +
					$"logged midpos: '{_tdg_pointingAndGrabbing.CapturedPointedAtEdgeMidPositions[i]}'");

				Vector3 vOffset = new Vector3(1.5f, 1.5f, 1.5f);
				Vector3 v_moveTo = _tdg_pointingAndGrabbing.CapturedPointedAtEdgeMidPositions[i] + vOffset;

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
				Debug.Log($"now moving '{_lnx_meshManipulator.Edge_LastSelected.MidPosition}' back to '{_tdg_pointingAndGrabbing.CapturedPointedAtEdgeMidPositions[i]}'...");

				_lnx_meshManipulator.MoveSelectedVerts(_tdg_pointingAndGrabbing.CapturedPointedAtEdgeMidPositions[i] );
				_lnx_meshManipulator._LNX_NavMesh.RefreshAfterMove(); //important so that the midpos will get re-calculated.

				UnityEngine.Assertions.Assert.AreApproximatelyEqual(
					_tdg_pointingAndGrabbing.CapturedPointedAtEdgeMidPositions[i].x, _lnx_meshManipulator.Edge_LastSelected.MidPosition.x
				);
				UnityEngine.Assertions.Assert.AreApproximatelyEqual(
					_tdg_pointingAndGrabbing.CapturedPointedAtEdgeMidPositions[i].y, _lnx_meshManipulator.Edge_LastSelected.MidPosition.y
				);
				UnityEngine.Assertions.Assert.AreApproximatelyEqual(
					_tdg_pointingAndGrabbing.CapturedPointedAtEdgeMidPositions[i].z, _lnx_meshManipulator.Edge_LastSelected.MidPosition.z
				);

				Debug.Log($"pos now: '{_lnx_meshManipulator.Edge_LastSelected.MidPosition}'");
				#endregion
				
			}
		}
		#endregion

	}
}

