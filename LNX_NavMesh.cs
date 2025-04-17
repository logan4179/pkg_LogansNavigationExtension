
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

		public string LayerMaskName;
		private int cachedLayerMask;
		public int CachedLayerMask => cachedLayerMask;

		public LNX_Triangle[] Triangles;

		public Vector3[] Vertices;

		[SerializeField] private List<LNX_Triangle> deletedTriangles;
		/// <summary>
		/// Which triangles in the Triangles array were added by modification as opposed to made 
		/// via the original triangulation.
		/// </summary>
		//[SerializeField] private List<int> addedTriangles;

		private int addedTrianglesStartIndex = -1;

		[HideInInspector] public Mesh _Mesh;

		[Header("BOUNDS")]
		/*[SerializeField]*/ private string dbg_Bounds;
		/// <summary>Stores the largest/smallest X, Y, and Z value of the navmesh. Elements 0 and 1 are lowest and 
		/// hightest X, elements 2 and 3 are lowest and highest Y, and elements 4 and 5 are lowest and highest z.</summary>
		[HideInInspector] public float[] Bounds;

		/// <summary>Stores the largest/smallest points defining the bounds of a navmesh. Elements 0-3 form the lower horizontal square of the 
		/// box, while 4-6 form the higher horizontal square of the bounding box. These theoretical boxes each run clockwise. Element 0 
		/// will be the lowest/most-negative value point, and element 4 will be the most positive value point</summary>
		[HideInInspector] public Vector3[] V_Bounds;

		[HideInInspector] public Vector3 V_BoundsCenter;
		[HideInInspector] public Vector3 V_BoundsSize;
		/// <summary>
		/// Longest distance from the bounds center to any corner on the bounding box. This is used as an efficiency value 
		/// in order to short-circuit (return early) from certain methods that don't need to run further logic based on the 
		/// value of this threshold..
		/// </summary>
		[HideInInspector] public float BoundsContainmentDistanceThreshold = -1;

		//[Header("flags")]

		[Header("VISUAL/DEBUG")]
		public Color color_mesh;
		public Vector3[] dbgVis_vertices;
		public Vector3[] dbgVis_normals;
		public int[] dbgVis_triangles;
		[Space(10)]

		public int[] dbgTriangulation_Areas;
		public Vector3[] dbgTriangulation_Vertices;
		public int[] dbgTriangulation_Indices;
		[Space(10)]

		public List<int> dbgKosher_Triangles = new List<int>(); //length should be 3x
		public List<Vector3> dbgKosher_vertices = new List<Vector3>();
		//[Header("OTHER")]
		//public NavMeshTriangulation OriginalTriangulation;

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
			return Triangles[edge.MyCoordinate.TrianglesIndex];
		}
		#endregion

		#region Vertex fetchers -----------------------------------------------------------------------
		public LNX_Vertex GetVertexAtCoordinate( LNX_ComponentCoordinate coord )
		{
			string dbgMe = $"GetVertexAtCoordinate({coord})\n";
			if( Triangles == null || Triangles.Length <= 0 || coord.TrianglesIndex > Triangles.Length-1 || coord.ComponentIndex > 2 || coord.ComponentIndex < 0 )
			{
				dbgMe += "returning null...";
				//Debug.Log(dbgMe);
				return null;
			}
			else
			{
				dbgMe += $"found vert";
				//Debug.Log(dbgMe);

				return Triangles[coord.TrianglesIndex].Verts[coord.ComponentIndex];
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

			if (Triangles == null || Triangles.Length <= 0 || coord.TrianglesIndex > Triangles.Length - 1 || coord.ComponentIndex > 2 || coord.ComponentIndex < 0)
			{
				return null;
			}
			else
			{
				LNX_Vertex vert = Triangles[coord.TrianglesIndex].Verts[coord.ComponentIndex];
				returnList.Add( vert );

				for ( int i = 0; i < vert.SharedVertexCoordinates.Length; i++ ) 
				{
					returnList.Add( Triangles[vert.SharedVertexCoordinates[i].TrianglesIndex].Verts[vert.SharedVertexCoordinates[i].TrianglesIndex] );
				}
			}

			return returnList;
		}
		#endregion

		#region Edge Fetchers --------------------------------------------------
		public LNX_Edge GetEdgeAtCoordinate( LNX_ComponentCoordinate coord )
		{
			if (Triangles == null || Triangles.Length <= 0 || coord.TrianglesIndex > Triangles.Length - 1 || coord.ComponentIndex > 2 || coord.ComponentIndex < 0)
			{
				return null;
			}
			else
			{
				return Triangles[coord.TrianglesIndex].Edges[coord.ComponentIndex];
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

		#region CREATION ---------------------------------------------------------
		[ContextMenu("z - call CalculateTriangulation()")]
		public void CalculateTriangulation()
		{
			Debug.Log($"{nameof(CalculateTriangulation)}()");

			if ( string.IsNullOrEmpty(LayerMaskName) )
			{
				Debug.LogError("LogansNavmeshExtender ERROR! You need to set an environmental layer mask in order to construct the navmesh.");
				return;
			}
			else
			{
				cachedLayerMask = LayerMask.GetMask(LayerMaskName);
			}

			// Make lists-------------------------
			List<int> constructedTriangles = new List<int>(); //length should be 3x
			List<Vector3> constructedVertices_unique = new List<Vector3>();
			List<int> constructedAreaIndices = new List<int>();

			FetchKosherTriangulation( ref constructedAreaIndices, ref constructedVertices_unique, ref constructedTriangles );

			_Mesh = new Mesh();

			if ( HaveModifications() ) // if this is a re-fetch, and we have modifications to consider... //todo: I think when I get the triangulation stuff finished, I can delete this check and re-work this if-then block
			{
				Debug.Log($"Triangle collection exists with modifications...");

				List<LNX_Triangle> newTriCollection = new List<LNX_Triangle>();

				for ( int i = 0; i < constructedAreaIndices.Count; i++ )
				{
					LNX_Triangle tri = new LNX_Triangle( i, ref constructedAreaIndices, ref constructedVertices_unique, ref constructedTriangles, cachedLayerMask );

					for ( int j = 0; j < Triangles.Length; j++ )
					{
						if ( Triangles[j].HasBeenModifiedAfterCreation && Triangles[j].OriginallyMatches(tri) )
						{
							Debug.Log($"new tri '{i}' originally matches old tri '{j}'");
							tri.AdoptModifiedValues( Triangles[i] );
							
							if( tri.Verts[0].AmModified && constructedVertices_unique[tri.Verts[0].MeshIndex_vertices] != tri.Verts[0].Position )
							{
								Debug.Log($"vert0 changing unique vert '{tri.Verts[0].MeshIndex_vertices}' from '{constructedVertices_unique[tri.Verts[0].MeshIndex_vertices]}' to '{tri.Verts[0].Position}'...");
								constructedVertices_unique[tri.Verts[0].MeshIndex_vertices] = tri.Verts[0].Position;
							}
							if (tri.Verts[1].AmModified && constructedVertices_unique[tri.Verts[1].MeshIndex_vertices] != tri.Verts[1].Position)
							{
								Debug.Log($"vert0 changing unique vert '{tri.Verts[1].MeshIndex_vertices}' from '{constructedVertices_unique[tri.Verts[1].MeshIndex_vertices]}' to '{tri.Verts[1].Position}'...");
								constructedVertices_unique[tri.Verts[1].MeshIndex_vertices] = tri.Verts[1].Position;
							}
							if (tri.Verts[2].AmModified && constructedVertices_unique[tri.Verts[2].MeshIndex_vertices] != tri.Verts[2].Position)
							{
								Debug.Log($"vert0 changing unique vert '{tri.Verts[2].MeshIndex_vertices}' from '{constructedVertices_unique[tri.Verts[2].MeshIndex_vertices]}' to '{tri.Verts[2].Position}'...");
								constructedVertices_unique[tri.Verts[2].MeshIndex_vertices] = tri.Verts[2].Position;
							}
								
						} //todo: right now the mesh doesn't respect modifications when you re-calculate triangulation
					}

					newTriCollection.Add( tri );
				}

				Triangles = newTriCollection.ToArray();
			}
			else //There are no modifications...
			{
				Debug.Log( $"no modifications..." );
				Triangles = new LNX_Triangle[ constructedAreaIndices.Count ];
				for ( int i = 0; i < constructedAreaIndices.Count; i++ )
				{
					Triangles[i] = new LNX_Triangle( i, ref constructedAreaIndices, ref constructedVertices_unique, ref constructedTriangles, cachedLayerMask );
				}
			}

			#region CONSTRUCT THE VISUALIZATION MESH--------------------------------
			List<Vector3> constructedNormals = new List<Vector3>(); //length should be 3x
			for (int i = 0; i < constructedVertices_unique.Count; i++)
			{
				constructedNormals.Add(Vector3.up); //todo: What should I actually do here?
			}

			//Construct mesh data...............
			_Mesh.vertices = constructedVertices_unique.ToArray();
			_Mesh.triangles = constructedTriangles.ToArray();
			_Mesh.normals = constructedNormals.ToArray();
			#endregion

			//Debug.Log($"for visualization mesh, constructed '{_mesh.triangles.Length}' tris (indices), '{_mesh.vertices.Length}' vertices, and '{_mesh.normals.Length}' normals...");

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				Triangles[i].CreateRelationships( Triangles );
			}

			CalculateBounds();

			UnityEditor.EditorUtility.SetDirty( this );
		}

		[ContextMenu("z call RegenerateVisualizationMesh()")]
		/// <summary>
		/// Re-generates the visualization mesh for the scene. Note: This does not need to be done in 
		/// addition to calculating the triangulation. CalculateTriangulation() does this automatically. Use this
		/// in cases where the Mesh needs to be re-generated for whatever reason.
		/// </summary>
		public void RegenerateVisualizationMesh()
		{
			Debug.Log($"{nameof(RegenerateVisualizationMesh)}()");
			_Mesh = new Mesh();

			List<int> trianglesList = new List<int>();
			int uniqueVertCount = 0;

			//Debug.Log($"Iterating through '{Triangles.Length}' triangles...");
			for( int i = 0; i < Triangles.Length; i++ )
			{
				//Debug.Log($"i: '{i}'...");

				for ( int j = 0; j < 3; j++ )
				{
					trianglesList.Add( Triangles[i].Verts[j].MeshIndex_vertices );

					if ( Triangles[i].Verts[j].MeshIndex_vertices > uniqueVertCount )
					{
						uniqueVertCount = Triangles[i].Verts[j].MeshIndex_vertices;
					}
				}
			}
			//Debug.LogWarning($"Logged '{trianglesList.Count}' triangle entries, and decided there were '{uniqueVertCount}' unique verts...");
			uniqueVertCount++;//need to add 1 because the last index will be the length - 1
			Vector3[] uniqueVerts = new Vector3[uniqueVertCount];
			for( int i_uniqVrt = 0; i_uniqVrt < uniqueVertCount; i_uniqVrt++ ) //I'm doing it this way to make sure the resulting mesh data will be exactly as it was originally.
			{
				//Debug.Log($"{nameof(i_uniqVrt)}: '{i_uniqVrt}'...");
				for ( int i_tris = 0; i_tris < Triangles.Length; i_tris++ )
				{
					if ( Triangles[i_tris].Verts[0].MeshIndex_vertices == i_uniqVrt )
					{
						uniqueVerts[i_uniqVrt] = Triangles[i_tris].Verts[0].OriginalPosition;
						//Debug.Log($"logging unique vert: '{Triangles[i_tris].Verts[0].MyCoordinate}'");
						break;
					}
					else if ( Triangles[i_tris].Verts[1].MeshIndex_vertices == i_uniqVrt )
					{
						uniqueVerts[i_uniqVrt] = Triangles[i_tris].Verts[1].OriginalPosition;
						//Debug.Log($"logging unique vert: '{Triangles[i_tris].Verts[1].MyCoordinate}'");
						break;
					}
					else if ( Triangles[i_tris].Verts[2].MeshIndex_vertices == i_uniqVrt )
					{
						uniqueVerts[i_uniqVrt] = Triangles[i_tris].Verts[2].OriginalPosition;
						//Debug.Log($"logging unique vert: '{Triangles[i_tris].Verts[2].MyCoordinate}'");
						break;
					}
				}
			}
			//Debug.Log($"finished loop...");

			_Mesh.vertices = uniqueVerts;
			Vector3[] nrmls = new Vector3[uniqueVerts.Length];
			for (int i = 0; i < nrmls.Length; i++)
			{
				nrmls[i] = Vector3.up; //todo: What should I actually do here?
			}
			_Mesh.normals = nrmls;

			_Mesh.triangles = trianglesList.ToArray(); //apparently this MUST come AFTER setting the vertices or will throw error

			dbgVis_triangles = _Mesh.triangles;
			dbgVis_vertices = _Mesh.vertices;
			dbgVis_normals = _Mesh.normals;
		}

		[ContextMenu("z call fetchKosherTriangulation()")]
		/// <summary>
		/// Returns a NavMeshTriangulation object without repeated vertices. Takes into account existing modifications.
		/// </summary>
		/// <returns></returns>
		public void FetchKosherTriangulation( ref List<int> areaList_passed, ref List<Vector3> vrtList_passed, ref List<int> trianglesList_passed )
		{
			Debug.Log($"{nameof(FetchKosherTriangulation)}()");
			NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
			Debug.Log($"inital triangulation has '{triangulation.areas.Length}' areas, '{triangulation.vertices.Length}' vertices, and '{triangulation.indices.Length}' indices.\n");

			bool hvMods = HaveModifications();

			for ( int i = 0; i < triangulation.areas.Length; i++ )
			{
				Debug.Log($"{i} --------------------------////////////////////////////////////\n");
				if ( !hvMods || !ContainsDeletion(triangulation, i) )
				{
					logTriInfoToKosherLists( triangulation, i, ref areaList_passed, ref vrtList_passed, ref trianglesList_passed );
				}
			}

			dbgTriangulation_Areas = triangulation.areas;
			dbgTriangulation_Vertices = triangulation.vertices;
			dbgTriangulation_Indices = triangulation.indices;

			dbgKosher_Triangles = trianglesList_passed;
			dbgKosher_vertices = vrtList_passed;
		}

		/// <summary>
		/// Logs the info of a tri into all the kosher lists.
		/// </summary>
		/// <param name="nmTrngltn"></param>
		/// <param name="triIndex">Corresponds directly to the navmeshtriangulation.areas array, and indirectly to the navmeshtriangulation.indices array by multiplying by 3.</param>
		/// <param name="areas_passed"></param>
		/// <param name="vrtLst_passed"></param>
		/// <param name="incs_passed"></param>
		private void logTriInfoToKosherLists( int areaIndx, Vector3 vrt0Pos, Vector3 vrt1Pos, Vector3 vrt2Pos, ref List<int> areas_passed, ref List<Vector3> vrtLst_passed, ref List<int> trianglesLst_passed )
		{
			string dbgThis = "";
			areas_passed.Add( areaIndx );

			int[] vertMatchIndices = new int[3] { -1, -1, -1 };
			dbgThis += ($"checking verts in growing list of '{vrtLst_passed.Count}' verts...\n");
			for ( int j = 0; j < vrtLst_passed.Count; j++ )
			{
				if ( vrtLst_passed[j] == vrt0Pos )
				{
					vertMatchIndices[0] = j;
				}
				else if ( vrtLst_passed[j] == vrt1Pos )
				{
					vertMatchIndices[1] = j;
				}
				else if ( vrtLst_passed[j] == vrt2Pos )
				{
					vertMatchIndices[2] = j;
				}
			}

			dbgThis += ($"end. match0: '{vertMatchIndices[0]}', match1: '{vertMatchIndices[1]}', match2: '{vertMatchIndices[2]}'...\n");
			if ( vertMatchIndices[0] == -1 )
			{
				dbgThis += $"adding new vert/indx for 0...\n";
				vrtLst_passed.Add( vrt0Pos );
				trianglesLst_passed.Add( vrtLst_passed.Count - 1 );
			}
			else
			{
				trianglesLst_passed.Add( vertMatchIndices[0] );
			}

			if ( vertMatchIndices[1] == -1 )
			{
				dbgThis += $"adding new vert/indx for 1...\n";

				vrtLst_passed.Add( vrt1Pos );
				trianglesLst_passed.Add( vrtLst_passed.Count - 1 );
			}
			else
			{
				trianglesLst_passed.Add( vertMatchIndices[1] );
			}

			if ( vertMatchIndices[2] == -1 )
			{
				dbgThis += $"adding new vert/indx for 2...\n";

				vrtLst_passed.Add( vrt2Pos );
				trianglesLst_passed.Add( vrtLst_passed.Count - 1 );
			}
			else
			{
				trianglesLst_passed.Add( vertMatchIndices[2] );
			}

			//Debug.Log(dbgThis);
		}

		/// <summary>
		/// Overload that takes in a NavMeshTriangulation object, and a starting index, and does the work of logging the triangulation info 
		/// to the passed kosher collections.
		/// </summary>
		/// <param name="nmTrngltn"></param>
		/// <param name="triIndex">Corresponds directly to the navmeshtriangulation.areas array, and indirectly to the navmeshtriangulation.indices array by multiplying by 3.</param>
		/// <param name="areas_passed"></param>
		/// <param name="vrtLst_passed"></param>
		/// <param name="incs_passed"></param>
		private void logTriInfoToKosherLists( NavMeshTriangulation nmTrngltn, int triIndex, ref List<int> areas_passed, ref List<Vector3> vrtLst_passed, ref List<int> trianglesLst_passed )
		{
			logTriInfoToKosherLists(nmTrngltn.areas[triIndex], 
				nmTrngltn.vertices[nmTrngltn.indices[triIndex * 3]], 
				nmTrngltn.vertices[nmTrngltn.indices[(triIndex * 3) + 1]],
				nmTrngltn.vertices[nmTrngltn.indices[(triIndex * 3) + 2]],
				ref areas_passed, ref vrtLst_passed, ref trianglesLst_passed
			);
		}

		#endregion

		[ContextMenu("z call CheckForRepeats()")]
		public void CheckForRepeats() //helper method. dws
		{
			for ( int i = 0; i < dbgTriangulation_Vertices.Length; i++ )
			{
				for ( int j = 0; j < dbgTriangulation_Vertices.Length; j++ )
				{
					if( i != j && dbgTriangulation_Vertices[i] == dbgTriangulation_Vertices[j] )
					{
						Debug.LogWarning($"found same at i: '{i}' and j: '{j}'");
						return;
					}
				}
			}
		}

		#region MODIFICATION-----------------------------------------------------------
		/// <summary>
		/// Checks to see if any madifications exist on this LNX_NavMesh. Warning: Relatively slow operation. 
		/// Not as cheap as checking a boolean flag.
		/// </summary>
		/// <returns></returns>
		public bool HaveModifications()
		{
			Debug.Log($"{nameof(HaveModifications)}()");

			if( deletedTriangles != null && deletedTriangles.Count > 0 )
			{
				Debug.LogWarning($"{nameof(deletedTriangles)}, count: '{deletedTriangles.Count}'");
				return true;
			}

			/*
			if ( addedTriangles != null && addedTriangles.Count > 0)
			{
				Debug.LogWarning($"{nameof(addedTriangles)}, count: '{addedTriangles.Count}'");
				return true;
			}
			*/
			if( addedTrianglesStartIndex > -1 )
			{
				return true;
			}

			if ( Triangles != null && Triangles.Length > 0 )
			{
				for( int i = 0; i < Triangles.Length; i++ )
				{
					if(Triangles[i].HasBeenModifiedAfterCreation )
					{
						Debug.LogWarning($"tri {i} has been modified.");
						return true;
					}
				}
			}

			//Debug.Log($"found no modifications. returning false....");

			return false;
		} //Todo: remember to unit test this
		
		public void MoveVert_managed( LNX_Vertex vert, Vector3 pos )
		{
			Triangles[vert.MyCoordinate.TrianglesIndex].MoveVert_managed( this, vert.MyCoordinate.ComponentIndex, pos );

			if( vert.MeshIndex_vertices > -1 )
			{
				Debug.Log($"moving vert '{vert.MyCoordinate.ToString()}' with {nameof(vert.MeshIndex_vertices)}: '{vert.MeshIndex_vertices}'");

				Vector3[] tmpVrts = _Mesh.vertices; //note: I can't get it to update the mesh if I only change the relevant vertex within the mesh object, It seems like I MUST create and assign a whole new array.
				tmpVrts[vert.MeshIndex_vertices] = vert.Position + pos;
				_Mesh.vertices = tmpVrts; //apparently you have to assign to the mesh in this manner in order to make this update (apparently I can't just change one of the existing vertices elements)...
			}
		}
		public void ClearModifications()
		{
			Debug.Log($"{nameof(ClearModifications)}()");
			for (int i = 0; i < Triangles.Length; i++)
			{
				Triangles[i].ClearModifications();
			}

			deletedTriangles = new List<LNX_Triangle>();
			//addedTriangles = new List<int>(); //todo: dws

			UnityEditor.EditorUtility.SetDirty(this);
			//flag_haveModifications = false; //todo: now using method instead. DWS
		}

		public void ChangeTriIndex(LNX_Triangle tri, int newIndex)
		{

		}
		#endregion

		#region DELETING ---------------------------------------------------------------------------------
		/*public void DeleteTriangle( int triIndex )
		{
			if ( triIndex < 0 || triIndex >= Triangles.Length )
			{
				Debug.LogError($"LNX ERROR! You tried to delete a triangle with an index greater than the Triangles list count. Returning early...");
				return;
			}

			DeleteTriangle( Triangles[triIndex] );
		}

		public void DeleteTriangle( LNX_Triangle triangle )
		{
			if ( !deletedTriangles.Contains(triangle) )
			{
				deletedTriangles.Add( triangle );
			}

			LNX_Triangle[] tempArray = new LNX_Triangle[Triangles.Length - 1];
			int runningIndex = 0;
			for ( int i = 0; i < Triangles.Length; i++ )
			{
				if ( Triangles[i] != triangle )
				{
					tempArray[runningIndex] = Triangles[i];
					runningIndex++;
				}
			}


			List<LNX_Triangle> triList = Triangles.ToList();
			triList.RemoveAt(triIndex);
		}

		public void DeleteTriangles( params LNX_Triangle[] tris )
		{

		}

		public bool ContainsDeletion( LNX_Triangle tri )
		{
			if( deletedTriangles != null && deletedTriangles.Count > 0 )
			{
				for ( int i = 0; i < deletedTriangles.Count; i++ )
				{
					if( deletedTriangles[i].OriginallyMatches(tri) )
					{
						return true;
					}
				}
			}

			return false;
		}*/

		public bool ContainsDeletion( NavMeshTriangulation nmTriangulation, int indx_indices )
		{
			if ( deletedTriangles != null && deletedTriangles.Count > 0 )
			{
				for ( int i = 0; i < deletedTriangles.Count; i++ )
				{
					if ( deletedTriangles[i].GetVertIndextAtOriginalPosition(nmTriangulation.vertices[indx_indices]) != -1 )
					{
						return true;
					}
				}
			}

			return false;
		}
		#endregion

		#region ADDING ---------------------------------------------------------------
		public void AddTriangle( Vector3 vrt0Pos, Vector3 vrt1Pos, Vector3 vrt2Pos)
		{
			List<Vector3> UniqueVerts = new List<Vector3>();
			List<int> constructedAreas = new List<int>();
			List<int> constructedTriangles = new List<int>();

			for ( int i = 0; i < Triangles.Length; i++ )
			{

			}

			logTriInfoToKosherLists( 0, vrt0Pos, vrt1Pos, vrt2Pos, ref constructedAreas, ref UniqueVerts, ref constructedTriangles );

		}

		public void AddTriangle(LNX_Triangle triangle)
		{
			List<int> constructedTriangles = _Mesh.triangles.ToList();

			int[] matchedVert = new int[3] { -1, -1, -1 };
			for (int i = 0; i < _Mesh.vertices.Length; i++) //Check if there's a new unique vertex...
			{
				if (triangle.Verts[0].Position == _Mesh.vertices[i])
				{
					matchedVert[0] = i;
				}
				else if (triangle.Verts[1].Position == _Mesh.vertices[i])
				{
					matchedVert[1] = i;
				}
				else if (triangle.Verts[2].Position == _Mesh.vertices[i])
				{
					matchedVert[2] = i;
				}
			}

			List<Vector3> constructedVertices = _Mesh.vertices.ToList();

			if (matchedVert[0] == -1)
			{
				constructedVertices.Add(triangle.Verts[0].Position);
				constructedTriangles.Add(constructedVertices.Count);
			}
			else
			{

			}
		}
		#endregion

		#region CALCULATION ---------------------------------------------------------
		public void CalculateBounds()
		{
			dbg_Bounds = string.Empty;

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

			if( Bounds.Length > 0 )
			{
				dbg_Bounds = $"{nameof(Bounds)}[{0}]: '{Bounds[0]}'\n" +
				$"{nameof(Bounds)}[{1}]: '{Bounds[1]}'\n" +
				$"{nameof(Bounds)}[{2}]: '{Bounds[2]}'\n" +
				$"{nameof(Bounds)}[{3}]: '{Bounds[3]}'\n" +
				$"{nameof(Bounds)}[{4}]: '{Bounds[4]}'\n" +
				$"{nameof(Bounds)}[{5}]: '{Bounds[5]}'\n\n";
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

			if ( V_Bounds.Length > 0 )
			{
				dbg_Bounds = $"{nameof(V_Bounds)}[{0}]: '{V_Bounds[0]}'\n" +
				$"{nameof(V_Bounds)}[{1}]: '{V_Bounds[1]}'\n" +
				$"{nameof(V_Bounds)}[{2}]: '{V_Bounds[2]}'\n" +
				$"{nameof(V_Bounds)}[{3}]: '{V_Bounds[3]}'\n" +
				$"{nameof(V_Bounds)}[{4}]: '{V_Bounds[4]}'\n" +
				$"{nameof(V_Bounds)}[{5}]: '{V_Bounds[5]}'\n" +
				$"{nameof(V_Bounds)}[{6}]: '{V_Bounds[6]}'\n" +
				$"{nameof(V_Bounds)}[{7}]: '{V_Bounds[7]}'\n\n";
			}

			V_BoundsCenter = (
				V_Bounds[0] + V_Bounds[1] + V_Bounds[2] + V_Bounds[3] + 
				V_Bounds[4] + V_Bounds[5] + V_Bounds[6] + V_Bounds[7]
				
			) / 8;

			dbg_Bounds += $"{nameof(V_BoundsCenter)}: '{V_BoundsCenter}'\n\n";

			V_BoundsSize = new Vector3(
				Mathf.Abs(Bounds[0] - Bounds[1]),
				Mathf.Abs(Bounds[2] - Bounds[3]),
				Mathf.Abs(Bounds[4] - Bounds[5])
			);

			dbg_Bounds += $"{nameof(V_BoundsSize)}: '{V_BoundsSize}'\n\n";

			BoundsContainmentDistanceThreshold = Mathf.Max
			(
				Vector3.Distance(V_BoundsCenter, V_Bounds[0]),
				Vector3.Distance(V_BoundsCenter, V_Bounds[4])
			);

			dbg_Bounds += $"{nameof(BoundsContainmentDistanceThreshold)}: '{BoundsContainmentDistanceThreshold}'\n\n";

		}

		[ContextMenu("z call RefeshMesh()")]
		public void RefeshMesh()
		{
			for (int i = 0; i < Triangles.Length; i++)
			{
				Triangles[i].RefreshTriangle(this, false);
			}

			CalculateBounds();
		}

		/*[SerializeField]*/
		private string dbgCalculatePath;
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
		#endregion

		#region API METHODS----------------------------------------------------------------
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

		#endregion
	}
}