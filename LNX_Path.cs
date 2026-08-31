
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_Path
	{
		[SerializeField] private List<LNX_NavmeshHit> pathPoints = new List<LNX_NavmeshHit>();
		public List<LNX_NavmeshHit> PathPoints => pathPoints;

		public int PointCount => (pathPoints != null && pathPoints.Count > -1) ? pathPoints.Count : -1;
		public LNX_NavmeshHit StartHit => pathPoints[0];
		public Vector3 StartPosition => pathPoints[0].Position;

		public Vector3 EndPosition => pathPoints[pathPoints.Count - 1].Position;
		public LNX_NavmeshHit EndHit => pathPoints[pathPoints.Count - 1];
		public int EndTriIndex => pathPoints[pathPoints.Count - 1].TriangleIndex;
		public LNX_ComponentCoordinate EndCoordinate_vert
		{
			get
			{
				if ( pathPoints == null || pathPoints.Count <= 0 )
				{
					return LNX_ComponentCoordinate.None;
				}
				else
				{
					return new LNX_ComponentCoordinate( pathPoints[pathPoints.Count-1].TriangleIndex, pathPoints[pathPoints.Count-1].VertIndex );
				}
			}
		}


		/// <summary>A Vector pointing in a straight line from start to end.</summary>
		public Vector3 V_CrowFlies => pathPoints[pathPoints.Count-1].Position - pathPoints[0].Position;
		public Vector3 V_CrowFiles_flat => LNX_Utils.FlatVector(pathPoints[pathPoints.Count - 1].Position - pathPoints[0].Position, v_navmeshSurfaceProjection_cached);

		[SerializeField] private Vector3 v_navmeshSurfaceProjection_cached;
		public Vector3 V_navmeshSurfaceProjection_cached => v_navmeshSurfaceProjection_cached;

		[SerializeField] private float totalDistance_cached;
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
				//return (pathPoints != null || pathPoints.Count > 0; //this can cause problems if pathPoints is null because, due to the or operator, it will try to evaluate both of these
				return !(pathPoints == null || pathPoints.Count <= 0);
			}
		}

		//[TextArea(1,20)] public string DBG_class; //todo: dws


		#region CONSTRUCTORS =====================================================
		public LNX_Path()
		{
			pathPoints = null;
			totalDistance_cached = -1f;
		}

		public LNX_Path( LNX_NavMeshSurface nm )
		{
			//DBG_class = $"ctorA\n";
			amStraight = true;
			totalDistance_cached = 0f;
			v_navmeshSurfaceProjection_cached = nm.GetSurfaceProjectionVector();
			pathPoints = new List<LNX_NavmeshHit>();
		}

		public LNX_Path( LNX_Path basePath )
		{
			/*
			DBG_class = $"ctorB (1 base path)\n" +
				$"LNX_Path(basePath: '{basePath}')\n" +
				$"===================================\n" +
				$"basePath.DBG_class: '{basePath.DBG_class}'\n" +
				$"===================================\n" +
				$"";
			*/

			amStraight = basePath.AmStraight;
			totalDistance_cached = 0f;
			v_navmeshSurfaceProjection_cached = basePath.v_navmeshSurfaceProjection_cached;

			pathPoints = new List<LNX_NavmeshHit>();

			if ( basePath.pathPoints != null && basePath.pathPoints.Count > 0 )
			{
				for (int i = 0; i < basePath.pathPoints.Count; i++)
				{
					AddPoint( basePath.pathPoints[i] );
				}
			}
		}

		public LNX_Path(LNX_Path basePath, LNX_Vertex endVert)
		{
			amStraight = basePath.AmStraight;
			totalDistance_cached = 0f;
			v_navmeshSurfaceProjection_cached = basePath.v_navmeshSurfaceProjection_cached;

			pathPoints = new List<LNX_NavmeshHit>();

			if (basePath.pathPoints != null && basePath.pathPoints.Count > 0)
			{
				for (int i = 0; i < basePath.pathPoints.Count - 1; i++)
				{
					AddPoint(basePath.pathPoints[i]);
				}
			}
			AddPoint(new LNX_NavmeshHit(endVert));
		}

		public LNX_Path(LNX_Vertex startVert, LNX_Path basePath, LNX_Vertex endVert)
		{
			amStraight = basePath.AmStraight;
			totalDistance_cached = 0f;
			v_navmeshSurfaceProjection_cached = basePath.v_navmeshSurfaceProjection_cached;

			pathPoints = new List<LNX_NavmeshHit>();

			if( basePath == null || basePath.pathPoints.Count < 2 )
			{
				Debug.LogError($"LNX ERROR! You called a path constructor meant for a path with multiple path points, but supplied " +
					$"a path model with less than 2 path points. Returning early...");
				return;
			}

			AddPoint(new LNX_NavmeshHit(startVert));

			if( basePath.PointCount > 2 )
			{
				for (int i = 1; i < basePath.pathPoints.Count - 1; i++)
				{
					AddPoint(basePath.pathPoints[i]);
				}
			}
			
			AddPoint(new LNX_NavmeshHit(endVert));
		}

		public LNX_Path( LNX_Path basePathA, LNX_Path basePathB )
		{
			/*
			DBG_class = $"ctorC (2 base paths)\n" +
				$"LNX_Path(basePathA: '{basePathA}', basePathB: '{basePathB}')\n" +
				$"basePathA dbg: '{basePathA}'\n" +
				$"basePathB dbg: '{basePathB}'\n" +

				$"";
			*/

			amStraight = basePathA.AmStraight && basePathB.amStraight && basePathA.V_CrowFlies == basePathB.V_CrowFlies;
			totalDistance_cached = 0f;
			v_navmeshSurfaceProjection_cached = basePathA.v_navmeshSurfaceProjection_cached;

			pathPoints = new List<LNX_NavmeshHit>();

			//DBG_class += $"adding pathpoints from constructor paths...\n";

			if ( basePathA.pathPoints != null && basePathA.pathPoints.Count > 0 )
			{
				//DBG_class += $"basePathA points are valid with '{basePathA.PathPoints.Count}' pts...\n";
				for ( int i = 0; i < basePathA.pathPoints.Count; i++)
				{
					//DBG_class += $"for'{i}' ({basePathA.PathPoints[i]})...\n";
					AddPoint( basePathA.pathPoints[i] );
				}

				//DBG_class += $"finished adding basePathA's points. pt count: '{PointCount}'. dist: '{TotalDistance}'...\n";
			}

			if (basePathB.pathPoints != null && basePathB.pathPoints.Count > 0 )
			{
				//DBG_class += $"basePathB points are valid with '{basePathB.PathPoints.Count}' pts...\n";

				for (int i = 0; i < basePathB.pathPoints.Count; i++)
				{
					//DBG_class += $"for'{i}' ({basePathB.PathPoints[i]})...\n";

					if ( i == 0 && basePathA.pathPoints != null && basePathA.pathPoints.Count > 0 && 
						basePathB.StartHit.Position == basePathA.EndHit.Position)
					{
						//DBG_class += $"first pt of pathB is same as last logged point of A. continuing..\n";
						continue;
					}
					else
					{
						AddPoint(basePathB.pathPoints[i]);
					}
				}
				//DBG_class += $"finished adding basePathA's points. pt count: '{PointCount}'. dist: '{TotalDistance}'...\n";

			}
		}
		
		public LNX_Path( Vector3 nvmshProjectionDir, params LNX_NavmeshHit[] hits)
		{
			//DBG_class = $"ctorD\n";

			pathPoints = new List<LNX_NavmeshHit>();
			totalDistance_cached = 0f;
			amStraight = true;
			v_navmeshSurfaceProjection_cached = nvmshProjectionDir;

			if (hits == null || hits.Length <= 0)
			{
				return;
			}

			for ( int i = 0; i < hits.Length; i++ )
			{
				AddPoint( hits[i] );
			}
			/* //todo: this was just replaced by above. dws
			if( hits.Length > 1 )
			{
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
			*/
		}

		public LNX_Path(Vector3 nvmshProjectionDir, List<LNX_NavmeshHit> hits)
		{
			//DBG_class = $"ctorE\n";

			pathPoints = new List<LNX_NavmeshHit>();
			totalDistance_cached = 0f;
			amStraight = true;
			v_navmeshSurfaceProjection_cached = nvmshProjectionDir;

			if (hits == null || hits.Count <= 0)
			{
				return;
			}

			for (int i = 0; i < hits.Count; i++)
			{
				AddPoint( hits[i] );
			}
		}

		#endregion -------------------------------------------------

		#region MAIN API METHODS ============================================

		public void AddPoint( LNX_NavmeshHit pt )
		{
			//DBG_class += $"AddPoint('{pt}') bc: '{pathPoints.Count}', amStraight: '{amStraight}'\n"; //<<<<<<<<<<<<<<<<<<<<<<<<<
			if (pathPoints == null)
			{
				//DBG_class += $"collection null. initializing new...\n";
				pathPoints = new List<LNX_NavmeshHit>();
			}

			pathPoints.Add( pt );
			//DBG_class += $"aa: '{pathPoints.Count}', amStraight: '{amStraight}'\n"; //<<<<<<<<<<<<<<<<<<<<<<<<<

			if ( pathPoints.Count > 1 )
			{
				totalDistance_cached += Vector3.Distance( pathPoints[pathPoints.Count - 2].Position, pt.Position );

				if ( amStraight && PathPoints.Count > 2 ) //no need to check straightness if path count is 2 or less
				{
					Vector3 firstDir_fltnd = LNX_Utils.FlatVector(
						pathPoints[1].Position - pathPoints[0].Position, v_navmeshSurfaceProjection_cached
					).normalized; //todo: can get rid of these two variables, and just do the if statement with these expressions. Want to efficiency test doing this

					Vector3 dirNew = LNX_Utils.FlatVector(
						pt.Position - pathPoints[pathPoints.Count - 2].Position, v_navmeshSurfaceProjection_cached
					).normalized;//<<
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
			//DBG_class += $"ac: '{pathPoints.Count}', amStraight: '{amStraight}'\n";
		}

		public LNX_Path Reversed()
		{
			List<LNX_NavmeshHit> reversedHits = new List<LNX_NavmeshHit>();

			if( pathPoints.Count == 1 )
			{
				return new LNX_Path( v_navmeshSurfaceProjection_cached, pathPoints[0] );
			}

			for ( int i = pathPoints.Count-1; i > -1; i-- )
			{

				reversedHits.Add( pathPoints[i] );
			}

			return new LNX_Path( v_navmeshSurfaceProjection_cached, reversedHits );
		}

		public LNX_Path Reversed( LNX_Vertex endVertOverride )
		{
			List<LNX_NavmeshHit> reversedHits = new List<LNX_NavmeshHit>();

			if (pathPoints.Count == 1)
			{
				return new LNX_Path( v_navmeshSurfaceProjection_cached, new LNX_NavmeshHit(endVertOverride) );
			}

			for (int i = pathPoints.Count - 1; i > 0; i--)
			{
				reversedHits.Add(pathPoints[i]);
			}
			reversedHits.Add(new LNX_NavmeshHit(endVertOverride));

			return new LNX_Path(v_navmeshSurfaceProjection_cached, reversedHits);
		}

		public bool ValueEquals( LNX_Path otherPath )
		{

			if ((pathPoints == null && otherPath.pathPoints != null) ||
				(pathPoints != null && otherPath.pathPoints == null))
			{
				return false;
			}

			if (pathPoints != null && otherPath.pathPoints != null &&
				pathPoints.Count != otherPath.pathPoints.Count
			)
			{
				return false;
			}

			if (pathPoints != null && otherPath.pathPoints != null)
			{
				for (int i = 0; i < pathPoints.Count; i++)
				{
					if (otherPath.pathPoints[i] != pathPoints[i])
					{
						return false;
					}
				}
			}

			return true;
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

			if( pathPoints == null || pathPoints.Count == 0 )
			{
				Debug.LogError($"LNX ERROR! {nameof(GetVectorPointingToPreviousPoint)}() cannot calculate a previous point because the path points " +
					$"list is null or 0-count...");
				return Vector3.zero;
			}

			if ( ptIndx > pathPoints.Count - 1 )
			{
				Debug.LogError($"LNX ERROR! You passed {nameof(GetVectorPointingToPreviousPoint)}() an index of {ptIndx}, but the path point list " +
					$"only contains {pathPoints.Count} points...");
				return Vector3.zero;
			}

			return pathPoints[ptIndx-1].Position - pathPoints[ptIndx].Position;
		}

		public Vector3 GetVectorPointingToNextPoint( int ptIndx )
		{
			if (ptIndx < 0)
			{
				Debug.LogError($"LNX ERROR! You passed {nameof(GetVectorPointingToPreviousPoint)}() with an index of {ptIndx}. Cannot get a vector to " +
					$"a PathPoint that does not exit...");
				return Vector3.zero;
			}

			if (pathPoints == null || pathPoints.Count == 0)
			{
				Debug.LogError($"LNX ERROR! {nameof(GetVectorPointingToPreviousPoint)}() cannot calculate a previous point because the path points " +
					$"list is null or 0-count...");
				return Vector3.zero;
			}

			if ( ptIndx > pathPoints.Count - 2 )
			{
				Debug.LogError($"LNX ERROR! You passed {nameof(GetVectorPointingToPreviousPoint)}() an index of {ptIndx}, but the path point list " +
					$"only contains {pathPoints.Count} points. Can't get next point...");
				return Vector3.zero;
			}

			return pathPoints[ptIndx].Position - pathPoints[ptIndx - 1].Position;
		}

		public bool FoundIssue()
		{
			if ( pathPoints == null )
			{
				return true;
			}

			if( pathPoints.Count > 0 )
			{
				for( int i = 0; i < pathPoints.Count; i++ )
				{
					for ( int j = 0; j < pathPoints.Count; j++ )
					{
						if( j == i )
						{
							continue;
						}

						if(pathPoints[i].Position == pathPoints[j].Position )
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		public void DrawMyGizmos( float pointSize, float lblHeight, int highlightIndex = -1 )
		{
			if( pathPoints == null || pathPoints.Count <= 0 )
			{
				return;
			}

			Vector3 vRise = v_navmeshSurfaceProjection_cached * 0.5f * pointSize;
			for ( int i = 0; i < pathPoints.Count; i++ )
			{
				Color prevColor = Gizmos.color;

				if( highlightIndex > -1 && i == highlightIndex )
				{
					Gizmos.color = Color.yellow;
				}
				Gizmos.DrawSphere( pathPoints[i].Position, pointSize );

				Gizmos.DrawLine(
					pathPoints[i].Position, pathPoints[i].Position + (pathPoints[i].Normal * lblHeight)
				);

				Handles.Label(
					pathPoints[i].Position + (pathPoints[i].Normal * lblHeight * 1.01f), $"{i}"
				);
				

				if (i > 0)
				{
					Handles.DrawDottedLine(
						pathPoints[i - 1].Position + vRise, pathPoints[i].Position + vRise, 8f
					);
				}

				if (highlightIndex > -1 && i == highlightIndex)
				{
					Gizmos.color = prevColor;
				}
			}
		}

		#region OPERATORS ======================================================
		/*
		public static bool operator ==(LNX_Path a, LNX_Path b)
		{
			Debug.Log("==");
			return a.Equals(b);
		}

		public static bool operator !=(LNX_Path a, LNX_Path b)
		{
			return !a.Equals(b);
		}

		public override bool Equals(object obj)
		{
			Debug.Log($"equals");
			if (!(obj is LNX_Path))
				return false;

			LNX_Path otherPath = (LNX_Path)obj;
			
			//if(otherPath.totalDistance_cached != totalDistance_cached ||otherPath.amStraight != amStraight)
			//{
				//return false;
			//}
			

			if( (pathPoints == null && otherPath.pathPoints != null) || 
				(pathPoints != null && otherPath.pathPoints == null) )
			{
				return false;
			}

			if ( pathPoints != null && otherPath.pathPoints != null &&
				pathPoints.Count != otherPath.pathPoints.Count
			)
			{
				return false;
			}

			if ( pathPoints != null && otherPath.pathPoints != null )
			{
				for (int i = 0; i < pathPoints.Count; i++)
				{
					if (otherPath.pathPoints[i] != pathPoints[i])
					{
						return false;
					}
				}
			}

			return true;
		}

		public override int GetHashCode()
		{
		
			return HashCode.Combine(
				pathPoints, totalDistance_cached, v_navmeshSurfaceProjection_cached, amStraight
			);
		}
		

		public static LNX_Path operator +(LNX_Path p1,
									 LNX_Path p2)
		{
			Debug.Log("it's hapening!");
			return new LNX_Path( p1, p2 );
		}
		*/

		public override string ToString()
		{
			if( !AmValid )
			{
				if( pathPoints == null )
				{
					return $"[Invalid Path(null pts collection)]";
				}
				else
				{
					return $"[Invalid Path(0 length pts collection)]";
				}
			}

			return $"LNX_Path{StartHit}_->_{EndHit}";
		}

		#endregion ---------------------------------------

		#region HELPERS ======================================================
		public string GetFullDiagString()
		{
			string rtrnString = $"{this.ToString()}\n" +
				$"point count: '{PointCount}\n" +
				$"";

			if (PointCount > 0)
			{
				for (int i = 0; i < PointCount; i++)
				{
					bool foundDuplicate = false;

					for (int j = 0; j < PointCount; j++)
					{
						if( j == i )
						{
							continue;
						}

						if(pathPoints[j] == pathPoints[i] )
						{
							foundDuplicate = true;
						}
					}

					rtrnString += $"pt{i}: '{pathPoints[i]}' {(foundDuplicate ? "<<<<<<<<<FOUND DUPLICATE!!!" : "")}\n" +
						$"";
				}
			}

			return rtrnString;
		}

		public bool DoesPathHaveDuplicatePoint()
		{
			if ( PointCount <= 0 )
			{
				return false;
			}

			for (int i = 0; i < PointCount; i++)
			{
				for (int j = 0; j < PointCount; j++)
				{
					if (j == i)
					{
						continue;
					}

					if (pathPoints[j] == pathPoints[i])
					{
						return true;
					}
				}
			}
			
			return false;
		}
		#endregion

	}
}