using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class ComponentGrabber : MonoBehaviour
    {
		public string DisplayName = "";
        public LNX_Component Mode;

		[Header("REFERENCE")]
		public LNX_NavMesh _navmesh;

		[Header("STATS")]
		public bool AutomaticallyGrab = true;
		public LNX_Component SnapTo = LNX_Component.None;
        [Range(0.05f, 2f), Tooltip("How easy it is to select a component")] public float Forgiveness = 0.25f;

		[Header("OTHER")]
		public LNX_ComponentCoordinate CurrentCoordinate;
		public LNX_Triangle CurrentlyGrabbedTriangle
		{
			get
			{
				if( Mode == LNX_Component.Triangle && CurrentCoordinate.TrianglesIndex > -1 )
				{
					return _navmesh.Triangles[CurrentCoordinate.TrianglesIndex];
				}
				else
				{
					return null;
				}
			}
		}
		public LNX_Edge CurrentlyGrabbedEdge
		{
			get
			{
				if (Mode == LNX_Component.Edge && CurrentCoordinate.ComponentIndex > -1)
				{
					return _navmesh.Triangles[CurrentCoordinate.TrianglesIndex].Edges[CurrentCoordinate.ComponentIndex];
				}
				else
				{
					return null;
				}
			}
		}
		public LNX_Vertex CurrentlyGrabbedVert
		{
			get
			{
				if (Mode == LNX_Component.Vertex && CurrentCoordinate.ComponentIndex > -1)
				{
					return _navmesh.Triangles[CurrentCoordinate.TrianglesIndex].Verts[CurrentCoordinate.ComponentIndex];
				}
				else
				{
					return null;
				}
			}
		}

		//[Header("DEBUG")]


		public LNX_ComponentCoordinate GrabComponent()
        {
			CurrentCoordinate = LNX_ComponentCoordinate.None;

            if (Mode == LNX_Component.None)
            {
                Debug.LogError($"LNX ERROR! Tried to capture a component, but component mode was set to None...");
                return LNX_ComponentCoordinate.None;
            }

			LNX_ProjectionHit hit = LNX_ProjectionHit.None;
            if( _navmesh.SamplePosition(transform.position, out hit, 2f, false) )
            {
			    if (Mode == LNX_Component.Vertex)
                {
					CurrentCoordinate = _navmesh.Triangles[hit.Index_Hit].GetClosestVertToPosition(transform.position).MyCoordinate;
                }
			    else if ( Mode == LNX_Component.Edge )
			    {
					float bestDist = Vector3.Distance(transform.position, _navmesh.GetEdge(hit.Index_Hit, 0).MidPosition);
					int bestEdge = 0;

					if (Vector3.Distance(transform.position, _navmesh.GetEdge(hit.Index_Hit, 1).MidPosition) < bestDist)
					{
						bestDist = Vector3.Distance(transform.position, _navmesh.GetEdge(hit.Index_Hit, 1).MidPosition);
						bestEdge = 1;
					}

					if (Vector3.Distance(transform.position, _navmesh.GetEdge(hit.Index_Hit, 2).MidPosition) < bestDist)
					{
						bestDist = Vector3.Distance(transform.position, _navmesh.GetEdge(hit.Index_Hit, 2).MidPosition);
						bestEdge = 2;
					}

					CurrentCoordinate = _navmesh.Triangles[hit.Index_Hit].Edges[bestEdge].MyCoordinate;
					Debug.Log($"Sample succesful. Grabbed edge '{CurrentCoordinate}'...");
				}
				else if( Mode == LNX_Component.Triangle )
				{
					CurrentCoordinate = new LNX_ComponentCoordinate( hit.Index_Hit, -1 );
					Debug.Log($"Sample succesful. Grabbed tri '{CurrentCoordinate}'...");

				}
			}
			else
			{
				Debug.LogWarning($"LNX WARNING! GrabComponent couldn't sample navmesh at current grabber position...");
			}

			return CurrentCoordinate;
        }

		public void DrawMyGizmos(float radius)
		{
			Gizmos.DrawSphere( transform.position, radius );
			Handles.Label( transform.position, DisplayName );
		}

		[HideInInspector, SerializeField] private Vector3 v_lastPos;
		private void OnDrawGizmos()
		{

			if( Selection.activeGameObject != this.gameObject )
            {
                return;
            }


			if( transform.position != v_lastPos )
			{
				if( AutomaticallyGrab && Mode != LNX_Component.None )
				{
					GrabComponent();
				}

				if( SnapTo == LNX_Component.Vertex )
				{
					LNX_Vertex closestVert = _navmesh.GetClosestVertexToPosition(transform.position);
					if (closestVert != null)
					{
						if ( Vector3.Distance(transform.position, closestVert.V_Position) < 0.35f )
						{
							transform.position = closestVert.V_Position;
						}
					}
				}
			}			

			v_lastPos = transform.position;

			DrawMyGizmos(0.025f);
		}
	}
}
