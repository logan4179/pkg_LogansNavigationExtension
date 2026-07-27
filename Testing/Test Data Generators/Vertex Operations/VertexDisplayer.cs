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
		[Range(0, 2)] public int Index_PathDisplay_vert = -1;

		public VertDisp_Relationship[] DisplayRelationships;
		public LNX_VertexRelationship[] ActualRelationships;
		public LNX_ComponentCoordinate[] SharedVertexCoordinates;

		[Header("DEBUG")]
		[Range(0f, 0.5f)] public float radius_pthPts = 0.1f;
		[Range(0f, 1f)] public float height_pthPts = 0.5f;


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

			DisplayRelationships = new VertDisp_Relationship[CurrentVert.Relationships.Length];
			ActualRelationships = new LNX_VertexRelationship[CurrentVert.Relationships.Length];
			int validCount = 0;
			int invalidCount = 0;
			for (int i = 0; i < CurrentVert.Relationships.Length; i++)
			{
				DisplayRelationships[i] = new VertDisp_Relationship( CurrentVert.Relationships[i] );
				ActualRelationships[i] = CurrentVert.Relationships[i];

				if(DisplayRelationships[i].AmValid )
				{
					validCount++;
				}
				else
				{
					invalidCount++;
				}
			}

			SharedVertexCoordinates = new LNX_ComponentCoordinate[CurrentVert.SharedVertexCoordinates.Length];
			for (int i = 0; i < CurrentVert.SharedVertexCoordinates.Length; i++)
			{
				SharedVertexCoordinates[i] = CurrentVert.SharedVertexCoordinates[i];
			}

			DBG_Operation += $"End of operation\n" +
				$"Relationships: '{DisplayRelationships.Length}'. valid: '{validCount}', invalid: '{invalidCount}'\n" +
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

			DBG_PathDisplay = $"Recalculating at: '{DateTime.Now}'...\n\n";


			if ( Index_PathDisplay_tri > -1 )
			{
				if( DisplayRelationships == null ||  DisplayRelationships.Length <= 0 )
				{
					Debug.LogError($"Relationships collectino null or 0 count...");
					DBG_PathDisplay += ($"Relationships collectino null or 0 count...");

				}
				else if ( (Index_PathDisplay_tri * 3) > DisplayRelationships.Length - 3 )
				{
					Debug.LogError($"index too high...");
					DBG_PathDisplay += ($"index too high...");
				}
				else
				{
					DBG_PathDisplay += $"using==================\n" +
						$"tri{Index_PathDisplay_tri}, vert{Index_PathDisplay_vert}\n" +
						$"relationship: '{(Index_PathDisplay_tri * 3) + Index_PathDisplay_vert}' / {ActualRelationships.Length}...\n" +
						$"\n";

					VertDisp_Relationship foundRel = DisplayRelationships[(Index_PathDisplay_tri * 3) + Index_PathDisplay_vert];
					LNX_Path foundPath = foundRel.Rel.PathTo;

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

					if ( !foundPath.AmValid || foundPath.FoundIssue())
					{
						Gizmos.color = Color.red;
					}
					else
					{
						Gizmos.color = Color.green;
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

		[ContextMenu("z call CheckIFPathShouldBeStraight()")]
		public void CheckIFPathShouldBeStraight()
		{
			string rprt = $"CheckIFPathShouldBeStraight()\n";

			VertDisp_Relationship foundRel = DisplayRelationships[(Index_PathDisplay_tri * 3) + Index_PathDisplay_vert];
			LNX_Path foundPath = foundRel.Rel.PathTo;
			rprt += ($"{foundPath}\n" +
				$"amStraight (cached): '{foundPath.AmStraight}'\n" +
				$"V_CrowFiles_flat: '{foundPath.V_CrowFiles_flat}'\n" +
				$"count: '{foundPath.PathPoints.Count}'...\n");
			if (foundPath.PathPoints.Count > 1)
			{
				Vector3 firstDir_fltnd = LNX_Utils.FlatVector(
					foundPath.PathPoints[1].Position - foundPath.PathPoints[0].Position, 
					foundPath.V_navmeshSurfaceProjection_cached
				).normalized;

				rprt += ($"firstDir_fltnd: '{firstDir_fltnd}'\n\n");
				bool amStraight = false;
				for ( int i = 0; i < foundPath.PathPoints.Count - 1; i++ )
				{
					rprt += $"for{i}...\n";

					Vector3 dirToNext = LNX_Utils.FlatVector(
						foundPath.PathPoints[i+1].Position - foundPath.PathPoints[i].Position,
						foundPath.V_navmeshSurfaceProjection_cached
						).normalized;

					rprt += ($"dirToNext: '{dirToNext}'\n" +
						$"dirNew != firstDir_fltnd: '{dirToNext != firstDir_fltnd}'\n" +
						$"ang: '{Vector3.Angle(firstDir_fltnd, dirToNext)}'\n");

					if (Vector3.Angle(firstDir_fltnd, dirToNext) > 0f)
					{
						rprt += $"decided NOT same<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<!!!!!!\n";
						amStraight = false;
					}
					else
					{
						rprt += "decided same\n";
					}
				}

				rprt += $"end. amStraight: '{amStraight}'\n";
			}

			Debug.Log( rprt );
			Debug.Log($"rprt:\n" +
				$"dbgclass: {foundPath.DBG_class}");
		}

		[ContextMenu("z call DoEet()")]
		public void DoEet()
		{
			VertDisp_Relationship foundRel = DisplayRelationships[(Index_PathDisplay_tri * 3) + Index_PathDisplay_vert];
			LNX_Path foundPath = foundRel.Rel.PathTo;

			Debug.Log( $"BEFORE! {foundPath}\n" +
				$"amStraight (cached): '{foundPath.AmStraight}'\n" +
				$"count: '{foundPath.PathPoints.Count}'\n");

			foundPath.AddPoint( new LNX_NavmeshHit(_navmesh.Triangles[22].Verts[1]) );
			Debug.Log($"after! {foundPath}\n" +
				$"amStraight (cached): '{foundPath.AmStraight}'\n" +
				$"count: '{foundPath.PathPoints.Count}'\n");


			Vector3 newVect = new Vector3(1f, 2f, 3f);
			Debug.Log($"BEFORE! '{newVect}'\n");

			newVect.x = 10f;
			newVect.y = 20f;
			
			Debug.Log($"after! '{newVect}'\n");

		}
		#endregion
	}

	[System.Serializable]
	public struct VertDisp_Relationship
	{
		public bool AmValid;
		public LNX_VertexRelationship Rel;

		public VertDisp_Relationship(LNX_VertexRelationship rel )
		{
			AmValid = rel.AmValid;
			Rel = rel;
		}
	}
}
