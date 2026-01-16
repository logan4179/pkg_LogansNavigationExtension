
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using static System.Net.WebRequestMethods;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public struct LNX_Path
	{
		public List<LNX_PathPoint> PathPoints;

		public Vector3 StartPoint => PathPoints[0].V_Position;

		public Vector3 EndPosition => PathPoints[PathPoints.Count - 1].V_Position;
		/// <summary>A Vector pointing in a straight line from start to end.</summary>
		public Vector3 V_CrowFlies => PathPoints[PathPoints.Count-1].V_Position - PathPoints[0].V_Position;

		private float totalDistance_cached;
		/// <summary>Distance of the entire path.</summary>
		public float TotalDistance => totalDistance_cached;

		[SerializeField, HideInInspector] private bool amStraight;
		/// <summary>
		/// Whether this path was straight when calculated in relation to the surface 
		/// orientation of the navmesh. Note: This value is only relevant if this path is 
		/// constructed with a provided LNX_Navmesh or provided surface normal.
		/// </summary>
		public bool AmStraight => amStraight;

		/// <summary>Tells if this path object has valid data to be used for pathing.</summary>
		public bool AmValid
		{
			get
			{
				return PathPoints != null && PathPoints.Count > 0;
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

		#region CONSTRUCTORS =====================================================
		public LNX_Path( List<Vector3> pts, List<Vector3>nrmls, Vector3 v_surfaceNormal )
		{
			amStraight = true;
			totalDistance_cached = 0f;

			PathPoints = new List<LNX_PathPoint>();

			if ( pts != null && pts.Count > 1 )
			{
				Vector3 dirTo = LNX_Utils.FlatVector( pts[1] - pts[0] ).normalized;

				for ( int i = 0; i < pts.Count; i++ )
				{
					PathPoints.Add( new LNX_PathPoint( pts[i], nrmls[i]) );

					if( i > 0 )
					{
						totalDistance_cached += Vector3.Distance( pts[i - 1], pts[i] );

						if( amStraight ) //only check the following if I still think I'm straight...
						{
							Vector3 dirNew = LNX_Utils.FlatVector( pts[i] - pts[i-1], v_surfaceNormal ).normalized;
							if( dirNew != dirTo )
							{
								amStraight = false;
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
			amStraight = straightness;
			totalDistance_cached = 0f;

			PathPoints = new List<LNX_PathPoint>();

			if (pts != null && pts.Count > 1)
			{
				for (int i = 0; i < pts.Count; i++)
				{
					PathPoints.Add( new LNX_PathPoint(pts[i],nrmls[i]) );

					if (i > 0)
					{
						totalDistance_cached += Vector3.Distance(pts[i - 1], pts[i]);
					}
				}
			}
		}
		
		public LNX_Path( LNX_NavmeshHit startHit, params LNX_Vertex[] verts )
		{
			PathPoints = new List<LNX_PathPoint>();
			totalDistance_cached = 0f;
			amStraight = true;

			PathPoints.Add( new LNX_PathPoint(startHit.HitPosition, startHit.Normal) );

			for ( int i = 0; i < verts.Length; i++ )
			{
				PathPoints.Add( new LNX_PathPoint(verts[i].V_Position, verts[i].CachedSurfaceNormal) );
			}
		}

		public LNX_Path( List<LNX_NavmeshHit> hits, LNX_NavMesh navmesh )
		{
			PathPoints = new List<LNX_PathPoint>();
			totalDistance_cached = 0f;
			amStraight = true;

			if (hits == null || hits.Count <= 0)
			{
				return;
			}

			Vector3 dirTo = LNX_Utils.FlatVector( hits[1].HitPosition - hits[0].HitPosition, navmesh.GetSurfaceNormalVector() ).normalized;

			for ( int i = 0; i < hits.Count; i++ )
			{
				PathPoints.Add( new LNX_PathPoint(hits[i]) );

				if ( i > 0 )
				{
					totalDistance_cached += Vector3.Distance( hits[i-1].HitPosition, hits[i].HitPosition );

					if ( amStraight ) //only check the following if I still think I'm straight...
					{
						Vector3 dirNew = LNX_Utils.FlatVector(hits[i].HitPosition - hits[i - 1].HitPosition, navmesh.GetSurfaceNormalVector()).normalized;
						if (dirNew != dirTo)
						{
							amStraight = false;
						}
						else
						{
							dirTo = dirNew;
						}
					}
				}
			}
		}

		public LNX_Path( LNX_NavMesh navmesh, params LNX_NavmeshHit[] hits )
		{
			PathPoints = new List<LNX_PathPoint>();
			totalDistance_cached = 0f;
			amStraight = true;
			if (hits == null || hits.Length <= 0 )
			{
				return;
			}
			
			if( hits.Length == 1 )
			{
				PathPoints.Add( new LNX_PathPoint(hits[0]) );
				return;
			}


			Vector3 dirTo = LNX_Utils.FlatVector(hits[1].HitPosition - hits[0].HitPosition, navmesh.GetSurfaceNormalVector()).normalized;

			for ( int i = 0; i < hits.Length; i++ )
			{
				PathPoints.Add( new LNX_PathPoint(hits[i]) );

				if (i > 0)
				{
					totalDistance_cached += Vector3.Distance(hits[i - 1].HitPosition, hits[i].HitPosition);

					if (amStraight) //only check the following if I still think I'm straight...
					{
						Vector3 dirNew = LNX_Utils.FlatVector(hits[i].HitPosition - hits[i - 1].HitPosition, navmesh.GetSurfaceNormalVector()).normalized;
						if (dirNew != dirTo)
						{
							amStraight = false;
						}
						else
						{
							dirTo = dirNew;
						}
					}
				}
			}
		}

		/*
		public LNX_Path( LNX_Path path_passed )
		{
			this = path_passed;
			
			PathPoints = path_passed.PathPoints;
			amStraight = path_passed.amStraight;

			totalDistance_cached = path_passed.TotalDistance;
			
		}
		*/

		public LNX_Path( LNX_NavMesh nm, LNX_Path path, LNX_Vertex endVert )
		{
			LNX_Path endPath;
			string s;

			if
			( 
				LNX_Utils.TryProjectPathThrough
				(
					nm, new LNX_NavmeshHit(-1, path.EndPosition), endVert, out endPath, ref s
				)
			)
			{

			}


			PathPoints = path.PathPoints;
			amStraight = path.amStraight;

			totalDistance_cached = path.TotalDistance;

			AddPoint( endVert );
		}
		#endregion -------------------------------------------------

		#region OPERATORS ======================================================
		public static bool operator == (LNX_Path a, LNX_Path b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(LNX_Path a, LNX_Path b)
		{
			return !a.Equals(b);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is LNX_Path))
				return false;

			LNX_Path otherPath = (LNX_Path)obj;
			if 
			(
				otherPath.totalDistance_cached != totalDistance_cached || 
				otherPath.amStraight != amStraight || 
				otherPath.PathPoints.Count != otherPath.PathPoints.Count
			)
			{
				return false;
			}

			for ( int i = 0; i < PathPoints.Count; i++ )
			{
				if(otherPath.PathPoints[i] != PathPoints[i] )
				{
					return false;
				}
			}

			return true;
		}
		#endregion ---------------------------------------

		public void AddPoint( Vector3 pos, Vector3 nrml )
		{
			if (PathPoints == null)
			{
				//Debug.Log("pathpoints collectoin was null");
				PathPoints = new List<LNX_PathPoint>();
			}

			PathPoints.Add( new LNX_PathPoint(pos, nrml) );

			if( PathPoints.Count <= 1 )
			{
				totalDistance_cached = 0f;
				return;
			}

			totalDistance_cached += Vector3.Distance( PathPoints[PathPoints.Count - 1].V_Position, pos );
			
			//determine straightness
			if ( PathPoints.Count > 1 )
			{
				if (amStraight) //Need to decide if path is straight...
				{
					Vector3 firstDir_fltnd = LNX_Utils.FlatVector( PathPoints[1].V_Position - PathPoints[0].V_Position, nrml ).normalized;

					Vector3 dirNew = LNX_Utils.FlatVector( pos - PathPoints[PathPoints.Count - 2].V_Position, nrml ).normalized;

					if (dirNew != firstDir_fltnd)
					{
						amStraight = false;
					}
				}
			}
		}

		public void AddPoint( LNX_PathPoint pt )
		{
			AddPoint( pt.V_Position, pt.V_normal );
		}

		public void AddPoint( LNX_NavmeshHit hit, LNX_NavMesh _navmesh )
		{
			AddPoint( hit.HitPosition, _navmesh.GetSurfaceNormalVector() );
		}

		public void AddPoint( LNX_Vertex vert )
		{
			AddPoint( vert.V_Position, vert.CachedSurfaceNormal );
		}

		public void AddPath( LNX_Path path )
		{
			for (int i = 0; i < path.PathPoints.Count; i++)
			{
				AddPoint( path.PathPoints[i] );
			}
		}

		public Vector3 GetVectorPointingToPreviousPoint( int ptIndx )
		{
			if( ptIndx <= 0 )
			{
				Debug.LogError($"LNX ERROR! You passed {nameof(GetVectorPointingToPreviousPoint)}() with an index of 0. Cannot get a vector to " +
					$"a PathPoint that does not exit...");
				return Vector3.zero;
			}

			if( PathPoints == null || PathPoints.Count == 0 )
			{
				Debug.LogError($"LNX ERROR! {nameof(GetVectorPointingToPreviousPoint)}() cannot calculate a previous point because the path points " +
					$"list is null or 0-count...");
				return Vector3.zero;
			}

			if ( ptIndx > PathPoints.Count - 1 )
			{
				Debug.LogError($"LNX ERROR! You passed {nameof(GetVectorPointingToPreviousPoint)}() an index of {ptIndx}, but the path point list " +
					$"only contains {PathPoints.Count} points...");
				return Vector3.zero;
			}

			return PathPoints[ptIndx-1].V_Position - PathPoints[ptIndx].V_Position;
		}

		public Vector3 GetVectorPointingToNextPoint( int ptIndx )
		{
			if (ptIndx < 0)
			{
				Debug.LogError($"LNX ERROR! You passed {nameof(GetVectorPointingToPreviousPoint)}() with an index of {ptIndx}. Cannot get a vector to " +
					$"a PathPoint that does not exit...");
				return Vector3.zero;
			}

			if (PathPoints == null || PathPoints.Count == 0)
			{
				Debug.LogError($"LNX ERROR! {nameof(GetVectorPointingToPreviousPoint)}() cannot calculate a previous point because the path points " +
					$"list is null or 0-count...");
				return Vector3.zero;
			}

			if ( ptIndx > PathPoints.Count - 2 )
			{
				Debug.LogError($"LNX ERROR! You passed {nameof(GetVectorPointingToPreviousPoint)}() an index of {ptIndx}, but the path point list " +
					$"only contains {PathPoints.Count} points. Can't get next point...");
				return Vector3.zero;
			}

			return PathPoints[ptIndx].V_Position - PathPoints[ptIndx - 1].V_Position;
		}

		public bool AmOnCourse( int currentPtIndx, Vector3 pos_passed, float threshold, float dist_checkIfOffCourseBeyondPrev)
		{
			if (currentPtIndx == 0 )
			{
				if ( Vector3.Distance(pos_passed, PathPoints[currentPtIndx].V_Position) <= 0.25f )
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
				float distToPrev = Vector3.Distance(pos_passed, PathPoints[currentPtIndx - 1].V_Position);
				//Vector3 v_prevToPos = Vector3.Normalize(pos_passed - PathPoints[currentPtIndx - 1].V_Position);

				float myDot = Vector3.Dot(
					GetVectorPointingToPreviousPoint(currentPtIndx - 1).normalized, 
					GetVectorPointingToNextPoint(currentPtIndx-1).normalized
				);

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

		public float GetCombinedDistance(LNX_Path pth)
		{
			return totalDistance_cached + pth.totalDistance_cached;
		}

		public void DrawMyGizmos( float pointSize, float lblHeight )
		{
			if( PathPoints == null || PathPoints.Count <= 0 )
			{
				return;
			}

			for ( int i = 0; i < PathPoints.Count; i++ )
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