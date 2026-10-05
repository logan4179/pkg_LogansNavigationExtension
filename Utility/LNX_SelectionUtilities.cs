using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace LogansNavigationExtension
{
    public static class LNX_SelectionUtilities
    {
		#region EDGE =======================================================================================================
		public static float GetPerspectiveAlignmentWithEdge( LNX_Edge edge, Vector3 prspctv, Vector3 vPointingDir )
		{
			Vector3 vTo = Vector3.Normalize(edge.MidPosition - prspctv);

			float alignment = Vector3.Dot(vTo, vPointingDir.normalized);

			return alignment;
		}

		public static LNX_NavmeshHit GetBestEdgeHitOnSurface(LNX_NavMeshSurface srfc, Vector3 pos, bool onlyTerminal = false, int forceLinkIndx = -1)
		{
			LNX_NavmeshHit bestHit = LNX_NavmeshHit.None;
			float bestDist = float.MaxValue;

			for (int i = 0; i < srfc.Triangles.Length; i++)
			{
				for (int i_edges = 0; i_edges < 3; i_edges++)
				{
					if (onlyTerminal && !srfc.Triangles[i].Edges[i_edges].AmTerminal)
					{
						continue;
					}

					LNX_NavmeshHit hit = srfc.Triangles[i].Edges[i_edges].ClosestHitOnEdge(pos);
					if ( forceLinkIndx > -1 )
					{
						hit = new LNX_NavmeshHit(hit.Position, hit.Normal, hit.SurfaceIndex, hit.TriangleIndex,
							hit.VertIndex, hit.EdgeIndex, forceLinkIndx);
					}
					float dist = Vector3.Distance(pos, hit.Position);
					if (dist < bestDist)
					{
						bestDist = dist;
						bestHit = hit;
					}

				}
			}
			return bestHit;
		}
		public static LNX_NavmeshHit GetBestEdgeHitOnSurface_dbg(
			LNX_NavMeshSurface srfc, Vector3 pos, ref LNX_MethodDebugReport rprt, bool onlyTerminal = false, int linkIndx = -1)
		{
			rprt.StartMethod($"GetBestEdgeHitOnSurface_dbg(pos: '{pos}', onlyTrml: '{onlyTerminal}')");

			LNX_NavmeshHit bestHit = LNX_NavmeshHit.None;
			float bestDist = float.MaxValue;

			for (int i = 0; i < srfc.Triangles.Length; i++)
			{
				for (int i_edges = 0; i_edges < 3; i_edges++)
				{
					if (onlyTerminal && !srfc.Triangles[i].Edges[i_edges].AmTerminal)
					{
						continue;
					}

					LNX_NavmeshHit hit = LNX_NavmeshHit.None;

					hit = srfc.Triangles[i].Edges[i_edges].ClosestHitOnEdge_dbg(pos, ref rprt);
					if (linkIndx > -1)
					{
						hit = new LNX_NavmeshHit(hit.Position, hit.Normal, hit.SurfaceIndex, hit.TriangleIndex,
							hit.VertIndex, hit.EdgeIndex, linkIndx);
					}

					float dist = Vector3.Distance(pos, hit.Position);
					if (dist < bestDist)
					{
						bestDist = dist;
						bestHit = hit;
						rprt.Log($"found new best on hit: '{hit}', dist: '{dist}'");
					}

				}
			}

			rprt.Log_And_End_Method($"finally returning: '{bestHit}'");
			return bestHit;
		}

		public static LNX_NavmeshHit GetIntersectionOnSpan(LNX_NavMeshSurface srfc, LNX_NavmeshHit prspctvHit, Vector3 prjction, LNX_ComponentCoordinate[] span )
		{
			LNX_NavmeshHit bestHit = LNX_NavmeshHit.None;

			for ( int i = 0; i < span.Length; i++ )
			{
				if 
				( 
					srfc.Triangles[span[i].TriangleIndex].Edges[span[i].ComponentIndex].DoesProjectionIntersectEdge(
					prspctvHit, prjction, out bestHit)
				)
				{
					break;
				}
			}

			return bestHit;
		}

		/// <summary>
		/// Whether 
		/// </summary>
		/// <returns></returns>
		public static bool AmPointingAtEdge( LNX_Edge edge, float forgiveness )
        {
			Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			Vector3 vPerspective = SceneView.lastActiveSceneView.camera.transform.position;

			float alignment = GetPerspectiveAlignmentWithEdge( edge, vPerspective, mouseRay.direction );
			if (alignment > (1f / forgiveness) )
			{
				return true;
			}

			return false;
		}
		public static bool AmPointingAtEdge(LNX_Edge edge, float forgiveness, out float alignment )
		{
			Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			Vector3 vPerspective = SceneView.lastActiveSceneView.camera.transform.position;

			Vector3 vTo = Vector3.Normalize(edge.MidPosition - vPerspective);

			alignment = GetPerspectiveAlignmentWithEdge(edge, vPerspective, mouseRay.direction);
			if (alignment > (1f / forgiveness))
			{
				return true;
			}

			return false;
		}
		public static bool AmPointingAtEdge(LNX_Edge edge, Vector3 perspctv, Ray mouseRay, float forgiveness, out float alignment)
		{
			alignment = GetPerspectiveAlignmentWithEdge(edge, perspctv, mouseRay.direction);
			if (alignment > (1f / forgiveness))
			{
				return true;
			}

			return false;
		}

		public static bool ResolveTerminalSpan( LNX_NavMeshSurface srfc, LNX_ComponentCoordinate startEdgeCoord, 
			LNX_ComponentCoordinate endEdgeCoord, out LNX_ComponentCoordinate[] span )
		{
			span = null;

			#region SHORT-CIRCUITING =====================================================================
			if
			(
				!srfc.Triangles[startEdgeCoord.TriangleIndex].Edges[startEdgeCoord.ComponentIndex].AmTerminal ||
				!srfc.Triangles[endEdgeCoord.TriangleIndex].Edges[endEdgeCoord.ComponentIndex].AmTerminal
			)
			{
				return false;
			}
			if (startEdgeCoord.ComponentIndex == endEdgeCoord.ComponentIndex)
			{
				span = new LNX_ComponentCoordinate[1] { startEdgeCoord };
				return true;
			}
			#endregion

			List<LNX_ComponentCoordinate> span_dirA = new List<LNX_ComponentCoordinate>() { startEdgeCoord };
			float dist_spanA = srfc.Triangles[startEdgeCoord.TriangleIndex].Edges[startEdgeCoord.ComponentIndex].EdgeLength;
			List<LNX_ComponentCoordinate> span_dirB = new List<LNX_ComponentCoordinate>() { startEdgeCoord };
			float dist_spanB = dist_spanA;

			LNX_Edge strtEdge = srfc.Triangles[startEdgeCoord.TriangleIndex].Edges[startEdgeCoord.ComponentIndex];
			LNX_Edge endEdge = srfc.Triangles[endEdgeCoord.TriangleIndex].Edges[endEdgeCoord.ComponentIndex];

			LNX_Vertex strtVrt = srfc.Triangles[startEdgeCoord.TriangleIndex].Verts[strtEdge.StartVertCoordinate.ComponentIndex];
			LNX_Vertex endVrt = srfc.Triangles[startEdgeCoord.TriangleIndex].Verts[strtEdge.EndVertCoordinate.ComponentIndex];
			#region SHORT-CIRCUITING =====================================================================
			if (strtVrt.SharedVertexCoordinates == null && endVrt.SharedVertexCoordinates == null)
			{
				return false;
			}
			#endregion

			if (strtVrt.SharedVertexCoordinates.Length > 0)
			{
				LNX_ComponentCoordinate crntVrtCoord = strtEdge.StartVertCoordinate;

				bool foundEnd = false;
				int safetyTimeout = 0;

				while (!foundEnd)
				{
					LNX_ComponentCoordinate edgeCoord = GetTerminalEdgeAtVert(srfc, crntVrtCoord);

					if (edgeCoord == LNX_ComponentCoordinate.None)
					{
						foundEnd = true;
						break;
					}

					if (edgeCoord == endEdgeCoord || edgeCoord == LNX_ComponentCoordinate.None)
					{
						foundEnd = true;
					}
					else if (GetStartVert(edgeCoord, srfc).SharesVertSpace_ViaRelational(crntVrtCoord))
					{
						crntVrtCoord = srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].EndVertCoordinate;
					}
					else
					{
						crntVrtCoord = srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].StartVertCoordinate;
					}

					span_dirA.Add(edgeCoord);
					dist_spanA += srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].EdgeLength;

					safetyTimeout++;
					if (safetyTimeout > srfc.Triangles.Length)
					{
						Debug.LogError($"LNX ERROR! Safety timeout was reached while trying to resolve terminal span");
						return false;
					}
				}
			}
			if (endVrt.SharedVertexCoordinates.Length > 0)
			{
				LNX_ComponentCoordinate crntVrtCoord = strtEdge.EndVertCoordinate;

				bool foundEnd = false;
				int safetyTimeout = 0;

				while (!foundEnd)
				{
					LNX_ComponentCoordinate edgeCoord = GetTerminalEdgeAtVert(srfc, crntVrtCoord);

					if (edgeCoord == LNX_ComponentCoordinate.None)
					{
						foundEnd = true;
						break;
					}

					if (edgeCoord == endEdgeCoord || edgeCoord == LNX_ComponentCoordinate.None)
					{
						foundEnd = true;
					}
					else if (GetEndVert(edgeCoord, srfc).SharesVertSpace_ViaRelational(crntVrtCoord))
					{
						crntVrtCoord = srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].StartVertCoordinate;
					}
					else
					{
						crntVrtCoord = srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].EndVertCoordinate;
					}

					span_dirB.Add(edgeCoord);
					dist_spanB += srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].EdgeLength;

					safetyTimeout++;
					if (safetyTimeout > srfc.Triangles.Length)
					{
						Debug.LogError($"LNX ERROR! Safety timeout was reached while trying to resolve terminal span");
						return false;
					}
				}
			}

			if (dist_spanA < dist_spanB)
			{
				span = span_dirA.ToArray();
				return true;
			}
			else if (dist_spanB < dist_spanA)
			{
				span = span_dirB.ToArray();
				return true;
			}

			return false;
		}

		public static bool ResolveTerminalSpan_dbg(LNX_NavMeshSurface srfc, LNX_ComponentCoordinate startEdgeCoord,
			LNX_ComponentCoordinate endEdgeCoord, out LNX_ComponentCoordinate[] span, ref LNX_MethodDebugReport rprt)
		{
			rprt.StartMethod($"ResolveTerminalSpan_dbg({startEdgeCoord}, {endEdgeCoord})");
			span = null;

			#region SHORT-CIRCUITING =====================================================================
			if
			(
				!srfc.Triangles[startEdgeCoord.TriangleIndex].Edges[startEdgeCoord.ComponentIndex].AmTerminal ||
				!srfc.Triangles[endEdgeCoord.TriangleIndex].Edges[endEdgeCoord.ComponentIndex].AmTerminal
			)
			{
				rprt.Log_And_End_Method($"start or end edge not terminal. Both should be terminal. Returning false early...");
				return false;
			}
			if( startEdgeCoord.ComponentIndex == endEdgeCoord.ComponentIndex )
			{
				span = new LNX_ComponentCoordinate[1] { startEdgeCoord };
				rprt.Log_And_End_Method($"startEdgeCoord and endEdgeCoord component index the same. Short-circuiting with assumed span...");
				return true;
			}
			#endregion

			List<LNX_ComponentCoordinate> span_dirA = new List<LNX_ComponentCoordinate>() { startEdgeCoord };
			float dist_spanA = srfc.Triangles[startEdgeCoord.TriangleIndex].Edges[startEdgeCoord.ComponentIndex].EdgeLength;
			List<LNX_ComponentCoordinate> span_dirB = new List<LNX_ComponentCoordinate>() { startEdgeCoord };
			float dist_spanB = dist_spanA;

			LNX_Edge strtEdge = srfc.Triangles[startEdgeCoord.TriangleIndex].Edges[startEdgeCoord.ComponentIndex];
			LNX_Edge endEdge = srfc.Triangles[endEdgeCoord.TriangleIndex].Edges[endEdgeCoord.ComponentIndex];

			LNX_Vertex strtVrt = srfc.Triangles[startEdgeCoord.TriangleIndex].Verts[strtEdge.StartVertCoordinate.ComponentIndex];
			LNX_Vertex endVrt = srfc.Triangles[startEdgeCoord.TriangleIndex].Verts[strtEdge.EndVertCoordinate.ComponentIndex];
			rprt.Log($"decided strtVrt: '{strtVrt}', endVrt: '{endVrt}'");
			#region SHORT-CIRCUITING =====================================================================
			if (strtVrt.SharedVertexCoordinates == null && endVrt.SharedVertexCoordinates == null)
			{
				rprt.Log_And_End_Method($"found that start and end verts don't have shared vert coordinates. Returning false early...");
				return false;
			}
			#endregion

			rprt.Log($"strtVrt.SharedVertexCoordinates.Length: '{strtVrt.SharedVertexCoordinates.Length}.",
				$"endVrt.SharedVertexCoordinates.Length: '{endVrt.SharedVertexCoordinates.Length}.");

			if (strtVrt.SharedVertexCoordinates.Length > 0)
			{
				rprt.Log($"assembling span_dirA...");
				LNX_ComponentCoordinate crntVrtCoord = strtEdge.StartVertCoordinate;

				bool foundEnd = false;
				int safetyTimeout = 0;

				while (!foundEnd)
				{
					LNX_ComponentCoordinate edgeCoord = GetTerminalEdgeAtVert(srfc, crntVrtCoord);

					if (edgeCoord == LNX_ComponentCoordinate.None)
					{
						foundEnd = true;
						break;
					}

					if (edgeCoord == endEdgeCoord || edgeCoord == LNX_ComponentCoordinate.None)
					{
						foundEnd = true;
					}
					else if (GetStartVert(edgeCoord, srfc).SharesVertSpace_ViaRelational(crntVrtCoord))
					{
						crntVrtCoord = srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].EndVertCoordinate;
					}
					else
					{
						crntVrtCoord = srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].StartVertCoordinate;
					}

					span_dirA.Add(edgeCoord);
					dist_spanA += srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].EdgeLength;

					safetyTimeout++;
					if (safetyTimeout > srfc.Triangles.Length)
					{
						Debug.LogError($"LNX ERROR! Safety timeout was reached while trying to resolve terminal span");
						rprt.Log_And_End_Method($"LNX ERROR! Safety timeout was reached while trying to resolve terminal span");
						return false;
					}
				}
				rprt.Log($"assembled span_dirA with '{span_dirA.Count}' coordinates. Dist: '{dist_spanA}'");
			}
			if (endVrt.SharedVertexCoordinates.Length > 0)
			{
				rprt.Log($"assembling span_dirB...");

				LNX_ComponentCoordinate crntVrtCoord = strtEdge.EndVertCoordinate;

				bool foundEnd = false;
				int safetyTimeout = 0;

				while (!foundEnd)
				{
					LNX_ComponentCoordinate edgeCoord = GetTerminalEdgeAtVert(srfc, crntVrtCoord);

					if (edgeCoord == LNX_ComponentCoordinate.None)
					{
						foundEnd = true;
						break;
					}

					if (edgeCoord == endEdgeCoord || edgeCoord == LNX_ComponentCoordinate.None)
					{
						foundEnd = true;
					}
					else if (GetEndVert(edgeCoord, srfc).SharesVertSpace_ViaRelational(crntVrtCoord))
					{
						crntVrtCoord = srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].StartVertCoordinate;
					}
					else
					{
						crntVrtCoord = srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].EndVertCoordinate;
					}

					span_dirB.Add(edgeCoord);
					dist_spanB += srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].EdgeLength;

					safetyTimeout++;
					if (safetyTimeout > srfc.Triangles.Length)
					{
						Debug.LogError($"LNX ERROR! Safety timeout was reached while trying to resolve terminal span");
						rprt.Log_And_End_Method($"LNX ERROR! Safety timeout was reached while trying to resolve terminal span");
						return false;
					}
				}
				rprt.Log($"assembled span_dirA with '{span_dirB.Count}' coordinates. Dist: '{dist_spanB}'");
			}

			if (dist_spanA < dist_spanB)
			{
				span = span_dirA.ToArray();
				rprt.Log_And_End_Method($"decided dist_spanA < dist_spanB. Using span_dirA. Returning true...");
				return true;
			}
			else if (dist_spanB < dist_spanA)
			{
				span = span_dirB.ToArray();
				rprt.Log_And_End_Method($"decided dist_spanB < dist_spanA. Using span_dirB. Returning true...");
				return true;
			}

			rprt.Log_And_End_Method($"Got to end. Returning false...");
			return false;
		}

		/*
		public static bool ResolveTerminalSpan(LNX_NavMeshSurface srfc, LNX_NavmeshHit startHit,
			LNX_NavmeshHit endHit, out LNX_NavmeshHit[] span)
		{
			span = null;

			#region SHORT-CIRCUITING =====================================================================
			if ( !srfc.GetEdge(startHit).AmTerminal || !srfc.GetEdge(endHit).AmTerminal )
			{
				return false;
			}
			if
			(
				startHit.EdgeIndex == endHit.EdgeIndex ||
				startHit.Position == endHit.Position ||
				(
					startHit.EdgeIndex > -1 &&
					srfc.GetEdge(startHit).PositionTouchesStartOrEnd(startHit.Position) && srfc.GetEdge(startHit).PositionTouchesStartOrEnd(endHit.Position)
				))
			{
				span = new LNX_NavmeshHit[1] {
					startHit
				};
				return true;
			}
			else if ( startHit.VertIndex > -1 ) //check all touching edges for if endhit touches the edge.
			{
				LNX_Vertex vert = srfc.Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex];
				for ( int i = 0; i < vert.SharedVertexCoordinates.Length; i++ )
				{
					LNX_ComponentCoordinate coord = srfc.Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].SharedVertexCoordinates[i];
					LNX_Edge touchingEdge = srfc.GetEdge(srfc.GetVertexAtCoordinate(coord).FirstFormingEdgeCoordinate);

					if ( touchingEdge.AmTerminal )
					{
						if 
						( 
							endHit.EdgeIndex == touchingEdge.ComponentIndex || 
							endHit.EdgeIndex == touchingEdge.SharedEdgeCoordinate.ComponentIndex ||
							touchingEdge.PositionTouchesStartOrEnd(endHit.Position) 
						)
						{
							span = new LNX_NavmeshHit[1] {
								startHit
							};
							return true;
						}
					}
				}
			}
			#endregion

			List<LNX_NavmeshHit> span_dirA = new List<LNX_NavmeshHit>() { startHit };
			float dist_spanA = srfc.Triangles[startEdgeCoord.TrianglesIndex].Edges[startEdgeCoord.ComponentIndex].EdgeLength;
			List<LNX_ComponentCoordinate> span_dirB = new List<LNX_ComponentCoordinate>() { startEdgeCoord };
			float dist_spanB = dist_spanA;

			LNX_Edge strtEdge = srfc.Triangles[startEdgeCoord.TrianglesIndex].Edges[startEdgeCoord.ComponentIndex];
			LNX_Edge endEdge = srfc.Triangles[endEdgeCoord.TrianglesIndex].Edges[endEdgeCoord.ComponentIndex];

			LNX_Vertex strtVrt = srfc.Triangles[startEdgeCoord.TrianglesIndex].Verts[strtEdge.StartVertCoordinate.ComponentIndex];
			LNX_Vertex endVrt = srfc.Triangles[startEdgeCoord.TrianglesIndex].Verts[strtEdge.EndVertCoordinate.ComponentIndex];
			#region SHORT-CIRCUITING =====================================================================
			if (strtVrt.SharedVertexCoordinates == null && endVrt.SharedVertexCoordinates == null)
			{
				return false;
			}
			#endregion

			if (strtVrt.SharedVertexCoordinates.Length > 0)
			{
				LNX_ComponentCoordinate crntVrtCoord = strtEdge.StartVertCoordinate;

				bool foundEnd = false;
				int safetyTimeout = 0;

				while (!foundEnd)
				{
					LNX_ComponentCoordinate edgeCoord = GetTerminalEdgeAtVert(srfc, crntVrtCoord);

					if (edgeCoord == LNX_ComponentCoordinate.None)
					{
						foundEnd = true;
						break;
					}

					if (edgeCoord == endEdgeCoord || edgeCoord == LNX_ComponentCoordinate.None)
					{
						foundEnd = true;
					}
					else if (GetStartVert(edgeCoord, srfc).SharesVertSpace_ViaRelational(crntVrtCoord))
					{
						crntVrtCoord = srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].EndVertCoordinate;
					}
					else
					{
						crntVrtCoord = srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].StartVertCoordinate;
					}

					span_dirA.Add(edgeCoord);
					dist_spanA += srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].EdgeLength;

					safetyTimeout++;
					if (safetyTimeout > srfc.Triangles.Length)
					{
						Debug.LogError($"LNX ERROR! Safety timeout was reached while trying to resolve terminal span");
						return false;
					}
				}
			}
			if (endVrt.SharedVertexCoordinates.Length > 0)
			{
				LNX_ComponentCoordinate crntVrtCoord = strtEdge.EndVertCoordinate;

				bool foundEnd = false;
				int safetyTimeout = 0;

				while (!foundEnd)
				{
					LNX_ComponentCoordinate edgeCoord = GetTerminalEdgeAtVert(srfc, crntVrtCoord);

					if (edgeCoord == LNX_ComponentCoordinate.None)
					{
						foundEnd = true;
						break;
					}

					if (edgeCoord == endEdgeCoord || edgeCoord == LNX_ComponentCoordinate.None)
					{
						foundEnd = true;
					}
					else if (GetEndVert(edgeCoord, srfc).SharesVertSpace_ViaRelational(crntVrtCoord))
					{
						crntVrtCoord = srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].StartVertCoordinate;
					}
					else
					{
						crntVrtCoord = srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].EndVertCoordinate;
					}

					span_dirB.Add(edgeCoord);
					dist_spanB += srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].EdgeLength;

					safetyTimeout++;
					if (safetyTimeout > srfc.Triangles.Length)
					{
						Debug.LogError($"LNX ERROR! Safety timeout was reached while trying to resolve terminal span");
						return false;
					}
				}
			}

			if (dist_spanA < dist_spanB)
			{
				span = span_dirA.ToArray();
				return true;
			}
			else if (dist_spanB < dist_spanA)
			{
				span = span_dirB.ToArray();
				return true;
			}

			return false;
		}
		*/

		public static LNX_ComponentCoordinate GetTerminalEdgeAtVert(LNX_NavMeshSurface srfc, LNX_ComponentCoordinate vrtCoord )
		{
			LNX_Vertex vrt = srfc.Triangles[vrtCoord.TriangleIndex].Verts[vrtCoord.ComponentIndex];
			if( vrt.SharedVertexCoordinates == null || vrt.SharedVertexCoordinates.Length <= 0 )
			{
				return LNX_ComponentCoordinate.None;
			}

			for ( int i = 0; i < vrt.SharedVertexCoordinates.Length; i++ )
			{
				if 
				( 
					srfc.Triangles[vrt.SharedVertexCoordinates[i].TriangleIndex].
					Edges[srfc.GetVertexAtCoordinate(vrt.SharedVertexCoordinates[i]).Index_FirstFormingEdge].AmTerminal
				)
				{
					return srfc.Triangles[vrt.SharedVertexCoordinates[i].TriangleIndex].
					Edges[srfc.GetVertexAtCoordinate(vrt.SharedVertexCoordinates[i]).Index_FirstFormingEdge].MyCoordinate;
				}
				if
				(
					srfc.Triangles[vrt.SharedVertexCoordinates[i].TriangleIndex].
					Edges[srfc.GetVertexAtCoordinate(vrt.SharedVertexCoordinates[i]).Index_SecondFormingEdge].AmTerminal
				)
				{
					return srfc.Triangles[vrt.SharedVertexCoordinates[i].TriangleIndex].
					Edges[srfc.GetVertexAtCoordinate(vrt.SharedVertexCoordinates[i]).Index_SecondFormingEdge].MyCoordinate;
				}
			}

			return LNX_ComponentCoordinate.None;
		}

		public static LNX_ComponentCoordinate GetTerminalEdgeAtEdgeStart( LNX_NavMeshSurface srfc, LNX_ComponentCoordinate edgeCoord )
		{
			//LNX_Edge edge = srfc.GetEdge(edgeCoord);
			//LNX_Vertex vrt = srfc.GetVertexAtCoordinate(edge.StartVertCoordinate);
			LNX_Vertex vrt = srfc.Triangles[edgeCoord.TriangleIndex].Verts[srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].StartVertCoordinate.ComponentIndex];
			if (vrt.SharedVertexCoordinates == null || vrt.SharedVertexCoordinates.Length <= 0)
			{
				return LNX_ComponentCoordinate.None;
			}


			for (int i = 0; i < vrt.SharedVertexCoordinates.Length; i++)
			{
				if
				(
					srfc.Triangles[vrt.SharedVertexCoordinates[i].TriangleIndex].
					Edges[srfc.GetVertexAtCoordinate(vrt.SharedVertexCoordinates[i]).Index_FirstFormingEdge].AmTerminal
				)
				{
					return srfc.Triangles[vrt.SharedVertexCoordinates[i].TriangleIndex].
					Edges[srfc.GetVertexAtCoordinate(vrt.SharedVertexCoordinates[i]).Index_FirstFormingEdge].MyCoordinate;
				}
			}

			return LNX_ComponentCoordinate.None;
		}
		
		#endregion


		public static LNX_Vertex GetStartVert(LNX_ComponentCoordinate edgeCoord, LNX_NavMeshSurface srfc )
		{
			return srfc.Triangles[
					edgeCoord.TriangleIndex
				].Verts
				[
					srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].StartVertCoordinate.ComponentIndex
				];
		}
		public static LNX_Vertex GetEndVert(LNX_ComponentCoordinate edgeCoord, LNX_NavMeshSurface srfc)
		{
			return srfc.Triangles[
					edgeCoord.TriangleIndex
				].Verts
				[
					srfc.Triangles[edgeCoord.TriangleIndex].Edges[edgeCoord.ComponentIndex].EndVertCoordinate.ComponentIndex
				];
		}
	}
}
