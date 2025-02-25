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
		public Color color_edgeLines = Color.white;
		public float Thickness_edges = 1f;
		public bool drawEdgeLabels = false;

		[Header("DEBUG VERTICES")]
		[SerializeField] private bool drawVertSpheres = false;
		public bool DrawVertLables = false;
		public Color color_vertSphere = Color.white;
		[Range(0.01f, 0.05f)] public float radius_vertSphere = 0.05f;

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


		private void OnDrawGizmos()
		{
			if ( !AmDebugging )
			{
				return;
			}

			if ( _mgr.Triangles != null && _mgr.Triangles.Length > 0 )
			{
				for ( int i = 0; i < _mgr.Triangles.Length; i++ )
				{
					DrawTriGizmos( _mgr.Triangles[i], (Index_TriFocus > -1 && Index_TriFocus == i) ? true : false );
					//DrawTriGizmos( Triangles[i], false ); //for when you don't want this class to do any focusing...

				}
			}
		}

		public void DrawTriGizmos(LNX_Triangle tri, bool amFocused)
		{
			if ( amFocused && !AmAllowingFocus )
			{
				amFocused = false;
			}

			bool amKosher = true;
			if ( tri.v_normal == Vector3.zero )
			{
				amKosher = false;
			}

			GUIStyle gstl_label = GUIStyle.none;
			gstl_label.normal.textColor = amKosher ? Color.white : Color.red;
			float len_edgeLables = Length_normalLines * 0.25f;

			#region EDGES -------------------------------------------------------
			if ( !amKosher )
			{
				Gizmos.color = Color.red;
				Handles.color = Color.red;
			}

			Gizmos.color = amFocused ? Color.yellow : color_edgeLines;
			Handles.color = amFocused ? Color.yellow : color_edgeLines;

			//Draw borders...
			if ( amFocused )
			{
				Handles.DrawLine( tri.Verts[0].Position, tri.Verts[1].Position, Thickness_edges * Thickness_focusTri );
				Handles.DrawLine( tri.Verts[1].Position, tri.Verts[2].Position, Thickness_edges * Thickness_focusTri );
				Handles.DrawLine( tri.Verts[2].Position, tri.Verts[0].Position, Thickness_edges * Thickness_focusTri );
			}
			else
			{
				//Gizmos.DrawLine( tri.Verts[0].Position, tri.Verts[1].Position );
				//Gizmos.DrawLine( tri.Verts[1].Position, tri.Verts[2].Position );
				//Gizmos.DrawLine( tri.Verts[2].Position, tri.Verts[0].Position );

				Handles.DrawLine( tri.Verts[0].Position, tri.Verts[1].Position, Thickness_edges );
				Handles.DrawLine( tri.Verts[1].Position, tri.Verts[2].Position, Thickness_edges );
				Handles.DrawLine( tri.Verts[2].Position, tri.Verts[0].Position, Thickness_edges );
			}

			if( DrawTriLabels )
			{
				//Handles.Label( tri.V_center + (tri.v_normal * length_labels * 1.5f) + (tri.v_normal * 0.05f), tri.Index_parallelWithParentArray.ToString(), gstl_label );
				Handles.Label( tri.V_center, tri.Index_parallelWithParentArray.ToString(), gstl_label );
			}

			if ( drawEdgeLabels )
			{
				Handles.Label(tri.Edges[0].MidPosition + (tri.Edges[0].v_cross * len_edgeLables), "e0", gstl_label);
				Handles.Label(tri.Edges[1].MidPosition + (tri.Edges[1].v_cross * len_edgeLables), "e1", gstl_label);
				Handles.Label(tri.Edges[2].MidPosition + (tri.Edges[2].v_cross * len_edgeLables), "e2", gstl_label);
			}
			#endregion

			if( drawVertSpheres )
			{
				Gizmos.color = color_vertSphere;

				Gizmos.DrawSphere( tri.Verts[0].Position, radius_vertSphere );
				Gizmos.DrawSphere( tri.Verts[1].Position, radius_vertSphere );
				Gizmos.DrawSphere( tri.Verts[2].Position, radius_vertSphere );

				if ( DrawVertLables )
				{
					Gizmos.color = amKosher ? Color.white : Color.red;

					float calcLength = Length_normalLines * 0.5f;
					Handles.Label( tri.Verts[0].Position + (tri.Verts[0].v_toCenter.normalized * calcLength), "v0", gstl_label );
					Handles.Label( tri.Verts[1].Position + (tri.Verts[1].v_toCenter.normalized * calcLength), "v1", gstl_label );
					Handles.Label( tri.Verts[2].Position + (tri.Verts[2].v_toCenter.normalized * calcLength), "v2", gstl_label );
				}
			}

			if ( drawNormalLines )
			{
				Gizmos.color = Color_normalLines;

				Gizmos.DrawLine(tri.V_center, tri.V_center + (tri.v_normal * Length_normalLines));

				Gizmos.DrawLine(tri.Verts[0].Position, tri.V_center);
				Gizmos.DrawLine(tri.Verts[1].Position, tri.V_center);
				Gizmos.DrawLine(tri.Verts[2].Position, tri.V_center);

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
				//Vector3 newTargetPosition = Handles.PositionHandle(tri.Verts[Index_VertFocus].Position, Quaternion.identity );

				//tri.Verts[Index_VertFocus].Position = newTargetPosition;
				//tri.Verts[Index_VertFocus].Position = V_vertPlacePos;
			}
		}
	}
}