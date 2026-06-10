using System;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_VertIsOnTerminalEdge : TDG_base
    {
        [Header("REFERENCE")]
        [SerializeField] private LNX_ComponentGrabber _grabber;
        public LNX_Vertex CurrentlyGrabbedVert => _grabber.CurrentlyGrabbedVert;

		[Header("RESULTS")]
        public bool MethodResult;



        [ContextMenu("z RunOperation")]
        public void RunOperation()
        {
			mthdDbg_Report.Clear();
			DBG_Operation = $"{DateTime.Now}\n";

			if( CurrentlyGrabbedVert == null )
			{
				DBG_Operation += $"currently grabbed vert is null. Stopping early...\n";
				return;
			}
			if ( CurrentlyGrabbedVert.SharedVertexCoordinates == null )
			{
				DBG_Operation += $"SharedVertexCoordinates for this vert is null. Stopping early...\n";
				return;
			}
			if ( CurrentlyGrabbedVert.SharedVertexCoordinates.Length <= 0 )
			{
				DBG_Operation += $"NOTICE! SharedVertexCoordinates length is 0. Is it supposed to be?\n";
			}

			DBG_Operation += $"using CurrentlyGrabbedVert: '{CurrentlyGrabbedVert}'\n" +
				$"this vert has: '{CurrentlyGrabbedVert.SharedVertexCoordinates.Length}' shared vert coords...\n\n" +
				$"commencing operaation..\n\n";

			DateTime dt_opStart = DateTime.Now;
			MethodResult = _navmesh.VertIsOnTerminalEdge(CurrentlyGrabbedVert.TriangleIndex, CurrentlyGrabbedVert.ComponentIndex);
			DateTime dt_opEnd = DateTime.Now;

			DBG_Operation += $"operation finished. Result: '{MethodResult}', meaning {(MethodResult ? "vert IS on terminal edge" : "vert is NOT on terminal edge")}.\n" +
				$"time taken: '{dt_opEnd.Subtract(dt_opStart).TotalMilliseconds}' ms\n";

			if ( !MethodResult )
			{
				Debug.LogWarning($"Got false on vert: '{CurrentlyGrabbedVert}'! This is notable since all verts should be terminal if the navmesh was derived from a Unity navmesh");
			}
        }

		protected override void OnDrawGizmos()
		{
			if
			(
				AmInUnitTest ||
				!SelectionIsOneOfTheFollowing(
					gameObject,
					_grabber.gameObject
				)
			)
			{
				return;
			}

			base.OnDrawGizmos();

			if( AutoCalculate )
			{
				RunOperation();
			}

			Gizmos.color = MethodResult ? Color.green : Color.red;

			Gizmos.DrawSphere(_grabber.transform.position, Radius_ObjectDebugSpheres );
		}
	}
}
