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
    public class B3_TriangleMethods_tests
    {
		LNX_NavMesh _serializedLNXNavmesh;

		LNX_MeshManipulator _lnx_meshManipulator;

		[Header("TEST OBJECTS")]
		TDG_ProjectThroughToPerimeter _tdg_projectThroughToPerimeter;

		#region A - Setup --------------------------------------------------------------------------------
		[Test]
		public void a1_SetupObjects()
		{
			GameObject go = GameObject.Find(LNX_UnitTestUtilities.Name_SerializedNavmeshGameobject);

			_serializedLNXNavmesh = go.GetComponent<LNX_NavMesh>();
			Assert.NotNull(_serializedLNXNavmesh);


			_lnx_meshManipulator = go.GetComponent<LNX_MeshManipulator>();
			Assert.NotNull(_lnx_meshManipulator);

		}

		[Test]
		public void a2_CreateTestObjectsFromJson()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(a2_CreateTestObjectsFromJson),
			"Creates the objects necessary for this test suite");

			if ( !File.Exists(TDG_Manager.filePath_testData_projectThroughToPerimeter) )
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}

			//-----------------------------
			_tdg_projectThroughToPerimeter = _serializedLNXNavmesh.gameObject.AddComponent<TDG_ProjectThroughToPerimeter>();
			string jsonString = File.ReadAllText( TDG_Manager.filePath_testData_projectThroughToPerimeter );
			JsonUtility.FromJsonOverwrite( jsonString, _tdg_projectThroughToPerimeter );
			Assert.NotNull( _tdg_projectThroughToPerimeter );
		}

		[Test]
		public void A3_Ensure_Test_Objects_Are_Valid()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(A3_Ensure_Test_Objects_Are_Valid),
				"Ensures that the objects created for testing have adequate/valid values"
			);

			Debug.Log($"Now checking that {nameof(TDG_ProjectThroughToPerimeter)} object is valid...");
			Assert.Greater( _tdg_projectThroughToPerimeter.CapturedStartPositions.Count, 0 );
			int commonCount = _tdg_projectThroughToPerimeter.CapturedStartPositions.Count;

			Assert.AreEqual( commonCount, _tdg_projectThroughToPerimeter.CapturedEndPositions.Count );
			Assert.AreEqual(commonCount, _tdg_projectThroughToPerimeter.CapturedTriCenters.Count);
			Assert.AreEqual(commonCount, _tdg_projectThroughToPerimeter.CapturedEdgeMidPoints.Count);
			Assert.AreEqual(commonCount, _tdg_projectThroughToPerimeter.CapturedProjectionPoints.Count);

		}
		#endregion

		#region B - PROJECTING -------------------------------------------------------------------------------

		[Test]
		public void b1_Triangle_ProjectThroughToPerimeter()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(b1_Triangle_ProjectThroughToPerimeter),
				$"Runs through multiple triangles and tests the results of the 'ProjectThroughToPerimeter' method ");

			Debug.Log($"Now iterating through '{_tdg_projectThroughToPerimeter.CapturedStartPositions.Count}' data points...");

			for ( int i = 0; i < _tdg_projectThroughToPerimeter.CapturedStartPositions.Count; i++ )
			{
				Debug.Log($"i: '{i}'...");
				LNX_Triangle tri = _serializedLNXNavmesh.GetTriangle( _tdg_projectThroughToPerimeter.CapturedTriCenters[i] );
				LNX_Edge edge = tri.GetEdge( _tdg_projectThroughToPerimeter.CapturedEdgeMidPoints[i] );
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
		}
		#endregion
	}
}