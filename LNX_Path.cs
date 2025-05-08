
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_Path
	{

		/// <summary>If the v_toNext.y is greater than this, it will enable turnongravity.</summary>
		[SerializeField] private float slopeHeight_switchGravity;
		/// <summary>The distance within which we consider a patrolpoint to be a 'corner' when compared to the </summary>
		[SerializeField] private float dist_cornerThreshold;
		/// <summary>Highest number the dot product can be as a condition of considering if this point needs a following corner.</summary>
		[SerializeField] private float threshold_cornerDotCheck;

		[HideInInspector] public int Index_currentPoint;
		public List<LNX_PathPoint> PathPoints;

		[HideInInspector] public Vector3 EndGoal => PathPoints[PathPoints.Count - 1].V_point;

		[SerializeField, HideInInspector] private int mask_solidEnvironment;

		/// <summary>Tells if this path object has valid data to be used for pathing.</summary>
		public bool AmValid
		{
			get
			{
				return PathPoints != null && PathPoints.Count > 0;
			}
		}

		public LNX_PathPoint CurrentPathPoint
		{
			get
			{
				return PathPoints[Index_currentPoint]; //this sometimes triggers out of range exception
			}
		}

		public Vector3 CurrentGoal
		{
			get
			{
				return PathPoints[Index_currentPoint].V_point; //todo: indexoutofrangeexception here
			}
		}

		public bool AmOnEndGoal
		{
			get
			{
				if (AmValid && Index_currentPoint >= PathPoints.Count - 1)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		[SerializeField, TextArea(1, 8)] private string dbgCalculatePath;

		[TextArea(1, 5)] public string dbgAmOnCourse;

		public LNX_Path( int mask_passed )
		{
			mask_solidEnvironment = mask_passed;

			Index_currentPoint = -1;
			PathPoints = new List<LNX_PathPoint>();
			dbgCalculatePath = string.Empty;
			dbgAmOnCourse = string.Empty;
			slopeHeight_switchGravity = 0.35f;
			dist_cornerThreshold = 0.75f;
			threshold_cornerDotCheck = 0f;
		}

		public LNX_Path( LNX_Path path_passed )
		{
			mask_solidEnvironment = path_passed.mask_solidEnvironment;

			Index_currentPoint = -1;
			PathPoints = path_passed.PathPoints;
			dbgCalculatePath = path_passed.dbgCalculatePath;
			dbgAmOnCourse = path_passed.dbgAmOnCourse;
			slopeHeight_switchGravity = path_passed.slopeHeight_switchGravity;
			dist_cornerThreshold = path_passed.dist_cornerThreshold;
			threshold_cornerDotCheck = path_passed.threshold_cornerDotCheck;
		}

		private static float dist_checkIfOffCourseBeyondPrev = 0.4f;

		public void SetPath( List<Vector3> pathPts )
		{
			//PathPoints = pathPts;
		}
		public bool AmOnCourse( Vector3 pos_passed, float threshold )
		{
			if ( Index_currentPoint == 0 )
			{
				if ( Vector3.Distance(pos_passed, PathPoints[Index_currentPoint].V_point) <= 0.25f )
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				float distToPrev = Vector3.Distance(pos_passed, PathPoints[Index_currentPoint - 1].V_point);
				Vector3 v_prevToPos = Vector3.Normalize(pos_passed - PathPoints[Index_currentPoint - 1].V_point);
				float myDot = Vector3.Dot(v_prevToPos, PathPoints[Index_currentPoint - 1].V_toNext);
				dbgAmOnCourse = $"distToPrev: '{distToPrev}', DOT: '{myDot}', v_prevToPos: '{v_prevToPos}', V_toNext: '{CurrentPathPoint.V_toNext}'";
				
				return (distToPrev < dist_checkIfOffCourseBeyondPrev || myDot >= threshold);
			}
		}

		public Vector3[] GetPathVectors()
		{
			Vector3[] myPath = new Vector3[0];
			if ( AmValid )
			{
				myPath = new Vector3[PathPoints.Count];
				for ( int i = 0; i < PathPoints.Count; i++ )
				{
					myPath[i] = PathPoints[i].V_point;
				}
			}

			return myPath;
		}
	}
}