using LogansNavigationExtension;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_base : MonoBehaviour
    {
		public string LastWriteTime;

		[SerializeField] protected LNX_NavMesh _navmesh;
        [SerializeField] protected LNX_NavMeshDebugger _debugger;

        [Header("PROBLEMS")]
		public bool DrawProblemPoints = true;
        public int index_focusProblem = 0;
        [SerializeField, Tooltip("These are meant to be positions that I'm currently experimenting with")]
        public List<Vector3> problemPositions;
		public List<Vector3> problemEndPositions;


		//[Space(10f)]

		[ContextMenu("z CaptureProblemPosition()")]
        public virtual void CaptureProblemPosition()
        {
            if (problemPositions == null)
            {
                problemPositions = new List<Vector3>();
            }

            problemPositions.Add( transform.position );
        }

        [ContextMenu("z SendToProblemPosition()")]
        public void SendToProblemPosition()
        {
            if ( index_focusProblem > -1 && problemPositions != null && problemPositions.Count > 0 )
            {
                transform.position = problemPositions[index_focusProblem];
            }
        }

        public string testName;
        protected virtual void OnDrawGizmos()
        {
			GUIStyle gstl = new GUIStyle();
			gstl.normal.textColor = Color.red;

			Handles.Label( transform.position + (Vector3.up * 0.2f), testName, gstl );

			if ( DrawProblemPoints && problemPositions != null && problemPositions.Count > 0 )
            {
                Gizmos.color = Color.yellow;
                gstl = new GUIStyle();
                gstl.normal.textColor = Color.yellow;
                float lineHeight = 0.15f;

                for ( int i = 0; i < problemPositions.Count; i++ )
                {
                    Gizmos.DrawLine(problemPositions[i], problemPositions[i] + (Vector3.up * lineHeight));
                    Handles.Label(problemPositions[i] + (Vector3.up * lineHeight), $"prob{i}", gstl);

                    if( problemEndPositions != null && problemEndPositions.Count > 0 && problemEndPositions.Count == problemPositions.Count )
                    {
						Gizmos.DrawLine( problemEndPositions[i], problemEndPositions[i] + (Vector3.up * lineHeight) );
						Handles.Label( problemEndPositions[i] + (Vector3.up * lineHeight), $"prob{i}", gstl );

						if ( i == index_focusProblem )
						{
							Gizmos.DrawLine( problemPositions[i], problemEndPositions[i] );
						}
					}
                }
            }
        }

		public void DrawTriGizmo( LNX_Triangle tri, Color col )
		{
			Gizmos.color = col;
			Handles.color = col;

			GUIStyle gstl_vertLines = new GUIStyle();
			gstl_vertLines.normal.textColor = col;

			//Draw borders...
			Handles.DrawLine( tri.Verts[0].V_Position, tri.Verts[1].V_Position );
			Handles.DrawLine( tri.Verts[1].V_Position, tri.Verts[2].V_Position );
			Handles.DrawLine( tri.Verts[2].V_Position, tri.Verts[0].V_Position );
		}

        public void DrawStandardFocusTriGizmos( LNX_Triangle tri, float raiseAmount, string lblString )
        {
            Color oldColor = Gizmos.color;

			Gizmos.color = Color.magenta;
			//Handles.color = Color.magenta;
			Vector3 vRaise = Vector3.up * raiseAmount;

			Gizmos.DrawLine( tri.Verts[0].V_Position, tri.V_Center + vRaise );
			Gizmos.DrawLine( tri.Verts[1].V_Position, tri.V_Center + vRaise );
			Gizmos.DrawLine( tri.Verts[2].V_Position, tri.V_Center + vRaise );

			Handles.Label( tri.V_Center + vRaise, lblString );

            Gizmos.color = oldColor;
		}

        public void DrawStandardEdgeFocusGizmos( LNX_Edge edge, float raiseAmount, string lblString )
		{
			Color oldColor = Gizmos.color;

			Gizmos.color = Color.magenta;
			Vector3 vRaise = Vector3.up * raiseAmount;

            Gizmos.DrawLine( edge.StartPosition, edge.StartPosition + vRaise );
            Handles.Label( edge.StartPosition + vRaise, "edgeStart" );

            Gizmos.DrawLine( edge.StartPosition + vRaise, edge.EndPosition + vRaise );
			Gizmos.DrawLine( edge.EndPosition, edge.EndPosition + vRaise );
			Handles.Label(edge.EndPosition + vRaise, "edgeEnd");


			Gizmos.color = oldColor;
		}
	}
}