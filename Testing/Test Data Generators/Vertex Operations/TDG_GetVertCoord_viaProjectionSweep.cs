using System;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_GetVertCoord_viaProjectionSweep : TDG_base
    {
        public LNX_ComponentGrabber _VertGrabber;

        public LNX_Vertex CurrentlyGrabbedVert => _VertGrabber.CurrentlyGrabbedVert;

		[Header("RESULTS")]
		public LNX_ComponentCoordinate CurrentCoordinate;

		[Header("DEBUG")]

		public bool UseDbgVersion;

		[SerializeField, HideInInspector] Vector3 cachedLastTransPos;

		public string DbgVertRelational;

		#region HELPERS ============================
		[ContextMenu("Z SayCurrentInfoString()")]
		public void SayCurrentInfoString()
		{
			Debug.Log( CurrentlyGrabbedVert.GetCurrentInfoString() );
		}
		#endregion

		protected override void OnDrawGizmos()
		{
			if (
				AmInUnitTest || (Selection.activeObject != gameObject && Selection.activeObject != _VertGrabber.gameObject)
			)
			{
				return;
			}

			base.OnDrawGizmos();

			if( CurrentlyGrabbedVert == null )
			{
				DBG_Operation = $"currently grabbed vert null. Returning...\n";
				return;
			}

			if( _VertGrabber.RecalculatedLastFrame || transform.position == cachedLastTransPos )
			{
				DBG_Operation = $"{DateTime.Now}\n" +
					$"using CurrentlyGrabbedVert: '{CurrentlyGrabbedVert}''\n" +
					$"";

				if( CurrentlyGrabbedVert.Relationships == null || CurrentlyGrabbedVert.Relationships.Length <= 0 )
				{
					DBG_Operation += $"relationships null or 0. short-circuiting...";
					return;
				}
				else if (CurrentlyGrabbedVert.SharedVertexCoordinates == null || CurrentlyGrabbedVert.SharedVertexCoordinates.Length <= 0)
				{
					DBG_Operation += $"shared coords null or 0. short-circuiting...";
					return;
				}
				else
				{
					DBG_Operation += $"vert has: '{CurrentlyGrabbedVert.Relationships.Length}' relationships, and " +
						$"'{CurrentlyGrabbedVert.SharedVertexCoordinates.Length}' shared coords...\n" +
						$"";
				}

				//DbgVertRelational = CurrentlyGrabbedVert.GetCurrentInfoString();
				DbgVertRelational = CurrentlyGrabbedVert.GetAnomolyString( _navmesh );


				if (UseDbgVersion)
				{
					mthdDbg_Report.StartReport();
					CurrentCoordinate = CurrentlyGrabbedVert.GetVertCoord_viaProjectionSweep_dbg(
						Vector3.Normalize(transform.position - CurrentlyGrabbedVert.V_Position),
						true,
						ref mthdDbg_Report
					);
					mthdDbg_Report.EndReport();
				}
				else
				{

				}

				DBG_Operation += $"end of operation. CurrentCoordinate: '{CurrentCoordinate}'...\n";

			}

			Gizmos.DrawLine(_VertGrabber.transform.position, transform.position);
			float elevation = 0.05f;
			for ( int i = 0; i < CurrentlyGrabbedVert.SharedVertexCoordinates.Length; i++ )
			{
				Vector3 vLegA_flat = CurrentlyGrabbedVert.Relationships[
					CurrentlyGrabbedVert.SharedVertexCoordinates[i].TrianglesIndex * 3 + 
					(CurrentlyGrabbedVert.SharedVertexCoordinates[i].ComponentIndex == 0 ? 1 : 0)
				].V_to.normalized;
				Vector3 vLegB_flat = CurrentlyGrabbedVert.Relationships[
					CurrentlyGrabbedVert.SharedVertexCoordinates[i].TrianglesIndex * 3 + 
					(CurrentlyGrabbedVert.SharedVertexCoordinates[i].ComponentIndex == 2 ? 1 : 2)
				].V_to.normalized;

				float hue = (uint)CurrentlyGrabbedVert.SharedVertexCoordinates[i].GetHashCode() / (float)uint.MaxValue;
				Color clr = Color.HSVToRGB(hue, 0.6f, 1f);
				Gizmos.color = clr;

				Vector3 v = CurrentlyGrabbedVert.V_Position + vLegA_flat + (Vector3.up * elevation);
				Gizmos.DrawLine(CurrentlyGrabbedVert.V_Position,
					v
				);
				Handles.Label(v, "legA");

				v = CurrentlyGrabbedVert.V_Position + vLegB_flat + (Vector3.up * elevation);
				Gizmos.DrawLine(CurrentlyGrabbedVert.V_Position,
					v
				);
				Handles.Label(v, "legB");

				//Gizmos.color = Color.maroon;
				v = CurrentlyGrabbedVert.V_Position + ((vLegA_flat + vLegB_flat) / 2f) + (Vector3.up * elevation);
				Gizmos.DrawLine(CurrentlyGrabbedVert.V_Position,
					v
				);

				Handles.Label(v, CurrentlyGrabbedVert.SharedVertexCoordinates[i].ToString() );

				elevation += 0.05f;
			}


			cachedLastTransPos = transform.position;
		}
	}
}
