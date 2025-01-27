using System;
using UnityEngine;

namespace LogansNavigationExtension
{
	[Serializable]
	public class LNX_PathPoint
	{

		public Vector3 V_point;

		public Vector3 V_toNext;

		/// <summary>
		/// Normalized vector pointing to the previous pathpoint.
		/// </summary>
		public Vector3 V_toPrev;


		/// <summary>
		/// Normalized vector describing the 'up' direction a crawling enemy would need to know based on the terrain 'relatively under' this point.
		/// </summary>
		public Vector3 V_normal;


		/// <summary>The distance to the next patrol point.</summary>
		public float Dist_toNext = -1f;

		public float Dist_toPrev = -1f;


		[HideInInspector] public bool AmEndPoint; //todo: dws?
		public bool Flag_amCorner;
		public bool flag_switchGravityOff; //todo: I don't think we need both of these flags...
		public bool flag_switchGravityOn; //todo: I don't think we need both of these flags...

		[TextArea(2, 10)] public string DebugClass;

		public LNX_PathPoint(Vector3 pt)
		{
			V_point = pt;
			V_normal = Vector3.up;

			V_toNext = Vector3.zero;
			V_toPrev = Vector3.zero;
			Dist_toNext = -1;
			Dist_toPrev = -1;
			AmEndPoint = false;

			flag_switchGravityOn = false;
			flag_switchGravityOff = false;
			Flag_amCorner = false;

			DebugClass = string.Empty;
		}

		public LNX_PathPoint( Vector3 pt, int mask_passed )
		{
			DebugClass = $"constructing at pt: '{pt}'...\n";
			AmEndPoint = false;

			flag_switchGravityOn = false;
			flag_switchGravityOff = false;
			Flag_amCorner = false;

			V_normal = Vector3.zero;
			V_point = pt;
			V_toNext = Vector3.zero;
			V_toPrev = Vector3.zero;
			Dist_toNext = -1;
			Dist_toPrev = -1;

			#region GENERATE NORMALS-----------------------------------------------------------------
			RaycastHit rcHit;
			DebugClass += "Normal check---------------------------\n";

			Collider[] hitColliders = Physics.OverlapSphere(V_point, 0.2f, mask_passed, QueryTriggerInteraction.Ignore);
			if ( hitColliders.Length > 0) //Note: I don't have to check if it's null here, bc it will never be null bc of the line above
			{
				DebugClass += $"Phusics.OverlapSphere succesful. closest: '{hitColliders[0].ClosestPoint(V_point)}'..\n";

				if ( LNX_Utils.CrossCast(V_point, 0.15f, out rcHit, mask_passed, QueryTriggerInteraction.Ignore))
				{
					DebugClass += ($"crosscast succesful, object hit: '{rcHit.transform.name}', crosscast hit at: '{rcHit.point}' normal: '{rcHit.normal}'\n");
					V_normal = rcHit.normal;
					V_point = hitColliders[0].ClosestPoint(V_point + V_normal);
					DebugClass += $"closest point was: '{V_point}'\n";
				}
				else
				{
					DebugClass += ($"crosscast Not succesful.~~~~~~~~~~~XXXXXXXXX\n");
				}
			}
			else
			{
				DebugClass += ($"hitcolliders length was: '{hitColliders.Length}'~~~~~~~~~~~XXXXXXXXX\n");
			}
			#endregion
		}

		/// <summary>
		/// This overload is for corner initialization.
		/// </summary>
		public LNX_PathPoint( LNX_PathPoint ptBefore, LNX_PathPoint ptAFter )
		{
			Flag_amCorner = true;

			//V_point = ptBefore.V_point + Vector3.ProjectOnPlane( ptAFter.V_point - ptBefore.V_point, ptBefore.V_normal );

			V_point = LNX_Utils.CreateCornerPathPoint( ptBefore, ptAFter );

			V_normal = ptBefore.V_normal;


			flag_switchGravityOff = false;
			flag_switchGravityOn = false;
			AmEndPoint = false;
			DebugClass = string.Empty;

			DebugClass += $"ptBefore.V_toPrev: '{ptBefore.V_toPrev}'\n";
		}



		public void DetermineGravityRequirement(float slopeHeight_switchGravity)
		{
			if (Mathf.Abs(V_toNext.y) >= slopeHeight_switchGravity)
			{
				flag_switchGravityOff = true;
			}
			else if (Mathf.Abs(V_toPrev.y) >= slopeHeight_switchGravity && Mathf.Abs(V_toNext.y) < slopeHeight_switchGravity)
			{
				flag_switchGravityOn = true;
			}
		}

		public void SetMyPreviousPoint(Vector3 prevPt)
		{
			V_toPrev = Vector3.Normalize(prevPt - V_point);
			Dist_toPrev = Vector3.Distance(V_point, prevPt);
			DebugClass += $"V_toPrev: '{V_toPrev}'\n";

		}

		public void SetMyNextPoint(Vector3 nxtPt)
		{
			V_toNext = Vector3.Normalize(nxtPt - V_point);
			Dist_toNext = Vector3.Distance(V_point, nxtPt);
			DebugClass += $"V_toNext: '{V_toPrev}'\n";

		}

		public void SetPreviousAndNextPoints(Vector3 prevPt, Vector3 nextPt)
		{
			SetMyPreviousPoint(prevPt);
			SetMyNextPoint(nextPt);
		}

		public void SetSpatialValues( LNX_PathPoint ptBefore, float slopeHeight_switchGravity, LNX_PathPoint ptAfter )
		{
			DebugClass += $"SetSpatialValues( before: '{ptBefore.V_point}', after: '{ptAfter.V_point}')\n";
			/*if( Flag_amCorner )
			{
				DebugClass += $"found am corner...\n";
				V_point = ptBefore.V_point + Vector3.Project(ptAfter.V_point - ptBefore.V_point, ptBefore.V_toPrev); //
			}*/

			SetPreviousAndNextPoints(ptBefore.V_point, ptAfter.V_point);

			DetermineGravityRequirement(slopeHeight_switchGravity);
		}

		public void SetSpatialValues( LNX_PathPoint ptBefore, float slopeHeight_switchGravity )
		{
			SetMyPreviousPoint(ptBefore.V_point);

			DetermineGravityRequirement(slopeHeight_switchGravity);

		}

		public void SetSpatialValues( float slopeHeight_switchGravity, LNX_PathPoint ptAfter )
		{
			SetMyNextPoint(ptAfter.V_point);

			DetermineGravityRequirement(slopeHeight_switchGravity);

		}
	}
}