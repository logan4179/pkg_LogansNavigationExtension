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
using System.Linq;

namespace LoganLand.LogansNavmeshExtension.Tests
{
    public class A_SetUpTests
    {
        NavMeshTriangulation _nmTriangulation;

        /// <summary>
		/// The LNX_NavMesh that gets generated by the test, when the tests is ran, from the scene geometry.
		/// </summary>
		LNX_NavMesh _sceneGeneratedLnxNavmesh;

		/// <summary>
		/// Added this so that we could save expected values from the scene generated navmesh for testing.
		/// </summary>
		LNX_NavmeshFullDataSaver _sceneGeneratedLnxNavmesh_expectedDataModel;

		/// <summary>Saved LNX_NavMesh that gets saved and reconstructed from JSON represented the expected state/values for the 
		/// checking tests. Note: This should ideally be the exact same as the generated navmesh. The idea is that this can 
		/// serialize (store) certain predictable, saveable, values, like the bounds positions/size, and then compared to the 
		/// dynamically-constructed lnx navmesh. Therefore, this object should be re-serialized to json anytime I make a change 
		/// to the scene geometry that makes the navmesh.</summary>
		LNX_NavMesh _jsonGeneratedLnxNavmesh; //note: originally I was going to use this to save the _mesh and triangulation collection lengths instead of testing a hard-coded value, but those things don't seem to get serialized to JSON. Leaving this here in case I think of something else to do with it...

		LNX_MeshManipulator _sceneGeneratedMeshManipulator, _serializedMeshManipulator;

        TDG_SamplePosition _test_samplePosition;

		TDG_SampleClosestPtOnPerimeter _test_closestOnPerimeter;


		#region A - Setup Tests---------------------------------------------------------------------------
		[Test]
		public void a1_CreateAndSetUpObjectsInTheScene()
        {
			#region FIND/HANDLE EXISTING SCENE NAVMESH ----------------------------------------------------------------
			LNX_NavMesh existingSceneMesh = GameObject.Find(LNX_UnitTestUtilities.Name_ExistingSceneNavmeshGameobject).GetComponent<LNX_NavMesh>();

			if ( existingSceneMesh != null )
			{
				Debug.Log($"found scene navmesh. now disabling...");
				existingSceneMesh.enabled = false;
			}
			else
			{
				Debug.LogWarning($"Didn't find scene navmesh...");
			}

			Assert.NotNull( existingSceneMesh );
			#endregion

			#region SETUP SCENE-GENERATED NAVMESH ---------------------------------------------------------------------
			GameObject go = new GameObject();
			go.name = LNX_UnitTestUtilities.Name_GeneratedNavmeshGameobject; //so that other test scripts can find this object.

			_sceneGeneratedLnxNavmesh = go.AddComponent<LNX_NavMesh>();
			Assert.NotNull( _sceneGeneratedLnxNavmesh );
			_sceneGeneratedLnxNavmesh.LayerMaskName = "lr_EnvSolid"; //not necessary, but just to be sure...
			_sceneGeneratedLnxNavmesh.CalculateTriangulation();
			Assert.NotNull(_sceneGeneratedLnxNavmesh._Mesh);
			Debug.Log($"mesh visual. {nameof(_sceneGeneratedLnxNavmesh._Mesh.vertices)} length: '{_sceneGeneratedLnxNavmesh._Mesh.vertices.Length}', " +
				$"{nameof(_sceneGeneratedLnxNavmesh._Mesh.triangles)} length: '{_sceneGeneratedLnxNavmesh._Mesh.triangles.Length}, " +
				$"{nameof(_sceneGeneratedLnxNavmesh._Mesh.normals)} length: '{_sceneGeneratedLnxNavmesh._Mesh.normals.Length}, ");

			Debug.Log($"Generated navmesh bounds information...");
			Debug.Log($"scene generated navmesh bounds size: '{_sceneGeneratedLnxNavmesh.V_BoundsSize}'");
			Debug.Log($"scene generated navmesh bounds center: '{_sceneGeneratedLnxNavmesh.V_BoundsCenter}'");

			_sceneGeneratedMeshManipulator = go.AddComponent<LNX_MeshManipulator>();
			_sceneGeneratedMeshManipulator._LNX_NavMesh = _sceneGeneratedLnxNavmesh;
			Assert.NotNull( _sceneGeneratedMeshManipulator );
			Debug.Log (string.Format(LNX_UnitTestUtilities.UnitTestSectionEndString, "set up scene-generated navmesh") );
			#endregion

			#region SETUP SCENE-GENERATED-NAVMESH DATA MODEL -------------------------------------------------------
			
			_sceneGeneratedLnxNavmesh_expectedDataModel = new LNX_NavmeshFullDataSaver();
			string jsonString = File.ReadAllText(TDG_Manager.filePath_sceneGeneratedNavmeshDataModel);
			JsonUtility.FromJsonOverwrite(jsonString, _sceneGeneratedLnxNavmesh_expectedDataModel);

			Assert.NotNull(_sceneGeneratedLnxNavmesh_expectedDataModel);
			Assert.NotNull(_sceneGeneratedLnxNavmesh_expectedDataModel._Lnx_Navmesh);

			Debug.Log("Now remaking data model navmesh from json...");
			jsonString = File.ReadAllText( TDG_Manager.filePath_sceneGeneratedLnxNavMesh );
			JsonUtility.FromJsonOverwrite( jsonString, _sceneGeneratedLnxNavmesh_expectedDataModel._Lnx_Navmesh );
			Debug.Log($"Generated datamodel bounds information...");
			Debug.Log($"data model navmesh bounds size: '{_sceneGeneratedLnxNavmesh_expectedDataModel._Lnx_Navmesh.V_BoundsSize}'");
			Debug.Log($"data model navmesh bounds center: '{_sceneGeneratedLnxNavmesh_expectedDataModel._Lnx_Navmesh.V_BoundsCenter}'");
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestSectionEndString, "set up scene-generated navmesh data model"));
			#endregion

			_sceneGeneratedMeshManipulator.ClearSelection(); //Not doing this was causing an error...

			#region SETUP SERIALIZED NAVMESH ---------------------------------------------------------------------
			GameObject go_serializedNavmesh = new GameObject();
			go_serializedNavmesh.name = LNX_UnitTestUtilities.Name_SerializedNavmeshGameobject; //so that other test scripts can find this
			_jsonGeneratedLnxNavmesh = go_serializedNavmesh.AddComponent<LNX_NavMesh>();
			jsonString = File.ReadAllText( TDG_Manager.filePath_serializedLnxNavMesh );
			JsonUtility.FromJsonOverwrite( jsonString, _jsonGeneratedLnxNavmesh );

			Assert.NotNull( _jsonGeneratedLnxNavmesh );

			_jsonGeneratedLnxNavmesh.ReconstructVisualizationMesh();

			_serializedMeshManipulator = go_serializedNavmesh.AddComponent<LNX_MeshManipulator>();
			_serializedMeshManipulator._LNX_NavMesh = _jsonGeneratedLnxNavmesh;
			Assert.NotNull( _serializedMeshManipulator );
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestMethodEndString, $"setting up json serialized navmesh...") );

			#endregion

			#region SET UP TRIANGULATION------------------------------------------
			Debug.Log( $"Now running NavMesh.CalculateTriangulation()..");
			_nmTriangulation = NavMesh.CalculateTriangulation();
			Debug.Log($"triangulation made '{_nmTriangulation.areas.Length}' areas, " +
				$"'{_nmTriangulation.indices.Length}' indices, and " +
				$"'{_nmTriangulation.vertices.Length}' vertices. ");

			if( _nmTriangulation.areas.Length < 60 || 
				_nmTriangulation.indices.Length < 60 || 
				_nmTriangulation.vertices.Length < 60 )
			{
				Debug.LogError($"It looks like the triangulation has changed considerably compared to what I was expecting. This might be due to the " +
					$"scene significantly changing, or having some geometry disabled in the scene that needs to be turned back on...");
			}

			#endregion
		}
		#endregion

		#region B) CHECK OBJECTS ------------------------------------------------------------------------------------------
		[Test]
        public void b1_Make_Sure_NavMesh_Has_Collection_Lengths_Consistent_With_Triangulation()
        {
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b1_Make_Sure_NavMesh_Has_Collection_Lengths_Consistent_With_Triangulation)) );
			
            Assert.AreEqual( _nmTriangulation.areas.Length, _sceneGeneratedLnxNavmesh.Triangles.Length );
		}

		[Test]
		public void b2_Check_That_Triangle_Indices_Properties_Are_Not_Negative_One()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b2_Check_That_Triangle_Indices_Properties_Are_Not_Negative_One)));

			for ( int i = 0; i < _sceneGeneratedLnxNavmesh.Triangles.Length; i++ )
			{
				Debug.Log($"{i}...");

				Debug.Log( $"checking Index_inCollection..." );
				Assert.Greater( _sceneGeneratedLnxNavmesh.Triangles[i].Index_inCollection, -1 );

				Debug.Log($"checking AreaIndex...");
				Assert.Greater(_sceneGeneratedLnxNavmesh.Triangles[i].AreaIndex, -1);
			}
		}

		[Test]
		public void b3_Check_That_Vert_Vismesh_Index_Properties_Are_Within_Bounds()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b3_Check_That_Vert_Vismesh_Index_Properties_Are_Within_Bounds)));

			for (int i = 0; i < _sceneGeneratedLnxNavmesh.Triangles.Length; i++)
			{
				Debug.Log($"{i}...");

				for (int i_verts = 0; i_verts < 3; i_verts++)
				{
					Debug.Log($"checking indices of vert[0]...");
					Debug.Log($"Verts[0].MeshIndex_triangles: '{_sceneGeneratedLnxNavmesh.Triangles[i].Verts[i_verts].Index_VisMesh_triangles}'");
					Assert.Greater(_sceneGeneratedLnxNavmesh.Triangles[i].Verts[i_verts].Index_VisMesh_triangles, -1);
					Assert.Less(_sceneGeneratedLnxNavmesh.Triangles[i].Verts[i_verts].Index_VisMesh_triangles, _sceneGeneratedLnxNavmesh._Mesh.triangles.Length);

					Debug.Log($"Verts[0].MeshIndex_vertices: '{_sceneGeneratedLnxNavmesh.Triangles[i].Verts[i_verts].Index_VisMesh_Vertices}'");
					Assert.Greater(_sceneGeneratedLnxNavmesh.Triangles[i].Verts[i_verts].Index_VisMesh_Vertices, -1);
					Assert.Less(_sceneGeneratedLnxNavmesh.Triangles[i].Verts[i_verts].Index_VisMesh_Vertices, _sceneGeneratedLnxNavmesh._Mesh.vertices.Length);
				}
			}
		}

		[Test]
		public void b4_Check_That_Bounds_Have_Expected_Values()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b4_Check_That_Bounds_Have_Expected_Values)));

			// CENTER-------------------------------------------------------
			Debug.Log($"Checking bounds center. scene generated navmesh bounds center: '{_sceneGeneratedLnxNavmesh.V_BoundsCenter}'. " +
				$"Datamodel bounds center: '{_sceneGeneratedLnxNavmesh_expectedDataModel._Lnx_Navmesh.V_BoundsCenter}'...");
			UnityEngine.Assertions.Assert.AreApproximatelyEqual( _sceneGeneratedLnxNavmesh_expectedDataModel._Lnx_Navmesh.V_BoundsCenter.x,
				_sceneGeneratedLnxNavmesh.V_BoundsCenter.x );
			UnityEngine.Assertions.Assert.AreApproximatelyEqual(_sceneGeneratedLnxNavmesh_expectedDataModel._Lnx_Navmesh.V_BoundsCenter.y,
				_sceneGeneratedLnxNavmesh.V_BoundsCenter.y);
			UnityEngine.Assertions.Assert.AreApproximatelyEqual(_sceneGeneratedLnxNavmesh_expectedDataModel._Lnx_Navmesh.V_BoundsCenter.z,
				_sceneGeneratedLnxNavmesh.V_BoundsCenter.z);
			// BOUNDS-------------------------------------------------------
			for ( int i = 0; i < 6; i++ )
			{
				Debug.Log($"{i}...");

				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _sceneGeneratedLnxNavmesh.V_Bounds[i].x, _jsonGeneratedLnxNavmesh.V_Bounds[i].x );
				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _sceneGeneratedLnxNavmesh.V_Bounds[i].y, _jsonGeneratedLnxNavmesh.V_Bounds[i].y );
				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _sceneGeneratedLnxNavmesh.V_Bounds[i].z, _jsonGeneratedLnxNavmesh.V_Bounds[i].z );
			}
		}

		[Test]
		public void b5_All_Triangle_Relationships_Array_Lengths_Equal_Master_Triangles_Array_Length()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b5_All_Triangle_Relationships_Array_Lengths_Equal_Master_Triangles_Array_Length)));

			Debug.Log($"\nChecking relationships...");
			Debug.Log($"Running through '{_sceneGeneratedLnxNavmesh.Triangles.Length}' triangles to check relationships..");
			for ( int i = 0; i < _sceneGeneratedLnxNavmesh.Triangles.Length; i++ )
			{
				Debug.Log($"checking tri '{i}'...");

				Assert.AreEqual( _sceneGeneratedLnxNavmesh.Triangles[i].Relationships.Length, _sceneGeneratedLnxNavmesh.Triangles.Length );
			}
		}

		[Test]
		public void b6_All_Triangle_AdjacentTriIndices_Array_Length_Are_Greater_Than_Zero()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b6_All_Triangle_AdjacentTriIndices_Array_Length_Are_Greater_Than_Zero)));

			Debug.Log($"\nChecking relationships...");
			Debug.Log($"Running through '{_sceneGeneratedLnxNavmesh.Triangles.Length}' triangles to check relationships..");
			for ( int i = 0; i < _sceneGeneratedLnxNavmesh.Triangles.Length; i++ )
			{
				Debug.Log($"checking tri '{i}'...");
				Assert.Greater( _sceneGeneratedLnxNavmesh.Triangles[i].AdjacentTriIndices.Length, 0 );
			}
		}
		#endregion

		#region C) CHECK VISUALIZATION MESH ------------------------------------------------------------------------------------------
		public static int largestMeshVisIndex_sceneGenerated = 0; //public and static, because I need to cache this and use it in another test file...
		public static int largestMeshVisINdex_serialized = 0; //public and static, because I need to cache this and use it in another test file...
		[Test]
		public void c1_Greatest_VisMeshIndex_Is_Same_As_Mesh_Vertices_Array_Length()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(c1_Greatest_VisMeshIndex_Is_Same_As_Mesh_Vertices_Array_Length)));

			Debug.Log($"Finding largest mesh vis index for meshes...");
			for (int i_triangles = 0; i_triangles < _sceneGeneratedLnxNavmesh.Triangles.Length; i_triangles++)
			{
				for (int i_verts = 0; i_verts < 3; i_verts++)
				{
					if (_sceneGeneratedLnxNavmesh.Triangles[i_triangles].Verts[i_verts].Index_VisMesh_Vertices > largestMeshVisIndex_sceneGenerated)
					{
						largestMeshVisIndex_sceneGenerated = _sceneGeneratedLnxNavmesh.Triangles[i_triangles].Verts[i_verts].Index_VisMesh_Vertices;
					}
				}
			}

			Debug.Log($"End of search. largest vis mesh index was: '{largestMeshVisIndex_sceneGenerated}'...");
			Assert.AreEqual(_sceneGeneratedLnxNavmesh._Mesh.vertices.Length - 1, largestMeshVisIndex_sceneGenerated);

			for (int i_triangles = 0; i_triangles < _jsonGeneratedLnxNavmesh.Triangles.Length; i_triangles++)
			{
				for (int i_verts = 0; i_verts < 3; i_verts++)
				{
					if (_jsonGeneratedLnxNavmesh.Triangles[i_triangles].Verts[i_verts].Index_VisMesh_Vertices > largestMeshVisINdex_serialized)
					{
						largestMeshVisINdex_serialized = _jsonGeneratedLnxNavmesh.Triangles[i_triangles].Verts[i_verts].Index_VisMesh_Vertices;
					}
				}
			}

			Debug.Log($"End of search over serialized navmesh. largest vis mesh index was: '{largestMeshVisINdex_serialized}'...");

			Assert.AreEqual(_jsonGeneratedLnxNavmesh._Mesh.vertices.Length - 1, largestMeshVisINdex_serialized);

		}

		[Test]
		public void c2_Mesh_Triangles_Array_Is_Expected_Length()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(c2_Mesh_Triangles_Array_Is_Expected_Length)));
			Debug.Log($"scene mesh's vismesh triangles collection null: '{_sceneGeneratedLnxNavmesh._Mesh.triangles == null}'");
			Debug.Log($"data model collection null: '{_sceneGeneratedLnxNavmesh_expectedDataModel._triangulation_areas == null}'");

			Assert.AreEqual( _sceneGeneratedLnxNavmesh_expectedDataModel._triangulation_areas.Length * 3,
				_sceneGeneratedLnxNavmesh._Mesh.triangles.Length );
		}

		[Test]
		public void c3_Mesh_Vertices_Array_Is_Expected_Length()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(c3_Mesh_Vertices_Array_Is_Expected_Length)));
			Debug.Log($"collection null: '{_sceneGeneratedLnxNavmesh._Mesh.vertices == null}'");

			Assert.AreEqual( _sceneGeneratedLnxNavmesh_expectedDataModel._Mesh_Vertices.Length, _sceneGeneratedLnxNavmesh._Mesh.vertices.Length );
		}

		/*
		[Test] //todo: implement
		public void c3_checkMeshNormalsArrayLength()
		{
			Debug.Log($"{nameof(c3_checkMeshNormalsArrayLength)}()---------------------------------");
			Debug.Log($"collection null: '{_sceneGeneratedLnxNavmesh._Mesh.normals == null}'");

			Assert.AreEqual( _sceneGeneratedLnxNavmesh._Mesh.normals.Length, expectedNumberOfUniqueVertsAfterTriangulation );
		}
		*/

		[Test]
		public void c4_All_VisMesh_Verts_Have_Counterpart_At_Position()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(c4_All_VisMesh_Verts_Have_Counterpart_At_Position)));

			for ( int i_uniqueVert = 0; i_uniqueVert < _sceneGeneratedLnxNavmesh._Mesh.vertices.Length; i_uniqueVert++ )
			{
				Debug.Log($"iterating mesh vertex: '{i_uniqueVert}' at pos: '{_sceneGeneratedLnxNavmesh._Mesh.vertices[i_uniqueVert]}'...");
				bool haveFound = false;

				for ( int i_triangles = 0; i_triangles < _sceneGeneratedLnxNavmesh.Triangles.Length; i_triangles++ )
				{
					Debug.Log($"iterating triangle: '{i_triangles}'...");

					for ( int i_vrts = 0; i_vrts < 3; i_vrts++ )
					{
						Debug.Log($"iterating vert: '{i_vrts}'...");

						if( _sceneGeneratedLnxNavmesh.Triangles[i_triangles].GetVertIndextAtPosition(_sceneGeneratedLnxNavmesh._Mesh.vertices[i_uniqueVert]) > -1 )
						{
							haveFound = true;
						}
					}
				}

				Assert.IsTrue( haveFound );
			}
		}
		#endregion

		/*
		[Test]
		public void aX_AllVertsUnique()
		{
			//todo: check that no triangles have verts occupying the same space
		}
		*/

		/*
		[Test]
		public void aX_SharedVertexCoordinates_Tests()
		{
			//TODO: Check that multiple vertices have the expected sharedvertex coordinates...
			Debug.Log($"\nChecking SharedVertexCoordinates...");

		}
		*/

		//TODO: these following tests need to be in their own separate script
		#region D - LNX_Navmesh function Tests---------------------------------------------------------------------------
		[Test]
        public void d1_SamplePosition_Tests() //todo: I think these need to be ran against the serialized navmesh
        {
			Debug.Log($"Creating test object from json...");

			if ( !File.Exists(TDG_Manager.filePath_testData_SamplePosition) )
			{
				Debug.LogError( $"PROBLEM!!!!! file at test path does not exist. Cannot perform test." );
				return;
			}

			#region SETUP TEST OBJECT-----------------------------
			_test_samplePosition = _jsonGeneratedLnxNavmesh.gameObject.AddComponent<TDG_SamplePosition>();

			string jsonString = File.ReadAllText( TDG_Manager.filePath_testData_SamplePosition );

			JsonUtility.FromJsonOverwrite(jsonString, _test_samplePosition);

			Assert.IsNotNull(_test_samplePosition.testPositions);
			Assert.Greater(_test_samplePosition.testPositions.Count, 0);
			#endregion

			Debug.Log($"Now sampling '{_test_samplePosition.testPositions.Count}' test positions...");
			for ( int i = 0; i < _test_samplePosition.testPositions.Count; i++ )
			{
				Debug.Log($"{i}...");
				LNX_ProjectionHit hit = new LNX_ProjectionHit();
				_jsonGeneratedLnxNavmesh.SamplePosition( _test_samplePosition.testPositions[i], out hit, 10f );

				Debug.Log($"expecting '{_test_samplePosition.hitPositions[i]}', hit: '{hit.HitPosition}'");

				//Assert.AreEqual( _test_samplePosition.hitPositions[i], hit.Position ); //got rounding point issue
				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.hitPositions[i].x, hit.HitPosition.x );
				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.hitPositions[i].y, hit.HitPosition.y );
				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.hitPositions[i].z, hit.HitPosition.z );

				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.triCenters[i].x, _jsonGeneratedLnxNavmesh.Triangles[hit.Index_hitTriangle].V_center.x );
				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.triCenters[i].y, _jsonGeneratedLnxNavmesh.Triangles[hit.Index_hitTriangle].V_center.y );
				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.triCenters[i].z, _jsonGeneratedLnxNavmesh.Triangles[hit.Index_hitTriangle].V_center.z );
			}
		}

		[Test]
		public void d2_Test_ClosestOnPerimeter()
		{
			Debug.Log($"Creating test object from json...");
			Debug.Log($"test object path: '{TDG_Manager.filePath_testData_sampleClosestPtOnPerim}'");

			if ( !File.Exists(TDG_Manager.filePath_testData_sampleClosestPtOnPerim) )
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}

			#region SETUP CLOSEST ON PERIMETER TEST -----------------------------
			//_test_closestOnPerimeter = _sceneGeneratedLnxNavmesh.gameObject.AddComponent<TDG_SampleClosestPtOnPerimeter>();
			_test_closestOnPerimeter = _jsonGeneratedLnxNavmesh.gameObject.AddComponent<TDG_SampleClosestPtOnPerimeter>();

			//Debug.Log ( File.Exists(filePath_test_closestOnPerimeter) );
			//Debug.Log( filePath_test_closestOnPerimeter );
			string jsonString = File.ReadAllText(TDG_Manager.filePath_testData_sampleClosestPtOnPerim);

			JsonUtility.FromJsonOverwrite(jsonString, _test_closestOnPerimeter);
			//Debug.Log( _test_samplePosition.problemPositions.Count );

			Assert.IsNotNull(_test_closestOnPerimeter.testPositions);
			Assert.Greater(_test_closestOnPerimeter.testPositions.Count, 0);
			#endregion

			for ( int i = 0; i < _test_closestOnPerimeter.testPositions.Count; i++ )
			{
				Debug.Log($"{i}. expecting: '{_test_closestOnPerimeter.hitPositions[i]}'...");

				LNX_ProjectionHit hit = new LNX_ProjectionHit();

				if ( _jsonGeneratedLnxNavmesh.SamplePosition(_test_closestOnPerimeter.testPositions[i], out hit, 10f) ) //It needs to do this in order to decide which triangle to use...
				{
					Vector3 v_result = _jsonGeneratedLnxNavmesh.Triangles[hit.Index_hitTriangle].ClosestPointOnPerimeter( _test_closestOnPerimeter.testPositions[i] );

					UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_closestOnPerimeter.hitPositions[i].x, v_result.x );
					UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_closestOnPerimeter.hitPositions[i].y, v_result.y );
					UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_closestOnPerimeter.hitPositions[i].z, v_result.z );
				}
			}
		}

		[Test]
		public void d3_Test_ClosestOnPerimeter_triCenters()
		{
			Debug.Log($"{nameof(d3_Test_ClosestOnPerimeter_triCenters)}---------------------------------------------------------");
			Debug.Log($"Sampling '{_test_closestOnPerimeter.testPositions.Count}' test positions at: '{System.DateTime.Now.ToString()}'");

			for (int i = 0; i < _test_closestOnPerimeter.testPositions.Count; i++)
			{
				Debug.Log($"{i}...");

				LNX_ProjectionHit hit = new LNX_ProjectionHit();

				if ( _jsonGeneratedLnxNavmesh.SamplePosition(_test_closestOnPerimeter.testPositions[i], out hit, 10f)) //It needs to do this in order to decide which triangle to use...
				{
					Vector3 v_result = _jsonGeneratedLnxNavmesh.Triangles[hit.Index_hitTriangle].ClosestPointOnPerimeter(_test_closestOnPerimeter.testPositions[i]);

					Debug.Log($"{i}. expecting: '{_test_closestOnPerimeter.hitPositions[i]}', ClosestPointOnPerimeter got: '{v_result}'. " +
						$"close: '{Vector3.Distance(v_result, _test_closestOnPerimeter.hitPositions[i])}'..");

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_closestOnPerimeter.triCenters[i].x, _jsonGeneratedLnxNavmesh.Triangles[hit.Index_hitTriangle].V_center.x);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_closestOnPerimeter.triCenters[i].y, _jsonGeneratedLnxNavmesh.Triangles[hit.Index_hitTriangle].V_center.y);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_closestOnPerimeter.triCenters[i].z, _jsonGeneratedLnxNavmesh.Triangles[hit.Index_hitTriangle].V_center.z);
				}
			}

			Debug.Log($"end of test: '{nameof(d3_Test_ClosestOnPerimeter_triCenters)}'");
		}
		#endregion


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

