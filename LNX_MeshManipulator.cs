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

		//[Header("OTHER")]
		public Vector3 manipulatorPos = Vector3.zero;

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

		[TextArea(1,5)] public string DBG_locked;
		[TextArea(1, 15)] public string DbgClass;

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

		public void TryPointAtBounds()
		{
			//todo: in the future, I need to make something that can detect if I'm pointing at
			//the bounds first before detecting if pointing at component for efficiency...
		}

		[SerializeField, TextArea(0,10)] private string DebugPointAt;
		public void TryPointAtComponentViaDirection( Vector3 vPerspective, Vector3 vDirection )
		{
			DebugPointAt = $"";

			Flag_AComponentIsCurrentlyHighlighted = false;

			if ( SelectMode == LNX_SelectMode.Faces )
			{
				Index_TriPointingAt = -1;

				float runningBestAlignment = 0.9f;

				for ( int i = 0; i < _LNX_NavMesh.Triangles.Length; i++ )
				{
					Vector3 vTo = Vector3.Normalize(_LNX_NavMesh.Triangles[i].V_center - vPerspective);
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
						Vector3 vTo = Vector3.Normalize( _LNX_NavMesh.GetVertexAtCoordinate(i,j).Position - vPerspective );
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
				float runningBestAlignment = 0.9f;

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

						if ( alignment > runningBestAlignment )
						{
							DebugPointAt += $"vert: [{i_tris}][{i_edges}]. alignment: '{alignment}'\n";
							runningBestAlignment = alignment;

							runningBestCoordinate = _LNX_NavMesh.Triangles[i_tris].Edges[i_edges].MyCoordinate;
						}
					}
				}

				if ( runningBestCoordinate != LNX_ComponentCoordinate.None )
				{
					Flag_AComponentIsCurrentlyHighlighted = true;
					Edge_CurrentlyPointingAt = _LNX_NavMesh.GetEdgeAtCoordinate( runningBestCoordinate );
				}
				else
				{
					Edge_CurrentlyPointingAt = null;
				}
			}
		}

		[SerializeField, TextArea(0, 10)] private string DebugSelectedReport;
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

				manipulatorPos = _LNX_NavMesh.Triangles[Index_TriLastSelected].V_center;
			}
			else if( SelectMode == LNX_SelectMode.Vertices )
			{
				DebugSelectedReport += $"pointing at: '{Vert_CurrentlyPointingAt.MyCoordinate}' \n";
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

				manipulatorPos = Vert_LastSelected.Position;
			}
			else if( SelectMode == LNX_SelectMode.Edges )
			{
				DebugSelectedReport += $"pointing at: '{Edge_CurrentlyPointingAt.ToString()}' \n";
				Edge_LastSelected = Edge_CurrentlyPointingAt; //note: something's funny with edge selection, particularly when selecting a terminal/border edge...
				
				LNX_Edge sharedEdge = amLocked ? null : _LNX_NavMesh.GetEdgeAtCoordinate( Edge_CurrentlyPointingAt.SharedEdge );

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

			if( Verts_currentlySelected.Count == 1 )
			{
				Debug.Log($"Have '{Verts_currentlySelected.Count}' verts currently selected at '{Vert_CurrentlyPointingAt.MyCoordinate}'...");
			}
			else
			{
				Debug.Log($"Have '{Verts_currentlySelected.Count}' verts currently selected...");
			}
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

			manipulatorPos = endPos;
			//Debug.Log( dbgMoveSelected );
		}

		[ContextMenu("z call TryInsertLoop")]
		public void TryInsertLoop()
		{
			#region check if I even can insert loop with current edge selection---------------------------------------------------------
			if ( Edges_currentlySelected.Count != 2 )
			{
				Debug.Log("can't");
				return;
			}

			LNX_Triangle tri0 = _LNX_NavMesh.GetTriangle( Edges_currentlySelected[0] );
			LNX_Triangle tri1 = _LNX_NavMesh.GetTriangle( Edges_currentlySelected[1] );

			Debug.Log($"edge0: '{Edges_currentlySelected[0].MyCoordinate}', edge1: '{Edges_currentlySelected[1].MyCoordinate}'");

			if (
				tri0.Relationships[tri1.Index_inCollection].NumberofSharedVerts != 2 ||
				!Edges_currentlySelected[0].AmTerminal || !Edges_currentlySelected[1].AmTerminal ||
				Edges_currentlySelected[0].AmTouching(Edges_currentlySelected[1])
			)
			{
				Debug.Log("can't");
				return;
			}
			#endregion

			#region Get the "Bridge" edges ---------------------
			LNX_ComponentCoordinate bridgeEdge0 = LNX_ComponentCoordinate.None;
			LNX_ComponentCoordinate bridgeEdge1 = LNX_ComponentCoordinate.None;
			for ( int i = 0; i < 3; i++ ) 
			{
				if( tri0.Edges[i] != Edges_currentlySelected[0] && tri0.Edges[i].SharedEdge.TrianglesIndex != tri1.Index_inCollection )
				{
					bridgeEdge0 = tri0.Edges[i].SharedEdge;
					Debug.Log($"Found edge index 1 at '{bridgeEdge0}'");
				}
				else if ( tri1.Edges[i] != Edges_currentlySelected[1] && tri1.Edges[i].SharedEdge.TrianglesIndex != tri0.Index_inCollection )
				{
					bridgeEdge1 = tri1.Edges[i].SharedEdge;
					Debug.Log($"Found edge index 2 at '{bridgeEdge1}'");
				}
			}

			//Debug.DrawLine(_LNX_NavMesh.GetEdgeAtCoordinate(edgeIndex0).MidPosition, _LNX_NavMesh.GetEdgeAtCoordinate(edgeIndex0).MidPosition + (Vector3.up * 2f), Color.blue, 2f );
			//Debug.DrawLine(_LNX_NavMesh.GetEdgeAtCoordinate(edgeIndex1).MidPosition, _LNX_NavMesh.GetEdgeAtCoordinate(edgeIndex1).MidPosition + (Vector3.up * 2f), Color.blue, 2f );
			#endregion

			Vector3 v_midPoint0 = Edges_currentlySelected[0].MidPosition;
			Vector3 v_midPoint1 = Edges_currentlySelected[1].MidPosition;


			Debug.Log("decided CAN cut. trying...");

			//_LNX_NavMesh.DeleteTriangle();

			/* old way
			//first, find the edge that should be moved...
			List<LNX_Vertex> vrtsFound = LNX_Utils.GetMoveVerts_forInsertLoop( _LNX_NavMesh, Edges_currentlySelected[0], Edges_currentlySelected[1] );
			Debug.Log($"found '{vrtsFound.Count}' verts...");

			if( vrtsFound.Count == 3 ) //There should be 3 verts to move, 2 shared at same position by tris
			{
				for ( int i = 0; i < 3; i++ ) // Reposition initial verts...
				{
					if ( vrtsFound[i].Position == _LNX_NavMesh.GetVertexAtCoordinate(Edges_currentlySelected[0].StartVertCoordinate).Position ||
						vrtsFound[i].Position == _LNX_NavMesh.GetVertexAtCoordinate(Edges_currentlySelected[0].EndVertCoordinate).Position
					)
					{
						//vrtsFound[i].Position = Edges_currentlySelected[0].MidPosition;
						_LNX_NavMesh.Triangles[vrtsFound[i].MyCoordinate.TriIndex].MoveVert_managed(
							_LNX_NavMesh,
							vrtsFound[i].MyCoordinate.ComponentIndex,
							Edges_currentlySelected[0].MidPosition,
							true
						);
					}
					if (vrtsFound[i].Position == _LNX_NavMesh.GetVertexAtCoordinate(Edges_currentlySelected[1].StartVertCoordinate).Position ||
						vrtsFound[i].Position == _LNX_NavMesh.GetVertexAtCoordinate(Edges_currentlySelected[1].EndVertCoordinate).Position
					)
					{
						//vrtsFound[i].Position = Edges_currentlySelected[1].MidPosition;
						_LNX_NavMesh.Triangles[vrtsFound[i].MyCoordinate.TriIndex].MoveVert_managed(
							_LNX_NavMesh,
							vrtsFound[i].MyCoordinate.ComponentIndex,
							Edges_currentlySelected[1].MidPosition,
							true
						);
					}
				}

				#region CREATE NEW TRIANGLES ------------------------------

				#endregion

				#region MAKE NEW CUT THE SELECTED VERTS --------------------

				#endregion


				_LNX_NavMesh.RefeshMesh();
			}
				*/
			//Debug.DrawLine(moveEdge.MidPosition, moveEdge.MidPosition + (Vector3.up * 3f), Color.yellow, 3f);

			//now I need to convert to verts...


		}
		#endregion

		public Ray ray;
		public bool AmPointingATBounds = false;

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			if( Application.isPlaying || _LNX_NavMesh == null )
			{
				return;
			}

			#region TRIANGLES ------------------------------------------------
			if ( _LNX_NavMesh != null && _LNX_NavMesh.Triangles != null )
			{
				Gizmos.color = _LNX_NavMesh.color_mesh;
				Gizmos.DrawMesh(_LNX_NavMesh._Mesh);

				if ( drawNavMeshLines || drawTriLabels || drawEdgeLabels )
				{
					GUIStyle gstl_label = GUIStyle.none;
					//gstl_nrml.normal.textColor = Color.white;

					for ( int i = 0; i < _LNX_NavMesh.Triangles.Length; i++ )
					{
						bool amKosher = _LNX_NavMesh.Triangles[i].v_normal != Vector3.zero;

						if( !amKosher )
						{
							Handles.color = Color.red;
							Gizmos.color = Color.red;
						}
						else
						{
							if( _LNX_NavMesh.Triangles[i].HasBeenModified )
							{
								Handles.color = color_modifiedTri;
								Gizmos.color = color_modifiedTri;
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
								_LNX_NavMesh.Triangles[i].V_center, 
								_LNX_NavMesh.Triangles[i].Index_inCollection.ToString(), gstl_label
							);
						}

						if( drawNavMeshLines )
						{
							LNX_Utils.DrawTriGizmos( _LNX_NavMesh.Triangles[i] );
							//LNX_Utils.DrawTriHandles( _LNX_NavMesh.Triangles[i], thickness_edges );
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
					if ( indices_selectedTris != null && indices_selectedTris.Count > 0 )
					{
						Handles.color = Color.white;

						for ( int i = 0; i < indices_selectedTris.Count; i++ )
						{
							LNX_Utils.DrawTriGizmos( _LNX_NavMesh.Triangles[indices_selectedTris[i]] );
						}
					}
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
			}
			#endregion

			#region VERTICES --------------------------------------------
			if( SelectMode == LNX_SelectMode.Vertices )
			{
				Handles.color = Color.white;
				Gizmos.color = Color.white;

				if( Vert_CurrentlyPointingAt != null )
				{
					Gizmos.DrawSphere( Vert_CurrentlyPointingAt.Position, Size_HoveredComponent * 0.020f );
				}

				if ( Verts_currentlySelected != null && Verts_currentlySelected.Count > 0 )
				{
					foreach ( LNX_Vertex vrt in Verts_currentlySelected )
					{
						if ( vrt == Vert_LastSelected )
						{
							Gizmos.color = Color.yellow;
							Gizmos.DrawSphere( vrt.Position, Size_HoveredComponent * 0.020f );
							Gizmos.color = Color.white;
						}
						else
						{
							Gizmos.DrawSphere( vrt.Position, Size_HoveredComponent * 0.013f );
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
			DbgClass += $"'{indices_selectedTris.Count}' tris\n" +
				$"'{Verts_currentlySelected.Count}' verts\n" +
				$"'{Edges_currentlySelected.Count}' edges\n" +
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