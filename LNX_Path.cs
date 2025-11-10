
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public struct LNX_Path
	{
		[HideInInspector] public int Index_currentPoint; //todo: do I even need this value?

		public List<LNX_PathPoint> PathPoints;

		public Vector3 StartPoint => PathPoints[0].V_Position;

		public Vector3 EndPoint => PathPoints[PathPoints.Count - 1].V_Position;
		/// <summary>A Vector pointing in a straight line from start to end.</summary>
		public Vector3 V_CrowFlies => PathPoints[PathPoints.Count-1].V_Position - PathPoints[0].V_Position;

		private float totalDistance_cached;
		/// <summary>Distance of the entire path.</summary>
		public float TotalDistance => totalDistance_cached;

		private bool amStraight_cached;
		/// <summary>
		/// Whether this path was straight when calculated in relation to the surface 
		/// orientation of the navmesh. Note: This value is only relevant if this path is 
		/// constructed with a provided LNX_Navmesh or provided surface normal.
		/// </summary>
		public bool AmStraight => amStraight_cached;

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
				return PathPoints[Index_currentPoint].V_Position; //todo: indexoutofrangeexception here
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

		private static LNX_Path none = new LNX_Path();

		public static LNX_Path None
		{
			get
			{
				return none;
			}
		}

		public LNX_Path( List<Vector3> pts, List<Vector3>nrmls, Vector3 v_surfaceNormal )
		{
			Index_currentPoint = 0;
			amStraight_cached = true;
			totalDistance_cached = 0f;

			PathPoints = new List<LNX_PathPoint>();

			if ( pts != null && pts.Count > 1 )
			{
				Vector3 dirTo = LNX_Utils.FlatVector( pts[1] - pts[0] ).normalized;

				for ( int i = 0; i < pts.Count; i++ )
				{
					PathPoints.Add
					( 
						new LNX_PathPoint
						(
							pts[i], 
							(i > 0 ? pts[i-1] : pts[i]),
							(i < pts.Count-1 ? pts[i+1] : pts[i]),
							nrmls[i]
						)
					);

					if( i > 0 )
					{
						totalDistance_cached += Vector3.Distance( pts[i - 1], pts[i] );

						if( amStraight_cached ) //only check the following if I still think I'm straight...
						{
							Vector3 dirNew = LNX_Utils.FlatVector( pts[i] - pts[i-1], v_surfaceNormal ).normalized;
							if( dirNew != dirTo )
							{
								amStraight_cached = false;
							}
							else
							{
								dirTo = dirNew;
							}
						}
					}
				}
			}
		}

		
		public LNX_Path(List<Vector3> pts, List<Vector3> nrmls, bool straightness )
		{
			Index_currentPoint = 0;
			amStraight_cached = straightness;
			totalDistance_cached = 0f;

			PathPoints = new List<LNX_PathPoint>();

			if (pts != null && pts.Count > 1)
			{
				for (int i = 0; i < pts.Count; i++)
				{
					PathPoints.Add
					(
						new LNX_PathPoint
						(
							pts[i],
							(i > 0 ? pts[i - 1] : pts[i]),
							(i < pts.Count - 1 ? pts[i + 1] : pts[i]),
							nrmls[i]
						)
					);

					if (i > 0)
					{
						totalDistance_cached += Vector3.Distance(pts[i - 1], pts[i]);
					}
				}
			}
		}
		

		public LNX_Path( List<LNX_ProjectionHit> hits, LNX_NavMesh navmesh )
		{
			Index_currentPoint = -1;
			PathPoints = new List<LNX_PathPoint>();
			totalDistance_cached = 0f;
			amStraight_cached = true;

			if ( hits != null && hits.Count > 1 )
			{

				Vector3 dirTo = LNX_Utils.FlatVector( hits[1].HitPosition - hits[0].HitPosition, navmesh.V_SurfaceOrientation ).normalized;
				amStraight_cached = true;

				for ( int i = 0; i < hits.Count; i++ )
				{
					PathPoints.Add //StackTrace 1
					( 
						new LNX_PathPoint
						(
							hits[i].HitPosition, 
							(i > 0 ? hits[i-1].HitPosition : hits[i].HitPosition),
							(i < hits.Count - 1 ? hits[i+1].HitPosition : hits[i].HitPosition),
							navmesh.Triangles[hits[i].Index_Hit].V_PathingNormal
						)
					);

					if ( i > 0 )
					{
						totalDistance_cached += Vector3.Distance( hits[i-1].HitPosition, hits[i].HitPosition );

						if (amStraight_cached) //only check the following if I still think I'm straight...
						{
							Vector3 dirNew = LNX_Utils.FlatVector(hits[i].HitPosition - hits[i - 1].HitPosition, navmesh.V_SurfaceOrientation).normalized;
							if (dirNew != dirTo)
							{
								amStraight_cached = false;
							}
							else
							{
								dirTo = dirNew;
							}
						}
					}
				}
			}
		}

		public LNX_Path( LNX_Path path_passed )
		{
			Index_currentPoint = 0;
			PathPoints = path_passed.PathPoints;
			amStraight_cached = false;

			totalDistance_cached = path_passed.TotalDistance;
		}

		private static float dist_checkIfOffCourseBeyondPrev = 0.4f;

		public void AddPoint( LNX_ProjectionHit hitPt, LNX_NavMesh _navmesh )
		{
			if (PathPoints == null)
			{
				//Debug.Log("pathpoints collectoin was null");
				PathPoints = new List<LNX_PathPoint>();
			}

			Vector3 vprv = PathPoints.Count == 0 ? hitPt.HitPosition : PathPoints[PathPoints.Count-1].V_Position;

			PathPoints.Add
			( 
				new LNX_PathPoint
				(
					hitPt.HitPosition,
					vprv,
					hitPt.HitPosition,
					_navmesh.Triangles[hitPt.Index_Hit].V_PathingNormal
				) 
			);


			totalDistance_cached += Vector3.Distance( vprv, hitPt.HitPosition );

			//TODO: determine straightness!!!
			if ( PathPoints.Count > 1 )
			{
				#region take care of previous point...
				PathPoints[PathPoints.Count - 2] = new LNX_PathPoint(
					PathPoints[PathPoints.Count - 2].V_Position,
					PathPoints[PathPoints.Count - 2].V_PreviousPoint,
					hitPt.HitPosition,
					PathPoints[PathPoints.Count - 2].V_normal
				);
				#endregion

				if ( amStraight_cached ) //Need to decide if path is straight...
				{
					Vector3 firstDir_fltnd = LNX_Utils.FlatVector( PathPoints[1].V_Position - PathPoints[0].V_Position, _navmesh.V_SurfaceOrientation ).normalized;

					Vector3 dirNew = LNX_Utils.FlatVector(
						hitPt.HitPosition - PathPoints[PathPoints.Count - 2].V_Position,
						_navmesh.V_SurfaceOrientation
					).normalized;

					if  ( dirNew != firstDir_fltnd )
					{
						amStraight_cached = false;
					}
				}
			}
		}

		public bool AmOnCourse( Vector3 pos_passed, float threshold )
		{
			if ( Index_currentPoint == 0 )
			{
				if ( Vector3.Distance(pos_passed, PathPoints[Index_currentPoint].V_Position) <= 0.25f )
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
				float distToPrev = Vector3.Distance(pos_passed, PathPoints[Index_currentPoint - 1].V_Position);
				Vector3 v_prevToPos = Vector3.Normalize(pos_passed - PathPoints[Index_currentPoint - 1].V_Position);
				float myDot = Vector3.Dot(v_prevToPos, PathPoints[Index_currentPoint - 1].V_ToNext);
				
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
					myPath[i] = PathPoints[i].V_Position;
				}
			}

			return myPath;
		}

		public void DrawMyGizmos( float pointSize, float lblHeight )
		{
			if( PathPoints == null || PathPoints.Count <= 0 )
			{
				return;
			}

			for( int i = 0; i < PathPoints.Count; i++ )
			{
				Gizmos.DrawSphere( PathPoints[i].V_Position, pointSize );

				Gizmos.DrawLine(
					PathPoints[i].V_Position, PathPoints[i].V_Position + (PathPoints[i].V_normal * lblHeight)
				);
				Handles.Label(
					PathPoints[i].V_Position + (PathPoints[i].V_normal * lblHeight), $"{i}" 
				);

				if( i > 0 )
				{
					Handles.DrawDottedLine(
						PathPoints[i-1].V_Position, PathPoints[i].V_Position, 8f
					);
				}
			}
		}
	}
}