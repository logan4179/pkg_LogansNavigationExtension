using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LogansNavigationExtension;
using UnityEngine.AI;
using System.IO;


namespace LoganLand.LogansNavmeshExtension.Tests
{
    public class A2_AtomicMethodsTests
    {
		LNX_NavMesh _serializedLNXNavmesh;

		LNX_MeshManipulator _lnx_meshManipulator;

		[Header("TEST OBJECTS")]
		TDG_Projecting _tdg_projecting;

		#region A - Setup --------------------------------------------------------------------------------
		[Test]
		public void a1_SetupObjects()
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
			LNX_UnitTestUtilities.LogTestStart(nameof(a2_CreateTestObjectsFromJson),
			"Creates the objects necessary for this test suite");

			#region projectingg -------------------------------------------------------
			if ( !File.Exists(TDG_Manager.filePath_testData_projectingTests) )
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}

			//CREATE TEST OBJECT -----------------------------
			_tdg_projecting = _serializedLNXNavmesh.gameObject.AddComponent<TDG_Projecting>();
			string jsonString = File.ReadAllText( TDG_Manager.filePath_testData_projectingTests );
			JsonUtility.FromJsonOverwrite( jsonString, _tdg_projecting );
			Assert.NotNull( _tdg_projecting );
			#endregion
		}

		[Test]
		public void A3_Ensure_Test_Objects_Are_Valid()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(A3_Ensure_Test_Objects_Are_Valid),
				"Ensures that the objects created for testing have adequate/valid values"
			);

			int commonCount = _tdg_projecting.StartPositions_EdgeProjecting.Count;

			Assert.Greater( commonCount, 0 );
			Assert.AreEqual(_tdg_projecting.EndPositions_EdgeProjecting.Count, commonCount );
			Assert.AreEqual(_tdg_projecting.CapturedTriCenters_EdgeProjecting.Count, commonCount );
			Assert.AreEqual(_tdg_projecting.CapturedResults_EdgeProjecting.Count, commonCount );
		}
		#endregion

		#region B - EDGE PROJECTING -------------------------------------------------------------------------------
		[Test]
		public void b1_Triangle_IsProjectedPointOnEdge()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(b1_Triangle_IsProjectedPointOnEdge),
				$"Runs through multiple triangles and tests the results of the 'IsProjectedPointOnEdge' method ");

			Debug.Log($"Now iterating through '{_tdg_projecting.StartPositions_EdgeProjecting.Count}' data points...");
			for ( int i = 0; i < _tdg_projecting.StartPositions_EdgeProjecting.Count; i++ )
			{
				Debug.Log($"i: '{i}'...");

				//first find the correct triangle...
				LNX_Triangle tri = null;
				for ( int i_tris = 0; i_tris < _serializedLNXNavmesh.Triangles.Length; i_tris++ ) 
				{
					if( Vector3.Distance( _serializedLNXNavmesh.Triangles[i_tris].V_center, _tdg_projecting.CapturedTriCenters_EdgeProjecting[i]) < 0.02f )
					{
						tri = _serializedLNXNavmesh.Triangles[i_tris];
						break;
					}
				}
				
				Assert.AreEqual( _tdg_projecting.CapturedResults_EdgeProjecting[i],
					tri.IsProjectedPointOnEdge(_tdg_projecting.StartPositions_EdgeProjecting[i], 
					_tdg_projecting.EndPositions_EdgeProjecting[i],
					_tdg_projecting.CapturedEdgeIndices[i]
					) 
				);
			}
		}
		#endregion
	}
}