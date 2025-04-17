using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_ProjectThroughToPerim : TDG_base
    {
		[SerializeField] private Transform trans_destination;

		public int foundTri;

		[SerializeField] private Vector3 cachedPositionOfDestinationTransForProblemTests;

		[SerializeField] private string DBG_;

		[ContextMenu("z CaptureDestinationTransPos()")]
		public void CaptureDestinationTransPos()
		{
			cachedPositionOfDestinationTransForProblemTests = trans_destination.position;
		}

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();

			if ( Selection.activeGameObject != gameObject && Selection.activeGameObject != trans_destination.gameObject )
			{
				return;
			}

			Vector3 v_startProject, v_endProject = Vector3.zero;
			int startTri = _mgr.AmWithinNavMeshProjection(transform.position, out v_startProject);
			int destTri = _mgr.AmWithinNavMeshProjection(trans_destination.position, out v_endProject);

			DBG_ = $"startTri: '{startTri}', pos: '{v_startProject}'\n" +
				$"destTri: '{destTri}', pos: '{v_endProject}'\n";

			if ( startTri > -1 && destTri > -1 )
			{
				DBG_ += $"success!!!\n";
				Gizmos.color = Color.red;

				Vector3 v_project = _mgr.Triangles[startTri].ProjectThroughToPerimeter( v_startProject, v_endProject );

				Gizmos.DrawSphere( v_project, 0.1f );
				Gizmos.DrawLine( v_startProject, v_project );
			}
		}
	}
}