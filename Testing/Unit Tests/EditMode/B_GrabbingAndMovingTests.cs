using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LogansNavigationExtension;
using UnityEngine.AI;
using JetBrains.Annotations;
using System.IO;

namespace LoganLand.LogansNavmeshExtension.Tests
{
    public class B_GrabbingAndMovingTests
    {
        LNX_NavMesh _previouslyGeneratedNavmesh;

		LNX_MeshManipulator _lnx_meshManipulator;

		[Header("TEST OBJECTS")]
		string filePath_test_PointingAndGrabbing = $"{Directory.GetCurrentDirectory()}\\Packages\\LogansNavigationExtension\\Testing\\Unit Tests\\Test Data\\pointingAndGrabbing_A.json";
		Test_pointingAndGrabbing _test_pointingAndGrabbing;

		string filePath_test_MeshManipulation = $"{Directory.GetCurrentDirectory()}\\Packages\\LogansNavigationExtension\\Testing\\Unit Tests\\Test Data\\meshManipulation_A.json";
		Test_MoveComponents _test_meshManipulation;


		#region A - Setup --------------------------------------------------------------------------------
		[Test]
		public void a1_SetupObjects()
		{
			GameObject go = GameObject.Find("TestLNX_Navmesh");

			_previouslyGeneratedNavmesh = go.GetComponent<LNX_NavMesh>();

			_lnx_meshManipulator = go.AddComponent<LNX_MeshManipulator>();
			_lnx_meshManipulator._LNX_NavMesh = _previouslyGeneratedNavmesh;

			Assert.NotNull(_previouslyGeneratedNavmesh);
		}

		[Test]
		public void a2_CreateTestObjectsFromJson()
		{
			Debug.Log($"Creating test object from Json...");

			#region pointing and grabbing -------------------------------------------------------
			if ( !File.Exists(filePath_test_PointingAndGrabbing) )
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}

			//CREATE TEST OBJECT -----------------------------
			_test_pointingAndGrabbing = _previouslyGeneratedNavmesh.gameObject.AddComponent<Test_pointingAndGrabbing>();
			string jsonString = File.ReadAllText(filePath_test_PointingAndGrabbing);
			JsonUtility.FromJsonOverwrite(jsonString, _test_pointingAndGrabbing);
			Assert.NotNull(_test_pointingAndGrabbing);
			#endregion

			#region mesh manipulation -------------------------------------------------------
			if ( !File.Exists(filePath_test_MeshManipulation) )
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}

			//CREATE TEST OBJECT -----------------------------
			_test_meshManipulation = _previouslyGeneratedNavmesh.gameObject.AddComponent<Test_MoveComponents>();
			jsonString = File.ReadAllText( filePath_test_MeshManipulation );
			JsonUtility.FromJsonOverwrite( jsonString, _test_meshManipulation );
			Assert.NotNull( _test_meshManipulation );
			#endregion
		}
		#endregion

		#region B - Pointing and grabbing components ----------------------------------------------------
		[Test]
		public void b1_PointingAtAndGrabbingVerts()
		{
			Debug.Log($"{nameof(b1_PointingAtAndGrabbingVerts)}--------------------------------");
			Debug.Log($"Creating test object from Json...");

			if ( !File.Exists(filePath_test_PointingAndGrabbing) )
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}
			else
			{
				Debug.Log($"File path does exist. Creating test object...");
			}

			#region CREATE TEST OBJECT -----------------------------
			_test_pointingAndGrabbing = _previouslyGeneratedNavmesh.gameObject.AddComponent<Test_pointingAndGrabbing>();
			string jsonString = File.ReadAllText(filePath_test_PointingAndGrabbing);

			JsonUtility.FromJsonOverwrite(jsonString, _test_pointingAndGrabbing);
			#endregion
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestSectionEndString, "setup") );

			//Debug.Log(_lnx_meshManipulator.SelectMode);
			_lnx_meshManipulator.SelectMode = LNX_SelectMode.Vertices;
			for (int i = 0; i < _test_pointingAndGrabbing.TestPositions_vert.Count; i++)
			{
				Debug.Log($"{i}...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_test_pointingAndGrabbing.TestPositions_vert[i],
					_test_pointingAndGrabbing.TestDirections_vert[i]
				);

				if (_lnx_meshManipulator.Vert_CurrentlyPointingAt == null)
				{
					Assert.AreEqual(_test_pointingAndGrabbing.CapturedVertPositions[i], Vector3.zero);
				}
				else
				{
					//Debug.Log($"{_test_pointingAndGrabbing.CapturedVertPositions[i]} || {_lnx_meshManipulator.Vert_CurrentlyPointingAt.Position}");

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_pointingAndGrabbing.CapturedVertPositions[i].x, _lnx_meshManipulator.Vert_CurrentlyPointingAt.Position.x);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_pointingAndGrabbing.CapturedVertPositions[i].y, _lnx_meshManipulator.Vert_CurrentlyPointingAt.Position.y);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_pointingAndGrabbing.CapturedVertPositions[i].z, _lnx_meshManipulator.Vert_CurrentlyPointingAt.Position.z);

					//Debug.Log("trying grab stuff...");
					_lnx_meshManipulator.TryGrab();

					//-------------------------------------------------------------------------------------------------------
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedPositions_vert[i].x, _lnx_meshManipulator.Vert_LastSelected.Position.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedPositions_vert[i].y, _lnx_meshManipulator.Vert_LastSelected.Position.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedPositions_vert[i].z, _lnx_meshManipulator.Vert_LastSelected.Position.z
					);


					//-------------------------------------------------------------------------------------------------------
					// Debug.Log($"trying count. expecting '{_test_pointingAndGrabbing.CapturedNumberOfSharedVerts[i]}'...");
					UnityEngine.Assertions.Assert.AreEqual(_test_pointingAndGrabbing.CapturedNumberOfSharedVerts[i], _lnx_meshManipulator.Verts_currentlySelected.Count);

					//-------------------------------------------------------------------------------------------------------
					//Debug.Log($"trying manipulator pos. expecting '{_test_pointingAndGrabbing.GrabbedManipulatorPos_vert[i]}'...");
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedManipulatorPos_vert[i].x, _lnx_meshManipulator.manipulatorPos.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedManipulatorPos_vert[i].y, _lnx_meshManipulator.manipulatorPos.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedManipulatorPos_vert[i].z, _lnx_meshManipulator.manipulatorPos.z
					);
				}
			}
		}

		[Test]
		public void b2_PointingAtAndGrabbingEdges()
		{
			Debug.Log($"{nameof(b2_PointingAtAndGrabbingEdges)}--------------------------------");

			_lnx_meshManipulator.SelectMode = LNX_SelectMode.Edges;
			for (int i = 0; i < _test_pointingAndGrabbing.TestPositions_vert.Count; i++)
			{
				Debug.Log($"{i}...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_test_pointingAndGrabbing.TestPositions_edge[i],
					_test_pointingAndGrabbing.TestDirections_edge[i]
				);

				Debug.Log($"flag after pointing: '{_lnx_meshManipulator.Flag_AComponentIsCurrentlyHighlighted}'");

				if (_lnx_meshManipulator.Edge_CurrentlyPointingAt == null)
				{
					Debug.Log($"{nameof(_lnx_meshManipulator.Edge_CurrentlyPointingAt)} was null...");
					Assert.AreEqual(_test_pointingAndGrabbing.CapturedEdgeCenterPositions[i], Vector3.zero);
				}
				else
				{
					Debug.Log($"{nameof(_lnx_meshManipulator.Edge_CurrentlyPointingAt)} was NOT null...");

					//Debug.Log($"{_test_pointingAndGrabbing.CapturedEdgeCenterPositions[i]} || {_lnx_meshManipulator.Edge_CurrentlyPointingAt.Position}");

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_pointingAndGrabbing.CapturedEdgeCenterPositions[i].x, _lnx_meshManipulator.Edge_CurrentlyPointingAt.MidPosition.x);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_pointingAndGrabbing.CapturedEdgeCenterPositions[i].y, _lnx_meshManipulator.Edge_CurrentlyPointingAt.MidPosition.y);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_pointingAndGrabbing.CapturedEdgeCenterPositions[i].z, _lnx_meshManipulator.Edge_CurrentlyPointingAt.MidPosition.z);

					//Debug.Log("trying grab stuff...");
					_lnx_meshManipulator.TryGrab();

					//-------------------------------------------------------------------------------------------------------
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedPositions_edge[i].x, _lnx_meshManipulator.Edge_LastSelected.MidPosition.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedPositions_edge[i].y, _lnx_meshManipulator.Edge_LastSelected.MidPosition.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedPositions_edge[i].z, _lnx_meshManipulator.Edge_LastSelected.MidPosition.z
					);

					//-------------------------------------------------------------------------------------------------------
					// Debug.Log($"trying count. expecting '{_test_pointingAndGrabbing.CapturedNumberOfSharedVerts[i]}'...");
					UnityEngine.Assertions.Assert.AreEqual(_test_pointingAndGrabbing.CapturedNumberOfSharedVerts_edge[i], _lnx_meshManipulator.Verts_currentlySelected.Count);

					//-------------------------------------------------------------------------------------------------------
					//Debug.Log($"trying manipulator pos. expecting '{_test_pointingAndGrabbing.GrabbedManipulatorPos_edge[i]}'...");
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedManipulatorPos_edge[i].x, _lnx_meshManipulator.manipulatorPos.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedManipulatorPos_edge[i].y, _lnx_meshManipulator.manipulatorPos.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedManipulatorPos_edge[i].z, _lnx_meshManipulator.manipulatorPos.z
					);
				}
			}
		}

		[Test]
		public void b3_PointingAtAndGrabbingFaces()
		{
			Debug.Log($"{nameof(b3_PointingAtAndGrabbingFaces)}--------------------------------");

			_lnx_meshManipulator.SelectMode = LNX_SelectMode.Faces;
			Debug.Log($"running '{_test_pointingAndGrabbing.TestPositions_face.Count}' test positions...");
			for (int i = 0; i < _test_pointingAndGrabbing.TestPositions_face.Count; i++)
			{
				Debug.Log($"{i}...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_test_pointingAndGrabbing.TestPositions_face[i],
					_test_pointingAndGrabbing.TestDirections_face[i]
				);

				if (_lnx_meshManipulator.Index_TriPointingAt < 0)
				{
					Assert.AreEqual(_test_pointingAndGrabbing.CapturedFaceCenterPositions[i], Vector3.zero);
				}
				else
				{
					//Debug.Log($"{_test_pointingAndGrabbing.CapturedEdgeCenterPositions[i]} || {_lnx_meshManipulator.Edge_CurrentlyPointingAt.Position}");

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_pointingAndGrabbing.CapturedFaceCenterPositions[i].x, _lnx_meshManipulator.PointingAtTri.V_center.x);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_pointingAndGrabbing.CapturedFaceCenterPositions[i].y, _lnx_meshManipulator.PointingAtTri.V_center.y);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_pointingAndGrabbing.CapturedFaceCenterPositions[i].z, _lnx_meshManipulator.PointingAtTri.V_center.z);

					//Debug.Log("trying grab stuff...");
					_lnx_meshManipulator.TryGrab();

					//-------------------------------------------------------------------------------------------------------
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedPositions_face[i].x, _lnx_meshManipulator.LastSelectedTri.V_center.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedPositions_face[i].y, _lnx_meshManipulator.LastSelectedTri.V_center.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedPositions_face[i].z, _lnx_meshManipulator.LastSelectedTri.V_center.z
					);


					//-------------------------------------------------------------------------------------------------------
					// Debug.Log($"trying count. expecting '{_test_pointingAndGrabbing.CapturedNumberOfSharedVerts[i]}'...");
					UnityEngine.Assertions.Assert.AreEqual(_test_pointingAndGrabbing.CapturedNumberOfSharedVerts_face[i], _lnx_meshManipulator.Verts_currentlySelected.Count);

					//-------------------------------------------------------------------------------------------------------
					//Debug.Log($"trying manipulator pos. expecting '{_test_pointingAndGrabbing.GrabbedManipulatorPos_edge[i]}'...");
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedManipulatorPos_face[i].x, _lnx_meshManipulator.manipulatorPos.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedManipulatorPos_face[i].y, _lnx_meshManipulator.manipulatorPos.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_pointingAndGrabbing.GrabbedManipulatorPos_face[i].z, _lnx_meshManipulator.manipulatorPos.z
					);
				}
			}
		}

		#endregion

		#region C - Moving Components -----------------------------------------------------------------------
		[Test]
		public void c1_MovingVerts()
		{
			Debug.Log($"{nameof(c1_MovingVerts)}--------------------------------");
			Debug.Log($"Creating test object from Json...");

			#region CREATE TEST OBJECT -----------------------------
			if (!File.Exists(filePath_test_MeshManipulation))
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}
			else
			{
				Debug.Log($"File path does exist. Creating test object...");
			}

			_test_meshManipulation = _previouslyGeneratedNavmesh.gameObject.AddComponent<Test_MoveComponents>();
			string jsonString = File.ReadAllText( filePath_test_MeshManipulation );

			JsonUtility.FromJsonOverwrite( jsonString, _test_meshManipulation );
			#endregion

			if( _test_meshManipulation.TestMousePositions_vert == null || _test_meshManipulation.TestMousePositions_vert.Count == 0 )
			{
				Debug.LogError($"couldn't do test because {nameof(_test_meshManipulation.TestMousePositions_vert)} was either null or 0 count. Returning early...");
				return;
			}
			else
			{
				Debug.Log($"{nameof(_test_meshManipulation.TestMousePositions_vert)} count is '{_test_meshManipulation.TestMousePositions_vert.Count}'. Proceeding...");
			}
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestSectionEndString, "setup"));

			Debug.Log($"now selecting and moving {_test_meshManipulation.TestMousePositions_vert.Count} verts...");
			//Debug.Log(_lnx_meshManipulator.SelectMode);
			_lnx_meshManipulator.SelectMode = LNX_SelectMode.Vertices;
			for ( int i = 0; i < _test_meshManipulation.TestMousePositions_vert.Count; i++ )
			{
				Debug.Log($"{i}...........................................................................");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_test_meshManipulation.TestMousePositions_vert[i],
					_test_meshManipulation.TestMouseDirections_vert[i]
				);

				if ( _lnx_meshManipulator.Vert_CurrentlyPointingAt == null )
				{
					Debug.LogError($"tried pointing at vert, but got null. Returning early...");
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
						$"expected position: '{_test_meshManipulation.GrabbedPositions_vert[i]}'");

					Vector3 vOffset = new Vector3( 1.5f, 1.5f, 1.5f );
					Vector3 v_moveTo = _test_meshManipulation.GrabbedPositions_vert[i] + vOffset;

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
					Debug.Log($"now moving '{_lnx_meshManipulator.Vert_LastSelected.Position}' back to '{_test_meshManipulation.GrabbedPositions_vert[i]}'...");

					_lnx_meshManipulator.MoveSelectedVerts( _test_meshManipulation.GrabbedPositions_vert[i] );

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_meshManipulation.GrabbedPositions_vert[i].x, _lnx_meshManipulator.Vert_LastSelected.Position.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_meshManipulation.GrabbedPositions_vert[i].y, _lnx_meshManipulator.Vert_LastSelected.Position.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_meshManipulation.GrabbedPositions_vert[i].z, _lnx_meshManipulator.Vert_LastSelected.Position.z
					);

					Debug.Log($"pos now: '{_lnx_meshManipulator.Vert_LastSelected.Position}'");
					#endregion
				}
			}
		}

		[Test]
		public void c2_MovingEdges()
		{
			Debug.Log($"{nameof(c2_MovingEdges)}--------------------------------");
			Debug.Log($"Creating test object from Json...");

			if (_test_meshManipulation.TestMousePositions_edge == null || _test_meshManipulation.TestMousePositions_edge.Count == 0)
			{
				Debug.LogError($"couldn't do test because {nameof(_test_meshManipulation.TestMousePositions_edge)} was either null or 0 count. Returning early...");
				return;
			}
			else
			{
				Debug.Log($"{nameof(_test_meshManipulation.TestMousePositions_edge)} count is '{_test_meshManipulation.TestMousePositions_edge.Count}'. Proceeding...");
			}
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestSectionEndString, "setup"));

			_lnx_meshManipulator.SelectMode = LNX_SelectMode.Edges;
			for ( int i = 0; i < _test_meshManipulation.TestMousePositions_edge.Count; i++ )
			{
				Debug.Log($"{i}...........................................................................");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_test_meshManipulation.TestMousePositions_edge[i],
					_test_meshManipulation.TestMouseDirections_edge[i]
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
						$"logged midpos: '{_test_meshManipulation.GrabbedMidPositions_edge[i]}'");

					Vector3 vOffset = new Vector3(1.5f, 1.5f, 1.5f);
					Vector3 v_moveTo = _test_meshManipulation.GrabbedMidPositions_edge[i] + vOffset;

					Debug.Log($"now moving to '{v_moveTo}'...");

					_lnx_meshManipulator.MoveSelectedVerts( v_moveTo );
					_lnx_meshManipulator._LNX_NavMesh.RefeshMesh(); //important so that the midpos will get re-calculated.

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
					Debug.Log($"now moving '{_lnx_meshManipulator.Edge_LastSelected.MidPosition}' back to '{_test_meshManipulation.GrabbedMidPositions_edge[i]}'...");

					_lnx_meshManipulator.MoveSelectedVerts( _test_meshManipulation.GrabbedMidPositions_edge[i] );
					_lnx_meshManipulator._LNX_NavMesh.RefeshMesh(); //important so that the midpos will get re-calculated.

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_meshManipulation.GrabbedMidPositions_edge[i].x, _lnx_meshManipulator.Edge_LastSelected.MidPosition.x
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_meshManipulation.GrabbedMidPositions_edge[i].y, _lnx_meshManipulator.Edge_LastSelected.MidPosition.y
					);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(
						_test_meshManipulation.GrabbedMidPositions_edge[i].z, _lnx_meshManipulator.Edge_LastSelected.MidPosition.z
					);

					Debug.Log($"pos now: '{_lnx_meshManipulator.Edge_LastSelected.MidPosition}'");
					#endregion
				}
			}
		}
		#endregion

	}
}

