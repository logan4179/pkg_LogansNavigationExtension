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
		LNX_NavMeshSurface _testGeneratedLnxNavmesh;

		TDG_SamplePosition _tdg_samplePosition;

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
				_testGeneratedLnxNavmesh = go.AddComponent<LNX_NavMeshSurface>();
				Assert.NotNull(_testGeneratedLnxNavmesh);
				Debug.Log($"scene-generated navmesh created, now calculating triangulation...");

				//todo: dws the following line...
				//_testGeneratedLnxNavmesh.LayerMaskName = "lr_EnvSolid"; //not necessary, but just to be sure...
				_testGeneratedLnxNavmesh.MyLayerMask = LayerMask.GetMask("lr_EnvSolid");

				_testGeneratedLnxNavmesh.CreateFromSceneTriangulation();
				Assert.NotNull(_testGeneratedLnxNavmesh._VisualizationMesh);
				Debug.Log($"mesh visual. {nameof(_testGeneratedLnxNavmesh._VisualizationMesh.vertices)} length: '{_testGeneratedLnxNavmesh._VisualizationMesh.vertices.Length}', " +
					$"{nameof(_testGeneratedLnxNavmesh._VisualizationMesh.triangles)} length: '{_testGeneratedLnxNavmesh._VisualizationMesh.triangles.Length}, " +
					$"{nameof(_testGeneratedLnxNavmesh._VisualizationMesh.normals)} length: '{_testGeneratedLnxNavmesh._VisualizationMesh.normals.Length}, ");

				Debug.Log($"Generated navmesh bounds information...");
				Debug.Log($"scene generated navmesh bounds size: '{_testGeneratedLnxNavmesh.V_BoundsSize}'");
				Debug.Log($"scene generated navmesh bounds center: '{_testGeneratedLnxNavmesh.V_BoundsCenter}'");

				Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestSectionEndString, "set up scene-generated navmesh"));
			}

			_testGeneratedLnxNavmesh = go.GetComponent<LNX_NavMeshSurface>();

			Assert.NotNull(_testGeneratedLnxNavmesh);
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
					_testGeneratedLnxNavmesh.Triangles[hit.TriangleIndex].V_Center.x);
				UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_samplePosition._dataCapture.VectorCaptureLists[2].vectors[i].y,
					_testGeneratedLnxNavmesh.Triangles[hit.TriangleIndex].V_Center.y);
				UnityEngine.Assertions.Assert.AreApproximatelyEqual(_tdg_samplePosition._dataCapture.VectorCaptureLists[2].vectors[i].z,
					_testGeneratedLnxNavmesh.Triangles[hit.TriangleIndex].V_Center.z);
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
		public void B4_Raycasting()
		{
			LNX_UnitTestUtilities.LogTestStart(nameof(B4_Raycasting),
				"Checks that the LNX_Navmesh.Raycast() method works as expected");
			//todo: will need to also test paths from raycasts...

			Debug.Log($"Checking '{_tdg_raycasting._dataCapture.VectorCaptureLists[0].vectors.Count}' data points...");
			for ( int i = 0; i < _tdg_raycasting._dataCapture.VectorCaptureLists[0].vectors.Count; i++ )
			{
				Debug.Log($"{i}...");

				bool rslt = _testGeneratedLnxNavmesh.Raycast
				(
					_tdg_raycasting._dataCapture.VectorCaptureLists[0].vectors[i],
					_tdg_raycasting._dataCapture.VectorCaptureLists[1].vectors[1], 3f
				);

				Debug.Log($"operation result was '{rslt}'. Asserting equality against captured result " +
					$"'{_tdg_raycasting._dataCapture.BooleanCaptureList.booleans[i]}'...");

				Assert.AreEqual( _tdg_raycasting._dataCapture.BooleanCaptureList.booleans[i], rslt );
			}
		}
		#endregion
	}
}