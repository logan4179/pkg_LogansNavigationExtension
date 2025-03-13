using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LogansNavigationExtension;
using UnityEngine.AI;
using JetBrains.Annotations;
using System.IO;
using static UnityEngine.Networking.UnityWebRequest;

namespace LoganLand.LogansNavmeshExtension.Tests
{
    public class LNXEditModeTests
    {
        NavMeshTriangulation _nmTriangulation;

        string filePath_lnx_navmesh = $"{Directory.GetCurrentDirectory()}\\Packages\\LogansNavigationExtension\\Testing\\Unit Tests\\Test Data\\nm_B.json";
        LNX_NavMesh _lnx_navmesh;

		LNX_MeshManipulator _lnx_meshManipulator;

        string filePath_test_SamplePosition = $"{Directory.GetCurrentDirectory()}\\Packages\\LogansNavigationExtension\\Testing\\Unit Tests\\Test Data\\SamplePosition_data_A.json";
        Test_SamplePosition _test_samplePosition;

		string filePath_test_closestOnPerimeter = $"{Directory.GetCurrentDirectory()}\\Packages\\LogansNavigationExtension\\Testing\\Unit Tests\\Test Data\\closestOnPerimeter_data_A.json";
		LNX_TestClosestOnPerimeter _test_closestOnPerimeter;

		string filePath_test_pointingAndGrabbing = $"{Directory.GetCurrentDirectory()}\\Packages\\LogansNavigationExtension\\Testing\\Unit Tests\\Test Data\\pointingAndGrabbing_A.json";
		Test_pointingAndGrabbing _test_pointingAndGrabbing;

		#region A - Setup Tests---------------------------------------------------------------------------
		[Test]
		public void a1_SetUpObjects()
        {
			GameObject go = new GameObject();

			_lnx_navmesh = go.AddComponent<LNX_NavMesh>();

			_lnx_meshManipulator = go.AddComponent<LNX_MeshManipulator>();
			_lnx_meshManipulator._LNX_NavMesh = _lnx_navmesh;

			//Note: decided not to do the following lines which create a navmesh from json because the navmesh is setup by 
			//a navmeshtriangulation object, which needs to be tested...
			//Debug.Log (File.Exists(filePath_lnx_navmesh) );
			// Debug.Log(filePath_lnx_navmesh);
			//jsonString = File.ReadAllText(filePath_lnx_navmesh);
			//Debug.Log( nmJsonString );
			//nm = JsonUtility.FromJson<LNX_NavMesh>( nmJsonString ); //note: this won't work bc FromJson() doesn't support deserializing a monobehavior object
			//JsonUtility.FromJsonOverwrite(jsonString, nm); //note: this apparently does work to deserialize a monobehaviour...
			//Debug.Log(nm.Triangles.Length);
		}

		[Test]
        public void a2_FetchTriangulation_Tests()
        {
			_nmTriangulation = NavMesh.CalculateTriangulation();
			Debug.Log($"{nameof(NavMesh.CalculateTriangulation)} calculated '{_nmTriangulation.vertices}' vertices, '{_nmTriangulation.areas}' areas, and '{_nmTriangulation.indices}' indices.");

			_lnx_navmesh.LayerMaskName = "lr_EnvSolid"; //not necessary, but just to be sure...
			_lnx_navmesh.FetchTriangulation();

            Assert.AreEqual( _nmTriangulation.areas.Length, _lnx_navmesh.Triangles.Length );


        }

		[Test]
		public void a3_Relationships_Tests()
		{
			Debug.Log($"\nChecking relationships...");
			for ( int i = 0; i < _lnx_navmesh.Triangles.Length; i++ )
			{
				Debug.Log($"checking tri '{i}'...");

				Assert.AreEqual( _lnx_navmesh.Triangles[i].Relationships.Length, _lnx_navmesh.Triangles.Length );

				Assert.Greater( _lnx_navmesh.Triangles[i].AdjacentTriIndices.Length, 0 );
			}
		}

		/*
		[Test]
		public void aX_Modifications_Tests()
		{
			//TODO: Test modifications when I can figure out how I want to do this...
			Debug.Log($"\nChecking modifications...");

		}
		*/
		#endregion

		#region B - LNX_Navmesh function Tests---------------------------------------------------------------------------
		[Test]
        public void b1_SamplePosition_Tests()
        {
			Debug.Log($"Creating test object from json...");

			if ( !File.Exists(filePath_test_SamplePosition) )
			{
				Debug.LogError( $"PROBLEM!!!!! file at test path does not exist. Cannot perform test." );
				return;
			}

			#region SETUP TEST OBJECT-----------------------------
			_test_samplePosition = _lnx_navmesh.gameObject.AddComponent<Test_SamplePosition>();
			//Debug.Log ( File.Exists(filePath_test_SamplePosition) );
			//Debug.Log( filePath_test_SamplePosition );
			string jsonString = File.ReadAllText(filePath_test_SamplePosition);

			JsonUtility.FromJsonOverwrite(jsonString, _test_samplePosition);
			//Debug.Log( _test_samplePosition.problemPositions.Count );

			Assert.IsNotNull(_test_samplePosition.testPositions);
			Assert.Greater(_test_samplePosition.testPositions.Count, 0);
			#endregion

			for ( int i = 0; i < _test_samplePosition.testPositions.Count; i++ )
			{
				Debug.Log($"{i}...");
				LNX_ProjectionHit hit = new LNX_ProjectionHit();
				_lnx_navmesh.SamplePosition( _test_samplePosition.testPositions[i], out hit, 10f );
				//Assert.AreEqual( _test_samplePosition.hitPositions[i], hit.Position ); //got rounding point issue
				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.hitPositions[i].x, hit.HitPosition.x );
				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.hitPositions[i].y, hit.HitPosition.y );
				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.hitPositions[i].z, hit.HitPosition.z );

				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.triCenters[i].x, _lnx_navmesh.Triangles[hit.Index_intersectedTri].V_center.x );
				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.triCenters[i].y, _lnx_navmesh.Triangles[hit.Index_intersectedTri].V_center.y );
				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.triCenters[i].z, _lnx_navmesh.Triangles[hit.Index_intersectedTri].V_center.z );
			}
		}

		[Test]
		public void b2_Test_ClosestOnPerimeter()
		{
			Debug.Log($"Creating test object from json...");

			if ( !File.Exists(filePath_test_closestOnPerimeter) )
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}

			#region SETUP CLOSEST ON PERIMETER TEST -----------------------------
			_test_closestOnPerimeter = _lnx_navmesh.gameObject.AddComponent<LNX_TestClosestOnPerimeter>();
			//Debug.Log ( File.Exists(filePath_test_closestOnPerimeter) );
			//Debug.Log( filePath_test_closestOnPerimeter );
			string jsonString = File.ReadAllText(filePath_test_closestOnPerimeter);

			JsonUtility.FromJsonOverwrite(jsonString, _test_closestOnPerimeter);
			//Debug.Log( _test_samplePosition.problemPositions.Count );

			Assert.IsNotNull(_test_closestOnPerimeter.testPositions);
			Assert.Greater(_test_closestOnPerimeter.testPositions.Count, 0);
			#endregion

			for ( int i = 0; i < _test_closestOnPerimeter.testPositions.Count; i++ )
			{
				Debug.Log($"{i}. expecting: '{_test_closestOnPerimeter.resultPositions[i]}'...");

				LNX_ProjectionHit hit = new LNX_ProjectionHit();

				if ( _lnx_navmesh.SamplePosition(_test_closestOnPerimeter.testPositions[i], out hit, 10f) ) //It needs to do this in order to decide which triangle to use...
				{
					Vector3 v_result = _lnx_navmesh.Triangles[hit.Index_intersectedTri].ClosestPointOnPerimeter( _test_closestOnPerimeter.testPositions[i] );

					UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_closestOnPerimeter.resultPositions[i].x, v_result.x );
					UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_closestOnPerimeter.resultPositions[i].y, v_result.y );
					UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_closestOnPerimeter.resultPositions[i].z, v_result.z );
				}
			}
		}

		[Test]
		public void b3_Test_ClosestOnPerimeter_triCenters()
		{
			Debug.Log($"Sampling '{_test_closestOnPerimeter.testPositions.Count}' test positions at: '{System.DateTime.Now.ToString()}'");

			for (int i = 0; i < _test_closestOnPerimeter.testPositions.Count; i++)
			{
				Debug.Log($"{i}. expecting: '{_test_closestOnPerimeter.resultPositions[i]}'...");

				LNX_ProjectionHit hit = new LNX_ProjectionHit();

				if (_lnx_navmesh.SamplePosition(_test_closestOnPerimeter.testPositions[i], out hit, 10f)) //It needs to do this in order to decide which triangle to use...
				{
					Vector3 v_result = _lnx_navmesh.Triangles[hit.Index_intersectedTri].ClosestPointOnPerimeter(_test_closestOnPerimeter.testPositions[i]);

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_closestOnPerimeter.triCenters[i].x, _lnx_navmesh.Triangles[hit.Index_intersectedTri].V_center.x);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_closestOnPerimeter.triCenters[i].y, _lnx_navmesh.Triangles[hit.Index_intersectedTri].V_center.y);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_closestOnPerimeter.triCenters[i].z, _lnx_navmesh.Triangles[hit.Index_intersectedTri].V_center.z);
				}
			}
		}
		#endregion

		#region C - Mesh Manipulation Tests---------------------------------------------------------------------------
		[Test]
		public void c1_SetupPointingAndGrabbingObject()
		{
			Debug.Log($"Creating test object from Json...");

			if (!File.Exists(filePath_test_pointingAndGrabbing))
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}

			#region CREATE TEST OBJECT -----------------------------
			_test_pointingAndGrabbing = _lnx_navmesh.gameObject.AddComponent<Test_pointingAndGrabbing>();
			string jsonString = File.ReadAllText(filePath_test_pointingAndGrabbing);

			JsonUtility.FromJsonOverwrite(jsonString, _test_pointingAndGrabbing);
			#endregion
		}

		[Test]
		public void c2_PointingAtAndGrabbingVerts_Tests()
		{
			Debug.Log($"{nameof(c2_PointingAtAndGrabbingVerts_Tests)}--------------------------------");
			Debug.Log($"Creating test object from Json...");

			if ( !File.Exists(filePath_test_pointingAndGrabbing) )
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}

			#region CREATE TEST OBJECT -----------------------------
			_test_pointingAndGrabbing = _lnx_navmesh.gameObject.AddComponent<Test_pointingAndGrabbing>();
			string jsonString = File.ReadAllText( filePath_test_pointingAndGrabbing );

			JsonUtility.FromJsonOverwrite( jsonString, _test_pointingAndGrabbing );
			#endregion

			//Debug.Log(_lnx_meshManipulator.SelectMode);
			_lnx_meshManipulator.SelectMode = LNX_SelectMode.Vertices;
			for( int i = 0; i < _test_pointingAndGrabbing.TestPositions_vert.Count; i++ )
			{
				Debug.Log($"{i}...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_test_pointingAndGrabbing.TestPositions_vert[i],
					_test_pointingAndGrabbing.TestDirections_vert[i]
				);

				if( _lnx_meshManipulator.Vert_CurrentlyPointingAt == null )
				{
					Assert.AreEqual( _test_pointingAndGrabbing.CapturedVertPositions[i], Vector3.zero );
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
					UnityEngine.Assertions.Assert.AreEqual( _test_pointingAndGrabbing.CapturedNumberOfSharedVerts[i], _lnx_meshManipulator.Verts_currentlySelected.Count );

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
		public void c3_PointingAtAndGrabbingEdges_Tests()
		{
			Debug.Log($"{nameof(c3_PointingAtAndGrabbingEdges_Tests)}--------------------------------");

			_lnx_meshManipulator.SelectMode = LNX_SelectMode.Edges;
			for ( int i = 0; i < _test_pointingAndGrabbing.TestPositions_vert.Count; i++ )
			{
				Debug.Log($"{i}...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_test_pointingAndGrabbing.TestPositions_edge[i],
					_test_pointingAndGrabbing.TestDirections_edge[i]
				);

				if ( _lnx_meshManipulator.Edge_CurrentlyPointingAt == null )
				{
					Assert.AreEqual( _test_pointingAndGrabbing.CapturedEdgeCenterPositions[i], Vector3.zero );
				}
				else
				{
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
					UnityEngine.Assertions.Assert.AreEqual( _test_pointingAndGrabbing.CapturedNumberOfSharedVerts_edge[i], _lnx_meshManipulator.Verts_currentlySelected.Count );

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
		public void c4_PointingAtAndGrabbingFaces_Tests()
		{
			Debug.Log($"{nameof(c4_PointingAtAndGrabbingFaces_Tests)}--------------------------------");

			_lnx_meshManipulator.SelectMode = LNX_SelectMode.Faces;
			Debug.Log($"running '{_test_pointingAndGrabbing.TestPositions_face.Count}' test positions...");
			for ( int i = 0; i < _test_pointingAndGrabbing.TestPositions_face.Count; i++ )
			{
				Debug.Log($"{i}...");

				_lnx_meshManipulator.TryPointAtComponentViaDirection(
					_test_pointingAndGrabbing.TestPositions_face[i],
					_test_pointingAndGrabbing.TestDirections_face[i]
				);

				if ( _lnx_meshManipulator.Index_TriPointingAt < 0 )
				{
					Assert.AreEqual( _test_pointingAndGrabbing.CapturedFaceCenterPositions[i], Vector3.zero );
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

		/*
		[Test]
		public void x_Test_SelectFace()
		{
			//make tests showing that I can select faces at expected mouse positions, and that I 
			//cannot select them at other expected mouse positions
		}
		*/

		/*
		[Test]
		public void x_Test_SelectEdge()
		{
			//make tests showing that I can select edges at expected mouse positions, and that I 
			//cannot select them at other expected mouse positions
		}
		*/

		/*
		[Test]
		public void x_Test_SelectVertex()
		{
			//make tests showing that I can select verts at expected mouse positions, and that I 
			//cannot select them at other expected mouse positions
		}
		*/

		/*
		 * 
		[Test]
		public void x_Test_SelectCutEdge()
		{
			//make tests showing that I can and cannot cut edges in various scenarios
		}
		*/


		/*
        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator LNXEditModeTestsWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.

			yield return null;
        }
		*/

	}
}

