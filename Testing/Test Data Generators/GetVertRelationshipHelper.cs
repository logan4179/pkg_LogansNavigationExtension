using LogansNavigationExtension;
using System;
using UnityEditor;
using UnityEngine;

namespace LNX_Debugging
{
	public class GetVertRelationshipHelper : MonoBehaviour
	{
		public LNX_NavMeshSurface _navmesh;
		public LNX_ComponentGrabber PerspVrtGrabber;
		public LNX_ComponentGrabber ToVrtGrabber;

		public LNX_Vertex PerspectiveVert => PerspVrtGrabber.CurrentlyGrabbedVert;
		public LNX_Vertex ToVert => ToVrtGrabber.CurrentlyGrabbedVert;

		[TextArea(1, 20)]
		public string DBG_Operation;


		[Header("RESULT")]
		public LNX_VertexRelationship ResultRelationship;




		[ContextMenu("RunOperation")]
		public void RunOperation()
		{
			DBG_Operation = $"Recalculating at: '{DateTime.Now}'...\n";
			ResultRelationship = null;

			if (PerspectiveVert == null)
			{
				Debug.LogError($"PerspectiveVert is null...");
				return;
			}
			if (ToVert == null)
			{
				Debug.LogError($"ToVert is null...");
				return;
			}

			DBG_Operation += $"using PerspectiveVert: '{PerspectiveVert}'\n" +
				$"using ToVert: '{ToVert}'\n" +
				$"";

			ResultRelationship = PerspectiveVert.GetRelationship(ToVert.MyCoordinate);

			DBG_Operation += $"got relationship: '{ResultRelationship}'\n";

			#region SELF ===================================================


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
					PerspVrtGrabber.gameObject,
					ToVrtGrabber.gameObject
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


		#endregion


	}
}