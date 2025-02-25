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
    public class LNXEditModeTests
    {
        NavMeshTriangulation _nmTriangulation;

        string filePath_lnx_navmesh = $"{Directory.GetCurrentDirectory()}\\Packages\\LogansNavigationExtension\\Testing\\Unit Tests\\Test Data\\nm_B.json";
        LNX_NavMesh _nm;

        string filePath_test_SamplePosition = $"{Directory.GetCurrentDirectory()}\\Packages\\LogansNavigationExtension\\Testing\\Unit Tests\\Test Data\\SamplePosition_data_A.json";
        Test_SamplePosition _test_samplePosition;

		string filePath_test_closestOnPerimeter = $"{Directory.GetCurrentDirectory()}\\Packages\\LogansNavigationExtension\\Testing\\Unit Tests\\Test Data\\closestOnPerimeter_data_A.json";
		LNX_TestClosestOnPerimeter _test_closestOnPerimeter;

		[Test]
		public void a_SetUpFromJson()
        {
			#region SETUP NAVMESH ---------------------------------------------
			GameObject go = new GameObject();
			_nm = go.AddComponent<LNX_NavMesh>();
            string jsonString = "";

			_nmTriangulation = NavMesh.CalculateTriangulation();
            Debug.Log( $"{nameof(NavMesh.CalculateTriangulation)} calculated '{_nmTriangulation.vertices}' vertices, '{_nmTriangulation.areas}' areas, and '{_nmTriangulation.indices}' indices." );

			//Note: decided not to do the following lines which create a navmesh from json because the navmesh is setup by 
			//a navmeshtriangulation object, which needs to be tested...
			//Debug.Log (File.Exists(filePath_lnx_navmesh) );
			// Debug.Log(filePath_lnx_navmesh);
			//jsonString = File.ReadAllText(filePath_lnx_navmesh);
			//Debug.Log( nmJsonString );
			//nm = JsonUtility.FromJson<LNX_NavMesh>( nmJsonString ); //note: this won't work bc FromJson() doesn't support deserializing a monobehavior object
			//JsonUtility.FromJsonOverwrite(jsonString, nm); //note: this apparently does work to deserialize a monobehaviour...
			//Debug.Log(nm.Triangles.Length);

			#endregion

			#region SETUP SAMPLE POSITION TEST -----------------------------
			_test_samplePosition = go.AddComponent<Test_SamplePosition>();
			//Debug.Log ( File.Exists(filePath_test_SamplePosition) );
		    //Debug.Log( filePath_test_SamplePosition );
			jsonString = File.ReadAllText(filePath_test_SamplePosition);

			JsonUtility.FromJsonOverwrite( jsonString, _test_samplePosition );
            //Debug.Log( _test_samplePosition.problemPositions.Count );

            Assert.IsNotNull( _test_samplePosition.testPositions );
            Assert.Greater( _test_samplePosition.testPositions.Count, 0 );
			#endregion

			#region SETUP CLOSEST ON PERIMETER TEST -----------------------------
			_test_closestOnPerimeter = go.AddComponent<LNX_TestClosestOnPerimeter>();
			//Debug.Log ( File.Exists(filePath_test_closestOnPerimeter) );
			//Debug.Log( filePath_test_closestOnPerimeter );
			jsonString = File.ReadAllText( filePath_test_closestOnPerimeter );

			JsonUtility.FromJsonOverwrite( jsonString, _test_closestOnPerimeter );
			//Debug.Log( _test_samplePosition.problemPositions.Count );

			Assert.IsNotNull( _test_closestOnPerimeter.testPositions );
			Assert.Greater( _test_closestOnPerimeter.testPositions.Count, 0 );
			#endregion
		}

		[Test]
        public void b_FetchTriangulation_Tests()
        {
			_nm.LayerMaskName = "lr_EnvSolid"; //not necessary, but just to be sure...
			_nm.FetchTriangulation();

            Assert.AreEqual( _nmTriangulation.areas.Length, _nm.Triangles.Length );
        }

        
		[Test]
        public void c_SamplePosition_Tests()
        {
            Debug.Log($"Sampling '{_test_samplePosition.testPositions.Count}' test positions at: '{System.DateTime.Now.ToString()}'");

			for ( int i = 0; i < _test_samplePosition.testPositions.Count; i++ )
			{
				Debug.Log($"{i}...");
				LNX_ProjectionHit hit = new LNX_ProjectionHit();
				_nm.SamplePosition( _test_samplePosition.testPositions[i], out hit, 10f );
				//Assert.AreEqual( _test_samplePosition.hitPositions[i], hit.Position ); //got rounding point issue
				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.hitPositions[i].x, hit.HitPosition.x );
				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.hitPositions[i].y, hit.HitPosition.y );
				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.hitPositions[i].z, hit.HitPosition.z );

				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.triCenters[i].x, _nm.Triangles[hit.Index_intersectedTri].V_center.x );
				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.triCenters[i].y, _nm.Triangles[hit.Index_intersectedTri].V_center.y );
				UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_samplePosition.triCenters[i].z, _nm.Triangles[hit.Index_intersectedTri].V_center.z );
			}
		}

		[Test]
		public void d_Test_ClosestOnPerimeter()
		{
			Debug.Log($"Sampling '{_test_samplePosition.testPositions.Count}' test positions at: '{System.DateTime.Now.ToString()}'");

			for ( int i = 0; i < _test_closestOnPerimeter.testPositions.Count; i++ )
			{
				Debug.Log($"{i}. expecting: '{_test_closestOnPerimeter.resultPositions[i]}'...");

				LNX_ProjectionHit hit = new LNX_ProjectionHit();

				if ( _nm.SamplePosition(_test_closestOnPerimeter.testPositions[i], out hit, 10f) ) //It needs to do this in order to decide which triangle to use...
				{
					Vector3 v_result = _nm.Triangles[hit.Index_intersectedTri].ClosestPointOnPerimeter( _test_closestOnPerimeter.testPositions[i] );

					UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_closestOnPerimeter.resultPositions[i].x, v_result.x );
					UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_closestOnPerimeter.resultPositions[i].y, v_result.y );
					UnityEngine.Assertions.Assert.AreApproximatelyEqual( _test_closestOnPerimeter.resultPositions[i].z, v_result.z );
				}
			}
		}

		[Test]
		public void e_Test_ClosestOnPerimeter_triCenters()
		{
			Debug.Log($"Sampling '{_test_samplePosition.testPositions.Count}' test positions at: '{System.DateTime.Now.ToString()}'");

			for (int i = 0; i < _test_closestOnPerimeter.testPositions.Count; i++)
			{
				Debug.Log($"{i}. expecting: '{_test_closestOnPerimeter.resultPositions[i]}'...");

				LNX_ProjectionHit hit = new LNX_ProjectionHit();

				if (_nm.SamplePosition(_test_closestOnPerimeter.testPositions[i], out hit, 10f)) //It needs to do this in order to decide which triangle to use...
				{
					Vector3 v_result = _nm.Triangles[hit.Index_intersectedTri].ClosestPointOnPerimeter(_test_closestOnPerimeter.testPositions[i]);

					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_closestOnPerimeter.triCenters[i].x, _nm.Triangles[hit.Index_intersectedTri].V_center.x);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_closestOnPerimeter.triCenters[i].y, _nm.Triangles[hit.Index_intersectedTri].V_center.y);
					UnityEngine.Assertions.Assert.AreApproximatelyEqual(_test_closestOnPerimeter.triCenters[i].z, _nm.Triangles[hit.Index_intersectedTri].V_center.z);
				}
			}
		}

		/*
		[Test]
		public void x_Test_SelectFace()
		{
			//make tests showing that I can select faces at expected mouse positions, and that I 
			//cannot select them at other expected mouse positions
		}
		*/

		/*
		[Test]
		public void x_Test_SelectEdge()
		{
			//make tests showing that I can select edges at expected mouse positions, and that I 
			//cannot select them at other expected mouse positions
		}
		*/

		/*
		[Test]
		public void x_Test_SelectVertex()
		{
			//make tests showing that I can select verts at expected mouse positions, and that I 
			//cannot select them at other expected mouse positions
		}
		*/

		/*
		 * 
		[Test]
		public void x_Test_SelectCutEdge()
		{
			//make tests showing that I can and cannot cut edges in various scenarios
		}
		*/


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

