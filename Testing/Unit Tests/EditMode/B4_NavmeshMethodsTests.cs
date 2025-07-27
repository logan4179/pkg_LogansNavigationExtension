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
	public class B4_NavmeshMethodsTests
	{
		LNX_NavMesh _serializedLNXNavmesh;

		TDG_SamplePosition _tdg_samplePosition;

		TDG_SampleClosestPtOnPerimeter _tdg_closestOnPerimeter;

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

			#region Sample Position -------------------------------------------------------
			if (!File.Exists(TDG_Manager.filePath_testData_SamplePosition))
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}

			//CREATE TEST OBJECT -----------------------------
			_tdg_samplePosition = _serializedLNXNavmesh.gameObject.AddComponent<TDG_SamplePosition>();
			string jsonString = File.ReadAllText(TDG_Manager.filePath_testData_SamplePosition);
			JsonUtility.FromJsonOverwrite(jsonString, _tdg_samplePosition);
			Assert.NotNull(_tdg_samplePosition);
			#endregion
		}

		[Test]
		public void A3_Ensure_Test_Objects_Are_Valid()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(A3_Ensure_Test_Objects_Are_Valid),
				"Ensures that the objects created for testing have adequate/valid values"
			);

			Debug.Log($"Setting up Sampling tests...");
			#region SETUP SAMPLE POSITION TEST -----------------------------
			if (!File.Exists(TDG_Manager.filePath_testData_SamplePosition))
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}

			_tdg_samplePosition = _serializedLNXNavmesh.gameObject.AddComponent<TDG_SamplePosition>();

			string jsonString = File.ReadAllText(TDG_Manager.filePath_testData_SamplePosition);

			JsonUtility.FromJsonOverwrite(jsonString, _tdg_samplePosition);

			Debug.Log($"Created Sampling test object. Counts: '{_tdg_samplePosition.samplePositions.Count}', " +
				$"'{_tdg_samplePosition.capturedHitPositions.Count}', and '{_tdg_samplePosition.capturedTriCenters.Count}'...");

			Assert.IsNotNull(_tdg_samplePosition.samplePositions);
			Assert.Greater(_tdg_samplePosition.samplePositions.Count, 0);

			Assert.IsNotNull(_tdg_samplePosition.capturedHitPositions);
			Assert.Greater(_tdg_samplePosition.capturedHitPositions.Count, 0);

			Assert.IsNotNull(_tdg_samplePosition.capturedTriCenters);
			Assert.Greater(_tdg_samplePosition.capturedTriCenters.Count, 0);

			Assert.AreEqual(_tdg_samplePosition.samplePositions.Count, _tdg_samplePosition.capturedHitPositions.Count);

			if (!File.Exists(TDG_Manager.filePath_testData_sampleClosestPtOnPerim))
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}
			#endregion

			#region SETUP CLOSEST ON PERIMETER TEST -----------------------------
			if (!File.Exists(TDG_Manager.filePath_testData_sampleClosestPtOnPerim))
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}
			_tdg_closestOnPerimeter = _serializedLNXNavmesh.gameObject.AddComponent<TDG_SampleClosestPtOnPerimeter>();

			jsonString = File.ReadAllText(TDG_Manager.filePath_testData_sampleClosestPtOnPerim);

			JsonUtility.FromJsonOverwrite(jsonString, _tdg_closestOnPerimeter);
			Debug.Log($"Created closestOnPerimeter test object. Counts: '{_tdg_closestOnPerimeter.problemPositions.Count}', ");

			Assert.IsNotNull(_tdg_closestOnPerimeter.problemPositions);
			Assert.Greater(_tdg_closestOnPerimeter.problemPositions.Count, 0);
			#endregion
		}
		#endregion


		#region B) LNX_Navmesh function Tests---------------------------------------------------------------------------
		[Test]
		public void B1_SamplePosition_Tests()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(B1_SamplePosition_Tests),
			"Checks that the LNX_Navmesh.SamplePosition() method works as expected");

			Debug.Log($"Now sampling '{_tdg_samplePosition.samplePositions.Count}' test positions...");
			for (int i = 0; i < _tdg_samplePosition.samplePositions.Count; i++)
			{
				Debug.Log($"{i}...");
				LNX_ProjectionHit hit = new LNX_ProjectionHit();
				_serializedLNXNavmesh.SamplePosition(_tdg_samplePosition.samplePositions[i], out hit, 10f);

				Debug.Log($"expecting '{_tdg_samplePosition.capturedHitPositions[i]}', hit: '{hit.HitPosition}'");

				//Assert.AreEqual( _test_samplePosition.hitPositions[i], hit.Position ); //got rounding point issue
				UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_samplePosition.capturedHitPositions[i].x, hit.HitPosition.x);
				UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_samplePosition.capturedHitPositions[i].y, hit.HitPosition.y);
				UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_samplePosition.capturedHitPositions[i].z, hit.HitPosition.z);

				UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_samplePosition.capturedTriCenters[i].x,
					_serializedLNXNavmesh.Triangles[hit.Index_Hit].V_Center.x);
				UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_samplePosition.capturedTriCenters[i].y,
					_serializedLNXNavmesh.Triangles[hit.Index_Hit].V_Center.y);
				UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_samplePosition.capturedTriCenters[i].z,
					_serializedLNXNavmesh.Triangles[hit.Index_Hit].V_Center.z);
			}
		}

		[Test]
		public void B2_Test_ClosestOnPerimeter()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(B2_Test_ClosestOnPerimeter),
			"Checks that the LNX_Navmesh.SamplePosition() method works as expected");

			for (int i = 0; i < _tdg_closestOnPerimeter.problemPositions.Count; i++)
			{
				Debug.Log($"{i}. expecting: '{_tdg_closestOnPerimeter.capturedPerimeterPositions[i]}'...");

				LNX_ProjectionHit hit = new LNX_ProjectionHit();

				if (_serializedLNXNavmesh.SamplePosition(_tdg_closestOnPerimeter.problemPositions[i], out hit, 10f)) //It needs to do this in order to decide which triangle to use...
				{
					Vector3 v_result = _serializedLNXNavmesh.Triangles[hit.Index_Hit].ClosestPointOnPerimeter(_tdg_closestOnPerimeter.problemPositions[i]);

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_closestOnPerimeter.capturedPerimeterPositions[i].x, v_result.x);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_closestOnPerimeter.capturedPerimeterPositions[i].y, v_result.y);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_closestOnPerimeter.capturedPerimeterPositions[i].z, v_result.z);
				}
			}
		}

		[Test]
		public void B3_Test_ClosestOnPerimeter_triCenters()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(B3_Test_ClosestOnPerimeter_triCenters),
				"Checks that the LNX_Navmesh.SamplePosition() method works as expected");

			for (int i = 0; i < _tdg_closestOnPerimeter.problemPositions.Count; i++)
			{
				Debug.Log($"{i}...");

				LNX_ProjectionHit hit = new LNX_ProjectionHit();

				if (_serializedLNXNavmesh.SamplePosition(_tdg_closestOnPerimeter.problemPositions[i], out hit, 10f)) //It needs to do this in order to decide which triangle to use...
				{
					Vector3 v_result = _serializedLNXNavmesh.Triangles[hit.Index_Hit].ClosestPointOnPerimeter(_tdg_closestOnPerimeter.problemPositions[i]);

					Debug.Log($"{i}. expecting: '{_tdg_closestOnPerimeter.capturedPerimeterPositions[i]}', ClosestPointOnPerimeter got: '{v_result}'. " +
						$"close: '{Vector3.Distance(v_result, _tdg_closestOnPerimeter.capturedPerimeterPositions[i])}'..");

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_closestOnPerimeter.capturedTriCenters[i].x, _serializedLNXNavmesh.Triangles[hit.Index_Hit].V_Center.x);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_closestOnPerimeter.capturedTriCenters[i].y, _serializedLNXNavmesh.Triangles[hit.Index_Hit].V_Center.y);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_closestOnPerimeter.capturedTriCenters[i].z, _serializedLNXNavmesh.Triangles[hit.Index_Hit].V_Center.z);
				}
			}
		}
		#endregion
	}
}