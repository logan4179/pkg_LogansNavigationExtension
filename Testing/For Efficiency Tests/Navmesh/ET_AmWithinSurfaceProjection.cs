using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace LogansNavigationExtension
{
    public class ET_AmWithinSurfaceProjection : ET_Base
    {
		public List<Vector3> SamplePositions_onMesh = new List<Vector3>();
		public List<Vector3> SamplePositions_offMesh = new List<Vector3>();


		[ContextMenu("z call RunTest()")]
		public override void RunTests()
		{
			float TotalTestsTime_OnMesh_originalState = 13.5991f;
			float TotalTestsTime_OffMesh_originalState = 13.6099654f;
			float AverageOperationTime_OnMesh_originalState = 1.35991f;
			float AverageOperationTime_OffMesh_originalState = 1.360996f;

			float TotalTestsTime_OnMesh_MostRecentState = 0.8368642f;
			float TotalTestsTime_OffMesh_MostRecentState = 0.8258476f;
			float AverageOperationTime_OnMesh_MostRecentState = 0.08368642f;
			float AverageOperationTime_OffMesh_MostRecentState = 0.08258476f;
			

			base.RunTests();
			DateTime dt_loopEnd;

			#region ON MESH ----------------------------------------------

			DateTime dt_loopstart = DateTime.Now;
			double total = 0;

			for ( int i = 0; i < SamplePositions_onMesh.Count; i++ )
			{
				//dt_loopstart = DateTime.Now;  //Note: I think I should generally comment this out when gathering data so it doesn't effect the results

				LNX_ProjectionHit hit;
				bool rslt = _navmesh.AmWithinSurfaceProjection( SamplePositions_onMesh[i], out hit );
				//total += DateTime.Now.Subtract(dt_loopstart).TotalMilliseconds; //Note: I think I should generally comment this out when gathering data so it doesn't effect the results

				//Debug.Log($"iteration: '{i}' took '{DateTime.Now.Subtract(dt_loopstart)}'..."); //Note: I think I should generally comment this out when gathering data so it doesn't effect the results
			}
			dt_loopEnd = DateTime.Now;

			SayLoopReportString($"In Shape Finished report---", dt_loopstart, dt_loopEnd, SamplePositions_onMesh.Count,
				TotalTestsTime_OnMesh_MostRecentState, AverageOperationTime_OnMesh_MostRecentState
			);
			#endregion ON MESH

			#region OFF MESH ----------------------------------------------
			dt_loopstart = DateTime.Now;
			total = 0;

			for ( int i = 0; i < SamplePositions_offMesh.Count; i++ )
			{
				//dt_loopstart = DateTime.Now;  //Note: I think I should generally comment this out when gathering data so it doesn't effect the results

				LNX_ProjectionHit hit;
				bool rslt = _navmesh.AmWithinSurfaceProjection(SamplePositions_onMesh[i], out hit);
				//total += DateTime.Now.Subtract(dt_loopstart).TotalMilliseconds;  //Note: I think I should generally comment this out when gathering data so it doesn't effect the results

				//Debug.Log($"iteration: '{i}' took '{DateTime.Now.Subtract(dt_loopstart)}'...");  //Note: I think I should generally comment this out when gathering data so it doesn't effect the results
			}
			dt_loopEnd = DateTime.Now;

			SayLoopReportString($"Off Mesh Finished report---", dt_loopstart, dt_loopEnd, SamplePositions_onMesh.Count,
				TotalTestsTime_OffMesh_MostRecentState, AverageOperationTime_OffMesh_MostRecentState
			);			
			#endregion OFF MESH
		}

		[ContextMenu("z call GenerateRandomPositions()")]
		public void GenerateRandomPositions()
		{
			SamplePositions_onMesh = new List<Vector3>();
			SamplePositions_offMesh = new List<Vector3>();

			for ( int i = 0; i < 10000; i++ )
			{
				SamplePositions_onMesh.Add
				(
					new Vector3
					( 
						UnityEngine.Random.Range(_navmesh.Bounds_LowestX, _navmesh.Bounds_HighestX),
						UnityEngine.Random.Range(_navmesh.Bounds_LowestY, _navmesh.Bounds_HighestY),
						UnityEngine.Random.Range(_navmesh.Bounds_LowestZ, _navmesh.Bounds_HighestZ)
					)
				);

				int xSign = UnityEngine.Random.Range(-1f, 1.01f) < 0 ? -1 : 1;
				int ySign = UnityEngine.Random.Range(-1f, 1.01f) < 0 ? -1 : 1;
				int zSign = UnityEngine.Random.Range(-1f, 1.01f) < 0 ? -1 : 1;
				float finalX = xSign == 1 ? _navmesh.Bounds_HighestX + UnityEngine.Random.Range(0.2f, 80f) : _navmesh.Bounds_LowestX - UnityEngine.Random.Range(0.2f, 80f);
				float finalY = ySign == 1 ? _navmesh.Bounds_HighestY + UnityEngine.Random.Range(0.2f, 80f) : _navmesh.Bounds_LowestY - UnityEngine.Random.Range(0.2f, 80f);
				float finalZ = zSign == 1 ? _navmesh.Bounds_HighestZ + UnityEngine.Random.Range(0.2f, 80f) : _navmesh.Bounds_LowestZ - UnityEngine.Random.Range(0.2f, 80f);

				SamplePositions_offMesh.Add( new Vector3(finalX, finalY, finalZ) );
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
			13.5991 s			1.35991 ms				10,000

		OFFMESH
			totalTime		avgOperationTime			iterations
			13.6099654 s		1.360996 ms				10,000

	CONDITION 1 [WAY FASTER] - AFTER TAKING AWAY DEBUGGING STRING INTERPOLATION IN METHOD CHAIN
		ONMESH
			totalTime		avgOperationTime			iterations
			0.8368642 s		0.08368642 ms				10,000

		OFFMESH
			totalTime		avgOperationTime			iterations
			0.8258476 s		0.08258476 ms				10,000

	CONDITION 2 [needs to be done] - changed AmWithInSurfaceProjection into essentially an overload of the SamplePosition method


*/