using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_IsBetweenEdgePath : TDG_base
    {
		[Header("START OF DERIVED CLASS-------------------")]
		public Transform Trans_PosParameter;
		public Transform Trans_CaptureTriangleA;
		public Transform Trans_CaptureTriangleB;

		//TODO: NEED TO CREATE DATA AND FULLY INTEGRATE THIS INTO THE TDG MANAGER CLASS
		public LNX_ComponentCoordinate Coord_EdgeA;
		public LNX_ComponentCoordinate Coord_EdgeB;

		[Header("CURRENT RESULTS")]
		public bool CurrentResult;
		public LNX_Triangle FocusTriangleA => _navmesh.Triangles[Coord_EdgeA.TrianglesIndex];
		public LNX_Edge FocusEdgeA => FocusTriangleA.Edges[Coord_EdgeA.ComponentIndex];

		public LNX_Triangle FocusTriangleB => _navmesh.Triangles[Coord_EdgeB.TrianglesIndex];
		public LNX_Edge FocusEdgeB => FocusTriangleB.Edges[Coord_EdgeB.ComponentIndex];


		[Header("DATA")]
		public List<Vector3> CapturedPerspectivePositions = new List<Vector3>();
		public List<Vector3> CapturedTriCenterPositions = new List<Vector3>();


		//[Header("DEBUG")]

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			//CapturedPerspectivePositions.Add(transform.position);
			//CapturedTriCenterPositions.Add(CurrentTriangle.V_Center);

			DrawDataPointCapture(CapturedPerspectivePositions[CapturedPerspectivePositions.Count - 1],
				Color.magenta
			);


			Debug.Log($"'{CapturedPerspectivePositions[CapturedPerspectivePositions.Count - 1]}'");
		}

		#region HELPERS ---------------------------------------------------
		[ContextMenu("z call CaptureTriangles()")]
		public void CaptureTriangles()
		{
			Debug.Log($"{nameof(CaptureTriangles)}()...");

			LNX_ProjectionHit hit = LNX_ProjectionHit.None;

			if (_navmesh.SamplePosition(Trans_CaptureTriangleA.position, out hit, 2f, false))
			{
				int edgeIndx = 0;
				float runningClosestDist = Vector3.Distance( 
					Trans_CaptureTriangleA.position, 
					_navmesh.Triangles[hit.Index_Hit].Edges[0].MidPosition
				);

				if (
					Vector3.Distance(
						Trans_CaptureTriangleA.position, _navmesh.Triangles[hit.Index_Hit].Edges[1].MidPosition
					) < runningClosestDist
				)
				{
					runningClosestDist = Vector3.Distance(
						Trans_CaptureTriangleA.position, _navmesh.Triangles[hit.Index_Hit].Edges[1].MidPosition
					);
					edgeIndx = 1;
				}

				if (
					Vector3.Distance(
						Trans_CaptureTriangleA.position, _navmesh.Triangles[hit.Index_Hit].Edges[2].MidPosition
					) < runningClosestDist
				)
				{
					runningClosestDist = Vector3.Distance(
						Trans_CaptureTriangleA.position, _navmesh.Triangles[hit.Index_Hit].Edges[2].MidPosition
					);
					edgeIndx = 2;
				}

				Coord_EdgeA = new LNX_ComponentCoordinate( hit.Index_Hit, edgeIndx );
				Debug.Log($"Succesful sample! Set new triangle to: '{hit.Index_Hit}'");
			}
			else
			{
				Debug.Log($"sample unsuccesful...");
			}

			if ( _navmesh.SamplePosition(Trans_CaptureTriangleB.position, out hit, 2f, false) )
			{
				int edgeIndx = 0;
				float runningClosestDist = Vector3.Distance(
					Trans_CaptureTriangleB.position,
					_navmesh.Triangles[hit.Index_Hit].Edges[0].MidPosition
				);

				if (
					Vector3.Distance(
						Trans_CaptureTriangleB.position, _navmesh.Triangles[hit.Index_Hit].Edges[1].MidPosition
					) < runningClosestDist
				)
				{
					runningClosestDist = Vector3.Distance(
						Trans_CaptureTriangleB.position, _navmesh.Triangles[hit.Index_Hit].Edges[1].MidPosition
					);
					edgeIndx = 1;
				}

				if (
					Vector3.Distance(
						Trans_CaptureTriangleB.position, _navmesh.Triangles[hit.Index_Hit].Edges[2].MidPosition
					) < runningClosestDist
				)
				{
					runningClosestDist = Vector3.Distance(
						Trans_CaptureTriangleB.position, _navmesh.Triangles[hit.Index_Hit].Edges[2].MidPosition
					);
					edgeIndx = 2;
				}

				Coord_EdgeB = new LNX_ComponentCoordinate(hit.Index_Hit, edgeIndx);
				Debug.Log($"Succesful sample! Set new triangle to: '{hit.Index_Hit}'");
			}
			else
			{
				Debug.Log($"sample unsuccesful...");
			}
		}

		[ContextMenu("z call DoEet()")]
		public void DoEet()
		{

		}

		[ContextMenu("z call SendToDataPoint")]
		public void SendToDataPoint()
		{
			transform.position = CapturedPerspectivePositions[Index_GoToDataPoint];
		}
		#endregion

		protected override void OnDrawGizmos()
		{
			DBG_Operation = "";

			if
			(
				Selection.activeObject != gameObject &&
				Selection.activeGameObject != Trans_PosParameter.gameObject &&
				Selection.activeGameObject != Trans_CaptureTriangleA.gameObject &&
				Selection.activeObject != Trans_CaptureTriangleB.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			base.OnDrawGizmos();

			DrawStandardFocusTriGizmos( FocusTriangleA, 1f, "triA", Color.magenta );
			DrawStandardEdgeFocusGizmos( FocusEdgeA, 0.25f, "edgeA", Color.yellow );
			Gizmos.DrawSphere(Trans_CaptureTriangleA.position, Radius_ObjectDebugSpheres * 0.5f);

			DrawStandardFocusTriGizmos(FocusTriangleB, 1f, "triB", Color.magenta);
			DrawStandardEdgeFocusGizmos(FocusEdgeB, 0.25f, "edgeB", Color.yellow);
			Gizmos.DrawSphere(Trans_CaptureTriangleB.position, Radius_ObjectDebugSpheres * 0.5f);

			DBG_Operation += $"Commencing operation...\n";

			CurrentResult = _navmesh.IsBetweenEdgePath( transform.position, Coord_EdgeA, Coord_EdgeB );

			if (CurrentResult)
			{
				Gizmos.color = Color.green;
			}
			else
			{
				Gizmos.color = Color.red;
			}

			Gizmos.DrawSphere(Trans_PosParameter.position, Radius_ObjectDebugSpheres);

			Gizmos.DrawLine( FocusEdgeA.StartPosition, FocusEdgeB.StartPosition );
			Gizmos.DrawLine( FocusEdgeA.EndPosition, FocusEdgeB.EndPosition );
		}

		#region WRITING ----------------------------------------------------
		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			bool rslt = TDG_Manager.WriteTestObjectToJson(TDG_Manager.filePath_testData_isInCenterSweep, this);

			if (rslt)
			{
				LastWriteTime = System.DateTime.Now.ToString();
				return true;
			}

			return false;
		}
		#endregion
	}
}
