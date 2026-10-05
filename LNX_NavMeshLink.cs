using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class LNX_NavMeshLink : MonoBehaviour
    {
		public LNX_Manager _Manager;
		public LNX_NavMeshSurface _SurfaceA;
		public LNX_NavMeshSurface _SurfaceB;

		[HideInInspector] public LNX_NavmeshHit SpanAStartHit;
		[HideInInspector] public LNX_NavmeshHit SpanAEndHit;
		[HideInInspector] public LNX_NavmeshHit SpanBStartHit;
		[HideInInspector] public LNX_NavmeshHit SpanBEndHit;

		//[Header("OTHER")]
		[SerializeField] private int myLinkIndex;
		public int MyLinkIndex => myLinkIndex;
		[SerializeField, HideInInspector] private Mesh _VisualizationMesh;
		[SerializeField] private LNX_ComponentCoordinate[] spanA;
		[SerializeField] private LNX_ComponentCoordinate[] spanB;
		//[SerializeField, HideInInspector] private LNX_NavmeshHit[] spanA;
		//[SerializeField, HideInInspector] private LNX_NavmeshHit[] spanB;

		//[Header("SELECTION")]
		public bool SetSpans = false;
		[HideInInspector] public Vector3 HandlePos_spanAStart;
		[HideInInspector] public Vector3 HandlePos_spanAEnd;
		[HideInInspector] public Vector3 HandlePos_spanBStart;
		[HideInInspector] public Vector3 HandlePos_spanBEnd;


		[Header("DEBUG")]
		[SerializeField] private bool drawVisualizationMesh;
		[SerializeField] private Color color_visualMesh;
		public LNX_MethodDebugReport MthdRprt;
		[SerializeField, TextArea(1,10)] private string dbg_class;

		public void ReconstructVisualizationMesh()
		{
			#region CREATE VISUALIZATION MESH ---------------------------------
			_VisualizationMesh = new Mesh();

			_VisualizationMesh.vertices = new Vector3[4]
			{
				SpanAStartHit.Position,
				SpanAEndHit.Position,
				SpanBStartHit.Position,
				SpanBEndHit.Position
			};

			Vector3 nrml = Vector3.Normalize(
				(_SurfaceA.GetSurfaceProjectionVector() + _SurfaceB.GetSurfaceProjectionVector()) * 0.5f
			);
			Vector3[] nrmls = new Vector3[4] { nrml, nrml, nrml, nrml };
			_VisualizationMesh.normals = nrmls;

			_VisualizationMesh.triangles = new int[6] 
			{ 
				//0, 1, 2, //for some reason, this makes it face the wrong way
				2, 1, 0,

				//2, 3, 0,
				1, 2, 3
			};
			#endregion
		}

		#region HANDLES/SPANS ========================================
		public void UpdateHandles( Vector3 newHndlPos_strtA, Vector3 newHndlPos_endA, Vector3 newHndlPos_strtB, Vector3 newHndlPos_endB )
		{
			//Debug.Log($"UpdateHandles(aStart: '{newHndlPos_strtA}', aEnd: '{newHndlPos_endA}', bStart: '{newHndlPos_strtB}', bEnd: '{newHndlPos_endB}')");
			bool moveChange = false;
			bool spanAChange = false;
			bool spanBChange = false;

			#region UPDATE HANDLES ===============================================
			if (HandlePos_spanAStart != newHndlPos_strtA )
			{
				moveChange = true;

				LNX_NavmeshHit oldHit = SpanAStartHit;
				SpanAStartHit = LNX_SelectionUtilities.GetBestEdgeHitOnSurface(_SurfaceA, newHndlPos_strtA, true, myLinkIndex);
				if (SpanAStartHit.EdgeIndex != oldHit.EdgeIndex || SpanAStartHit.TriangleIndex != oldHit.TriangleIndex)
				{
					spanAChange = true;
				}
			}
			HandlePos_spanAStart = SpanAStartHit.Position;

			if ( HandlePos_spanAEnd != newHndlPos_endA )
			{
				moveChange = true;
				LNX_NavmeshHit oldHit = SpanAEndHit;
				SpanAEndHit = LNX_SelectionUtilities.GetBestEdgeHitOnSurface(_SurfaceA, newHndlPos_endA, true, myLinkIndex);

				if ( SpanAEndHit.EdgeIndex != oldHit.EdgeIndex || SpanAEndHit.TriangleIndex != oldHit.TriangleIndex )
				{
					spanAChange = true;
				}
			}
			HandlePos_spanAEnd = SpanAEndHit.Position;

			if (HandlePos_spanBStart != newHndlPos_strtB)
			{
				moveChange = true;

				LNX_NavmeshHit oldHit = SpanBStartHit;
				SpanBStartHit = LNX_SelectionUtilities.GetBestEdgeHitOnSurface(_SurfaceB, newHndlPos_strtB, true, myLinkIndex);

				if (SpanBStartHit.EdgeIndex != oldHit.EdgeIndex || SpanBStartHit.TriangleIndex != oldHit.TriangleIndex)
				{
					spanBChange = true;
				}
			}
			HandlePos_spanBStart = SpanBStartHit.Position;

			if ( HandlePos_spanBEnd != newHndlPos_endB )
			{
				moveChange = true;

				LNX_NavmeshHit oldHit = SpanBEndHit;
				SpanBEndHit = LNX_SelectionUtilities.GetBestEdgeHitOnSurface(_SurfaceB, newHndlPos_endB, true, myLinkIndex);

				if (SpanBEndHit.EdgeIndex != oldHit.EdgeIndex || SpanBEndHit.TriangleIndex != oldHit.TriangleIndex)
				{
					spanBChange = true;
				}
			}
			HandlePos_spanBEnd = SpanBEndHit.Position;
			#endregion

			#region CALCULATE SPAN =====================
			if ( spanAChange )
			{
				Debug.Log($"recalculating spanA using '{SpanAStartHit.AsEdgeCoordinate()}' and '{SpanAEndHit.AsEdgeCoordinate()}'...");
				LNX_SelectionUtilities.ResolveTerminalSpan(
					_SurfaceA, SpanAStartHit.AsEdgeCoordinate(), SpanAEndHit.AsEdgeCoordinate(), out spanA
				);
			}
			if (spanBChange)
			{
				Debug.Log($"recalculating spanA using '{SpanBStartHit.AsEdgeCoordinate()}' and '{SpanBEndHit.AsEdgeCoordinate()}'...");

				LNX_SelectionUtilities.ResolveTerminalSpan(
					_SurfaceB, SpanBStartHit.AsEdgeCoordinate(), SpanBEndHit.AsEdgeCoordinate(), out spanB
				);
			}
			#endregion
			if ( moveChange )
			{
				//Debug.Log($"movechange");

				ReconstructVisualizationMesh();
				GenerateDbgText();
			}
		}

		[ContextMenu("z call ZeroOutHandles()")]
		public void ZeroOutHandles()
		{
			HandlePos_spanAStart = transform.position + (Vector3.up) + (Vector3.left);
			HandlePos_spanAEnd = transform.position + (Vector3.up) + (Vector3.right);

			HandlePos_spanBStart = transform.position + (Vector3.down) + (Vector3.left);
			HandlePos_spanBEnd = transform.position + (Vector3.down) + (Vector3.left);
		}

		[ContextMenu("z call RefreshSpans()")]
		public void RefreshSpans()
		{
			SpanAStartHit = LNX_SelectionUtilities.GetBestEdgeHitOnSurface(_SurfaceA, HandlePos_spanAStart, true, myLinkIndex);
			SpanAEndHit = LNX_SelectionUtilities.GetBestEdgeHitOnSurface(_SurfaceA, HandlePos_spanAEnd, true, myLinkIndex);
			SpanBStartHit = LNX_SelectionUtilities.GetBestEdgeHitOnSurface(_SurfaceB, HandlePos_spanBStart, true, myLinkIndex);
			SpanBEndHit = LNX_SelectionUtilities.GetBestEdgeHitOnSurface(_SurfaceB, HandlePos_spanBEnd, true, myLinkIndex);

			HandlePos_spanAStart = SpanAStartHit.Position;
			HandlePos_spanAEnd = SpanAEndHit.Position;
			HandlePos_spanBStart = SpanBStartHit.Position;
			HandlePos_spanBEnd = SpanBEndHit.Position;

			LNX_SelectionUtilities.ResolveTerminalSpan(
				_SurfaceA, SpanAStartHit.AsEdgeCoordinate(), SpanAEndHit.AsEdgeCoordinate(), out spanA
			);
			LNX_SelectionUtilities.ResolveTerminalSpan(
				_SurfaceB, SpanBStartHit.AsEdgeCoordinate(), SpanBEndHit.AsEdgeCoordinate(), out spanB
			);

			ReconstructVisualizationMesh();
			GenerateDbgText();
		}
		#endregion

		#region SETTERS ======================================
		public void SetLinkIndex( int indx )
		{
			Debug.Log($"{this}.SetLinkIndex({indx})");

			myLinkIndex = indx;

			SpanAStartHit = new LNX_NavmeshHit(SpanAStartHit.Position, SpanAStartHit.Normal, SpanAStartHit.SurfaceIndex, 
				SpanAStartHit.TriangleIndex, SpanAStartHit.VertIndex, SpanAStartHit.EdgeIndex, indx);
			SpanAEndHit = new LNX_NavmeshHit(SpanAEndHit.Position, SpanAEndHit.Normal, SpanAEndHit.SurfaceIndex,
				SpanAEndHit.TriangleIndex, SpanAEndHit.VertIndex, SpanAEndHit.EdgeIndex, indx);

			SpanBStartHit = new LNX_NavmeshHit(SpanBStartHit.Position, SpanBStartHit.Normal, SpanBStartHit.SurfaceIndex,
				SpanBStartHit.TriangleIndex, SpanBStartHit.VertIndex, SpanBStartHit.EdgeIndex, indx);
			SpanBEndHit = new LNX_NavmeshHit(SpanBEndHit.Position, SpanBEndHit.Normal, SpanBEndHit.SurfaceIndex,
				SpanBEndHit.TriangleIndex, SpanBEndHit.VertIndex, SpanBEndHit.EdgeIndex, indx);
		}
		#endregion

		public Vector3 FlattenVectorToSpanA(Vector3 pos)
		{
			if( _SurfaceA.SurfaceOrientation == LNX_Direction.PositiveY )
			{
				return new Vector3(pos.x, MathF.Min(SpanAStartHit.Position.y, SpanAEndHit.Position.y), pos.z);
			}
			else if ( _SurfaceA.SurfaceOrientation == LNX_Direction.NegativeY )
			{
				return new Vector3(pos.x, MathF.Max(SpanAStartHit.Position.y, SpanAEndHit.Position.y), pos.z);
			}
			else if (_SurfaceA.SurfaceOrientation == LNX_Direction.PositiveX)
			{
				return new Vector3(MathF.Min(SpanAStartHit.Position.x, SpanAEndHit.Position.x), pos.y, pos.z);
			}
			else if (_SurfaceA.SurfaceOrientation == LNX_Direction.NegativeX)
			{
				return new Vector3(MathF.Max(SpanAStartHit.Position.x, SpanAEndHit.Position.x), pos.y, pos.z);
			}
			else if (_SurfaceA.SurfaceOrientation == LNX_Direction.PositiveZ)
			{
				return new Vector3(pos.x, pos.y, MathF.Min(SpanAStartHit.Position.z, SpanAEndHit.Position.z));
			}
			else if ( _SurfaceA.SurfaceOrientation == LNX_Direction.NegativeZ )
			{
				return new Vector3(pos.x, pos.y, MathF.Min(SpanAStartHit.Position.z, SpanAEndHit.Position.z));
			}

			Debug.LogError($"LNX ERROR! Surface orientation on surface: '{_SurfaceA}' apparently not set correctly...");
			return Vector3.zero;
		}

		public Vector3 FlattenVectorToSpanB(Vector3 pos)
		{
			if (_SurfaceB.SurfaceOrientation == LNX_Direction.PositiveY)
			{
				return new Vector3(pos.x, MathF.Min(SpanBStartHit.Position.y, SpanBEndHit.Position.y), pos.z);
			}
			else if (_SurfaceB.SurfaceOrientation == LNX_Direction.NegativeY)
			{
				return new Vector3(pos.x, MathF.Max(SpanBStartHit.Position.y, SpanBEndHit.Position.y), pos.z);
			}
			else if (_SurfaceB.SurfaceOrientation == LNX_Direction.PositiveX)
			{
				return new Vector3(MathF.Min(SpanBStartHit.Position.x, SpanBEndHit.Position.x), pos.y, pos.z);
			}
			else if (_SurfaceB.SurfaceOrientation == LNX_Direction.NegativeX)
			{
				return new Vector3(MathF.Max(SpanBStartHit.Position.x, SpanBEndHit.Position.x), pos.y, pos.z);
			}
			else if (_SurfaceB.SurfaceOrientation == LNX_Direction.PositiveZ)
			{
				return new Vector3(pos.x, pos.y, MathF.Min(SpanBStartHit.Position.z, SpanBEndHit.Position.z));
			}
			else if (_SurfaceB.SurfaceOrientation == LNX_Direction.NegativeZ)
			{
				return new Vector3(pos.x, pos.y, MathF.Min(SpanBStartHit.Position.z, SpanBEndHit.Position.z));
			}

			Debug.LogError($"LNX ERROR! Surface orientation on surface: '{_SurfaceB}' apparently not set correctly...");
			return Vector3.zero;
		}

		public LNX_NavmeshHit GetIntersectionAcrossLink(
			LNX_NavmeshHit prspctvHit, Vector3 prjction)
		{
			if ( prspctvHit.SurfaceIndex == _SurfaceA.MySurfaceIndex )
			{
				return LNX_SelectionUtilities.GetIntersectionOnSpan(_SurfaceB, prspctvHit, prjction, spanB);
			}
			if (prspctvHit.SurfaceIndex == _SurfaceB.MySurfaceIndex)
			{
				return LNX_SelectionUtilities.GetIntersectionOnSpan(_SurfaceA, prspctvHit, prjction, spanA );
			}

			return LNX_NavmeshHit.None;
		}

		public bool TouchesSurface( int srfcIndx )
		{
			if ( _SurfaceA == null && _SurfaceB == null )
			{
				return false;
			}

			if ( _SurfaceA == null )
			{
				return _SurfaceB.MySurfaceIndex == srfcIndx;
			}
			if (_SurfaceB == null)
			{
				return _SurfaceA.MySurfaceIndex == srfcIndx;
			}

			return _SurfaceA.MySurfaceIndex == srfcIndx || _SurfaceB.MySurfaceIndex == srfcIndx;
		}
		public bool TouchesSurface( LNX_NavMeshSurface srfc )
		{
			if (_SurfaceA == null && _SurfaceB == null)
			{
				return false;
			}

			return (srfc == _SurfaceA || srfc == _SurfaceB);
		}

		public bool TouchesSurfaceIsland( LNX_NavmeshHit hit )
		{
			if (_SurfaceA == null && _SurfaceB == null)
			{
				return false;
			}
			if ( hit.SurfaceIndex == _SurfaceA.MySurfaceIndex )
			{
				return _SurfaceA.GetAdjacencyDepthToTriangle(hit.TriangleIndex, spanA[0].TriangleIndex) > -1;
			}
			else if (hit.SurfaceIndex == _SurfaceB.MySurfaceIndex)
			{
				return _SurfaceB.GetAdjacencyDepthToTriangle(hit.TriangleIndex, spanB[0].TriangleIndex) > -1;
			}
			else
			{
				return false;
			}
		}
		public bool TouchesSurfaceIsland_dbg( LNX_NavmeshHit hit, ref LNX_MethodDebugReport rprt )
		{
			rprt.StartMethod($"{name}.TouchesSurfaceIsland_dbg(hit: '{hit}')");
			if (_SurfaceA == null && _SurfaceB == null)
			{
				rprt.Log_And_End_Method($"surfaces are not set. Short-circuit returning false...");
				return false;
			}

			if (hit.SurfaceIndex == _SurfaceA.MySurfaceIndex)
			{
				rprt.Log($"hit surface index is same as _SurfaceA.MySurfaceIndex...");

				int rslt = _SurfaceA.GetAdjacencyDepthToTriangle_dbg(hit.TriangleIndex, spanA[0].TriangleIndex, ref rprt);

				rprt.Log_And_End_Method($"returning {rslt} > 0...");
				return rslt > -1;
			}
			else if (hit.SurfaceIndex == _SurfaceB.MySurfaceIndex)
			{
				rprt.Log($"hit surface index is same as _SurfaceB.MySurfaceIndex...");

				int rslt = _SurfaceA.GetAdjacencyDepthToTriangle_dbg(hit.TriangleIndex, spanB[0].TriangleIndex, ref rprt);

				rprt.Log_And_End_Method($"returning {rslt} > 0...");
				return rslt > -1;
			}
			else
			{
				rprt.Log_And_End_Method($"Supplied hit surface index: '{hit.SurfaceIndex}' did not belong to either " +
					$"surfaces on link '{name}'. Returning false...");
				return false;
			}
		}

		public bool HitIsOnEitherSpan(LNX_NavmeshHit hit )
		{
			#region SHORT-CIRCUITING ===============================
			if (hit.SurfaceIndex != _SurfaceA.MySurfaceIndex &&
				hit.SurfaceIndex != _SurfaceB.MySurfaceIndex)
			{
				return false;
			}

			if (hit.EdgeIndex == -1 && hit.VertIndex == -1)
			{
				return false;
			}
			#endregion

			if (hit.SurfaceIndex == _SurfaceA.MySurfaceIndex)
			{
				for (int i = 0; i < spanA.Length; i++)
				{
					if (_SurfaceA.GetEdge(spanA[i]).HitTouchesEdge(hit))
					{
						if (i == 0)
						{
							LNX_Vertex nxtVrt = null;

							if
							(
								_SurfaceA.GetEdge(spanA[0]).StartPosition == _SurfaceA.GetEdge(spanA[1]).StartPosition ||
								_SurfaceA.GetEdge(spanA[0]).EndPosition == _SurfaceA.GetEdge(spanA[1]).StartPosition
							)
							{
								nxtVrt = _SurfaceA.GetVertexAtCoordinate(_SurfaceA.GetEdge(spanA[1]).StartVertCoordinate);
							}
							else if
							(
								_SurfaceA.GetEdge(spanA[0]).StartPosition == _SurfaceA.GetEdge(spanA[1]).EndPosition ||
								_SurfaceA.GetEdge(spanA[0]).EndPosition == _SurfaceA.GetEdge(spanA[1]).EndPosition
							)
							{
								nxtVrt = _SurfaceA.GetVertexAtCoordinate(_SurfaceA.GetEdge(spanA[1]).EndVertCoordinate);
							}

							if
							(
								LNX_Utils.PositionIsDistallyWithin
								(
									hit.Position, SpanAStartHit.Position, nxtVrt.V_Position
								)
							)
							{
								return true;
							}
						}
						else if (i == spanA.Length - 1)
						{
							LNX_Vertex prevVrt = null;

							if
							(
								_SurfaceA.GetEdge(spanA[i - 1]).StartPosition == _SurfaceA.GetEdge(spanA[i]).StartPosition ||
								_SurfaceA.GetEdge(spanA[i - 1]).EndPosition == _SurfaceA.GetEdge(spanA[i]).StartPosition
							)
							{
								prevVrt = _SurfaceA.GetVertexAtCoordinate(_SurfaceA.GetEdge(spanA[i]).StartVertCoordinate);
							}
							else if
							(
							_SurfaceA.GetEdge(spanA[i - 1]).StartPosition == _SurfaceA.GetEdge(spanA[i]).EndPosition ||
							_SurfaceA.GetEdge(spanA[i - 1]).EndPosition == _SurfaceA.GetEdge(spanA[i]).EndPosition
							)
							{
								prevVrt = _SurfaceA.GetVertexAtCoordinate(_SurfaceA.GetEdge(spanA[i]).EndVertCoordinate);
							}

							if
							(
								LNX_Utils.PositionIsDistallyWithin
								(
									hit.Position, SpanAEndHit.Position, prevVrt.V_Position
								)
							)
							{
								return true;
							}
						}
						else //In this case, we can assume it's definitely on the span...
						{
							return true;
						}
					}
				}
			}
			else if (hit.SurfaceIndex == _SurfaceB.MySurfaceIndex)
			{
				for (int i = 0; i < spanB.Length; i++)
				{
					if (_SurfaceB.GetEdge(spanB[i]).HitTouchesEdge(hit))
					{
						if (i == 0)
						{
							LNX_Vertex nxtVrt = null;

							if
							(
								_SurfaceB.GetEdge(spanB[0]).StartPosition == _SurfaceB.GetEdge(spanB[1]).StartPosition ||
								_SurfaceB.GetEdge(spanB[0]).EndPosition == _SurfaceB.GetEdge(spanB[1]).StartPosition
							)
							{
								nxtVrt = _SurfaceB.GetVertexAtCoordinate(_SurfaceB.GetEdge(spanB[1]).StartVertCoordinate);
							}
							else if
							(
								_SurfaceB.GetEdge(spanB[0]).StartPosition == _SurfaceB.GetEdge(spanB[1]).EndPosition ||
								_SurfaceB.GetEdge(spanB[0]).EndPosition == _SurfaceB.GetEdge(spanB[1]).EndPosition
							)
							{
								nxtVrt = _SurfaceB.GetVertexAtCoordinate(_SurfaceB.GetEdge(spanB[1]).EndVertCoordinate);
							}

							if
							(
								LNX_Utils.PositionIsDistallyWithin
								(
									hit.Position, SpanBStartHit.Position, nxtVrt.V_Position
								)
							)
							{
								return true;
							}
						}
						else if (i == spanB.Length - 1)
						{
							LNX_Vertex prevVrt = null;

							if
							(
								_SurfaceB.GetEdge(spanB[i - 1]).StartPosition == _SurfaceB.GetEdge(spanB[i]).StartPosition ||
								_SurfaceB.GetEdge(spanB[i - 1]).EndPosition == _SurfaceB.GetEdge(spanB[i]).StartPosition
							)
							{
								prevVrt = _SurfaceB.GetVertexAtCoordinate(_SurfaceB.GetEdge(spanB[i]).StartVertCoordinate);
							}
							else if
							(
							_SurfaceB.GetEdge(spanB[i - 1]).StartPosition == _SurfaceB.GetEdge(spanB[i]).EndPosition ||
							_SurfaceB.GetEdge(spanB[i - 1]).EndPosition == _SurfaceB.GetEdge(spanB[i]).EndPosition
							)
							{
								prevVrt = _SurfaceB.GetVertexAtCoordinate(_SurfaceB.GetEdge(spanB[i]).EndVertCoordinate);
							}

							if
							(
								LNX_Utils.PositionIsDistallyWithin
								(
									hit.Position, SpanBEndHit.Position, prevVrt.V_Position
								)
							)
							{
								return true;
							}
						}
						else //In this case, we can assume it's definitely on the span...
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		public bool HitIsOnEitherSpan_dbg(LNX_NavmeshHit hit, ref LNX_MethodDebugReport rprt )
		{
			rprt.StartMethod($"HitIsOnEitherSpan_dbg('{hit}')");
			rprt.Log($"note, SurfaceA srface index: '{_SurfaceA.MySurfaceIndex}, Surfaceb: '{_SurfaceB.MySurfaceIndex}'");

			#region SHORT-CIRCUITING ===============================
			if (hit.SurfaceIndex != _SurfaceA.MySurfaceIndex &&
				hit.SurfaceIndex != _SurfaceB.MySurfaceIndex)
			{
				rprt.Log_And_End_Method($"ss 1");
				return false;
			}

			if (hit.EdgeIndex == -1 && hit.VertIndex == -1)
			{
				rprt.Log_And_End_Method($"ss 2");

				return false;
			}
			#endregion

			if ( hit.SurfaceIndex == _SurfaceA.MySurfaceIndex )
			{
				rprt.Log($"hit surface index is same as surfaceA index. Inspecting SpanA...");
				for (int i = 0; i < spanA.Length; i++ )
				{
					rprt.Log($"for{i} ('{spanA[i]}')=======================",
						$"edge: '{_SurfaceA.GetEdge(spanA[i])}'...");
					if ( _SurfaceA.GetEdge(spanA[i]).HitTouchesEdge(hit) )
					{
						rprt.Log($"hit touches this this edge. Investigating further...");
						if ( i == 0 )
						{
							rprt.Log($"element is on the beginning of span, so position needs special inspection...");
							LNX_Vertex nxtVrt = null;

							if
							(
								_SurfaceA.GetEdge(spanA[0]).StartPosition == _SurfaceA.GetEdge(spanA[1]).StartPosition ||
								_SurfaceA.GetEdge(spanA[0]).EndPosition == _SurfaceA.GetEdge(spanA[1]).StartPosition
							)
							{
								nxtVrt = _SurfaceA.GetVertexAtCoordinate(_SurfaceA.GetEdge(spanA[1]).StartVertCoordinate);
								rprt.Log($"a) set nxtVrt to: '{nxtVrt}'...");
							}
							else if
							(
								_SurfaceA.GetEdge(spanA[0]).StartPosition == _SurfaceA.GetEdge(spanA[1]).EndPosition ||
								_SurfaceA.GetEdge(spanA[0]).EndPosition == _SurfaceA.GetEdge(spanA[1]).EndPosition
							)
							{
								nxtVrt = _SurfaceA.GetVertexAtCoordinate(_SurfaceA.GetEdge(spanA[1]).EndVertCoordinate);
								rprt.Log($"b) set nxtVrt to: '{nxtVrt}'...");
							}

							rprt.Log($"checking if hit position is distally within spanAstartHit and next vert...");
							if
							(
								LNX_Utils.PositionIsDistallyWithin
								(
									hit.Position, SpanAStartHit.Position, nxtVrt.V_Position
								)
							)
							{
								rprt.Log_And_End_Method($"position IS distally within. Returning true...");
								return true;
							}
						}
						else if (i == spanA.Length - 1)
						{
							LNX_Vertex prevVrt = null;

							if
							(
								_SurfaceA.GetEdge(spanA[i - 1]).StartPosition == _SurfaceA.GetEdge(spanA[i]).StartPosition ||
								_SurfaceA.GetEdge(spanA[i - 1]).EndPosition == _SurfaceA.GetEdge(spanA[i]).StartPosition
							)
							{
								prevVrt = _SurfaceA.GetVertexAtCoordinate(_SurfaceA.GetEdge(spanA[i]).StartVertCoordinate);
							}
							else if
							(
							_SurfaceA.GetEdge(spanA[i - 1]).StartPosition == _SurfaceA.GetEdge(spanA[i]).EndPosition ||
							_SurfaceA.GetEdge(spanA[i - 1]).EndPosition == _SurfaceA.GetEdge(spanA[i]).EndPosition
							)
							{
								prevVrt = _SurfaceA.GetVertexAtCoordinate(_SurfaceA.GetEdge(spanA[i]).EndVertCoordinate);
							}

							rprt.Log($"checking if hit position is distally withinb spanAstartHit and next vert...");
							if
							(
								LNX_Utils.PositionIsDistallyWithin
								(
									hit.Position, SpanAEndHit.Position, prevVrt.V_Position
								)
							)
							{
								rprt.Log_And_End_Method($"decided hit was, indeed, distally within. Returning true...");
								return true;
							}
						}
						else //In this case, we can assume it's definitely on the span...
						{
							rprt.Log_And_End_Method($"Not on span ends, so can assume hit is definitely within the span. Returning true...");
							return true;
						}
					}
				}
			}
			else if (hit.SurfaceIndex == _SurfaceB.MySurfaceIndex )
			{
				rprt.Log($"hit surface index is same as surfaceB index. Inspecting SpanB...");
				for (int i = 0; i < spanB.Length; i++)
				{
					rprt.Log($"for{i}=======================",
						$"edge: '{_SurfaceB.GetEdge(spanA[i])}'..."); 
					if (_SurfaceB.GetEdge(spanB[i]).HitTouchesEdge(hit))
					{
						rprt.Log($"hit touches this this edge. Investigating further...");
						if (i == 0)
						{
							rprt.Log($"element is on the beginning of span, so position needs special inspection...");
							LNX_Vertex nxtVrt = null;

							if
							(
								_SurfaceB.GetEdge(spanB[0]).StartPosition == _SurfaceB.GetEdge(spanB[1]).StartPosition ||
								_SurfaceB.GetEdge(spanB[0]).EndPosition == _SurfaceB.GetEdge(spanB[1]).StartPosition
							)
							{
								nxtVrt = _SurfaceB.GetVertexAtCoordinate(_SurfaceB.GetEdge(spanB[1]).StartVertCoordinate);
								rprt.Log($"a) set nxtVrt to: '{nxtVrt}'...");
							}
							else if
							(
								_SurfaceB.GetEdge(spanB[0]).StartPosition == _SurfaceB.GetEdge(spanB[1]).EndPosition ||
								_SurfaceB.GetEdge(spanB[0]).EndPosition == _SurfaceB.GetEdge(spanB[1]).EndPosition
							)
							{
								nxtVrt = _SurfaceB.GetVertexAtCoordinate(_SurfaceB.GetEdge(spanB[1]).EndVertCoordinate);
								rprt.Log($"b) set nxtVrt to: '{nxtVrt}'...");
							}

							rprt.Log($"checking if hit position is distally within spanBstartHit and next vert...");
							if
							(
								LNX_Utils.PositionIsDistallyWithin
								(
									hit.Position, SpanBStartHit.Position, nxtVrt.V_Position
								)
							)
							{
								rprt.Log_And_End_Method($"position IS distally within. Returning true...");
								return true;
							}
						}
						else if (i == spanB.Length - 1)
						{
							LNX_Vertex prevVrt = null;

							if
							(
								_SurfaceB.GetEdge(spanB[i - 1]).StartPosition == _SurfaceB.GetEdge(spanB[i]).StartPosition ||
								_SurfaceB.GetEdge(spanB[i - 1]).EndPosition == _SurfaceB.GetEdge(spanB[i]).StartPosition
							)
							{
								prevVrt = _SurfaceB.GetVertexAtCoordinate(_SurfaceB.GetEdge(spanB[i]).StartVertCoordinate);
							}
							else if
							(
							_SurfaceB.GetEdge(spanB[i - 1]).StartPosition == _SurfaceB.GetEdge(spanB[i]).EndPosition ||
							_SurfaceB.GetEdge(spanB[i - 1]).EndPosition == _SurfaceB.GetEdge(spanB[i]).EndPosition
							)
							{
								prevVrt = _SurfaceB.GetVertexAtCoordinate(_SurfaceB.GetEdge(spanB[i]).EndVertCoordinate);
							}

							rprt.Log($"checking if hit position is distally withinb spanAstartHit and next vert...");
							if
							(
								LNX_Utils.PositionIsDistallyWithin
								(
									hit.Position, SpanBEndHit.Position, prevVrt.V_Position
								)
							)
							{
								rprt.Log_And_End_Method($"decided hit was, indeed, distally within. Returning true...");
								return true;
							}
						}
						else //In this case, we can assume it's definitely on the span...
						{
							rprt.Log_And_End_Method($"Not on span ends, so can assume hit is definitely within the span. Returning true...");
							return true;
						}
					}
				}
			}

			return false;
		}

		[ContextMenu("z call GenerateDbgText")]
		public void GenerateDbgText()
		{
			dbg_class = $"A: '{_SurfaceA}' --> B: '{_SurfaceB}'\n" +
			$"SpanA ({spanA.Length}) ==================================\n";
			for (int i = 0; i < spanA.Length; i++)
			{
				dbg_class += $"{spanA[i]}";
				if ( i > 10 )
				{
					break;
				}
				else if ( i < spanA.Length - 1 )
				{
					dbg_class += " > ";
				}
			}
			dbg_class += $"\nSpanB ({spanB.Length}) ==================================\n";
			for (int i = 0; i < spanB.Length; i++)
			{
				dbg_class += $"{spanB[i]}";
				if (i > 10)
				{
					break;
				}
				else if (i < spanB.Length - 1)
				{
					dbg_class += " > ";
				}
			}
			dbg_class += $"\n=========================================\n" +
				$"SpanAStartHit: '{SpanAStartHit}', SpanAEndHit: '{SpanAEndHit}'\n" +
				$"SpanBStartHit: '{SpanBStartHit}', SpanBEndHit: '{SpanBEndHit}'";
		}

		#region OPERATORS ====================================
		public override string ToString()
		{
			return $"[{name}_({_SurfaceA.MySurfaceIndex}_to_{_SurfaceB.MySurfaceIndex})]";
		}
		#endregion

#if UNITY_EDITOR
		public void DrawMyGizmos() //by putting this in a method instead of calling OnDrawGizmos here, I make it more likely that I won't forget to add a reference to this link in the manager
		{
			if ( drawVisualizationMesh && _VisualizationMesh != null && _VisualizationMesh.vertices != null &&
				_VisualizationMesh.vertices.Length > 0)
			{
				Gizmos.color = color_visualMesh;
				Gizmos.DrawMesh(_VisualizationMesh);
			}

			if (SetSpans && Selection.activeGameObject == gameObject )
			{
				Handles.Label(HandlePos_spanAStart + ((_SurfaceA.GetSurfaceProjectionVector() + Vector3.one) * 0.07f), "a1");
				Handles.Label(HandlePos_spanAEnd + ((_SurfaceA.GetSurfaceProjectionVector() + Vector3.one) * 0.07f), "a2");

				Handles.Label(HandlePos_spanBStart + (/*_SurfaceB.GetSurfaceProjectionVector()*/Vector3.one * 0.07f), "b1");
				Handles.Label(HandlePos_spanBEnd + (/*_SurfaceB.GetSurfaceProjectionVector()*/Vector3.one * 0.07f), "b2");
			}


		}
#endif
	}
}