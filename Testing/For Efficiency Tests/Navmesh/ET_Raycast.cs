using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class ET_Raycast : ET_Base
    {
		public List<Vector3> SampleStartPositions_onMesh = new List<Vector3>();
		public List<Vector3> SampleEndPositions_onMesh = new List<Vector3>();

		public List<Vector3> SampleStartPositions_offMesh = new List<Vector3>();
		public List<Vector3> SampleEndPositions_offMesh = new List<Vector3>();


		[ContextMenu("z call RunTest()")]
		public override void RunTests()
		{
			//original
			/*
			float TotalTestsTime_OnMesh_originalState = 0.5827368f;
			float TotalTestsTime_OnMesh_withPath_originalState = 1.4132262f;

			float TotalTestsTime_OffMesh_originalState = 0.0822393f;
			float TotalTestsTime_OffMesh_withPath_originalState = 0.0777550f;

			float AverageOperationTime_OnMesh_originalState = 0.5827368f;
			float AverageOperationTime_OnMesh_withPath_originalState = 1.4132262f;

			float AverageOperationTime_OffMesh_originalState = 0.0822393f;
			float AverageOperationTime_OffMesh_withPath_originalState = 0.077755f;
			*/

			//most recent
			float TotalTestsTime_OnMesh_MostRecentState = 0.1880214f;
			float TotalTestsTime_OnMesh_withPath_MostRecentState = 0.1950448f;

			float TotalTestsTime_OffMesh_MostRecentState = float.MinValue;
			float TotalTestsTime_OffMesh_withPath_MostRecentState = 0.0685856f;

			float AverageOperationTime_OnMesh_MostRecentState = 0.1880214f;
			float AverageOperationTime_OnMesh_withPath_MostRecentState = 0.1950448f;

			float AverageOperationTime_OffMesh_MostRecentState = float.MinValue;
			float AverageOperationTime_OffMesh_withPath_MostRecentState = 0.0685856f;


			base.RunTests();
			DateTime dt_loopEnd;

			#region ON MESH ----------------------------------------------
			double total = 0;
			LNX_Path outPath;

			DateTime dt_loopstart = DateTime.Now;

			for ( int i = 0; i < SampleStartPositions_onMesh.Count; i++ )
			{
				//dt_loopstart = DateTime.Now;  //Note: I think I should generally comment this out when gathering data so it doesn't effect the results

				bool rslt = _navmesh.Raycast(SampleStartPositions_onMesh[i], SampleEndPositions_onMesh[i], 5f, true );
				//total += DateTime.Now.Subtract(dt_loopstart).TotalMilliseconds; //Note: I think I should generally comment this out when gathering data so it doesn't effect the results

				//Debug.Log($"iteration: '{i}' took '{DateTime.Now.Subtract(dt_loopstart)}'..."); //Note: I think I should generally comment this out when gathering data so it doesn't effect the results
			}
			dt_loopEnd = DateTime.Now;

			SayLoopReportString($"On Mesh Finished report---", dt_loopstart, dt_loopEnd, SampleStartPositions_onMesh.Count,
				TotalTestsTime_OnMesh_MostRecentState, AverageOperationTime_OnMesh_MostRecentState
			);
			#endregion

			#region ON MESH WITH PATH----------------------------------------------
			dt_loopstart = DateTime.Now;

			for (int i = 0; i < SampleStartPositions_onMesh.Count; i++)
			{
				//dt_loopstart = DateTime.Now;  //Note: I think I should generally comment this out when gathering data so it doesn't effect the results

				bool rslt = _navmesh.Raycast( SampleStartPositions_onMesh[i], SampleEndPositions_onMesh[i], 5f, out outPath, true );
				//total += DateTime.Now.Subtract(dt_loopstart).TotalMilliseconds; //Note: I think I should generally comment this out when gathering data so it doesn't effect the results

				//Debug.Log($"iteration: '{i}' took '{DateTime.Now.Subtract(dt_loopstart)}'..."); //Note: I think I should generally comment this out when gathering data so it doesn't effect the results
			}
			dt_loopEnd = DateTime.Now;

			SayLoopReportString($"On Mesh with path Finished report---", dt_loopstart, dt_loopEnd, SampleStartPositions_onMesh.Count,
				TotalTestsTime_OnMesh_withPath_MostRecentState, AverageOperationTime_OnMesh_withPath_MostRecentState
			);
			#endregion

			#region OFF MESH ----------------------------------------------
			dt_loopstart = DateTime.Now;
			total = 0;

			for (int i = 0; i < SampleStartPositions_offMesh.Count; i++)
			{
				//dt_loopstart = DateTime.Now;  //Note: I think I should generally comment this out when gathering data so it doesn't effect the results

				bool rslt = _navmesh.Raycast(SampleStartPositions_offMesh[i], SampleEndPositions_offMesh[i], 5f, false );
				//total += DateTime.Now.Subtract(dt_loopstart).TotalMilliseconds;  //Note: I think I should generally comment this out when gathering data so it doesn't effect the results

				//Debug.Log($"iteration: '{i}' took '{DateTime.Now.Subtract(dt_loopstart)}'...");  //Note: I think I should generally comment this out when gathering data so it doesn't effect the results
			}
			dt_loopEnd = DateTime.Now;

			SayLoopReportString($"Off Mesh Finished report---", dt_loopstart, dt_loopEnd, SampleStartPositions_offMesh.Count,
				TotalTestsTime_OffMesh_MostRecentState, AverageOperationTime_OffMesh_MostRecentState
			);
			#endregion

			#region OFF MESH WITH PATH----------------------------------------------
			dt_loopstart = DateTime.Now;

			for ( int i = 0; i < SampleStartPositions_offMesh.Count; i++ )
			{
				//dt_loopstart = DateTime.Now;  //Note: I think I should generally comment this out when gathering data so it doesn't effect the results

				bool rslt = _navmesh.Raycast(SampleStartPositions_offMesh[i], SampleEndPositions_offMesh[i], 5f, out outPath, true);
				//total += DateTime.Now.Subtract(dt_loopstart).TotalMilliseconds; //Note: I think I should generally comment this out when gathering data so it doesn't effect the results

				//Debug.Log($"iteration: '{i}' took '{DateTime.Now.Subtract(dt_loopstart)}'..."); //Note: I think I should generally comment this out when gathering data so it doesn't effect the results
			}
			dt_loopEnd = DateTime.Now;

			SayLoopReportString($"Off Mesh with path Finished report---", dt_loopstart, dt_loopEnd, SampleStartPositions_offMesh.Count,
				TotalTestsTime_OffMesh_withPath_MostRecentState, AverageOperationTime_OffMesh_withPath_MostRecentState
			);
			#endregion
		}

		[ContextMenu("z call GenerateRandomPositions()")]
		public void GenerateRandomPositions()
		{
			SampleStartPositions_onMesh = new List<Vector3>();
			SampleEndPositions_onMesh = new List<Vector3>();

			SampleStartPositions_offMesh = new List<Vector3>();
			SampleEndPositions_offMesh = new List<Vector3>();

			for ( int i = 0; i < 1000; i++ )
			{
				LNX_Triangle pickedTri = _navmesh.Triangles[ UnityEngine.Random.Range(0, _navmesh.Triangles.Length-1) ];

				SampleStartPositions_onMesh.Add
				(
					pickedTri.V_Center +
					(pickedTri.Verts[UnityEngine.Random.Range(0,3)].V_Position - pickedTri.V_Center) * UnityEngine.Random.Range(0f, 1f)
					
				);

				pickedTri = _navmesh.Triangles[UnityEngine.Random.Range(0, _navmesh.Triangles.Length - 1)];

				SampleEndPositions_onMesh.Add
				(
					pickedTri.V_Center +
					(pickedTri.Verts[UnityEngine.Random.Range(0, 3)].V_Position - pickedTri.V_Center) * UnityEngine.Random.Range(0f, 1f)
				);


				int xSign = UnityEngine.Random.Range(-1f, 1.01f) < 0 ? -1 : 1;
				int ySign = UnityEngine.Random.Range(-1f, 1.01f) < 0 ? -1 : 1;
				int zSign = UnityEngine.Random.Range(-1f, 1.01f) < 0 ? -1 : 1;
				float finalX = xSign == 1 ? _navmesh.Bounds_HighestX + UnityEngine.Random.Range(0.2f, 80f) : _navmesh.Bounds_LowestX - UnityEngine.Random.Range(0.2f, 80f);
				float finalY = ySign == 1 ? _navmesh.Bounds_HighestY + UnityEngine.Random.Range(0.2f, 80f) : _navmesh.Bounds_LowestY - UnityEngine.Random.Range(0.2f, 80f);
				float finalZ = zSign == 1 ? _navmesh.Bounds_HighestZ + UnityEngine.Random.Range(0.2f, 80f) : _navmesh.Bounds_LowestZ - UnityEngine.Random.Range(0.2f, 80f);

				SampleStartPositions_offMesh.Add(new Vector3(finalX, finalY, finalZ));

				xSign = UnityEngine.Random.Range(-1f, 1.01f) < 0 ? -1 : 1;
				ySign = UnityEngine.Random.Range(-1f, 1.01f) < 0 ? -1 : 1;
				zSign = UnityEngine.Random.Range(-1f, 1.01f) < 0 ? -1 : 1;
				finalX = xSign == 1 ? _navmesh.Bounds_HighestX + UnityEngine.Random.Range(0.2f, 80f) : _navmesh.Bounds_LowestX - UnityEngine.Random.Range(0.2f, 80f);
				finalY = ySign == 1 ? _navmesh.Bounds_HighestY + UnityEngine.Random.Range(0.2f, 80f) : _navmesh.Bounds_LowestY - UnityEngine.Random.Range(0.2f, 80f);
				finalZ = zSign == 1 ? _navmesh.Bounds_HighestZ + UnityEngine.Random.Range(0.2f, 80f) : _navmesh.Bounds_LowestZ - UnityEngine.Random.Range(0.2f, 80f);

				SampleEndPositions_offMesh.Add( new Vector3(finalX, finalY, finalZ) );
			}
		}
	}
}

/*
NOTES-----------------////////////////////////////////

Run Results----------
	ORIGINAL CONDITION (0)
		ONMESH
			totalTime		avgOperationTime			iterations
			0.5827368 s		0.5827368 ms				10,000

		ONMESH with path
			totalTime		avgOperationTime			iterations
			1.4132262 s		1.4132262 ms				10,000

		OFFMESH
			totalTime		avgOperationTime			iterations
			0.0822393 s		0.0822393 ms				10,000

		OFFMESH with path
			totalTime		avgOperationTime			iterations
			0.0777550 s		0.077755 ms				10,000

	CONDITION (1) - BIG IMPROVEMENT!!! - Prevented the raycast method from calling both AmWithinSurfaceProjection() and SamplePosition() in certain case 
		ONMESH
			totalTime		avgOperationTime			iterations
			0.1880214 s		0.1880214 ms				10,000

		ONMESH with path
			totalTime		avgOperationTime			iterations
			0.1950448 s		0.1950448 ms				10,000

		OFFMESH
			totalTime		avgOperationTime			iterations
			0 s				0 ms						10,000

		OFFMESH with path
			totalTime		avgOperationTime			iterations
			0.0685856 s		0.0685856 ms				10,000
*/