using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_DoesEdgeObstructTriPath : TDG_base
    {
		//TODO: NEED TO CREATE DATA AND FULLY INTEGRATE THIS INTO THE TDG MANAGER CLASS

		[Header("START OF DERIVED CLASS-------------------")]
		public Transform Trans_CaptureObstructEdge;
		public Transform Trans_CaptureTriangleA;
		public Transform Trans_CaptureTriangleB;

		public LNX_ComponentCoordinate Coord_EdgeA;
		public LNX_ComponentCoordinate Coord_EdgeB;
		public LNX_ComponentCoordinate Coord_ObstructEdge;

		//[Header("CURRENT RESULTS")]
		[HideInInspector] public bool CurrentResult;
		public LNX_Triangle FocusTriangleA => _navmesh.Triangles[Coord_EdgeA.TrianglesIndex];
		public LNX_Edge WidestEdgeA => FocusTriangleA.Edges[Coord_EdgeA.ComponentIndex];

		public LNX_Triangle FocusTriangleB => _navmesh.Triangles[Coord_EdgeB.TrianglesIndex];
		public LNX_Edge WidestEdgeB => FocusTriangleB.Edges[Coord_EdgeB.ComponentIndex];

		public LNX_Edge ObstructEdge => _navmesh.GetEdge(Coord_ObstructEdge);

		[Header("DATA")]
		public List<Vector3> CapturedTriCentersA = new List<Vector3>();
		public List<Vector3> CapturedTriCentersB = new List<Vector3>();
		public List<Vector3> CapturedObstructEdgeCenters = new List<Vector3>();

		[Header("PROBLEM POINTS")]
		public List<Vector3> ProblemPoints_TriGrabberA = new List<Vector3>();
		public List<Vector3> ProblemPoints_TriGrabberB = new List<Vector3>();
		public List<Vector3> ProblemPoints_ObstructEdgeGrabber = new List<Vector3>();

		[Header("DEBUG")]
		public string DBG_Method;

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			Debug.Log($"{nameof(CaptureDataPoint)}()");
			CapturedTriCentersA.Add(FocusTriangleA.V_Center);
			CapturedTriCentersB.Add(FocusTriangleB.V_Center);
			CapturedObstructEdgeCenters.Add(ObstructEdge.MidPosition);

			DrawDataPointCapture(CapturedTriCentersA[CapturedTriCentersA.Count - 1],
				Color.magenta
			);

			DrawDataPointCapture(CapturedTriCentersB[CapturedTriCentersB.Count - 1],
				Color.magenta
			);

			DrawDataPointCapture(CapturedObstructEdgeCenters[CapturedObstructEdgeCenters.Count - 1],
				Color.magenta
			);
		}

		#region HELPERS ---------------------------------------------------
		[ContextMenu("z call CaptureComponents()")]
		public void CaptureComponents()
		{
			Debug.Log($"{nameof(CaptureComponents)}()...");

			LNX_ProjectionHit hit = LNX_ProjectionHit.None;

			int triIndxA = 0;
			int triIndxB = 0;

			#region GET TRIANGLES ---------------------------------------
			if (_navmesh.SamplePosition(Trans_CaptureTriangleA.position, out hit, 2f, false))
			{
				Coord_EdgeA = new LNX_ComponentCoordinate(hit.Index_Hit, 0);
				triIndxA = hit.Index_Hit;
				//Coord_EdgeA = new LNX_ComponentCoordinate(hit.Index_Hit, 
				//LNX_Utils.GetWidestEdgeFromPerspective(Trans_CaptureTriangleA.position, _navmesh.Triangles[hit.Index_Hit]).ComponentIndex);
				Debug.Log($"Succesful sample! Set new triangle to: '{hit.Index_Hit}'");
			}
			else
			{
				Debug.LogWarning($"sample unsuccesful...");
			}

			if (_navmesh.SamplePosition(Trans_CaptureTriangleB.position, out hit, 2f, false))
			{
				Coord_EdgeB = new LNX_ComponentCoordinate(hit.Index_Hit, 0);
				triIndxB = hit.Index_Hit;

				//Coord_EdgeB = new LNX_ComponentCoordinate(hit.Index_Hit,
				//LNX_Utils.GetWidestEdgeFromPerspective(Trans_CaptureTriangleB.position, _navmesh.Triangles[hit.Index_Hit]).ComponentIndex);
				Debug.Log($"Succesful sample! Set new triangle to: '{hit.Index_Hit}'");
			}
			else
			{
				Debug.LogWarning($"sample unsuccesful...");
			}
			#endregion

			#region GET WIDEST PERSPECTIVE EDGES --------------------
			Vector3 vctr = (FocusTriangleA.V_Center + FocusTriangleB.V_Center) / 2f;
			Coord_EdgeA = new LNX_ComponentCoordinate( triIndxA, LNX_Utils.GetWidestEdgeFromPerspective(vctr, _navmesh.Triangles[triIndxA]).ComponentIndex);
			Coord_EdgeB = new LNX_ComponentCoordinate( triIndxB, LNX_Utils.GetWidestEdgeFromPerspective(vctr, _navmesh.Triangles[triIndxB]).ComponentIndex);
			#endregion

			if ( _navmesh.SamplePosition(Trans_CaptureObstructEdge.position, out hit, 2f, false) )
			{
				float bestDist = Vector3.Distance(Trans_CaptureObstructEdge.position, _navmesh.GetEdge(hit.Index_Hit, 0).MidPosition);
				int bestEdge = 0;

				if (Vector3.Distance(Trans_CaptureObstructEdge.position, _navmesh.GetEdge(hit.Index_Hit, 1).MidPosition) < bestDist)
				{
					bestDist = Vector3.Distance(Trans_CaptureObstructEdge.position, _navmesh.GetEdge(hit.Index_Hit, 1).MidPosition);
					bestEdge = 1;
				}

				if (Vector3.Distance(Trans_CaptureObstructEdge.position, _navmesh.GetEdge(hit.Index_Hit, 2).MidPosition) < bestDist)
				{
					bestDist = Vector3.Distance(Trans_CaptureObstructEdge.position, _navmesh.GetEdge(hit.Index_Hit, 2).MidPosition);
					bestEdge = 2;
				}

				Coord_ObstructEdge = new LNX_ComponentCoordinate(hit.Index_Hit, bestEdge );
				Debug.Log($"Succesful sample! Set new triangle to: '{hit.Index_Hit}'");
			}
			else
			{
				Debug.LogWarning($"sample unsuccesful...");
			}
		}

		[ContextMenu("z call SendToDataPoint")]
		public void SendToDataPoint()
		{
			//transform.position = CapturedPerspectivePositions[Index_GoToDataPoint];
		}

		[ContextMenu("z call CaptureProblemPosition (derived)")]
		public override void CaptureProblemPosition()
		{
			ProblemPoints_TriGrabberA.Add(Trans_CaptureTriangleA.position);
			ProblemPoints_TriGrabberB.Add(Trans_CaptureTriangleB.position);
			ProblemPoints_ObstructEdgeGrabber.Add( Trans_CaptureObstructEdge.position );
		}

		[ContextMenu("z call SendToProblemPosition (derived)")]
		public override void SendToProblemPosition()
		{
			Trans_CaptureTriangleA.position = ProblemPoints_TriGrabberA[index_focusProblem];
			Trans_CaptureTriangleB.position = ProblemPoints_TriGrabberB[index_focusProblem];
			Trans_CaptureObstructEdge.position = ProblemPoints_ObstructEdgeGrabber[index_focusProblem];
		}
		#endregion

		[HideInInspector] public Vector3 Trans_CaptureTriangleA_lastpos;
		[HideInInspector] public Vector3 Trans_CaptureTriangleB_lastpos;
		[HideInInspector] public Vector3 Trans_PosParam_lastpos;

		protected override void OnDrawGizmos()
		{
			DBG_Operation = "";

			if
			(
				Selection.activeObject != gameObject &&
				Selection.activeGameObject != Trans_CaptureObstructEdge.gameObject &&
				Selection.activeGameObject != Trans_CaptureTriangleA.gameObject &&
				Selection.activeObject != Trans_CaptureTriangleB.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			base.OnDrawGizmos();

			if( 
				Trans_CaptureObstructEdge.position != Trans_PosParam_lastpos || 
				Trans_CaptureTriangleA.position != Trans_CaptureTriangleA_lastpos ||
				Trans_CaptureTriangleB.position != Trans_CaptureTriangleB_lastpos
			)
			{
				CaptureComponents();

				Trans_PosParam_lastpos = Trans_CaptureObstructEdge.position;
				Trans_CaptureTriangleA_lastpos = Trans_CaptureTriangleA.position;
				Trans_CaptureTriangleB_lastpos = Trans_CaptureTriangleB.position;
			}

			DrawStandardFocusTriGizmos(FocusTriangleA, 1f, "triA", Color.magenta);
			DrawStandardEdgeFocusGizmos(WidestEdgeA, 0.25f, "edgeA", Color.yellow);
			Gizmos.DrawSphere(Trans_CaptureTriangleA.position, Radius_ObjectDebugSpheres * 0.5f);

			DrawStandardFocusTriGizmos(FocusTriangleB, 1f, "triB", Color.magenta);
			DrawStandardEdgeFocusGizmos(WidestEdgeB, 0.25f, "edgeB", Color.yellow);
			Gizmos.DrawSphere(Trans_CaptureTriangleB.position, Radius_ObjectDebugSpheres * 0.5f);

			DrawStandardEdgeFocusGizmos(ObstructEdge, 0.25f, "ObstructEdge", Color.yellow);

			DBG_Operation += $"using edgeA: '{Coord_EdgeA}', and edgeB: '{Coord_EdgeB}'...\n";

			DBG_Operation += $"Commencing operation...\n";

			CurrentResult = LNX_Utils.DoesEdgeObstructTriPath(
				_navmesh, ObstructEdge, Coord_EdgeA.TrianglesIndex, Coord_EdgeB.TrianglesIndex ,
				ref DBG_Method
			);

			DBG_Operation += $"Operation returned: '{CurrentResult}'\n";

			if (CurrentResult)
			{
				Gizmos.color = Color.green;
			}
			else
			{
				Gizmos.color = Color.red;
			}

			Gizmos.DrawSphere(Trans_CaptureObstructEdge.position, Radius_ObjectDebugSpheres);

			//Gizmos.DrawLine(WidestEdgeA.StartPosition, WidestEdgeB.StartPosition);
			//Gizmos.DrawLine(WidestEdgeA.EndPosition, WidestEdgeB.EndPosition);
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
