using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class ET_IsInShapeProject : ET_Base
    {

        public List<Vector3> SamplePositions_InShape = new List<Vector3>();
		public List<Vector3> SamplePositions_OffShape = new List<Vector3>();


		public Vector3 TriCenter;

		public LNX_Triangle CurrentTri;

		[ContextMenu("z call RecallTriFromSavedCenter()")]
		public void RecallTriFromSavedCenter()
		{
			LNX_NavmeshHit hit;

            CurrentTri = _navmesh.GetTriangle(TriCenter);

			TriCenter = CurrentTri.V_Center;
		}

		[ContextMenu("z call SampleTri()")]
		public void SampleTri()
		{
            LNX_NavmeshHit hit;

            _navmesh.SamplePosition( transform.position, out hit, 10f, false );

			CurrentTri = _navmesh.Triangles[hit.TriangleIndex];
			TriCenter = CurrentTri.V_Center;
		}

		[ContextMenu("z call RunTests()")]
        public override void RunTests()
        {
			base.RunTests();

			//[Header("ORIGINAL STATS")]
			float TotalTestsTime_InShape_originalState = 0.43250f;
			float AvgOperationTime_InShape_originalState = 0.043250f;
			float TotalTestsTime_OffShape_originalState = 0.57332f;
			float AvgOperationTime_OffShape_originalState = 0.057332f;


			float TotalTestsTime_InShape_mostRecentState = 0.3653134f;
			float AvgOperationTime_InShape_mostRecentState = 0.03653134f;
			float TotalTestsTime_OffShape_mostRecentState = 0.1150248f;
			float AvgOperationTime_OffShape_mostRecentState = 0.01150248f;

			DateTime dt_loopEnd;
			TimeSpan ts_loop;

			double total = 0;

			DateTime dt_loopstart = DateTime.Now;

			#region IN SHAPE -----------------------------------------
			for ( int i = 0; i < SamplePositions_InShape.Count; i++ )
			{
				//dt_loopstart = DateTime.Now;

				LNX_NavmeshHit hit = LNX_NavmeshHit.None;
				bool rslt = CurrentTri.IsInShapeProject(SamplePositions_InShape[i], out hit );
				if( !rslt )
				{
					Debug.LogError($"test '{i}' was supposed to be in shape, but apparently it wasn't...");
				}
				//total += DateTime.Now.Subtract(dt_loopstart).TotalMilliseconds;

				//Debug.Log($"iteration: '{i}' took '{DateTime.Now.Subtract(dt_loopstart)}'...");
			}
			dt_loopEnd = DateTime.Now;

			ts_loop = dt_loopEnd.Subtract(dt_loopstart);

			SayLoopReportString($"In Shape Finished report---", dt_loopstart, dt_loopEnd, SamplePositions_InShape.Count,
				TotalTestsTime_InShape_mostRecentState, AvgOperationTime_InShape_mostRecentState
			);
			#endregion

			#region OFF SHAPE -----------------------------------------
			dt_loopstart = DateTime.Now;
			for (int i = 0; i < SamplePositions_OffShape.Count; i++)
			{
				//dt_loopstart = DateTime.Now;

				LNX_NavmeshHit hit = LNX_NavmeshHit.None;
				bool rslt = CurrentTri.IsInShapeProject(SamplePositions_OffShape[i], out hit );
				if (rslt)
				{
					Debug.LogError($"test '{i}' was supposed to be off shape, but apparently it wasn't...");
				}
				//total += DateTime.Now.Subtract(dt_loopstart).TotalMilliseconds;

				//Debug.Log($"iteration: '{i}' took '{DateTime.Now.Subtract(dt_loopstart)}'...");
			}

			dt_loopEnd = DateTime.Now;

			ts_loop = dt_loopEnd.Subtract(dt_loopstart);

			SayLoopReportString($"Off Shape Finished report---", dt_loopstart, dt_loopEnd, SamplePositions_OffShape.Count,
				TotalTestsTime_OffShape_mostRecentState, AvgOperationTime_OffShape_mostRecentState
			);
			#endregion
		}

		[ContextMenu("z call GenerateRandomPositions()")]
		public void GenerateRandomPositions()
		{
			SamplePositions_InShape = new List<Vector3>();

			for ( int i = 0; i < 10000; i++ )
			{
				SamplePositions_InShape.Add
				( 
					TriCenter + ((CurrentTri.Verts[UnityEngine.Random.Range(0, 3)].V_Position - TriCenter) * UnityEngine.Random.Range(0f, 1f))
				);

				SamplePositions_OffShape.Add
				(
					TriCenter + ((CurrentTri.Verts[UnityEngine.Random.Range(0, 3)].V_Position - TriCenter) * UnityEngine.Random.Range(1.1f, 80f))
				);
			}
		}

		[ContextMenu("z call SayCurrentTri()")]
		public void SayCurrentTri()
		{
			CurrentTri.SayCurrentInfo(_navmesh);
		}

		private void OnDrawGizmos()
		{
			if ( Selection.activeObject != gameObject )
			{
				return;
			}

			DrawStandardFocusTriGizmos( CurrentTri, 1f, CurrentTri.Index_inCollection.ToString() );
		}
	}
}

/*
NOTES-----------------////////////////////////////////

Run Results----------
	ORIGINAL CONDITION
		ON SHAPE
			totalTime		avgOperationTime			iterations
			0.43250 s		0.043250 ms					10,000

		OFF SHAPE
			totalTime		avgOperationTime			iterations
			0.57332 s		0.057332 ms					10,000

	CONDITION (2) - Took away some string interpolation for debugging in the Tri.IsInShapeProject() method.
		IN SHAPE
			totalTime		avgOperationTime			iterations
			0.3653134 s		0.03653134 ms					10,000

		OFF SHAPE
			totalTime		avgOperationTime			iterations
			0.1150248 s		0.01150248 ms				10,000

		CONDITION (3) [WAY FASTER!] - Took away some string interpolation for debugging in the Vert.IsInCenterSweep() method.
		IN SHAPE
			totalTime		avgOperationTime			iterations
			0.0423121 s		0.00423121 ms				10,000

		OFF SHAPE
			totalTime		avgOperationTime			iterations
			 0.0095160s		0.0009516 ms				10,000
*/