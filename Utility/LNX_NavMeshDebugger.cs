using System;
using System.Linq;
using System.Net.Sockets;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.AI;

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

		public Vector3 V_vertPlacePos;

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

		[ContextMenu("z call SayFocusedVertInfo()")]
		public void SayFocusedVertInfo()
		{
			FocusedVert.SayCurrentInfo();
		}

		[ContextMenu("z call SayFocusedVertRelational()")]
		public void SayFocusedVertRelational()
		{
			FocusedVert.SayAllRelationships();
		}

		[ContextMenu("z call SendGrabberToFocusTri()")]
		public void SendGrabberToFocusTri()
		{
			Grabber_FocusTri.transform.position = _mgr.Triangles[Index_SendFocusTriGrabberTo].V_Center;
		}

		[ContextMenu("z call SayVisualMeshInfo()")]
		public void SayVisualMeshInfo()
		{
			string s = $"Vertices '{_mgr._VisualizationMesh.vertices.Length}' \n";

			for( int i = 0; i < _mgr._VisualizationMesh.vertices.Length; i++ )
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

		[ContextMenu("z call SayBounds()")]
		public void SayBounds()
		{
			string s = $"\n";

			s += $"lowX: '{_mgr.Bounds[0]}', highX: '{_mgr.Bounds[1]}'\n" +
				$"lowY: '{_mgr.Bounds[2]}', highY: '{_mgr.Bounds[3]}'\n" +
				$"lowZ: '{_mgr.Bounds[4]}', highZ: '{_mgr.Bounds[5]}'\n" +
				$"V_BoundsSize: '{_mgr.V_BoundsSize}', bounds center: '{_mgr.V_BoundsCenter}'";

			Debug.Log(s);
		}

		[ContextMenu("z call SayRelationshipsCount")]
		public void SayRelationshipsCount()
		{
			int relCount = 0;

			for ( int i = 0; i < _mgr.Triangles.Length; i++ )
			{
				for( int i_vrts = 0; i_vrts < 3; i_vrts++ )
				{
					if (_mgr.Triangles[i].Verts[i_vrts].Relationships != null && _mgr.Triangles[i].Verts[i_vrts].Relationships.Length > 0 )
					{
						for (int i_rels = 0; i_rels < _mgr.Triangles[i].Verts[i_vrts].Relationships.Length; i_rels++)
						{
							if (_mgr.Triangles[i].Verts[i_vrts].Relationships[i_rels].PathTo != LNX_Path.None )
							{
								relCount++;
							}
						}
					}
				}
			}

			Debug.Log(relCount);
		}

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
		#endregion

	}
}