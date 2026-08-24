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
		public LNX_NavMeshSurface _navmesh;
        public LNX_ComponentGrabber VertGrabber;
		public LNX_ComponentGrabber PathToVertGrabber;
		public LNX_Vertex CurrentVert => VertGrabber.CurrentlyGrabbedVert;
		public LNX_Vertex PathToVert => PathToVertGrabber.CurrentlyGrabbedVert;

		[TextArea(1, 20)]
		public string DBG_Operation;
		[TextArea(1, 10)]
		public string DBG_PathDisplay;

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
		[Range(-1, 83)]  public int Index_PathDisplay_tri = -1;
		[Range(0, 2)] public int Index_PathDisplay_vert = 0;

		public LNX_ComponentCoordinate[] SharedVertexCoordinates;

		[Header("DEBUG")]
		[Range(0f, 0.5f)] public float radius_pthPts = 0.1f;
		[Range(0f, 1f)] public float height_pthPts = 0.5f;
		public bool AutoGrab = true;


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

			SharedVertexCoordinates = new LNX_ComponentCoordinate[CurrentVert.SharedVertexCoordinates.Length];
			for (int i = 0; i < CurrentVert.SharedVertexCoordinates.Length; i++)
			{
				SharedVertexCoordinates[i] = CurrentVert.SharedVertexCoordinates[i];
			}

			DBG_Operation += $"End of operation\n" +
				$"Relationships: '{CurrentVert.Relationships.Length}'\n" +
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
					VertGrabber.gameObject,
					PathToVertGrabber.gameObject
				)
			)
			{
				return;
			}

			DBG_PathDisplay = $"Recalculating at: '{DateTime.Now}'...\n\n";

			if 
			( 
				AutoGrab && 
				(Index_PathDisplay_tri != PathToVert.TriangleIndex || Index_PathDisplay_vert != PathToVert.ComponentIndex)
				
			)
			{
				Index_PathDisplay_tri = PathToVert.TriangleIndex;
				Index_PathDisplay_vert = PathToVert.ComponentIndex;
			}


			if ( Index_PathDisplay_tri > -1 )
			{
				if( CurrentVert.Relationships == null )
				{
					Debug.LogError($"Relationships collectino null...");
					DBG_PathDisplay += ($"Relationships collectino null...");
				}
				if (CurrentVert.Relationships.Length <= 0)
				{
					Debug.LogError($"Relationships collectino 0 count...");
					DBG_PathDisplay += ($"Relationships collectino 0 count...");
				}
				else if ( (Index_PathDisplay_tri * 3) > CurrentVert.Relationships.Length - 3 )
				{
					Debug.LogError($"index too high...");
					DBG_PathDisplay += ($"index too high...");
				}
				else
				{
					DBG_PathDisplay += $"using==================\n" +
						$"tri{Index_PathDisplay_tri}, vert{Index_PathDisplay_vert}\n" +
						$"relationship: '{(Index_PathDisplay_tri * 3) + Index_PathDisplay_vert}' / {CurrentVert.Relationships.Length}...\n" +
						$"\n";

					LNX_VertexRelationship foundRel = CurrentVert.Relationships[(Index_PathDisplay_tri * 3) + Index_PathDisplay_vert];

					if( foundRel == null )
					{
						DBG_PathDisplay += $"found relationship is null";
						return;
					}

					if (foundRel.PathTo == null)
					{
						DBG_PathDisplay += $"found relationship is null";
						return;
					}

					LNX_Path foundPath = foundRel.PathTo;

					DBG_PathDisplay += $"Path: '{foundPath}', \n" +
						$"valid: '{foundPath.AmValid}', foundIssue: '{foundPath.FoundIssue()}'\n" +
						$"amStraight: '{foundPath.AmStraight}'\n" +
						$"count: '{foundPath.PointCount}'\n" +
						$"\n";


					if( foundPath.PointCount == 1 && 
						foundPath.EndPosition == CurrentVert.V_Position )
					{
						DBG_PathDisplay += $"SHARED!\n";
					}
					else
					{

					}

					Gizmos.color = Color.white;
					Gizmos.DrawLine(
						foundPath.EndPosition,
						foundPath.EndPosition +
						(Vector3.up * 2f)
					);

					if ( foundPath == null || !foundPath.AmValid || foundPath.FoundIssue())
					{
						Gizmos.color = Color.red;
					}
					else
					{
						Gizmos.color = Color.magenta;
					}

					foundPath.DrawMyGizmos(radius_pthPts, height_pthPts, false);

					Gizmos.DrawLine(
						_navmesh.Triangles[Index_PathDisplay_tri].Verts[Index_PathDisplay_vert].V_Position,
						_navmesh.Triangles[Index_PathDisplay_tri].Verts[Index_PathDisplay_vert].V_Position + 
						(Vector3.up * 2f)
					);
				}
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
			CurrentVert.SayCurrentInfo( _navmesh );
		}

		[ContextMenu("z call DoEet()")]
		public void DoEet()
		{


		}
		#endregion
	}
}
