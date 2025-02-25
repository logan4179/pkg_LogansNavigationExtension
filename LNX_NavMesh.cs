
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

				for ( int i = 0; i < vert.SharedVertexCoordinates.Count; i++ ) 
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


		public int[] testAreas;
		public Vector3[] testVertices;
		public int[] testIndices;
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

			Triangles = new LNX_Triangle[tringltn.areas.Length];

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				Triangles[i] = new LNX_Triangle( i, tringltn, cachedLayerMask );
			}

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				Triangles[i].CreateRelationships( Triangles );
			}

			testAreas = tringltn.areas;
			testVertices = tringltn.vertices;
			testIndices = tringltn.indices;

			CalculateBounds();
		}

		[ContextMenu("z call RefeshMesh()")]
		public void RefeshMesh()
		{
			for (int i = 0; i < Triangles.Length; i++)
			{
				Triangles[i].RefreshTriangle( this, false );
			}

			CalculateBounds();
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
			DBG_GetClosestTri = $"Searching through '{Triangles.Length}' tris...\n";
			int rtrnIndx = -1;
			float runningClosestDist = float.MaxValue;

			Vector3 currentPt = Vector3.zero;
			closestPt = Vector3.zero;

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				DBG_GetClosestTri += $"i: '{i}'....................\n";
				LNX_Triangle tri = Triangles[i];

				if ( tri.IsInShapeProjectAlongNormal(pos, out currentPt) )
				{
					DBG_GetClosestTri += $"found AM in shape project at '{currentPt}'...\n";
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

			DBG_GetClosestTri += $"finished. returning: '{rtrnIndx}' with pt: '{closestPt}'\n";
			return rtrnIndx;
		}

        [SerializeField] private string DBG_GetClosestTri;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="hit"></param>
		/// <param name="maxDistance"></param>
		/// <returns></returns>
        public bool SamplePosition( Vector3 pos, out LNX_ProjectionHit hit, float maxDistance )
        {
            DBG_GetClosestTri = $"Searching through '{Triangles.Length}' tris...\n";

			if( Vector3.Distance(V_BoundsCenter, pos) > (maxDistance + BoundsContainmentDistanceThreshold) )
			{
				DBG_GetClosestTri += $"distance threshold short circuit";
				hit = LNX_ProjectionHit.None;
				return false;
			}

            float runningClosestDist = float.MaxValue;
            Vector3 currentPt = Vector3.zero;
			hit.HitPosition = Vector3.zero;
			hit.Index_intersectedTri = -1;

            for ( int i = 0; i < Triangles.Length; i++ )
            {
                DBG_GetClosestTri += $"i: '{i}'....................\n";
                LNX_Triangle tri = Triangles[i];

				if ( tri.IsInShapeProjectAlongNormal(pos, out currentPt) )
				{
                    DBG_GetClosestTri += $"found AM in shape project at '{currentPt}'...\n";
					//note: The reason I'm not immediately returning this tri here is because concievably
					// you could have two navmesh polys "on top of each other", (IE: in line with
					// each other's normals), which would result in more than one tri considering
					// this point to be within it's bounds, and you need to decide which one is
					// the better option...
				}
                else
                {
					DBG_GetClosestTri += $"found am NOT in shape project...\n";

					currentPt = tri.ClosestPointOnPerimeter( pos );
				}

				if ( Vector3.Distance(pos, currentPt) < runningClosestDist )
				{
					hit.HitPosition = currentPt;
					runningClosestDist = Vector3.Distance( pos, hit.HitPosition );
					hit.Index_intersectedTri = i;
				}
            }

            DBG_GetClosestTri += $"finished. returning: '{hit.Index_intersectedTri}' with pt: '{hit.HitPosition}'\n";

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