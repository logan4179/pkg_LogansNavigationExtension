using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace LogansNavigationExtension
{
    public class LNX_ComponentGrabber : MonoBehaviour
    {
		public string DisplayName = "";

		[Header("SAMPLING")]
        [Tooltip("Dictates what gets grabbed in grabbing operations")] public LNX_Component Mode;
		public bool ConsiderClosestOffPerimeter;

		[Header("REFERENCE")]
		public LNX_NavMesh _navmesh;

		[Header("STATS")]
		public bool AutomaticallyGrab = true;
		public LNX_Component SnapTo = LNX_Component.None;
        [Range(0.05f, 2f), Tooltip("How easy it is to select a component")] 
		public float Forgiveness = 0.25f;

		[Header("DEBUG")]
		[SerializeField] bool drawLabel;
		[SerializeField] bool drawFocusTriGizmos;

		public Vector3 V_labelOffset;
		public Transform Trans_drawLineTo;
		[SerializeField] private bool recalculatedLastFrame = false;
		public bool RecalculatedLastFrame => recalculatedLastFrame;
		[SerializeField] bool drawComponentCoordinateInsteadOfLabel = false;

		[Header("OTHER")]
		public LNX_NavmeshHit CurrentHit;
		public LNX_ComponentCoordinate CurrentCoordinate;
		[Tooltip("Restricts sampling to a certain tri. Note: Not yet implemented")]
		public int Index_TriRestrict = -1;

		public LNX_Triangle CurrentlyGrabbedTriangle
		{
			get
			{
				if( CurrentHit.TriangleIndex > -1 )
				{
					return _navmesh.Triangles[CurrentHit.TriangleIndex];
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
			CurrentHit = LNX_NavmeshHit.None;
			CurrentCoordinate = LNX_ComponentCoordinate.None;

			if( Index_TriRestrict != -1 )
			{
				if ( !_navmesh.Triangles[Index_TriRestrict].IsInShapeProject(transform.position, out CurrentHit))
				{
					return LNX_ComponentCoordinate.None;
				}
			}
			else if ( !_navmesh.SamplePosition(transform.position, out CurrentHit, 2f, ConsiderClosestOffPerimeter))
			{
				//Debug.LogWarning($"LNX WARNING! GrabComponent couldn't sample navmesh at current grabber position...");
				return LNX_ComponentCoordinate.None;
			}

			if (Mode == LNX_Component.Vertex)
            {
				CurrentCoordinate = _navmesh.Triangles[CurrentHit.TriangleIndex].GetClosestVertToPosition(transform.position).MyCoordinate;
			}
			else if ( Mode == LNX_Component.Edge )
			{
				float bestDist = Vector3.Distance(transform.position, _navmesh.GetEdge(CurrentHit.TriangleIndex, 0).MidPosition);
				int bestEdge = 0;

				if (Vector3.Distance(transform.position, _navmesh.GetEdge(CurrentHit.TriangleIndex, 1).MidPosition) < bestDist)
				{
					bestDist = Vector3.Distance(transform.position, _navmesh.GetEdge(CurrentHit.TriangleIndex, 1).MidPosition);
					bestEdge = 1;
				}

				if (Vector3.Distance(transform.position, _navmesh.GetEdge(CurrentHit.TriangleIndex, 2).MidPosition) < bestDist)
				{
					bestDist = Vector3.Distance(transform.position, _navmesh.GetEdge(CurrentHit.TriangleIndex, 2).MidPosition);
					bestEdge = 2;
				}

				CurrentCoordinate = _navmesh.Triangles[CurrentHit.TriangleIndex].Edges[bestEdge].MyCoordinate;
				//Debug.Log($"Sample succesful. Grabbed edge '{CurrentCoordinate}'...");
			}
			else if( Mode == LNX_Component.Triangle )
			{
				CurrentCoordinate = new LNX_ComponentCoordinate(CurrentHit.TriangleIndex, -1 );
				//Debug.Log($"Sample succesful. Grabbed tri '{CurrentCoordinate}'...");

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

		[ContextMenu("z call SayCurrentSampledTri()")]
		public void SayCurrentSampledTri()
		{
			if( CurrentlyGrabbedTriangle == null )
			{
				Debug.Log($"CurrentlyGrabbedTriangle null...");
			}
			else
			{
				//CurrentlyGrabbedTriangle.SayCurrentInfo(_navmesh);
				//Debug.Log(CurrentlyGrabbedTriangle.GetAnomolyString(_navmesh));
				CurrentlyGrabbedTriangle.GetRelationalString();

			}
		}

		public void DrawMyGizmos(float radius)
		{
			Gizmos.DrawSphere( transform.position, radius );
			string lbl = DisplayName;

			if( drawLabel )
			{ 
				if( drawComponentCoordinateInsteadOfLabel )
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
			}

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

				if( AutomaticallyGrab )
				{
					GrabComponent();
				}
			}			

			v_lastPos = transform.position;

			if (drawFocusTriGizmos && CurrentlyGrabbedTriangle != null)
			{
				LNX_DrawingUtils.DrawTriGizmos( CurrentlyGrabbedTriangle, Color.yellow,
					false, false, true, 0.02f, true, 0.1f, false, -1f
				);
			}
			DrawMyGizmos(0.025f);
		}
	}
}
