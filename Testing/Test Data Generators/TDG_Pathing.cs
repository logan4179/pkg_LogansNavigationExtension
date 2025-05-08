using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_Pathing : TDG_base
    {
		[SerializeField] private Transform trans_Destination;

		LNX_Path _path;

		[SerializeField] private string dbg_path;

		protected override void OnDrawGizmos()
		{
			base.OnDrawGizmos();

			if ( Selection.activeGameObject != gameObject )
			{
				return;
			}

			Handles.Label( trans_Destination.position + (Vector3.up * 0.2f), "destination" );

			dbg_path = string.Empty;
			if( _mgr.CalculatePath(transform.position, trans_Destination.position, 1f, out _path) )
			{
				dbg_path += $"found path.";
			}
			else
			{
				dbg_path += $"no path...";
			}
		}
	}
}