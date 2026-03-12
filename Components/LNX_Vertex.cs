using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_Vertex
	{
		#region IDENTITY/LOCATING ============================================================
		/// <summary>Current position of this vertex in 3d space. Potentially modified after initial
		/// construction of the tri this vertex belongs to.</summary>
		public Vector3 V_Position;
		public Vector3 V_flattenedPosition => LNX_Utils.FlatVector( V_Position, v_surfaceNormal_cached );

		[SerializeField, HideInInspector] private Vector3 originalPosition;
		/// <summary>Initial position, in 3d space, of this vertex upon creation of it's owning triangle, 
		/// before any modifications </summary>
		public Vector3 OriginalPosition => originalPosition;

		public LNX_ComponentCoordinate MyCoordinate;
		public int TriangleIndex => MyCoordinate.TrianglesIndex;
		public int ComponentIndex => MyCoordinate.ComponentIndex;

		/// <summary>Index corresponding to the visualization mesh's triangles array that this vertex 
		/// corresponds to.</summary>
		public int Index_VisMesh_triangles
		{
			get
			{
				return (MyCoordinate.TrianglesIndex * 3) + MyCoordinate.ComponentIndex;
			}
		}

		/// <summary>Index corresponding to the visualization mesh's vertices array that this vertex 
		/// corresponds to.</summary>
		public int Index_VisMesh_Vertices = -1;
		#endregion--------------------------------------------------------

		//[Header("CALCULATED/DERIVED")] //---------------------------------------------------------------
		/// <summary>Aangle at the inner corner of the triangle at this vertex.</summary>
		public float AngleAtBend => Vector3.Angle(V_ToFirstSiblingVert.normalized, V_ToSecondSiblingVert.normalized); //~~

		/// <summary>Aangle at the inner corner of the triangle at this vertex assuming all verts are flattneed.</summary>
		public float AngleAtBend_flattened
		{
			get
			{
				return Vector3.Angle(
					LNX_Utils.FlatVector(V_ToFirstSiblingVert.normalized, v_surfaceNormal_cached), //~~
					LNX_Utils.FlatVector(V_ToSecondSiblingVert.normalized, v_surfaceNormal_cached) //~~
				);
			}
		}

		/// <summary>
		/// Signed angle going from V_ToFirstSiblingVert to V_ToSecondSiblingVert. You can use -SignedAngle (negative) to 
		/// get the signed angle from V_ToSecondSiblingVert to V_ToFirstSiblingVert.
		/// </summary>
		public float SignedAngle => Vector3.SignedAngle( V_ToFirstSiblingVert_flat, V_ToSecondSiblingVert_flat, v_surfaceNormal_cached );

		/// <summary>Cached center vector for the owning triangle. This is for exposed property calculation </summary>
		[SerializeField, HideInInspector] private Vector3 v_triCenter_cached;

		/// <summary>Normalized directional vector pointing from this vertex to the center of it's triangle </summary>
		[HideInInspector] public Vector3 v_toCenter => Vector3.Normalize( v_triCenter_cached - V_Position );

		[HideInInspector] public float DistanceToCenter => Vector3.Distance( V_Position, v_triCenter_cached );

		/// <summary>Should be the same as the Surface Orientation setting for the navmesh that this vert's triangle belongs to.</summary>
		[SerializeField, HideInInspector] private Vector3 v_surfaceNormal_cached;
		public Vector3 CachedSurfaceNormal => v_surfaceNormal_cached;


		// TRUTH...........
		public bool AmModified
		{
			get {  return V_Position != originalPosition; }
		}



		//public bool AmOnTerminalEdge; //todo: Implement

		#region RELATIONAL ======================================================================
		[HideInInspector] public LNX_VertexRelationship[] Relationships;

		/// <summary>Index where you can find this vertex from the perspective of other Vertices.</summary>
		public int Index_Relational => (MyCoordinate.TrianglesIndex * 3) + MyCoordinate.ComponentIndex;

		//todo: all these index properties need to be unit tested for accuracy
		public int Index_FirstSiblingVert => MyCoordinate.ComponentIndex == 0 ? 1 : 0;
		public LNX_ComponentCoordinate Coordinate_FirstSibling => new LNX_ComponentCoordinate(MyCoordinate.TrianglesIndex, Index_FirstSiblingVert);
		//private int firstSiblingRelationshipIndex => MyCoordinate.ComponentIndex == 0 ? (MyCoordinate.TrianglesIndex * 3) + 1 : MyCoordinate.TrianglesIndex * 3;
		private int firstSiblingRelationshipIndex => (MyCoordinate.TrianglesIndex * 3) + Index_FirstSiblingVert;

		public LNX_VertexRelationship FirstSiblingRelationship
		{
			get
			{
				/*return MyCoordinate.ComponentIndex == 0 ?
					Relationships[(MyCoordinate.TrianglesIndex * 3) + 1] : Relationships[MyCoordinate.TrianglesIndex * 3];*/

				return Relationships[firstSiblingRelationshipIndex];
			}
		}

		public int Index_SecondSiblingVert => MyCoordinate.ComponentIndex == 2 ? 1 : 2;
		public LNX_ComponentCoordinate Coordinate_SecondSibling => new LNX_ComponentCoordinate(MyCoordinate.TrianglesIndex, Index_SecondSiblingVert);

		private int secondSiblingRelationshipIndex => (MyCoordinate.TrianglesIndex * 3) + Index_SecondSiblingVert;

		public LNX_VertexRelationship SecondSiblingRelationship
		{
			get
			{
				/*return MyCoordinate.ComponentIndex == 2 ?
					Relationships[(MyCoordinate.TrianglesIndex * 3) + 1] : Relationships[(MyCoordinate.TrianglesIndex * 3) + 2];*/
				return Relationships[secondSiblingRelationshipIndex];
			}
		}

		/// <summary> Returns a localized (0 origin) vector pointing from this vert to it's first sibling vert. </summary>
		public Vector3 V_ToFirstSiblingVert
		{
			get
			{
				return Relationships[firstSiblingRelationshipIndex].V_to;
			}
		}
		public Vector3 V_ToFirstSiblingVert_flat
		{
			get
			{
				return LNX_Utils.FlatVector(V_ToFirstSiblingVert).normalized;
			}
		}
		/// <summary> Returns a localized (0 origin) vector pointing from this vert to it's first sibling vert. </summary>
		public Vector3 V_ToSecondSiblingVert
		{
			get
			{
				return Relationships[secondSiblingRelationshipIndex].V_to;
			}
		}
		public Vector3 V_ToSecondSiblingVert_flat
		{
			get
			{
				return LNX_Utils.FlatVector(V_ToSecondSiblingVert).normalized;
			}
		}

		public float DistToFirstSiblingVert_path => FirstSiblingRelationship.PathDistance;
		public float DistToSecondSiblingVert_path => SecondSiblingRelationship.PathDistance;
		public float DistToFirstSiblingVert_straight => FirstSiblingRelationship.V_to.magnitude;
		public float DistToSecondSiblingVert_straight => SecondSiblingRelationship.V_to.magnitude;

		/// <summary>Collection of vertices sharing the same space as this one.</summary>
		public LNX_ComponentCoordinate[] SharedVertexCoordinates;
		#endregion --------------------------------------------------------------------------------

		public LNX_Vertex (List<LNX_AtomicTriangle> atomicTris, int triIndx, int cmpntIndx, LNX_NavMesh nvmsh )
        {
			//Debug.Log($"vert[{triIndx}][{cmpntIndx}] ctor...");

			MyCoordinate = new LNX_ComponentCoordinate( triIndx, cmpntIndx );

			if ( cmpntIndx == 0 )
			{
				V_Position = atomicTris[triIndx].VertPos0_current;
				originalPosition = atomicTris[triIndx].VertPos0_orig;
			}
			else if ( cmpntIndx == 1 )
			{
				V_Position = atomicTris[triIndx].VertPos1_current;
				originalPosition = atomicTris[triIndx].VertPos1_orig;
			}
			else
			{
				V_Position = atomicTris[triIndx].VertPos2_current;
				originalPosition = atomicTris[triIndx].VertPos2_orig;
			}

			v_surfaceNormal_cached = nvmsh.GetSurfaceNormalVector();

			//v_triCenter_cached = atomicTris[triIndx].Center;
			v_triCenter_cached = (atomicTris[triIndx].VertPos0_current + atomicTris[triIndx].VertPos1_current + atomicTris[triIndx].VertPos2_current) / 3f;

			//Debug.Log($"{nameof(v_triCenter_cached)}: '{v_triCenter_cached}', from atomic: '{atomicTris[triIndx].Center}'");

			if( v_triCenter_cached == Vector3.zero )
			{
				Debug.LogError($"{nameof(v_triCenter_cached)}: '{v_triCenter_cached}', from atomic: '{atomicTris[triIndx].Center}'");
			}

			Index_VisMesh_Vertices = -1;
		}

		public void CalculateDerivedInfo(LNX_Triangle tri, LNX_NavMesh nvmsh )
		{
			#region ESTABLISH SIBLING RELATIONSHIPS FIRST --------------------------------------------------			
			Relationships = new LNX_VertexRelationship[nvmsh.Triangles.Length * 3];
			//First establish initial relationships sibling relationships. This is important to do 
			//now so that the rest can raycast without error...
			
			Relationships[firstSiblingRelationshipIndex] = new LNX_VertexRelationship(
				this, tri, Index_FirstSiblingVert
			);
			Relationships[secondSiblingRelationshipIndex] = new LNX_VertexRelationship(
				this, tri, Index_SecondSiblingVert
			);
			
			#endregion
		}

		public void AdoptValues( LNX_Vertex vert )
		{
			V_Position = vert.V_Position;
			originalPosition = vert.originalPosition;
			v_surfaceNormal_cached = vert.v_surfaceNormal_cached;
			v_triCenter_cached = vert.v_triCenter_cached;

			MyCoordinate = vert.MyCoordinate;

			Relationships = vert.Relationships;
			SharedVertexCoordinates = vert.SharedVertexCoordinates;

		}

		public void CreateRelationships( LNX_NavMesh nvmsh ) //todo: unit test
		{
			//Debug.Log( $"vert[{MyCoordinate}].{nameof(CreateRelationships)}()---" );

			//DateTime dt_start = DateTime.Now;
			//why does this take so long?
			Relationships = new LNX_VertexRelationship[nvmsh.Triangles.Length * 3];

			#region ESTABLISH SIBLING RELATIONSHIPS FIRST --------------------------------------------------			
			//Note: Even though I've already done this in the CalculateDerivedInfo() method, I need to do 
			//this again here, because those relationships are gone now that I've re-initialized the
			//Relationships collection above...
			Relationships[firstSiblingRelationshipIndex] = new LNX_VertexRelationship(
				this, nvmsh.GetTriangle(this), Index_FirstSiblingVert
			);
			Relationships[secondSiblingRelationshipIndex] = new LNX_VertexRelationship(
				this, nvmsh.GetTriangle(this), Index_SecondSiblingVert
			);
			#endregion
			//Debug.Log($"creating sibling relationships took: '{DateTime.Now.Subtract(dt_start)}'");

			List<LNX_ComponentCoordinate> temp_sharedVrtCoords = new List<LNX_ComponentCoordinate>();

			for ( int i = 0; i < nvmsh.Triangles.Length; i++ ) //Note: Before optimization this look took about 1.6 seconds
			{
				if( i == MyCoordinate.TrianglesIndex )
				{
					continue;
				}

				Relationships[(i*3)] = new LNX_VertexRelationship( this, nvmsh.Triangles[i].Verts[0], nvmsh, true ); 
				Relationships[(i*3)+1] = new LNX_VertexRelationship( this, nvmsh.Triangles[i].Verts[1], nvmsh, true);
				Relationships[(i*3)+2] = new LNX_VertexRelationship( this, nvmsh.Triangles[i].Verts[2], nvmsh, true);

				if ( nvmsh.Triangles[i].Verts[0].V_Position == V_Position )
				{
					temp_sharedVrtCoords.Add( nvmsh.Triangles[i].Verts[0].MyCoordinate );
				}
				else if ( nvmsh.Triangles[i].Verts[1].V_Position == V_Position )
				{
					temp_sharedVrtCoords.Add( nvmsh.Triangles[i].Verts[1].MyCoordinate );
				}
				else if ( nvmsh.Triangles[i].Verts[2].V_Position == V_Position )
				{
					temp_sharedVrtCoords.Add(nvmsh.Triangles[i].Verts[2].MyCoordinate);
				}
			}

			//Debug.Log($"creating the rest took: '{DateTime.Now.Subtract(dt_start)}'");

			SharedVertexCoordinates = temp_sharedVrtCoords.ToArray();
		}
		public void TriIndexChanged(int newIndex)
		{
			MyCoordinate = new LNX_ComponentCoordinate(newIndex, MyCoordinate.ComponentIndex);
		}

		#region API METHODS ------------------------------------------------------------
		[NonSerialized] public string DBG_IsInCenterSweep;
		public bool IsInCenterSweep( Vector3 pos, Vector3 nrml )
		{
			DBG_IsInCenterSweep = $"Vert{MyCoordinate.ComponentIndex}.{nameof(IsInCenterSweep)}({pos}) " +
				$"report...\n";

			string s = "";
			return LNX_Utils.AmInVectorCone( pos, V_ToFirstSiblingVert_flat, V_ToSecondSiblingVert_flat, v_surfaceNormal_cached, ref s );
		}

		/// <summary>
		/// Returns a path to the supplied LNX_Vertex by fetching it from the relationships collection. This 
		/// will NOT work if called before the relationships collection has been properly set up.
		/// </summary>
		/// <param name="otherVert"></param>
		/// <returns></returns>
		public LNX_Path GetPathTo(LNX_Vertex otherVert)
		{
			return GetRelationship(otherVert).PathTo;
		}
		#endregion

		#region RELATIONAL METHODS----------------------------------------------
		public bool SharesVertSpaceWithTri( LNX_Triangle tri )
		{
			if ( tri.Index_inCollection == MyCoordinate.TrianglesIndex )
			{
				return true;
			}

			if ( SharedVertexCoordinates != null && SharedVertexCoordinates.Length > 0 )
			{
				for ( int i = 0; i < SharedVertexCoordinates.Length; i++ )
				{
					if ( SharedVertexCoordinates[i].TrianglesIndex == tri.Index_inCollection )
					{
						return true;
					}
				}
			}
			else //fallback for when relational data isn't loaded...
			{
				if
				(
					tri.Verts[0].V_Position == V_Position ||
					tri.Verts[1].V_Position == V_Position ||
					tri.Verts[2].V_Position == V_Position
				)
				{
					return true;
				}
			}

			return false;
		}

		public bool SharesVertSpace( LNX_Vertex vert ) //todo: this method won't be necessary if we unify the verts
		{
			if( SharedVertexCoordinates != null && SharedVertexCoordinates.Length > 0 )
			{
				for ( int i = 0; i < SharedVertexCoordinates.Length; i++ )
				{
					if ( SharedVertexCoordinates[i] == vert.MyCoordinate )
					{
						return true;
					}
				}
			}

			return V_Position == vert.V_Position;
		}

		public bool AreSiblings( LNX_ComponentCoordinate otherVertCoordinate )
		{
			return MyCoordinate.TrianglesIndex > -1 &&
				otherVertCoordinate.TrianglesIndex > -1 &&
				MyCoordinate.TrianglesIndex == otherVertCoordinate.TrianglesIndex;
		}

		public bool AreSiblings( LNX_Vertex otherVert )
		{
			return MyCoordinate.TrianglesIndex > -1 && 
				otherVert.MyCoordinate.TrianglesIndex > -1 && 
				MyCoordinate.TrianglesIndex == otherVert.MyCoordinate.TrianglesIndex;
		}

		public LNX_VertexRelationship GetRelationship( LNX_Vertex otherVert )
		{
			return Relationships[otherVert.Index_Relational];
		}

		public LNX_VertexRelationship GetRelationship( LNX_ComponentCoordinate vertCoord )
		{
			return Relationships[vertCoord.TrianglesIndex * 3 + (vertCoord.ComponentIndex)];
		}

		public bool IsRelationshipCollectionValid()
		{
			if( Relationships == null || Relationships.Length <= 0 )
			{
				return false;
			}

			for( int i = 0; i < Relationships.Length; i++ )
			{
				//if( Relationships[i] == LNX_VertexRelationship.None )
				if ( !Relationships[i].AmValid )
				{
					return false;
				}
			}

			return true;
		}
		#endregion

		public void DbgPing(ref string dbgString, LNX_NavmeshHit endPoint, LNX_NavMesh nm, 
			float maxAllowableDist, LNX_Path runningPath, List<LNX_ComponentCoordinate> backstopverts = null
		)
		{
			dbgString = $"{this}.Ping('{endPoint}', max: '{maxAllowableDist}', bkstps: " +
				$"'{(backstopverts == null ? "null" : backstopverts.Count)}')\n";

			//LNX_Path rtrnPath = runningPath;

			#region SHORT-CIRCUITING ========================================
			dbgString += $"first, raycasting to see if endPoint is visible from this vert...\n";
			LNX_Path rcPath = new LNX_Path();
			if (!nm.Raycast(new LNX_NavmeshHit(this), endPoint, out rcPath))
			{
				dbgString += $"endpoint WAS visible. Making path and returning...\n";
				return;
			}

			dbgString += $"endpoint NOT visible. Continuing...\n";
			#endregion ---------------------------------------

			#region ASSEMBLE NEW (FORWARD) BACKSTOP ============================================
			dbgString += $"Now assembling a forward backstop, which will include this vertex...\n";
			List<LNX_ComponentCoordinate> fwdBackstopVerts = new List<LNX_ComponentCoordinate>();
			if (backstopverts != null && backstopverts.Count > 0)
			{
				for (int i = 0; i < backstopverts.Count; i++)
				{
					fwdBackstopVerts.Add(backstopverts[i]);
				}
			}

			if( !fwdBackstopVerts.Contains(MyCoordinate) )
			{
				fwdBackstopVerts.Add(MyCoordinate);
			}

			dbgString += $"fwd backstop count now: '{fwdBackstopVerts.Count}'...\n" +
				$"now getting visible verts from This vert...\n";

			string dbgGetVisVrtsAt = "";
			List<LNX_Path> vsblVrtPths = new List<LNX_Path>();
			List<LNX_ComponentCoordinate> visibleVrts = nm.GetVisibleVertsFromPoint(this, out vsblVrtPths, ref dbgGetVisVrtsAt, false, fwdBackstopVerts);

			if (visibleVrts.Count <= 0)
			{
				Debug.Log($"Ping() method tried to get visible verts from '{ToString()}', but failed to get any " +
					$"that weren't part of backstop collection. Returning...");
				return;
			}

			for (int i = 0; i < visibleVrts.Count; i++)
			{
				fwdBackstopVerts.Add(visibleVrts[i]);
			}
			dbgString += $"Decided there are '{visibleVrts.Count}' visible verts (not in backstop) from this vert.\n" +
				$"final backstop count: '{fwdBackstopVerts.Count}'...\n";


			#endregion
		}

		public LNX_Path Ping( DateTime dt, LNX_NavmeshHit endPoint, LNX_NavMesh nm, float maxAllowableDist, LNX_Path runningPath,
			List<LNX_ComponentCoordinate> backstopverts = null
		)
		{
			if( DateTime.Now.Subtract(dt).TotalSeconds > 30 )
			{
				Debug.LogWarning($"time limit reached at vert: '{this}'!");
				return LNX_Path.None;
			}

			#region SHORT-CIRCUITING ========================================
			LNX_Path pth = LNX_Path.None;
			if ( !nm.Raycast(new LNX_NavmeshHit(this), endPoint, out pth) )
			{
				runningPath.AddPath( pth );
				return runningPath;
			}
			#endregion ---------------------------------------

			LNX_Path rtrnPath = runningPath;

			#region ASSEMBLE NEW(FORWARD) BACKSTOP ============================================
			List<LNX_ComponentCoordinate> fwdBackstopVerts = new List<LNX_ComponentCoordinate>();

			if ( backstopverts != null && backstopverts.Count > 0 )
			{
				for ( int i = 0; i < backstopverts.Count; i++ )
				{
					fwdBackstopVerts.Add( backstopverts[i] );
				}
			}

			if ( !fwdBackstopVerts.Contains(MyCoordinate) )
			{
				fwdBackstopVerts.Add( MyCoordinate );
			}
			#endregion

			string dbgGetVisVrtsAt = "";
			List<LNX_Path> vsblVrtPths = new List<LNX_Path>();
			List<LNX_ComponentCoordinate> visibleVrts = nm.GetVisibleVertsFromPoint(this, out vsblVrtPths, ref dbgGetVisVrtsAt, false, fwdBackstopVerts );
			for ( int i = 0; i < visibleVrts.Count; i++ )
			{
				fwdBackstopVerts.Add( visibleVrts[i] );
			}

			if( visibleVrts.Count <= 0 )
			{
				Debug.Log($"Ping() method tried to get visible verts from '{ToString()}', but failed to get any " +
					$"that weren't part of backstop collection. Returning...");
				return LNX_Path.None;
			}
			else
			{
				for ( int i = 0; i < visibleVrts.Count; i++ )
				{
					LNX_Path path_continuationToVsblVrt;
					
					LNX_Utils.TryProjectPathThrough(
						nm, runningPath.EndHit, 
						nm.Triangles[visibleVrts[i].TrianglesIndex].Verts[visibleVrts[i].ComponentIndex], 
						out path_continuationToVsblVrt
					);
						
					LNX_Path p = nm.Triangles[visibleVrts[i].TrianglesIndex].Verts[visibleVrts[i].ComponentIndex].Ping(
						dt, endPoint, nm, maxAllowableDist, path_continuationToVsblVrt, fwdBackstopVerts
					);

					if (p != LNX_Path.None && runningPath.GetCombinedDistance(p) < maxAllowableDist && p.TotalDistance < rtrnPath.TotalDistance)
					{
						rtrnPath = p;
					}

					if (DateTime.Now.Subtract(dt).TotalSeconds > 30)
					{
						Debug.LogWarning($"time limit reached at vert: '{this}'!");
						return LNX_Path.None;
					}
				}
			}

			return rtrnPath;
		}


		#region HELPERS --------------------------------------------------
		public string GetCurrentInfoString()
		{
			return $"Vert.{nameof(SayCurrentInfo)}()\n" +
				$"{nameof(MyCoordinate)}: '{MyCoordinate}'\n" +
				$"{nameof(V_Position)}: '{V_Position}'\n" +
				$"{nameof(originalPosition)}: '{originalPosition}'\n" +

				$"{nameof(v_triCenter_cached)}: '{v_triCenter_cached}'\n" +
				$"{nameof(v_surfaceNormal_cached)}: '{v_surfaceNormal_cached}'\n" +
				$"{nameof(Relationships)} count: '{Relationships.Length}\n" +
				$"{nameof(Index_VisMesh_Vertices)}: '{Index_VisMesh_Vertices}'\n" +
				$"{nameof(AngleAtBend)}: '{AngleAtBend}'\n" +
				$"{nameof(AngleAtBend_flattened)}: '{AngleAtBend_flattened}'\n" +

				$"{nameof(Index_FirstSiblingVert)}: '{Index_FirstSiblingVert}'\n" +
				$"{nameof(firstSiblingRelationshipIndex)}: '{firstSiblingRelationshipIndex}'\n" +
				$"{nameof(V_ToFirstSiblingVert)}: '{V_ToFirstSiblingVert}'\n" +

				$"{nameof(Index_SecondSiblingVert)}: '{Index_SecondSiblingVert}'\n" +
				$"{nameof(secondSiblingRelationshipIndex)}: '{secondSiblingRelationshipIndex}'\n" +
				$"{nameof(V_ToSecondSiblingVert)}: '{V_ToSecondSiblingVert}'\n" +

				$"{nameof(SharedVertexCoordinates)} length: '{SharedVertexCoordinates.Length}'\n" +

				$"";
		}

		public void SayCurrentInfo()
		{
			Debug.Log( GetCurrentInfoString() );
		}

		public override string ToString()
		{
			//return $"{MyCoordinate.ToString()} {V_Position}";
			return $"{MyCoordinate.ToString()}";

		}

		public string GetAnomolyString(LNX_NavMesh nm )
		{
			string returnString = string.Empty;

			if (
				MyCoordinate.TrianglesIndex < 0 ||
				MyCoordinate.TrianglesIndex > nm.Triangles.Length - 1 ||
				MyCoordinate.ComponentIndex < 0 ||
				MyCoordinate.ComponentIndex > 2
			)
			{
				returnString += $"{nameof(MyCoordinate)}: '{MyCoordinate}'\n";
			}

			if (V_Position == Vector3.zero)
			{
				returnString += $"{nameof(V_Position)}: '{V_Position}'\n";
			}

			if (originalPosition == Vector3.zero)
			{
				returnString += $"{nameof(originalPosition)}: '{originalPosition}'\n";
			}

			if (CachedSurfaceNormal == Vector3.zero)
			{
				returnString += $"{nameof(CachedSurfaceNormal)}: '{CachedSurfaceNormal}'\n";
			}

			if (Index_VisMesh_Vertices == -1 )
			{
				returnString += $"{nameof(Index_VisMesh_Vertices)}: '{Index_VisMesh_Vertices}'\n";
			}

			if (v_triCenter_cached == Vector3.zero)
			{
				returnString += $"{nameof(v_triCenter_cached)}: '{v_triCenter_cached}'\n";
			}

			if (v_surfaceNormal_cached == Vector3.zero)
			{
				returnString += $"{nameof(v_surfaceNormal_cached)}: '{v_surfaceNormal_cached}'\n";
			}

			if( AngleAtBend > 180 || AngleAtBend < float.MinValue )
			{
				returnString += $"{nameof(AngleAtBend)}: '{AngleAtBend}'\n";
			}

			if ( DistanceToCenter <= 0 )
			{
				returnString += $"{nameof(DistanceToCenter)} was '{DistanceToCenter}'\n";
			}

			#region RElATIONAL------------------------------------------------
			if ( Relationships == null || Relationships.Length == 0 )
			{
				returnString += $"{nameof(Relationships)} collection not set\n";
			}

			if (SharedVertexCoordinates.Length <= 0 )
			{
				returnString += $"{nameof(SharedVertexCoordinates)} length: '{SharedVertexCoordinates.Length}'\n";
			}

			if ( FirstSiblingRelationship.V_to == Vector3.zero )
			{
				returnString += $"{nameof(FirstSiblingRelationship)}.{nameof(FirstSiblingRelationship.V_to)} was '{FirstSiblingRelationship.V_to}'\n";
			}

			if( FirstSiblingRelationship.V_to != V_ToFirstSiblingVert )
			{
				returnString += $"{nameof(FirstSiblingRelationship)}.{nameof(FirstSiblingRelationship.V_to)} at '{FirstSiblingRelationship.V_to}' was NOT equal to " +
					$"{nameof(V_ToFirstSiblingVert)} at '{V_ToFirstSiblingVert}'\n";
			}

			if (SecondSiblingRelationship.V_to == Vector3.zero)
			{
				returnString += $"{nameof(SecondSiblingRelationship)}.{nameof(SecondSiblingRelationship.V_to)} was '{SecondSiblingRelationship.V_to}'\n";
			}

			if ( FirstSiblingRelationship.V_to == SecondSiblingRelationship.V_to )
			{
				returnString += $"{nameof(FirstSiblingRelationship)}.{nameof(FirstSiblingRelationship.V_to)} was Equal to {nameof(SecondSiblingRelationship.V_to)}\n";
			}

			if ( V_ToFirstSiblingVert == V_ToSecondSiblingVert )
			{
				returnString += $"{nameof(V_ToFirstSiblingVert)} was Equal to {nameof(V_ToSecondSiblingVert)}\n";
			}
			#endregion

			return returnString;
		}

		public string GetRelationalString()
		{
			string s = $"Vert[{ComponentIndex}].{nameof(GetRelationalString)}()\n";

			if( Relationships != null )
			{
				s += $"{nameof(Relationships)} count: '{Relationships.Length}'\n" +
				$"{nameof(FirstSiblingRelationship)}: '{FirstSiblingRelationship}'\n" +
				$"{nameof(SecondSiblingRelationship)}: '{SecondSiblingRelationship}'\n\n" +
				$"{nameof(SharedVertexCoordinates)} count: '{SharedVertexCoordinates.Length}'\n" +
				$"";
			}
			else
			{
				s += "relationships collection was null...\n";
			}


			if (SharedVertexCoordinates == null)
			{
				s += $"{nameof(SharedVertexCoordinates)} collection is null\n";
			}
			else
			{
				s += $"{nameof(SharedVertexCoordinates)} length: '{SharedVertexCoordinates.Length}'\n";
			}

			return s;
		}		

		public void SayAllRelationships()
		{
			string s = $"{this}.{nameof(SayAllRelationships)}()\n";
			int canSeeCount = 0;
			int cannotSeeCount = 0;
			int amValidCount = 0;

			if( Relationships == null )
			{
				s += $"relationships collection is null. Returning early...";
			}
			else if( Relationships.Length == 0 )
			{
				s += $"relationships collection count is only 0. Returning early...";
			}
			else
			{
				s += $"relationships collection count is '{Relationships.Length}'. Iterating through all...\n\n";


				for( int i = 0; i < Relationships.Length; i++ )
				{
					s += $"({i}) : {Relationships[i]}\n\n";
					if(Relationships[i].CanSee )
					{
						canSeeCount++;
					}
					else
					{
						cannotSeeCount++;
					}

					if(Relationships[i].AmValid )
					{
						amValidCount++;
					}
				}
			}

			s += $"\nREPORT==============================\n" +
				$"can see count: '{canSeeCount}'\n" +
				$"can NOT see count: '{cannotSeeCount}'\n" +
				$"amValid count: '{amValidCount}'";

			Debug.Log( s );

			Debug.Log($"can see count: '{canSeeCount}'\n" +
				$"can NOT see count: '{cannotSeeCount}'\n" +
				$"amValidCount: '{amValidCount}'"
			);
		}
		#endregion
	}
}