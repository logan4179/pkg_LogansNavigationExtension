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
    public class D_DeletingAndAddingTests
    {
		LNX_NavMesh _serializedLNXNavmesh;

		LNX_MeshManipulator _lnx_meshManipulator;

		[Header("TEST OBJECTS")]
		TDG_DeleteTests _tdg_deleteTests;


		#region A - Setup --------------------------------------------------------------------------------
		[Test]
		public void a1_SetupLnxNavmeshReferences()
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

			if ( !File.Exists(TDG_Manager.filePath_testData_deleteTests) )
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}

			//CREATE TEST OBJECT -----------------------------
			_tdg_deleteTests = _serializedLNXNavmesh.gameObject.AddComponent<TDG_DeleteTests>();
			string jsonString = File.ReadAllText( TDG_Manager.filePath_testData_deleteTests );
			JsonUtility.FromJsonOverwrite( jsonString, _tdg_deleteTests );
			Assert.NotNull( _tdg_deleteTests );

			Assert.Greater( _tdg_deleteTests.TestMousePositions_face.Count, 0 );

			Debug.Log($"{nameof(_tdg_deleteTests.TestMousePositions_face)} count is '{_tdg_deleteTests.TestMousePositions_face.Count}'. Proceeding...");
			
		}
		#endregion

		int mainListCount_before;
		int meshTrianglesCount_before;
		int meshVerticesCount_before;
		[Test]
		public void b1_TriangleCountIsOneLessAfterDeletion()
		{
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b1_TriangleCountIsOneLessAfterDeletion)) );

			#region SETUP TEST CONDITIONS---------------------------------------------
			Debug.Log($"mesh manipulator null: '{_lnx_meshManipulator == null}'..");
			_lnx_meshManipulator.ClearSelection(); //just to be sure...
			Debug.Log($"attempting to select first json serialized tri...");

			_lnx_meshManipulator.ChangeSelectMode( LNX_SelectMode.Faces );

			_lnx_meshManipulator.TryPointAtComponentViaDirection(
				_tdg_deleteTests.TestMousePositions_face[0], _tdg_deleteTests.TestDirections_face[0] );

			Debug.Log($"Trying to grab first triangle...");
			_lnx_meshManipulator.TryGrab();

			if( _lnx_meshManipulator.Index_TriLastSelected <= -1 )
			{
				Debug.LogError($"Something's wrong. index_triLastSelected is -1, so selection attempt failed. Can't succesfully continue test...");
				return;
			}
			else
			{
				Debug.Log($"Succesfully grabbed triangle '{_lnx_meshManipulator.Index_TriLastSelected}'...");
			}
			#endregion

			mainListCount_before = _serializedLNXNavmesh.Triangles.Length;
			meshTrianglesCount_before = _serializedLNXNavmesh._VisualizationMesh.triangles.Length;
			meshVerticesCount_before = _serializedLNXNavmesh._VisualizationMesh.vertices.Length;
			Debug.Log( $"Triangles.Length before: '{mainListCount_before}', mesh triangles count before: '{meshTrianglesCount_before}', " +
				$"mesh vertices count before: '{meshVerticesCount_before}'" );

			Debug.Log($"Currently have '{_lnx_meshManipulator.indices_selectedTris.Count}' tris selected (there should only be one). Attempting delete on tri index: '{_lnx_meshManipulator.indices_selectedTris[0]}'...");
			_lnx_meshManipulator.DeleteSelectedTriangles();

			Debug.Log($"tris length: '{_serializedLNXNavmesh.Triangles.Length}'");
			_serializedLNXNavmesh.ReconstructVisualizationMesh();

			Debug.Log( $"Triangles.Length after: '{_serializedLNXNavmesh.Triangles.Length}, mesh " +
				$"triangles count after: '{_serializedLNXNavmesh._VisualizationMesh.triangles.Length}', " +
				$"mesh vertices count after: '{_serializedLNXNavmesh._VisualizationMesh.vertices.Length}'");

			Assert.AreEqual( mainListCount_before - 1, _serializedLNXNavmesh.Triangles.Length );

		}

		[Test]
		public void b2_VisMeshVertsAndTrisAreCorrectCountAfterDeletion()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b2_VisMeshVertsAndTrisAreCorrectCountAfterDeletion)));

			Assert.AreEqual( _serializedLNXNavmesh._VisualizationMesh.triangles.Length, meshTrianglesCount_before - 3 );
			Assert.AreEqual( _serializedLNXNavmesh._VisualizationMesh.vertices.Length, meshVerticesCount_before );
		}

		[Test]
		public void b3_TriangleMainIndicesAreUnbrokenAfterFirstDeletion()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b3_TriangleMainIndicesAreUnbrokenAfterFirstDeletion)));

			Debug.Log($"checking '{_serializedLNXNavmesh.Triangles.Length}' triangles to make sure their indices are unbroken...");
			for ( int i = 0; i < _serializedLNXNavmesh.Triangles.Length; i++ )
			{
				Debug.Log($"i: '{i}'...");
				Assert.AreEqual( i, _serializedLNXNavmesh.Triangles[i].Index_inCollection );
			}
		}

	}
}

