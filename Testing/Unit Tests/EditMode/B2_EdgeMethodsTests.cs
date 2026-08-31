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
    public class B2_EdgeMethodsTests 
    {
		LNX_NavMeshSurface _serializedLNXNavmesh;

		//LNX_MeshManipulator _lnx_meshManipulator;

		[Header("TEST OBJECTS")]
		TDG_DoesPositionLieOnEdge _tdg_doesPositionLieOnEdge;

		#region A - Setup --------------------------------------------------------------------------------
		[Test]
		public void a1_SetupObjects()
		{
			GameObject go = GameObject.Find(LNX_UnitTestUtilities.Name_SerializedNavmeshGameobject);

			_serializedLNXNavmesh = go.GetComponent<LNX_NavMeshSurface>();
			Assert.NotNull(_serializedLNXNavmesh);


			//_lnx_meshManipulator = go.GetComponent<LNX_MeshManipulator>();
			//Assert.NotNull(_lnx_meshManipulator);

		}

		[Test]
		public void a2_CreateTestObjectsFromJson()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(a2_CreateTestObjectsFromJson),
			"Creates the objects necessary for this test suite");

			#region Edge.DoesPositionLieOnEdge() -------------------------------------------------------
			if ( File.Exists(TDG_Manager.filePath_testData_doesPositionLieOnEdge) )
			{
				Debug.Log($"File exists for {nameof(TDG_DoesPositionLieOnEdge)}. Building test objects...");
			}
			else
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist for {nameof(TDG_DoesPositionLieOnEdge)}. Cannot perform test.");
				return;
			}

			_tdg_doesPositionLieOnEdge = _serializedLNXNavmesh.gameObject.AddComponent<TDG_DoesPositionLieOnEdge>();
			_tdg_doesPositionLieOnEdge.AmInUnitTest = true;

			string jsonString = File.ReadAllText( TDG_Manager.filePath_testData_doesPositionLieOnEdge );
			JsonUtility.FromJsonOverwrite( jsonString, _tdg_doesPositionLieOnEdge );
			Debug.Log($"Created object from JSON. Asserting object is not null...");
			Assert.NotNull( _tdg_doesPositionLieOnEdge );
			#endregion

		}

		[Test]
		public void A3_Ensure_Test_Objects_Are_Valid()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(A3_Ensure_Test_Objects_Are_Valid),
				"Ensures that the objects created for testing have adequate/valid values"
			);

			#region Edge.DoesPositionLieOnEdge() -------------------------------------------------------
			Assert.Greater( _tdg_doesPositionLieOnEdge.CapturedPositions.Count, 0 );
			int commonCollectionCount = _tdg_doesPositionLieOnEdge.CapturedPositions.Count;

			Assert.AreEqual(
				commonCollectionCount,
				_tdg_doesPositionLieOnEdge.CapturedResults.Count
			);

			Assert.AreEqual(
				commonCollectionCount,
				_tdg_doesPositionLieOnEdge.CapturedTriCenters.Count
			);

			Assert.AreEqual(
				commonCollectionCount,
				_tdg_doesPositionLieOnEdge.CapturedEdgeCenters.Count
			);
			#endregion

		}
		#endregion

		#region B - EDGE METHODS -----------------------------------------------------------------
		[Test]
		public void B1_DoesPositionLieOnEdge()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(B1_DoesPositionLieOnEdge),
				$"Tests that method LNX_Edge.{nameof(LNX_Edge.DoesPositionLieOnEdge)}() works as intended."
			);

			Debug.Log( $"Now testing '{_tdg_doesPositionLieOnEdge.CapturedPositions.Count}' data points..." );
			for ( int i = 0; i < _tdg_doesPositionLieOnEdge.CapturedPositions.Count; i++ )
			{
				Debug.Log($"i: '{i}'...");
				LNX_Triangle tri = _serializedLNXNavmesh.GetTriangle(_tdg_doesPositionLieOnEdge.CapturedTriCenters[i]);
				LNX_Edge edge = tri.GetEdge( _tdg_doesPositionLieOnEdge.CapturedEdgeCenters[i] );

				bool rslt = edge.DoesPositionLieOnEdge(
					_tdg_doesPositionLieOnEdge.CapturedPositions[i]
				);

				Assert.AreEqual( _tdg_doesPositionLieOnEdge.CapturedResults[i], rslt );
			}
		}
		#endregion
	}
}