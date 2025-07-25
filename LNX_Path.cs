
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public struct LNX_Path
	{
		[HideInInspector] public int Index_currentPoint;
		public List<LNX_PathPoint> PathPoints;

		[HideInInspector] public Vector3 EndGoal => PathPoints[PathPoints.Count - 1].V_Point;

		private float distance;
		public float Distance => distance;


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
				return PathPoints[Index_currentPoint].V_Point; //todo: indexoutofrangeexception here
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

		private static LNX_Path none = new LNX_Path();

		public static LNX_Path None
		{
			get
			{
				return none;
			}
		}


		public LNX_Path( List<Vector3> pts, List<Vector3>nrmls )
		{

			Index_currentPoint = -1;
			PathPoints = new List<LNX_PathPoint>();
			dbgCalculatePath = string.Empty;
			dbgAmOnCourse = string.Empty;

			distance = 0f;
			if ( pts != null && pts.Count > 1 )
			{
				for ( int i = 0; i < pts.Count; i++ )
				{
					PathPoints.Add( new LNX_PathPoint(pts[i], nrmls[i]) );

					if( i > 0 )
					{
						distance += Vector3.Distance( pts[i - 1], pts[i] );
					}
				}
			}
		}

		public LNX_Path( LNX_Path path_passed )
		{
			Index_currentPoint = -1;
			PathPoints = path_passed.PathPoints;
			dbgCalculatePath = path_passed.dbgCalculatePath;
			dbgAmOnCourse = path_passed.dbgAmOnCourse;

			distance = path_passed.Distance;
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
				if ( Vector3.Distance(pos_passed, PathPoints[Index_currentPoint].V_Point) <= 0.25f )
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
				float distToPrev = Vector3.Distance(pos_passed, PathPoints[Index_currentPoint - 1].V_Point);
				Vector3 v_prevToPos = Vector3.Normalize(pos_passed - PathPoints[Index_currentPoint - 1].V_Point);
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
					myPath[i] = PathPoints[i].V_Point;
				}
			}

			return myPath;
		}
	}
}