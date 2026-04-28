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
				Grabber_vertA.RecalculatedLastFrame ||
				Grabber_vertB.RecalculatedLastFrame
			)
			{
				Debug.Log("Recalculating...");

				DBG_Operation = "";
				DBG_Method = "";

				if( Grabber_vertA.CurrentlyGrabbedVert == null )
				{
					DBG_Operation += $"{nameof(Grabber_vertA.CurrentlyGrabbedVert)} is null. Returning early...";
					return;
				}
				if ( Grabber_vertB.CurrentlyGrabbedVert == null )
				{
					DBG_Operation += $"{nameof(Grabber_vertB.CurrentlyGrabbedVert)} is null. Returning early...";
					return;
				}

				DBG_Operation += $"commencing operation...\n";

				mthdDbg_Report.StartReport( name );

				Relationship_aToB = new LNX_VertexRelationship(
					vertA, vertB, _navmesh, ref mthdDbg_Report
				);

				mthdDbg_Report.EndReport();
			}
		}
	}
}
