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

		[SerializeField] protected LNX_NavMesh _mgr;
        [SerializeField] protected LNX_NavMeshDebugger _debugger;

        public int Index_problemPos = 0;
        [SerializeField, Tooltip("These are meant to be positions that I'm currently experimenting with")]
        public List<Vector3> testPositions;

		//[Space(10f)]

		[ContextMenu("z CaptureTestPosition()")]
        public virtual void CaptureTestPosition()
        {
            if (testPositions == null)
            {
                testPositions = new List<Vector3>();
            }

            testPositions.Add( transform.position );
        }

        [ContextMenu("z SendToProblemPosition()")]
        public void SendToProblemPosition()
        {
            if ( Index_problemPos > -1 && testPositions != null && testPositions.Count > 0 )
            {
                transform.position = testPositions[Index_problemPos];
            }
        }

        public string testName;
        protected virtual void OnDrawGizmos()
        {
			GUIStyle gstl = new GUIStyle();
			gstl.normal.textColor = Color.red;

			Handles.Label( transform.position + (Vector3.up * 0.2f), testName, gstl );

			if ( Selection.activeGameObject == gameObject )
			{
			    if ( testPositions != null && testPositions.Count > 0 )
                {
                    Gizmos.color = Color.red;
                    gstl = new GUIStyle();
                    gstl.normal.textColor = Color.red;
                    float lineHeight = 1.3f;

                    for (int i = 0; i < testPositions.Count; i++)
                    {
                        Gizmos.DrawLine(testPositions[i], testPositions[i] + (Vector3.up * lineHeight));
                        Handles.Label(testPositions[i] + (Vector3.up * lineHeight) + (Vector3.up * 0.1f), i.ToString(), gstl);
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
	}
}