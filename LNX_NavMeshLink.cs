using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
		[SerializeField, HideInInspector] private int myLinkIndex;
		public int MyLinkIndex => myLinkIndex;
		[SerializeField, HideInInspector] private Mesh _VisualizationMesh;
		[SerializeField, HideInInspector] private LNX_ComponentCoordinate[] spanA;
		[SerializeField, HideInInspector] private LNX_ComponentCoordinate[] spanB;
		//[SerializeField, HideInInspector] private LNX_NavmeshHit[] spanA;
		//[SerializeField, HideInInspector] private LNX_NavmeshHit[] spanB;

		//[Header("SELECTION")]
		public bool SetSpans = false;
		[HideInInspector] public Vector3 HandlePos_spanAStart; //todo: try putting these in editor class
		[HideInInspector] public Vector3 HandlePos_spanAEnd;
		[HideInInspector] public Vector3 HandlePos_spanBStart;
		[HideInInspector] public Vector3 HandlePos_spanBEnd;


		[Header("DEBUG")]
		[SerializeField] private bool drawVisualizationMesh;
		[SerializeField] private Color color_visualMesh;
		public LNX_MethodDebugReport MthdRprt;
		[SerializeField, TextArea(1,10)] private string dbg_class;

		#region HELPERS ========================================================
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
			Debug.Log($"{name}.RefreshSpans()");
			if ( _SurfaceA == null )
			{
				Debug.LogError($"LNX ERROR! _SurfaceA was null");
				return;
			}
			if (_SurfaceB == null)
			{
				Debug.LogError($"LNX ERROR! _SurfaceB was null");
				return;
			}

			string rprt = string.Empty;
			LNX_NavmeshHit oldSpnAStrtHit = SpanAStartHit;
			LNX_NavmeshHit oldSpnAEndHit = SpanAEndHit;
			LNX_NavmeshHit oldspnBStrtHit = SpanBStartHit;
			LNX_NavmeshHit oldSpnBEndHit = SpanBEndHit;

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

			Debug.Log($"startcoord: '{SpanBStartHit}', endcoord: '{SpanBEndHit}'");
			LNX_SelectionUtilities.ResolveTerminalSpan(
				_SurfaceB, SpanBStartHit.AsEdgeCoordinate(), SpanBEndHit.AsEdgeCoordinate(), out spanB
			);

			if ( SpanAStartHit != oldSpnAStrtHit )
			{
				rprt += $"changed spanA start to: '{SpanAStartHit}', ";
			}
			if (SpanAEndHit != oldSpnAEndHit )
			{
				rprt += $"changed spanA end to: '{SpanAEndHit}', ";
			}
			if (SpanBStartHit != oldspnBStrtHit)
			{
				rprt += $"changed SpanBStartHit to: '{SpanBStartHit}', ";
			}
			if (SpanBEndHit != oldSpnBEndHit)
			{
				rprt += $"changed SpanBEndHit to: '{SpanBEndHit}', ";
			}

			if (!string.IsNullOrEmpty(rprt)) 
			{
				Debug.Log(name + "\n" + rprt);
			}

			ReconstructVisualizationMesh();
			GenerateDbgText();
		}

		[ContextMenu("z call AddToManagerInScene()")]
		public void AddToManagerInScene()
		{
			LNX_Manager mgr = FindAnyObjectByType<LNX_Manager>();
			Debug.Log($"found: '{mgr}'");

			if ( !mgr.Links.Contains(this))
			{
				mgr.Links.Add( this );
				myLinkIndex = mgr.Links.IndexOf(this);
				Debug.Log($"myLInkINdex: '{myLinkIndex}'.");
			}
			else
			{
				Debug.LogWarning($"manager collection already contianed this link at index: '{mgr.Links.IndexOf(this)}'");
			}
		}

		public void SetLinkIndex(int indx)
		{
			Debug.Log($"{this}.SetLinkIndex({indx})");

			if (myLinkIndex != indx)
			{
				RefreshSpans();
			}

			myLinkIndex = indx;
		}
		#endregion

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
				//Debug.Log($"HandlePos_spanAStart changed...");
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
				//Debug.Log($"HandlePos_spanAEnd changed...");

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
				//Debug.Log($"HandlePos_spanBStart changed...");

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
				//Debug.Log($"HandlePos_spanBEnd changed. Getting best edge hit on surface...");

				moveChange = true;

				LNX_NavmeshHit oldHit = SpanBEndHit;
				SpanBEndHit = LNX_SelectionUtilities.GetBestEdgeHitOnSurface(_SurfaceB, newHndlPos_endB, true, myLinkIndex);

				//Debug.Log($"got: '{SpanBEndHit}'...");

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
				//Debug.Log($"recalculating spanA using '{SpanAStartHit.AsEdgeCoordinate()}' and '{SpanAEndHit.AsEdgeCoordinate()}'...");
				LNX_SelectionUtilities.ResolveTerminalSpan(
					_SurfaceA, SpanAStartHit.AsEdgeCoordinate(), SpanAEndHit.AsEdgeCoordinate(), out spanA
				);
			}
			if (spanBChange)
			{
				//Debug.Log($"recalculating spanB using '{SpanBStartHit.AsEdgeCoordinate()}' and '{SpanBEndHit.AsEdgeCoordinate()}'...");

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
				rprt.Log($"hit surface index is same as _SurfaceA.MySurfaceIndex ({_SurfaceA.name})...",
					"Checking adjacency depth from this hit to spanA...");

				int rslt = _SurfaceA.GetAdjacencyDepthToTriangle_dbg(hit.TriangleIndex, spanA[0].TriangleIndex, ref rprt);

				rprt.Log_And_End_Method($"returning {rslt} > 0...");
				return rslt > -1;
			}
			else if (hit.SurfaceIndex == _SurfaceB.MySurfaceIndex)
			{
				rprt.Log($"hit surface index is same as _SurfaceB.MySurfaceIndex ({_SurfaceB.name})...",
					"Checking adjacency depth from this hit to spanB...");

				int rslt = _SurfaceB.GetAdjacencyDepthToTriangle_dbg(hit.TriangleIndex, spanB[0].TriangleIndex, ref rprt);

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


		public bool HitIsOnSpanA( LNX_NavmeshHit hit )
		{
			#region SHORT-CIRCUITING ===============================
			if ( hit.SurfaceIndex != _SurfaceA.MySurfaceIndex )
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
				if (spanA.Length == 1) //as I recently found out, this needs to be handled differently than when there are multiple edges in a span...
				{
					if
					(
						LNX_Utils.PositionIsDistallyWithin
						(
							hit.Position, SpanAStartHit.Position, SpanAEndHit.Position
						)
					)
					{
						return true;
					}
				}

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

			return false;
		}

		public bool HitIsOnSpanB(LNX_NavmeshHit hit)
		{
			#region SHORT-CIRCUITING ===============================
			if (hit.SurfaceIndex != _SurfaceB.MySurfaceIndex)
			{
				return false;
			}

			if (hit.EdgeIndex == -1 && hit.VertIndex == -1)
			{
				return false;
			}
			#endregion

			if (hit.SurfaceIndex == _SurfaceB.MySurfaceIndex)
			{
				if (spanB.Length == 1) //as I recently found out, this needs to be handled differently than when there are multiple edges in a span...
				{
					if
					(
						LNX_Utils.PositionIsDistallyWithin
						(
							hit.Position, SpanBStartHit.Position, SpanBEndHit.Position
						)
					)
					{
						return true;
					}
				}
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

		public bool HitIsOnEitherSpan(LNX_NavmeshHit hit)
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
				if (spanA.Length == 1) //as I recently found out, this needs to be handled differently than when there are multiple edges in a span...
				{
					if
					(
						LNX_Utils.PositionIsDistallyWithin
						(
							hit.Position, SpanAStartHit.Position, SpanAEndHit.Position
						)
					)
					{
						return true;
					}
				}

				for (int i = 0; i < spanA.Length; i++)
				{
					if (_SurfaceA.GetEdge(spanA[i]).HitTouchesEdge(hit))
					{
						if (i == 0)
						{
							LNX_Vertex nxtVrtInSpan = null;

							if
							(
								_SurfaceA.GetEdge(spanA[0]).StartPosition == _SurfaceA.GetEdge(spanA[1]).StartPosition ||
								_SurfaceA.GetEdge(spanA[0]).EndPosition == _SurfaceA.GetEdge(spanA[1]).StartPosition
							)
							{
								nxtVrtInSpan = _SurfaceA.GetVertexAtCoordinate(_SurfaceA.GetEdge(spanA[1]).StartVertCoordinate);
							}
							else if
							(
								_SurfaceA.GetEdge(spanA[0]).StartPosition == _SurfaceA.GetEdge(spanA[1]).EndPosition ||
								_SurfaceA.GetEdge(spanA[0]).EndPosition == _SurfaceA.GetEdge(spanA[1]).EndPosition
							)
							{
								nxtVrtInSpan = _SurfaceA.GetVertexAtCoordinate(_SurfaceA.GetEdge(spanA[1]).EndVertCoordinate);
							}

							if
							(
								LNX_Utils.PositionIsDistallyWithin
								(
									hit.Position, SpanAStartHit.Position, nxtVrtInSpan.V_Position
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
				if (spanB.Length == 1) //as I recently found out, this needs to be handled differently than when there are multiple edges in a span...
				{
					if
					(
						LNX_Utils.PositionIsDistallyWithin
						(
							hit.Position, SpanBStartHit.Position, SpanBEndHit.Position
						)
					)
					{
						return true;
					}
				}
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
			rprt.Log($"note, SurfaceA srface index: '{_SurfaceA.MySurfaceIndex}, SurfaceB: '{_SurfaceB.MySurfaceIndex}'");

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
				rprt.Log($"hit surface index is same as surfaceA index. Inspecting '{spanA.Length}' edges on SpanA...");

				if ( spanA.Length == 1 ) //as I recently found out, this needs to be handled differently than when there are multiple edges in a span...
				{
					rprt.Log($"checking if hit position is distally withinb spanAstartHit and SpanAEndHit...");
					if
					(
						LNX_Utils.PositionIsDistallyWithin
						(
							hit.Position, SpanAStartHit.Position, SpanAEndHit.Position
						)
					)
					{
						rprt.Log_And_End_Method($"decided hit was, indeed, distally within. Returning true...");
						return true;
					}
				}

				for (int i = 0; i < spanA.Length; i++ )
				{
					rprt.Log($"for{i} ('{spanA[i]}')=======================",
						$"(using edge: '{_SurfaceA.GetEdge(spanA[i])}')...");
					if ( _SurfaceA.GetEdge(spanA[i]).HitTouchesEdge(hit) )
					{
						rprt.Log($"hit touches this this edge. Investigating further...");
						if ( i == 0 )
						{
							rprt.Log($"element is on the beginning edge of span, so position needs special inspection...");
							LNX_Vertex nxtVrtInSpan = null;

							if
							(
								_SurfaceA.GetEdge(spanA[0]).StartPosition == _SurfaceA.GetEdge(spanA[1]).StartPosition || 
								_SurfaceA.GetEdge(spanA[0]).EndPosition == _SurfaceA.GetEdge(spanA[1]).StartPosition
							)
							{
								nxtVrtInSpan = _SurfaceA.GetVertexAtCoordinate(_SurfaceA.GetEdge(spanA[1]).StartVertCoordinate);
								rprt.Log($"a) set nxtVrtInSpan to: '{nxtVrtInSpan}'...");
							}
							else if
							(
								_SurfaceA.GetEdge(spanA[0]).StartPosition == _SurfaceA.GetEdge(spanA[1]).EndPosition ||
								_SurfaceA.GetEdge(spanA[0]).EndPosition == _SurfaceA.GetEdge(spanA[1]).EndPosition
							)
							{
								nxtVrtInSpan = _SurfaceA.GetVertexAtCoordinate(_SurfaceA.GetEdge(spanA[1]).EndVertCoordinate);
								rprt.Log($"b) set nxtVrtInSpan to: '{nxtVrtInSpan}'...");
							}

							rprt.Log($"checking if hit position is distally within spanAstartHit and next vert...");
							if
							(
								LNX_Utils.PositionIsDistallyWithin
								(
									hit.Position, SpanAStartHit.Position, nxtVrtInSpan.V_Position
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
				rprt.Log($"hit surface index is same as surfaceB index. Inspecting '{spanB.Length}' edges on SpanB...");

				if (spanB.Length == 1) //as I recently found out, this needs to be handled differently than when there are multiple edges in a span...
				{
					rprt.Log($"checking if hit position is distally within SpanBStartHit and SpanAEndHit...");
					if
					(
						LNX_Utils.PositionIsDistallyWithin
						(
							hit.Position, SpanBStartHit.Position, SpanBEndHit.Position
						)
					)
					{
						rprt.Log_And_End_Method($"decided hit was, indeed, distally within. Returning true...");
						return true;
					}
				}
				for (int i = 0; i < spanB.Length; i++)
				{
					rprt.Log($"for{i}=======================",
						$"(using edge: '{_SurfaceB.GetEdge(spanA[i])}')..."); 
					if (_SurfaceB.GetEdge(spanB[i]).HitTouchesEdge(hit))
					{
						rprt.Log($"hit touches this this edge. Investigating further...");
						if (i == 0)
						{
							rprt.Log($"element is on the beginning edge of span, so position needs special inspection...");
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

			rprt.Log_And_End_Method($"finally returning false...");
			return false;
		}

		public bool ProjectFromSurfaceAToSpanB(LNX_NavmeshHit projectionOrigin, Vector3 projectionDirection, 
			out LNX_NavmeshHit projectedHit)
		{
			projectedHit = LNX_NavmeshHit.None;

			#region SHORT-CIRCUITING =================================
			if (projectionOrigin.SurfaceIndex != _SurfaceA.MySurfaceIndex)
			{
				return false;
			}

			Vector3 origin = _SurfaceA.FlatVector(projectionOrigin.Position);
			projectionDirection = _SurfaceA.FlatVector(projectionDirection).normalized;

			if (!ProjectionClearsSpans(projectionOrigin, projectionDirection))
			{
				return false;
			}

			#endregion

			for (int i = 0; i < spanB.Length; i++)
			{
				Vector3 intersectPos = Vector3.zero;

				if (_SurfaceB.GetEdge(spanB[i]).FlatProject(origin, projectionDirection, _SurfaceA.SurfaceOrientation, out intersectPos))
				{
					Vector3 v_nxtPrjct = LNX_Utils.GetRotationBetweenDirections(_SurfaceA.SurfaceOrientation, _SurfaceB.SurfaceOrientation) * projectionDirection;
					Vector3 nxtOrigin = _SurfaceB.FlatVector(intersectPos);
					Vector3 nxtIntersectPos = Vector3.zero;

					if (!_SurfaceB.GetEdge(spanB[i]).FlatProject(nxtOrigin, v_nxtPrjct, out nxtIntersectPos, true))
					{
						projectedHit = new LNX_NavmeshHit(
							_SurfaceB.GetEdge(spanB[i]),
							intersectPos + /*(nxtIntersectPos)*/new Vector3(nxtIntersectPos.x * v_nxtPrjct.x, nxtIntersectPos.y * v_nxtPrjct.y, nxtIntersectPos.z * v_nxtPrjct.z), //todo: what do we do right here?
							_SurfaceB.Triangles[spanB[i].TriangleIndex].V_PathingNormal
						);

						return true;
					}
					else
					{
						Debug.LogError($"LNX ERROR! Something went wrong projecting onto surfaceB");
						return false;
					}
				}
			}

			return false;
		}
		public bool ProjectFromSurfaceAToSpanB_dbg(LNX_NavmeshHit projectionOrigin, Vector3 projectionDirection,
	out LNX_NavmeshHit projectedHit, ref LNX_MethodDebugReport rprt )
		{
			rprt.StartMethod($"ProjectFromSurfaceAToSpanB_dbg(projectionOrigin: '{projectionOrigin}', " +
				$"projectionDirection: '{projectionDirection}')");

			rprt.Log($"_SurfaceA orientation '{_SurfaceA.SurfaceOrientation}'",
				$"_surfaceB orientation: '{_SurfaceB.SurfaceOrientation}'");
			projectedHit = LNX_NavmeshHit.None;

			#region SHORT-CIRCUITING =================================
			if (projectionOrigin.SurfaceIndex != _SurfaceA.MySurfaceIndex)
			{
				rprt.Log_And_End_Method($"SS1: projectionOrigin.SurfaceIndex != _SurfaceA.MySurfaceIndex. Returning false...");
				return false;
			}

			Vector3 origin = _SurfaceA.FlatVector(projectionOrigin.Position);
			projectionDirection = _SurfaceA.FlatVector(projectionDirection).normalized;

			rprt.Log($"Created values...",
				$"origin: '{origin}'",
				$"projectionDirection: '{LNX_UnitTestUtilities.LongVectorString(projectionDirection)}'");

			rprt.Log($"checking if projection clears spans...");
			if ( !ProjectionClearsSpans(projectionOrigin, projectionDirection) )
			{
				rprt.Log_And_End_Method($"SS2: ProjectionClearsSpans fail. Returning false...");

				return false;
			}
			else
			{
				rprt.Log($"projection DOES clea spans. Continuing method...");
			}
			#endregion

			rprt.EmptyLine();
			rprt.Log($"for-looping through spanB with '{spanB.Length}' values...");
			for ( int i = 0; i < spanB.Length; i++ )
			{
				rprt.Log($"for{i}======================================",
					$"spanB[{i}]: '{spanB[i]}'...",
					$"using edge: '{_SurfaceB.GetEdge(spanB[i])}, cachedSrfcOrient: '{_SurfaceB.GetEdge(spanB[i]).V_SurfaceProjectionDirection_cached}'...",
					$"now flat projecting direction: '{projectionDirection}' against this span edge flattened to surface A...");

				Vector3 intersectPos = Vector3.zero;

				if ( _SurfaceB.GetEdge(spanB[i]).FlatProject(origin, projectionDirection, _SurfaceA.SurfaceOrientation, out intersectPos) )
				{
					rprt.Log($"FlatProject succesful. got intersectPos: '{intersectPos}'...");
					Debug.DrawLine(intersectPos, intersectPos + (Vector3.up * 1f), Color.magenta, 2f);
					Vector3 v_nxtPrjctDir = LNX_Utils.GetRotationBetweenDirections(_SurfaceA.SurfaceOrientation, _SurfaceB.SurfaceOrientation) * projectionDirection;

					Vector3 nxtOrigin = _SurfaceB.FlatVector(intersectPos);
					Vector3 nxtIntersectPos = Vector3.zero;
					rprt.Log($"v_nxtPrjctDir set to: '{LNX_UnitTestUtilities.LongVectorString(v_nxtPrjctDir)}'...",
						$"nxtOrigin updated to: '{nxtOrigin}'...");

					rprt.Log($"now projecting against this edge flattened to surfaceB...");

					//if (!_SurfaceB.GetEdge(spanB[i]).FlatProject(nxtOrigin, v_nxtPrjct, out nxtIntersectPos, true))
					if ( _SurfaceB.GetEdge(spanB[i]).FlatProject_dbg(nxtOrigin, v_nxtPrjctDir, out nxtIntersectPos, true, ref rprt) )
					{
						rprt.Log($"Flat project succesful. got intersectpos: '{nxtIntersectPos}'...");
						projectedHit = new LNX_NavmeshHit(
							_SurfaceB.GetEdge(spanB[i]),
							intersectPos + /*(nxtIntersectPos)*/new Vector3(nxtIntersectPos.x * v_nxtPrjctDir.x, nxtIntersectPos.y * v_nxtPrjctDir.y, nxtIntersectPos.z * v_nxtPrjctDir.z), //todo: what do we do right here?
							_SurfaceB.Triangles[spanB[i].TriangleIndex].V_PathingNormal
						);
						rprt.Log_And_End_Method($"Created projectedHit: '{projectedHit}'. Returning true...");
						return true;
					}
					else
					{
						Debug.LogError($"LNX ERROR! Something went wrong projecting onto surfaceB");
						rprt.Log_And_End_Method($"LNX ERROR! Something went wrong projecting onto surfaceB");
						return false;
					}
				}
				else
				{
					rprt.Log($"flat project was unsuccesful...");
				}
			}

			rprt.EmptyLine();
			rprt.Log_And_End_Method($"finally, returning false. projectedHit: '{projectedHit}'...");
			return false;
		}
		public bool ProjectFromSurfaceBToSpanA(LNX_NavmeshHit projectionOrigin, Vector3 projectionDirection, out LNX_NavmeshHit projectedHit )
		{
			projectedHit = LNX_NavmeshHit.None;

			#region SHORT-CIRCUITING =================================
			if (projectionOrigin.SurfaceIndex != _SurfaceB.MySurfaceIndex)
			{
				return false;
			}

			Vector3 origin = _SurfaceB.FlatVector(projectionOrigin.Position);
			projectionDirection = _SurfaceB.FlatVector(projectionDirection).normalized;

			if (!ProjectionClearsSpans(projectionOrigin, projectionDirection))
			{
				return false;
			}
			#endregion

			for (int i = 0; i < spanA.Length; i++)
			{
				Vector3 intersectPos = Vector3.zero;

				if (_SurfaceA.GetEdge(spanA[i]).FlatProject(origin, projectionDirection, _SurfaceB.SurfaceOrientation, out intersectPos))
				{
					Vector3 v_nxtPrjct = GetBtoACardinalDirection(projectionDirection);
					Vector3 nxtOrigin = _SurfaceA.FlatVector(intersectPos);
					Vector3 nxtIntersectPos = Vector3.zero;

					if (!_SurfaceA.GetEdge(spanA[i]).FlatProject(nxtOrigin, v_nxtPrjct, out nxtIntersectPos, true))
					{
						Debug.LogError($"LNX ERROR! Something went wrong projecting onto spanA");
						return false;
					}
					else
					{
						projectedHit = new LNX_NavmeshHit(
							_SurfaceA.GetEdge(spanA[i]),
							intersectPos + /*(nxtIntersectPos)*/new Vector3(nxtIntersectPos.x * v_nxtPrjct.x, nxtIntersectPos.y * v_nxtPrjct.y, nxtIntersectPos.z * v_nxtPrjct.z), //todo: what do we do right here?
							_SurfaceA.Triangles[spanA[i].TriangleIndex].V_PathingNormal
						);
						return true;
					}
				}
			}

			return false;
		}
		public bool ProjectFromSurfaceBToSpanA_dbg(LNX_NavmeshHit projectionOrigin, Vector3 projectionDirection,
			out LNX_NavmeshHit projectedHit, ref LNX_MethodDebugReport rprt)
		{
			rprt.StartMethod($"ProjectFromSurfaceAToSpanB_dbg(projectionOrigin: '{projectionOrigin}', " +
				$"projectionDirection: '{projectionDirection}')");

			rprt.Log($"_SurfaceB orientation '{_SurfaceB.SurfaceOrientation}'",
				$"_surfaceA orientation: '{_SurfaceA.SurfaceOrientation}'");
			projectedHit = LNX_NavmeshHit.None;

			#region SHORT-CIRCUITING =================================
			if (projectionOrigin.SurfaceIndex != _SurfaceB.MySurfaceIndex)
			{
				rprt.Log_And_End_Method($"SS1: projectionOrigin.SurfaceIndex != _SurfaceB.MySurfaceIndex. Returning false...");
				return false;
			}

			Vector3 origin = _SurfaceB.FlatVector(projectionOrigin.Position);
			projectionDirection = _SurfaceB.FlatVector(projectionDirection).normalized;

			rprt.Log($"Created values...",
				$"origin: '{origin}'",
				$"projectionDirection: '{projectionDirection}'");

			rprt.Log($"checking if projection clears spans...");
			if( !ProjectionClearsSpans(projectionOrigin, projectionDirection))
			{
				rprt.Log_And_End_Method($"SS2: ProjectionClearsSpans fail. Returning false...");

				return false;
			}
			#endregion

			rprt.EmptyLine();
			rprt.Log($"for-looping through spanA with '{spanA.Length}' values...");
			for (int i = 0; i < spanA.Length; i++)
			{
				rprt.Log($"for{i}======================================",
					$"spanA[{i}]: '{spanA[i]}'...",
					$"using edge: '{_SurfaceA.GetEdge(spanA[i])}, cachedSrfcOrient: '{_SurfaceA.GetEdge(spanA[i]).V_SurfaceProjectionDirection_cached}'...",
					"now flat projecting against this edge flattened to surface B...");

				Vector3 intersectPos = Vector3.zero;

				if (_SurfaceA.GetEdge(spanA[i]).FlatProject(origin, projectionDirection, _SurfaceB.SurfaceOrientation, out intersectPos))
				{
					rprt.Log($"FlatProject true. intersectPos: '{intersectPos}'...");
					//Debug.DrawLine(intersectPos, intersectPos + (Vector3.up * 1f), Color.magenta, 2f);
					Vector3 v_nxtPrjct = LNX_Utils.GetRotationBetweenDirections(_SurfaceB.SurfaceOrientation, _SurfaceA.SurfaceOrientation) * projectionDirection;
					Vector3 nxtOrigin = _SurfaceA.FlatVector(intersectPos);
					Vector3 nxtIntersectPos = Vector3.zero;

					rprt.Log($"now projecting against this edge flattened to surfaceA...");

					//if (!_SurfaceB.GetEdge(spanB[i]).FlatProject(nxtOrigin, v_nxtPrjct, out nxtIntersectPos, true))
					if (!_SurfaceA.GetEdge(spanA[i]).FlatProject_dbg(nxtOrigin, v_nxtPrjct, out nxtIntersectPos, true, ref rprt))

					{
						Debug.LogError($"LNX ERROR! Something went wrong projecting onto spanA");
						rprt.Log_And_End_Method($"LNX ERROR! Something went wrong projecting onto spanB");
						return false;
					}
					else
					{
						rprt.Log($"Flat project succesful. got intersectpos: '{nxtIntersectPos}'...");
						projectedHit = new LNX_NavmeshHit(
							_SurfaceA.GetEdge(spanA[i]),
							intersectPos + /*(nxtIntersectPos)*/new Vector3(nxtIntersectPos.x * v_nxtPrjct.x, nxtIntersectPos.y * v_nxtPrjct.y, nxtIntersectPos.z * v_nxtPrjct.z), //todo: what do we do right here?
							_SurfaceA.Triangles[spanA[i].TriangleIndex].V_PathingNormal
						);
						rprt.Log_And_End_Method($"Created projectedHit: '{projectedHit}'. Returning true...");
						return true;
					}
				}
				else
				{
					rprt.Log($"flat project was unsuccesful...");
				}
			}

			rprt.EmptyLine();
			rprt.Log_And_End_Method($"finally, returning false. projectedHit: '{projectedHit}'...");
			return false;
		}
		private Vector3 GetAtoBCardinalDirection(Vector3 originalProjection)
		{
			if (_SurfaceA.SurfaceOrientation == _SurfaceB.SurfaceOrientation)
			{
				return originalProjection;
			}

			if (_SurfaceA.SurfaceOrientation == LNX_Direction.PositiveY)
			{
				if ( SpanBStartHit.Position.y > SpanAStartHit.Position.y )
				{
					return Vector3.up;
				}
				else
				{
					return Vector3.down;
				}
			}
			else if (_SurfaceA.SurfaceOrientation == LNX_Direction.NegativeY)
			{
				if ( SpanBStartHit.Position.y < SpanAStartHit.Position.y )
				{
					return Vector3.down;
				}
				else
				{
					return Vector3.up;
				}
			}

			if (_SurfaceA.SurfaceOrientation == LNX_Direction.PositiveX)
			{
				if ( SpanBStartHit.Position.x > SpanAStartHit.Position.x)
				{
					return Vector3.right;
				}
				else
				{
					return Vector3.left;
				}
			}
			if (_SurfaceA.SurfaceOrientation == LNX_Direction.NegativeX)
			{
				if ( SpanBStartHit.Position.x < SpanAStartHit.Position.x )
				{
					return Vector3.left;
				}
				else
				{
					return Vector3.right;
				}
			}

			if (_SurfaceA.SurfaceOrientation == LNX_Direction.PositiveZ)
			{
				if ( SpanBStartHit.Position.z > SpanAStartHit.Position.z )
				{
					return Vector3.forward;
				}
				else
				{
					return Vector3.back;
				}
			}
			if ( _SurfaceA.SurfaceOrientation == LNX_Direction.NegativeZ )
			{
				if ( SpanBStartHit.Position.z < SpanAStartHit.Position.z)
				{
					return Vector3.back;
				}
				else
				{
					return Vector3.forward;
				}
			}

			Debug.LogError($"error. Shouldn't have gotten here");
			return Vector3.zero;
		}
		private Vector3 GetBtoACardinalDirection(Vector3 originalProjection)
		{
			if ( _SurfaceB.SurfaceOrientation == _SurfaceA.SurfaceOrientation )
			{
				return originalProjection;
			}

			if ( _SurfaceB.SurfaceOrientation == LNX_Direction.PositiveY )
			{
				if ( SpanAStartHit.Position.y > SpanBStartHit.Position.y )
				{
					return Vector3.up;
				}
				else
				{
					return Vector3.down;
				}
			}
			else if ( _SurfaceB.SurfaceOrientation == LNX_Direction.NegativeY )
			{
				if ( SpanAStartHit.Position.y < SpanBStartHit.Position.y)
				{
					return Vector3.down;
				}
				else
				{
					return Vector3.up;
				}
			}
			
			if ( _SurfaceB.SurfaceOrientation == LNX_Direction.PositiveX )
			{
				if ( SpanAStartHit.Position.x > SpanBStartHit.Position.x )
				{
					return Vector3.right;
				}
				else
				{
					return Vector3.left;
				}
			}
			if ( _SurfaceB.SurfaceOrientation == LNX_Direction.NegativeX )
			{
				if ( SpanAStartHit.Position.x < SpanBStartHit.Position.x )
				{
					return Vector3.left;
				}
				else
				{
					return Vector3.right;
				}
			}

			if ( _SurfaceB.SurfaceOrientation == LNX_Direction.PositiveZ )
			{
				if ( SpanAStartHit.Position.z > SpanBStartHit.Position.z)
				{
					return Vector3.forward;
				}
				else
				{
					return Vector3.back;
				}
			}
			if (_SurfaceB.SurfaceOrientation == LNX_Direction.NegativeZ )
			{
				if ( SpanAStartHit.Position.z < SpanBStartHit.Position.z )
				{
					return Vector3.back;
				}
				else
				{
					return Vector3.forward;
				}
			}

			Debug.LogError($"error. Shouldn't have gotten here");
			return Vector3.zero;
		}

		public bool ProjectionClearsSpans( LNX_NavmeshHit startHit, Vector3 projectionDirection )
		{
			if ( startHit.SurfaceIndex == _SurfaceA.MySurfaceIndex )
			{
				Vector3 origin = _SurfaceA.FlatVector( startHit.Position );
				projectionDirection = _SurfaceA.FlatVector( projectionDirection ).normalized;

				if
				(
					!LNX_Utils.AmInVectorCone
					(
						projectionDirection,
						_SurfaceA.FlatVector(SpanAStartHit.Position - origin).normalized,
						_SurfaceA.FlatVector(SpanAEndHit.Position - origin).normalized, 
						_SurfaceA.GetSurfaceProjectionVector(), true
					)
				)
				{
					return false;
				}

				if
				(
					!LNX_Utils.AmInVectorCone
					(
						projectionDirection,
						_SurfaceA.FlatVector(SpanBStartHit.Position - origin).normalized,
						_SurfaceA.FlatVector(SpanBEndHit.Position - origin).normalized, 
						_SurfaceA.GetSurfaceProjectionVector(), true
					)
				)
				{
					return false;
				}
			}
			else if (startHit.SurfaceIndex == _SurfaceB.MySurfaceIndex)
			{
				Vector3 origin = _SurfaceB.FlatVector(startHit.Position);
				projectionDirection = _SurfaceB.FlatVector(projectionDirection).normalized;

				if
				(
					!LNX_Utils.AmInVectorCone
					(
						projectionDirection,
						_SurfaceB.FlatVector(SpanAStartHit.Position - origin).normalized,
						_SurfaceB.FlatVector(SpanAEndHit.Position - origin).normalized,
						_SurfaceB.GetSurfaceProjectionVector(), true
					)
				)
				{
					return false;
				}

				if
				(
					!LNX_Utils.AmInVectorCone
					(
						projectionDirection,
						_SurfaceB.FlatVector(SpanBStartHit.Position - origin).normalized,
						_SurfaceB.FlatVector(SpanBEndHit.Position - origin).normalized,
						_SurfaceB.GetSurfaceProjectionVector(), true
					)
				)
				{
					return false;
				}
			}
			else
			{
				Debug.LogError($"LNX ERROR! startHit: '{startHit}' was on a surface not belonging to this link. Returning ");
				return false;
			}

			return true;
		}
		public bool ProjectionClearsSpans_dbg(LNX_NavmeshHit startHit, Vector3 projectionDirection, ref LNX_MethodDebugReport rprt)
		{
			rprt.StartMethod($"ProjectionClearsSpans_dbg(startHit: '{startHit}', projectionDirection: '{projectionDirection}')");
			
			if (startHit.SurfaceIndex == _SurfaceA.MySurfaceIndex)
			{
				rprt.Log($"startHit is on _surfaceA...");
				Vector3 origin = _SurfaceA.FlatVector(startHit.Position);
				projectionDirection = _SurfaceA.FlatVector(projectionDirection).normalized;

				rprt.Log($"running AmInVectorCone test against SpanAStartHit: '{SpanAStartHit}', and SpanAEndHit: '{SpanAEndHit}'...");
				if
				(
					!LNX_Utils.AmInVectorCone
					(
						projectionDirection,
						_SurfaceA.FlatVector(SpanAStartHit.Position - origin).normalized,
						_SurfaceA.FlatVector(SpanAEndHit.Position - origin).normalized,
						_SurfaceA.GetSurfaceProjectionVector(), true
					)
				)
				{
					rprt.Log_And_End_Method($"failed first AmInVectorCone() check. Returning false...");
					return false;
				}
				else
				{
					rprt.Log($"passed first AmInVectorCone() check...");
				}

				rprt.Log($"running AmInVectorCone test against SpanBStartHit: '{SpanBStartHit}', and SpanBEndHit: '{SpanBEndHit}'...");
				if
				(
					!LNX_Utils.AmInVectorCone_dbg
					(
						projectionDirection,
						_SurfaceA.FlatVector(SpanBStartHit.Position - origin).normalized,
						_SurfaceA.FlatVector(SpanBEndHit.Position - origin).normalized,
						_SurfaceA.GetSurfaceProjectionVector(), 
						ref rprt, true
					)
				)
				{
					rprt.Log_And_End_Method($"failed second AmInVectorCone() check. Returning false...");
					return false;
				}
				else
				{
					rprt.Log($"passed second AmInVectorCone() check...");
				}
			}
			else if (startHit.SurfaceIndex == _SurfaceB.MySurfaceIndex)
			{
				rprt.Log($"startHit is on _surfaceB...");

				Vector3 origin = _SurfaceB.FlatVector(startHit.Position);
				projectionDirection = _SurfaceB.FlatVector(projectionDirection).normalized;

				rprt.Log($"running AmInVectorCone test against SpanAStartHit: '{SpanAStartHit}', and SpanAEndHit: '{SpanAEndHit}'...");
				if
				(
					!LNX_Utils.AmInVectorCone
					(
						projectionDirection,
						_SurfaceB.FlatVector(SpanAStartHit.Position - origin).normalized,
						_SurfaceB.FlatVector(SpanAEndHit.Position - origin).normalized,
						_SurfaceB.GetSurfaceProjectionVector(), true
					)
				)
				{
					rprt.Log_And_End_Method($"failed first AmInVectorCone() check. Returning false...");
					return false;
				}
				else
				{
					rprt.Log($"passed first AmInVectorCone() check...");
				}

				rprt.Log($"running AmInVectorCone test against SpanBStartHit: '{SpanBStartHit}', and SpanBEndHit: '{SpanBEndHit}'...");
				if
				(
					!LNX_Utils.AmInVectorCone
					(
						projectionDirection,
						_SurfaceB.FlatVector(SpanBStartHit.Position - origin).normalized,
						_SurfaceB.FlatVector(SpanBEndHit.Position - origin).normalized,
						_SurfaceB.GetSurfaceProjectionVector(), true
					)
				)
				{
					rprt.Log_And_End_Method($"failed second AmInVectorCone() check. Returning false...");
					return false;
				}
				else
				{
					rprt.Log($"passed second AmInVectorCone() check...");
				}
			}
			else
			{
				Debug.LogError($"LNX ERROR! startHit: '{startHit}' was on a surface not belonging to this link. Returning ");
				rprt.Log_And_End_Method($"LNX ERROR! startHit: '{startHit}' was on a surface not belonging to this link. Returning ");

				return false;
			}

			rprt.Log_And_End_Method($"finally returning true...");
			return true;
		}
		[ContextMenu("z call GenerateDbgText")]
		public void GenerateDbgText()
		{
			dbg_class = $"myLinkIndex: '{myLinkIndex}'\n" +
				$"A: '{_SurfaceA}' --> B: '{_SurfaceB}'\n" +
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