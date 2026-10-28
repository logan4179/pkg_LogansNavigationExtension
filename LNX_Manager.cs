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
		public bool Raycast(LNX_NavmeshHit startHit, LNX_NavmeshHit endHit, out LNX_Path outPath, bool crossOverLinks)
		{
			outPath = new LNX_Path(startHit);

			#region SHORT-CIRCUITING ===============================================
			if (startHit.SurfaceIndex != endHit.SurfaceIndex)
			{
				Debug.LogError($"LNX ERROR! Calling this Raycast overload on the manager is only meant for " +
					$"a start and end hit that are on the same surface. The supplied hits ('{startHit}', and '{endHit}')" +
					$"have different surface indices. This means that the method can't resolve a directional vector." +
					$"Use the overload that allows you to supply a projection direction if you want to do this. " +
					$"Returning false early...");
				return false;
			}
			#endregion

			if (!crossOverLinks)
			{
				return Surfaces[startHit.SurfaceIndex].Raycast(startHit, endHit, out outPath);
			}

			LNX_NavmeshHit runningStartHit = startHit;
			Vector3 projectDir = Surfaces[startHit.SurfaceIndex].FlatVector(endHit.Position - startHit.Position);
			Vector3 runningProjectDir = projectDir;
			int safetyTimeout = 0;

			bool amFinished = false;
			while (!amFinished)
			{
				LNX_Path rcPath = null;
				if (!Surfaces[runningStartHit.SurfaceIndex].Raycast(runningStartHit, runningProjectDir, out rcPath, endHit))
				{
					outPath.AddPath(rcPath);
					amFinished = true;
					return false;
				}
				else
				{
					outPath.AddPath(rcPath);

					LNX_ComponentCoordinate linkHitInfo = GetLinkInfoAtHit(rcPath.EndHit); //Note: the componentcoordinate that it returns is used differently than normal...
																						   //LNX_ComponentCoordinate linkHitInfo = GetLinkInfoAtHit_dbg(rcPath.EndHit, ref rprt);
					if (linkHitInfo.TriangleIndex == -1) //Note: TriangleIndex is actually being used as the index of the link that was hit
					{
						amFinished = true;
						return true;
					}

					if (linkHitInfo.ComponentIndex == 1) //hit is on spanA...
					{
						LNX_NavmeshHit spanBHit = LNX_NavmeshHit.None;
						if (!Links[linkHitInfo.TriangleIndex].ProjectOntoSpanB(runningStartHit, runningProjectDir, out spanBHit))
						{
							amFinished = true;
							return true;
						}
						else
						{
							outPath.AddPoint(spanBHit);

							runningStartHit = spanBHit;
							Quaternion qrot = LNX_Utils.GetRotationBetweenDirections(
								Links[linkHitInfo.TriangleIndex]._SurfaceA.SurfaceOrientation,
								Links[linkHitInfo.TriangleIndex]._SurfaceB.SurfaceOrientation
							);
							//runningProjectDir = qrot * projectDir;
							runningProjectDir = qrot * runningProjectDir;
						}
					}
					else if (linkHitInfo.ComponentIndex == 2) //hit is on spanB...
					{
						LNX_NavmeshHit spanAHit = LNX_NavmeshHit.None;
						if (!Links[linkHitInfo.TriangleIndex].ProjectOntoSpanA(runningStartHit, runningProjectDir, out spanAHit))
						{
							amFinished = true;
							return true;
						}
						else
						{
							outPath.AddPoint(spanAHit);

							runningStartHit = spanAHit;
							Quaternion qrot = LNX_Utils.GetRotationBetweenDirections(
								Links[linkHitInfo.TriangleIndex]._SurfaceB.SurfaceOrientation,
								Links[linkHitInfo.TriangleIndex]._SurfaceA.SurfaceOrientation
							);
							//runningProjectDir = qrot * projectDir;
							runningProjectDir = qrot * runningProjectDir;
						}
					}
					else
					{
						return true;
					}
				}

				safetyTimeout++;
				if (safetyTimeout > Surfaces.Count + 1)
				{
					Debug.LogError($"safety timeout reached because while went more than '{Surfaces.Count + 1}' iterations! Breaking early");
					return false;
				}
			}

			return false;
		}
		public bool Raycast_dbg(LNX_NavmeshHit startHit, LNX_NavmeshHit endHit, out LNX_Path outPath, 
			bool crossOverLinks, ref LNX_MethodDebugReport rprt)
		{
			rprt.StartMethod($"Raycast_dbg(startHit: '{startHit}', endHit: '{endHit}', crossOverLinks: '{crossOverLinks}')");

			outPath = new LNX_Path(startHit);

			#region SHORT-CIRCUITING ===============================================
			if (startHit.SurfaceIndex != endHit.SurfaceIndex)
			{
				Debug.LogError($"LNX ERROR! Calling this Raycast overload on the manager is only meant for " +
					$"a start and end hit that are on the same surface. The supplied hits ('{startHit}', and '{endHit}')" +
					$"have different surface indices. This means that the method can't resolve a directional vector." +
					$"Use the overload that allows you to supply a projection direction if you want to do this. " +
					$"Returning false early...");

				rprt.Log_And_End_Method($"LNX ERROR! Calling this Raycast overload on the manager is only meant for " +
					$"a start and end hit that are on the same surface. The supplied hits ('{startHit}', and '{endHit}')" +
					$"have different surface indices. This means that the method can't resolve a directional vector." +
					$"Use the overload that allows you to supply a projection direction if you want to do this. " +
					$"Returning false early...");
				return false;
			}
			#endregion

			if (!crossOverLinks)
			{
				rprt.Log_And_End_Method($"NOT crossing over links. Calling surface raycast once and ending method...");
				return Surfaces[startHit.SurfaceIndex].Raycast(startHit, endHit, out outPath);
			}

			LNX_NavmeshHit runningStartHit = startHit;
			Vector3 projectDir = Surfaces[startHit.SurfaceIndex].FlatVector(endHit.Position - startHit.Position);
			Vector3 runningProjectDir = projectDir;
			int safetyTimeout = 0;

			rprt.Log($"crossing over links. While-looping until end...");
			bool amFinished = false;
			while (!amFinished)
			{
				rprt.Log($"while{safetyTimeout}==========================////////////////////////");
				LNX_Path rcPath = null;
				rprt.Log($"raycasting using runningStartHit: '{runningStartHit}, dir: '{runningProjectDir}'...");
				if ( !Surfaces[runningStartHit.SurfaceIndex].Raycast(runningStartHit, runningProjectDir, out rcPath, endHit) )
				{
					rprt.Log($"raycast returned false, meaning no obstruction was hit, and end SHOULD have been reached.",
						$"got path: '{rcPath}'. Adding this path to existing path...");
					outPath.AddPath( rcPath );
					amFinished = true;
					rprt.Log_And_End_Method($"new path has '{outPath.PathPoints}' points. Now returning false...");
					return false;
				}
				else
				{
					rprt.Log($"raycast returned true, meaning it DID hit an obstruction. Now appending path to outPath...");
					outPath.AddPath(rcPath);
					rprt.Log($"new path has '{outPath.PathPoints.Count}' points...");

					rprt.Log($"Now investigating if raycast hit a link span...");

					LNX_ComponentCoordinate linkHitInfo = GetLinkInfoAtHit(rcPath.EndHit); //Note: the componentcoordinate that it returns is used differently than normal...
					//LNX_ComponentCoordinate linkHitInfo = GetLinkInfoAtHit_dbg(rcPath.EndHit, ref rprt);

					rprt.Log($"got linkHitInfo: '{linkHitInfo}'...");

					if (linkHitInfo.TriangleIndex == -1) //Note: TriangleIndex is actually being used as the index of the link that was hit
					{
						rprt.Log($"linkIndex of -1 means it should have hit a true termination and stop now. Returning true...");
						amFinished = true;
						return true;
					}
					else
					{
						rprt.Log($"raycast DID indeed hit a link span...");
					}

					rprt.Log($"link goes from surfaceA: '{Links[linkHitInfo.TriangleIndex]._SurfaceA}' " +
						$"to surfaceB: '{Links[linkHitInfo.TriangleIndex]._SurfaceB}'..");

					if ( linkHitInfo.ComponentIndex == 1 ) //hit is on spanA...
					{
						rprt.Log($"found that hit is on SpanA of link '{linkHitInfo.TriangleIndex}'. Trying to project to spanB...");

						LNX_NavmeshHit spanBHit = LNX_NavmeshHit.None;
						//if (!Links[linkHitInfo.TriangleIndex].ProjectFromSurfaceAToSpanB(runningStartHit, runningProjectDir, out spanBHit))
						if (!Links[linkHitInfo.TriangleIndex].ProjectOntoSpanB_dbg(runningStartHit, runningProjectDir, out spanBHit, ref rprt))
						{
							rprt.Log_And_End_Method($"could NOT project to spanB. Assuming true termination hit and returning true...");
							amFinished = true;
							return true;
						}
						else
						{
							rprt.Log($"Projected succesfully to spanB at hit: '{spanBHit}'. Adding hit to outPath...");
							outPath.AddPoint( spanBHit );

							rprt.Log($"now re-orienting projection direction...");
							runningStartHit = spanBHit;
							Quaternion qrot = LNX_Utils.GetRotationBetweenDirections(
								Links[linkHitInfo.TriangleIndex]._SurfaceA.SurfaceOrientation,
								Links[linkHitInfo.TriangleIndex]._SurfaceB.SurfaceOrientation
							);
							//runningProjectDir = qrot * projectDir;
							runningProjectDir = qrot * runningProjectDir;

							rprt.Log($"Used rotation: '{qrot}' to create new runningProjectDir: '{runningProjectDir}'...");
						}
					}
					else if ( linkHitInfo.ComponentIndex == 2 ) //hit is on spanB...
					{
						rprt.Log($"found that hit is on SpanB of link '{linkHitInfo.ComponentIndex}'. Trying to project to spanA...");

						LNX_NavmeshHit spanAHit = LNX_NavmeshHit.None;
						if (!Links[linkHitInfo.TriangleIndex].ProjectOntoSpanA(runningStartHit, runningProjectDir, out spanAHit))
						{
							rprt.Log_And_End_Method($"could NOT project to spanA. Assuming true termination hit and returning true...");

							amFinished = true;
							return true;
						}
						else
						{
							rprt.Log($"Projected succesfully to spanA at hit: '{spanAHit}'. Adding hit to outPath...");
							outPath.AddPoint(spanAHit);

							rprt.Log($"now re-orienting projection direction...");
							runningStartHit = spanAHit;
							Quaternion qrot = LNX_Utils.GetRotationBetweenDirections(
								Links[linkHitInfo.TriangleIndex]._SurfaceB.SurfaceOrientation,
								Links[linkHitInfo.TriangleIndex]._SurfaceA.SurfaceOrientation
							);
							//runningProjectDir = qrot * projectDir;
							runningProjectDir = qrot * runningProjectDir; 
							rprt.Log($"Used rotation: '{qrot}' to create new runningProjectDir: '{runningProjectDir}'...");
						}
					}
					else
					{
						rprt.Log_And_End_Method($"Apparently hit is on neither span. Assuming terminal hit and returning true...");
						return true;
					}
				}

				safetyTimeout++;
				if (safetyTimeout > Surfaces.Count + 1)
				{
					Debug.LogError($"safety timeout reached because while went more than '{Surfaces.Count + 1}' iterations! Breaking early");
					rprt.Log_And_End_Method($"safety timeout reached because while went more than '{Surfaces.Count + 1}' iterations! Breaking early");
					return false;
				}
			}

			rprt.Log_And_End_Method($"Finally returning false...");
			return false;

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
							if (!Links[i].ProjectOntoSpanB(runningStartHit, projectDir, out spanBHit))
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
							if (!Links[i].ProjectOntoSpanA(runningStartHit, projectDir, out spanAHit))
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
							if (!Links[i].ProjectOntoSpanB(runningStartHit, projectDir, out spanBHit))
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
							if (!Links[i].ProjectOntoSpanA(runningStartHit, projectDir, out spanAHit))
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
		public bool CalculatePath( LNX_NavmeshHit startHit, LNX_NavmeshHit endHit, out LNX_Path path )
		{
			path = null;

			#region INVESTIGATE DIRECT/STRAIGHT PATHS ======================================
			if
			(
				startHit.SurfaceIndex == endHit.SurfaceIndex &&
				Surfaces[startHit.SurfaceIndex].HitsAreOnSameIsland(startHit, endHit)
			)
			{
				if (!Raycast(startHit, endHit, out path, true))
				{
					return true;
				}
			}

			if (Links.Count > 0)
			{
				List<LNX_Path> furledPaths = CalculatePossibleSurfaceSequences(startHit, endHit);
				float bestDist = -1f;

				if (furledPaths != null)
				{
					for (int i = 0; i < furledPaths.Count; i++)
					{
						if (furledPaths[i].PathPoints.Count < 2)
						{
							continue;
						}

						//Vector3 v = UnfurlPathToVector(furledPaths[i]); //todo: dws
						//Vector3 v = UnfurlPathToVector_dbg( furledPaths[i], ref rprt ); //todo: dws
						Vector3 v = furledPaths[i].UnfurledToDirectionalVector();

						LNX_Path rcPath = null;

						if (!Raycast(startHit, v, endHit, out rcPath))
						{
							if
							(
								rcPath != null &&
								(rcPath.TotalDistance < bestDist || bestDist == -1f)
							)
							{
								path = new LNX_Path(rcPath);
								bestDist = rcPath.TotalDistance;
							}
						}
					}
				}
			}

			if (path != null)
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
			bool hitsAreOnSameIsle = false;

			#region INVESTIGATE DIRECT/STRAIGHT PATHS ======================================
			rprt.Log($"investigating straight path...");
			if
			(
				startHit.SurfaceIndex == endHit.SurfaceIndex &&
				Surfaces[startHit.SurfaceIndex].HitsAreOnSameIsland(startHit, endHit)
			)
			{
				rprt.Log($"start and end hits are on same island. Raycasting...");
				hitsAreOnSameIsle = true;
				if ( !Raycast(startHit, endHit, out path, true) )
				{
					rprt.Log_And_End_Method($"Raycast false, meaning no obstruction. Returning true...");
					return true;
				}
				else
				{
					rprt.Log($"raycast was true so there was an obstruction...");
				}
			}
			path = null; //because it was just 'dirtied' in the raycast above

			if (Links.Count > 0)
			{
				rprt.Log($"investigating multi-surface raycasting against '{Links.Count}' links...",
					$"Calculating possible sequences...");

				//List<LNX_Path> furledPaths = CalculatePossibleSurfaceSequences_dbg(startHit, endHit, ref rprt, false);
				List<LNX_Path> furledPaths = CalculatePossibleSurfaceSequences(startHit, endHit, false);

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
						//Vector3 v = UnfurlPathToVector(furledPaths[i]); //todo: dws
						//Vector3 v = UnfurlPathToVector_dbg( furledPaths[i], ref rprt ); //todo: dws
						Vector3 v = furledPaths[i].UnfurledToDirectionalVector();

						rprt.Log($"got directional vector: '{v}'. Checking if vector will clear furled path sequence...");

						if ( !VectorClearsLinksInPath(startHit, v, furledPaths[i]) )
						{
							rprt.Log($"This vector WILL NOT clear the furled path. Continuing to inspect next path...");
							continue;
						}

						LNX_Path rcPath = null;

						rprt.Log($"This vector WILL clear the furled path. Continuing to raycast down path...");
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
				rprt.Log_And_End_Method($"path found with '{path.PointCount}' points. Returning true...");
				return true;
			}
			#endregion

			#region PATHFIND ======================================================
			rprt.Log($"Now path-finding...");
			LNX_Path bestPathFindPath = null;
			if ( hitsAreOnSameIsle )
			{
				rprt.Log($"hits are on same island. Starting with same-surface path...");
				if ( !Surfaces[startHit.SurfaceIndex].CalculatePath_dbg(startHit, endHit, out bestPathFindPath, ref rprt) )
				{
					Debug.LogError($"LNX ERROR! Hits are on same island, yet CalculatePath returned false. This shouldn't happen...");
					rprt.Log($"LNX ERROR! Hits are on same island, yet CalculatePath returned false. This shouldn't happen...");
				}
				else
				{
					rprt.Log($"same-surface path calculation returned true. Found path: '{bestPathFindPath}' with " +
						$"'{bestPathFindPath.PointCount}' points...");
				}
			}
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

		public List<LNX_Path> CalculatePossibleSurfaceSequences( LNX_NavmeshHit startHit, LNX_NavmeshHit endHit, 
			bool includeDirectSequence = true)
		{
			List<LNX_Path> returnSequences = new List<LNX_Path>();
			if
			(
				includeDirectSequence &&
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
						LNX_Path cont = CreateFurlSequenceContinuation(rollingSequences[i], newLinks[i_nwLnks]);

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
				if (safetyTimeout > Surfaces.Count * Links.Count)
				{
					Debug.LogError($"safety timeout reached at: '{safetyTimeout}'");
					return null;
				}
			}

			return returnSequences;
		}

		public List<LNX_Path> CalculatePossibleSurfaceSequences_dbg(LNX_NavmeshHit startHit, LNX_NavmeshHit endHit, 
			ref LNX_MethodDebugReport rprt, bool includeDirectSequence = true)
		{
			rprt.StartMethod($"CalculatePossibleSurfaceSequences_dbg('{startHit}', '{endHit}')");

			List<LNX_Path> returnSequences = new List<LNX_Path>();
			if
			(
				includeDirectSequence && 
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

			List<LNX_Path> crntRollingSequences = new List<LNX_Path>()
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
				rprt.Log($"while{safetyTimeout}====================================================================================");
				rprt.Log($"rolling sequences count: '{crntRollingSequences.Count}'...");
				List<LNX_Path> nextRollingSequences = new List<LNX_Path>();
				for ( int i = 0; i < crntRollingSequences.Count; i++ )
				{
					rprt.Log($"for rolling sequence {i}================================");
					rprt.Log($"using sequence: '{crntRollingSequences[i]}...");
					rprt.Log($"getting links touching surface of '{Surfaces[crntRollingSequences[i].EndHit.SurfaceIndex].name}'...");
					//List<int> newLinks = GetIndicesOfLinksTouchingSurfaceIsland( crntRollingSequences[i] ); //<<<<<<<<<<<<<<<<
					List<int> newLinks = GetIndicesOfLinksTouchingSurfaceIsland_dbg(crntRollingSequences[i], ref rprt); //<<<<<<<<<<<<<<<<

					rprt.Log($"got '{newLinks.Count}' links touching last surface...");

					for ( int i_nwLnks = 0; i_nwLnks < newLinks.Count; i_nwLnks++ )
					{
						rprt.Log($"for link ({Links[newLinks[i_nwLnks]]})=========");
						rprt.Log($"creating furl sequence continuation...");
						LNX_Path cont = CreateFurlSequenceContinuation( crntRollingSequences[i], newLinks[i_nwLnks] );

						rprt.Log($"created continuation: '{cont}'...",
							$"This will lead to surface: '{Surfaces[cont.EndHit.SurfaceIndex]}...",
							$"now checking if this continuation ends on endHit island...");

						if
						(
							cont.EndHit.SurfaceIndex == endHit.SurfaceIndex &&
							Surfaces[endHit.SurfaceIndex].HitsAreOnSameIsland(cont.EndHit, endHit)
						)
						{
							cont.AddPoint( endHit );
							returnSequences.Add(new LNX_Path(cont));
							rprt.EmptyLine();
							rprt.Log($"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!",
								$"this new sequence ends on end surface/island. added sequence: " +
								$"'{returnSequences[returnSequences.Count - 1]}' to return collection...", 
								"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
							rprt.EmptyLine();

						}
						else
						{
							rprt.Log($"this continuation does NOT end on endHit island. Adding this sequence to next rolling sequences list..");
							nextRollingSequences.Add( new LNX_Path(cont) );
						}
					}
				}

				if ( nextRollingSequences.Count <= 0 )
				{
					rprt.Log($"none of the new links resulted in new sequences. Ending while loop...");
					foundAll = true;
				}
				else
				{
					rprt.Log($"now setting up '{nextRollingSequences.Count}' rolling sequences for next while...");
					crntRollingSequences = new List<LNX_Path>();
					for (int i = 0; i < nextRollingSequences.Count; i++)
					{
						crntRollingSequences.Add(nextRollingSequences[i]);
					}
				}

				rprt.EmptyLine();
				safetyTimeout++;
				if (safetyTimeout > Surfaces.Count * Links.Count)
				{
					Debug.LogError($"safety timeout reached at: '{safetyTimeout}'");
					rprt.Log_And_End_Method($"safety timeout reached at: '{safetyTimeout}'");
					return null;
				}
			}

			rprt.Log_And_End_Method($"finally returning '{returnSequences.Count}' sequences...");

			return returnSequences;
		}

		private LNX_Path CreateFurlSequenceContinuation(LNX_Path basePath, int linkIndx)
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
					LNX_Utils.FlattenHitToHit(
						Links[linkIndx].SpanBStartHit, 
						basePath.EndHit, 
						Links[linkIndx]._SurfaceA.SurfaceOrientation)
				);
			}
			else if (basePath.EndHit.SurfaceIndex == Links[linkIndx]._SurfaceB.MySurfaceIndex)
			{
				rtrnPath.AddPoint
				(
					LNX_Utils.FlattenHitToHit(Links[linkIndx].SpanAStartHit, basePath.EndHit, 
					Links[linkIndx]._SurfaceB.SurfaceOrientation)
				);
			}
			else
			{
				Debug.LogError($"LNX ERROR! CreateFurlSequenceContinuation(basePath: '{basePath}', linkIndx: '{linkIndx}') was passed a base " +
					$"path that did NOT end on a surface that the link at index touches. Returning null early...");
				return null;
			}

			return rtrnPath;
		}

		#endregion

		public List<int> GetIndicesOfLinksTouchingSurfaceIsland( LNX_Path backstop )
		{
			List<int> returnCollection = new List<int>();

			for (int i = 0; i < Links.Count; i++)
			{
				if ( PathTouchesLink(backstop, i) )
				{
					continue;
				}

				if (Links[i].TouchesSameSurfaceIsland(backstop.EndHit))
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

				if ( PathTouchesLink_dbg(backstop, i, ref rprt) )
				{
					rprt.Log($"PathTouchesLink() returned true. Skipping...");
					continue;
				}
				else
				{
					rprt.Log($"PathTouchesLink() returned false");
				}

				if (Links[i].TouchesSameSurfaceIsland_dbg(backstop.EndHit, ref rprt))
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

		/// <summary>
		/// Returns information about whether a link exists at hit location, and which span the hit touches.
		/// </summary>
		/// <param name="hit"></param>
		/// <returns>LNX_ComponentCoordinate.TriangleIndex = Link Index
		/// <para>LNX_ComponentCoordinate.ComponentIndex = which span (1 = spanA, 2 = spanB)</para>
		/// </returns>
		private LNX_ComponentCoordinate GetLinkInfoAtHit( LNX_NavmeshHit hit )
		{
			int lnkIndx = -1;
			int whichSpan = -1;

			for (int i = 0; i < Links.Count; i++)
			{
				if (Links[i].TouchesSameSurfaceIsland(hit))
				{
					if (Links[i].HitIsOnSpanA(hit))
					{
						lnkIndx = i;
						whichSpan = 1;
						break;
					}
					else if (Links[i].HitIsOnSpanB(hit))
					{
						lnkIndx = i;
						whichSpan = 2;
						break;
					}
				}
			}

			if (lnkIndx < 0)
			{
				return LNX_ComponentCoordinate.None;
			}

			return new LNX_ComponentCoordinate(-1, lnkIndx, whichSpan);
		}
		private LNX_ComponentCoordinate GetLinkInfoAtHit_dbg(LNX_NavmeshHit hit, ref LNX_MethodDebugReport rprt )
		{
			rprt.StartMethod($"GetLinkInfoAtHit_dbg(hit: '{hit}')");
			int lnkIndx = -1;
			int whichSpan = -1;

			rprt.Log($"checking each link...");
			for (int i = 0; i < Links.Count; i++)
			{
				rprt.Log($"for{i}=======================",
				$"using Link: '{Links[i]}'...");

				if (Links[i].TouchesSameSurfaceIsland(hit))
				{
					rprt.Log($"Link DOES touch surface island. Figuring on which span...");

					if (Links[i].HitIsOnSpanA(hit))
					{
						rprt.Log($"found that hit is on spanA...");
						lnkIndx = i;
						whichSpan = 1;
						rprt.Log($"set lnkIndx: '{lnkIndx}', whichSpan: '{whichSpan}'...");
						break;
					}
					else if (Links[i].HitIsOnSpanB(hit))
					{
						rprt.Log($"found that hit is on spanB...");
						lnkIndx = i;
						whichSpan = 2;
						rprt.Log($"set lnkIndx: '{lnkIndx}', whichSpan: '{whichSpan}'...");
						break;
					}
					else
					{
						rprt.Log($"apparently hit is on neither link span...");
					}
				}
				else
				{
					rprt.Log($"apparently link does NOT touch surface island of hit...");
				}
			}

			if (lnkIndx < 0)
			{
				rprt.Log_And_End_Method($"finally returning none...");
				return LNX_ComponentCoordinate.None;
			}

			rprt.Log_And_End_Method($"finally lnkIndx: '{lnkIndx}'. Returning valid coordinate...");
			return new LNX_ComponentCoordinate(-1, lnkIndx, whichSpan);
		}
		public int GetIndexOfLinkTouchingHit(LNX_NavmeshHit hit)
		{
			for (int i = 0; i < Links.Count; i++)
			{
				if ( Links[i].TouchesSameSurfaceIsland(hit) && Links[i].HitIsOnEitherSpan(hit) )
				{
					return i;
				}
			}

			return -1;
		}
		public int GetIndexOfLinkTouchingHit_dbg(LNX_NavmeshHit hit, ref LNX_MethodDebugReport rprt)
		{
			rprt.StartMethod($"GetIndexOfLinkTouchingHit_dbg(hit: '{hit}')");
			for (int i = 0; i < Links.Count; i++)
			{
				rprt.Log($"for{i}=======================",
					$"checking Link: '{Links[i]}'...");

				rprt.Log($"first condition: '{Links[i].TouchesSameSurfaceIsland(hit)}'");

				if 
				(
					Links[i].TouchesSameSurfaceIsland(hit) && 
					//Links[i].HitIsOnEitherSpan(hit))
					Links[i].HitIsOnEitherSpan_dbg(hit, ref rprt)
				)
				{
				rprt.Log_And_End_Method($"Found that link contacts hit. returning '{i}'...");
					return i;
				}
				else
				{
					rprt.Log($"apparently link does NOT touch hit...");
				}
			}

			rprt.Log_And_End_Method($"finally returning -1...");
			return -1;
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
		public bool PathTouchesLink_dbg(LNX_Path path, int linkIndx, ref LNX_MethodDebugReport rprt)
		{
			rprt.StartMethod($"PathTouchesLink_dbg(path: '{path}', linkIndx: '{linkIndx}')");

			rprt.Log($"checking '{path.PointCount}' points...");
			for (int i = 0; i < path.PathPoints.Count; i++)
			{
				rprt.Log($"for{i} (hit: '{path.PathPoints[i]}')...");

				if (path.PathPoints[i].LinkIndex == linkIndx)
				{
					rprt.Log_And_End_Method($"hit link index is equal to passed linkIndx. Returning true...");
					return true;
				}
			}

			rprt.Log_And_End_Method($"finally returning false...");
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
		}
		

		
		public bool VectorClearsLinksInPath( LNX_NavmeshHit originHit, Vector3 vDir, LNX_Path path )
		{
			#region SHORT-CIRCUITING ================================
			if ( vDir == Vector3.zero )
			{
				Debug.LogError($"LNX ERROR! VectorClearsLinksInPath() was passed a projection direction vector ({vDir}) " +
					$"that was equal to Vector3.zero. Cannot proceed. Returning false early...");
				return false;
			}
			#endregion

			Vector3 v_origin = LNX_Utils.FlatVector(originHit.Position, originHit.SurfaceOrientation);
			Vector3 v_dir_flt = LNX_Utils.FlatVector(vDir, originHit.SurfaceOrientation).normalized;

			for ( int i = 0; i < path.PathPoints.Count - 1; i++ )
			{
				if ( path.PathPoints[i].LinkIndex > -1 )
				{
					if 
					( 
						!LNX_Utils.AmInVectorCone
						(
							v_dir_flt,
							Links[path.PathPoints[i].LinkIndex].SpanAStartHit.Position - originHit.Position,
							Links[path.PathPoints[i].LinkIndex].SpanAEndHit.Position - originHit.Position,
							originHit.SurfaceOrientation, true
						)
					)
					{
						return false;
					}
					if
					(
						!LNX_Utils.AmInVectorCone
						(
							v_dir_flt,
							Links[path.PathPoints[i].LinkIndex].SpanBStartHit.Position - originHit.Position,
							Links[path.PathPoints[i].LinkIndex].SpanBEndHit.Position - originHit.Position,
							originHit.SurfaceOrientation, true
						)
					)
					{
						return false;
					}
				}
			}

			return true;
		}
		

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

		#region HELPERS ========================================
		[ContextMenu("z call GetProblemStringForSurfaces()")]
		public string GetProblemStringForSurfaces()
		{
			errorMsg = string.Empty;
			for (int i = 0; i < Surfaces.Count; i++)
			{
				if (Surfaces[i].MySurfaceIndex != i)
				{
					if (string.IsNullOrEmpty(errorMsg))
					{
						errorMsg = "ERROR!\n";
					}

					errorMsg += $"{Surfaces[i].gameObject.name} surface index not set correctly\n";
				}

				if (Surfaces[i].StateFlag == -1)
				{
					Debug.Log($"Surface{i} state flag says transitioning. Refreshing spans for any links touching this surface...");
					for (int i_lnks = 0; i_lnks < Links.Count; i_lnks++)
					{
						if (Links[i_lnks].TouchesSurface(Surfaces[i]))
						{
							Links[i_lnks].RefreshSpans();
						}
					}
				}
			}

			return errorMsg;
		}

		[ContextMenu("z call GetProblemStringForLinks()")]
		public string GetProblemStringForLinks()
		{
			errorMsg = string.Empty;

			for ( int i = 0; i < Links.Count; i++ )
			{
				if ( Links[i].MyLinkIndex != i )
				{
					if ( string.IsNullOrWhiteSpace(errorMsg) )
					{
						errorMsg = "ERROR!\n";
					}

					errorMsg += $"Link{i} ({Links[i].gameObject.name}) index not set correctly\n";
				}

				if (Links[i].SpanAStartHit.LinkIndex != i)
				{
					errorMsg += $"Link{i} ({Links[i].gameObject.name}) SpanAStartHit linkIndex ('{Links[i].SpanAStartHit.LinkIndex}') doesn't match link\n";
				}
				if (Links[i].SpanAEndHit.LinkIndex != i)
				{
					errorMsg += $"Link{i} ({Links[i].gameObject.name}) SpanAEndHit linkIndex ('{Links[i].SpanAEndHit.LinkIndex}') doesn't match link\n";
				}
				if (Links[i].SpanBStartHit.LinkIndex != i)
				{
					errorMsg += $"Link{i} ({Links[i].gameObject.name}) SpanBStartHit linkIndex ('{Links[i].SpanBStartHit.LinkIndex}') doesn't match link\n";
				}
				if (Links[i].SpanBEndHit.LinkIndex != i)
				{
					errorMsg += $"Link{i} ({Links[i].gameObject.name}) SpanBEndHit linkIndex ('{Links[i].SpanBEndHit.LinkIndex}') doesn't match link\n";
				}
			}

			return errorMsg;
		}
		#endregion

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

			errorMsg += GetProblemStringForSurfaces();
			errorMsg += GetProblemStringForLinks();

			if ( !string.IsNullOrEmpty(errorMsg))
			{
				Debug.LogError(errorMsg);
			}
		}
#endif
	}
}
