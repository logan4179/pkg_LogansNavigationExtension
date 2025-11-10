using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_GetSharedAngle : TDG_base
    {
		public Transform Trans_getOTherTri;

		[Header("PARAMETERS")]
		public bool AtStart = true;

		[Header("PERSPECTIVE")]
		public LNX_ComponentCoordinate PerspectiveEdgeCoordinate = new LNX_ComponentCoordinate( 0, 0 );
		public LNX_Edge PerspectiveEdge => 
			_navmesh.Triangles[PerspectiveEdgeCoordinate.TrianglesIndex].Edges[PerspectiveEdgeCoordinate.ComponentIndex];
		public LNX_Triangle PerspectiveTriangle => _navmesh.Triangles[PerspectiveEdgeCoordinate.TrianglesIndex];

		public LNX_Vertex PerspectiveVert => _navmesh.GetVertexAtCoordinate( AtStart ? PerspectiveEdge.StartVertCoordinate : PerspectiveEdge.EndVertCoordinate );
				
		public LNX_Edge OtherEdge => _navmesh.GetEdge( PerspectiveEdge.SharedEdgeCoordinate );
		public LNX_Triangle OtherTriangle => _navmesh.GetTriangle(OtherEdge.MyCoordinate);
		public LNX_Vertex OtherVert
		{
			get
			{
				return PerspectiveVert.V_Position == OtherEdge.StartPosition ? _navmesh.GetVertexAtCoordinate(OtherEdge.StartVertCoordinate) : 
					_navmesh.GetVertexAtCoordinate(OtherEdge.EndVertCoordinate);
			}
		}


		[Header("DATA CAPTURE")]
		public float CurrentResult = 0f;

		public List<Vector3> CapturedTriCenters = new List<Vector3>();
		public List<Vector3> CapturedEdgeCenters = new List<Vector3>();

		[Header("DEBUG")]
		public string DBG_Triangle;


		protected override void OnDrawGizmos()
		{
			DBG_Operation = "";
			if (Selection.activeObject != gameObject && Selection.activeObject != Trans_getOTherTri.gameObject )
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Object not selected";
				return;
			}

			if (PerspectiveTriangle == null)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. CurrentTriangle null";
				return;
			}

			DBG_Operation += $"CurrentTriangle: '{PerspectiveTriangle.Index_inCollection}' at '{PerspectiveTriangle.V_Center}'\n";

			base.OnDrawGizmos();

			float prspctvRaise = 0.5f;

			DrawStandardFocusTriGizmos(PerspectiveTriangle, prspctvRaise, $"tri{PerspectiveTriangle.Index_inCollection}", Color.magenta );
			DrawStandardEdgeFocusGizmos( PerspectiveEdge, 0.1f * prspctvRaise, $"edge{PerspectiveEdgeCoordinate.ComponentIndex}", Color.yellow );
			Gizmos.DrawLine( PerspectiveVert.V_Position, PerspectiveVert.V_Position + (Vector3.up * 0.25f) );


			DrawStandardFocusTriGizmos( OtherTriangle, prspctvRaise * 0.7f, $"tri{PerspectiveTriangle.Index_inCollection}", Color.white );
			DrawStandardEdgeFocusGizmos(OtherEdge, 0.1f, $"edge{PerspectiveEdgeCoordinate.ComponentIndex}", Color.yellow);

			Gizmos.DrawSphere(transform.position, Radius_ObjectDebugSpheres);
			Gizmos.DrawSphere(Trans_getOTherTri.position, Radius_ObjectDebugSpheres);
			
			Gizmos.color = Color.yellow;
			Gizmos.DrawSphere(PerspectiveVert.V_Position, Radius_ObjectDebugSpheres * 0.5f);

			DBG_Operation += $"Ang at perspective vert: '{PerspectiveVert.AngleAtBend_flattened}'\n" +
				$"Ang at shared vert: '{OtherVert.AngleAtBend_flattened}'\n";

			DBG_Operation += $"commencing operations...\n";
			//CurrentResult = PerspectiveEdge.GetContinuousAngleBetween(_navmesh, OtherEdgeCoordinate, PerspectiveVert.V_Position);
			CurrentResult = PerspectiveEdge.GetCombinedSharedEdgeAngle( _navmesh, AtStart );

			DBG_Operation += $"rslt: '{CurrentResult}'\n\n" +
				$"report------------\n" +
				$"{PerspectiveEdge.DBG_GetContinuousAngleBetween}\n\n";

			DBG_Operation += $"Known fully visible...\n";
			for ( int i = 0; i < PerspectiveTriangle.KnownFullyVisibleTriangleIndices.Length; i++ )
			{
				DBG_Operation += $"{i},";
				DrawStandardFocusTriGizmos(_navmesh.Triangles[PerspectiveTriangle.KnownFullyVisibleTriangleIndices[i]], prspctvRaise * 0.1f, $"tri{PerspectiveTriangle.Index_inCollection}", Color.green);
			}

			DBG_Triangle = PerspectiveTriangle.DBG_FullyVisible;
		}

		#region HELPERS ---------------------------------------------------
		[ContextMenu("z call SampleFocusTri()")]
		public void SampleFocusTri()
		{
			Debug.Log($"{nameof(SampleFocusTri)}()...");

			LNX_ProjectionHit hit = LNX_ProjectionHit.None;

			if (_navmesh.SamplePosition(transform.position, out hit, 2f, false))
			{
				PerspectiveEdgeCoordinate = new LNX_ComponentCoordinate( hit.Index_Hit, 0 );
				Debug.Log($"Succesful sample! Set new triangle to: '{hit.Index_Hit}'");
			}
			else
			{
				Debug.Log($"sample unsuccesful...");
			}
		}

		[ContextMenu("z call SayFocusTris()")]
		public void SayFocusTris()
		{
			Debug.Log($"{nameof(SayFocusTris)}()...");

			Debug.Log($"PERSPECTIVE");
			PerspectiveTriangle.SayCurrentInfo(_navmesh);
			Debug.Log(PerspectiveTriangle.GetAnomolyString(_navmesh));

			Debug.Log($"OTHER");
			OtherTriangle.SayCurrentInfo(_navmesh);
			Debug.Log(OtherTriangle.GetAnomolyString(_navmesh));
		}
		/*
		[ContextMenu("z call DoEet()")]
		public void DoEet()
		{

		}

		[ContextMenu("z call SendToDataPoint")]
		public void SendToDataPoint()
		{
			
		}
		*/
		#endregion
	}
}
