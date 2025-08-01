using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LogansNavigationExtension;
using UnityEngine.AI;
using System.IO;
using System.ComponentModel;
using UnityEditor.SearchService;


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
		LNX_NavMesh _jsonGeneratedLnxNavmesh; //note: originally I was going to use this to save the _mesh and triangulation
		// collection lengths instead of testing a hard-coded value, but those things don't seem to get serialized to JSON. 
		// Leaving this here in case I think of something else to do with it...

		LNX_MeshManipulator _sceneGeneratedMeshManipulator, _serializedMeshManipulator;

        //TDG_SamplePosition _tdg_samplePosition;

		//TDG_SampleClosestPtOnPerimeter _test_closestOnPerimeter;


		#region A - Setup ---------------------------------------------------------------------------
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

			Debug.Log($"Created serialized navmesh object from data file. Tris: '{_jsonGeneratedLnxNavmesh.Triangles.Length}'\n" +
				$"test vert vismesh index: '{_jsonGeneratedLnxNavmesh.Triangles[13].Verts[1].Index_VisMesh_triangles}', " +
				$"'{_jsonGeneratedLnxNavmesh.Triangles[13].Verts[1].Index_VisMesh_Vertices}'");

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

		[Test]
		public void a2_CreateAndSetUpTestObjectsInTheScene()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(a2_CreateAndSetUpTestObjectsInTheScene),
				$"Creates test objects, then asserts that they have the correct values"
			);


		}
		#endregion

		#region B) CHECK NAVMESH -------------------------------------------------------------------------------------------
		[Test]
		public void b1_Check_That_Bounds_Have_Expected_Values()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(b1_Check_That_Bounds_Have_Expected_Values),
				$"Asserts that LNX_Navmesh.V_BoundsCenter and V_Bounds[] have the correct values"
			);

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
		#endregion

		#region C) CHECK TRIANGLES ------------------------------------------------------------------------------------------
		[Test]
        public void C1_Make_Sure_NavMesh_Has_Collection_Lengths_Consistent_With_Triangulation()
        {
			LNX_UnitTestUtilities.LogTestStart(nameof(C1_Make_Sure_NavMesh_Has_Collection_Lengths_Consistent_With_Triangulation),
				"Asserts that NavMeshTriangulation.areas.Length and the length of the scene generated lnxNavmesh.Triangles array are equal"
			);
			
            Assert.AreEqual( _nmTriangulation.areas.Length, _sceneGeneratedLnxNavmesh.Triangles.Length );
		}

		[Test]
		public void C2_Check_That_Triangle_Indices_Properties_Are_Not_Negative_One()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(C2_Check_That_Triangle_Indices_Properties_Are_Not_Negative_One)));

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
		public void C3_All_Triangle_Relationships_Array_Lengths_Equal_Master_Triangles_Array_Length()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(C3_All_Triangle_Relationships_Array_Lengths_Equal_Master_Triangles_Array_Length)));

			Debug.Log($"\nChecking relationships...");
			Debug.Log($"Running through '{_sceneGeneratedLnxNavmesh.Triangles.Length}' triangles to check relationships..");
			for (int i = 0; i < _sceneGeneratedLnxNavmesh.Triangles.Length; i++)
			{
				Debug.Log($"checking tri '{i}'...");

				Assert.AreEqual(_sceneGeneratedLnxNavmesh.Triangles[i].Relationships.Length, _sceneGeneratedLnxNavmesh.Triangles.Length);
			}
		}

		[Test]
		public void C4_All_Triangle_AdjacentTriIndices_Array_Length_Are_Greater_Than_Zero()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(C4_All_Triangle_AdjacentTriIndices_Array_Length_Are_Greater_Than_Zero)));

			Debug.Log($"\nChecking relationships...");
			Debug.Log($"Running through '{_sceneGeneratedLnxNavmesh.Triangles.Length}' triangles to check relationships..");
			for (int i = 0; i < _sceneGeneratedLnxNavmesh.Triangles.Length; i++)
			{
				Debug.Log($"checking tri '{i}'...");
				Assert.Greater(_sceneGeneratedLnxNavmesh.Triangles[i].AdjacentTriIndices.Length, 0);
			}
		}


		[Test]
		public void C5_Check_Triangle_Normals()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(C5_Check_Triangle_Normals),
				""
			);

			//Note: right now I'm kinda using a cheap hack by just checking if there are more than 5 tris without normals. In the future, 
			//I might want to 

			int numberOfNullNormals = 0;

			Debug.Log($"iterating through triangles to check their normals...");
			for ( int i = 0; i < _sceneGeneratedLnxNavmesh.Triangles.Length; i++ )
			{
				Debug.Log($"i: '{i}'...");
				if( _sceneGeneratedLnxNavmesh.Triangles[i].v_sampledNormal == Vector3.zero )
				{
					Debug.Log("found null normal...");
					numberOfNullNormals++;
				}
			}

			Debug.Log($"finished iterating. Found '{numberOfNullNormals}' abberant normals...");

			Assert.Less( numberOfNullNormals, 6 );
		}
		#endregion

		#region D) CHECK VERTICES -----------------------------------------------------------------------------------------
		[Test]
		public void D1_Vert_Positions_Are_Kosher()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(D1_Vert_Positions_Are_Kosher)));

			Debug.Log($"\nThis Test checks that all vertices belonging to a given triangle have unique positional values.\n");

			for ( int i_tris = 0; i_tris < _sceneGeneratedLnxNavmesh.Triangles.Length; i_tris++ )
			{
				Debug.Log($"iterating tri: '{i_tris}'...");

				bool foundSame = false;
				if( _sceneGeneratedLnxNavmesh.Triangles[i_tris].Verts[0].V_Position == _sceneGeneratedLnxNavmesh.Triangles[i_tris].Verts[1].V_Position ||
					_sceneGeneratedLnxNavmesh.Triangles[i_tris].Verts[0].V_Position == _sceneGeneratedLnxNavmesh.Triangles[i_tris].Verts[2].V_Position ||
					_sceneGeneratedLnxNavmesh.Triangles[i_tris].Verts[1].V_Position == _sceneGeneratedLnxNavmesh.Triangles[i_tris].Verts[2].V_Position
				)
				{
					foundSame = true;
				}

				Assert.IsFalse( foundSame );
			}
		}

		[Test]
		public void D2_Vert_Angles_Are_Kosher()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(D2_Vert_Angles_Are_Kosher)));

			Debug.Log($"\nThis Test checks that all vertices have non-zero angle values.\n");

			for ( int i_tris = 0; i_tris < _sceneGeneratedLnxNavmesh.Triangles.Length; i_tris++ )
			{
				Debug.Log($"iterating tri: '{i_tris}'...");

				for ( int i_verts = 0; i_verts < 3; i_verts++ )
				{
					Debug.Log($"iterating vert: '{i_verts}', at '{_sceneGeneratedLnxNavmesh.Triangles[i_tris].Verts[i_verts].V_Position}', with angle '{_sceneGeneratedLnxNavmesh.Triangles[i_tris].Verts[i_verts].AngleAtBend}'. " +
						$"flattened: '{_sceneGeneratedLnxNavmesh.Triangles[i_tris].Verts[i_verts].AngleAtBend_flattened}'...");

					Debug.Log($"Asserting that iterated vert has valid angles...");

					Assert.Greater( _sceneGeneratedLnxNavmesh.Triangles[i_tris].Verts[i_verts].AngleAtBend, 0f );
					Assert.Greater( _sceneGeneratedLnxNavmesh.Triangles[i_tris].Verts[i_verts].AngleAtBend_flattened, 0f);
				}
			}
		}

		[Test]
		public void D3_Vert_Coordinates_Are_Kosher()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(D3_Vert_Coordinates_Are_Kosher)));

			Debug.Log($"\nThis Test checks that all vertices have 'MyCoordinate' values that match their actual positions within the collections that " +
				$"they're stored in. This also inadvertantly ensures that there are no duplicate coordinates, and that the coordinates are unbroken.\n");

			for ( int i_tris = 0; i_tris < _sceneGeneratedLnxNavmesh.Triangles.Length; i_tris++ )
			{
				Debug.Log($"iterating tri: '{i_tris}'...");

				for ( int i_verts = 0; i_verts < 3; i_verts++ )
				{
					Debug.Log($"iterating vert: '{i_verts}'...");

					Debug.Log($"Asserting that iterated vert with coordinate: '{_sceneGeneratedLnxNavmesh.Triangles[i_tris].Verts[i_verts].MyCoordinate}' " +
						$"matches position in collection...");
					Assert.AreEqual( i_tris, _sceneGeneratedLnxNavmesh.Triangles[i_tris].Verts[i_verts].MyCoordinate.TrianglesIndex );
					Assert.AreEqual( i_verts, _sceneGeneratedLnxNavmesh.Triangles[i_tris].Verts[i_verts].MyCoordinate.ComponentIndex );
				}
			}
		}

		[Test]
		public void D4_Vert_Vismesh_Index_Properties_Are_Within_Bounds()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(D4_Vert_Vismesh_Index_Properties_Are_Within_Bounds)));

			for (int i = 0; i < _sceneGeneratedLnxNavmesh.Triangles.Length; i++)
			{
				Debug.Log($"{i}...");

				for (int i_verts = 0; i_verts < 3; i_verts++)
				{
					Debug.Log($"checking indices of vert[{i_verts}]...");
					Debug.Log($"Verts[{i_verts}].MeshIndex_triangles: '{_sceneGeneratedLnxNavmesh.Triangles[i].Verts[i_verts].Index_VisMesh_triangles}'");
					Assert.Greater(_sceneGeneratedLnxNavmesh.Triangles[i].Verts[i_verts].Index_VisMesh_triangles, -1);
					Assert.Less(_sceneGeneratedLnxNavmesh.Triangles[i].Verts[i_verts].Index_VisMesh_triangles, _sceneGeneratedLnxNavmesh._Mesh.triangles.Length);

					Debug.Log($"Verts[{i_verts}].MeshIndex_vertices: '{_sceneGeneratedLnxNavmesh.Triangles[i].Verts[i_verts].Index_VisMesh_Vertices}'");
					Assert.Greater(_sceneGeneratedLnxNavmesh.Triangles[i].Verts[i_verts].Index_VisMesh_Vertices, -1);
					Assert.Less(_sceneGeneratedLnxNavmesh.Triangles[i].Verts[i_verts].Index_VisMesh_Vertices, _sceneGeneratedLnxNavmesh._Mesh.vertices.Length);
				}
			}
		}

		[Test]
		public void D5_Vert_SharedVertexCoordinates_Collections_Are_Correct()
		{
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(D5_Vert_SharedVertexCoordinates_Collections_Are_Correct)) );

			Debug.Log($"\nThis Test ensures that all vertices have a sharedvertexcoordinates arrays that have the correct entries.\n");

			LNX_Vertex currentVert = null;
			for ( int i_tris = 0; i_tris < _sceneGeneratedLnxNavmesh.Triangles.Length; i_tris++ )
			{
				for ( int i_verts = 0; i_verts < 3; i_verts++ )
				{
					currentVert = _sceneGeneratedLnxNavmesh.Triangles[i_tris].Verts[i_verts];
					Debug.Log($"iterating currentVert: '{currentVert}'..................");

					List<LNX_Vertex> locatedSharedSpaceVerts = new List<LNX_Vertex>();

					for ( int i_tris_inner = 0; i_tris_inner < _sceneGeneratedLnxNavmesh.Triangles.Length; i_tris_inner++ )
					{
						for ( int i_verts_inner = 0; i_verts_inner < 3; i_verts_inner++ )
						{
							LNX_Vertex compareVert = _sceneGeneratedLnxNavmesh.Triangles[i_tris_inner].Verts[i_verts_inner];

							if( compareVert.V_Position == currentVert.V_Position && compareVert.MyCoordinate != currentVert.MyCoordinate )
							{
								Debug.Log($"Found a shared vert at: '{compareVert.MyCoordinate}'. Adding to list...");
								locatedSharedSpaceVerts.Add( compareVert );
							}
						}
					}

					Debug.Log($"finally located total of '{locatedSharedSpaceVerts.Count}' sharedspaceverts. Asserting this is equal to what was logged into " +
						$"current vert's collection...");
					Assert.AreEqual( currentVert.SharedVertexCoordinates.Length, locatedSharedSpaceVerts.Count );

					Debug.Log($"checking that all the '{currentVert.SharedVertexCoordinates.Length}' sharedverts match those" +
						$" that were just located...");
					for ( int i_sharedVertCoords = 0; i_sharedVertCoords < currentVert.SharedVertexCoordinates.Length; i_sharedVertCoords++ )
					{
						bool haveLocated = false;
						for( int i_justLocatedSharedVerts = 0; i_justLocatedSharedVerts < locatedSharedSpaceVerts.Count; i_justLocatedSharedVerts++ )
						{
							if( currentVert.SharedVertexCoordinates[i_sharedVertCoords] == locatedSharedSpaceVerts[i_justLocatedSharedVerts].MyCoordinate )
							{
								haveLocated = true;
								break;
							}
						}

						Assert.IsTrue( haveLocated );
					}
				}
			}
		}


		#endregion

		#region E) CHECK EDGES -------------------------------------------------------------------------------------------

		[Test]
		public void E1_Check_vCross_Gets_Calculated_Correctly()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(E1_Check_vCross_Gets_Calculated_Correctly),
				"Checks that the Edge.V_cross variable gets calculated correctly on instantiation."
			);

			Debug.Log($"iterating through '{_sceneGeneratedLnxNavmesh.Triangles.Length}' triangles...");
			for ( int i_tris = 0; i_tris < _sceneGeneratedLnxNavmesh.Triangles.Length; i_tris++ )
			{
				Debug.Log($"i: '{i_tris}'. current normal: '{_sceneGeneratedLnxNavmesh.Triangles[i_tris].v_sampledNormal}'...");

				if( _sceneGeneratedLnxNavmesh.Triangles[i_tris].v_sampledNormal == Vector3.zero )
				{
					Debug.Log($"tri normal was 0. Continuing...");
					continue;
				}

				for ( int i_edge = 0; i_edge < 3; i_edge++ ) 
				{
					Assert.AreNotEqual( Vector3.zero, _sceneGeneratedLnxNavmesh.Triangles[i_tris].Edges[i_edge].v_cross );

				}
			}
		}
		#endregion

		#region F) CHECK VISUALIZATION MESH ------------------------------------------------------------------------------------------
		public static int largestMeshVisIndex_sceneGenerated = 0; //public and static, because I need to cache this and use it in another test file...
		public static int largestMeshVisINdex_serialized = 0; //public and static, because I need to cache this and use it in another test file...
		[Test]
		public void F1_Greatest_VisMeshIndex_Is_Same_As_Mesh_Vertices_Array_Length()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(F1_Greatest_VisMeshIndex_Is_Same_As_Mesh_Vertices_Array_Length)));

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
		public void F2_Mesh_Triangles_Array_Is_Expected_Length()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(F2_Mesh_Triangles_Array_Is_Expected_Length)));
			Debug.Log($"scene mesh's vismesh triangles collection null: '{_sceneGeneratedLnxNavmesh._Mesh.triangles == null}'");
			Debug.Log($"data model collection null: '{_sceneGeneratedLnxNavmesh_expectedDataModel._triangulation_areas == null}'");

			Assert.AreEqual( _sceneGeneratedLnxNavmesh_expectedDataModel._triangulation_areas.Length * 3,
				_sceneGeneratedLnxNavmesh._Mesh.triangles.Length );
		}

		[Test]
		public void F3_Mesh_Vertices_Array_Is_Expected_Length()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(F3_Mesh_Vertices_Array_Is_Expected_Length)));
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
		public void F4_All_VisMesh_Verts_Have_Counterpart_At_Position()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(F4_All_VisMesh_Verts_Have_Counterpart_At_Position)));

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

