using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace LogansNavigationExtension
{
    [System.Serializable]
    public class LNX_NavMeshData
    {
		//public string GUID; //todo: I'm commenting this out, and planning to get rid of this because now that I'm using the GUID rather than saving my own unique id, this GUID exists in unity

		public LNX_SerializedTriData[] Triangles;

		public LNX_ComponentCoordinate[] boundsVerts;
		public LNX_ComponentCoordinate[] boundsEdges;

		public LNX_NavMeshData()
		{
			//GUID = ""; //todo: dws
			Triangles = null;
		}

		public LNX_NavMeshData( LNX_NavMesh nm )
		{
			Debug.Log( $"ctor. colLength: '{nm.Triangles.Length}'");
			//GUID = nm.cachedGUID; //todo: dws

			if ( nm.Triangles == null )
			{
				Triangles = null;
			}

			Triangles = new LNX_SerializedTriData[nm.Triangles.Length];
			
			for (int i = 0; i < nm.Triangles.Length; i++)
			{
				Triangles[i] = new LNX_SerializedTriData(nm.Triangles[i]);
			}

			boundsVerts = nm.BoundsVerts;
			boundsEdges = nm.BoundsEdges;
		}

		public void SupplyWithDataFromNavMesh(LNX_NavMesh nm)
		{
			//GUID = nm.UniqueID; //todo: dws

			if (nm.Triangles == null)
			{
				Debug.LogWarning($"LNX_WARNING! Triangles collection in supplied LNX_NavMesh was null...");
				return;
			}

			if( Triangles.Length != nm.Triangles.Length )
			{
				Triangles = new LNX_SerializedTriData[nm.Triangles.Length];
			}

			for ( int i = 0; i < nm.Triangles.Length; i++ )
			{
				Triangles[i].SupplyWithDataFromTriangle( nm.Triangles[i] );
			}
		}

		public void WriteMeToJSON( string fileName )
		{
			if ( File.Exists(fileName) )
			{
				Debug.LogWarning($"LLM WARNING! You just overwrote an existing file named: '{fileName}'.");
			}

			File.WriteAllText( fileName, JsonUtility.ToJson(this, true) );
		}

		public bool AmValidForUse()
		{
			/*
			if ( string.IsNullOrEmpty(GUID) || string.IsNullOrWhiteSpace(GUID) )
			{
				return false;
			}
			*/
			/*
			if( Triangles == null || Triangles.Length == 0 )
			{
				return false;
			}
			*/
			return true;
		}

		/// <summary>
		/// Calculates whether this object has the up to date values of a supplied LNX_NavMesh.
		/// </summary>
		/// <param name="nm"></param>
		/// <returns></returns>
		public bool MatchesNavmesh(LNX_NavMesh nm)
		{
			/*
			if (GUID != nm.GUID) //todo: dws
			{
				return false;
			}
			*/

			if ((Triangles == null && nm.Triangles != null) || Triangles != null && nm.Triangles == null)
			{
				return false;
			}

			if (Triangles != null && nm.Triangles != null)
			{
				if (Triangles.Length != nm.Triangles.Length)
				{
					return false;
				}

				for (int i = 0; i < Triangles.Length; i++)
				{
					if (!Triangles[i].EqualsTri(nm.Triangles[i]))
					{
						return false;
					}
				}
			}

			return true;
		}
	}

	[System.Serializable]
    public class LNX_SerializedTriData
    {
		//public int Index_inCollection;

		//public LNX_SerializedVertData[] Verts;
		public Vector3 vPos_0;
		public Vector3 vPos_1;
		public Vector3 vPos_2;

        //public LNX_SerializedEdgeData[] Edges;

		public int[] KnownFullyVisibleTriangleIndices;

		public LNX_SerializedTriData( LNX_Triangle tri )
		{
			vPos_0 = tri.Verts[0].V_Position;
			vPos_1 = tri.Verts[1].V_Position;
			vPos_2 = tri.Verts[2].V_Position;

			KnownFullyVisibleTriangleIndices = tri.KnownFullyVisibleTriangleIndices;
		}

		public bool EqualsTri(LNX_Triangle tri)
		{
			if
			( 
				vPos_0 != tri.Verts[0].V_Position ||
				vPos_1 != tri.Verts[1].V_Position ||
				vPos_2 != tri.Verts[2].V_Position
			)
			{
				return false;
			}

			/*
			if( Verts == null || Verts.Length < 3 )
			{
				return false;
			}
			else
			{
				if ( !Verts[0].EqualsVert(tri.Verts[0]) )
				{
					return false;
				}
				if (!Verts[1].EqualsVert(tri.Verts[1]))
				{
					return false;
				}
				if (!Verts[2].EqualsVert(tri.Verts[2]))
				{
					return false;
				}
			}
			*/

			/*
			if (Edges == null || Edges.Length < 3)
			{
				return false;
			}
			*/


			return true;
		}

		public void SupplyWithDataFromTriangle( LNX_Triangle tri )
		{
			vPos_0 = tri.Verts[0].V_Position;
			vPos_1 = tri.Verts[1].V_Position;
			vPos_2 = tri.Verts[2].V_Position;

			KnownFullyVisibleTriangleIndices = tri.KnownFullyVisibleTriangleIndices;
		}
	}

	/*
	[System.Serializable]
	public class LNX_SerializedVertData
	{
		public Vector3 V_Position;

		public bool EqualsVert( LNX_Vertex vert )
		{
			if( vert.V_Position != V_Position )
			{
				return false;
			}

			return true;
		}

		public void SupplyWithDataFromVertex( LNX_Vertex vert )
		{
			V_Position = vert.V_Position;
		}
	}
	*/

	/*
	[System.Serializable]
	public class LNX_SerializedEdgeData
	{


		public bool EqualsEdge(LNX_Edge edge)
		{
			return true;
		}
	}
	*/
}
