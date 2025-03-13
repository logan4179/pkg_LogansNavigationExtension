
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Windows;

namespace LogansNavigationExtension
{
    public class LNX_NavMesh : MonoBehaviour
    {
		public static LNX_NavMesh Instance;

		public LNX_Triangle[] Triangles;

		public Mesh _Mesh;

		public List<LNX_TriangleModification> triModifications;

		/// <summary>Stores the largest/smallest X, Y, and Z value of the navmesh. Elements 0 and 1 are lowest and 
		/// hightest X, elements 2 and 3 are lowest and highest Y, and elements 4 and 5 are lowest and highest z.</summary>
		public float[] Bounds;

		/// <summary>Stores the largest/smallest points defining the bounds of a navmesh. Elements 0-3 form the lower horizontal square of the 
		/// box, while 4-6 form the higher horizontal square of the bounding box. These theoretical boxes each run clockwise. Element 0 
		/// will be the lowest/most-negative value point, and element 4 will be the most positive value point</summary>
		public Vector3[] V_Bounds;

		public Vector3 V_BoundsCenter;
		public Vector3 V_BoundsSize;
		/// <summary>
		/// Longest distance from the bounds center to any corner on the bounding box. This is used as an efficiency value 
		/// in order to short-circuit (return early) from certain methods that don't need to run further logic based on the 
		/// value of this threshold..
		/// </summary>
		public float BoundsContainmentDistanceThreshold = -1;

        public string LayerMaskName;
        private int cachedLayerMask;
		public int CachedLayerMask => cachedLayerMask;

		//[Header("flags")]

		[Header("VISUAL/DEBUG")]
		public Color color_mesh;

		private void Awake()
		{
			Instance = this;
		}

		void Start()
        {
			Debug.Log("mgr start");
        }

		#region Triangle fetchers ------------------------------------------------------
		public LNX_Triangle[] GetAdjacentTriangles( LNX_Triangle tri )
		{
			LNX_Triangle[] triArray = new LNX_Triangle[tri.AdjacentTriIndices.Length];

			for ( int i = 0; i < tri.AdjacentTriIndices.Length; i++ )
			{
				triArray[i] = Triangles[tri.AdjacentTriIndices[i]];
			}

			return triArray;
		}

		public LNX_Triangle GetTriangle( LNX_Edge edge )
		{
			return Triangles[edge.MyCoordinate.TriIndex];
		}
		#endregion

		#region Vertex fetchers -----------------------------------------------------------------------
		public LNX_Vertex GetVertexAtCoordinate( LNX_ComponentCoordinate coord )
		{
			string dbgMe = $"GetVertexAtCoordinate({coord})\n";
			if( Triangles == null || Triangles.Length <= 0 || coord.TriIndex > Triangles.Length-1 || coord.ComponentIndex > 2 || coord.ComponentIndex < 0 )
			{
				dbgMe += "returning null...";
				//Debug.Log(dbgMe);
				return null;
			}
			else
			{
				dbgMe += $"found vert";
				//Debug.Log(dbgMe);

				return Triangles[coord.TriIndex].Verts[coord.ComponentIndex];
			}
		}
		public LNX_Vertex GetVertexAtCoordinate( int triIndex, int componentIndex )
		{
			if ( Triangles == null || Triangles.Length <= 0 || triIndex > Triangles.Length - 1 || componentIndex > 2 || componentIndex < 0 )
			{
				return null;
			}
			else
			{
				return Triangles[triIndex].Verts[componentIndex];
			}
		}

		public List<LNX_Vertex> GetVerticesAtCoordinate( LNX_ComponentCoordinate coord )
		{
			List<LNX_Vertex> returnList = new List<LNX_Vertex>();

			if (Triangles == null || Triangles.Length <= 0 || coord.TriIndex > Triangles.Length - 1 || coord.ComponentIndex > 2 || coord.ComponentIndex < 0)
			{
				return null;
			}
			else
			{
				LNX_Vertex vert = Triangles[coord.TriIndex].Verts[coord.ComponentIndex];
				returnList.Add( vert );

				for ( int i = 0; i < vert.SharedVertexCoordinates.Length; i++ ) 
				{
					returnList.Add( Triangles[vert.SharedVertexCoordinates[i].TriIndex].Verts[vert.SharedVertexCoordinates[i].TriIndex] );
				}
			}

			return returnList;
		}
		#endregion

		#region Edge Fetchers --------------------------------------------------
		public LNX_Edge GetEdgeAtCoordinate( LNX_ComponentCoordinate coord )
		{
			if (Triangles == null || Triangles.Length <= 0 || coord.TriIndex > Triangles.Length - 1 || coord.ComponentIndex > 2 || coord.ComponentIndex < 0)
			{
				return null;
			}
			else
			{
				return Triangles[coord.TriIndex].Edges[coord.ComponentIndex];
			}
		}

		public LNX_Edge GetEdgeAtCoordinate( int triIndex, int componentIndex )
		{
			if ( Triangles == null || Triangles.Length <= 0 || triIndex > Triangles.Length - 1 || componentIndex > 2 || componentIndex < 0 )
			{
				return null;
			}
			else
			{
				return Triangles[triIndex].Edges[componentIndex];
			}
		}
		#endregion


		public int[] dbg_Areas;
		public Vector3[] dbg_Vertices;
		public int[] dbg_Indices;
		[ContextMenu("z - FetchTriangulation()")]
		public void FetchTriangulation()
		{
			NavMeshTriangulation tringltn = NavMesh.CalculateTriangulation();

			Debug.Log($"Calculated triangulation with '{tringltn.areas.Length}' areas, '{tringltn.vertices.Length}', " +
				$"'{tringltn.indices.Length}' indices.");

			FetchTriangulation( tringltn );
		}
		public void FetchTriangulation( NavMeshTriangulation tringltn )
		{
			if ( string.IsNullOrEmpty(LayerMaskName) )
			{
				Debug.LogError("LogansNavmeshExtender ERROR! You need to set which layer mask to cast against.");
				return;
			}
			cachedLayerMask = LayerMask.GetMask( LayerMaskName );

			_Mesh = new Mesh();
			//Vector3[] meshVrts = new Vector3[tringltn.vertices.Length];
			Vector3[] meshVrts = tringltn.vertices;

			Vector3[] nrmls = new Vector3[tringltn.vertices.Length];

			// if this is a re-fetch, and we have modifications to consider...
			if ( Triangles != null && Triangles.Length > 0 && triModifications != null && triModifications.Count > 0 ) 
			{
				Debug.Log($"Triangle collection exists with modifications...");
				LNX_Triangle[] newTriCollection = new LNX_Triangle[tringltn.areas.Length];

				for ( int i = 0; i < newTriCollection.Length; i++ )
				{
					newTriCollection[i] = new LNX_Triangle( i, tringltn, cachedLayerMask );

					for ( int j = 0; j < triModifications.Count; j++ )
					{
						if( triModifications[j] != null && triModifications[j].OriginalTriangleState.ValueEquals(newTriCollection[i]) )
						{
							//Debug.Log($"Tri '{i}' using saved modification at old index '{triModifications[j].OriginalStateIndex}' in triangle construction...");
							newTriCollection[i] = new LNX_Triangle( triModifications[j], Triangles,  i, tringltn );
							
							// Correct the positioning of the verts of the modified triangle....
							meshVrts[newTriCollection[i].Verts[0].PositionInOriginalTriangulation] = newTriCollection[i].Verts[0].Position;
							meshVrts[newTriCollection[i].Verts[1].PositionInOriginalTriangulation] = newTriCollection[i].Verts[1].Position;
							meshVrts[newTriCollection[i].Verts[2].PositionInOriginalTriangulation] = newTriCollection[i].Verts[2].Position;
						}
					}

					if( newTriCollection[i].v_normal != Vector3.zero )
					{
						//Debug.DrawRay( newTriCollection[i].V_center, newTriCollection[i].v_normal );

						nrmls[newTriCollection[i].Verts[0].PositionInOriginalTriangulation] = newTriCollection[i].v_normal;
						nrmls[newTriCollection[i].Verts[1].PositionInOriginalTriangulation] = newTriCollection[i].v_normal;
						nrmls[newTriCollection[i].Verts[2].PositionInOriginalTriangulation] = newTriCollection[i].v_normal;
					}
					else
					{
						nrmls[newTriCollection[i].Verts[0].PositionInOriginalTriangulation] = Vector3.up;
						nrmls[newTriCollection[i].Verts[1].PositionInOriginalTriangulation] = Vector3.up;
						nrmls[newTriCollection[i].Verts[2].PositionInOriginalTriangulation] = Vector3.up;
					}
				}

				for (int i = 0; i < newTriCollection.Length; i++)
				{
					newTriCollection[i].CreateRelationships( newTriCollection );
				}

				Triangles = newTriCollection;
			}
			else
			{
				Debug.Log($"Triangle collection will be made anew...");

				Triangles = new LNX_Triangle[tringltn.areas.Length];
				triModifications = new List<LNX_TriangleModification>();
				meshVrts = tringltn.vertices;

				for ( int i = 0; i < Triangles.Length; i++ )
				{
					Triangles[i] = new LNX_Triangle( i, tringltn, cachedLayerMask );

					if ( Triangles[i].v_normal != Vector3.zero )
					{
						//Debug.DrawRay( Triangles[i].V_center, Triangles[i].v_normal, Color.cyan, 2f );

						nrmls[Triangles[i].Verts[0].PositionInOriginalTriangulation] = Triangles[i].v_normal;
						nrmls[Triangles[i].Verts[1].PositionInOriginalTriangulation] = Triangles[i].v_normal;
						nrmls[Triangles[i].Verts[2].PositionInOriginalTriangulation] = Triangles[i].v_normal;
					}
					else
					{
						nrmls[Triangles[i].Verts[0].PositionInOriginalTriangulation] = Vector3.up;
						nrmls[Triangles[i].Verts[1].PositionInOriginalTriangulation] = Vector3.up;
						nrmls[Triangles[i].Verts[2].PositionInOriginalTriangulation] = Vector3.up;
					}
				}

				for ( int i = 0; i < Triangles.Length; i++ )
				{
					Triangles[i].CreateRelationships( Triangles );
				}
			}

			#region FINISH THE MESH--------------------------------
			Debug.Log($"Finishing the mesh representation. Assigning '{meshVrts.Length}' verts, '{tringltn.indices.Length}' tris, and '{nrmls.Length}' normals...");
			_Mesh.vertices = meshVrts;
			_Mesh.triangles = tringltn.indices; //apparently this MUST come after setting the vertices. If you try to set triangles before vertices, it will throw an error

			_Mesh.normals = nrmls;
			#endregion

			dbg_Areas = tringltn.areas;
			dbg_Vertices = tringltn.vertices;
			dbg_Indices = tringltn.indices;

			CalculateBounds();
		}

		[ContextMenu("z call RefeshMesh()")]
		public void RefeshMesh()
		{
			for ( int i = 0; i < Triangles.Length; i++ )
			{
				Triangles[i].RefreshTriangle( this, false );
			}

			CalculateBounds();
		}

		public void MoveVert_managed( LNX_Vertex vert, Vector3 pos )
		{
			bool foundMod = false;

			if( triModifications.Count > 0 )
			{
				for( int i = 0; i < triModifications.Count; i++ )
				{
					if ( vert.MyCoordinate.TriIndex == triModifications[i].OriginalTriangleState.Index_parallelWithParentArray )
					{
						foundMod = true;
					}
				}
			}
			if( !foundMod )
			{
				triModifications.Add( new LNX_TriangleModification(Triangles[vert.MyCoordinate.TriIndex]) );
				Debug.Log("made new modification");
			}

			Triangles[vert.MyCoordinate.TriIndex].MoveVert_managed( this, vert.MyCoordinate.ComponentIndex, pos );

			Vector3[] tmpVrts = _Mesh.vertices; //note: I can't get it to update the mesh if I only change the relevant vertex, it seems like I MUST create and assign a whole new array.
			tmpVrts[vert.PositionInOriginalTriangulation] = vert.Position + pos;

			_Mesh.vertices = tmpVrts; //apparently you have to assign to the mesh in this manner in order to make this update (apparently I can't just change one of the existing vertices elements)...
			
		}

		public void ClearModifications()
		{

			if( triModifications == null || triModifications.Count <= 0 )
			{
				Debug.LogWarning($"no existing modifications on LNX_Navmesh");
				return;
			}

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				for( int j = 0; j < triModifications.Count; j++ )
				{
					if( i == triModifications[j].OriginalTriangleState.Index_parallelWithParentArray )
					{
						Debug.Log($"matched modification at Triangles[{i}] and mod[{j}]");
						Triangles[i].AdoptValues( triModifications[j].OriginalTriangleState );
					}
				}
			}

			triModifications = new List<LNX_TriangleModification>();

			//todo: now need to put modified triangles back to where they were before...
		}

		public void CalculateBounds()
		{
			Bounds = new float[6] 
			{ 
				float.MaxValue, float.MinValue, 
				float.MaxValue, float.MinValue, 
				float.MaxValue, float.MinValue
			};

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				for ( int j = 0; j < 3; j++ )
				{
					if ( Triangles[i].Verts[j].Position.x < Bounds[0] )
					{
						Bounds[0] = Triangles[i].Verts[j].Position.x;
					}
					else if ( Triangles[i].Verts[j].Position.x > Bounds[1] )
					{
						Bounds[1] = Triangles[i].Verts[j].Position.x;
					}

					if ( Triangles[i].Verts[j].Position.y < Bounds[2] )
					{
						Bounds[2] = Triangles[i].Verts[j].Position.y;
					}
					else if ( Triangles[i].Verts[j].Position.y > Bounds[3])
					{
						Bounds[3] = Triangles[i].Verts[j].Position.y;
					}

					if ( Triangles[i].Verts[j].Position.z < Bounds[4] )
					{
						Bounds[4] = Triangles[i].Verts[j].Position.z;
					}
					else if ( Triangles[i].Verts[j].Position.z > Bounds[5] )
					{
						Bounds[5] = Triangles[i].Verts[j].Position.z;
					}
				}
			}

			V_Bounds = new Vector3[8]
			{
				new Vector3(Bounds[0], Bounds[2], Bounds[4]), //most negative point
				new Vector3(Bounds[0], Bounds[2], Bounds[5]),
				new Vector3(Bounds[1], Bounds[2], Bounds[5]),
				new Vector3(Bounds[1], Bounds[2], Bounds[4]),
				new Vector3(Bounds[1], Bounds[3], Bounds[5]), //most positive point
				new Vector3(Bounds[1], Bounds[3], Bounds[4]),
				new Vector3(Bounds[0], Bounds[3], Bounds[4]),
				new Vector3(Bounds[0], Bounds[3], Bounds[5]),
			};

			V_BoundsCenter = (
				V_Bounds[0] + V_Bounds[1] + V_Bounds[2] + V_Bounds[3] + 
				V_Bounds[4] + V_Bounds[5] + V_Bounds[6] + V_Bounds[7]
				
			) / 8;

			V_BoundsSize = new Vector3(
				Mathf.Abs(Bounds[0] - Bounds[1]),
				Mathf.Abs(Bounds[2] - Bounds[3]),
				Mathf.Abs(Bounds[4] - Bounds[5])
			);

			BoundsContainmentDistanceThreshold = Mathf.Max
			(
				Vector3.Distance(V_BoundsCenter, V_Bounds[0]),
				Vector3.Distance(V_BoundsCenter, V_Bounds[4])
			);
		}

		/*[SerializeField]*/ private string dbgCalculatePath;
		public bool CalculatePath( Vector3 startPos_passed, Vector3 endPos_passed, float maxDistance )
		{
			LNX_ProjectionHit lnxHit = new LNX_ProjectionHit();

			if( SamplePosition(startPos_passed, out lnxHit, maxDistance) )
			{
				startPos_passed = lnxHit.HitPosition;
				dbgCalculatePath += $"SamplePosition() hit startpos\n";
			}
			else
			{
				dbgCalculatePath += $"SamplePosition() did NOT hit startpos.\n";
				return false; //todo: returning a boolean is newly added. Make sure this return boolean is being properly used...
			}

			if ( SamplePosition(endPos_passed, out lnxHit, maxDistance) )
			{
				endPos_passed = lnxHit.HitPosition;
				dbgCalculatePath += $"SamplePosition() hit endpos\n";
			}
			else
			{
				dbgCalculatePath += $"SamplePosition() did NOT hit endpos.\n";
				return false; //todo: returning a boolean is newly added. Make sure this return boolean is being properly used...
			}

			return true;
		}

		/// <summary>
		/// Returns true if the supplied position is within the projection of any triangle on the navmesh, 
		/// projected along it's normal.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="closestPt">Closest point to the supplied position on the surface of the Navmesh</param>
		/// <returns></returns>
		public int AmWithinNavMeshProjection( Vector3 pos, out Vector3 closestPt )
        {
			DbgSamplePosition = $"Searching through '{Triangles.Length}' tris...\n";
			int rtrnIndx = -1;
			float runningClosestDist = float.MaxValue;

			Vector3 currentPt = Vector3.zero;
			closestPt = Vector3.zero;

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				DbgSamplePosition += $"i: '{i}'....................\n";
				LNX_Triangle tri = Triangles[i];

				if ( tri.IsInShapeProjectAlongNormal(pos, out currentPt) )
				{
					DbgSamplePosition += $"found AM in shape project at '{currentPt}'...\n";
					//note: The reason I'm not immediately returning this tri here is because concievably
					// you could have two navmesh polys "on top of each other", (IE: in line with
					// each other's normals), which would result in more than one tri considering
					// this point to be within it's bounds, and I need to decide which one is
					// the better option...

				    if ( Vector3.Distance(pos, currentPt) < runningClosestDist )
				    {
					    closestPt = currentPt;
					    runningClosestDist = Vector3.Distance( pos, closestPt );
					    rtrnIndx = i;
				    }
				}
			}

			DbgSamplePosition += $"finished. returning: '{rtrnIndx}' with pt: '{closestPt}'\n";
			return rtrnIndx;
		}

        [SerializeField, HideInInspector] private string DbgSamplePosition;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="hit"></param>
		/// <param name="maxDistance"></param>
		/// <returns></returns>
        public bool SamplePosition( Vector3 pos, out LNX_ProjectionHit hit, float maxDistance )
        {
            DbgSamplePosition = $"Searching through '{Triangles.Length}' tris...\n";

			if( Vector3.Distance(V_BoundsCenter, pos) > (maxDistance + BoundsContainmentDistanceThreshold) )
			{
				DbgSamplePosition += $"distance threshold short circuit";
				hit = LNX_ProjectionHit.None;
				return false;
			}

            float runningClosestDist = float.MaxValue;
            Vector3 currentPt = Vector3.zero;
			hit.HitPosition = Vector3.zero;
			hit.Index_intersectedTri = -1;

            for ( int i = 0; i < Triangles.Length; i++ )
            {
                DbgSamplePosition += $"i: '{i}'....................\n";
                LNX_Triangle tri = Triangles[i];

				if ( tri.IsInShapeProjectAlongNormal(pos, out currentPt) )
				{
                    DbgSamplePosition += $"found AM in shape project at '{currentPt}'...\n";
					//note: The reason I'm not immediately returning this tri here is because concievably
					// you could have two navmesh polys "on top of each other", (IE: in line with
					// each other's normals), which would result in more than one tri considering
					// this point to be within it's bounds, and you need to decide which one is
					// the better option...
				}
                else
                {
					DbgSamplePosition += $"found am NOT in shape project...\n";

					currentPt = tri.ClosestPointOnPerimeter( pos );
				}

				if ( Vector3.Distance(pos, currentPt) < runningClosestDist )
				{
					hit.HitPosition = currentPt;
					runningClosestDist = Vector3.Distance( pos, hit.HitPosition );
					hit.Index_intersectedTri = i;
				}
            }

            DbgSamplePosition += $"finished. returning: '{hit.Index_intersectedTri}' with pt: '{hit.HitPosition}'\n";

            if( runningClosestDist <= maxDistance )
			{
				return true;
			}
			else
			{
				return false;
			}
        }
	}
}