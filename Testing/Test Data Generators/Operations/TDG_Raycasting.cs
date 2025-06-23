using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_Raycasting : MonoBehaviour
    {
        public LNX_NavMesh _Lnx_Navmesh;

		public Transform startTrans;
		public Transform endTrans;

		public List<Vector3> CapturedStartPositions = new List<Vector3>();
		public List<Vector3> CapturedEndPositions = new List<Vector3>();


		public bool RaycastResult = false;

		[TextArea(1,20)]
		public string DebugRaycast;

		[ContextMenu("z CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			CapturedStartPositions.Add( startTrans.position );
			CapturedEndPositions.Add( endTrans.position );

			//Debug.Log($"Logged '{rslt_CurrentProjectedPtOnEdge}'...");
		}

		private void OnDrawGizmos()
		{
			DebugRaycast = "";

			if( Selection.activeObject == gameObject )
			{
				RaycastResult = _Lnx_Navmesh.Raycast( startTrans.position, endTrans.position, 3f );

				Gizmos.color = Color.red;
				if ( !RaycastResult)
				{
					Gizmos.color = Color.green;
				}

				Gizmos.DrawLine(startTrans.position, endTrans.position);

				DebugRaycast = _Lnx_Navmesh.DBGRaycast;
			}
		}
	}
}
