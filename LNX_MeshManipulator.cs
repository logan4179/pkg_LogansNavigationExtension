using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
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

		//[Header("DEBUG")]
		[SerializeField] private bool drawNavMeshLines = true;
		[SerializeField] private Color color_edgeLines;
		[Range(0f, 8f)] public float thickness_edges = 0.2f;

		[Range(0f, 8f)] public float Size_SelectedComponent = 0.2f;
		[Range(0f, 8f)] public float Size_FocusedComponent = 0.2f;
		[Range(0f, 8f)] public float Size_HoveredComponent = 0.2f;
		[SerializeField] private Color color_lockedTri = Color.white;
		[Range(0f, 8f)] public float Thickness_LockedTriEdge = 0.2f;


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
			//Debug.Log($"{nameof(ClearState)}()");

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
				Debug.Log("here");
				ClearSelection();
			}

			if ( mode == LNX_SelectMode.None )
			{
				
			}
			else if( mode == LNX_SelectMode.Vertices )
			{
				if( mode != SelectMode ) //selection has changed to this...
				{

				}
			}
			else if( mode == LNX_SelectMode.Edges )
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

				for ( int i = 0; i < _LNX_NavMesh.Triangles.Length; i++ )
				{
					if (amLocked && !indices_lockedTris.Contains(i))
					{
						continue;
					}

					for ( int j = 0; j < 3; j++ )
					{
						Vector3 vTo = Vector3.Normalize( _LNX_NavMesh.GetEdgeAtCoordinate(i, j).MidPosition - vPerspective );
						float alignment = Vector3.Dot( vTo, vDirection );

						if ( alignment > runningBestAlignment )
						{
							DebugPointAt += $"vert: [{i}][{j}]. alignment: '{alignment}'\n";
							runningBestAlignment = alignment;

							runningBestCoordinate = new LNX_ComponentCoordinate( i, j );
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
					//DebugSelected += "a";
				}
				else if( indices_selectedTris.Contains(Index_TriPointingAt) ) //remove from selection...
				{
					indices_selectedTris.Remove( Index_TriPointingAt );
					//DebugSelected += "b";

				}
				else if ( !indices_selectedTris.Contains(Index_TriPointingAt) ) //add to selection...
				{
					indices_selectedTris.Add( Index_TriPointingAt );
					//DebugSelected += "c";
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
				DebugSelectedReport += $"pointing at: '{Vert_CurrentlyPointingAt.ToString()}' \n";
				Vert_LastSelected = Vert_CurrentlyPointingAt;

				if ( !amHoldingAddInputModifier )
				{
					Verts_currentlySelected = new List<LNX_Vertex>() { Vert_CurrentlyPointingAt };
					//DebugSelected += "a";
				}
				else if ( Verts_currentlySelected.Contains(Vert_CurrentlyPointingAt) ) //remove from selection...
				{
					Verts_currentlySelected.Remove( Vert_CurrentlyPointingAt );

					//DebugSelected += "b";

				}
				else if ( !Verts_currentlySelected.Contains(Vert_CurrentlyPointingAt) ) //add to selection...
				{
					Verts_currentlySelected.Add( Vert_CurrentlyPointingAt );
	
					//DebugSelected += "c";
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
				DebugSelectedReport += $"now checking for additional verts. Shared collection is showing '{Verts_currentlySelected[i].SharedVertexCoordinates.Count}'...\n";
				for ( int j = 0; j < Verts_currentlySelected[i].SharedVertexCoordinates.Count; j++ )
				{
					DebugSelectedReport += $"trying {Verts_currentlySelected[i].SharedVertexCoordinates[j].ToString()}... ";

					if( amLocked && !indices_lockedTris.Contains(Verts_currentlySelected[i].SharedVertexCoordinates[j].TriIndex) )
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
			#endregion
		}

		#region MESH MANIPULATION -----------------------------------------------------
		public void MoveSelectedVerts(Vector3 pos)
		{
			string dbgMoveSelected = $"{nameof(MoveSelectedVerts)}('{pos}') on '{Verts_currentlySelected.Count}' verts...\n";
			Vector3 vDiff = pos - manipulatorPos;
			dbgMoveSelected += $"vdiff: '{vDiff}', manip pos: '{manipulatorPos}'\n";

			if ( Verts_currentlySelected != null && Verts_currentlySelected.Count > 0 )
			{
				foreach ( LNX_Vertex vrt in Verts_currentlySelected )
				{
					_LNX_NavMesh.Triangles[vrt.MyCoordinate.TriIndex].MoveVert_protected(
						_LNX_NavMesh, vrt.MyCoordinate.ComponentIndex, vDiff, SelectMode == LNX_SelectMode.Vertices ? false : false
					);
				}
			}

			manipulatorPos = pos;
			//Debug.Log( dbgMoveSelected );
		}

		[ContextMenu("z call TryCut")]
		public void TryCut()
		{
			if ( Edges_currentlySelected.Count != 2 )
			{
				Debug.Log("can't");
				return;
			}

			LNX_Triangle tri0 = Edges_currentlySelected[0].MyTri(_LNX_NavMesh);
			LNX_Triangle tri1 = Edges_currentlySelected[1].MyTri(_LNX_NavMesh);

			if ( 
				!tri0.AmAdjacentToTri(tri1) ||
				!Edges_currentlySelected[0].AmTerminal || !Edges_currentlySelected[1].AmTerminal ||
				Edges_currentlySelected[0].AmTouching(Edges_currentlySelected[1])
			)
			{
				Debug.Log("can't");
				return;
			}

			Debug.Log("doing...");
			//first, find the edge that should be moved...
			LNX_Edge moveEdge = null;
			foreach( LNX_Edge edge in tri0.Edges )
			{
				if( edge != Edges_currentlySelected[0] && !Edges_currentlySelected[0].AmTouching(edge) )
				{
					moveEdge = tri0.Edges[0]; todo:does this work?
				}
			}

		}
		#endregion

		private bool selectedVertsContainTriIndex( int triIndex )
		{
			for ( int i = 0; i < Verts_currentlySelected.Count; i++ )
			{
				if( Verts_currentlySelected[i].MyCoordinate.TriIndex == triIndex )
				{
					return true;
				}
			}

			return false;
		}

		public Ray ray;
		public bool AmPointingATBounds = false;

		public Vector3 RayOrigin;
		public Vector3 RayDirection;
		public float RayTryMag = 1f;


		private void OnDrawGizmos()
		{
			if( Application.isPlaying || _LNX_NavMesh == null )
			{
				return;
			}

			//Draw the mouse ray...
			/*Gizmos.DrawLine(
				SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 1f, 
				SceneView.lastActiveSceneView.camera.transform.position + ray.direction
			);*/

			#region TRIANGLES ------------------------------------------------
			if ( _LNX_NavMesh != null && _LNX_NavMesh.Triangles != null )
			{
				Handles.color = color_edgeLines;

				for ( int i = 0; i < _LNX_NavMesh.Triangles.Length; i++ )
				{
					DrawTriGizmos(_LNX_NavMesh.Triangles[i], thickness_edges );
				}

				if( amLocked )
				{
					Handles.color = color_lockedTri;

					for ( int i = 0; i < indices_lockedTris.Count; i++ )
					{
						DrawTriGizmos( _LNX_NavMesh.Triangles[indices_lockedTris[i]], Thickness_LockedTriEdge );
					}
				}
				else
				{
					if ( indices_selectedTris != null && indices_selectedTris.Count > 0 )
					{
						Handles.color = Color.white;

						for ( int i = 0; i < indices_selectedTris.Count; i++ )
						{
							DrawTriGizmos( _LNX_NavMesh.Triangles[indices_selectedTris[i]], Size_SelectedComponent );
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
						Handles.color = Color.yellow;
						DrawTriGizmos( PointingAtTri, Size_HoveredComponent );
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
				Handles.color = Color.white;
				Gizmos.color = Color.white;

				if ( Edge_CurrentlyPointingAt != null )
				{
					Handles.DrawLine( Edge_CurrentlyPointingAt.StartPosition, Edge_CurrentlyPointingAt.EndPosition, Size_HoveredComponent * 0.020f );
				}

				if ( Edges_currentlySelected != null && Edges_currentlySelected.Count > 0 )
				{
					if( amLocked && indices_lockedTris != null && indices_lockedTris.Count > 0 )
					{
						Debug.Log("h");
						for ( int i = 0; i < indices_lockedTris.Count; i++ )
						{
							Gizmos.color = color_lockedTri;
							DrawTriGizmos( _LNX_NavMesh.Triangles[indices_lockedTris[i]], Thickness_LockedTriEdge );
						}
					}

					foreach ( LNX_Edge edg in Edges_currentlySelected )
					{

						if ( edg == Edge_LastSelected )
						{
							//Gizmos.color = Color.yellow;
							Handles.color = Color.yellow;

							Handles.DrawLine( edg.StartPosition, edg.EndPosition, Size_HoveredComponent * 0.020f );
							//Gizmos.color = Color.white;
							Handles.color = Color.white;

						}
						else
						{
							Handles.DrawLine( edg.StartPosition, edg.EndPosition, Size_HoveredComponent * 0.013f );
						}
					}
				}
			}
			#endregion

			generateDbgString();
		}

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

		public void DrawTriGizmos( LNX_Triangle tri, float thickness )
		{
			Handles.DrawLine( tri.Verts[0].Position, tri.Verts[1].Position, thickness );
			Handles.DrawLine( tri.Verts[1].Position, tri.Verts[2].Position, thickness );
			Handles.DrawLine( tri.Verts[2].Position, tri.Verts[0].Position, thickness );
		}

		public void DebugRay( Ray ray )
		{
			Debug.DrawLine( ray.origin, ray.origin + (ray.direction.normalized * RayTryMag), Color.magenta, 10f );
		}
	}
}