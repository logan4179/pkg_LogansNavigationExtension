using System;
using System.Data;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_CreateVertexRelationships : TDG_base
    {
        public LNX_ComponentGrabber Grabber_vertA;
		private LNX_Vertex vertA => Grabber_vertA.CurrentlyGrabbedVert;

        public LNX_ComponentGrabber Grabber_vertB;
		private LNX_Vertex vertB => Grabber_vertB.CurrentlyGrabbedVert;

		[Header("STATS")]
		[Range(0.01f, 1f)] public float Radius_vertSpheres = 0.1f;

		[Header("RESULTS")]
		public LNX_VertexRelationship Relationship_aToB;

		public Color Clr_pathPts;
		[Range(0.01f, 0.5f)] public float size_pathPts;
		public float len_pthPts;

		[ContextMenu("z call CreateRelationship()")]
		public void CreateRelationship()
		{
			DBG_Operation = $"{DateTime.Now}\n";
			mthdDbg_Report.Clear();

			if (Grabber_vertA.CurrentlyGrabbedVert == null)
			{
				DBG_Operation += $"{nameof(Grabber_vertA.CurrentlyGrabbedVert)} is null. Returning early...";
				return;
			}
			if (Grabber_vertB.CurrentlyGrabbedVert == null)
			{
				DBG_Operation += $"{nameof(Grabber_vertB.CurrentlyGrabbedVert)} is null. Returning early...";
				return;
			}

			DBG_Operation += $"using vertA: '{Grabber_vertA.CurrentlyGrabbedVert}', vertB: '{Grabber_vertB.CurrentlyGrabbedVert}'\n" +
				$"commencing operation...\n";

			mthdDbg_Report.StartReport(name);
			DateTime dt_opStart = DateTime.Now;
			Relationship_aToB = new LNX_VertexRelationship(
				vertA, vertB, _navmesh, ref mthdDbg_Report
			);
			DateTime dt_opEnd = DateTime.Now;
			mthdDbg_Report.EndReport();

			DBG_Operation += $"Operation complete with rel: '{Relationship_aToB}'\n\n" +
				$"rel info===============\n" +
				$"{Relationship_aToB.GetInfoString()}\n" +
				$"operation took '{dt_opEnd.Subtract(dt_opStart).TotalMilliseconds} ms'\n";
		}

		protected override void OnDrawGizmos()
		{
			if
			(
				AmInUnitTest ||
				!SelectionIsOneOfTheFollowing(
					gameObject,
					Grabber_vertA.gameObject,
					Grabber_vertB.gameObject
				)
			)
			{
				return;
			}

			base.OnDrawGizmos();

			Gizmos.DrawSphere( Grabber_vertA.transform.position, Radius_vertSpheres );
			Gizmos.DrawSphere( Grabber_vertB.transform.position, Radius_vertSpheres );

			//DBG_Operation += $"Commencing operation...\n";

			if (
				AutoCalculate &&
				(Grabber_vertA.RecalculatedLastFrame ||
				Grabber_vertB.RecalculatedLastFrame)
			)
			{
				CreateRelationship();

			}

			Gizmos.color = Clr_pathPts;
			Relationship_aToB.PathTo.DrawMyGizmos(size_pathPts, len_pthPts);

		}
	}
}
