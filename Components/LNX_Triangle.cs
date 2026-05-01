using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.iOS;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_Triangle
	{
		/// <summary>Describes this triangle's position inside the containing manager's Triangles array </summary>
		[SerializeField, HideInInspector] private int index_inCollection;
		public int Index_inCollection => index_inCollection;
		/// <summary>Index where this triangle's vertices start in the visualization mesh's .triangles array. Note: 
		/// this is just Index_inCollection * 3.</summary>
		public int MeshIndex_trianglesStart => index_inCollection * 3;

		[HideInInspector, Tooltip("Corresponds to the area indices set up in the Navigation window.")] 
		public int AreaIndex;

		[Header("COMPONENTS")]
		public LNX_Vertex[] Verts;
		public LNX_Edge[] Edges;

		[Header("CALCULATED/DERIVED")]
		[HideInInspector] public Vector3 V_Center; //todo: look into possibly making this a property calculated as needed as long as it won't hamper performance too much

		/// <summary>The center of the triangle "flattened" with respect to the surface orientation of the navmesh.</summary>
		public Vector3 V_FlattenedCenter => LNX_Utils.FlatVector( V_Center, v_SurfaceNormal_cached );
		/// <summary>Distance around the triangle</summary>
		public float Perimeter
		{
			get
			{
				return Edges[0].EdgeLength + Edges[1].EdgeLength + Edges[2].EdgeLength;
			}
		}
		/// <summary>The longest edge of this triangle. This can be used for effecient decision-making. 
		/// IE: IF a position's distance from this triangle's center is greater than half of this value, it can't possibly 
		/// be on this triangle</summary>
		public float LongestEdgeLength
		{
			get
			{
				return Mathf.Max( Edges[0].EdgeLength, Edges[1].EdgeLength, Edges[2].EdgeLength );
			}
		}
		public float ShortestEdgeLength
		{
			get
			{
				return Mathf.Min( Edges[0].EdgeLength, Edges[1].EdgeLength, Edges[2].EdgeLength );
			}
		}
		public float Area
		{
			get //"Heron's formula"
			{
				return Mathf.Sqrt(
				Perimeter * 0.5f *
				((Perimeter * 0.5f) - Edges[0].EdgeLength) *
				((Perimeter * 0.5f) - Edges[1].EdgeLength) *
				((Perimeter * 0.5f) - Edges[2].EdgeLength)
				);
			}
		}

		public float Slope //todo: this needs to account for when triangles are completely facing the wrong direction. IE: when they're "kinked"
		{
			get
			{
				return Vector3.Angle( v_SurfaceNormal_cached, V_PlaneFaceNormal );
			}
		}

		/// <summary>The quaternion describing the angle that this triangle is "facing" compared to the projection of the navmesh</summary>
		public Quaternion FaceRotation
		{
			get
			{
				return Quaternion.FromToRotation(v_SurfaceNormal_cached, V_PlaneFaceNormal);
			}
		}

		[Header("RELATIONAL")]
		/// <summary>
		/// Array of indices of triangles that share at least one vertex with this triangle.
		/// </summary>
		//public int[] AdjacentTriIndices;


		/// <summary>Collection of indices of triangles known to be fully visible. Note: currently it's not guaranteed that all fully-visible 
		/// tris will be in this list. These are only the ones I can tell for sure. To be used for efficiency short-circuiting.</summary>
		private int[] indices_knownFullyVisibleTriangles;

		public int[] KnownFullyVisibleTriangleIndices => indices_knownFullyVisibleTriangles;


		//[Header("FLAGS")]
		/// <summary>Marks a vert dirty after a re-position of vert so that it's containing triangle knows to 
		/// re-calculate it's derived info when the user stops moving the vert.</summary>
		[SerializeField, HideInInspector] private bool dirtyFlag_repositionedVert = false;
		public bool DirtyFlag_RepositionedVert => dirtyFlag_repositionedVert;

		/// <summary>Whether this triangle was added by a mesh modification, as opposed to being created 
		/// as part of the original navmesh triangulation.</summary>
		public bool WasAddedViaMod;

		public bool HasBeenModifiedAfterCreation
		{
			get
			{
				return Verts[0].AmModified || Verts[1].AmModified || Verts[2].AmModified;
			}
		}

		public bool HasTerminalEdge
		{
			get
			{
				return Edges[0].AmTerminal || Edges[1].AmTerminal || Edges[2].AmTerminal;
			}
		}

		/// <summary>Whether this triangle's face direction is oriented correctly </summary>
		public bool AmKinked => Vector3.Dot(v_SurfaceNormal_cached, V_PlaneFaceNormal) <= 0f; //note: this will only work if the plane face normal is calculated correctly. Will need to make sure to do that

		//[Header("OTHER")]
		/// <summary>Normal derived by sampling the terrain underfoot.</summary>
		[HideInInspector] public Vector3 v_sampledNormal;
		/// <summary>Normal derived from the layout of the triangle's vertices. IE: The direction the plane 
		/// formed by the vertices is facing.</summary>
		[HideInInspector] public Vector3 V_PlaneFaceNormal;
		/// <summary>This is the normal used for shape projecting. It should be the same as the SurfaceOrientation 
		/// of the LNX_Navmesh this triangle belongs to, getting cached from a passed reference during the constructor.</summary>
		[HideInInspector] public Vector3 v_SurfaceNormal_cached;
		/// <summary>Gets the best normal this triangle is able to provide for traversing over it's surface. </summary>
		public Vector3 V_PathingNormal
		{
			get
			{
				return V_PlaneFaceNormal != Vector3.zero ? V_PlaneFaceNormal : v_SurfaceNormal_cached;
			}
		}

		//[Header("DEBUG")]
		//[TextArea(0,10)] public string DBG_Class;
		//[SerializeField] private string DbgCalculateTriInfo;
		/*[TextArea(0, 10)]*/
		//[HideInInspector] public string DBG_Relationships;

		public LNX_Triangle( int parallelIndex, int areaIndx, List<LNX_AtomicTriangle> atomicTris, LNX_NavMesh navMesh )
		{
			//DBG_Class = $"ctor '({DateTime.Now.ToString()})'...\n";

			index_inCollection = parallelIndex;
			dirtyFlag_repositionedVert = false;

			AreaIndex = areaIndx;
			v_SurfaceNormal_cached = navMesh.GetSurfaceProjectionVector();

			V_Center = atomicTris[parallelIndex].Center;

			Verts = new LNX_Vertex[3];
			Verts[0] = new LNX_Vertex( atomicTris, Index_inCollection, 0, navMesh ); //stack trace 4
			Verts[1] = new LNX_Vertex( atomicTris, index_inCollection, 1, navMesh );
			Verts[2] = new LNX_Vertex( atomicTris, index_inCollection, 2, navMesh );

			Edges = new LNX_Edge[3];
			Edges[0] = new LNX_Edge( atomicTris, this, Verts[1], Verts[2], index_inCollection, 0 );
			Edges[1] = new LNX_Edge( atomicTris, this, Verts[0], Verts[2], index_inCollection, 1 );
			Edges[2] = new LNX_Edge( atomicTris, this, Verts[0], Verts[1], index_inCollection, 2 );

			CalculateDerivedInfo( navMesh );
			SampleNormal( navMesh ); 
		}

		public void RefreshMe( LNX_NavMesh nm, bool meshContinuityHasChanged )
		{
			Debug.Log($"{nameof(RefreshMe)}() on {this.ToString()} at {DateTime.Now}");
			//DateTime dt_methodStart = DateTime.Now;
			//case 1: a single vert on the mesh has been moved
			//case 2: A tri has been added
			//case 3: A tri has been deleted

			if( dirtyFlag_repositionedVert )
			{
				CalculateDerivedInfo( nm );

				SampleNormal( nm );
				//note: Calling SampleNormal() here may seem expensive, but technically
				//it seems necessary bc if the triangle changes it should probably resample
			}

			Verts[0].CreateRelationships(nm);
			Verts[1].CreateRelationships(nm);
			Verts[2].CreateRelationships(nm);

			if ( meshContinuityHasChanged )
			{
				Edges[0].CreateRelationships(nm);
				Edges[1].CreateRelationships(nm);
				Edges[2].CreateRelationships(nm);
			}
		}

		public string DBG_FullyVisible;
		public void CalculateCompletelyVisibleTris(LNX_NavMesh nm, LNX_Edge[] terminalEdges )
		{
			//DBG_FullyVisible = $"{this.ToString()}.{nameof(CalculateCompletelyVisibleTris)}() at {DateTime.Now}----------\n";
			//Debug.Log(DBG_FullyVisible);
			
			List<int> temp_fullyVisTriIndices = new List<int>();

			//DBG_FullyVisible += $"First, inspecting composite edges for visibility based on angle...\n";
			#region ADD TRIS WITH SHARED COMPOSITE EDGES...
			// Start with composing edges....
			for (int i = 0; i < 3; i++) //Add any shared edge triangles that have the correct angle...
			{
				/*DBG_FullyVisible += $"Edge{i}...\n" +
					$"start shared angle: '{Edges[i].GetCombinedSharedEdgeAngle(nm, true)}'---\n" +
					//$"{Edges[i].DBG_GetSharedAngle}\n" +
					$"end shared angle: '{Edges[i].GetCombinedSharedEdgeAngle(nm, false)}'---\n" +
					//$"{Edges[i].DBG_GetSharedAngle}\n" +
					$"";*/
				if
				(
					Edges[i].SharedEdgeCoordinate != LNX_ComponentCoordinate.None &&
					Edges[i].GetCombinedSharedEdgeAngle(nm, true) <= 180f &&
					Edges[i].GetCombinedSharedEdgeAngle(nm, false) <= 180f
				)
				{
					//DBG_FullyVisible += $"Adding '{Edges[i].SharedEdgeCoordinate.TrianglesIndex}'...\n";
					temp_fullyVisTriIndices.Add(Edges[i].SharedEdgeCoordinate.TrianglesIndex);

					//Now check the next triangle out...
					nm.GetEdge(Edges[i].SharedEdgeCoordinate);
					if( 
						nm.GetVertexAtCoordinate(nm.GetEdge(Edges[i].SharedEdgeCoordinate).StartVertCoordinate).AngleAtBend_flattened <= 90f &&
						nm.GetVertexAtCoordinate(nm.GetEdge(Edges[i].SharedEdgeCoordinate).EndVertCoordinate).AngleAtBend_flattened <= 90f
					)
					{
						//temp_fullyVisTriIndices.Add()
					}
				}
				else
				{
					//DBG_FullyVisible += $"not adding any indices..\n";
				}
			}
			#endregion

			//DBG_FullyVisible += $"\nNow checking the rest based on terminal edge obstruction. There are '{terminalEdges.Length}' terminal edges...\n";

			//Debug.Log($"Now checking the rest based on terminal edge obstruction. There are '{terminalEdges.Length}' terminal edges...");

			LNX_ComponentCoordinate obstructEdgeCheck = new LNX_ComponentCoordinate(53, 0);

			for( int i = 0; i < nm.Triangles.Length; i++ )
			{
				//Debug.Log($"(for)tri '{i}'...\n");

				#region SHORT-CIRCUITING ======================================================
				if ( i == index_inCollection || temp_fullyVisTriIndices.Contains(i) )
				{
					//Debug.Log("Bypassing obstruction check because of index...");
					continue;
				}

				if 
				( 
					nm.Triangles[i].KnownFullyVisibleTriangleIndices != null && 
					nm.Triangles[i].KnownFullyVisibleTriangleIndices.Length > 0
				)
				{
					//Debug.LogWarning("shortcircuit");
					if( nm.Triangles[i].HasIndexInKnownFullyVisibleList(index_inCollection) )
					{
						temp_fullyVisTriIndices.Add(i);
					}
					continue;
				}
				#endregion --------------------------------------------------------

				bool foundObstruction = false;
				
				for ( int i_trmnlEdgs = 0; i_trmnlEdgs < terminalEdges.Length; i_trmnlEdgs++ )
				{
					//Debug.Log($"Checking if terminal edge: '{terminalEdges[i_trmnlEdgs].MyCoordinate}' obstructs...");

					string DBG_Encompass = "";
					if
					(
						LNX_Utils.DoesEdgeObstructTriPath(terminalEdges[i_trmnlEdgs], this, nm.Triangles[i], ref DBG_Encompass)
					)
					{
						//Debug.Log($"Found obstruction by edge: '{terminalEdges[i_trmnlEdgs]}'!");

						foundObstruction = true;
						break;
					}
				}

				if( !foundObstruction )
				{
					//Debug.Log($"Adding tri: '{i}'...");

					temp_fullyVisTriIndices.Add(i);

					#region ADD ANY THAT NOW HAVE 2 VISIBLE EDGES==============================
					/*
					for ( int i_tris = 0; i_tris < nm.Triangles.Length; i_tris++ ) //curently this doesn't seem like it works correctly
					{
						if( temp_fullyVisTriIndices.Contains(i_tris) )
						{
							continue;
						}

						int foundSharedEdgeCount = 0;

						for (int i_tmp = 0; i_tmp < temp_fullyVisTriIndices.Count; i_tmp++)
						{
							if (nm.Triangles[i_tris].HasSharedEdgeWith(i) )
							{
								foundSharedEdgeCount++;
								if( foundSharedEdgeCount > 1 )
								{
									DBG_FullyVisible += $"after adding fully visibile tri, found that tri{i_tris} now shares 2 edges with known fully visible...\n";
									Debug.LogWarning($"added known visible via 2 shared edges...");
									temp_fullyVisTriIndices.Add(i_tris);
									break;
								}
							}
						}

						if( foundSharedEdgeCount > 1 )
						{
							break;
						}
					}
					*/
					#endregion
				}

			}

			DBG_FullyVisible += $"refresh end. Now have '{temp_fullyVisTriIndices.Count}' known fully visible tris...";
			indices_knownFullyVisibleTriangles = temp_fullyVisTriIndices.ToArray();
			//Debug.Log(DBG_FullyVisible);
		}

		public void ClearKnownVisible()
		{
			indices_knownFullyVisibleTriangles = new int[0];
		}

		/// <summary>
		/// Calculates/recalculates the information a tri derives about itself using the positions of it's vertices. 
		/// Use this after you edit a tri's components.
		/// </summary>
		private void CalculateDerivedInfo( LNX_NavMesh nm )
		{
			V_Center = (Verts[0].V_Position + Verts[1].V_Position + Verts[2].V_Position) / 3f;

			#region CALCULATE PLANEFACE NORMAL------------------------------
			//Note: This calculation needs to come before the edges calculate their derived info.
			V_PlaneFaceNormal = Vector3.Cross(
				Vector3.Normalize(Verts[0].V_Position - Verts[1].V_Position),
				Vector3.Normalize(Verts[2].V_Position - Verts[1].V_Position)
			).normalized;
			if ( Vector3.Dot(v_SurfaceNormal_cached, V_PlaneFaceNormal) > Vector3.Dot(v_SurfaceNormal_cached, -V_PlaneFaceNormal) )
			{
				V_PlaneFaceNormal = -V_PlaneFaceNormal;
			}
			#endregion

			Verts[0].CalculateDerivedInfo(this, nm);
			Verts[1].CalculateDerivedInfo(this, nm);
			Verts[2].CalculateDerivedInfo(this, nm);

			Edges[0].CalculateDerivedInfo(this, nm );
			Edges[1].CalculateDerivedInfo(this, nm );
			Edges[2].CalculateDerivedInfo(this, nm );
		}

		public void SampleNormal( LNX_NavMesh nm )
		{
			//DbgCalculateTriInfo += $"{nameof(SampleNormal)}() report\n";

			RaycastHit rcHit = new RaycastHit();

			#region CALCULATE DERIVED NORMAL ----------------------------------
			Vector3 castDir = Vector3.Cross(
				Vector3.Normalize(Verts[0].V_Position - Verts[1].V_Position),
				Vector3.Normalize(Verts[2].V_Position - Verts[1].V_Position)
			);
			V_PlaneFaceNormal = -castDir.normalized; //setting this here to a default because why not...

			//DbgCalculateTriInfo += $"castdir decided to be: '{castDir}'\n";

			if (
				Physics.Linecast(V_Center - (castDir.normalized * 0.3f),
				V_Center + (castDir.normalized * 0.3f),
				out rcHit, nm.MyLayerMask))
			{
				//DbgCalculateTriInfo += $"rc1 success. hit at: '{rcHit.point}'\n";
				v_sampledNormal = rcHit.normal;
				//v_planarNormal = -castDir.normalized; //because if we hit something, that tells us which direction the planar normal should probably face.
			}
			else if (
				Physics.Linecast(V_Center + (castDir.normalized * 0.3f),
				V_Center - (castDir.normalized * 0.3f),
				out rcHit, nm.MyLayerMask))
			{
				//DbgCalculateTriInfo += $"rc2 success\n";
				v_sampledNormal = rcHit.normal;
				V_PlaneFaceNormal = castDir.normalized;//because if we hit something, that tells us which direction the planar normal should probably face.
			}
			#endregion

			#region CALCULATE SAMPLED NORMAL--------------------------------------------------
			if (
				v_SurfaceNormal_cached != Vector3.zero &&
				Physics.Linecast(V_Center + (v_SurfaceNormal_cached * 0.3f),
				V_Center + (-v_SurfaceNormal_cached * 0.3f),
				out rcHit, nm.MyLayerMask))
			{
				//DbgCalculateTriInfo += $"linecast success. hit at: '{rcHit.point}'\n";
				v_sampledNormal = rcHit.normal;
			}
			else
			{
				v_sampledNormal = Vector3.zero;
				//DbgCalculateTriInfo += $"";
			}
			#endregion
		}

		#region MAIN API METHODS----------------------------------------------------------------------

		[NonSerialized] public double TotalTime_IsInShapeProject;
		/// <summary>
		/// Determines if a supplied position is within a theoretical sweep 
		/// (or cast) of the triangle's shape along the direction of the navmesh orientation.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="projectedPos">supplied position projected onto the surface of this triangle</param>
		/// <returns></returns>
		public bool IsInShapeProject( Vector3 pos, out Vector3 projectedPos ) //todo: this one doesn't return correct if triangle surface is slanted
		{
			//todo: currently, it doesn't set projectedPos to the correct "out" value
			//todo: I can short-circuit here depending on distance, and it will make this and everything that relies on it way more performant...

			pos = LNX_Utils.FlatVector(pos, v_SurfaceNormal_cached);

			if( !LNX_Utils.AmInArea(pos, Verts[0].V_flattenedPosition, Verts[1].V_flattenedPosition, Verts[2].V_flattenedPosition, v_SurfaceNormal_cached, true) )
			{
				projectedPos = Vector3.zero;
				return false;
			}

			#region DETERMINE THE PROJECTED POSITION--------------------------------------
			if (Slope == 0f)
			{
				projectedPos = pos;
			}
			else
			{

				//Trying quaternion multiplication----------------------------------------
				/*
				//I still feel like this is worth investigating further because it reads so much better. I believe the reason the 
				result is a little bent looking is because I'm rotating a directional vector around an axis, which is going to 
				affect the x and z, when I actually need the x and z to remain the same.
				Vector3 v_fltCtr_to_fltPos = flatPos - V_FlattenedCenter;
				Vector3 v_ctrToPos = pos - V_Center;

				//projectedPos = V_Center + (Rotation * v_fltCtr_to_fltPos); //bent
				projectedPos = V_Center + (Rotation * LNX_Utils.FlatVector(v_ctrToPos, v_projectionNormal) ); //also bent...why? looks like the exact same result

				Vector3 vtry = Vector3.Project(  );
				*/
				//-------------------------------------------------------

				//Use the law of sines...
				// Note: This isn't currently perfect. It seems to create a point slightly below the surface of the triangle.
				Vector3 edgePrjct = Edges[1].StartPosition + 
					Vector3.Project(pos - Edges[1].StartPosition, Edges[1].V_StartToEnd);//this gets a point on the edge closest to the pos
				//float lenA = Vector3.Distance(edgePrjct, flatPos); //orig

				//todo: replace the following with LNX_Utils method and make sure tdgs are still showing correct results
				float lenA = Vector3.Distance( LNX_Utils.FlatVector(edgePrjct, v_SurfaceNormal_cached), pos );

				float angA = 90f * Mathf.Deg2Rad;
				float angC = Slope * Mathf.Deg2Rad;
				float lenC = (Mathf.Sin(angC) * lenA) / MathF.Sin(angA);
				//projectedPos = flatPos + (v_projectionNormal * lenC);
				projectedPos = LNX_Utils.FlooredVector(pos, edgePrjct, v_SurfaceNormal_cached) + (v_SurfaceNormal_cached * lenC);
			}
			#endregion

			return true;
		}
		public bool IsInShapeProject_dbg(Vector3 pos, out Vector3 projectedPos, ref LNX_MethodDebugReport rprt ) //todo: this one doesn't return correct if triangle surface is slanted
		{
			rprt.StartMethod($"IsInShapeProject_dbg(pos: '{pos}')");

			//todo: currently, it doesn't set projectedPos to the correct "out" value
			//todo: I can short-circuit here depending on distance, and it will make this and everything that relies on it way more performant...

			pos = LNX_Utils.FlatVector(pos, v_SurfaceNormal_cached);

			rprt.Log($"Checking with LNX_Utils.AmInArea()...");
			if 
			( 
				!LNX_Utils.AmInArea_dbg(
					pos, Verts[0].V_flattenedPosition, Verts[1].V_flattenedPosition, 
					Verts[2].V_flattenedPosition, v_SurfaceNormal_cached, true, ref rprt
				)
			)
			{
				projectedPos = Vector3.zero;
				rprt.Log_And_End_Method($"found that position is NOT in area. Returning false...", "IsInShapeProject_dbg()");
				return false;
			}

			rprt.Log($"LNX_Utils.AmInArea() returned true. Now calculating projected position...");
			rprt.Log($"note: slope: '{Slope}'...");
			#region DETERMINE THE PROJECTED POSITION--------------------------------------
			//DBG_IsInShapeProject += $"flatpos: '{flatPos}'\n";

			if (Slope == 0f)
			{
				projectedPos = pos;
			}
			else
			{

				//Trying quaternion multiplication----------------------------------------
				/*
				//I still feel like this is worth investigating further because it reads so much better. I believe the reason the 
				result is a little bent looking is because I'm rotating a directional vector around an axis, which is going to 
				affect the x and z, when I actually need the x and z to remain the same.
				Vector3 v_fltCtr_to_fltPos = flatPos - V_FlattenedCenter;
				Vector3 v_ctrToPos = pos - V_Center;

				//projectedPos = V_Center + (Rotation * v_fltCtr_to_fltPos); //bent
				projectedPos = V_Center + (Rotation * LNX_Utils.FlatVector(v_ctrToPos, v_projectionNormal) ); //also bent...why? looks like the exact same result

				Vector3 vtry = Vector3.Project(  );
				*/
				//-------------------------------------------------------


				//Use the law of sines...
				// Note: This isn't currently perfect. It seems to create a point slightly below the surface of the triangle.
				Vector3 edgePrjct = Edges[1].StartPosition +
					Vector3.Project(pos - Edges[1].StartPosition, Edges[1].V_StartToEnd);//this gets a point on the edge closest to the pos
																						 //float lenA = Vector3.Distance(edgePrjct, flatPos); //orig

				//todo: replace the following with LNX_Utils method and make sure tdgs are still showing correct results
				float lenA = Vector3.Distance(LNX_Utils.FlatVector(edgePrjct, v_SurfaceNormal_cached), pos);

				float angA = 90f * Mathf.Deg2Rad;
				float angC = Slope * Mathf.Deg2Rad;
				float lenC = (Mathf.Sin(angC) * lenA) / MathF.Sin(angA);
				//projectedPos = flatPos + (v_projectionNormal * lenC);
				projectedPos = LNX_Utils.FlooredVector(pos, edgePrjct, v_SurfaceNormal_cached) + (v_SurfaceNormal_cached * lenC);
			}
			#endregion

			rprt.Log_And_End_Method($"end of method. projectedPos: '{projectedPos}'...");		

			return true;
		}


		public Vector3 ClosestPointOnPerimeter( Vector3 pos )
		{
			//Debug.Log($"Edges.length: '{Edges.Length}'");
			Vector3 vA = Edges[0].ClosestPointOnEdge( pos );
			Vector3 vB = Edges[1].ClosestPointOnEdge( pos );
			Vector3 vC = Edges[2].ClosestPointOnEdge( pos );

			float distToA = Vector3.Distance( pos, vA );
			float distToB = Vector3.Distance( pos, vB );
			float distToC = Vector3.Distance( pos, vC );

			if( distToA < distToB && distToA < distToC )
			{
				return vA;
			}
			else if( distToB < distToA && distToB < distToC )
			{
				return vB;
			}
			else
			{
				return vC;
			}
		}



		//todo: I think it's maybe a little wierd that I'm passing in two vectors (innerPos, and outerPos), as opposed to two lnxHits. It looks like where I'm using this currently, I could 
		//just pass in two hit objects.
		public bool ProjectThroughToPerimeter( Vector3 innerPos, Vector3 outerPos, out LNX_NavmeshHit outHit, ref string dbgRprt, int indx_edgeExclude = -1, bool returnHitOnAdjacenttTriangle = false )
		{
			outHit = LNX_NavmeshHit.None;

			LNX_NavmeshHit projectedEdgeHit = LNX_NavmeshHit.None;

			Vector3 ftndInrPos = LNX_Utils.FlatVector( innerPos, v_SurfaceNormal_cached );
			Vector3 ftndOuterPos = LNX_Utils.FlatVector( outerPos, v_SurfaceNormal_cached );

			Vector3 v_to_flat = LNX_Utils.FlatVector( ftndOuterPos - ftndInrPos ).normalized;

			if ( ftndInrPos == ftndOuterPos ) //short-circuit
			{
				dbgRprt += $"Found flattened positions are the same. returning early...";
				return false;
			}

			dbgRprt += $"\tchecking each edge...\n";
			for (int i = 0; i < 3; i++)
			{
				dbgRprt += $"for edge{i}...\n";
				if ( Edges[i].DoesPositionLieOnEdge(ftndInrPos) )
				{
					float dotProd = Vector3.Dot(Edges[i].v_Cross_flat, v_to_flat);
					if 
					( 
						dotProd < 0f || //this means the projection is towards the outside of the triangle...
						v_to_flat == Edges[i].V_StartToEnd_flattened || v_to_flat == Edges[i].V_EndToStart_flattened //if the projection runs parallel with the edge...
					) 
					{
						outHit = new LNX_NavmeshHit(Edges[i].ClosestPointOnEdge(innerPos), v_SurfaceNormal_cached, innerPos,
							(returnHitOnAdjacenttTriangle && Edges[i].SharedEdgeCoordinate != LNX_ComponentCoordinate.None) ? 
							Edges[i].SharedEdgeCoordinate : new LNX_ComponentCoordinate(index_inCollection, i));
						return true;
					}
				}
				else if ( indx_edgeExclude != i && Edges[i].DoesProjectionIntersectEdge(innerPos, ftndOuterPos, out projectedEdgeHit) )
				{
					dbgRprt += $"method succeeded on edge 0. Here are the reports...\n";
					outHit = new LNX_NavmeshHit (projectedEdgeHit.Position, v_SurfaceNormal_cached, innerPos,
					(returnHitOnAdjacenttTriangle && Edges[i].SharedEdgeCoordinate != LNX_ComponentCoordinate.None) ? 
					Edges[i].SharedEdgeCoordinate : new LNX_ComponentCoordinate(index_inCollection, i));
					return true;
				}
			}

			return false;
		}
		
		public bool ProjectThroughToPerimeter( LNX_NavmeshHit innerHit, LNX_NavmeshHit outerHit, out LNX_NavmeshHit perimHit, int indx_edgeExclude = -1, bool returnHitOnAdjacenttTriangle = false)
		{
			perimHit = LNX_NavmeshHit.None;

			if( innerHit == outerHit) //short-circuit
			{
				return false;
			}

			LNX_NavmeshHit projectedEdgeHit = LNX_NavmeshHit.None;
			Vector3 ftndInrPos = LNX_Utils.FlatVector(innerHit.Position, v_SurfaceNormal_cached);
			Vector3 ftndOuterPos = LNX_Utils.FlatVector(outerHit.Position, v_SurfaceNormal_cached);
			Vector3 v_projection_flat = LNX_Utils.FlatVector(ftndOuterPos - ftndInrPos).normalized;

			if (ftndInrPos == ftndOuterPos) //short-circuit
			{
				return false;
			}

			for (int i = 0; i < 3; i++)
			{
				if ( Edges[i].DoesPositionLieOnEdge(ftndInrPos) &&
					v_projection_flat == Edges[i].V_StartToEnd_flattened || v_projection_flat == Edges[i].V_EndToStart_flattened //if the projection runs parallel with the edge...
				)
				{
					perimHit = new LNX_NavmeshHit(Edges[i].ClosestPointOnEdge(innerHit.Position), v_SurfaceNormal_cached, innerHit.Position,
						(returnHitOnAdjacenttTriangle && Edges[i].SharedEdgeCoordinate != LNX_ComponentCoordinate.None) ?
						Edges[i].SharedEdgeCoordinate : new LNX_ComponentCoordinate(index_inCollection, i));
					return true;
				}
				else
				{
					if (indx_edgeExclude != i && Edges[i].DoesProjectionIntersectEdge(innerHit.Position, ftndOuterPos, out projectedEdgeHit))
					{
						perimHit = new LNX_NavmeshHit(projectedEdgeHit.Position, v_SurfaceNormal_cached, innerHit.Position,
						(returnHitOnAdjacenttTriangle && Edges[i].SharedEdgeCoordinate != LNX_ComponentCoordinate.None) ?
						Edges[i].SharedEdgeCoordinate : new LNX_ComponentCoordinate(index_inCollection, i));

						return true;
					}
				}
			}

			return false;
		}


		public bool ProjectThroughToPerimeter_dbg( LNX_NavmeshHit innerHit, LNX_NavmeshHit outerHit, 
			out LNX_NavmeshHit perimHit, ref LNX_MethodDebugReport rprt, int indx_edgeExclude = -1, 
			bool returnHitOnAdjacenttTriangle = false)
		{
			//rprt.Log($"tab lvl: {rprt.MethodLvl}");
			rprt.StartMethod($"{this.ToString()}.ProjectThroughToPerimeter_dbg( {innerHit}, {outerHit}, {indx_edgeExclude} )");
			//rprt.Log($"tab lvl: {rprt.MethodLvl}");
			perimHit = LNX_NavmeshHit.None;
			//rprt.Log($"trictr: '{V_Center}', V_PlaneFaceNormal: '{V_PlaneFaceNormal}', srfcNrml: '{v_SurfaceNormal_cached}'");

			if( innerHit == outerHit) //short-circuit
			{
				rprt.Log("inner hit and outer hit determined to be the same. Short-circuiting...");
				rprt.EndMethod("ProjectThroughToPerimeter()");
				return false;
			}

			Vector3 projectedEdgePosition = Vector3.zero;
			Vector3 ftndInrPos = LNX_Utils.FlatVector(innerHit.Position, v_SurfaceNormal_cached);
			Vector3 ftndOuterPos = LNX_Utils.FlatVector(outerHit.Position, v_SurfaceNormal_cached);

			Vector3 v_projection_flat = LNX_Utils.FlatVector(ftndOuterPos - ftndInrPos).normalized;
			//rprt.Log($"using projection: '{v_projection_flat}'...");

			if (ftndInrPos == ftndOuterPos) //short-circuit
			{
				rprt.Log($"Found flattened positions are the same. returning early...");
				rprt.EndMethod("ProjectThroughToPerimeter()");
				return false;
			}

			rprt.Log($"No short-circuits. Now checking each edge...");
			LNX_NavmeshHit bestEdgeIntersectHit = LNX_NavmeshHit.None;
			for (int i = 0; i < 3; i++)
			{
				rprt.Log($"for edge{i}...");
				/*rprt.Log("==================================================",
					$"edge[{i}].vcross: '{Edges[i].v_Cross}', flat: '{Edges[i].v_Cross_flat}', ",
					$"calc: '{Vector3.Cross(Edges[i].V_StartToEnd, V_PlaneFaceNormal).normalized}', shouldflip: " +
					$"'{Vector3.Dot(Vector3.Cross(Edges[i].V_StartToEnd, V_PlaneFaceNormal).normalized, Edges[i].v_toCenter) < 0}', " +
					$"flat calc: '{LNX_Utils.FlatVector(Vector3.Cross(Edges[i].V_StartToEnd, V_PlaneFaceNormal).normalized, v_SurfaceNormal_cached).normalized}'", 
					$"v_toctr: '{Edges[i].v_toCenter}', cmpr dot: '{Vector3.Dot(Edges[i].v_Cross, Edges[i].v_toCenter)}'",
					"======================================================"
					);*/

				if( i == indx_edgeExclude )
				{
					rprt.Log($"excluding this edge because of exclude parameter...");
					continue;
				}

				rprt.Log($"checking if projection intersects this edge...");
				if ( Edges[i].DoesProjectionIntersectEdge_dbg(innerHit.Position, outerHit.Position, out bestEdgeIntersectHit, ref rprt, true) )
				{
					//at this point, check if the dot of the projection and this edge's cross vector are opposite.
					//if so, keep checking the next edges

					rprt.Log($"using dot: '{Vector3.Dot(v_projection_flat, Edges[i].v_Cross_flat)}'...");

					if (Vector3.Dot(v_projection_flat, Edges[i].v_Cross_flat) < 0f)
					{
						rprt.Log_And_End_Method($"Projection DOES intesect this edge on an endpoint, and pointed outside the triangle" +
							$". Returning true...",
							"ProjectThroughToPerimeter_dbg");
						perimHit = bestEdgeIntersectHit;
						return true;
					}
					else
					{
						rprt.Log($"Projection Does intersect this edge on an endpoint, but it's pointed inside the triangle...",
							$"Caching this edge as best, but continuing in case there's a better choice...");
						bestEdgeIntersectHit = perimHit;
					}					
				}
			}

			if( bestEdgeIntersectHit != LNX_NavmeshHit.None )
			{
				rprt.Log_And_End_Method($"End of iterating through edges. Logged best edge intersect hit of '{bestEdgeIntersectHit}'. Returning true.",
					"ProjectThroughToPerimeter_dbg");
				perimHit = bestEdgeIntersectHit;
				return true;
			}

			rprt.Log($"End of method. returning false...");

			rprt.EndMethod("ProjectThroughToPerimeter_dbg()");
			return false;

		}
		
		#endregion

		#region MODIFICATION ----------------------------------------------------


		/// <summary>
		/// Movies a vertex belonging to this triangle in a managed fashion. Sets appropriate flags and 
		/// does what's necessary after a movement has been made.
		/// </summary>
		/// <param name="vertIndex"></param>
		/// <param name="pos"></param>
		/// <param name="positionIsAbsolute"></param>
		public void MoveVert_managed( LNX_NavMesh nm, int vertIndex, Vector3 pos, bool positionIsAbsolute = false )
		{
			Verts[vertIndex].V_Position = (positionIsAbsolute ? pos : Verts[vertIndex].V_Position + pos);

			dirtyFlag_repositionedVert = true;

			for ( int i = 0; i < nm.Triangles.Length; i++ )
			{
				if( i == index_inCollection )
				{
					continue;
				}

				if( AmAdjacentToTri(nm.Triangles[i]) )
				{
					nm.Triangles[i].ForceMarkDirty();
				}
			}
		}

		public void ForceMarkDirty()
		{
			dirtyFlag_repositionedVert = true;
		}

		public void ChangeIndex_action(int newIndex)
		{
			index_inCollection = newIndex;

			Verts[0].TriIndexChanged(newIndex);
			Verts[1].TriIndexChanged(newIndex);
			Verts[2].TriIndexChanged(newIndex);

			Edges[0].TriIndexChanged(newIndex);
			Edges[1].TriIndexChanged(newIndex);
			Edges[2].TriIndexChanged(newIndex);
		}

		public void ClearModifications()
		{
			//Debug.Log($"tri[{index_inCollection}].ClearModifications()");

			if( Verts[0].AmModified )
			{
				Verts[0].V_Position = Verts[0].OriginalPosition;
				Debug.LogWarning($"vert 0 was modified");
			}
			if ( Verts[1].AmModified )
			{
				Verts[1].V_Position = Verts[1].OriginalPosition;
				Debug.LogWarning($"vert 1 was modified");

			}
			if ( Verts[2].AmModified )
			{
				Verts[2].V_Position = Verts[2].OriginalPosition;
				Debug.LogWarning($"vert 2 was modified");

			}
		}
		#endregion

		#region GETTERS/IDENTIFIERS -----------------------------------------------------
		public bool VertsEqual(LNX_Triangle otherTri)
		{
			if (
				otherTri.Verts == null || otherTri.Verts.Length != 3 || Verts == null || Verts.Length != 3
			)
			{
				return false;
			}

			if (
				otherTri.GetVertIndextAtPosition(Verts[0].V_Position, false) == -1 ||
				otherTri.GetVertIndextAtPosition(Verts[1].V_Position, false) == -1 ||
				otherTri.GetVertIndextAtPosition(Verts[2].V_Position, false) == -1
			)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Tests if this triangle's original state is a match the supplied triangle's current state.
		/// </summary>
		/// <param name="otherTri"></param>
		/// <returns></returns>
		public bool OriginallyPositionallyMatches(LNX_Triangle otherTri)
		{
			if (
				otherTri.Verts == null || otherTri.Verts.Length != 3 || Verts == null || Verts.Length != 3
			)
			{
				return false;
			}

			if (
				otherTri.GetVertIndextAtOriginalPosition(Verts[0].OriginalPosition) == -1 ||
				otherTri.GetVertIndextAtOriginalPosition(Verts[1].OriginalPosition) == -1 ||
				otherTri.GetVertIndextAtOriginalPosition(Verts[2].OriginalPosition) == -1
			)
			{
				return false;
			}

			return true;
		}
		public bool OriginallyPositionallyMatches( LNX_AtomicTriangle otherTri ) //todo: this isn't correct, look inside...
		{
			if
			(
				Verts[0].OriginalPosition == otherTri.VertPos0_orig && //this actually needs to do something like LNX_Triangle.GetVertIndextAtOriginalPosition()
				Verts[1].OriginalPosition == otherTri.VertPos1_orig &&
				Verts[2].OriginalPosition == otherTri.VertPos2_orig
			)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// For checking if another triangle has equal values.
		/// </summary>
		/// <param name="tri"></param>
		/// <returns></returns>
		public bool ValueEquals(LNX_Triangle tri)
		{
			if (!VertsEqual(tri))
			{
				return false;
			}

			if (
				V_Center != tri.V_Center ||
				v_sampledNormal != tri.v_sampledNormal ||
				Perimeter != tri.Perimeter ||
				AreaIndex != tri.AreaIndex ||
				LongestEdgeLength != tri.LongestEdgeLength ||
				ShortestEdgeLength != tri.ShortestEdgeLength
			)
			{
				return false;
			}

			return true;
		}

		public LNX_Vertex GetVertexAtOriginalPosition( Vector3 pos, bool includeFlattened = true )
		{
			int indx = GetVertIndextAtOriginalPosition(pos);

			if( indx > -1 )
			{
				return Verts[indx];
			}

			return null;
		}

		/// <summary>
		/// Returns any vertices owned by this triangle that exist at the supplied position.
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public int GetVertIndextAtPosition( Vector3 pos, bool includeFlattened = true)
		{
			if( includeFlattened )
			{
				Vector3 v = LNX_Utils.FlatVector( pos, v_SurfaceNormal_cached );
				if ( Verts[0].V_flattenedPosition == v )
				{
					return 0;
				}
				else if ( Verts[1].V_flattenedPosition == v )
				{
					return 1;
				}
				else if ( Verts[2].V_flattenedPosition == v )
				{
					return 2;
				}
			}
			else
			{
				if (Verts[0].V_Position == pos)
				{
					return 0;
				}
				else if (Verts[1].V_Position == pos)
				{
					return 1;
				}
				else if (Verts[2].V_Position == pos)
				{
					return 2;
				}
			}

			return -1;
		}

		public LNX_Vertex GetVertexAtCurrentPosition( Vector3 pos )
		{
			int indx = GetVertIndextAtPosition(pos);

			if( indx < 0 )
			{
				return null;
			}
			else
			{
				return Verts[indx];
			}
		}

		/// <summary>
		/// Returns any vertices owned by this triangle that originally existed at the supplied position before 
		/// being modified.
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public int GetVertIndextAtOriginalPosition(Vector3 pos)
		{
			if (Verts[0].OriginalPosition == pos)
			{
				return 0;
			}
			else if (Verts[1].OriginalPosition == pos)
			{
				return 1;
			}
			else if (Verts[2].OriginalPosition == pos)
			{
				return 2;
			}

			return -1;
		}

		public LNX_Vertex GetClosestVertToPosition( Vector3 pos )
		{
			float runningBestDist = Vector3.Distance( pos, Verts[0].V_Position );
			int runningBestIndx = 0;

			float dTo = Vector3.Distance(pos, Verts[1].V_Position);
			if ( dTo < runningBestDist )
			{
				runningBestDist = dTo;
				runningBestIndx = 1;
			}

			dTo = Vector3.Distance(pos, Verts[2].V_Position);
			if (dTo < runningBestDist)
			{
				runningBestDist = dTo;
				runningBestIndx = 2;
			}

			return Verts[runningBestIndx];
		}

		public bool HasVertAtPosition( Vector3 pos, bool includeFlattened = true )
		{
			if ( GetVertIndextAtPosition(pos, includeFlattened) > -1 )
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		public bool HasVertAtPosition( LNX_Vertex vert, bool includeFlattened = true)
		{
			if( GetVertIndextAtPosition(vert.V_Position, includeFlattened) > -1 )
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool HasVertAtOriginalPosition( Vector3 pos )
		{
			if ( GetVertIndextAtOriginalPosition(pos) > -1 )
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public LNX_Edge GetEdge( Vector3 midPt )
		{
			if( Edges[0].MidPosition == midPt )
			{
				return Edges[0];
			}
			if ( Edges[1].MidPosition == midPt )
			{
				return Edges[1];
			}
			if ( Edges[2].MidPosition == midPt )
			{
				return Edges[2];
			}

			return null;
		}

		/// <summary>
		/// Returns a point that describes the "lowest" point on the triangle with respect to the SurfaceOrientation of the navmesh.
		/// </summary>
		/// <returns></returns>
		private Vector3 GetProjectionBase()
		{
			float genrtdX = V_Center.x;
			float genrtdY = V_Center.y;
			float genrtdZ = V_Center.z;

			if( v_SurfaceNormal_cached == Vector3.up || v_SurfaceNormal_cached == Vector3.down )
			{
				genrtdY = Mathf.Min(Verts[0].V_Position.y, Mathf.Min(Verts[1].V_Position.y, Verts[2].V_Position.y) );
			}
			else if( v_SurfaceNormal_cached == Vector3.forward || v_SurfaceNormal_cached == Vector3.back )
			{
				genrtdZ = Mathf.Min( Verts[0].V_Position.z, Mathf.Min(Verts[1].V_Position.z, Verts[2].V_Position.z) );
			}
			else if ( v_SurfaceNormal_cached == Vector3.right || v_SurfaceNormal_cached == Vector3.left )
			{
				genrtdX = Mathf.Min(Verts[0].V_Position.x, Mathf.Min(Verts[1].V_Position.x, Verts[2].V_Position.x) );
			}

			return new Vector3 ( genrtdX, genrtdY, genrtdZ );
		}

		public bool IsPositionOnAnyEdge(Vector3 pos, bool flatten = false)
		{
			if (flatten)
			{
				pos = LNX_Utils.FlatVector(pos, v_SurfaceNormal_cached);
			}

			if (Edges[0].DoesPositionLieOnEdge(pos))
			{
				return true;
			}
			else if (Edges[1].DoesPositionLieOnEdge(pos))
			{
				return true;
			}
			else if (Edges[2].DoesPositionLieOnEdge(pos))
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Returns true if the tri with the supplied index is touching this triangle at any vertex.
		/// </summary>
		/// <param name="indx"></param>
		/// <returns></returns>
		public bool AmAdjacentToTri( LNX_Triangle tri ) //todo: unit test
		{
			if( tri.index_inCollection == Index_inCollection )
			{
				Debug.LogWarning($"LNX WARNING! {nameof(LNX_Triangle)}.{nameof(AmAdjacentToTri)} was passed it's own index! Was this intentional?");
				return true;
			}

			if( GetNumberOfSharedVerts(tri) > 0 )
			{
				return true;
			}

			return false;
		}

		public bool AmAdjacentToVert( LNX_Vertex vert )
		{
			return HasVertAtPosition( vert.V_Position );
		}

		public int GetNumberOfSharedVerts(LNX_Triangle tri )
		{
			if (tri.index_inCollection == index_inCollection)
			{
				return 3;
			}

			int count = 0;
			if( Verts[0].SharesVertSpaceWithTri(tri) )
			{
				count++;
			}
			if ( Verts[1].SharesVertSpaceWithTri(tri) )
			{
				count++;
			}
			if ( Verts[2].SharesVertSpaceWithTri(tri) )
			{
				count++;
			}

			return count;
		}

		public bool HasSharedEdgeWith(int triIndex) //note: Each tri can only share a single edge with another single tri.
		{
			for ( int i = 0; i < 3; i++ )
			{
				if(Edges[i].SharedEdgeCoordinate.TrianglesIndex == triIndex )
				{
					return true;
				}
			}

			return false;
		}
		
		public bool HasIndexInKnownFullyVisibleList( int triIndex )
		{
			if( triIndex == index_inCollection )
			{
				return true;
			}

			if( indices_knownFullyVisibleTriangles != null &&indices_knownFullyVisibleTriangles.Length > 0 )
			{
				for ( int i = 0; i < indices_knownFullyVisibleTriangles.Length; i++ )
				{
					if( indices_knownFullyVisibleTriangles[i] == triIndex )
					{
						return true;
					}
				}
			}

			return false;
		}

		#endregion

		public void LoadWithSerializedData(LNX_SerializedTriData data )
		{
			indices_knownFullyVisibleTriangles = data.KnownFullyVisibleTriangleIndices;
		}

		#region HELPERS --------------------------------------------------
		public string GetCurrentInfoString(LNX_NavMesh nm)
		{
			string completelyVisibleTrisSTring = $"";

			if ( indices_knownFullyVisibleTriangles == null )
			{
				completelyVisibleTrisSTring += $"indices_knownFullyVisibleTriangles collection is null\n";
			}
			else
			{
				completelyVisibleTrisSTring = $"indices_knownFullyVisibleTriangles count: '{indices_knownFullyVisibleTriangles.Length}'\n";
				for( int i = 0; i < indices_knownFullyVisibleTriangles.Length; i++ )
				{
					completelyVisibleTrisSTring += $"[{i}]: '{indices_knownFullyVisibleTriangles[i]}'\n";
				}
			}

			return $"Triangle.{nameof(SayCurrentInfo)}()...\n" +
				$"{nameof(index_inCollection)}: '{index_inCollection}'\n" +
				$"{nameof(V_Center)}: '{V_Center}'\n" +
				$"{nameof(MeshIndex_trianglesStart)}: '{MeshIndex_trianglesStart}'\n\n" +
				$"NORMALS-----------------------\n" +
				$"{nameof(v_sampledNormal)}: '{v_sampledNormal}'\n" +
				$"{nameof(V_PlaneFaceNormal)}: '{V_PlaneFaceNormal}'\n" +
				$"{nameof(AmKinked)}: '{AmKinked}'\n" +
				$"\n" +
				$"PROPERTIES--------------------\n" +
				$"{nameof(V_FlattenedCenter)}: '{V_FlattenedCenter}'\n" +
				$"{nameof(FaceRotation)}: '{FaceRotation}'\n" +
				$"{nameof(Slope)}: '{Slope}'\n" +
				$"projection base: '{GetProjectionBase()}'\n\n" +
				$"Relational---------------------\n" +

				$"{nameof(indices_knownFullyVisibleTriangles)} report...\n" +
				$"{completelyVisibleTrisSTring}\n\n" +

				$"Vertices---------------------\n" +
				$"{Verts[0].GetCurrentInfoString()}\n" +
				$"{Verts[1].GetCurrentInfoString()}\n" +
				$"{Verts[2].GetCurrentInfoString()}\n\n" +

				$"Edges---------------------\n" +
				$"{Edges[0].GetCurrentInfoString(nm)}\n" +
				$"{Edges[1].GetCurrentInfoString(nm)}\n" +
				$"{Edges[2].GetCurrentInfoString(nm)}\n" +
				$"";
		}

		public void SayCurrentInfo(LNX_NavMesh nm)
		{
			Debug.Log( GetCurrentInfoString(nm) );
		}

		public string GetAnomolyString( LNX_NavMesh nm )
		{
			string returnString = string.Empty;

			if( Index_inCollection < 0 )
			{
				returnString += $"{nameof(Index_inCollection)}: '{Index_inCollection}'\n";
			}

			if (MeshIndex_trianglesStart < 0)
			{
				returnString += $"{nameof(MeshIndex_trianglesStart)}: '{MeshIndex_trianglesStart}'\n";
			}

			bool correctNumberOfVerts = true;

			if ( Verts == null || Verts.Length == 0 )
			{
				returnString += $"{nameof(Verts)} collection not set\n";
				correctNumberOfVerts = false;
			}
			else if( Verts.Length != 2 )
			{
				correctNumberOfVerts = false;
			}

			bool correctNumberOfEdges = true;
			if ( Edges == null || Edges.Length == 0)
			{
				returnString += $"{nameof(Edges)} collection not set\n";
				correctNumberOfEdges = false;
			}
			else if(  Edges.Length != 3 )
			{
				correctNumberOfEdges = false;
			}

			if (AmKinked)
			{
				returnString += $"{nameof(AmKinked)} is true\n";
			}

			//Note: Add more checks as you go...

			#region VERTS -------------------------------------------
			if( correctNumberOfVerts )
			{
				string v0_string = Verts[0].GetAnomolyString(nm );
				string v1_string = Verts[1].GetAnomolyString(nm );
				string v2_string = Verts[2].GetAnomolyString(nm );

				if 
				(
					!string.IsNullOrWhiteSpace(v0_string) || 
					!string.IsNullOrWhiteSpace(v1_string) ||
					!string.IsNullOrWhiteSpace(v2_string)
				)
				{
					returnString += $"Anomoly found in verts!\n";

					if( !string.IsNullOrWhiteSpace(v0_string) )
					{
						returnString += $"Vert0---\n" +
							$"{v0_string}\n";
					}
					if (!string.IsNullOrWhiteSpace(v1_string))
					{
						returnString += $"Vert1---\n" +
							$"{v1_string}\n";
					}
					if (!string.IsNullOrWhiteSpace(v2_string))
					{
						returnString += $"Vert2---\n" +
							$"{v2_string}\n";
					}
				}
			}
			else
			{
				returnString += $"this triangle does NOT have the correct number of verts...\n";
			}

			#endregion

			#region EDGES -------------------------------------------
			if (correctNumberOfEdges)
			{
				string e0_string = Edges[0].GetAnomolyString(nm);
				string e1_string = Edges[1].GetAnomolyString(nm);
				string e2_string = Edges[2].GetAnomolyString(nm);

				if
				(
					!string.IsNullOrWhiteSpace(e0_string) ||
					!string.IsNullOrWhiteSpace(e1_string) ||
					!string.IsNullOrWhiteSpace(e2_string)
				)
				{
					returnString += $"Anomoly found in Edges!\n";

					if (!string.IsNullOrWhiteSpace(e0_string))
					{
						returnString += $"Edge0---\n" +
							$"{e0_string}\n";
					}
					if (!string.IsNullOrWhiteSpace(e1_string))
					{
						returnString += $"Edge1---\n" +
							$"{e1_string}\n";
					}
					if (!string.IsNullOrWhiteSpace(e2_string))
					{
						returnString += $"Edge2---\n" +
							$"{e2_string}\n";
					}
				}
			}
			else
			{
				returnString += $"this triangle does NOT have the correct number of edges...\n";
			}
			#endregion
			return returnString;
		}

		public string GetRelationalString()
		{
			return $"LNX_Triangle[{Index_inCollection}].{nameof(GetRelationalString)}()\n" +
				$"Verts----\n" +
				$"vert0\n" +
				$"{Verts[0].GetRelationalString()}\n" +
				$"vert1\n" +
				$"{Verts[1].GetRelationalString()}\n" +
				$"vert2\n" +
				$"{Verts[2].GetRelationalString()}\n" +

				$"\nEdges----\n" +
				$"edge0\n" +
				$"{Edges[0].GetRelationalString()}\n" +
				$"edge1\n" +
				$"{Edges[1].GetRelationalString()}\n" +
				$"edge2\n" +
				$"{Edges[2].GetRelationalString()}\n" +
				$"";
		}
		#endregion

		#region OPERATORS ==================================================
		public override string ToString()
		{
			return $"Tri{index_inCollection}";

			//return $"Tri{index_inCollection} at {V_Center}";
		}

		#endregion
	}
}