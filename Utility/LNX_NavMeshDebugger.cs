using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.AI;

namespace LogansNavigationExtension
{
	public class LNX_NavMeshDebugger : MonoBehaviour
    {
		[SerializeField] public LNX_NavMesh _mgr;



		[Header("DEBUG")]
		public bool AmDebugging = true;

		[Header("FOCUS")]
		public bool AmAllowingFocus = true;
		public bool FocusExclusively = true;
		/*[Range(0, 23)]*/ public int Index_TriFocus = 0;
		[Range(0, 7)] public float Thickness_focusTri = 1f;
		public bool AmAllowingVertFocus = true;
		[Range(0, 2)] public int Index_VertFocus = 0;
		public LNX_Triangle FocusedTri
		{
			get
			{
				if( _mgr.Triangles == null || Index_TriFocus < 0 || Index_TriFocus > _mgr.Triangles.Length-1 )
				{
					return null;
				}
				else
				{
					return _mgr.Triangles[Index_TriFocus];
				}
			}
		}

		public LNX_Vertex FocusedVert
		{
			get
			{
				if ( FocusedTri == null || Index_VertFocus < 0 || Index_VertFocus > 2 )
				{
					return null;
				}
				else
				{
					return FocusedTri.Verts[Index_VertFocus];
				}
			}
		}


		[Header("DEBUG TRIANGLES")]
		public bool DrawTriLabels = true;

		[Header("DEBUG EDGES")]
		public bool DrawEdges = true;
		public Color color_edgeLines = Color.white;
		public float Thickness_edges = 1f;
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

		[ContextMenu("z call CalculateAllDerived()")]
		public void CalculateAllDerived()
		{
			Debug.Log($"{nameof(CalculateAllDerived)}");

			for ( int i = 0; i < _mgr.Triangles.Length; i++ )
			{
				_mgr.Triangles[i].CalculateDerivedInfo();
			}
		}

		[ContextMenu("z call SayFocusedTriInfo()")]
		public void SayFocusedTriInfo()
		{
			Debug.Log($"{nameof(FocusedTri.Index_inCollection)}: '{FocusedTri.Index_inCollection}' \n" +
				$"{nameof(FocusedTri.MeshIndex_trianglesStart)}: '{FocusedTri.MeshIndex_trianglesStart}\n" +
				$"{nameof(FocusedTri.V_Center)}: '{FocusedTri.V_Center}'\n" +
				$"{nameof(FocusedTri.v_sampledNormal)}: '{FocusedTri.v_sampledNormal}' \n" +
				$"");

			Debug.Log($"vismesh normal: '{_mgr._Mesh.normals[_mgr._Mesh.triangles[FocusedTri.MeshIndex_trianglesStart]]}'\n" +
				$"");
		}

		[ContextMenu("z call SayVisualMeshInfo()")]
		public void SayVisualMeshInfo()
		{
			string s = $"Vertices '{_mgr._Mesh.vertices.Length}' \n";

			for( int i = 0; i < _mgr._Mesh.vertices.Length; i++ )
			{
				s += $"vert pos {i}: '{_mgr._Mesh.vertices[i]}'\n";
			}

			s += $"\nNormals '{_mgr._Mesh.normals.Length}' \n";

			for (int i = 0; i < _mgr._Mesh.normals.Length; i++)
			{
				s += $"normal {i}: '{_mgr._Mesh.normals[i]}'\n";
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

		private void OnDrawGizmos()
		{
			if ( !AmDebugging )
			{
				return;
			}

			if ( _mgr.Triangles != null && _mgr.Triangles.Length > 0 )
			{
				if ( AmAllowingFocus && FocusExclusively )
				{
					DrawTriGizmos( _mgr.Triangles[Index_TriFocus], true );
				}
				else
				{
					for (int i = 0; i < _mgr.Triangles.Length; i++)
					{
						DrawTriGizmos(_mgr.Triangles[i], (Index_TriFocus > -1 && Index_TriFocus == i) ? true : false);
					}
				}
			}
		}

		public void DrawTriGizmos(LNX_Triangle tri, bool amFocused)
		{
			if ( amFocused && !AmAllowingFocus )
			{
				amFocused = false;
			}

			bool amKosher = tri.v_sampledNormal != Vector3.zero;

			GUIStyle gstl_label = GUIStyle.none;
			gstl_label.normal.textColor = amKosher ? Color.white : Color.red;

			if (DrawTriLabels)
			{
				//Handles.Label( tri.V_center + (tri.v_normal * length_labels * 1.5f) + (tri.v_normal * 0.05f), tri.Index_parallelWithParentArray.ToString(), gstl_label );
				Handles.Label(tri.V_Center, tri.Index_inCollection.ToString(), gstl_label);
			}

			#region EDGES -------------------------------------------------------
			float len_edgeLables = Length_normalLines * 0.25f;

			if( amFocused )
			{
				Gizmos.color = Color.yellow;
				Handles.color = Color.yellow;
			}
			else
			{
				Gizmos.color = amKosher ? color_edgeLines : Color.red;
				Handles.color = amKosher ? color_edgeLines : Color.red;
			}

			//Draw borders...
			if( DrawEdges )
			{
				if ( amFocused )
				{
					LNX_Utils.DrawTriHandles( tri, Thickness_edges * Thickness_focusTri );
				}
				else
				{
					LNX_Utils.DrawTriHandles(tri, Thickness_edges );
				}
			}

			if ( drawEdgeLabels )
			{
				Handles.Label(tri.Edges[0].MidPosition + (tri.Edges[0].v_cross * len_edgeLables), "e0", gstl_label);
				Handles.Label(tri.Edges[1].MidPosition + (tri.Edges[1].v_cross * len_edgeLables), "e1", gstl_label);
				Handles.Label(tri.Edges[2].MidPosition + (tri.Edges[2].v_cross * len_edgeLables), "e2", gstl_label);
			}
			#endregion

			#region VERTS -----------------------------------------------
			if ( drawVertSpheres )
			{
				if( amFocused && AmAllowingVertFocus )
				{
					//Gizmos.DrawSphere( tri.Verts[Index_VertFocus].Position, radius_vertSphere * 3f );
					Handles.DrawWireDisc(tri.Verts[Index_VertFocus].V_Position, Vector3.up, radius_vertSphere * 4f);
				}
				Gizmos.color = color_vertSphere;

				Gizmos.DrawSphere( tri.Verts[0].V_Position, radius_vertSphere );
				Gizmos.DrawSphere( tri.Verts[1].V_Position, radius_vertSphere );
				Gizmos.DrawSphere( tri.Verts[2].V_Position, radius_vertSphere );
			}

			if ( DrawVertLables )
			{
				Gizmos.color = amKosher ? color_vertSphere : Color.red;

				float calcLength = Length_normalLines * 0.5f;
				Handles.Label(tri.Verts[0].V_Position + (tri.Verts[0].v_toCenter.normalized * calcLength), "v0", gstl_label);
				Handles.Label(tri.Verts[1].V_Position + (tri.Verts[1].v_toCenter.normalized * calcLength), "v1", gstl_label);
				Handles.Label(tri.Verts[2].V_Position + (tri.Verts[2].v_toCenter.normalized * calcLength), "v2", gstl_label);
			}
			#endregion

			if ( drawNormalLines )
			{
				Gizmos.color = Color_normalLines;

				Gizmos.DrawLine( tri.V_Center, tri.V_Center + (tri.v_sampledNormal * Length_normalLines) );
				
				Gizmos.DrawLine(tri.Verts[0].V_Position, tri.V_Center);
				Gizmos.DrawLine(tri.Verts[1].V_Position, tri.V_Center);
				Gizmos.DrawLine(tri.Verts[2].V_Position, tri.V_Center);

				Gizmos.DrawLine( tri.Edges[0].MidPosition, tri.Edges[0].MidPosition + (tri.Edges[0].v_cross * len_edgeLables) );
				Gizmos.DrawLine( tri.Edges[1].MidPosition, tri.Edges[1].MidPosition + (tri.Edges[1].v_cross * len_edgeLables) );
				Gizmos.DrawLine( tri.Edges[2].MidPosition, tri.Edges[2].MidPosition + (tri.Edges[2].v_cross * len_edgeLables) );
			}

			if ( amDrawingBounds && _mgr.Bounds != null && _mgr.Bounds.Length == 6 )
			{
				Gizmos.color = Color_boundsLines;

				Gizmos.DrawWireCube( _mgr.V_BoundsCenter, _mgr.V_BoundsSize );
				Gizmos.DrawCube(_mgr.V_Bounds[0], Vector3.one * 5f);
				Gizmos.DrawCube(_mgr.V_Bounds[4], Vector3.one);

			}

			if ( AmAllowingFocus && amFocused && AmAllowingVertFocus )
			{
				Gizmos.DrawLine(tri.Verts[Index_VertFocus].V_Position, tri.Verts[Index_VertFocus].V_Position + (Vector3.up * 1.2f) );
			}
		}
	}
}