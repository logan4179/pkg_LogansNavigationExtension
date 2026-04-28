
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public struct LNX_Path
	{
		public List<LNX_NavmeshHit> PathPoints;

		public LNX_NavmeshHit StartHit => PathPoints[0];
		public Vector3 StartPosition => PathPoints[0].Position;
		public LNX_ComponentCoordinate StartCoordinate => PathPoints[0].Coordinate;

		public Vector3 EndPosition => PathPoints[PathPoints.Count - 1].Position;
		public LNX_ComponentCoordinate EndCoordinate => PathPoints[PathPoints.Count - 1].Coordinate;
		public int EndTriIndex => EndCoordinate.TrianglesIndex;
		public int EndComponentIndex => EndCoordinate.ComponentIndex;
		public LNX_NavmeshHit EndHit => PathPoints[PathPoints.Count - 1];

		/// <summary>A Vector pointing in a straight line from start to end.</summary>
		public Vector3 V_CrowFlies => PathPoints[PathPoints.Count-1].Position - PathPoints[0].Position;

		public Vector3 V_CrowFlies_flattened => LNX_Utils.FlatVector( V_CrowFlies, v_navmeshSurfaceProjection_cached );

		[SerializeField] private Vector3 v_navmeshSurfaceProjection_cached;

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
		public LNX_Path( LNX_NavMesh nm )
		{
			amStraight = true;
			totalDistance_cached = 0f;
			v_navmeshSurfaceProjection_cached = nm.GetSurfaceProjectionVector();
			PathPoints = new List<LNX_NavmeshHit>();
		}

		public LNX_Path( List<Vector3> pts, List<Vector3>nrmls, Vector3 v_surfaceProjection )
		{
			amStraight = true;
			totalDistance_cached = 0f;
			v_navmeshSurfaceProjection_cached = v_surfaceProjection;

			PathPoints = new List<LNX_NavmeshHit>();

			if ( pts != null && pts.Count > 1 )
			{
				Vector3 dirTo = LNX_Utils.FlatVector( pts[1] - pts[0], v_surfaceProjection).normalized;

				for ( int i = 0; i < pts.Count; i++ )
				{
					PathPoints.Add( new LNX_NavmeshHit( pts[i], nrmls[i]) );

					if( i > 0 )
					{
						totalDistance_cached += Vector3.Distance( pts[i - 1], pts[i] );

						if( amStraight ) //only check the following if I still think I'm straight...
						{
							Vector3 dirNew = LNX_Utils.FlatVector( pts[i] - pts[i-1], v_surfaceProjection ).normalized;
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

		public LNX_Path( LNX_Path basePath )
		{
			amStraight = basePath.amStraight;
			totalDistance_cached = basePath.totalDistance_cached;
			v_navmeshSurfaceProjection_cached = basePath.v_navmeshSurfaceProjection_cached;

			PathPoints = new List<LNX_NavmeshHit>();

			if ( basePath.PathPoints != null && basePath.PathPoints.Count > 1 )
			{
				Vector3 dirTo = LNX_Utils.FlatVector( basePath.PathPoints[1].Position - basePath.PathPoints[0].Position,
					v_navmeshSurfaceProjection_cached ).normalized;

				for ( int i = 0; i < basePath.PathPoints.Count; i++ )
				{
					PathPoints.Add( basePath.PathPoints[i] );
				}
			}
		}

		public LNX_Path( LNX_Path basePathA, LNX_Path basePathB )
		{
			amStraight = basePathA.AmStraight && basePathB.amStraight && basePathA.V_CrowFlies == basePathB.V_CrowFlies;
			totalDistance_cached = basePathA.totalDistance_cached + basePathB.totalDistance_cached;
			v_navmeshSurfaceProjection_cached = basePathA.v_navmeshSurfaceProjection_cached;

			PathPoints = new List<LNX_NavmeshHit>();

			if ( basePathA.PathPoints != null && basePathA.PathPoints.Count > 1 )
			{
				for ( int i = 0; i < basePathA.PathPoints.Count; i++)
				{
					PathPoints.Add( basePathA.PathPoints[i] );
				}
			}

			if (basePathB.PathPoints != null && basePathB.PathPoints.Count > 1)
			{
				for (int i = 0; i < basePathB.PathPoints.Count; i++)
				{
					if( i == 0 && basePathA.PathPoints != null && basePathA.PathPoints.Count > 0 && 
						basePathB.StartHit == basePathA.EndHit)
					{
						continue; //don't log if same...
					}

					PathPoints.Add(basePathB.PathPoints[i]);
				}
			}
		}

		public LNX_Path(List<Vector3> pts, List<Vector3> nrmls, bool straightness, Vector3 v_surfaceProjection)
		{
			amStraight = straightness;
			totalDistance_cached = 0f;
			v_navmeshSurfaceProjection_cached = v_surfaceProjection;

			PathPoints = new List<LNX_NavmeshHit>();

			if (pts != null && pts.Count > 1)
			{
				for (int i = 0; i < pts.Count; i++)
				{
					PathPoints.Add( new LNX_NavmeshHit(pts[i], nrmls[i]) );

					if (i > 0)
					{
						totalDistance_cached += Vector3.Distance(pts[i - 1], pts[i]);
					}
				}
			}
		}
		
		/*
		public LNX_Path( LNX_NavmeshHit startHit, params LNX_Vertex[] verts ) //I don't think I should use this bc it won't have correct pathing if I just pass in a collection of verts.
		{
			PathPoints = new List<LNX_NavmeshHit>();
			totalDistance_cached = 0f;
			amStraight = true;

			PathPoints.Add( new LNX_NavmeshHit(startHit.HitPosition, startHit.Normal) );

			for ( int i = 0; i < verts.Length; i++ )
			{
				PathPoints.Add( new LNX_NavmeshHit(verts[i].V_Position, verts[i].CachedSurfaceNormal) );
			}
		}
		*/

		public LNX_Path( LNX_NavMesh navmesh, params LNX_NavmeshHit[] hits )
		{
			PathPoints = hits.ToList();
			totalDistance_cached = 0f;
			amStraight = true;
			v_navmeshSurfaceProjection_cached = navmesh.GetSurfaceProjectionVector();

			if ( hits == null || hits.Length <= 0 )
			{
				return;
			}

			Vector3 dirTo = LNX_Utils.FlatVector( hits[1].Position - hits[0].Position, v_navmeshSurfaceProjection_cached ).normalized;

			for ( int i = 0; i < hits.Length; i++ )
			{
				if ( i > 0 )
				{
					totalDistance_cached += Vector3.Distance( hits[i - 1].Position, hits[i].Position );

					if ( amStraight ) //only check the following if I still think I'm straight...
					{
						Vector3 dirNew = LNX_Utils.FlatVector(hits[i].Position - hits[i - 1].Position, v_navmeshSurfaceProjection_cached).normalized;
						if ( dirNew != dirTo )
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
		public LNX_Path( LNX_NavMesh nm, LNX_Path path, LNX_Vertex endVert ) //I think it's possible we might not want to use this one
		{
			PathPoints = new List<LNX_NavmeshHit>();
			totalDistance_cached = 0f;

			for ( int i = 0; i < path.PathPoints.Count; i++ )
			{
				PathPoints.Add( path.PathPoints[i] );
			}
			
			LNX_Path continuationPath;
			string s;

			if
			( 
				LNX_Utils.TryProjectPathThrough
				(
					nm, new LNX_NavmeshHit(-1, path.EndPosition), endVert, out continuationPath
				)
			)
			{

			}

			Vector3 dirTo = LNX_Utils.FlatVector(hits[1].HitPosition - hits[0].HitPosition, navmesh.GetSurfaceNormalVector()).normalized;

			for (int i = 0; i < hits.Length; i++)
			{
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


			//////////////////////
			PathPoints = path.PathPoints;
			amStraight = path.amStraight;

			totalDistance_cached = path.TotalDistance;

			AddPoint( endVert );
		}
		*/
		#endregion -------------------------------------------------

		#region MAIN API METHODS ============================================
		public void AddPoint( Vector3 pos, Vector3 nrml )
		{
			if (PathPoints == null)
			{
				PathPoints = new List<LNX_NavmeshHit>();
			}

			PathPoints.Add( new LNX_NavmeshHit(pos, nrml) );

			if( PathPoints.Count <= 1 )
			{
				totalDistance_cached = 0f;
				return;
			}

			totalDistance_cached += Vector3.Distance( PathPoints[PathPoints.Count - 1].Position, pos );
			
			//determine straightness
			if ( PathPoints.Count > 1 )
			{
				if (amStraight) //Need to decide if path is straight...
				{
					Vector3 firstDir_fltnd = LNX_Utils.FlatVector( PathPoints[1].Position - PathPoints[0].Position, nrml ).normalized;

					Vector3 dirNew = LNX_Utils.FlatVector( pos - PathPoints[PathPoints.Count - 2].Position, nrml ).normalized;

					if (dirNew != firstDir_fltnd)
					{
						amStraight = false;
					}
				}
			}
		}

		public void AddPoint( LNX_NavmeshHit pt )
		{
			if (PathPoints == null)
			{
				PathPoints = new List<LNX_NavmeshHit>();
			}

			PathPoints.Add( pt );

			if (PathPoints.Count <= 1)
			{
				totalDistance_cached = 0f;
				return;
			}

			totalDistance_cached += Vector3.Distance( PathPoints[PathPoints.Count - 1].Position, pt.Position );

			//determine straightness
			if (PathPoints.Count > 1)
			{
				if (amStraight) //Need to decide if path is still straight...
				{
					Vector3 firstDir_fltnd = LNX_Utils.FlatVector(PathPoints[1].Position - PathPoints[0].Position, pt.Normal).normalized; //todo: can get rid of these two variables, and just do the if statement with these expressions. Want to efficiency test doing this

					Vector3 dirNew = LNX_Utils.FlatVector(pt.Position - PathPoints[PathPoints.Count - 2].Position, pt.Normal).normalized;//<<

					if (dirNew != firstDir_fltnd)
					{
						amStraight = false;
					}
				}
			}
		}

		public void AddPath( LNX_Path path )
		{
			if( PathPoints == null )
			{
				PathPoints = new List<LNX_NavmeshHit>();
			}

			for (int i = 0; i < path.PathPoints.Count; i++)
			{
				//AddPoint( path.PathPoints[i] );
				PathPoints.Add( path.PathPoints[i] );

				if (amStraight && PathPoints.Count > 1 ) //Need to decide if path is still straight...
				{
					Vector3 firstDir_fltnd = LNX_Utils.FlatVector(PathPoints[1].Position - PathPoints[0].Position, v_navmeshSurfaceProjection_cached).normalized;

					Vector3 dirNew = LNX_Utils.FlatVector(path.PathPoints[i].Position - PathPoints[PathPoints.Count - 2].Position, v_navmeshSurfaceProjection_cached).normalized;

					if (dirNew != firstDir_fltnd)
					{
						amStraight = false;
					}
				}
			}

			totalDistance_cached += path.totalDistance_cached;
		}
		#endregion

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

			return PathPoints[ptIndx-1].Position - PathPoints[ptIndx].Position;
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

			return PathPoints[ptIndx].Position - PathPoints[ptIndx - 1].Position;
		}

		public bool AmOnCourse( int currentPtIndx, Vector3 pos_passed, float threshold, float dist_checkIfOffCourseBeyondPrev)
		{
			if (currentPtIndx == 0 )
			{
				if ( Vector3.Distance(pos_passed, PathPoints[currentPtIndx].Position) <= 0.25f )
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
				float distToPrev = Vector3.Distance(pos_passed, PathPoints[currentPtIndx - 1].Position);
				//Vector3 v_prevToPos = Vector3.Normalize(pos_passed - PathPoints[currentPtIndx - 1].V_Position);

				float myDot = Vector3.Dot(
					GetVectorPointingToPreviousPoint(currentPtIndx - 1).normalized, 
					GetVectorPointingToNextPoint(currentPtIndx-1).normalized
				);

				return (distToPrev < dist_checkIfOffCourseBeyondPrev || myDot >= threshold);
			}
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
				Gizmos.DrawSphere( PathPoints[i].Position, pointSize );

				Gizmos.DrawLine(
					PathPoints[i].Position, PathPoints[i].Position + (PathPoints[i].Normal * lblHeight)
				);
				Handles.Label(
					PathPoints[i].Position + (PathPoints[i].Normal * lblHeight), $"{i}" 
				);

				if( i > 0 )
				{
					Handles.DrawDottedLine(
						PathPoints[i-1].Position, PathPoints[i].Position, 8f
					);
				}
			}
		}

		#region OPERATORS ======================================================
		public static bool operator ==(LNX_Path a, LNX_Path b)
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
				otherPath.amStraight != amStraight
			)
			{
				return false;
			}

			if( (PathPoints == null && otherPath.PathPoints != null) || 
				(PathPoints != null && otherPath.PathPoints != null) )
			{
				return false;
			}

			if ( PathPoints != null && otherPath.PathPoints != null )
			{
				for (int i = 0; i < PathPoints.Count; i++)
				{
					if (otherPath.PathPoints[i] != PathPoints[i])
					{
						return false;
					}
				}
			}

			return true;
		}

		public static LNX_Path operator +(LNX_Path p1,
									 LNX_Path p2)
		{
			Debug.Log("it's hapening!");
			return new LNX_Path( p1, p2 );
		}

		public override int GetHashCode()
		{
		
			return HashCode.Combine(
				PathPoints, totalDistance_cached, v_navmeshSurfaceProjection_cached, amStraight
			);
		}

		public override string ToString()
		{
			if( !AmValid )
			{
				return $"[Invalid Path]";
			}

			return $"[{StartCoordinate}] -> [{EndCoordinate}]";
		}
		
		#endregion ---------------------------------------

	}
}