using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_TryProjectPathThrough : TDG_base
    {
		//TODO: NEED TO CREATE DATA AND FULLY INTEGRATE THIS INTO THE TDG MANAGER CLASS
		//todo: can get rid of a lot of the code here by implementing component grabbers, and datacapturers, etc

		[Header("START OF DERIVED CLASS-------------------")]
		public LNX_ComponentGrabber Grabber_StartVert;
		public LNX_ComponentGrabber Grabber_EndVert;

		public LNX_Triangle StartTri => Grabber_StartVert.CurrentlyGrabbedTriangle;
		public LNX_Vertex StartVert => Grabber_StartVert.CurrentlyGrabbedVert;
		public LNX_Triangle EndTri => Grabber_EndVert.CurrentlyGrabbedTriangle;
		public LNX_Vertex EndVert => Grabber_EndVert.CurrentlyGrabbedVert;

		[Header("RESULTS")]
		public bool CurrentResult;
		public LNX_Path RsltPath;

		[Header("DEBUG")]
		[Range(0f,0.15f)] public float triRaise = 0.1f;

		protected override void OnDrawGizmos()
		{
			if
			(
				Selection.activeGameObject != gameObject &&
				Selection.activeGameObject != Grabber_StartVert.gameObject &&
				Selection.activeGameObject != Grabber_EndVert.gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			DBG_Operation = "";
			DBG_Method = "";

			base.OnDrawGizmos();

			DrawStandardFocusTriGizmos(StartTri, triRaise, "", Color.magenta, true, triRaise, true, false);
			Grabber_StartVert.DrawMyGizmos(Radius_ObjectDebugSpheres);

			DrawStandardFocusTriGizmos(EndTri, triRaise, "", Color.magenta, true, triRaise, true, false);
			Grabber_EndVert.DrawMyGizmos(Radius_ObjectDebugSpheres);


			DBG_Operation += $"using startVert: '{StartVert}', and endvert: '{EndVert}'...\n";

			DBG_Operation += $"Commencing operation...\n";

			CurrentResult = LNX_Utils.TryProjectThrough( _navmesh,	StartVert, EndVert, out RsltPath, ref DBG_Method );

			DBG_Operation += $"Operation returned: '{CurrentResult}'\n";

			if (CurrentResult)
			{
				Gizmos.color = Color.green;
				Handles.color = Color.green;

				RsltPath.DrawMyGizmos(0.015f, 0.35f);

			}
			else
			{
				Gizmos.color = Color.red;
				Gizmos.DrawLine(StartVert.V_Position, EndVert.V_Position);

			}

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
