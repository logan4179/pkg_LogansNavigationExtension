using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace LogansNavigationExtension
{
    public class TDG_GetWidestEdgeFromPerspective_edgeOverload : TDG_base
    {
		//TODO: NEED TO CREATE DATA AND FULLY INTEGRATE THIS INTO THE TDG MANAGER CLASS

		public LNX_ComponentGrabber PerspectiveEdgeGrabber;
		public LNX_ComponentGrabber OtherTriGrabber;

		LNX_Edge PerspectiveEdge => PerspectiveEdgeGrabber.CurrentlyGrabbedEdge;
		public LNX_Triangle PerspectiveTri => _navmesh.Triangles[PerspectiveEdgeGrabber.CurrentCoordinate.TrianglesIndex];

		LNX_Triangle OtherTriangle => OtherTriGrabber.CurrentlyGrabbedTriangle;


		//[Header("CURRENT RESULTS")]
		[HideInInspector, SerializeField] private LNX_ComponentCoordinate ResultEdgeCoordinate;
		public LNX_Edge ResultEdge => _navmesh.GetEdge( ResultEdgeCoordinate );


		[Header("DEBUG")]
		public Color Clr_BridgeVisual;

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			_dataCapture.CaptureDataPoint( PerspectiveEdge.MidPosition, OtherTriangle.V_Center, ResultEdge.MidPosition );			
		}

		[ContextMenu("z call CaptureProblemPosition()")]
		public override void CaptureProblemPosition()
		{
			_dataCapture_problems.CaptureDataPoint( PerspectiveEdgeGrabber.transform.position, OtherTriGrabber.transform.position );
		}

		#region HELPERS ---------------------------------------------------
		[ContextMenu("z call SampleComponents()")]
		public void SampleComponents()
		{
			Debug.Log($"{nameof(SampleComponents)}()...");

			PerspectiveEdgeGrabber.GrabComponent();

			if( PerspectiveEdgeGrabber.CurrentCoordinate == LNX_ComponentCoordinate.None )
			{
				Debug.Log($"Something went wrong trying to grab the perspective edge. ");
			}

			OtherTriGrabber.GrabComponent();

			if ( OtherTriGrabber.CurrentCoordinate == LNX_ComponentCoordinate.None )
			{
				Debug.Log($"sample unsuccesful...");
			}
		}

		[ContextMenu("z call SayFocusComponents()")]
		public void SayFocusComponents()
		{
			Debug.Log($"{nameof(SayFocusComponents)}()...");

			OtherTriangle.SayCurrentInfo(_navmesh);

			Debug.Log(OtherTriangle.GetAnomolyString(_navmesh));
		}

		[ContextMenu("z call DoEet()")]
		public void DoEet()
		{

		}

		[ContextMenu("z call SendToProblemPosition()")]
		public void SendToProblemPosition()
		{
			PerspectiveEdgeGrabber.transform.position = _dataCapture_problems.VectorCaptureLists[0].vectors[Index_GoToProblem];
			OtherTriGrabber.transform.position = _dataCapture_problems.VectorCaptureLists[1].vectors[Index_GoToProblem];

			SampleComponents();
		}
		#endregion

		protected override void OnDrawGizmos()
		{
			DBG_Operation = "";

			#region	SHORT-CIRCUITING -------------------------------------------------
			if (Selection.activeObject != gameObject && 
				Selection.activeObject != PerspectiveEdgeGrabber.gameObject && 
				Selection.activeGameObject != OtherTriGrabber.gameObject)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Object not selected";
				return;
			}

			if (PerspectiveEdge == null)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. PerspectiveEdge null";
				return;
			}

			if (OtherTriangle == null)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. OtherTriangle null";
				return;
			}
			#endregion

			DBG_Operation += $"using perspective edge: '{PerspectiveEdge}' at '{PerspectiveEdge.MidPosition}'\n" +
				$"using OtherTriangle: '{OtherTriangle.Index_inCollection}' at '{OtherTriangle.V_Center}'\n";

			base.OnDrawGizmos();

			PerspectiveEdgeGrabber.DrawMyGizmos(Radius_ObjectDebugSpheres);
			OtherTriGrabber.DrawMyGizmos(Radius_ObjectDebugSpheres);

			DrawStandardFocusTriGizmos(PerspectiveTri, 0.1f, $"PerspectiveTri", Color.magenta, true, 0.01f, true);

			DrawStandardEdgeFocusGizmos(PerspectiveEdge, 0.1f, $"prspctvEdge", Color.green);
			DrawStandardFocusTriGizmos(OtherTriangle, 0.1f, $"otherTri", Color.magenta, true, 0.01f, true);

			LNX_Edge rsltEdge = null;

			string dbgMthd = string.Empty;
			DBG_Operation += $"Commencing operation...\n";
			rsltEdge = LNX_Utils.GetWidestEdgeFromPerspective(PerspectiveEdge, OtherTriangle, ref dbgMthd);
			DBG_Operation += $"Completed operation. Captured edge null?: '{rsltEdge == null}'\n";

			if (rsltEdge != null )
			{
				ResultEdgeCoordinate = rsltEdge.MyCoordinate;
				DrawStandardEdgeFocusGizmos(rsltEdge, 0.3f, "foundEdge", Color.yellow);

				DrawEdgeBridgeVisual( PerspectiveEdge, rsltEdge, Clr_BridgeVisual );
				Gizmos.color = Color.green;
			}
			else
			{
				Gizmos.color = Color.red;
			}


			DBG_Operation += $"Operation complete. \n" +
				$"";

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
