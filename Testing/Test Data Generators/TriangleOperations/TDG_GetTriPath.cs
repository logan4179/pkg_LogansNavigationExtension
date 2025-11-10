using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_GetTriPath : TDG_base
    {
		//TODO: NEED TO CREATE DATA AND FULLY INTEGRATE THIS INTO THE TDG MANAGER CLASS

		[Header("START OF DERIVED CLASS-------------------")]
		public LNX_ComponentGrabber Grabber_TriA;
		public LNX_ComponentGrabber Grabber_TriB;

		[Header("CURRENT RESULTS")]
		public LNX_Quad CurrentResult;
		public LNX_Triangle TriangleA => Grabber_TriA.CurrentlyGrabbedTriangle;
		public LNX_Triangle TriangleB => Grabber_TriB.CurrentlyGrabbedTriangle;


		[Header("DEBUG")]
		[Range(0f, 0.25f)] public float edgeRaise = 0.15f;
		[Range(0f, 0.5f)] public float triRaise = 0.5f;
		public Color Color_quad;

		public string DBG_Method;

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			Debug.Log($"{nameof(CaptureDataPoint)}()");

		}

		[ContextMenu("z call CaptureProblemPoint()")]
		public void CaptureProblemPoint()
		{
			Debug.Log($"{nameof(CaptureProblemPoint)}()");

			_dataCapture_problems.CaptureDataPoint(TriangleA.V_Center, TriangleB.V_Center);
		}

		#region HELPERS ---------------------------------------------------
		[ContextMenu("z call CaptureComponents()")]
		public void CaptureComponents()
		{
			Debug.Log($"{nameof(CaptureComponents)}()...");

			Grabber_TriA.GrabComponent();
			Grabber_TriB.GrabComponent();
		}

		[ContextMenu("z call GoToProblemPoint(derived)")]
		public override void GoToProblemPoint()
		{
			base.GoToProblemPoint();

			CaptureComponents();
		}

		#endregion

		protected override void OnDrawGizmos()
		{
			if
			(
				Selection.activeObject != gameObject &&
				Selection.activeGameObject != Grabber_TriA.gameObject &&
				Selection.activeGameObject != Grabber_TriB.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			DBG_Operation = "";
			DBG_Method = "";

			base.OnDrawGizmos();

			if( TriangleA == null )
			{
				DBG_Operation += $"{nameof(TriangleA)} was null. Returning early...\n";
				return;
			}

			if (TriangleB == null)
			{
				DBG_Operation += $"{nameof(TriangleB)} was null. Returning early...\n";
				return;
			}

			DrawStandardFocusTriGizmos(TriangleA, triRaise, "triA", Color.magenta, true, 0.025f, true);
			Grabber_TriA.DrawMyGizmos( Radius_ObjectDebugSpheres );

			DrawStandardFocusTriGizmos(TriangleB, triRaise, "triB", Color.magenta, true, 0.025f, true);
			Grabber_TriB.DrawMyGizmos( Radius_ObjectDebugSpheres );

			DBG_Operation += $"using TriangleA: '{TriangleA}', and TriangleB: '{TriangleB}'...\n";

			DBG_Operation += $"Commencing operation...\n";

			CurrentResult = LNX_Utils.GetTriPath( TriangleA, TriangleB, ref DBG_Method );

			DBG_Operation += $"Operation returned: '{CurrentResult}'\n";

			Gizmos.color = Color_quad;
			DrawQuadVisual( CurrentResult );

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
