using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class LNX_ComponentGrabber : MonoBehaviour
    {
		public string DisplayName = "";

		[Header("SAMPLING")]
        public LNX_Component Mode;
		public bool ConsiderClosestOffPerimeter;

		[Header("REFERENCE")]
		public LNX_NavMesh _navmesh;

		[Header("STATS")]
		public bool AutomaticallyGrab = true;
		public LNX_Component SnapTo = LNX_Component.None;
        [Range(0.05f, 2f), Tooltip("How easy it is to select a component")] public float Forgiveness = 0.25f;

		[Header("DEBUG")]
		public Vector3 V_labelOffset;
		public Transform Trans_drawLineTo;
		[SerializeField] private bool recalculatedLastFrame = false;
		public bool RecalculatedLastFrame => recalculatedLastFrame;
		[SerializeField] bool useComponentCoordinateInsteadOfLabel = false;

		[Header("OTHER")]
		public LNX_NavmeshHit CurrentHit;
		public LNX_ComponentCoordinate CurrentCoordinate;

		public LNX_Triangle CurrentlyGrabbedTriangle
		{
			get
			{
				if( CurrentCoordinate.TrianglesIndex > -1 )
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

		[ExecuteInEditMode]
		private void OnEnable()
		{
			Debug.Log($"{nameof(LNX_ComponentGrabber)}.{nameof(OnEnable)}");
		}

		public LNX_ComponentCoordinate GrabComponent()
        {

            if (Mode == LNX_Component.None)
            {
                Debug.LogError($"LNX ERROR! Tried to capture a component, but component mode was set to None...");
                return LNX_ComponentCoordinate.None;
            }

			CurrentHit = LNX_NavmeshHit.None;
            if( _navmesh.SamplePosition(transform.position, out CurrentHit, 2f, ConsiderClosestOffPerimeter) )
            {
			    if (Mode == LNX_Component.Vertex)
                {
					CurrentCoordinate = _navmesh.Triangles[CurrentHit.TriIndex].GetClosestVertToPosition(transform.position).MyCoordinate;
                }
			    else if ( Mode == LNX_Component.Edge )
			    {
					float bestDist = Vector3.Distance(transform.position, _navmesh.GetEdge(CurrentHit.TriIndex, 0).MidPosition);
					int bestEdge = 0;

					if (Vector3.Distance(transform.position, _navmesh.GetEdge(CurrentHit.TriIndex, 1).MidPosition) < bestDist)
					{
						bestDist = Vector3.Distance(transform.position, _navmesh.GetEdge(CurrentHit.TriIndex, 1).MidPosition);
						bestEdge = 1;
					}

					if (Vector3.Distance(transform.position, _navmesh.GetEdge(CurrentHit.TriIndex, 2).MidPosition) < bestDist)
					{
						bestDist = Vector3.Distance(transform.position, _navmesh.GetEdge(CurrentHit.TriIndex, 2).MidPosition);
						bestEdge = 2;
					}

					CurrentCoordinate = _navmesh.Triangles[CurrentHit.TriIndex].Edges[bestEdge].MyCoordinate;
					Debug.Log($"Sample succesful. Grabbed edge '{CurrentCoordinate}'...");
				}
				else if( Mode == LNX_Component.Triangle )
				{
					CurrentCoordinate = new LNX_ComponentCoordinate(CurrentHit.TriIndex, -1 );
					Debug.Log($"Sample succesful. Grabbed tri '{CurrentCoordinate}'...");

				}
			}
			else
			{
				Debug.LogWarning($"LNX WARNING! GrabComponent couldn't sample navmesh at current grabber position...");
			}

			return CurrentCoordinate;
        }

		public Vector3 GetCurrentlyGrabbedPosition()
		{
			if (Mode == LNX_Component.None)
			{
				Debug.LogError($"LNX ERROR! Cannot get currently grabbed position if Mode is set to none");
				return Vector3.zero;
			}
			else if (Mode == LNX_Component.Vertex)
			{
				return CurrentlyGrabbedVert.V_Position;
			}
			else if ( Mode == LNX_Component.Triangle )
			{
				return CurrentlyGrabbedTriangle.V_Center; //todo: maybe in the future I can get the closest point on a tri surface
			}

			return Vector3.zero;
		}

		[ContextMenu("z call SayCurrentlyGrabbed()")]
		public void SayCurrentlyGrabbed()
		{
			if ( Mode == LNX_Component.Vertex )
			{
				CurrentlyGrabbedVert.SayCurrentInfo();
				Debug.Log( CurrentlyGrabbedVert.GetAnomolyString(_navmesh) );
			}
			else if ( Mode == LNX_Component.Triangle )
			{
				CurrentlyGrabbedTriangle.SayCurrentInfo(_navmesh);
				Debug.Log(CurrentlyGrabbedTriangle.GetAnomolyString(_navmesh));
			}
		}

		public void DrawMyGizmos(float radius)
		{
			Gizmos.DrawSphere( transform.position, radius );
			string lbl = DisplayName;

			if( useComponentCoordinateInsteadOfLabel )
			{
				if ( Mode == LNX_Component.Vertex && CurrentlyGrabbedVert != null ) 
				{
					Handles.Label(transform.position + V_labelOffset, CurrentlyGrabbedVert.ToString() );
				}
				else if ( Mode == LNX_Component.Edge && CurrentlyGrabbedEdge != null )
				{
					Handles.Label(transform.position + V_labelOffset, CurrentlyGrabbedEdge.ToString());
				}
				else if (Mode == LNX_Component.Triangle && CurrentlyGrabbedTriangle != null)
				{
					Handles.Label(transform.position + V_labelOffset, CurrentlyGrabbedTriangle.ToString());
				}
			}
			else
			{
				Handles.Label(transform.position + V_labelOffset, string.IsNullOrEmpty(lbl) ? DisplayName : lbl);
			}

			Gizmos.DrawLine( transform.position, transform.position + V_labelOffset );

			if (Trans_drawLineTo != null)
			{
				Gizmos.DrawLine(transform.position, Trans_drawLineTo.position);
			}
		}

		[HideInInspector, SerializeField] private Vector3 v_lastPos;
		private void OnDrawGizmos()
		{
			if( Selection.activeGameObject != this.gameObject )
            {
                return;
            }

			recalculatedLastFrame = false;
			if( transform.position != v_lastPos )
			{
				recalculatedLastFrame = true;
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
