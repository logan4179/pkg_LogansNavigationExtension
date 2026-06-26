
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

		public int PointCount => (PathPoints != null && PathPoints.Count > -1) ? PathPoints.Count : -1;
		public LNX_NavmeshHit StartHit => PathPoints[0];
		public Vector3 StartPosition => PathPoints[0].Position;

		public Vector3 EndPosition => PathPoints[PathPoints.Count - 1].Position;
		public LNX_NavmeshHit EndHit => PathPoints[PathPoints.Count - 1];
		public int EndTriIndex => PathPoints[PathPoints.Count - 1].TriangleIndex;
		public LNX_ComponentCoordinate EndCoordinate_vert
		{
			get
			{
				if ( PathPoints == null || PathPoints.Count <= 0 )
				{
					return LNX_ComponentCoordinate.None;
				}
				else
				{
					return new LNX_ComponentCoordinate( PathPoints[PathPoints.Count-1].TriangleIndex, PathPoints[PathPoints.Count-1].VertIndex );
				}
			}
		}


		/// <summary>A Vector pointing in a straight line from start to end.</summary>
		public Vector3 V_CrowFlies => PathPoints[PathPoints.Count-1].Position - PathPoints[0].Position;
		public Vector3 V_CrowFiles_flat => LNX_Utils.FlatVector(PathPoints[PathPoints.Count - 1].Position - PathPoints[0].Position, v_navmeshSurfaceProjection_cached);

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

		//[TextArea(1,20)] public string DBG_class;


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
			//DBG_class = $"ctorA\n";
			amStraight = true;
			totalDistance_cached = 0f;
			v_navmeshSurfaceProjection_cached = nm.GetSurfaceProjectionVector();
			PathPoints = new List<LNX_NavmeshHit>();
		}

		public LNX_Path( LNX_Path basePath )
		{
			//DBG_class = $"ctorB\n" + basePath.DBG_class;

			amStraight = basePath.AmStraight;
			totalDistance_cached = basePath.totalDistance_cached;
			v_navmeshSurfaceProjection_cached = basePath.v_navmeshSurfaceProjection_cached;

			PathPoints = new List<LNX_NavmeshHit>();

			if ( basePath.PathPoints != null && basePath.PathPoints.Count > 1)
			{
				for (int i = 0; i < basePath.PathPoints.Count; i++)
				{
					AddPoint( basePath.PathPoints[i] );
				}
			}
		}

		public LNX_Path( LNX_Path basePathA, LNX_Path basePathB )//////////////////////
		{
			//DBG_class = $"ctorC\n";

			amStraight = basePathA.AmStraight && basePathB.amStraight && basePathA.V_CrowFlies == basePathB.V_CrowFlies;
			totalDistance_cached = basePathA.totalDistance_cached + basePathB.totalDistance_cached;
			v_navmeshSurfaceProjection_cached = basePathA.v_navmeshSurfaceProjection_cached;

			PathPoints = new List<LNX_NavmeshHit>();

			//DBG_class += $"adding pathpoints from constructor paths...\n";

			if ( basePathA.PathPoints != null && basePathA.PathPoints.Count > 1 )
			{
				//DBG_class += $"basePathA points are valid with '{basePathA.PathPoints.Count}' pts...\n";
				for ( int i = 0; i < basePathA.PathPoints.Count; i++)
				{
					//DBG_class += $"for'{i}' ({basePathA.PathPoints[i]})...\n";
					AddPoint( basePathA.PathPoints[i] );
				}

				//DBG_class += $"finished adding basePathA's points. pt count: '{PointCount}'. dist: '{TotalDistance}'...\n";
			}

			if (basePathB.PathPoints != null && basePathB.PathPoints.Count > 1)
			{
				//DBG_class += $"basePathB points are valid with '{basePathB.PathPoints.Count}' pts...\n";

				for (int i = 0; i < basePathB.PathPoints.Count; i++)
				{
					//DBG_class += $"for'{i}' ({basePathB.PathPoints[i]})...\n";

					if ( i == 0 && basePathA.PathPoints != null && basePathA.PathPoints.Count > 0 && 
						basePathB.StartHit.Position == basePathA.EndHit.Position)
					{
						//DBG_class += $"first pt of pathB is same as last logged point. continuing..\n";
						continue;
					}
					else
					{
						AddPoint(basePathB.PathPoints[i]);
					}
				}
				//DBG_class += $"finished adding basePathA's points. pt count: '{PointCount}'. dist: '{TotalDistance}'...\n";

			}
		}
		
		public LNX_Path( Vector3 nvmshProjectionDir, params LNX_NavmeshHit[] hits)
		{
			//DBG_class = $"ctorD\n";

			PathPoints = hits.ToList();
			totalDistance_cached = 0f;
			amStraight = true;
			v_navmeshSurfaceProjection_cached = nvmshProjectionDir;

			if (hits == null || hits.Length <= 0)
			{
				return;
			}

			Vector3 dirTo = LNX_Utils.FlatVector(hits[1].Position - hits[0].Position, v_navmeshSurfaceProjection_cached).normalized;

			for (int i = 0; i < hits.Length; i++)
			{
				if (i > 0)
				{
					totalDistance_cached += Vector3.Distance(hits[i - 1].Position, hits[i].Position);

					if (amStraight) //only check the following if I still think I'm straight...
					{
						Vector3 dirNew = LNX_Utils.FlatVector(hits[i].Position - hits[i - 1].Position, v_navmeshSurfaceProjection_cached).normalized;
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

		public LNX_Path(Vector3 nvmshProjectionDir, List<LNX_NavmeshHit> hits)
		{
			//DBG_class = $"ctorE\n";

			PathPoints = hits.ToList();
			totalDistance_cached = 0f;
			amStraight = true;
			v_navmeshSurfaceProjection_cached = nvmshProjectionDir;

			if (hits == null || hits.Count <= 0)
			{
				return;
			}

			Vector3 dirTo = LNX_Utils.FlatVector(hits[1].Position - hits[0].Position, v_navmeshSurfaceProjection_cached).normalized;

			for (int i = 0; i < hits.Count; i++)
			{
				if (i > 0)
				{
					totalDistance_cached += Vector3.Distance(hits[i - 1].Position, hits[i].Position);

					if (amStraight) //only check the following if I still think I'm straight...
					{
						Vector3 dirNew = LNX_Utils.FlatVector(hits[i].Position - hits[i - 1].Position, v_navmeshSurfaceProjection_cached).normalized;
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

		#endregion -------------------------------------------------

		#region MAIN API METHODS ============================================

		public void AddPoint( LNX_NavmeshHit pt )
		{
			//Debug.Log($"string size: '{DBG_class.Length}'");
			//DBG_class += $"AddPoint('{pt}')\n"; //<<<<<<<<<<<<<<<<<<<<<<<<<
			if (PathPoints == null)
			{
				PathPoints = new List<LNX_NavmeshHit>();
			}

			PathPoints.Add( pt );

			if (PathPoints.Count <= 1)
			{
				//DBG_class += $"pathpoints count <= 1...\n";
				totalDistance_cached = 0f;
				return;
			}

			totalDistance_cached += Vector3.Distance( PathPoints[PathPoints.Count - 2].Position, pt.Position );
			//DBG_class += $"adding dist: '{Vector3.Distance(PathPoints[PathPoints.Count - 2].Position, pt.Position)}', new totalDist: '{totalDistance_cached}'\n";

			//determine straightness
			if ( PathPoints.Count > 1 )
			{
				if (amStraight) //Need to decide if path is still straight...
				{
					Vector3 firstDir_fltnd = LNX_Utils.FlatVector(PathPoints[1].Position - PathPoints[0].Position, pt.Normal).normalized; //todo: can get rid of these two variables, and just do the if statement with these expressions. Want to efficiency test doing this

					Vector3 dirNew = LNX_Utils.FlatVector(pt.Position - PathPoints[PathPoints.Count - 2].Position, pt.Normal).normalized;//<<
					//DBG_class += $"determining straightness using firstDir: '{LNX_UnitTestUtilities.LongVectorString(firstDir_fltnd)}', " +
						//$"newDir: '{LNX_UnitTestUtilities.LongVectorString(dirNew)}'\n";

					/*
					if (dirNew != firstDir_fltnd)
					{
						// todo: issue: for some reason, this check sometimes gets erroneously triggered due to the vectors being slighly
						// different (by like a 10,000th of a percentage, extremely small floating point precision difference). My tests
						// show that when this happens, using Vector3.Angle() to test directionality instead seems a little more reliable
						// at considering the two angles to be the same. I wonder if LNX_Triangle.ProjectOnPerimeter() is still not perfect
						// and perhaps producing a projection that is just a little off. Using the following angle check instead seems pretty
						// okay for now. Perhaps one day I should revisit this and improve it?
						DBG_class += $"decided not equal. angDiff: '{Vector3.Angle(firstDir_fltnd, dirNew)}'. Changing amStraight to false...\n";
						amStraight = false;
					}*/
					if( Vector3.Angle(firstDir_fltnd, dirNew) > 0f )
					{
						//DBG_class += $"decided not equal. angDiff: '{Vector3.Angle(firstDir_fltnd, dirNew)}'. Changing amStraight to false...\n";
						amStraight = false;
					}
					else
					{
						//DBG_class += $"decided AM equal...\n";
					}
				}
			}
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

		public void DrawMyGizmos( float pointSize, float lblHeight, bool drawFullLabels = false )
		{
			if( PathPoints == null || PathPoints.Count <= 0 )
			{
				return;
			}

			Vector3 vRise = v_navmeshSurfaceProjection_cached * 0.5f * pointSize;
			for ( int i = 0; i < PathPoints.Count; i++ )
			{
				Gizmos.DrawSphere( PathPoints[i].Position, pointSize );

				Gizmos.DrawLine(
					PathPoints[i].Position, PathPoints[i].Position + (PathPoints[i].Normal * lblHeight)
				);

				if( drawFullLabels )
				{
					Handles.Label(
						PathPoints[i].Position + (PathPoints[i].Normal * lblHeight * 1.01f), $"{i}\n{PathPoints[i]}" 
					);
				}
				else
				{
					Handles.Label(
						PathPoints[i].Position + (PathPoints[i].Normal * lblHeight * 1.01f), $"{i}"
					);
				}

				if (i > 0)
				{
					Handles.DrawDottedLine(
						PathPoints[i - 1].Position + vRise, PathPoints[i].Position + vRise, 8f
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
			/*
			if
			(
				otherPath.totalDistance_cached != totalDistance_cached ||
				otherPath.amStraight != amStraight
			)
			{
				return false;
			}
			*/

			if( (PathPoints == null && otherPath.PathPoints != null) || 
				(PathPoints != null && otherPath.PathPoints == null) )
			{
				return false;
			}

			if ( PathPoints != null && otherPath.PathPoints != null &&
				PathPoints.Count != otherPath.PathPoints.Count
			)
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

			return $"LNX_Path{StartHit}_->_{EndHit}";
		}
		
		#endregion ---------------------------------------

	}
}