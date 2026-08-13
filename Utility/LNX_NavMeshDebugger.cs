using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;


namespace LogansNavigationExtension
{
	public class LNX_NavMeshDebugger : MonoBehaviour
    {
		[SerializeField] public LNX_NavMesh _mgr;

		public LNX_ComponentGrabber Grabber_FocusTri;
		
		public LNX_ComponentGrabber Grabber_FocusEdge;
		
		public LNX_ComponentGrabber Grabber_FocusVert;

		public LNX_NavMeshData _mgrData;

		[Header("DEBUG")]
		public bool AmDebugging = true;

		[Header("FOCUS")]
		public bool AmAllowingFocus = true;
		public bool FocusExclusively = true;

		public int Index_SendFocusTriGrabberTo = 0;
		public LNX_Triangle FocusedTri => Grabber_FocusTri.CurrentlyGrabbedTriangle;
		public LNX_Edge FocusedEdge => Grabber_FocusEdge.CurrentlyGrabbedEdge;
		public LNX_Vertex FocusedVert => Grabber_FocusVert.CurrentlyGrabbedVert;

		[Header("DEBUG TRIANGLES")]
		public bool DrawTriangles = true;
		public bool DrawTriLabels = true;
		[Range(0f, 0.25f)] public float Thickness_focusTri = 0.1f;

		[Header("DEBUG EDGES")]
		public bool DrawEdges = true;
		public Color color_edgeLines = Color.white;
		public float Thickness_edges = 1f;
		[Range(0.02f, 0.5f)] public float Length_edgeLblsInward = 0.1f;
		public bool drawEdgeLabels = false;

		[Header("DEBUG VERTICES")]
		[SerializeField] private bool drawVertSpheres = false;
		public bool DrawVertLables = false;
		public Color color_vertSphere = Color.white;
		[Range(0.005f, 0.05f)] public float radius_vertSphere = 0.05f;

		[Header("DEBUG NORMALS")]
		[SerializeField] private bool drawNormalLines = false;
		public Color Color_normalLines = Color.white;
		[Range(0.05f, 1f)] public float Length_normalLines = 0.5f;

		[Header("DEBUG BOUNDS")]
		[SerializeField] private bool amDrawingBounds = false;
		public Color Color_boundsLines = Color.white;

		[Header("NAVMESH TRIANGULATION")]
		public bool OnlyNMGizmos = false;

		[Header("SAY SPECIFIED VERT RELATIONAL")]
		public LNX_ComponentCoordinate Coord_specifiedVrt_sayRelational;


		

		private void OnEnable()
		{
			Debug.Log("OE");
		}

		private void Reset()
		{
			Debug.Log("reset");
		}

		private void OnDrawGizmos()
		{
			if ( !AmDebugging || _mgr == null || _mgr.Triangles != null && _mgr.Triangles.Length <= 0 )
			{
				return;
			}

			if (FetchedRel != null && FetchedRel.PathTo != null )
			{
				//FetchedRel.PathTo.DrawMyGizmos(0.1f, 0.5f, false);
			}

			if ( FocusedTri != null )
			{
				//Debug.Log($"focustri: '{FocusedTri}'");
				//DrawTriGizmos( FocusedTri, true, true, true, true, false, true, false );
				LNX_DrawingUtils.DrawTriGizmos(FocusedTri, Color.yellow, true, true, true, Length_edgeLblsInward, true, Length_edgeLblsInward * 0.5f,
					true, Length_normalLines
				);

				Vector3 vEnd = FocusedTri.Edges[1].MidPosition + FocusedTri.Edges[1].v_Cross_flat;
				Gizmos.DrawLine(FocusedTri.Edges[1].MidPosition, 
					vEnd
				);
			}

			if( DrawTriangles )
			{
				for ( int i = 0; i < _mgr.Triangles.Length; i++ )
				{
					//DrawTriGizmos(_mgr.Triangles[i], (FocusedTri != null && i == FocusedTri.Index_inCollection) ? true : false, DrawTriLabels, 
					//drawEdgeLabels, DrawEdges, drawVertSpheres, DrawVertLables, drawNormalLines );

				
					LNX_DrawingUtils.DrawTriGizmos(_mgr.Triangles[i],
						(FocusedTri != null && i == FocusedTri.Index_inCollection) ? Color.yellow : color_edgeLines,  
						DrawTriLabels, DrawEdges, drawEdgeLabels, Length_edgeLblsInward, DrawVertLables, Length_normalLines * 0.5f,
						drawNormalLines, Length_normalLines
					);
				

					Handles.Label(_mgr.Triangles[i].V_Center, $"{i}");
				}
			}


			if( FocusedEdge != null )
			{
				DrawStandardEdgeFocusGizmos( Grabber_FocusEdge.CurrentlyGrabbedEdge, 0.25f, "", Color.yellow, true);
			}

			if( FocusedVert != null )
			{

			}

			if (amDrawingBounds && _mgr.Bounds != null && _mgr.Bounds.Length == 6)
			{
				Gizmos.color = Color_boundsLines;

				Gizmos.DrawWireCube(_mgr.V_BoundsCenter, _mgr.V_BoundsSize);
				Gizmos.DrawCube(_mgr.V_Bounds[0], Vector3.one * 5f);
				Gizmos.DrawCube(_mgr.V_Bounds[4], Vector3.one);

			}

			/*
			if (AmAllowingFocus && amFocused && Grabber_FocusVert.gameObject.activeSelf && FocusedVert != null)
			{
				Gizmos.DrawLine(FocusedVert.V_Position, FocusedVert.V_Position + (Vector3.up * 1.2f));
			}
			*/
		}

		public void DrawStandardEdgeFocusGizmos(LNX_Edge edge, float raiseAmount, string lblString, Color clr, bool incldStrtAndEndLbls = false)
		{
			Color oldColor = Gizmos.color;

			Gizmos.color = clr;
			Vector3 vRaise = Vector3.up * raiseAmount;

			Handles.Label(edge.MidPosition + vRaise, edge.ToString());

			Gizmos.DrawLine(edge.StartPosition, edge.StartPosition + vRaise);


			Gizmos.DrawLine(edge.StartPosition + vRaise, edge.EndPosition + vRaise);
			Gizmos.DrawLine(edge.EndPosition, edge.EndPosition + vRaise);

			if (incldStrtAndEndLbls)
			{
				Handles.Label(edge.StartPosition + vRaise, "eStrt");
				Handles.Label(edge.EndPosition + vRaise, "eEnd");
			}

			Gizmos.color = oldColor;
		}

		#region HELPERS ========================================
		[ContextMenu("z call RecalculateAllDerivedInfo()")]
		public void RecalculateAllDerivedInfo() //todo: dws
		{
			Debug.Log($"RecalculateAllDerivedInfo()");

			foreach (LNX_Triangle tri in _mgr.Triangles)
			{
				tri.CalculateDerivedInfo(_mgr);

				if ( tri.Index_inCollection == 43 )
				{
					//Debug.Log(tri.dbgDerived);
				}
			}
			Debug.Log($"RecalculateAllDerivedInfo finished...");
		}

		[ContextMenu("z call CalculateProximalRelationships()")]
		public void CalculateProximalRelationships()
		{
			Debug.Log($"CalculateProximalRelationships");

			for (int i = 0; i < _mgr.Triangles.Length; i++)
			{
				_mgr.Triangles[i].Verts[0].Relationships = null;
				_mgr.Triangles[i].Verts[1].Relationships = null;
				_mgr.Triangles[i].Verts[2].Relationships = null;
			}

			DateTime dt_start = DateTime.Now;
			for (int i = 0; i < _mgr.Triangles.Length; i++)
			{
				//Debug.Log($"for triangle '{i}'...");

				_mgr.Triangles[i].Verts[0].CreateRelationships(_mgr, true, true, false);
				_mgr.Triangles[i].Verts[1].CreateRelationships(_mgr, true, true, false);
				_mgr.Triangles[i].Verts[2].CreateRelationships(_mgr, true, true, false);
			}

			Debug.Log($"finished. took: '{DateTime.Now.Subtract(dt_start).TotalMilliseconds}' ms");



			/*
			Debug.Log($"now checking validity...");
			for (int i = 0; i < _mgr.Triangles.Length; i++)
			{
				Debug.Log($"checking '{_mgr.Triangles[i].Verts[0].Relationships.Length}' relationships on vert0...");
				for (int j = 0; j < _mgr.Triangles[i].Verts[0].Relationships.Length; j++)
				{
					Debug.Log($"for rel{j}...");
					Debug.Log($"valid: '{_mgr.Triangles[i].Verts[0].Relationships[j].AmValid}'");
					
				}

				_mgr.Triangles[i].Verts[0].Relationships = null;
				_mgr.Triangles[i].Verts[1].Relationships = null;
				_mgr.Triangles[i].Verts[2].Relationships = null;
			}
			*/
		}

		public LNX_VertexRelationship FetchedRel;
		[ContextMenu("z call DoEet()")]
		public void DoEet()
		{
			//FetchedRel = _mgr.Triangles[0].Verts[0].GetFurthestDistanceRelationshipOnTriangle(76);

			string filePath = Path.Combine($"{Directory.GetCurrentDirectory()}\\Assets\\LNX Testing", 
				"lnxNavmesh_expectedProximalRelationships.json");

			System.IO.File.WriteAllText(filePath, JsonUtility.ToJson(_mgr, true));

		}

		[ContextMenu("z call CompareWithProximalModel()")]
		public void CompareWithProximalModel()
		{
			string filePath = Path.Combine($"{Directory.GetCurrentDirectory()}\\Assets\\LNX Testing",
				"lnxNavmesh_expectedProximalRelationships.json");

			string myJsonString = File.ReadAllText(filePath);
			GameObject go = new GameObject();

			LNX_NavMesh newNavmesh = go.AddComponent<LNX_NavMesh>();

			JsonUtility.FromJsonOverwrite( myJsonString, newNavmesh);

			Debug.Log($"new navmesh triangle count: '{newNavmesh.Triangles.Length}', " +
			$"model triangle count: '{_mgr.Triangles.Length}'");

			if ( newNavmesh.Triangles.Length != _mgr.Triangles.Length )
			{
				Debug.LogError($"constructeed navemsh triangle count ('{newNavmesh.Triangles.Length}') different from model's " +
					$"triangle count: '{_mgr.Triangles.Length}'");
				return;
			}

			for (int i = 0; i < _mgr.Triangles.Length; ++i)
			{
				Debug.Log($"for tri{i}...");

				for( int i_vrts = 0; i_vrts < 3; i_vrts++ )
				{
					Debug.Log($"for vert{i_vrts}...");

					if
					(
						_mgr.Triangles[i].Verts[i_vrts].Relationships.Length !=
						newNavmesh.Triangles[i].Verts[i_vrts].Relationships.Length
					)
					{
						Debug.LogError($"newNavmesh.Triangles[i].Verts[i_vrts].Relationships.Length: '{newNavmesh.Triangles[i].Verts[i_vrts].Relationships.Length}' " +
							$"different from _mgr.Triangles[i].Verts[i_vrts].Relationships.Length: '{_mgr.Triangles[i].Verts[i_vrts].Relationships.Length}'");
						DestroyImmediate(go);
						return;
					}

					for( int i_rels = 0; i_rels < _mgr.Triangles[i].Verts[i_vrts].Relationships.Length; i_rels++ )
					{
						//Debug.Log($"for rel{i_rels}...");

						if ( _mgr.Triangles[i].Verts[i_vrts].Relationships[i_rels] == null )
						{
							if( !newNavmesh.Triangles[i].Verts[i_vrts].Relationships[i_rels].AmValid	)
							{
								Debug.Log($"mgr rel is null and newnavmesh rel not valid. Continuing...");
								continue;
							}
							else
							{
								Debug.Log($"Appears to be a problem.\n" +
									$"data model rel null: '{newNavmesh.Triangles[i].Verts[i_vrts].Relationships[i_rels] == null}'...\n" + 
									$"data model rel valid: '{newNavmesh.Triangles[i].Verts[i_vrts].Relationships[i_rels].AmValid}'...\n" +
									$"existing rel null: '{_mgr.Triangles[i].Verts[i_vrts].Relationships[i_rels] == null}'...");
								DestroyImmediate(go);

								return;
							}
						}

						if
						(
							!_mgr.Triangles[i].Verts[i_vrts].Relationships[i_rels].ValueEquals
							(
								newNavmesh.Triangles[i].Verts[i_vrts].Relationships[i_rels]
							)
						)
						{
							Debug.LogError($"newNavmesh.Triangles[{i}].Verts[{i_vrts}].Relationships[{i_rels}]: '{newNavmesh.Triangles[i].Verts[i_vrts].Relationships[i_rels]}' " +
								$"different from _mgr.Triangles[{i}].Verts[{i_vrts}].Relationships[{i_rels}]: '{_mgr.Triangles[i].Verts[i_vrts].Relationships[i_rels]}'");
							DestroyImmediate(go);

							return;
						}
					}
				}
			}

			DestroyImmediate(go);

			Debug.Log($"finished");
		}

		public VertexDisplayer VrtDsplr;
		[ContextMenu("z call TryRelationships()")]
		public void TryRelationships()
		{
			CalculateProximalRelationships();
			
			float timeoutAmt = 25f;

			DateTime dt_methodStart = DateTime.Now;

			Debug.Log($"now creating distal relationships...");

			for (int i = 0; i < _mgr.Triangles.Length; i++)
			{
				Debug.Log($"for tri{i} =====================/////////////////////////////////////////////////////");

				DateTime dt_relStart = DateTime.Now;

				_mgr.Triangles[i].Verts[0].CreateRelationships(_mgr, false, false, true);
				if (DateTime.Now.Subtract(dt_relStart).TotalSeconds > timeoutAmt)
				{
					Debug.Log($"timeout hit after creating all relationships for tri{i}vert{0}. Breaking...");
					break;
				}

				_mgr.Triangles[i].Verts[1].CreateRelationships(_mgr, false, false, true);
				if (DateTime.Now.Subtract(dt_relStart).TotalSeconds > timeoutAmt)
				{
					Debug.Log($"timeout hit after creating all relationships for tri{i}vert{1}. Breaking...");
					break;
				}

				_mgr.Triangles[i].Verts[2].CreateRelationships(_mgr, false, false, true);
				if( DateTime.Now.Subtract(dt_relStart).TotalSeconds >  timeoutAmt )
				{
					Debug.Log($"timeout hit after creating all relationships for tri{i}vert{2}. Breaking...");
					break;
				}
			}

			Debug.Log($"running operation on vert displayer...");
			VrtDsplr.RunOperation();

		}


		//[ContextMenu("z call CalculateDistalRelationships()")]
		public void CalculateDistalRelationships()
		{
			Debug.Log($"CalculateDistalRelationships");
			/*
			for (int i = 0; i < _mgr.Triangles.Length; i++)
			{
				_mgr.Triangles[i].Verts[0].Relationships = null;
				_mgr.Triangles[i].Verts[1].Relationships = null;
				_mgr.Triangles[i].Verts[2].Relationships = null;
			}
			*/

			float timeoutAmt = 25f;

			DateTime dt_methodStart = DateTime.Now;
			
			for (int i = 0; i < _mgr.Triangles.Length; i++)
			{
				Debug.Log($"for tri{i} ====/////////////////////////////////////////////////////////");

				DateTime dt_relStart = DateTime.Now;
				
				_mgr.Triangles[i].Verts[0].CreateRelationships(_mgr, false, false, true );
				

				Debug.Log($"end of CreateRelationships for vert0. Elapsed time: '{DateTime.Now.Subtract(dt_relStart)}'...");
				if (DateTime.Now.Subtract(dt_methodStart).TotalSeconds > timeoutAmt)
				{
					Debug.Log($"time limit exceeded. Breaking at tri{i}, vert{0}...");
					break;
				}

				return;

				FetchedRel = _mgr.Triangles[0].Verts[0].GetFurthestDistanceRelationshipOnTriangle(76);


				dt_relStart = DateTime.Now;
				_mgr.Triangles[i].Verts[1].CreateRelationships(_mgr, false, false, true );
				Debug.Log($"end of CreateRelationships for vert1. Elapsed time: '{DateTime.Now.Subtract(dt_relStart)}'...");
				if (DateTime.Now.Subtract(dt_methodStart).TotalSeconds > timeoutAmt)
				{
					Debug.Log($"time limit exceeded. Breaking at tri{i}, vert{1}...");
					break;
				}

				dt_relStart = DateTime.Now;
				_mgr.Triangles[i].Verts[2].CreateRelationships(_mgr, false, false, true );
				Debug.Log($"end of CreateRelationships for vert2. Elapsed time: '{DateTime.Now.Subtract(dt_relStart)}'...");
				if ( DateTime.Now.Subtract(dt_methodStart).TotalSeconds > timeoutAmt )
				{
					Debug.Log($"time limit exceeded. Breaking at tri{i}, vert{2}...");
					break;
				}

				break;
			}
			

			Debug.Log($"method finished. Elapsed time: '{DateTime.Now.Subtract(dt_methodStart)}'");
		}

		//[ContextMenu("z call RecreateAllRelationships()")]
		public void RecreateAllRelationships()
		{
			Debug.Log($"RecreateAllRelationships");

			DateTime dt_start = DateTime.Now;

			for (int i = 0; i < _mgr.Triangles.Length; i++)
			{
				_mgr.Triangles[i].Verts[0].Relationships = null;
				_mgr.Triangles[i].Verts[1].Relationships = null;
				_mgr.Triangles[i].Verts[2].Relationships = null;

				if (DateTime.Now.Subtract(dt_start).TotalSeconds > 20f)
				{
					Debug.Log($"time limit exceeded. Breaking...");
					return;
				}
			}

			for (int i = 0; i < _mgr.Triangles.Length; i++)
			{
				_mgr.Triangles[i].Verts[0].CreateRelationships(_mgr, true, true, false);
				_mgr.Triangles[i].Verts[1].CreateRelationships(_mgr, true, true, false);
				_mgr.Triangles[i].Verts[2].CreateRelationships(_mgr, true, true, false);

				if (DateTime.Now.Subtract(dt_start).TotalSeconds > 20f)
				{
					Debug.Log($"time limit exceeded. Breaking...");
					return;
				}
			}

			for (int i = 0; i < _mgr.Triangles.Length; i++)
			{
				_mgr.Triangles[i].Verts[0].CreateRelationships(_mgr, false, false, true);
				_mgr.Triangles[i].Verts[1].CreateRelationships(_mgr, false, false, true);
				_mgr.Triangles[i].Verts[2].CreateRelationships(_mgr, false, false, true);

				if (DateTime.Now.Subtract(dt_start).TotalSeconds > 20f)
				{
					Debug.Log($"time limit exceeded. Breaking...");
					return;
				}
			}

			Debug.Log($"finished. Elapsed time: '{DateTime.Now.Subtract(dt_start)}'");

		}

		//[Header("VERT MANIPULATION")]
		/*
		[ContextMenu("z call CalculateAllDerived()")]
		public void CalculateAllDerived()
		{
			Debug.Log($"{nameof(CalculateAllDerived)}");

			for ( int i = 0; i < _mgr.Triangles.Length; i++ )
			{
				_mgr.Triangles[i].CalculateDerivedInfo();
			}
		}
		*/

		[ContextMenu("z call SayFocusedTriInfo()")]
		public void SayFocusedTriInfo()
		{
			FocusedTri.SayCurrentInfo(_mgr);
		}


		[ContextMenu("z call SayFocusedVertRelational()")]
		public void SayFocusedVertRelational()
		{
			FocusedVert.SayAllRelationships();
		}

		[ContextMenu("z call SaySpecifieddVertRelational()")]
		public void SaySpecifieddVertRelational()
		{
			_mgr.Triangles[Coord_specifiedVrt_sayRelational.TrianglesIndex].Verts[Coord_specifiedVrt_sayRelational.ComponentIndex].
				SayAllRelationships();
		}

		[ContextMenu("z call SayNavMeshInfo()")]
		public void SayNavMeshInfo()
		{
			string s = $"Tri count: '{_mgr.Triangles.Length}' \n";



			Debug.Log(s);
		}

		[ContextMenu("z call SayVisualMeshInfo()")]
		public void SayVisualMeshInfo()
		{
			string s = $"Vertices '{_mgr._VisualizationMesh.vertices.Length}' \n";

			for (int i = 0; i < _mgr._VisualizationMesh.vertices.Length; i++)
			{
				s += $"vert pos {i}: '{_mgr._VisualizationMesh.vertices[i]}'\n";
			}

			s += $"\nNormals '{_mgr._VisualizationMesh.normals.Length}' \n";

			for (int i = 0; i < _mgr._VisualizationMesh.normals.Length; i++)
			{
				s += $"normal {i}: '{_mgr._VisualizationMesh.normals[i]}'\n";
			}

			Debug.Log(s);
		}

		[ContextMenu("z call SayBoundsInfo()")]
		public void SayBoundsInfo()
		{
			string s = $"\n";

			s += $"lowX: '{_mgr.Bounds[0]}', highX: '{_mgr.Bounds[1]}'\n" +
				$"lowY: '{_mgr.Bounds[2]}', highY: '{_mgr.Bounds[3]}'\n" +
				$"lowZ: '{_mgr.Bounds[4]}', highZ: '{_mgr.Bounds[5]}'\n" +
				$"V_BoundsSize: '{LNX_UnitTestUtilities.LongVectorString(_mgr.V_BoundsSize)}' \n" +
				$"V_BoundsCenter: '{LNX_UnitTestUtilities.LongVectorString(_mgr.V_BoundsCenter)}'";

			Debug.Log(s);
		}

		[ContextMenu("z call SayRelationshipsCount")]
		public void SayRelationshipsCount()
		{
			int relCount = 0;

			for (int i = 0; i < _mgr.Triangles.Length; i++)
			{
				for (int i_vrts = 0; i_vrts < 3; i_vrts++)
				{
					if (_mgr.Triangles[i].Verts[i_vrts].Relationships != null && _mgr.Triangles[i].Verts[i_vrts].Relationships.Length > 0)
					{
						for (int i_rels = 0; i_rels < _mgr.Triangles[i].Verts[i_vrts].Relationships.Length; i_rels++)
						{
							if (_mgr.Triangles[i].Verts[i_vrts].Relationships[i_rels].PathTo != null)
							{
								relCount++;
							}
						}
					}
				}
			}

			Debug.Log(relCount);
		}

		[ContextMenu("z call SendGrabberToFocusTri()")]
		public void SendGrabberToFocusTri()
		{
			Grabber_FocusTri.transform.position = _mgr.Triangles[Index_SendFocusTriGrabberTo].V_Center;
		}

		#endregion

	}
}