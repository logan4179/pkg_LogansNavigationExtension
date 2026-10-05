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
        [Tooltip("Dictates what component CurrentCoordinate refers to")] public LNX_Component CurrentCoordinateMode;

		[Tooltip("Considers closest off perimeter when sampling currentHit")]
		public bool cnsdrClsestOffPerimParameter;

		[Header("REFERENCE")]
		public LNX_NavMeshSurface _navmesh;

		[Header("TRI SPECIFIC")]
		[Tooltip("Restricts sampling to a certain tri. Note: Not yet implemented")]
		public int Index_TriRestrict = -1;

		[Header("EDGE SPECIFIC")]
		public bool SelectOnlyTerminalEdges;

		[Header("STATS")]
		public bool AutomaticallyGrab = true;
		public LNX_Component SnapTo = LNX_Component.None;
        [Range(0.05f, 2f), Tooltip("How easy it is to select a component")] 
		public float Forgiveness = 0.25f;


		[Header("RESULTS")]
		public LNX_NavmeshHit CurrentHit;
		public LNX_ComponentCoordinate CurrentCoordinate;

		[Header("DEBUG")]
		[SerializeField] bool drawLabel;
		[SerializeField] bool drawFocusTriGizmos;
		[SerializeField] bool drawNormalLines = false;
		[SerializeField, Range(0f, 0.15f)] private float sphereRadius = 0.02f;

		public Vector3 V_labelOffset;
		public Transform Trans_drawLineTo;
		[SerializeField] private Color color_drawLineToTrans = Color.white;
		[SerializeField] private bool recalculatedLastFrame = false;
		public bool RecalculatedLastFrame => recalculatedLastFrame;
		[SerializeField] bool drawComponentCoordinateInsteadOfLabel = false;

		[SerializeField, TextArea(1, 10)] public string DBG_Component;


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
				if (CurrentCoordinateMode == LNX_Component.Edge && CurrentCoordinate.ComponentIndex > -1)
				{
					return _navmesh.Triangles[CurrentCoordinate.TriangleIndex].Edges[CurrentCoordinate.ComponentIndex];
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
				if (CurrentCoordinateMode == LNX_Component.Vertex && CurrentCoordinate.ComponentIndex > -1)
				{
					return _navmesh.Triangles[CurrentCoordinate.TriangleIndex].Verts[CurrentCoordinate.ComponentIndex];
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

		[ContextMenu("z call GrabComponent()")]
		public void GrabComponent()
        {
			#region CURRENT HIT ==============================================
			CurrentHit = LNX_NavmeshHit.None;

			if( Index_TriRestrict != -1 )
			{
				if ( !_navmesh.Triangles[Index_TriRestrict].IsInShapeProject(transform.position, out CurrentHit))
				{
					Debug.LogWarning($"LNX WARNING! IsINShapeProject didn't work..");

					return;
				}
			}
			else if ( !_navmesh.SamplePosition(transform.position, out CurrentHit, 2f, cnsdrClsestOffPerimParameter))
			{
				Debug.LogWarning($"LNX WARNING! GrabComponent couldn't sample navmesh at current grabber position...");
				return;
			}
			#endregion


			CurrentCoordinate = LNX_ComponentCoordinate.None;

			if (CurrentCoordinateMode == LNX_Component.Vertex)
            {
				CurrentCoordinate = _navmesh.Triangles[CurrentHit.TriangleIndex].GetClosestVertToPosition(transform.position).MyCoordinate;
			}
			else if ( CurrentCoordinateMode == LNX_Component.Edge )
			{
				CurrentHit = LNX_SelectionUtilities.GetBestEdgeHitOnSurface(_navmesh, transform.position, SelectOnlyTerminalEdges );
				CurrentCoordinate = new LNX_ComponentCoordinate(CurrentHit.SurfaceIndex, CurrentHit.TriangleIndex, CurrentHit.EdgeIndex);
				DBG_Component = $"Edge: '{CurrentlyGrabbedEdge}'\n" +
					$"MyCoordinate: '{CurrentlyGrabbedEdge.MyCoordinate}'\n" +
					$"StartVertCoord: '{CurrentlyGrabbedEdge.StartVertCoordinate}', EndVertCoord: '{CurrentlyGrabbedEdge.EndVertCoordinate}'\n" +
					$"";
			}
			else if( CurrentCoordinateMode == LNX_Component.Triangle )
			{
				CurrentCoordinate = new LNX_ComponentCoordinate(CurrentHit.SurfaceIndex, CurrentHit.TriangleIndex, -1 );
				//Debug.Log($"Sample succesful. Grabbed tri '{CurrentCoordinate}'...");

			}
        }

		public Vector3 GetCurrentlyGrabbedPosition()
		{
			if (CurrentCoordinateMode == LNX_Component.None)
			{
				Debug.LogError($"LNX ERROR! Cannot get currently grabbed position if Mode is set to none");
				return Vector3.zero;
			}
			else if (CurrentCoordinateMode == LNX_Component.Vertex)
			{
				return CurrentlyGrabbedVert.V_Position;
			}
			else if ( CurrentCoordinateMode == LNX_Component.Triangle )
			{
				return CurrentlyGrabbedTriangle.V_Center; //todo: maybe in the future I can get the closest point on a tri surface
			}

			return Vector3.zero;
		}

		[ContextMenu("z call SayCurrentlyGrabbed()")]
		public void SayCurrentlyGrabbed()
		{
			if ( CurrentCoordinateMode == LNX_Component.Vertex )
			{
				CurrentlyGrabbedVert.SayCurrentInfo(_navmesh);
				Debug.Log( CurrentlyGrabbedVert.GetAnomolyString(_navmesh) );
			}
			else if ( CurrentCoordinateMode == LNX_Component.Triangle )
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
				CurrentlyGrabbedTriangle.GetRelationalString(_navmesh);

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
					if ( CurrentCoordinateMode == LNX_Component.Vertex && CurrentlyGrabbedVert != null ) 
					{
						Handles.Label(transform.position + V_labelOffset, CurrentlyGrabbedVert.ToString() );
					}
					else if ( CurrentCoordinateMode == LNX_Component.Edge && CurrentlyGrabbedEdge != null )
					{
						Handles.Label(transform.position + V_labelOffset, CurrentlyGrabbedEdge.ToString());
					}
					else if (CurrentCoordinateMode == LNX_Component.Triangle && CurrentlyGrabbedTriangle != null)
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
				Gizmos.color = color_drawLineToTrans;
				Gizmos.DrawLine(transform.position, Trans_drawLineTo.position);
			}
		}

		public void DrawMyGizmos()
		{
			if (sphereRadius > 0)
			{
				Gizmos.DrawSphere(transform.position, sphereRadius);
			}
			string lbl = DisplayName;

			if (drawLabel)
			{
				if (drawComponentCoordinateInsteadOfLabel)
				{
					if (CurrentCoordinateMode == LNX_Component.Vertex && CurrentlyGrabbedVert != null)
					{
						Handles.Label(transform.position + V_labelOffset, CurrentlyGrabbedVert.ToString());
					}
					else if (CurrentCoordinateMode == LNX_Component.Edge && CurrentlyGrabbedEdge != null)
					{
						Handles.Label(transform.position + V_labelOffset, CurrentlyGrabbedEdge.ToString());
					}
					else if (CurrentCoordinateMode == LNX_Component.Triangle && CurrentlyGrabbedTriangle != null)
					{
						Handles.Label(transform.position + V_labelOffset, CurrentlyGrabbedTriangle.ToString());
					}
				}
				else
				{
					Handles.Label(transform.position + V_labelOffset, string.IsNullOrEmpty(lbl) ? DisplayName : lbl);
				}

				Gizmos.DrawLine(transform.position, transform.position + V_labelOffset);
			}

			if (Trans_drawLineTo != null)
			{
				Gizmos.color = color_drawLineToTrans;
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
				LNX_DrawingUtilities.DrawTriGizmos( CurrentlyGrabbedTriangle, Color.yellow,
					false, false, true, 0.02f, true, CurrentlyGrabbedTriangle.ShortestEdgeLength * 0.3f, 
					drawNormalLines, 0.25f
				);
			}
			DrawMyGizmos();
		}
	}
}
