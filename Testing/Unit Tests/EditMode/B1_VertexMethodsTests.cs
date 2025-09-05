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
	public class B1_VertexMethodsTests
	{
		LNX_NavMesh _serializedLNXNavmesh;

		//LNX_MeshManipulator _lnx_meshManipulator;

		[Header("TEST OBJECTS")]
		TDG_IsInCenterSweep _tdg_isInCenterSweep;

		#region A - Setup --------------------------------------------------------------------------------
		[Test]
		public void a1_SetupObjects()
		{
			GameObject go = GameObject.Find(LNX_UnitTestUtilities.Name_SerializedNavmeshGameobject);

			_serializedLNXNavmesh = go.GetComponent<LNX_NavMesh>();
			Assert.NotNull(_serializedLNXNavmesh);
		}

		[Test]
		public void a2_CreateTestObjectsFromJson()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(a2_CreateTestObjectsFromJson),
			"Creates the objects necessary for this test suite");

			#region IsInCenterSweep------------------------------
			if ( File.Exists(TDG_Manager.filePath_testData_isInCenterSweep) )
			{
				Debug.Log($"File exists. Continuing...");
			}
			else
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}

			_tdg_isInCenterSweep = _serializedLNXNavmesh.gameObject.AddComponent<TDG_IsInCenterSweep>();
			string jsonString = File.ReadAllText( TDG_Manager.filePath_testData_isInCenterSweep );
			JsonUtility.FromJsonOverwrite( jsonString, _tdg_isInCenterSweep );
			Assert.NotNull( _tdg_isInCenterSweep );
			#endregion
		}

		[Test]
		public void A3_Ensure_Test_Objects_Are_Valid()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(A3_Ensure_Test_Objects_Are_Valid),
				"Ensures that the objects created for testing have adequate/valid values"
			);

			#region IsInCenterSweep------------------------------
			Debug.Log($"Now checking that {nameof(TDG_IsInCenterSweep)} object test data is valid...");
			//todo: Implement...
			Assert.Greater(_tdg_isInCenterSweep.CapturedStartPositions.Count, 0);
			int commonCount = _tdg_isInCenterSweep.CapturedStartPositions.Count;

			Assert.AreEqual(commonCount, _tdg_isInCenterSweep.CapturedTriCenterPositions.Count);

			Assert.AreEqual( commonCount, _tdg_isInCenterSweep.Results_Vert0.Count );
			Assert.AreEqual(commonCount, _tdg_isInCenterSweep.CapturedVertPositions_vert0.Count);

			Assert.AreEqual(commonCount, _tdg_isInCenterSweep.Results_Vert1.Count);
			Assert.AreEqual(commonCount, _tdg_isInCenterSweep.CapturedVertPositions_vert1.Count);

			Assert.AreEqual(commonCount, _tdg_isInCenterSweep.Results_Vert2.Count);
			Assert.AreEqual(commonCount, _tdg_isInCenterSweep.CapturedVertPositions_vert2.Count);

			#endregion


		}
		#endregion

		#region B - PROJECTING -------------------------------------------------------------------------------

		[Test]
		public void b1_IsInCenterSweep()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(b1_IsInCenterSweep),
				$"Tests functionality of {nameof(LNX_Vertex.IsInCenterSweep)}() method");

			//todo: Implement...
			for ( int i = 0; i < _tdg_isInCenterSweep.CapturedStartPositions.Count; i++ )
			{
				Debug.Log($"i: '{i}'...");

				LNX_Triangle tri = _serializedLNXNavmesh.GetTriangle( _tdg_isInCenterSweep.CapturedTriCenterPositions[i] );

				Debug.Log("vert0...");
				LNX_Vertex vert0 = tri.GetVertexAtOriginalPosition(_tdg_isInCenterSweep.CapturedVertPositions_vert0[i] );
				bool rslt = vert0.IsInCenterSweep( _tdg_isInCenterSweep.CapturedStartPositions[i] );
				Assert.AreEqual(_tdg_isInCenterSweep.Results_Vert0[i], rslt );

				Debug.Log("vert1...");
				LNX_Vertex vert1 = tri.GetVertexAtOriginalPosition(_tdg_isInCenterSweep.CapturedVertPositions_vert1[i]);
				rslt = vert1.IsInCenterSweep(_tdg_isInCenterSweep.CapturedStartPositions[i]);
				Assert.AreEqual(_tdg_isInCenterSweep.Results_Vert1[i], rslt);

				Debug.Log("vert2...");
				LNX_Vertex vert2 = tri.GetVertexAtOriginalPosition(_tdg_isInCenterSweep.CapturedVertPositions_vert2[i]);
				rslt = vert2.IsInCenterSweep(_tdg_isInCenterSweep.CapturedStartPositions[i]);
				Assert.AreEqual(_tdg_isInCenterSweep.Results_Vert2[i], rslt);
			}



			/*
			Debug.Log($"Now iterating through '{_tdg_projectThroughToPerimeter.CapturedStartPositions.Count}' data points...");

			for (int i = 0; i < _tdg_projectThroughToPerimeter.CapturedStartPositions.Count; i++)
			{
				Debug.Log($"i: '{i}'...");
				LNX_Triangle tri = _serializedLNXNavmesh.GetTriangle(_tdg_projectThroughToPerimeter.CapturedTriCenters[i]);
				LNX_Edge edge = tri.GetEdge(_tdg_projectThroughToPerimeter.CapturedEdgeMidPoints[i]);
				LNX_ProjectionHit hit = LNX_ProjectionHit.None;

				Debug.Log($"projecting through to perimeter...");
				hit = tri.ProjectThroughToPerimeter(
					_tdg_projectThroughToPerimeter.CapturedStartPositions[i],
					_tdg_projectThroughToPerimeter.CapturedEndPositions[i]
				);
				Debug.Log($"projected through. hit position: '{hit.HitPosition}'. Captured hit position is: " +
					$"'{_tdg_projectThroughToPerimeter.CapturedProjectionPoints[i]}'." +
					$"Now asserting value is as expected...");

				//Assert.AreEqual( _tdg_projectThroughToPerimeter.CapturedProjectionPoints[i], hit.HitPosition );

				UnityEngine.Assertions.Assert.AreApproximatelyEqual(
					_tdg_projectThroughToPerimeter.CapturedProjectionPoints[i].x,
					hit.HitPosition.x
				);

				UnityEngine.Assertions.Assert.AreApproximatelyEqual(
					_tdg_projectThroughToPerimeter.CapturedProjectionPoints[i].y,
					hit.HitPosition.y
				);

				UnityEngine.Assertions.Assert.AreApproximatelyEqual(
					_tdg_projectThroughToPerimeter.CapturedProjectionPoints[i].z,
					hit.HitPosition.z
				);
			}
			*/
		}
		#endregion
	}
}