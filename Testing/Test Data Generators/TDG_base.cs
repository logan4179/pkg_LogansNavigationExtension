using LogansNavigationExtension;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
	/// <summary>
	/// Base class for the "Test Data Generator" - The thing that creates all the data for my unit tests
	/// </summary>
    public class TDG_base : MonoBehaviour
    {
		public string testName;
		public string LastWriteTime;

        [TextArea(1,10)] public string Description;

		[SerializeField] protected LNX_NavMesh _navmesh;
		public bool AutoCalculate = true;


		[Header("PROBLEMS")]
		public int Index_GoToProblem = 0;
		public TDG_DataCapture _dataCapture_problems;

		public bool DrawProblemPoints = true;
		//[SerializeField, Tooltip("These are meant to be positions that I'm currently experimenting with")]
        //public List<Vector3> problemPositions;
		//public List<Vector3> problemEndPositions;

		[Header("DATA")]
		public TDG_DataCapture _dataCapture;

		[Header("DEBUG (BASE)")]
		[Range(0f, 0.3f)] public float Radius_ObjectDebugSpheres = 0.2f;
		[Range(0f, 0.15f)] public float Radius_ProjectPos = 0.1f;
		[HideInInspector] public bool AmInUnitTest = false;
		public bool UseDebugVersion = false;


		[TextArea(1, 20)]
		public string DBG_Operation;

		//[TextArea(1, 20)]
		//public string DBG_Method;
		public LNX_MethodDebugReport mthdDbg_Report;


		//[Space(10f)]

		#region HELPERS --------------------------------------------
		public virtual void CaptureProblemPosition()
        {

        }

		[ContextMenu("z call GoToProblemPoint(base)")]
		public virtual void GoToProblemPoint()
		{
			_dataCapture_problems.SendTo(Index_GoToProblem);
		}

		public bool SelectionIsOneOfTheFollowing( params GameObject[] gameObjects )
		{
			for( int i = 0; i < gameObjects.Length; i++ )
			{
				if( Selection.activeGameObject == gameObjects[i] )
				{
					return true;
				}
			}

			return false;
		}

		#endregion

		protected virtual void OnDrawGizmos()
        {
			GUIStyle gstl = new GUIStyle();
			gstl.normal.textColor = Color.red;

			Handles.Label( transform.position + (Vector3.up * 0.2f), testName, gstl );
        }

		public void DrawTriGizmo(LNX_Triangle tri, Color col, float offsetHeight = 0f)
		{
			Color oldColor = Gizmos.color;

			Gizmos.color = col;
			Handles.color = col;

			GUIStyle gstl_vertLines = new GUIStyle();
			gstl_vertLines.normal.textColor = col;

			Vector3 v_offsetHeight = Vector3.up * offsetHeight;

			//Draw borders...
			Handles.DrawLine(tri.Verts[0].V_Position + v_offsetHeight, tri.Verts[1].V_Position + v_offsetHeight);
			Handles.DrawLine(tri.Verts[1].V_Position + v_offsetHeight, tri.Verts[2].V_Position + v_offsetHeight);
			Handles.DrawLine(tri.Verts[2].V_Position + v_offsetHeight, tri.Verts[0].V_Position + v_offsetHeight);

			Gizmos.color = oldColor;
		}

		public void DrawStandardFocusTriGizmos(LNX_Triangle tri, float raiseAmount, string lblString, Color clr, bool drawTriGizmo = false, float triGzmoRaiseAmt = 0f, bool lblAll = false, bool drawToCtrLines = true)
		{
			Color oldColor = Gizmos.color;

			Gizmos.color = clr;
			Vector3 vRaise = Vector3.up * raiseAmount;

			if (drawToCtrLines)
			{
				Gizmos.DrawLine(tri.Verts[0].V_Position, tri.V_Center + vRaise);
				Gizmos.DrawLine(tri.Verts[1].V_Position, tri.V_Center + vRaise);
				Gizmos.DrawLine(tri.Verts[2].V_Position, tri.V_Center + vRaise);
			}

			Handles.Label(tri.V_Center + vRaise, lblString);

			if (drawTriGizmo)
			{
				DrawTriGizmo(tri, clr, triGzmoRaiseAmt);
			}

			if (lblAll)
			{
				for (int i = 0; i < 3; i++)
				{
					Handles.Label( tri.Verts[i].V_Position + (tri.Verts[i].v_toCenter * 0.1f), $"v{i}" );
				}

				for (int i = 0; i < 3; i++)
				{
					Handles.Label(tri.Edges[i].MidPosition + (tri.Edges[i].v_toCenter * 0.15f), $"e{i}");
				}
			}

			Gizmos.color = oldColor;
		}

		public void DrawStandardEdgeFocusGizmos( LNX_Edge edge, float raiseAmount, string lblString, Color clr, bool drawMidPt = false )
		{
			Color oldColor = Gizmos.color;

			Gizmos.color = clr;
			Vector3 vRaise = Vector3.up * raiseAmount;

			Handles.Label(edge.MidPosition + (vRaise * 1.3f), lblString );

			Gizmos.DrawLine( edge.StartPosition, edge.StartPosition + vRaise );
            Handles.Label( edge.StartPosition + vRaise, "eStrt" );

            Gizmos.DrawLine( edge.StartPosition + vRaise, edge.EndPosition + vRaise );
			Gizmos.DrawLine( edge.EndPosition, edge.EndPosition + vRaise );
			Handles.Label(edge.EndPosition + vRaise, "eEnd");

			if( drawMidPt )
			{
				Gizmos.DrawLine(edge.MidPosition + vRaise, edge.MidPosition + vRaise);
				Gizmos.DrawLine(edge.MidPosition, edge.MidPosition + vRaise);
				Handles.Label(edge.MidPosition + vRaise, "Mid");
			}

			Gizmos.color = oldColor;
		}

		public void DrawEdgeBridgeVisual(LNX_Edge strtEdge, LNX_Edge endEdge, Color clr )
		{
			Color oldClr = Gizmos.color;

			Gizmos.color = clr;

			if( LNX_Utils.AreEdgesAlignedFromTheirPerspectives(strtEdge, endEdge) )
			{
				Gizmos.DrawLine(strtEdge.StartPosition, endEdge.StartPosition);
				Gizmos.DrawLine(strtEdge.EndPosition, endEdge.EndPosition);
			}
			else
			{
				Gizmos.DrawLine(strtEdge.StartPosition, endEdge.EndPosition);
				Gizmos.DrawLine(strtEdge.EndPosition, endEdge.StartPosition);
			}
		}

		public void DrawQuadVisual(Vector3 crnrA, Vector3 crnrB, Vector3 crnrC, Vector3 crnrD)
		{
			Gizmos.DrawLine( crnrA, crnrB );
			Gizmos.DrawLine( crnrB, crnrC );
			Gizmos.DrawLine( crnrC, crnrD );
			Gizmos.DrawLine( crnrD, crnrA );
		}

		public void DrawQuadVisual(LNX_Quad q)
		{
			DrawQuadVisual( q.crnrA, q.crnrB, q.crnrC, q.crnrD );
		}
		public void DrawDataPointCapture( Vector3 pos, Color clr )
        {
			Debug.DrawRay(
	            pos, Vector3.up, clr, 2f
            );
		}
		public void DrawDataPointCapture(LNX_NavmeshHit hit, Color clr)
		{
			Debug.DrawRay(
				hit.Position, Vector3.up, clr, 2f
			);
		}

		/// <summary>
		/// Logs the problem positions to the console. This can be useful for saving the values in 
		/// a text document when refactoring a tdg
		/// </summary>
		[ContextMenu("z call SayProblemPositions()")]
        public void SayProblemPositions()
        {

		}
	}
}