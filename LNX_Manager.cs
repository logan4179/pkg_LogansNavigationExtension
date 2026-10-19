using JetBrains.Annotations;
using LogansNavigationExtension.AI;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace LogansNavigationExtension
{
	public class LNX_Manager : MonoBehaviour
	{
		public static LNX_Manager Instance;

		public List<LNX_NavMeshSurface> Surfaces;
		public List<LNX_NavMeshLink> Links;
		public List<LNX_Agent> Agents;

		[Header("DEBUG")]
		[SerializeField] private string errorMsg;

		private void Awake()
		{
			Instance = this;

			for (int i = 0; i < Surfaces.Count; i++)
			{

			}

			for (int i = 0; i < Agents.Count; i++)
			{
				Agents[i].SetManager(this);
			}
		}

		void Start()
		{
			for (int i = 0; i < Surfaces.Count; i++)
			{
				Surfaces[i].MySurfaceIndex = i;
			}

			for (int i = 0; i < Agents.Count; i++)
			{
				Agents[i].SetManager(this);
			}
		}

		#region GETTERS ==============================================
		public LNX_Edge GetEdge(LNX_ComponentCoordinate coord)
		{
			return Surfaces[coord.SurfaceIndex].Triangles[coord.TriangleIndex].Edges[coord.ComponentIndex];
		}
		#endregion

		#region SETTERS ======================================

		#endregion

		#region HELPERS ===============================================
		[ContextMenu("z call ResetSurfaceIndices()")]
		public void ResetSurfaceIndices()
		{
			Debug.Log($"ResetSurfaceIndices()");

			for (int i = 0; i < Surfaces.Count; i++)
			{
				Surfaces[i].SetSurfaceIndex(i);
			}

			Debug.Log($"todo: verify that links section works next time I do this");
			for (int i = 0; i < Links.Count; i++)
			{
				Links[i].RefreshSpans(); //todo: need to verify that this works
			}
		}
		[ContextMenu("z call ResetLinkIndices()")]
		public void ResetLinkIndices()
		{
			Debug.Log($"ResetLinkIndices()");

			for (int i = 0; i < Links.Count; i++)
			{
				Links[i].SetLinkIndex(i);
			}
		}
		[ContextMenu("z call RefreshLinks()")]
		public void RefreshLinks()
		{
			Debug.Log($"RefreshLinks()");

			for (int i = 0; i < Links.Count; i++)
			{
				Links[i].RefreshSpans(); //todo: need to verify that this works
			}
		}
		#endregion

		public LNX_NavmeshHit SampleClosestHit(Vector3 pos, float maxSampleDistance, bool considerClosesetOffPerimeter)
		{
			LNX_NavmeshHit returnHit = LNX_NavmeshHit.None;

			Surfaces[0].SamplePosition(pos, out returnHit, maxSampleDistance, considerClosesetOffPerimeter);

			return returnHit;
		}

		#region RAYCASTING ================================================================================
		public bool Raycast(LNX_NavmeshHit startHit, Vector3 projectDir, out LNX_Path outPath, float allowedDistance, bool allowRelationships = false)
		{
			return Surfaces[0].Raycast(startHit, projectDir, out outPath, allowedDistance, allowRelationships);
		}
		public bool Raycast_dbg(LNX_NavmeshHit startHit, Vector3 projectDir, out LNX_Path outPath, float allowedDistance,
			ref LNX_MethodDebugReport rprt, bool allowRelationships = false)
		{

			return Surfaces[0].Raycast_dbg(startHit, projectDir, out outPath, allowedDistance, ref rprt, allowRelationships);
		}
		public bool Raycast(LNX_NavmeshHit startHit, LNX_NavmeshHit endHit, out LNX_Path outPath)
		{
			return Surfaces[startHit.SurfaceIndex].Raycast(startHit, endHit, out outPath);
		}
		public bool Raycast_dbg(LNX_NavmeshHit startHit, LNX_NavmeshHit endHIt, out LNX_Path outPath, ref LNX_MethodDebugReport rprt)
		{
			return Surfaces[0].Raycast_dbg(startHit, endHIt, out outPath, ref rprt);
		}

		public bool Raycast(LNX_NavmeshHit startHit, Vector3 projectDir, LNX_NavmeshHit endHit, out LNX_Path outPath)
		{
			LNX_Path p = null;
			outPath = null;

			#region SHORT-CIRCUITING ============================
			if (projectDir == Vector3.zero)
			{
				Debug.LogError($"LNX ERROR! You tried to raycast with a projectDir of Vector3.zero. Returning early...");
				return false;
			}
			#endregion

			projectDir = Surfaces[startHit.SurfaceIndex].FlatVector(projectDir);

			LNX_Path runningPath = new LNX_Path();
			LNX_NavmeshHit runningStartHit = startHit;
			Vector3 runningProjectDir = projectDir;
			int safetyTimeout = 0;
			int safetyLimit = 10;
			for (int i = 0; i < Surfaces.Count; i++)
			{
				safetyLimit += Surfaces[i].Triangles.Length;
			}

			bool endReached = false;
			while (!endReached)
			{
				LNX_Path rcPath = null;

				if (!Surfaces[runningStartHit.SurfaceIndex].Raycast(runningStartHit, runningProjectDir, out rcPath, endHit))
				{
					runningPath.AddPath(rcPath);
					outPath = new LNX_Path(runningPath);
					endReached = true;
					return false;
				}
				else
				{
					runningPath.AddPath(rcPath);

					bool foundLink = false;
					for (int i = 0; i < Links.Count; i++)
					{
						if (!Links[i].TouchesSurface(rcPath.EndHit.SurfaceIndex))
						{
							continue;
						}

						if (Links[i].HitIsOnSpanA(rcPath.EndHit))
						{
							LNX_NavmeshHit spanBHit = LNX_NavmeshHit.None;
							if (!Links[i].ProjectFromSurfaceAToSpanB(runningStartHit, projectDir, out spanBHit))
							{
								endReached = true;
								return true;
							}
							else
							{
								runningStartHit = spanBHit;
								Quaternion qrot = Quaternion.FromToRotation
								(
									Links[i]._SurfaceA.GetSurfaceProjectionVector(),
									Links[i]._SurfaceB.GetSurfaceProjectionVector()
								);
								runningProjectDir = qrot * projectDir;
								foundLink = true;

								break;
							}
						}
						else if (Links[i].HitIsOnSpanB(rcPath.EndHit))
						{
							LNX_NavmeshHit spanAHit = LNX_NavmeshHit.None;
							if (!Links[i].ProjectFromSurfaceBToSpanA(runningStartHit, projectDir, out spanAHit))
							{
								endReached = true;
								return true;
							}
							else
							{
								runningStartHit = spanAHit;
								Quaternion qrot = Quaternion.FromToRotation
								(
									Links[i]._SurfaceB.GetSurfaceProjectionVector(),
									Links[i]._SurfaceA.GetSurfaceProjectionVector()
								);
								runningProjectDir = qrot * projectDir;
								foundLink = true;

								break;
							}
						}
					}

					if (!foundLink)
					{
						return true;
					}
				}

				safetyTimeout++;
				if (safetyTimeout > safetyLimit)
				{
					Debug.LogError($"safety timeout reached because while went more than '{safetyLimit}' iterations! Breaking early");
					return false;
				}
			}

			return true;
		}
		public bool Raycast_dbg(LNX_NavmeshHit startHit, Vector3 projectDir, LNX_NavmeshHit endHit, out LNX_Path outPath, ref LNX_MethodDebugReport rprt)
		{
			rprt.StartMethod($"Raycast_dbg(startHit: '{startHit}', projectDir: '{projectDir}', endHit: '{endHit}')");
			LNX_Path p = null;
			outPath = null;

			#region SHORT-CIRCUITING ============================
			if (projectDir == Vector3.zero)
			{
				Debug.LogError($"LNX ERROR! You tried to raycast with a projectDir of Vector3.zero. Returning early...");
				rprt.Log_And_End_Method($"LNX ERROR! You tried to raycast with a projectDir of Vector3.zero. Returning early...");
				return false;
			}
			#endregion

			projectDir = Surfaces[startHit.SurfaceIndex].FlatVector(projectDir);
			rprt.Log($"flattened projectDir to: '{projectDir}'...");

			LNX_Path runningPath = new LNX_Path();
			LNX_NavmeshHit runningStartHit = startHit;
			Vector3 runningProjectDir = projectDir;
			int safetyTimeout = 0;
			int safetyLimit = 10;
			for (int i = 0; i < Surfaces.Count; i++)
			{
				safetyLimit += Surfaces[i].Triangles.Length;
			}
			rprt.Log($"safetyLimit initialized to '{safetyLimit}'...",
				$"now while-looping to end...");

			bool endReached = false;
			while (!endReached)
			{
				rprt.Log($"while{safetyTimeout}============================================");
				LNX_Path rcPath = null;
				rprt.Log($"raycasting on surface{runningStartHit.SurfaceIndex} with dir: '{runningProjectDir}'...");

				if ( !Surfaces[runningStartHit.SurfaceIndex].Raycast(runningStartHit, runningProjectDir, out rcPath, endHit))
				{
					rprt.Log($"Raycast returned false, so it didn't hit anything. Assuming hit end. rcPath endhit: '{rcPath.EndHit}'...");

					runningPath.AddPath(rcPath);
					outPath = new LNX_Path(runningPath);
					endReached = true;
					rprt.Log_And_End_Method($"decided am on end. Returning false...");
					return false;
				}
				else
				{
					rprt.Log($"raycast hit: '{rcPath.EndHit}'. Inspecting hit to decide what's next...");
					runningPath.AddPath(rcPath);

					rprt.Log($"not yet at end. Checking '{Links.Count}' links to see if one was hit...");
					bool foundLink = false;
					for (int i = 0; i < Links.Count; i++)
					{
						rprt.Log($"for Link{i}...");
						if (!Links[i].TouchesSurface(rcPath.EndHit.SurfaceIndex))
						{
							rprt.Log($"this link doesn't touch the surface of the endhit so it can't be right. Continuing...");
							continue;
						}

						if (Links[i].HitIsOnSpanA(rcPath.EndHit))
						{
							rprt.Log($"hit is on spanA of this link. Now trying to project across link...");
							LNX_NavmeshHit spanBHit = LNX_NavmeshHit.None;
							if (!Links[i].ProjectFromSurfaceAToSpanB(runningStartHit, projectDir, out spanBHit))
							{
								rprt.Log_And_End_Method($"Could NOT project across link. Returning true...");
								endReached = true;
								return true;
							}
							else
							{
								rprt.Log($"Succesfully projected across link. Current runningStartHit: '{runningStartHit}', runningProjectDir: '{runningProjectDir}'",
									$"Generating next running values...");

								runningStartHit = spanBHit;
								Quaternion qrot = Quaternion.FromToRotation
								(
									Links[i]._SurfaceA.GetSurfaceProjectionVector(),
									Links[i]._SurfaceB.GetSurfaceProjectionVector()
								);
								runningProjectDir = qrot * projectDir;
								foundLink = true;

								rprt.Log($"runningStartHit: '{runningStartHit}'. runningProjectDir: '{runningProjectDir}'");
								break;
							}
						}
						else if (Links[i].HitIsOnSpanB(rcPath.EndHit))
						{
							rprt.Log($"hit is on spanB of this link. Now trying to project across link...");

							LNX_NavmeshHit spanAHit = LNX_NavmeshHit.None;
							if (!Links[i].ProjectFromSurfaceBToSpanA(runningStartHit, projectDir, out spanAHit))
							{
								rprt.Log_And_End_Method($"Could NOT project across link. Returning true...");
								endReached = true;
								return true;
							}
							else
							{
								rprt.Log($"Succesfully projected across link. Current runningStartHit: '{runningStartHit}', runningProjectDir: '{runningProjectDir}'",
									$"Generating next running values...");
								runningStartHit = spanAHit;
								Quaternion qrot = Quaternion.FromToRotation
								(
									Links[i]._SurfaceB.GetSurfaceProjectionVector(),
									Links[i]._SurfaceA.GetSurfaceProjectionVector()
								);
								runningProjectDir = qrot * projectDir;
								foundLink = true;

								rprt.Log($"runningStartHit: '{runningStartHit}'. runningProjectDir: '{runningProjectDir}'");
								break;
							}
						}
						else
						{
							rprt.Log($"Hit is on neither spans. Continuing...");

						}
					}

					if (!foundLink)
					{
						rprt.Log_And_End_Method($"exhausted all Link possibilities. Returning true...");
						return true;
					}
				}

				safetyTimeout++;
				if (safetyTimeout > safetyLimit)
				{
					Debug.LogError($"safety timeout reached because while went more than '{safetyLimit}' iterations! Breaking early");
					rprt.Log_And_End_Method($"safety timeout reached because while went more than '{safetyLimit}' iterations! Breaking early");
					return false;
				}
			}

			rprt.Log_And_End_Method($"Finally returning true...");
			return true;
		}
		#endregion

		#region PATHING ===================================================================================
		public bool CalculatePath( LNX_NavmeshHit startHit, LNX_NavmeshHit endHit, out LNX_Path path ) //OUTDATED! UPDATE TO DBG VERSION
		{
			path = null;

			#region INVESTIGATE DIRECT/STRAIGHT PATHS ======================================
			if
			(
				startHit.SurfaceIndex == endHit.SurfaceIndex &&
				Surfaces[startHit.SurfaceIndex].HitsAreOnSameIsland(startHit, endHit)
			)
			{
				if ( !Raycast(startHit, endHit, out path) )
				{
					return true;
				}
			}

			if ( Links.Count > 0 )
			{
				List<LNX_Path> furledPaths = CalculatePossibleSurfaceSequences( startHit, endHit );
				float bestDist = -1f;

				for ( int i = 0; i < furledPaths.Count; i++ )
				{
					Vector3 v = UnfurlPathToVector( furledPaths[i] );
					LNX_Path rcPath = null;
					if ( !Raycast(startHit, v, endHit, out rcPath) )
					{
						if
						(
							rcPath != null &&
							( rcPath.TotalDistance < bestDist || bestDist != -1f )
						)
						{
							path = new LNX_Path( rcPath );
							bestDist = rcPath.TotalDistance;
						}
					}
				}
			}

			if ( path != null )
			{
				return true;
			}
			#endregion

			#region PATHFIND ======================================================

			#endregion

			return false;
		}
		public bool CalculatePath_dbg(LNX_NavmeshHit startHit, LNX_NavmeshHit endHit, 
			out LNX_Path path, ref LNX_MethodDebugReport rprt)
		{
			rprt.StartMethod($"CalculatePath_dbg(startHit: '{startHit}', endHit: '{endHit}')");

			path = null;

			#region INVESTIGATE DIRECT/STRAIGHT PATHS ======================================
			rprt.Log($"invistigating straight path...");
			if
			(
				startHit.SurfaceIndex == endHit.SurfaceIndex &&
				Surfaces[startHit.SurfaceIndex].HitsAreOnSameIsland(startHit, endHit)
			)
			{
				rprt.Log($"start and end hits are on same island. Raycasting...");
				if ( !Raycast(startHit, endHit, out path) )
				{
					rprt.Log_And_End_Method($"Raycast false, meaning no obstruction. Returning true...");
					return true;
				}
				else
				{
					rprt.Log($"raycast was true so there was an obstruction...");
				}
			}

			if (Links.Count > 0)
			{
				rprt.Log($"investigating multi-surface raycasting over '{Links.Count}' links...");

				List<LNX_Path> furledPaths = CalculatePossibleSurfaceSequences_dbg(startHit, endHit, ref rprt);
				float bestDist = -1f;

				rprt.EmptyLine();

				if ( furledPaths != null )
				{
					rprt.Log($"found '{furledPaths.Count}' furled paths. Inspecting straight-line pathing from each of them...");

					for (int i = 0; i < furledPaths.Count; i++)
					{
						rprt.Log($"for{i}=====================");
						rprt.Log($"using fuledPaths[{i}]: '{furledPaths[i]}'...", 
							$"path has: '{furledPaths[i].PathPoints.Count}' points...");

						if (furledPaths[i].PathPoints.Count < 2 )
						{
							rprt.Log($"path point count too low to unfurl path. Assuming this corresponds to same-island surface sequence. Skipping...");
							continue;
						}

						rprt.Log("now unfurling path to vector...");
						Vector3 v = UnfurlPathToVector(furledPaths[i]);
						//Vector3 v = UnfurlPathToVector_dbg( furledPaths[i], ref rprt );

						rprt.Log($"got directional vector: '{v}'. Raycasting in this direction...");
						LNX_Path rcPath = null;


						if ( !Raycast(startHit, v, endHit, out rcPath) )
						{
							rprt.Log($"raycast false, so no obstruction...", 
								$"rcPath null: '{rcPath == null}'");
							if
							(
								rcPath != null &&
								(rcPath.TotalDistance < bestDist || bestDist == -1f)
							)
							{
								path = new LNX_Path(rcPath);
								bestDist = rcPath.TotalDistance;
								rprt.Log($"new bestDist: '{bestDist}', new best path: '{path}'...");
							}
						}
						else
						{
							rprt.Log($"raycast true. Hit obstruction...");
						}
					}
				}
				else
				{
					rprt.Log($"furledPaths list null. Couldn't find furled path");
				}
			}

			if (path != null)
			{
				rprt.Log_And_End_Method($"path found. Returning true...");
				return true;
			}
			#endregion

			#region PATHFIND ======================================================

			#endregion

			rprt.Log_And_End_Method($"finally returning false...");
			return false;
		}


		public bool CalculatePath( Vector3 startPt, Vector3 endPt, float maxSampleDistance, out LNX_Path path, 
            bool considerClosesetOffPerimeter)
        {
            LNX_NavmeshHit strtHt = SampleClosestHit(startPt, maxSampleDistance, considerClosesetOffPerimeter);
            LNX_NavmeshHit endHt = SampleClosestHit(endPt, maxSampleDistance, considerClosesetOffPerimeter);
			path = new LNX_Path();

            if( strtHt == LNX_NavmeshHit.None )
            {
                Debug.LogError($"LNX ERROR! Couldn't sample start position. Returning early");
                return false;
            }
			if ( endHt == LNX_NavmeshHit.None )
			{
				Debug.LogError($"LNX ERROR! Couldn't sample end position. Returning early");
				return false;
			}

            return CalculatePath( strtHt, endHt, out path );
		}

		public List<LNX_Path> CalculatePossibleSurfaceSequences( LNX_NavmeshHit startHit, LNX_NavmeshHit endHit )
		{
			List<LNX_Path> returnSequences = new List<LNX_Path>();
			if
			(
				startHit.SurfaceIndex == endHit.SurfaceIndex &&
				Surfaces[startHit.SurfaceIndex].HitsAreOnSameIsland(startHit, endHit)
			)
			{
				returnSequences.Add
				(
					new LNX_Path
					(
						Surfaces[startHit.SurfaceIndex].GetSurfaceProjectionVector(), startHit
					)
				);
			}

			List<LNX_Path> rollingSequences = new List<LNX_Path>()
			{
				new LNX_Path
				(
					Surfaces[startHit.SurfaceIndex].GetSurfaceProjectionVector(), startHit
				)
			};

			bool foundAll = false;
			int safetyTimeout = 0;

			while (!foundAll)
			{
				List<LNX_Path> newRollingSequences = new List<LNX_Path>();
				for (int i = 0; i < rollingSequences.Count; i++)
				{
					List<int> newLinks = GetIndicesOfLinksTouchingSurfaceIsland(rollingSequences[i]);

					for (int i_nwLnks = 0; i_nwLnks < newLinks.Count; i_nwLnks++)
					{
						LNX_Path cont = CreateFurlContinuation(rollingSequences[i], newLinks[i_nwLnks]);

						if
						(
							cont.EndHit.SurfaceIndex == endHit.SurfaceIndex &&
							Surfaces[endHit.SurfaceIndex].HitsAreOnSameIsland(cont.EndHit, endHit)
						)
						{
							cont.AddPoint(endHit);
							returnSequences.Add(new LNX_Path(cont));
						}
						else
						{
							newRollingSequences.Add(new LNX_Path(cont));
						}
					}
				}

				if (newRollingSequences.Count <= 0)
				{
					foundAll = true;
				}
				else
				{
					rollingSequences = new List<LNX_Path>();
					for (int i = 0; i < newRollingSequences.Count; i++)
					{
						rollingSequences.Add(newRollingSequences[i]);
					}
				}

				safetyTimeout++;
				if (safetyTimeout > Surfaces.Count)
				{
					Debug.LogError($"safety timeout reached at: '{safetyTimeout}'");
					return null;
				}
			}

			return returnSequences;
		}

		public List<LNX_Path> CalculatePossibleSurfaceSequences_dbg(LNX_NavmeshHit startHit, LNX_NavmeshHit endHit, ref LNX_MethodDebugReport rprt)
		{
			rprt.StartMethod($"CalculatePossibleSurfaceSequences_dbg('{startHit}', '{endHit}')");

			List<LNX_Path> returnSequences = new List<LNX_Path>();
			if
			(
				startHit.SurfaceIndex == endHit.SurfaceIndex &&
				Surfaces[startHit.SurfaceIndex].HitsAreOnSameIsland(startHit, endHit)
			)
			{
				rprt.Log($"start and end hit are on same island. Can shortcut a direct sequence...");
				returnSequences.Add
				(
					new LNX_Path
					(
						Surfaces[startHit.SurfaceIndex].GetSurfaceProjectionVector(), startHit
					)
				);
				rprt.Log($"added direct sequence: '{returnSequences[0]}' to return collection...");
			}

			List<LNX_Path> rollingSequences = new List<LNX_Path>()
			{
				new LNX_Path
				(
					Surfaces[startHit.SurfaceIndex].GetSurfaceProjectionVector(), startHit
				)
			};

			rprt.Log($"whiling...");
			rprt.EmptyLine();

			bool foundAll = false;
			int safetyTimeout = 0;

			while ( !foundAll )
			{
				rprt.Log($"while{safetyTimeout}===========================================");
				rprt.Log($"rolling sequences count: '{rollingSequences.Count}'...");
				List<LNX_Path> newRollingSequences = new List<LNX_Path>();
				for ( int i = 0; i < rollingSequences.Count; i++ )
				{
					rprt.Log($"for rolling sequence {i} ({rollingSequences[i]})...");

					rprt.Log($"getting links touching last surface of '{rollingSequences[i]}'...");
					List<int> newLinks = GetIndicesOfLinksTouchingSurfaceIsland( rollingSequences[i] ); //<<<<<<<<<<<<<<<<
					rprt.Log($"got '{newLinks.Count}' links touching last surface...");

					for ( int i_nwLnks = 0; i_nwLnks < newLinks.Count; i_nwLnks++ )
					{
						rprt.Log($"for newLink ({Links[newLinks[i_nwLnks]]})...");
						LNX_Path cont = CreateFurlContinuation(rollingSequences[i], newLinks[i_nwLnks]);
						//LNX_Path cont = Links[newLinks[i_nwLnks]].CreateSequenceContinuation_dbg(rollingSequences[i], ref rprt);

						rprt.Log($"created continuation path from this link that leads to surface: '{cont.EndHit.SurfaceIndex}'...");

						if
						(
							cont.EndHit.SurfaceIndex == endHit.SurfaceIndex &&
							Surfaces[endHit.SurfaceIndex].HitsAreOnSameIsland(cont.EndHit, endHit)
						)
						{
							cont.AddPoint( endHit );
							returnSequences.Add(new LNX_Path(cont));
							rprt.Log($"this new sequence ends on end surface/island. added sequence: " +
								$"'{returnSequences[returnSequences.Count - 1]}' to return collection");
						}
						else
						{
							rprt.Log($"adding this sequence to next rolling sequences list..");
							newRollingSequences.Add( new LNX_Path(cont) );
						}
					}
				}

				if ( newRollingSequences.Count <= 0 )
				{
					rprt.Log($"none of the new links resulted in new sequences. Ending while loop...");
					foundAll = true;
				}
				else
				{
					rprt.Log($"now setting up '{newRollingSequences.Count}' rolling sequences for next while...");
					rollingSequences = new List<LNX_Path>();
					for (int i = 0; i < newRollingSequences.Count; i++)
					{
						rollingSequences.Add(newRollingSequences[i]);
					}
				}

				rprt.EmptyLine();
				safetyTimeout++;
				if (safetyTimeout > Surfaces.Count)
				{
					Debug.LogError($"safety timeout reached at: '{safetyTimeout}'");
					rprt.Log_And_End_Method($"safety timeout reached at: '{safetyTimeout}'");
					return null;
				}
			}

			rprt.Log_And_End_Method($"finally returning '{returnSequences.Count}' sequences...");

			return returnSequences;
		}

		private LNX_Path CreateFurlContinuation(LNX_Path basePath, int linkIndx)
		{
			#region SHORT-CIRCUITING =============================
			if
			(
				basePath.EndHit.SurfaceIndex != Links[linkIndx]._SurfaceA.MySurfaceIndex &&
				basePath.EndHit.SurfaceIndex != Links[linkIndx]._SurfaceB.MySurfaceIndex
			)
			{
				return null;
			}
			if ( basePath == null || basePath.PointCount < 1 )
			{
				Debug.Log($"LNX ERROR! You tried to create a furl continuation on a path with no points. Returning null...");
				return null;
			}
			#endregion

			LNX_Path rtrnPath = new LNX_Path(basePath);
			if (basePath.EndHit.SurfaceIndex == Links[linkIndx]._SurfaceA.MySurfaceIndex)
			{
				rtrnPath.AddPoint
				(
					LNX_Utils.FlattenHitToHit(Links[linkIndx].SpanBStartHit, basePath.EndHit, Links[linkIndx]._SurfaceA.SurfaceOrientation)
				);
			}
			else if (basePath.EndHit.SurfaceIndex == Links[linkIndx]._SurfaceB.MySurfaceIndex)
			{
				rtrnPath.AddPoint
				(
					LNX_Utils.FlattenHitToHit(Links[linkIndx].SpanAStartHit, basePath.EndHit, Links[linkIndx]._SurfaceB.SurfaceOrientation)
				);
			}

			return rtrnPath;
		}

		#endregion

		public List<int> GetIndicesOfLinksTouchingSurfaceIsland(LNX_NavmeshHit hit, List<int> avoidLinks)
		{
			List<int> returnCollection = new List<int>();

			for (int i = 0; i < Links.Count; i++)
			{
				if (avoidLinks.Contains(i))
				{
					continue;
				}

				if (Links[i].TouchesSurfaceIsland(hit))
				{
					returnCollection.Add(i);
				}
			}

			return returnCollection;
		}
		public List<int> GetIndicesOfLinksTouchingSurfaceIsland_dbg(LNX_NavmeshHit hit, List<int> avoidLinks, ref LNX_MethodDebugReport rprt)
		{
			rprt.StartMethod($"GetIndicesOfLinksTouchingSurfaceIsland_dbg(hit: '{hit}', avoid: '{avoidLinks.Count}')");
			List<int> returnCollection = new List<int>();

			for (int i = 0; i < Links.Count; i++)
			{
				rprt.Log($"checking link{i} ({Links[i].name})...");
				if (avoidLinks.Contains(i))
				{
					rprt.Log($"avoiding this link...");
					continue;
				}

				if (Links[i].TouchesSurfaceIsland_dbg(hit, ref rprt))
				{
					rprt.Log($"this link touches surface island of hit. Adding to returnCollection...");
					returnCollection.Add(i);
				}
				else
				{
					rprt.Log("link does NOT touch surface island...");
				}
			}

			rprt.Log_And_End_Method($"finally, returning '{returnCollection.Count}' indices");
			return returnCollection;
		}

		public List<int> GetIndicesOfLinksTouchingSurfaceIsland( LNX_NavmeshHit surfaceHit )
		{
			List<int> returnCollection = new List<int>();

			for (int i = 0; i < Links.Count; i++)
			{
				if (Links[i].TouchesSurfaceIsland(surfaceHit))
				{
					returnCollection.Add(i);
				}
			}

			return returnCollection;
		}
		public List<int> GetIndicesOfLinksTouchingSurfaceIsland( LNX_Path backstop )
		{
			List<int> returnCollection = new List<int>();

			for (int i = 0; i < Links.Count; i++)
			{
				if ( PathTouchesLink(backstop, i) )
				{
					continue;
				}

				if (Links[i].TouchesSurfaceIsland(backstop.EndHit))
				{
					returnCollection.Add(i);
				}
			}

			return returnCollection;
		}
		public List<int> GetIndicesOfLinksTouchingSurfaceIsland_dbg(LNX_Path backstop, ref LNX_MethodDebugReport rprt)
		{
			rprt.StartMethod($"GetIndicesOfLinksTouchingSurfaceIsland_dbg(backstop: '{backstop}')");
			List<int> returnCollection = new List<int>();

			rprt.Log($"checking all links...");
			for (int i = 0; i < Links.Count; i++)
			{
				rprt.Log($"for{i}==============================", 
					$"Checking link {Links[i]}...");

				if (PathTouchesLink(backstop, i))
				{
					rprt.Log($"PathTouchesLink returned true. Skipping...");
					continue;
				}
				else
				{
					rprt.Log($"PathTouchesLink() returned false");
				}

				if (Links[i].TouchesSurfaceIsland_dbg(backstop.EndHit, ref rprt))
				{
					rprt.Log($"This link touches the endhit surface island. Adding to return collection...");
					returnCollection.Add(i);
				}
				else
				{
					rprt.Log($"TouchesSurfaceIsland_dbg() returned false");
				}
			}

			rprt.Log_And_End_Method($"finally returning: '{returnCollection.Count}' links...");
			return returnCollection;
		}

		#region PATH METHODS =====================================================
		public bool PathTouchesLink(LNX_Path path, int linkIndx)
		{
			for ( int i = 0; i < path.PathPoints.Count; i++ )
			{
				if (path.PathPoints[i].LinkIndex == linkIndx)
				{
					return true;
				}
			}

			return false;
		}
		public Vector3 UnfurlPathToVector( LNX_Path furledPath )
		{
			#region SHORT-CIRCUITING ====================================
			if ( furledPath.PointCount < 2 )
			{
				Debug.LogError($"LNX ERROR! You called UnfurlPathToVector(), passing in a path with a point count less than 2. Returning early...");
				return Vector3.zero;
			}
			#endregion

			Vector3 v = furledPath.StartPosition;

			Quaternion runningRot = Quaternion.identity;

			for (int i = 0; i < furledPath.PointCount - 1; i++)
			{
				Vector3 vto = LNX_Utils.FlattenVectorToVector(
					furledPath.PathPoints[i + 1].Position - furledPath.PathPoints[i].Position,
					furledPath.PathPoints[i].Position,
					Surfaces[furledPath.PathPoints[i].SurfaceIndex].SurfaceOrientation
				);

				if
				(
					i > 0 && //because the first two points should NOT need to be rotated
					Surfaces[furledPath.PathPoints[i].SurfaceIndex].SurfaceOrientation !=
					Surfaces[furledPath.PathPoints[i - 1].SurfaceIndex].SurfaceOrientation
				)
				{
					Quaternion qrot = Quaternion.FromToRotation
					(
						Surfaces[furledPath.PathPoints[i].SurfaceIndex].GetSurfaceProjectionVector(),
						Surfaces[furledPath.PathPoints[i - 1].SurfaceIndex].GetSurfaceProjectionVector()
					);

					runningRot = runningRot * qrot;
					v += runningRot * vto;

				}
				else
				{
					v += vto;
				}
			}
			
			return LNX_Utils.FlattenVectorToVector(
				v - furledPath.StartPosition, furledPath.StartPosition, Surfaces[furledPath.StartHit.SurfaceIndex].SurfaceOrientation
			);
		}
		public Vector3 UnfurlPathToVector_dbg(LNX_Path furledPath, ref LNX_MethodDebugReport rprt )
		{
			rprt.StartMethod($"UnfurlPathToVector_dbg('{furledPath}')");

			#region SHORT-CIRCUITING ====================================
			if (furledPath.PointCount < 2)
			{
				Debug.LogError($"LNX ERROR! You called UnfurlPathToVector(), passing in a path with a point count less than 2. Returning early...");
				rprt.Log_And_End_Method($"LNX ERROR! You called UnfurlPathToVector(), passing in a path with a point count less than 2. Returning early...");
				return Vector3.zero;
			}
			#endregion

			Vector3 v = furledPath.StartPosition;

			rprt.Log($"return vector initialized to: '{v}'...");

			if (furledPath.PointCount > 2)
			{
				rprt.Log($"iterating through '{furledPath.PointCount}' pts...");

				Quaternion runningRot = Quaternion.identity;

				for ( int i = 0; i < furledPath.PointCount-1; i++ )
				{
					rprt.Log($"for({i})...");
					rprt.Log($"furledPath.PathPoints[{i}]: '{furledPath.PathPoints[i]}', " +
						$"spv: '{Surfaces[furledPath.PathPoints[i].SurfaceIndex].GetSurfaceProjectionVector()}'");
					rprt.Log($"furledPath.PathPoints[{i}+1]: '{furledPath.PathPoints[i + 1]}', " +
						$"spv: '{Surfaces[furledPath.PathPoints[i + 1].SurfaceIndex].GetSurfaceProjectionVector()}'");

					Vector3 vto = LNX_Utils.FlattenVectorToVector(
						furledPath.PathPoints[i + 1].Position - furledPath.PathPoints[i].Position,
						furledPath.PathPoints[i].Position,
						Surfaces[furledPath.PathPoints[i].SurfaceIndex].SurfaceOrientation
					);
					rprt.Log($"using vto: '{vto}'...");

					if 
					( 
						i > 0 && //because the first two points should NOT need to be rotated
						Surfaces[furledPath.PathPoints[i].SurfaceIndex].SurfaceOrientation != 
						Surfaces[furledPath.PathPoints[i-1].SurfaceIndex].SurfaceOrientation
					)
					{
						rprt.Log($"updating rotation...");

						Quaternion qrot = Quaternion.FromToRotation
						(
							Surfaces[furledPath.PathPoints[i].SurfaceIndex].GetSurfaceProjectionVector(),
							Surfaces[furledPath.PathPoints[i-1].SurfaceIndex].GetSurfaceProjectionVector()
						);

						runningRot = runningRot * qrot;
						rprt.Log($"running rot now: '{runningRot.eulerAngles}'");

						rprt.Log($"adding '{runningRot * vto}' to '{v}'...");
						v += runningRot * vto;
						Debug.DrawRay(v, Vector3.up * 5f, Color.magenta, 4f);

					}
					else
					{
						rprt.Log($"rotation NOT necessary. Adding '{vto}' to '{v}'");
						v += vto;
						Debug.DrawRay(v, Vector3.up * 5f, Color.magenta, 4f);
					}

					rprt.Log($"v now: '{v}'...");
				}
			}

			rprt.Log_And_End_Method($"finally returning '{Surfaces[furledPath.StartHit.SurfaceIndex].FlatVector(v)}'...");
			
			return LNX_Utils.FlattenVectorToVector(
				v - furledPath.StartPosition, furledPath.StartPosition, Surfaces[furledPath.StartHit.SurfaceIndex].SurfaceOrientation
			); //This returns a "relative" vector
			

			/*
			return LNX_Utils.FlattenVectorToVector(
				v, furledPath.StartPosition, Surfaces[furledPath.StartHit.SurfaceIndex].SurfaceOrientation
			); //for "absolute" vector. I don't think this makes sense because I think unfurling a path only makes sense in the context of a relative vector
			*/
		}
		/*
		public bool VectorClearsLinks( LNX_NavmeshHit originHit, Vector3 vDir, LNX_Path path )
		{
			#region SHORT-CIRCUITING ================================
			if ( vDir == Vector3.zero )
			{
				return false;
			}
			#endregion

			Quaternion runningOrientationRot = Quaternion.identity;
			Vector3 runningUnfurlPoint = path.PathPoints[0].Position;

			for ( int i = 0; i < path.PathPoints.Count - 1; i++ )
			{
				Vector3 vToNext = Surfaces[i].FlatVector(path.PathPoints[i+1].Position - path.PathPoints[i].Position);

				if
				(
					i > 0 && 
					Surfaces[path.PathPoints[i].SurfaceIndex].SurfaceOrientation !=
					Surfaces[path.PathPoints[i+1].SurfaceIndex].SurfaceOrientation
				)
				{
					runningOrientationRot *= Quaternion.FromToRotation
					(
						Surfaces[path.PathPoints[i].SurfaceIndex].GetSurfaceProjectionVector(),
						Surfaces[path.PathPoints[i + 1].SurfaceIndex].GetSurfaceProjectionVector()
					);
				}

				runningUnfurlPoint += runningOrientationRot * vToNext;

				if ( path.PathPoints[i + 1].LinkIndex > -1 )
				{
					Vector3 v_toStartA = Surfaces[path.PathPoints[i+1].SurfaceIndex].FlatVector()
					LNX_Utils.AmInVectorCone(v)
				}
			}
		}
		*/

		public float CalculateFlatPathDistance(LNX_Path p )
		{
			float dist = 0f;
			if ( p.PathPoints.Count > 1 )
			{
				for ( int i = 0; i < p.PathPoints.Count-1; i++ )
				{

				}
			}

			return dist;
		}
		#endregion

		[ContextMenu("z call ReconstructVisualizationMeshes()")]
		public void ReconstructVisualizationMeshes()
		{
			for ( int i = 0; i < Surfaces.Count; i++ )
			{
				Surfaces[i].ReconstructVisualizationMesh();
			}

			for ( int i = 0; i < Links.Count; i++ )
			{
				Links[i].ReconstructVisualizationMesh();
			}
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			for( int i = 0; i < Links.Count; i++ )
			{
				Links[i].DrawMyGizmos();
			}

			if ( Selection.activeGameObject != gameObject )
			{
				return;
			}

			errorMsg = string.Empty;
			for( int i = 0; i < Surfaces.Count; i++ )
			{
				if (Surfaces[i].MySurfaceIndex != i )
				{
					if ( string.IsNullOrEmpty(errorMsg) )
					{
						errorMsg = "ERROR!\n";
					}

					errorMsg += $"{Surfaces[i].gameObject.name} surface index not set correctly\n";
				}

				if (Surfaces[i].StateFlag == -1 )
				{
					Debug.Log($"Surface{i} state flag says transitioning. Refreshing spans for any links touching this surface...");
					for ( int i_lnks = 0; i_lnks < Links.Count; i_lnks++ )
					{
						if (Links[i_lnks].TouchesSurface(Surfaces[i]) )
						{
							Links[i_lnks].RefreshSpans();
						}
					}
				}
			}

			for (int i = 0; i < Links.Count; i++)
			{
				if (Links[i].MyLinkIndex != i)
				{
					if (string.IsNullOrEmpty(errorMsg))
					{
						errorMsg = "ERROR!\n";
					}

					errorMsg += $"{Links[i].gameObject.name} link index not set correctly\n";
				}
			}
		}
#endif
	}
}
