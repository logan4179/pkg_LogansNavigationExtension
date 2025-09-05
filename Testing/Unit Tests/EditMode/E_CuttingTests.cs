using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LogansNavigationExtension;
using UnityEngine.AI;
using System.IO;
using System.Linq;

namespace LoganLand.LogansNavmeshExtension.Tests
{
	public class E_CuttingTests
	{
		LNX_NavMesh _serializedLNXNavmesh;

		LNX_MeshManipulator _lnx_meshManipulator;

		[Header("TEST OBJECTS")]
		TDG_Cutting _tdg_cuttingTests;


		#region A - Setup --------------------------------------------------------------------------------
		[Test]
		public void a1_SetupLnxNavmeshReferences()
		{
			GameObject go = GameObject.Find(LNX_UnitTestUtilities.Name_SerializedNavmeshGameobject);

			_serializedLNXNavmesh = go.GetComponent<LNX_NavMesh>();
			Assert.NotNull(_serializedLNXNavmesh);


			_lnx_meshManipulator = go.GetComponent<LNX_MeshManipulator>();
			Assert.NotNull(_serializedLNXNavmesh);

		}

		[Test]
		public void a2_CreateTestObjectsFromJson()
		{
			Debug.Log($"{nameof(a2_CreateTestObjectsFromJson)}()...");

			if ( !File.Exists(TDG_Manager.filePath_testData_cuttingTests))
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}

			//CREATE TEST OBJECT -----------------------------
			_tdg_cuttingTests = _serializedLNXNavmesh.gameObject.AddComponent<TDG_Cutting>();
			string jsonString = File.ReadAllText( TDG_Manager.filePath_testData_cuttingTests );
			JsonUtility.FromJsonOverwrite( jsonString, _tdg_cuttingTests );
			Assert.NotNull( _tdg_cuttingTests );

			Assert.Greater( _tdg_cuttingTests.TestMouseDirections_edge.Count, 0 );

			Debug.Log($"{nameof(_tdg_cuttingTests.TestMouseDirections_edge)} count is '{_tdg_cuttingTests.TestMouseDirections_edge.Count}'. Proceeding...");

		}
		#endregion

		int triCount_before = 0;
		int triCount_after = 0;
		int visMesh_vertsLength_before = 0;
		int visMesh_vertsLength_after = 0;
		[Test]
		public void b1_tri_Collection_Length_Is_Correct_After_Cut()
		{
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b1_tri_Collection_Length_Is_Correct_After_Cut)) );

			_lnx_meshManipulator.ClearSelection(); //just to be sure...
			_lnx_meshManipulator.ChangeSelectMode( LNX_SelectMode.Edges );

			_lnx_meshManipulator.TryPointAtComponentViaDirection(
			_tdg_cuttingTests.TestMousePositions_edge[0], _tdg_cuttingTests.TestMouseDirections_edge[0]);
			Debug.Log($"Trying to grab first edge...");
			_lnx_meshManipulator.TryGrab();

			_lnx_meshManipulator.TryPointAtComponentViaDirection(
			_tdg_cuttingTests.TestMousePositions_edge[1], _tdg_cuttingTests.TestMouseDirections_edge[1]);
			Debug.Log($"Trying to grab second edge...");
			_lnx_meshManipulator.TryGrab( true );

			if ( _lnx_meshManipulator.Edges_currentlySelected.Count != 2 )
			{
				Debug.LogError($"Something's wrong. Edges_currentlySelected count is not 2, so selection attempt failed. Can't succesfully continue test...");
				return;
			}
			else
			{
				Debug.Log($"Succesfully grabbed two edges, proceding...");
			}

			triCount_before = _lnx_meshManipulator._LNX_NavMesh.Triangles.Length;
			Debug.Log($"tri collection length before: '{triCount_before}'...");

			visMesh_vertsLength_before = _lnx_meshManipulator._LNX_NavMesh._VisualizationMesh.vertices.Length;
			_lnx_meshManipulator.TryInsertLoop();

			triCount_after = _lnx_meshManipulator._LNX_NavMesh.Triangles.Length;

			Assert.AreEqual( triCount_before + 2, triCount_after);

			visMesh_vertsLength_after = _lnx_meshManipulator._LNX_NavMesh._VisualizationMesh.vertices.Length;

			Debug.Log($"End of test. Tri collection length after: '{triCount_after}'");
		}

		[Test]
		public void b2_triangles_Collection_Indices_Unbroken_After_Cut()
		{
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b2_triangles_Collection_Indices_Unbroken_After_Cut)) );

			Debug.Log($"checking '{_lnx_meshManipulator._LNX_NavMesh.Triangles.Length}' triangles to make sure their indices are unbroken...");
			for (int i = 0; i < _lnx_meshManipulator._LNX_NavMesh.Triangles.Length; i++)
			{
				Debug.Log($"i: '{i}'...");
				Assert.AreEqual(i, _lnx_meshManipulator._LNX_NavMesh.Triangles[i].Index_inCollection);
			}
		}

		[Test]
		public void b3_wasAdded_Flags_Set_True_On_New_Tris_After_Cut()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b3_wasAdded_Flags_Set_True_On_New_Tris_After_Cut)));

			Debug.Log($"Checking that the last two triangles have their 'wasadded' flags set to true...");
			Assert.IsTrue( _lnx_meshManipulator._LNX_NavMesh.Triangles[_lnx_meshManipulator._LNX_NavMesh.Triangles.Length - 1].WasAddedViaMod );
			Assert.IsTrue( _lnx_meshManipulator._LNX_NavMesh.Triangles[_lnx_meshManipulator._LNX_NavMesh.Triangles.Length - 2].WasAddedViaMod );

		}

		[Test]
		public void b4_All_Edge_Coordinates_Correct_After_Cut()
		{
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b4_All_Edge_Coordinates_Correct_After_Cut)) );

			for ( int i_tris = 0; i_tris < _lnx_meshManipulator._LNX_NavMesh.Triangles.Length; i_tris++ )
			{
				Debug.Log($"iterating tri: '{i_tris}'...");
				for ( int i_edges = 0; i_edges < 3; i_edges++ )
				{
					Debug.Log($"iterating edge: '{i_edges}'...");

					Assert.AreEqual( _lnx_meshManipulator._LNX_NavMesh.Triangles[i_tris].Index_inCollection, 
						_lnx_meshManipulator._LNX_NavMesh.Triangles[i_tris].Edges[i_edges].MyCoordinate.TrianglesIndex );
				}
			}
		}

		[Test]
		public void b5_All_Vert_Coordinates_Correct_After_Cut()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b4_All_Edge_Coordinates_Correct_After_Cut)));

			for (int i_tris = 0; i_tris < _lnx_meshManipulator._LNX_NavMesh.Triangles.Length; i_tris++)
			{
				for (int i_verts = 0; i_verts < 3; i_verts++)
				{
					Assert.AreEqual(_lnx_meshManipulator._LNX_NavMesh.Triangles[i_tris].Index_inCollection,
						_lnx_meshManipulator._LNX_NavMesh.Triangles[i_tris].Verts[i_verts].MyCoordinate.TrianglesIndex);
				}
			}
		}

		#region C - VISUALIZATION MESH -----------------------------------------------------
		[Test]
		public void c1_visMesh_triangles_Length_Is_Correct_After_Cut()
		{
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(c1_visMesh_triangles_Length_Is_Correct_After_Cut)) );

			Debug.Log($"VisMesh triangles length: '{_lnx_meshManipulator._LNX_NavMesh._VisualizationMesh.triangles.Length}'...");

			Assert.AreEqual( triCount_after * 3, _lnx_meshManipulator._LNX_NavMesh._VisualizationMesh.triangles.Length );

			Debug.Log($"");
		}

		[Test]
		public void c2_visMesh_Vertices_Length_Is_Correct_After_Cut()
		{
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(c2_visMesh_Vertices_Length_Is_Correct_After_Cut)) );

			Assert.AreEqual( visMesh_vertsLength_before + 2, _lnx_meshManipulator._LNX_NavMesh._VisualizationMesh.vertices.Length );
		}

		#endregion
	}
}

