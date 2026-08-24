using LogansNavigationExtension;
using System;
using UnityEditor;
using UnityEngine;

namespace LNX_Debugging
{
	/// <summary>
	/// Use this to show non-serialized info about a triangle
	/// </summary>
	public class TriangleDisplayer : MonoBehaviour
    {
		public LNX_NavMeshSurface _navmesh;
		public LNX_ComponentGrabber TriGrabber;
		public LNX_Triangle CurrentTri => TriGrabber.CurrentlyGrabbedTriangle;
		[TextArea(1, 20)]
		public string DBG_Operation;


		[Header("SELF")]
		[TextArea(1, 20)]
		public Vector3 V_Center;
		public int Index_inCollection;



		[ContextMenu("RunOperation")]
		public void RunOperation()
		{
			DBG_Operation = $"Recalculating at: '{DateTime.Now}'...\n";

			if ( CurrentTri == null )
			{
				Debug.LogError($"Currently grabbed vert is null...");
				return;
			}

			DBG_Operation += $"using current tri: '{CurrentTri}'\n" +
				$"";

			#region SELF ===================================================
			V_Center = CurrentTri.V_Center;
			Index_inCollection = CurrentTri.Index_inCollection;

			#endregion

			DBG_Operation += $"End of operation\n" +
				$"";
		}



		private void OnDrawGizmos()
		{
			if
			(
				!SelectionIsOneOfTheFollowing(
					gameObject,
					TriGrabber.gameObject
				)
			)
			{
				return;
			}

			
		}

		public bool SelectionIsOneOfTheFollowing(params GameObject[] gameObjects)
		{
			for (int i = 0; i < gameObjects.Length; i++)
			{
				if (Selection.activeGameObject == gameObjects[i])
				{
					return true;
				}
			}

			return false;
		}

		#region HELPERS ======================================
		[ContextMenu("z call SayCurrentVertInfo()")]
		public void SayCurrentVertInfo()
		{
			CurrentTri.SayCurrentInfo(_navmesh);
		}

		#endregion

	}
}