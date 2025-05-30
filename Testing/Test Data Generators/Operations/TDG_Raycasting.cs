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

		public bool RaycastResult = false;

		[TextArea(1,20)]
		public string DebugRaycast;

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
