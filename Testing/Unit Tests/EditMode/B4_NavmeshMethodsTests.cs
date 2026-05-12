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
		LNX_NavMesh _testGeneratedLnxNavmesh;

		TDG_SamplePosition _tdg_samplePosition;

		TDG_SampleClosestPtOnPerimeter _tdg_sampleClosestPtOnPerimeter;

		TDG_Raycasting _tdg_raycasting;

		#region A - Setup --------------------------------------------------------------------------------
		[Test]
		public void a1_SetupObjects()
		{
			GameObject go = GameObject.Find(LNX_UnitTestUtilities.Name_SerializedNavmeshGameobject);

			if (go == null)
			{
				Debug.LogWarning($"Couldn't find serialized navmesh in scene. Making anew...");
				go = new GameObject();
				go.name = LNX_UnitTestUtilities.Name_GeneratedNavmeshGameobject; //so that other test scripts can find this object.
				_testGeneratedLnxNavmesh = go.AddComponent<LNX_NavMesh>();
				Assert.NotNull(_testGeneratedLnxNavmesh);
				Debug.Log($"scene-generated navmesh created, now calculating triangulation...");

				//todo: dws the following line...
				//_testGeneratedLnxNavmesh.LayerMaskName = "lr_EnvSolid"; //not necessary, but just to be sure...
				_testGeneratedLnxNavmesh.MyLayerMask = LayerMask.GetMask("lr_EnvSolid");

				_testGeneratedLnxNavmesh.CalculateTriangulation();
				Assert.NotNull(_testGeneratedLnxNavmesh._VisualizationMesh);
				Debug.Log($"mesh visual. {nameof(_testGeneratedLnxNavmesh._VisualizationMesh.vertices)} length: '{_testGeneratedLnxNavmesh._VisualizationMesh.vertices.Length}', " +
					$"{nameof(_testGeneratedLnxNavmesh._VisualizationMesh.triangles)} length: '{_testGeneratedLnxNavmesh._VisualizationMesh.triangles.Length}, " +
					$"{nameof(_testGeneratedLnxNavmesh._VisualizationMesh.normals)} length: '{_testGeneratedLnxNavmesh._VisualizationMesh.normals.Length}, ");

				Debug.Log($"Generated navmesh bounds information...");
				Debug.Log($"scene generated navmesh bounds size: '{_testGeneratedLnxNavmesh.V_BoundsSize}'");
				Debug.Log($"scene generated navmesh bounds center: '{_testGeneratedLnxNavmesh.V_BoundsCenter}'");

				Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestSectionEndString, "set up scene-generated navmesh"));
			}

			_testGeneratedLnxNavmesh = go.GetComponent<LNX_NavMesh>();

			Assert.NotNull(_testGeneratedLnxNavmesh);
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

			string jsonString = File.ReadAllText(TDG_Manager.filePath_testData_SamplePosition);
			Assert.IsNotEmpty(jsonString);
			Debug.Log($"Read json string for tdg_SamplePosition. string length: '{jsonString.Length}'...");

			_tdg_samplePosition = _testGeneratedLnxNavmesh.gameObject.AddComponent<TDG_SamplePosition>();
			_tdg_samplePosition.AmInUnitTest = true;
			JsonUtility.FromJsonOverwrite(jsonString, _tdg_samplePosition);
			Assert.NotNull(_tdg_samplePosition);

			Debug.Log($"Created Sampling test object for tdg_SamplePosition");
			#endregion

			#region CLOSEST ON PERIMETER -----------------------------
			Debug.Log("\n2) SAMPLE CLOSEST POINT ON TRIANGLE PERIMETER SETUP....");
			if (!File.Exists(TDG_Manager.filePath_testData_sampleClosestPtOnPerim))
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}
			_tdg_sampleClosestPtOnPerimeter = _testGeneratedLnxNavmesh.gameObject.AddComponent<TDG_SampleClosestPtOnPerimeter>();
			_tdg_sampleClosestPtOnPerimeter.AmInUnitTest = true;

			jsonString = File.ReadAllText(TDG_Manager.filePath_testData_sampleClosestPtOnPerim);

			JsonUtility.FromJsonOverwrite(jsonString, _tdg_sampleClosestPtOnPerimeter);
			Debug.Log($"Created {nameof(_tdg_sampleClosestPtOnPerimeter)} test object. Asserting necessary collections " +
				$"are not null for testing...");

			#endregion

			#region Raycast -------------------------------------------------------
			if ( !File.Exists(TDG_Manager.filePath_testData_Raycasting) )
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}

			//CREATE TEST OBJECT -----------------------------
			_tdg_raycasting = _testGeneratedLnxNavmesh.gameObject.AddComponent<TDG_Raycasting>();
			_tdg_raycasting.AmInUnitTest = true;

			jsonString = File.ReadAllText( TDG_Manager.filePath_testData_Raycasting );
			JsonUtility.FromJsonOverwrite(jsonString, _tdg_raycasting);
			Assert.NotNull(_tdg_raycasting);
			#endregion
		}

		[Test]
		public void A3_Ensure_Test_Objects_Are_Valid()
		{
			LNX_UnitTestUtilities.LogTestStart(
				nameof(A3_Ensure_Test_Objects_Are_Valid),
				"Ensures that the objects created for testing have adequate/valid values"
			);

			#region Sample Position--------------------------------------------------------------
			Assert.NotNull(_tdg_samplePosition._dataCapture);
			Assert.NotNull( _tdg_samplePosition._dataCapture.VectorCaptureLists ); //just tried to run this test after a long time. 
			//test fails here. It almost looks like maybe I never serialized this one with the new data capture thing, but I"m 
			//not sure...

			Assert.Greater(_tdg_samplePosition._dataCapture.VectorCaptureLists.Count, 0);
			Debug.Log($"tdg_SamplePosition");

			Debug.Log($"Created Sampling test object. Counts: '{_tdg_samplePosition._dataCapture.VectorCaptureLists[0].vectors.Count}', " +
				$"'{_tdg_samplePosition._dataCapture.VectorCaptureLists[1].vectors.Count}', and " +
				$"'{_tdg_samplePosition._dataCapture.VectorCaptureLists[2].vectors.Count}'...");

			Assert.IsNotNull(_tdg_samplePosition._dataCapture.VectorCaptureLists[0].vectors); //sample positions
			Assert.Greater(_tdg_samplePosition._dataCapture.VectorCaptureLists[0].vectors.Count, 0);

			Assert.IsNotNull(_tdg_samplePosition._dataCapture.VectorCaptureLists[1].vectors); //captured hit positions
			Assert.Greater(_tdg_samplePosition._dataCapture.VectorCaptureLists[1].vectors.Count, 0);

			Assert.IsNotNull(_tdg_samplePosition._dataCapture.VectorCaptureLists[2].vectors); //captured tri centers
			Assert.Greater(_tdg_samplePosition._dataCapture.VectorCaptureLists[2].vectors.Count, 0);

			Assert.AreEqual(_tdg_samplePosition._dataCapture.VectorCaptureLists[0].vectors.Count, 
				_tdg_samplePosition._dataCapture.VectorCaptureLists[1].vectors.Count);

			if (!File.Exists(TDG_Manager.filePath_testData_sampleClosestPtOnPerim))
			{
				Debug.LogError($"PROBLEM!!!!! file at test path does not exist. Cannot perform test.");
				return;
			}
			#endregion

			#region SETUP CLOSEST ON PERIMETER TEST -----------------------------
			Debug.Log("\n2) SAMPLE CLOSEST POINT ON TRIANGLE PERIMETER SETUP....");
			Debug.Log($"Created {nameof(_tdg_sampleClosestPtOnPerimeter)} test object. Asserting necessary collections " +
				$"are not null for testing...");

			Assert.IsNotNull(_tdg_sampleClosestPtOnPerimeter._dataCapture.VectorCaptureLists[0].vectors); //sample from 
			Assert.IsNotNull(_tdg_sampleClosestPtOnPerimeter._dataCapture.VectorCaptureLists[1].vectors); //captured perimeter
			Assert.IsNotNull(_tdg_sampleClosestPtOnPerimeter._dataCapture.VectorCaptureLists[2].vectors); //captured tri centers

			Debug.Log( $"Collection Counts: \n" +
				$"samplefrom: '{_tdg_sampleClosestPtOnPerimeter._dataCapture.VectorCaptureLists[0].vectors.Count}'\n" +
				$"perimPositions: '{_tdg_sampleClosestPtOnPerimeter._dataCapture.VectorCaptureLists[1].vectors.Count}'\n" +
				$"tri centers: '{_tdg_sampleClosestPtOnPerimeter._dataCapture.VectorCaptureLists[2].vectors.Count}'\n" +
				$"");

			Debug.Log("Asserting collection counts are above 0...");
			Assert.Greater(_tdg_sampleClosestPtOnPerimeter._dataCapture.VectorCaptureLists[0].vectors.Count, 0);
			Assert.Greater(_tdg_sampleClosestPtOnPerimeter._dataCapture.VectorCaptureLists[1].vectors.Count, 0);
			Assert.Greater(_tdg_sampleClosestPtOnPerimeter._dataCapture.VectorCaptureLists[2].vectors.Count, 0);

			#endregion

			#region Raycasting --------------------------------------------
			Debug.Log($"Checking raycast data...");

			Assert.Greater(_tdg_raycasting.CapturedStartPositions.Count, 0);
			int commongCount = _tdg_raycasting.CapturedStartPositions.Count;

			Assert.AreEqual( commongCount, _tdg_raycasting.CapturedEndPositions.Count );
			Assert.AreEqual( commongCount, _tdg_raycasting.CapturedRaycastResults.Count );

			#endregion

		}
		#endregion


		#region B) LNX_Navmesh function Tests---------------------------------------------------------------------------
		[Test]
		public void B1_SamplePosition_Tests()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(B1_SamplePosition_Tests),
			"Checks that the LNX_Navmesh.SamplePosition() method works as expected");

			Debug.Log($"Now sampling '{_tdg_samplePosition._dataCapture.VectorCaptureLists[0].vectors.Count}' test positions...");
			for (int i = 0; i < _tdg_samplePosition._dataCapture.VectorCaptureLists[0].vectors.Count; i++)
			{
				Debug.Log($"{i}...");
				LNX_NavmeshHit hit = new LNX_NavmeshHit();
				_testGeneratedLnxNavmesh.SamplePosition(_tdg_samplePosition._dataCapture.VectorCaptureLists[0].vectors[i], out hit, 10f);

				Debug.Log($"expecting '{_tdg_samplePosition._dataCapture.VectorCaptureLists[0].vectors[i]}', hit: '{hit.Position}'");

				//Assert.AreEqual( _test_samplePosition.hitPositions[i], hit.Position ); //got rounding point issue
				UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_samplePosition._dataCapture.VectorCaptureLists[0].vectors[i].x, hit.Position.x);
				UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_samplePosition._dataCapture.VectorCaptureLists[0].vectors[i].y, hit.Position.y);
				UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_samplePosition._dataCapture.VectorCaptureLists[0].vectors[i].z, hit.Position.z);

				UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_samplePosition._dataCapture.VectorCaptureLists[2].vectors[i].x,
					_testGeneratedLnxNavmesh.Triangles[hit.TriIndex].V_Center.x);
				UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_samplePosition._dataCapture.VectorCaptureLists[2].vectors[i].y,
					_testGeneratedLnxNavmesh.Triangles[hit.TriIndex].V_Center.y);
				UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_samplePosition._dataCapture.VectorCaptureLists[2].vectors[i].z,
					_testGeneratedLnxNavmesh.Triangles[hit.TriIndex].V_Center.z);
			}
		}

		[Test]
		public void B2_Test_ClosestOnPerimeter()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(B2_Test_ClosestOnPerimeter),
			"Checks that the LNX_Navmesh.SamplePosition() method works as expected");

			//todo: redo this now that I've gotten rid of the problem positions thing...
			/*
			for (int i = 0; i < _tdg_sampleClosestPtOnPerimeter.problemPositions.Count; i++)
			{
				Debug.Log($"{i}. expecting: '{_tdg_sampleClosestPtOnPerimeter.capturedPerimeterPositions[i]}'...");

				LNX_NavmeshHit hit = new LNX_NavmeshHit();

				if (_serializedLNXNavmesh.SamplePosition(_tdg_sampleClosestPtOnPerimeter.problemPositions[i], out hit, 10f)) //It needs to do this in order to decide which triangle to use...
				{
					Vector3 v_result = _serializedLNXNavmesh.Triangles[hit.TriIndex].ClosestPointOnPerimeter(_tdg_sampleClosestPtOnPerimeter.problemPositions[i]);

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_sampleClosestPtOnPerimeter.capturedPerimeterPositions[i].x, v_result.x);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_sampleClosestPtOnPerimeter.capturedPerimeterPositions[i].y, v_result.y);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_sampleClosestPtOnPerimeter.capturedPerimeterPositions[i].z, v_result.z);
				}
			}
			*/
		}

		[Test]
		public void B3_Test_ClosestOnPerimeter_triCenters()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(B3_Test_ClosestOnPerimeter_triCenters),
				"Checks that the LNX_Navmesh.SamplePosition() method works as expected");

			//todo: redo this now that I've gotten rid of the problem positions thing...
			/*
			for (int i = 0; i < _tdg_sampleClosestPtOnPerimeter.problemPositions.Count; i++)
			{
				Debug.Log($"{i}...");

				LNX_NavmeshHit hit = new LNX_NavmeshHit();

				if (_serializedLNXNavmesh.SamplePosition(_tdg_sampleClosestPtOnPerimeter.problemPositions[i], out hit, 10f)) //It needs to do this in order to decide which triangle to use...
				{
					Vector3 v_result = _serializedLNXNavmesh.Triangles[hit.TriIndex].ClosestPointOnPerimeter(_tdg_sampleClosestPtOnPerimeter.problemPositions[i]);

					Debug.Log($"{i}. expecting: '{_tdg_sampleClosestPtOnPerimeter.capturedPerimeterPositions[i]}', ClosestPointOnPerimeter got: '{v_result}'. " +
						$"close: '{Vector3.Distance(v_result, _tdg_sampleClosestPtOnPerimeter.capturedPerimeterPositions[i])}'..");

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_sampleClosestPtOnPerimeter.capturedTriCenters[i].x, 
						_serializedLNXNavmesh.Triangles[hit.TriIndex].V_Center.x);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_sampleClosestPtOnPerimeter.capturedTriCenters[i].y, 
						_serializedLNXNavmesh.Triangles[hit.TriIndex].V_Center.y);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_sampleClosestPtOnPerimeter.capturedTriCenters[i].z, 
						_serializedLNXNavmesh.Triangles[hit.TriIndex].V_Center.z);
				}
			}
			*/
		}

		[Test]
		public void B4_Raycasting()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(B4_Raycasting),
				"Checks that the LNX_Navmesh.Raycast() method works as expected");
			//todo: will need to also test paths from raycasts...

			Debug.Log($"Checking '{_tdg_raycasting.CapturedStartPositions.Count}' data points...");
			for ( int i = 0; i < _tdg_raycasting.CapturedStartPositions.Count; i++ )
			{
				Debug.Log($"{i}...");

				bool rslt = _testGeneratedLnxNavmesh.Raycast
				(
					_tdg_raycasting.CapturedStartPositions[i], _tdg_raycasting.CapturedEndPositions[i], 3f
				);

				Debug.Log($"operation result was '{rslt}'. Asserting equality against captured result '{_tdg_raycasting.CapturedRaycastResults[i]}'...");

				Assert.AreEqual( _tdg_raycasting.CapturedRaycastResults[i], rslt );
			}
		}
		#endregion
	}
}