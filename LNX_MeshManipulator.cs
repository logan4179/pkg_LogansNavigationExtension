using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.PlayerSettings;

namespace LogansNavigationExtension
{
    public class LNX_MeshManipulator : MonoBehaviour
    {
		[SerializeField] public LNX_NavMesh _LNX_NavMesh;

		//[Header("FOCUS")]
		public LNX_SelectMode SelectMode = LNX_SelectMode.None;

		public LNX_OperationMode OperationMode = LNX_OperationMode.Pointing;

		public bool AmMagnetized = false;

		public bool MeshIsValidForDrawing
		{
			get
			{
				return drawMeshVisualization && _LNX_NavMesh._Mesh != null && _LNX_NavMesh._Mesh.vertices != null && _LNX_NavMesh._Mesh.vertices.Length > 0;

			}
		}

		#region TRIANGLES ------------------------
		/// <summary>
		/// This is the 'primary' or last selected triangle
		/// </summary>
		public int Index_TriLastSelected = 0;
		public int Index_TriPointingAt = -1;
		/// <summary>Use this list for when only the triangles selected matter, as opposed to the edges or vertices contained </summary>
		public List<int> indices_selectedTris;

		public List<int> indices_lockedTris;

		public LNX_Triangle LastSelectedTri
		{
			get
			{
				if ( _LNX_NavMesh.Triangles == null || Index_TriLastSelected < 0 || Index_TriLastSelected > _LNX_NavMesh.Triangles.Length - 1 ) //todo: dws
				{
					return null;
				}
				else
				{
					return _LNX_NavMesh.Triangles[Index_TriLastSelected];
				}
			}
		}

		public LNX_Triangle PointingAtTri
		{
			get
			{
				if ( _LNX_NavMesh.Triangles == null || Index_TriPointingAt < 0 || Index_TriPointingAt > _LNX_NavMesh.Triangles.Length - 1 )
				{
					return null;
				}
				else
				{
					return _LNX_NavMesh.Triangles[Index_TriPointingAt];
				}
			}
		}

		public bool HaveTrisSelected
		{
			get
			{
				return indices_selectedTris != null && indices_selectedTris.Count > 0;
			}
		}
		#endregion

		#region VERTICES ---------------------------
		public LNX_Vertex Vert_LastSelected;
		public LNX_Vertex Vert_CurrentlyPointingAt;
		/// <summary>
		/// List of vertices that are currently selected. Note: does NOT have vertices sharing the same position. Only unique positions.
		/// </summary>
		public List<LNX_Vertex> Verts_currentlySelected;

		public bool HaveVertsSelected
		{
			get
			{
				return Verts_currentlySelected != null && Verts_currentlySelected.Count > 0;
			}
		}
		#endregion

		#region EDGES ---------------------------
		public LNX_Edge Edge_LastSelected;
		public LNX_Edge Edge_CurrentlyPointingAt;
		/// <summary>
		/// List of vertices that are currently selected. Note: does NOT have vertices sharing the same position. Only unique positions.
		/// </summary>
		public List<LNX_Edge> Edges_currentlySelected;
		#endregion

		#region DEBUGGING ----------------------
		[SerializeField] private bool drawTriLabels = true;
		[SerializeField] private bool drawMeshVisualization = true;
		[SerializeField] private Color color_selectedComponent;

		[SerializeField] private bool drawNavMeshLines = true;
		[SerializeField] private Color color_edgeLines;
		[SerializeField] private Color color_modifiedTri;

		[Range(0f, 8f)] public float thickness_edges = 0.2f;
		[SerializeField] private bool drawEdgeLabels = false;

		[Range(0f, 8f)] public float Size_SelectedComponent = 0.2f;
		[Range(0f, 8f)] public float Size_FocusedComponent = 0.2f;
		[Range(0f, 8f)] public float Size_HoveredComponent = 0.2f;
		[SerializeField] private Color color_lockedTri = Color.white;
		[Range(0f, 8f)] public float Thickness_LockedTriEdge = 0.2f;

		#endregion

		//[Header("FLAGS")]
		/// <summary>Tells whether the mouse is currently considered to be pointing at a selectable component. Gets set 
		/// in the trypoint method</summary>
		public bool Flag_AComponentIsCurrentlyHighlighted = false;

		/// <summary>
		/// Tells whether am in 'Locked' state, where you can only select verts or edges that are part of 
		/// already locked triangle selection.
		/// </summary>
		bool amLocked = false;
		/// <summary>
		/// Tells whether am in 'Locked' state, where you can only select verts or edges that are part of 
		/// already locked triangle selection.
		/// </summary>
		public bool Flag_AmLocked => amLocked;

		#region MESH MANIPULATOR EDITOR SCRIPT STUFF -----------------------------------------------
		public Vector3 manipulatorPos = Vector3.zero;
		public Vector3 v_lastSnapPos = Vector3.zero;
		public bool amSnapped = false;

		#endregion

		#region CLEARING/INITIALIZING ------------------------
		public void InitState()
		{
			ClearSelection();

			OperationMode = LNX_OperationMode.Pointing;

			amLocked = false;

			indices_lockedTris = new List<int>();
		}

		public void ClearSelection()
		{
			Debug.Log($"{nameof(ClearSelection)}()");

			ClearTris();
			ClearVerts();
			ClearEdges();

			Flag_AComponentIsCurrentlyHighlighted = false;
		}

		void ClearTris()
		{
			Index_TriPointingAt = -1;
			Index_TriLastSelected = -1;
			indices_selectedTris = new List<int>();
		}

		void ClearVerts()
		{
			Verts_currentlySelected = new List<LNX_Vertex>();
			Vert_LastSelected = null;
			Vert_CurrentlyPointingAt = null;
		}

		void ClearEdges()
		{
			Edges_currentlySelected = new List<LNX_Edge>();
			Edge_LastSelected = null;
			Edge_CurrentlyPointingAt = null;
		}
		#endregion

		public void ChangeSelectMode( LNX_SelectMode mode )
		{
			if ( mode != SelectMode || mode == LNX_SelectMode.None ) //selection has changed to this... also, added selectmode == none check to allow a way to force initialization if necessary.
			{
				ClearSelection();
			}

			if ( mode == LNX_SelectMode.None )
			{
				
			}
			else if ( mode == LNX_SelectMode.Vertices )
			{
				if( mode != SelectMode ) //selection has changed to this...
				{

				}
			}
			else if ( mode == LNX_SelectMode.Edges )
			{

			}

			SelectMode = mode;
		}

		[TextArea(1, 5)] public string DBG_Magnetized;

		[TextArea(1,5)] public string DBG_locked;
		[TextArea(1, 8)] public string DbgClass;

		public void FlipLocked()
		{
			bool newLockState = !amLocked;

			if ( newLockState == true )
			{
				indices_lockedTris = indices_selectedTris;
				indices_selectedTris = new List<int>();
				ClearVerts();
				ClearEdges();

			}
			else
			{
				indices_lockedTris = new List<int>();
			}

			amLocked = newLockState;
		}

		[ContextMenu("z call SayIfSnapped")]
		public void SayIfSnapped()
		{
			Debug.Log(amSnapped);
		}

		public void TryPointAtBounds()
		{
			//todo: in the future, I need to make something that can detect if I'm pointing at
			//the bounds first before detecting if pointing at component for efficiency...
		}

		[SerializeField, TextArea(0,10)] private string DebugPointAt;
		public void TryPointAtComponentViaDirection( Vector3 vPerspective, Vector3 vDirection )
		{
			DebugPointAt = "";

			Flag_AComponentIsCurrentlyHighlighted = false;

			if ( SelectMode == LNX_SelectMode.Faces )
			{
				Index_TriPointingAt = -1;

				float runningBestAlignment = 0.9f;

				for ( int i = 0; i < _LNX_NavMesh.Triangles.Length; i++ )
				{
					Vector3 vTo = Vector3.Normalize(_LNX_NavMesh.Triangles[i].V_Center - vPerspective);
					float alignment = Vector3.Dot(vTo, vDirection);

					if ( alignment > runningBestAlignment )
					{
						DebugPointAt += $"tri: [{i}]. alignment: '{alignment}'\n";

						runningBestAlignment = alignment;
						Index_TriPointingAt = i;
						DebugPointAt += $"{nameof(Index_TriPointingAt)}: '{Index_TriPointingAt}'\n";
					}
				}

				if ( Index_TriPointingAt > -1 )
				{
					Flag_AComponentIsCurrentlyHighlighted = true;
				}
			}
			else if ( SelectMode == LNX_SelectMode.Vertices )
			{
				float runningBestAlignment = 0.998f;
				int runningBestTriIndex = -1;
				int runningBestVertIndex = -1;

				for ( int i = 0; i < _LNX_NavMesh.Triangles.Length; i++ )
				{
					if ( amLocked && !indices_lockedTris.Contains(i) )
					{
						continue;
					}

					for ( int j = 0; j < 3; j++ )
					{
						Vector3 vTo = Vector3.Normalize( _LNX_NavMesh.GetVertexAtCoordinate(i,j).V_Position - vPerspective );
						float alignment = Vector3.Dot( vTo, vDirection );

						if ( alignment > runningBestAlignment )
						{
							DebugPointAt += $"vert: [{i}][{j}]. alignment: '{alignment}'\n";
							runningBestAlignment = alignment;
							runningBestTriIndex = i;
							runningBestVertIndex = j;
						}
					}
				}

				if( runningBestTriIndex > -1 )
				{
					Flag_AComponentIsCurrentlyHighlighted = true;
				}
				else
				{
					Vert_CurrentlyPointingAt = null;

				}

				Vert_CurrentlyPointingAt = _LNX_NavMesh.GetVertexAtCoordinate( runningBestTriIndex, runningBestVertIndex );
			}
			else if ( SelectMode == LNX_SelectMode.Edges )
			{
				LNX_ComponentCoordinate runningBestCoordinate = LNX_ComponentCoordinate.None;
				float runningBestAlignment = 0.97f;

				for ( int i_tris = 0; i_tris < _LNX_NavMesh.Triangles.Length; i_tris++ )
				{
					if ( amLocked && !indices_lockedTris.Contains(i_tris) )
					{
						continue;
					}

					for ( int i_edges = 0; i_edges < 3; i_edges++ )
					{
						Vector3 vTo = Vector3.Normalize(
							_LNX_NavMesh.Triangles[i_tris].Edges[i_edges].MidPosition - vPerspective
						);

						float alignment = Vector3.Dot( vTo, vDirection );
						/*
						if( i_tris == 82 && i_edges == 2 )
						{
							DebugPointAt += $"[forced]: alignment: '{alignment}'";
						}*/

						if ( alignment > runningBestAlignment )
						{
							DebugPointAt += $"edge: [{i_tris}][{i_edges}]. alignment: '{alignment}'\n";
							runningBestAlignment = alignment;

							runningBestCoordinate = _LNX_NavMesh.Triangles[i_tris].Edges[i_edges].MyCoordinate;
						}
					}
				}

				Edge_CurrentlyPointingAt = null;

				if ( runningBestCoordinate != LNX_ComponentCoordinate.None )
				{
					Flag_AComponentIsCurrentlyHighlighted = true;
					Edge_CurrentlyPointingAt = _LNX_NavMesh.GetEdgeAtCoordinate( runningBestCoordinate );
					//Debug.Log($"running best: '{runningBestCoordinate}', getting edge: '{Edge_CurrentlyPointingAt.MyCoordinate}'");
				}
				else
				{
					Edge_CurrentlyPointingAt = null;
					//Debug.Log("null");
				}
			}
		}

		[SerializeField, TextArea(0, 5)] private string DebugSelectedReport;
		public void TryGrab( bool amHoldingAddInputModifier = false )
		{
			DebugSelectedReport = $"Mode: '{SelectMode}', mod: '{amHoldingAddInputModifier}'\n";

			if ( !Flag_AComponentIsCurrentlyHighlighted )
			{
				ClearSelection();
				return;
			}

			if ( SelectMode == LNX_SelectMode.Faces )
			{
				DebugSelectedReport += $"pointing at: '{Index_TriPointingAt}' \n";
				Index_TriLastSelected = Index_TriPointingAt;

				if( !amHoldingAddInputModifier )
				{
					indices_selectedTris = new List<int>() { Index_TriPointingAt };
				}
				else if( indices_selectedTris.Contains(Index_TriPointingAt) ) //remove from selection...
				{
					indices_selectedTris.Remove( Index_TriPointingAt );
				}
				else if ( !indices_selectedTris.Contains(Index_TriPointingAt) ) //add to selection...
				{
					indices_selectedTris.Add( Index_TriPointingAt );
				}

				#region construct vertices list -----------------------------------------
				Verts_currentlySelected = new List<LNX_Vertex>();
				for ( int i = 0; i < indices_selectedTris.Count; i++ )
				{
					for ( int j = 0; j < 3; j++ )
					{
						Verts_currentlySelected.Add(_LNX_NavMesh.Triangles[indices_selectedTris[i]].Verts[j] );
					}
				}
				#endregion

				manipulatorPos = _LNX_NavMesh.Triangles[Index_TriLastSelected].V_Center;
			}
			else if( SelectMode == LNX_SelectMode.Vertices )
			{
				DebugSelectedReport += $"pointing at: '{Vert_CurrentlyPointingAt.ToString()}' \n";
				Vert_LastSelected = Vert_CurrentlyPointingAt;

				if ( !amHoldingAddInputModifier )
				{
					Verts_currentlySelected = new List<LNX_Vertex>() { Vert_CurrentlyPointingAt };
				}
				else if ( Verts_currentlySelected.Contains(Vert_CurrentlyPointingAt) ) //remove from selection...
				{
					Verts_currentlySelected.Remove( Vert_CurrentlyPointingAt );
				}
				else if ( !Verts_currentlySelected.Contains(Vert_CurrentlyPointingAt) ) //add to selection...
				{
					Verts_currentlySelected.Add( Vert_CurrentlyPointingAt );
				}

				manipulatorPos = Vert_LastSelected.V_Position;
			}
			else if( SelectMode == LNX_SelectMode.Edges )
			{
				DebugSelectedReport += $"pointing at: '{Edge_CurrentlyPointingAt.ToString()}' \n";
				Edge_LastSelected = Edge_CurrentlyPointingAt; //note: something's funny with edge selection, particularly when selecting a terminal/border edge...
				
				LNX_Edge sharedEdge = amLocked ? null : _LNX_NavMesh.GetEdgeAtCoordinate( Edge_CurrentlyPointingAt.SharedEdgeCoordinate );

				if ( !amHoldingAddInputModifier )
				{
					Edges_currentlySelected = new List<LNX_Edge>() { Edge_CurrentlyPointingAt };

					if ( sharedEdge != null )
					{
						Edges_currentlySelected.Add( sharedEdge );
					}
					DebugSelectedReport += "selected one...\n";
				}
				else
				{
					if ( Edges_currentlySelected.Contains(Edge_CurrentlyPointingAt) || Edges_currentlySelected.Contains(sharedEdge) )
					{
						Edges_currentlySelected.Remove( Edge_CurrentlyPointingAt );

						if( sharedEdge != null )
						{
							Edges_currentlySelected.Remove( sharedEdge );
						}

						DebugSelectedReport += "removed selected and/or shared from selection\n";
					}
					else
					{
						if( !Edges_currentlySelected.Contains(Edge_CurrentlyPointingAt) )
						{
							Edges_currentlySelected.Add( Edge_CurrentlyPointingAt );
							DebugSelectedReport += $"added pointingat at '{Edge_CurrentlyPointingAt.MyCoordinate}' to selection\n";

						}

						if ( sharedEdge != null && !Edges_currentlySelected.Contains(sharedEdge) )
						{
							Edges_currentlySelected.Add( sharedEdge );
							DebugSelectedReport += $"added shared at '{sharedEdge.MyCoordinate}' to selection\n";

						}
					}
				}

				#region construct vertices list -----------------------------------------
				Verts_currentlySelected = new List<LNX_Vertex>();
				DebugSelectedReport += $"generating initial vertices list looking through '{Edges_currentlySelected.Count}' edges...\n";
				for ( int i = 0; i < Edges_currentlySelected.Count; i++ )
				{
					DebugSelectedReport += $"edge: {i} ({Edges_currentlySelected[i].MyCoordinate})...\n";
					Verts_currentlySelected.Add( _LNX_NavMesh.GetVertexAtCoordinate(Edges_currentlySelected[i].StartVertCoordinate) );
					Verts_currentlySelected.Add( _LNX_NavMesh.GetVertexAtCoordinate(Edges_currentlySelected[i].EndVertCoordinate) );
					DebugSelectedReport += "end\n";
				}
				#endregion

				manipulatorPos = Edge_LastSelected.MidPosition;
			}

			#region add shared vertices -----------------------------------------
			for ( int i = 0; i < Verts_currentlySelected.Count; i++ )
			{
				DebugSelectedReport += $"now checking for additional verts. Shared collection is showing '{Verts_currentlySelected[i].SharedVertexCoordinates.Length}'...\n";
				for ( int j = 0; j < Verts_currentlySelected[i].SharedVertexCoordinates.Length; j++ )
				{
					DebugSelectedReport += $"trying {Verts_currentlySelected[i].SharedVertexCoordinates[j].ToString()}... ";

					if( amLocked && !indices_lockedTris.Contains(Verts_currentlySelected[i].SharedVertexCoordinates[j].TrianglesIndex) )
					{
						continue;
					}

					if ( !Verts_currentlySelected.Contains(_LNX_NavMesh.GetVertexAtCoordinate(Verts_currentlySelected[i].SharedVertexCoordinates[j])) )
					{
						DebugSelectedReport += $"added\n";
						Verts_currentlySelected.Add( _LNX_NavMesh.GetVertexAtCoordinate(Verts_currentlySelected[i].SharedVertexCoordinates[j]) );
					}
					else
					{
						DebugSelectedReport += $"NOT added/already contained...\n";
					}
				}
			}
			/*
			if( Verts_currentlySelected.Count == 1 )
			{
				Debug.Log($"Have '{Verts_currentlySelected.Count}' verts currently selected at '{Vert_CurrentlyPointingAt.MyCoordinate}'...");
			}
			else
			{
				Debug.Log($"Have '{Verts_currentlySelected.Count}' verts currently selected...");
			}*/
			#endregion
		}

		#region MESH MANIPULATION -----------------------------------------------------
		public void MoveSelectedVerts( Vector3 endPos )
		{
			string dbgMoveSelected = $"{nameof(MoveSelectedVerts)}('{endPos}') on '{Verts_currentlySelected.Count}' verts...\n";
			Vector3 vDiff = endPos - manipulatorPos;
			dbgMoveSelected += $"vdiff: '{vDiff}', manip pos: '{manipulatorPos}'\n";

			if ( Verts_currentlySelected != null && Verts_currentlySelected.Count > 0 )
			{
				foreach ( LNX_Vertex vrt in Verts_currentlySelected )
				{
					_LNX_NavMesh.MoveVert_managed( vrt, vDiff );

					/*_LNX_NavMesh.Triangles[vrt.MyCoordinate.TriIndex].MoveVert_managed(
						_LNX_NavMesh, vrt.MyCoordinate.ComponentIndex, vDiff
					);*/
				}
			}

			manipulatorPos = endPos; //Note: This isn't totally necessary now with the way I'm doing movement inside the 
			//editor script, but if I take this away, it screws up the movement unit tests.

			//Debug.Log( dbgMoveSelected );
		}

		public void DeleteSelectedTriangles()
		{
			if( indices_selectedTris == null || indices_selectedTris.Count <= 0 )
			{
				Debug.LogWarning("LNX WARNING! Tried to delete, but no triangles are selected.");
				return;
			}
			
			LNX_Triangle[] tris = new LNX_Triangle[indices_selectedTris.Count];
			for( int i = 0; i < tris.Length; i++ )
			{
				tris[i] = _LNX_NavMesh.Triangles[ indices_selectedTris[i] ];
			}

			DeleteTriangles( tris );
		}

		private void DeleteTriangles( params LNX_Triangle[] tris )
		{
			_LNX_NavMesh.DeleteTriangles( tris );

			ClearSelection();
		}

		public Vector3 dbg_edge0_start, dbg_edge0_mid, dbg_edge0_end;
		public Vector3 dbg_edge1_start, dbg_edge1_mid, dbg_edge1_end;

		public Vector3 dbg_bridgeMid0, dbg_bridgeMid1;

		[ContextMenu("z call TryInsertLoop")]
		public void TryInsertLoop()
		{
			#region check if I even can insert loop with current edge selection---------------------------------------------------------
			if (Edges_currentlySelected.Count != 2)
			{
				//Debug.Log("can't");
				return;
			}

			LNX_Triangle tri0 = _LNX_NavMesh.GetTriangle(Edges_currentlySelected[0].MyCoordinate);
			LNX_Triangle tri1 = _LNX_NavMesh.GetTriangle(Edges_currentlySelected[1].MyCoordinate);

			//Debug.Log($"edge0: '{Edges_currentlySelected[0].MyCoordinate}', edge1: '{Edges_currentlySelected[1].MyCoordinate}'");

			if (
				tri0.Relationships[tri1.Index_inCollection].GetNumberOfSharedVerts() != 2 ||
				!Edges_currentlySelected[0].AmTerminal || !Edges_currentlySelected[1].AmTerminal ||
				Edges_currentlySelected[0].AmTouching(Edges_currentlySelected[1])
			)
			{
				//Debug.Log("can't");
				return;
			}
			#endregion

			#region Find the "Bridge" edges ---------------------
			// Note: these are the edges that will be at the start and end of the bridge
			LNX_Edge endEdge0 = null;
			LNX_Edge endEdge1 = null;
			for ( int i_edges = 0; i_edges < 3; i_edges++ )
			{
				if ( tri0.Edges[i_edges] != Edges_currentlySelected[0] && tri0.Edges[i_edges].SharedEdgeCoordinate.TrianglesIndex != tri1.Index_inCollection )
				{
					endEdge0 = new LNX_Edge(_LNX_NavMesh.GetEdgeAtCoordinate(tri0.Edges[i_edges].SharedEdgeCoordinate));
					//Debug.Log($"Found edge index 1 at '{endEdge0}'");
				}
				else if (tri1.Edges[i_edges] != Edges_currentlySelected[1] && tri1.Edges[i_edges].SharedEdgeCoordinate.TrianglesIndex != tri0.Index_inCollection)
				{
					endEdge1 = new LNX_Edge(_LNX_NavMesh.GetEdgeAtCoordinate(tri1.Edges[i_edges].SharedEdgeCoordinate));
					//Debug.Log($"Found edge index 2 at '{endEdge1}'");
				}
			}

			dbg_edge0_start = endEdge0.StartPosition;
			dbg_edge0_mid = endEdge0.MidPosition;
			dbg_edge0_end = endEdge0.EndPosition;

			dbg_edge1_start = endEdge1.StartPosition;
			dbg_edge1_mid = endEdge1.MidPosition;
			dbg_edge1_end = endEdge1.EndPosition;
			#endregion

			Vector3 v_midPoint0 = Edges_currentlySelected[0].MidPosition;
			Vector3 v_midPoint1 = Edges_currentlySelected[1].MidPosition;

			dbg_bridgeMid0 = v_midPoint0;
			dbg_bridgeMid1 = v_midPoint1;

			Debug.Log("decided CAN cut. trying...");

			DeleteTriangles(tri0, tri1);

			LNX_Triangle[] trisToAdd = new LNX_Triangle[4]
			{
				new LNX_Triangle(_LNX_NavMesh.Triangles.Length, 0, endEdge0.StartPosition, endEdge0.EndPosition, v_midPoint0, _LNX_NavMesh ),
				new LNX_Triangle(_LNX_NavMesh.Triangles.Length+1, 0, endEdge0.EndPosition, v_midPoint1, v_midPoint0, _LNX_NavMesh ),
				new LNX_Triangle(_LNX_NavMesh.Triangles.Length+2, 0, v_midPoint0, v_midPoint1, endEdge1.StartPosition, _LNX_NavMesh ),
				new LNX_Triangle(_LNX_NavMesh.Triangles.Length+3, 0,  endEdge1.StartPosition, v_midPoint1, endEdge1.EndPosition, _LNX_NavMesh ),
			};

			_LNX_NavMesh.AddTriangles( trisToAdd );
			
		}
		#endregion

		public Ray ray;
		public bool AmPointingATBounds = false;

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			if( Application.isPlaying || _LNX_NavMesh == null || _LNX_NavMesh.Triangles == null )
			{
				return;
			}

			/*
			Handles.Label( dbg_edge0_start, "e0 start" );
			Handles.Label(dbg_edge0_mid, "e0 mid");
			Handles.Label(dbg_edge0_end, "e0 end");

			Handles.Label(dbg_edge1_start, "e1 start");
			Handles.Label(dbg_edge1_mid, "e1 mid");
			Handles.Label(dbg_edge1_end, "e1 end");

			Handles.Label(dbg_bridgeMid0, "brdgMid0");
			Handles.Label(dbg_bridgeMid1, "brdgMid1");
			*/

			#region TRIANGLES ------------------------------------------------
			Gizmos.color = _LNX_NavMesh.color_mesh;

			if ( drawMeshVisualization && MeshIsValidForDrawing )
			{
				Gizmos.DrawMesh(_LNX_NavMesh._Mesh);
			}

			if ( drawNavMeshLines || drawTriLabels || drawEdgeLabels )
			{
				GUIStyle gstl_label = GUIStyle.none;
				//gstl_nrml.normal.textColor = Color.white;

				for ( int i = 0; i < _LNX_NavMesh.Triangles.Length; i++ )
				{
					bool amKosher = _LNX_NavMesh.Triangles[i].v_sampledNormal != Vector3.zero;
					bool useGizmos = true;

					if( !amKosher )
					{
						Handles.color = Color.red;
						Gizmos.color = Color.red;
					}
					else
					{
						if( _LNX_NavMesh.Triangles[i].HasBeenModifiedAfterCreation )
						{
							Handles.color = color_modifiedTri;
							Gizmos.color = color_modifiedTri;
						}
						else if(indices_selectedTris != null && indices_selectedTris.Contains(i) )
						{
							Handles.color = color_selectedComponent;
							Gizmos.color = color_selectedComponent;
							useGizmos = false;
						}
						else
						{
							Handles.color = color_edgeLines;
							Gizmos.color = color_edgeLines;
						}
					}

					if ( drawTriLabels )
					{
						gstl_label.normal.textColor = amKosher ? Color.white : Color.red;

						Handles.Label( 
							_LNX_NavMesh.Triangles[i].V_Center, 
							_LNX_NavMesh.Triangles[i].Index_inCollection.ToString(), gstl_label
						);
					}

					if( drawNavMeshLines )
					{
						if( useGizmos )
						{
							LNX_Utils.DrawTriGizmos( _LNX_NavMesh.Triangles[i] );
						}
						else
						{
							LNX_Utils.DrawTriHandles( _LNX_NavMesh.Triangles[i], Size_SelectedComponent );

						}
					}

					if ( drawEdgeLabels )
					{
						Handles.Label( 
							_LNX_NavMesh.Triangles[i].Edges[0].MidPosition + (_LNX_NavMesh.Triangles[i].Edges[0].v_cross * 0.05f), 
							"e0", gstl_label);
						Handles.Label(
							_LNX_NavMesh.Triangles[i].Edges[1].MidPosition + (_LNX_NavMesh.Triangles[i].Edges[1].v_cross * 0.05f), 
							"e1", gstl_label);
						Handles.Label(
							_LNX_NavMesh.Triangles[i].Edges[2].MidPosition + (_LNX_NavMesh.Triangles[i].Edges[2].v_cross * 0.05f), 
							"e2", gstl_label);
					}
				}
			}

			if( amLocked )
			{
				//Handles.color = color_lockedTri;
				Gizmos.color = color_lockedTri;

				for ( int i = 0; i < indices_lockedTris.Count; i++ )
				{
					LNX_Utils.DrawTriGizmos( _LNX_NavMesh.Triangles[indices_lockedTris[i]] );

				}
			}
			else
			{
				/*if ( indices_selectedTris != null && indices_selectedTris.Count > 0 )
				{
					Handles.color = Color.white;

					for ( int i = 0; i < indices_selectedTris.Count; i++ )
					{
						LNX_Utils.DrawTriGizmos( _LNX_NavMesh.Triangles[indices_selectedTris[i]] );
					}
				}*/
				/*
				if( Index_TriLastSelected > -1 )
				{
					Handles.color = new Color(1f, 0.2f, 0f);
					DrawTriGizmos( LastSelectedTri, Size_FocusedComponent );
				}
				*/
				if ( PointingAtTri != null )
				{
					//Handles.color = Color.yellow;
					Gizmos.color = Color.yellow;
					LNX_Utils.DrawTriGizmos( PointingAtTri );
				}
			}
			#endregion

			#region VERTICES --------------------------------------------
			if( SelectMode == LNX_SelectMode.Vertices )
			{
				Handles.color = Color.white;
				Gizmos.color = Color.white;

				if( Vert_CurrentlyPointingAt != null )
				{
					Gizmos.DrawSphere( Vert_CurrentlyPointingAt.V_Position, Size_HoveredComponent * 0.020f );
				}

				if ( Verts_currentlySelected != null && Verts_currentlySelected.Count > 0 )
				{
					foreach ( LNX_Vertex vrt in Verts_currentlySelected )
					{
						if ( vrt == Vert_LastSelected )
						{
							Gizmos.color = Color.yellow;
							Gizmos.DrawSphere( vrt.V_Position, Size_HoveredComponent * 0.020f );
							Gizmos.color = Color.white;
						}
						else
						{
							Gizmos.DrawSphere( vrt.V_Position, Size_HoveredComponent * 0.013f );
						}
					}
				}
			}
			#endregion

			#region EDGES --------------------------------------------
			if ( SelectMode == LNX_SelectMode.Edges )
			{
				if ( Edges_currentlySelected != null && Edges_currentlySelected.Count > 0 )
				{
					Gizmos.color = Color.white;
					Handles.color = Color.white;
					foreach ( LNX_Edge edg in Edges_currentlySelected )
					{
						if ( edg == Edge_LastSelected )
						{
							Gizmos.color = Color.yellow;
							Handles.color = Color.yellow;

							//Handles.DrawLine( edg.StartPosition, edg.EndPosition, Size_HoveredComponent * 0.020f );
							Gizmos.DrawLine( edg.StartPosition, edg.EndPosition );

							Gizmos.color = Color.white;
							Handles.color = Color.white;
						}
						else
						{
							//Handles.DrawLine( edg.StartPosition, edg.EndPosition, Size_HoveredComponent * 0.013f );
							Gizmos.DrawLine( edg.StartPosition, edg.EndPosition );
						}
					}
				}

				if ( Edge_CurrentlyPointingAt != null )
				{
					Handles.color = Color.yellow;
					Gizmos.color = Color.yellow;

					//Handles.DrawLine( Edge_CurrentlyPointingAt.StartPosition, Edge_CurrentlyPointingAt.EndPosition, Size_HoveredComponent * 0.020f );
					Gizmos.DrawLine(Edge_CurrentlyPointingAt.StartPosition, Edge_CurrentlyPointingAt.EndPosition);

				}
			}
			#endregion

			//Debug.Log(name);
			generateDbgString();
		}
#endif

		private void generateDbgString()
		{
			DbgClass = $"Selected --------------------\n";
			DbgClass += $"'{(indices_selectedTris == null ? "null" : indices_selectedTris.Count)}' tris\n" +
				$"'{(Verts_currentlySelected == null ? "null" : Verts_currentlySelected.Count)}' verts\n" +
				$"'{(Edges_currentlySelected == null ? "null" : Edges_currentlySelected.Count)}' edges\n" +
				$"";

			DbgClass += $"\nLock State-----------------\n" +
				$"{nameof(amLocked)}: '{amLocked}'\n";

			if( indices_lockedTris == null )
			{
				DbgClass += $"{nameof(indices_lockedTris)} collection is null...\n";
			}
			else
			{
				DbgClass += $"{nameof(indices_lockedTris)} count: '{indices_lockedTris.Count}'\n";
			}

			DbgClass += $"\nPointing --------------------\n";
			DbgClass += $"{nameof(Flag_AComponentIsCurrentlyHighlighted)}: '{Flag_AComponentIsCurrentlyHighlighted}'\n" +
				$"";


		}
	}
}