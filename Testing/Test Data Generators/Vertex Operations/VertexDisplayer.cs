using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LogansNavigationExtension
{
	/// <summary>
	/// Use this to show non-serialized info about a vertex
	/// </summary>
    public class VertexDisplayer : MonoBehaviour
    {
		public LNX_NavMesh _navmesh;
        public LNX_ComponentGrabber VertGrabber;
		public LNX_Vertex CurrentVert => VertGrabber.CurrentlyGrabbedVert;
		[TextArea(1, 20)]
		public string DBG_Operation;

		[Header("SELF")]
		public Vector3 V_Position;
		public LNX_ComponentCoordinate MyCoordinate;
		public int Index_Relational;
		public int Index_VisMesh_Vertices = -1;
		public Vector3 v_toCenter;
		public float DistanceToCenter;
		public Vector3 CachedSurfaceNormal;

		[Header("SIBLINGS")]
		public int firstSiblingRelationshipIndex;
		public LNX_VertexRelationship FirstSiblingRelationship;


		public int secondSiblingRelationshipIndex;
		public LNX_VertexRelationship SecondSiblingRelationship;


		[Header("RELATIONAL")]
		public VertDisp_Relationship[] Relationships;
		public LNX_ComponentCoordinate[] SharedVertexCoordinates;



		[ContextMenu("RunOperation")]
		public void RunOperation()
		{
			DBG_Operation = $"Recalculating at: '{DateTime.Now}'...\n";

			if ( CurrentVert == null )
			{
				Debug.LogError($"Currently grabbed vert is null...");
				return;
			}

			DBG_Operation += $"using current vert: '{CurrentVert}'\n" +
				$"";

			#region SELF ===================================================
			V_Position = CurrentVert.V_Position;
			MyCoordinate = CurrentVert.MyCoordinate;
			Index_Relational = CurrentVert.Index_Relational;
			Index_VisMesh_Vertices = CurrentVert.Index_VisMesh_Vertices;
			v_toCenter = CurrentVert.v_toCenter;
			DistanceToCenter = CurrentVert.DistanceToCenter;
			CachedSurfaceNormal = CurrentVert.CachedSurfaceNormal;
			#endregion

			#region SIBLINGS ===============================================
			firstSiblingRelationshipIndex = CurrentVert.firstSiblingRelationshipIndex;
			FirstSiblingRelationship = CurrentVert.FirstSiblingRelationship;

			secondSiblingRelationshipIndex = CurrentVert.secondSiblingRelationshipIndex;
			SecondSiblingRelationship = CurrentVert.SecondSiblingRelationship;
			#endregion

			Relationships = new VertDisp_Relationship[CurrentVert.Relationships.Length];
			for (int i = 0; i < CurrentVert.Relationships.Length; i++)
			{
				Relationships[i] = new VertDisp_Relationship( CurrentVert.Relationships[i] );
			}

			SharedVertexCoordinates = new LNX_ComponentCoordinate[CurrentVert.SharedVertexCoordinates.Length];
			for (int i = 0; i < CurrentVert.SharedVertexCoordinates.Length; i++)
			{
				SharedVertexCoordinates[i] = CurrentVert.SharedVertexCoordinates[i];
			}

			DBG_Operation += $"End of operation\n" +
				$"Relationships: '{Relationships.Length}'\n" +
				$"SharedVertexCoordinates: '{SharedVertexCoordinates.Length}'\n" +
				$"IsRelationshipCollectionValid: '{CurrentVert.IsRelationshipCollectionValid(_navmesh)}'\n" +
				$"";
		}

		private void OnDrawGizmos()
		{
			if
			(
				!SelectionIsOneOfTheFollowing(
					gameObject,
					VertGrabber.gameObject
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
	}

	[System.Serializable]
	public struct VertDisp_Relationship
	{
		public bool AmValid;
		LNX_VertexRelationship Rel;

		public VertDisp_Relationship(LNX_VertexRelationship rel )
		{
			AmValid = rel.AmValid;
			Rel = rel;
		}
	}
}
