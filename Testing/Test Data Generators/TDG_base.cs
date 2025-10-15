using LogansNavigationExtension;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_base : MonoBehaviour
    {
		public string testName;
		public string LastWriteTime;

        [TextArea(1,5)] public string Description;

		[SerializeField] protected LNX_NavMesh _navmesh;

        [Header("PROBLEMS")]
		public bool DrawProblemPoints = true;
        public int index_focusProblem = 0;
		public int Index_GoToDataPoint = 0;
		[SerializeField, Tooltip("These are meant to be positions that I'm currently experimenting with")]
        public List<Vector3> problemPositions;
		public List<Vector3> problemEndPositions;

        [Header("DEBUG (BASE)")]
		public bool AmInUnitTest = false;
		[Range(0f, 0.3f)] public float Radius_ObjectDebugSpheres = 0.2f;
		[Range(0f, 0.15f)] public float Radius_ProjectPos = 0.1f;

		[TextArea(1, 20)]
		public string DBG_Operation;

		//[Space(10f)]

		#region HELPERS --------------------------------------------
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
        public virtual void SendToProblemPosition()
        {
            if ( index_focusProblem > -1 && problemPositions != null && problemPositions.Count > 0 )
            {
                transform.position = problemPositions[index_focusProblem];
            }
        }

        public LNX_Triangle CaptureTriFromSample(Vector3 samplePos)
        {
			LNX_ProjectionHit hit = LNX_ProjectionHit.None;

			if (_navmesh.SamplePosition(samplePos, out hit, 2f, false))
            {
				return _navmesh.Triangles[hit.Index_Hit];
            }

			return null;
		}

		public LNX_Edge CaptureEdgeFromSample(Vector3 samplePos)
		{
			LNX_ProjectionHit hit = LNX_ProjectionHit.None;

			if (_navmesh.SamplePosition(samplePos, out hit, 2f, false))
			{
				float bestDist = Vector3.Distance(samplePos, _navmesh.GetEdge(hit.Index_Hit, 0).MidPosition);
				int bestEdge = 0;

				if (Vector3.Distance(samplePos, _navmesh.GetEdge(hit.Index_Hit, 1).MidPosition) < bestDist)
				{
					bestDist = Vector3.Distance(samplePos, _navmesh.GetEdge(hit.Index_Hit, 1).MidPosition);
					bestEdge = 1;
				}

				if (Vector3.Distance(samplePos, _navmesh.GetEdge(hit.Index_Hit, 2).MidPosition) < bestDist)
				{
					bestDist = Vector3.Distance(samplePos, _navmesh.GetEdge(hit.Index_Hit, 2).MidPosition);
					bestEdge = 2;
				}

				return _navmesh.Triangles[hit.Index_Hit].Edges[bestEdge];
			}

			return null;
		}

		#endregion

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
			Color oldColor = Gizmos.color;

			Gizmos.color = col;
			Handles.color = col;

			GUIStyle gstl_vertLines = new GUIStyle();
			gstl_vertLines.normal.textColor = col;

			//Draw borders...
			Handles.DrawLine( tri.Verts[0].V_Position, tri.Verts[1].V_Position );
			Handles.DrawLine( tri.Verts[1].V_Position, tri.Verts[2].V_Position );
			Handles.DrawLine( tri.Verts[2].V_Position, tri.Verts[0].V_Position );

			Gizmos.color = oldColor;
		}

		public void DrawStandardFocusTriGizmos( LNX_Triangle tri, float raiseAmount, string lblString, Color clr )
        {
            Color oldColor = Gizmos.color;

			Gizmos.color = clr;
			Vector3 vRaise = Vector3.up * raiseAmount;

			Gizmos.DrawLine( tri.Verts[0].V_Position, tri.V_Center + vRaise );
			Gizmos.DrawLine( tri.Verts[1].V_Position, tri.V_Center + vRaise );
			Gizmos.DrawLine( tri.Verts[2].V_Position, tri.V_Center + vRaise );

			Handles.Label( tri.V_Center + vRaise, lblString );

            DrawTriGizmo( tri, clr );

            Gizmos.color = oldColor;
		}

        public void DrawStandardEdgeFocusGizmos( LNX_Edge edge, float raiseAmount, string lblString, Color clr )
		{
			Color oldColor = Gizmos.color;

			Gizmos.color = clr;
			Vector3 vRaise = Vector3.up * raiseAmount;

			Handles.Label(edge.MidPosition + vRaise, edge.ToString() );

			Gizmos.DrawLine( edge.StartPosition, edge.StartPosition + vRaise );
            Handles.Label( edge.StartPosition + vRaise, "eStrt" );

            Gizmos.DrawLine( edge.StartPosition + vRaise, edge.EndPosition + vRaise );
			Gizmos.DrawLine( edge.EndPosition, edge.EndPosition + vRaise );
			Handles.Label(edge.EndPosition + vRaise, "eEnd");


			Gizmos.color = oldColor;
		}

        public void DrawDataPointCapture( Vector3 pos, Color clr )
        {
			Debug.DrawRay(
	            pos, Vector3.up, clr, 2f
            );
		}

        /// <summary>
        /// Logs the problem positions to the console. This can be useful for saving the values in 
        /// a text document when refactoring a tdg
        /// </summary>
        [ContextMenu("z call SayProblemPositions()")]
        public void SayProblemPositions()
        {
            string s = "";
            if (problemPositions == null)
            {
                Debug.LogError("Problem positions collection was null...");
                return;
            }

            s = "logging problem positions...\n";
            for ( int i = 0; i < problemPositions.Count; i++ )
            {
                s += LNX_UnitTestUtilities.LongVectorString(problemPositions[i]) + "\n";
            }

            if ( problemEndPositions == null || problemEndPositions.Count <= 0 )
            {
                Debug.LogWarning($"warning, '{nameof(problemEndPositions)}' either null or no values. Is this intentional?");
            }
            else
            {
				s += "logging problem END positions...";
				for (int i = 0; i < problemEndPositions.Count; i++)
				{
					s += LNX_UnitTestUtilities.LongVectorString(problemEndPositions[i]) + "\n";
				}
			}

            Debug.Log(s);
		}
	}
}