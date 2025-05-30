
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Windows;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_NavMesh : MonoBehaviour
    {
		public static LNX_NavMesh Instance;

		public LNX_Direction ProjectionDirection = LNX_Direction.PositiveY;

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
		public Vector3[] dbgMesh_vertices;
		public Vector3[] dbgMesh_normals;
		public int[] dbgMesh_triangles;
		[Space(10)]

		public int[] dbgTriangulation_Areas;
		public Vector3[] dbgTriangulation_Vertices;
		public int[] dbgTriangulation_Indices;
		[Space(10)]

		public List<int> dbgKosher_Triangles = new List<int>(); //length should be 3x
		public List<Vector3> dbgKosher_vertices = new List<Vector3>();
		//[Header("OTHER")]
		//public NavMeshTriangulation OriginalTriangulation;
		private void OnEnable()
		{
			Debug.Log("lnx_navmesh.onenable()");
		}
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

		public LNX_Triangle GetTriangle( LNX_ComponentCoordinate coord )
		{
			return Triangles[coord.TrianglesIndex];
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

		#region CREATION/SETUP ---------------------------------------------------------
		[ContextMenu("z - call CalculateTriangulation()")]
		public void CalculateTriangulation()
		{
			Debug.Log( $"{nameof(CalculateTriangulation)}()" );

			if ( string.IsNullOrEmpty(LayerMaskName) )
			{
				Debug.LogError("LogansNavmeshExtender ERROR! You need to set an environmental layer mask in order to construct the navmesh.");
				return;
			}
			else
			{
				cachedLayerMask = LayerMask.GetMask( LayerMaskName );
			}

			// Make lists-------------------------
			List<Vector3> constructedVertices_unique = new List<Vector3>();

			_Mesh = new Mesh();

			#region DEAL WITH TRIANGULATION -----------------------------------------------------------------------------
			NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
			Debug.Log($"inital triangulation has '{triangulation.areas.Length}' areas, '{triangulation.vertices.Length}' " +
				$"vertices, and '{triangulation.indices.Length}' indices.\n");

			List<LNX_Triangle> newTriCollection = new List<LNX_Triangle>();

			bool hvMods = HaveModifications();

			for ( int i = 0; i < triangulation.areas.Length; i++ )
			{
				Debug.Log($"{i} --------------------------////////////////////////////////////\n");
				if ( !hvMods || !ContainsDeletion(triangulation, i) )
				{
					LNX_Triangle tri = new LNX_Triangle( newTriCollection.Count, triangulation.areas[i],
						triangulation.vertices[triangulation.indices[i * 3]],
						triangulation.vertices[triangulation.indices[(i * 3) + 1]],
						triangulation.vertices[triangulation.indices[(i * 3) + 2]],
						this
					);

					newTriCollection.Add( tri );

					if( hvMods )
					{
						for ( int j = 0; j < Triangles.Length; j++ )
						{
							if ( Triangles[j].HasBeenModifiedAfterCreation && Triangles[j].PositionallyMatches(tri) )
							{
								Debug.Log($"new tri '{i}' originally matches old tri '{j}'");
								tri.AdoptModifiedValues(Triangles[i]);
							}
						}
					}
				}
			}
			#endregion

			Triangles = newTriCollection.ToArray();

			#region UPDATE THE MESH VERT POSITIONING IF ANY ARE MODIFIED ----------------------------
			if ( hvMods )
			{
				Debug.Log($"There are mods, so now going through the mesh vertices collection to see if any of their positions need to be changed...");
				for ( int i_uniqueVrts = 0; i_uniqueVrts < constructedVertices_unique.Count; i_uniqueVrts++ )
				{
					bool foundModifiedMatch = false;

					for ( int i_Triangles = 0; i_Triangles < Triangles.Length; i_Triangles++ )
					{
						if ( Triangles[i_Triangles].HasBeenModifiedAfterCreation )
						{
							for ( int i_verts = 0; i_verts < 3; i_verts++ )
							{
								if( constructedVertices_unique[i_uniqueVrts] == Triangles[i_Triangles].Verts[i_verts].OriginalPosition )
								{
									Debug.Log($"mesh vert at index: '{i_uniqueVrts}', position: '{constructedVertices_unique[i_uniqueVrts]}' " +
										$"matches original position of tri: '{i_Triangles}'. Changing position to: '{Triangles[i_Triangles].Verts[i_verts].Position}'...");

									constructedVertices_unique[i_uniqueVrts] = Triangles[i_Triangles].Verts[i_verts].Position;
									foundModifiedMatch = true;

									break;
								}
							}
						}

						if ( foundModifiedMatch )
						{
							break;
						}
					}
				}
			}
			#endregion

			ReconstructVisualizationMesh();

			//Debug.Log($"for visualization mesh, constructed '{_mesh.triangles.Length}' tris (indices), '{_mesh.vertices.Length}' vertices, and '{_mesh.normals.Length}' normals...");

			RecalculateRelational();

			Debug.Log($"End of {nameof(CalculateTriangulation)}(). Created '{Triangles.Length}' triangles, and '{constructedVertices_unique.Count}' unique vertices for the mesh.");

			UnityEditor.EditorUtility.SetDirty( this );
		}

		public void CalculateBounds()
		{
			dbg_Bounds = string.Empty;

			Bounds = new float[6]
			{
				float.MaxValue, float.MinValue,
				float.MaxValue, float.MinValue,
				float.MaxValue, float.MinValue
			};

			for (int i = 0; i < Triangles.Length; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					if (Triangles[i].Verts[j].Position.x < Bounds[0])
					{
						Bounds[0] = Triangles[i].Verts[j].Position.x;
					}
					else if (Triangles[i].Verts[j].Position.x > Bounds[1])
					{
						Bounds[1] = Triangles[i].Verts[j].Position.x;
					}

					if (Triangles[i].Verts[j].Position.y < Bounds[2])
					{
						Bounds[2] = Triangles[i].Verts[j].Position.y;
					}
					else if (Triangles[i].Verts[j].Position.y > Bounds[3])
					{
						Bounds[3] = Triangles[i].Verts[j].Position.y;
					}

					if (Triangles[i].Verts[j].Position.z < Bounds[4])
					{
						Bounds[4] = Triangles[i].Verts[j].Position.z;
					}
					else if (Triangles[i].Verts[j].Position.z > Bounds[5])
					{
						Bounds[5] = Triangles[i].Verts[j].Position.z;
					}
				}
			}

			if (Bounds.Length > 0)
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

			if (V_Bounds.Length > 0)
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

		[ContextMenu("z call ReconstructVisualizationMesh()")]
		/// <summary>
		/// Re-constructs the visualization mesh for the scene. Use this
		/// in cases where the Mesh needs to be re-made when the Triangle info can be assumed to be correct/un-changed. IE: When Unity is 
		/// closed and reopened, and the mesh information needs to be remade because it's not serialized.
		/// </summary>
		/// <param name="assumeCollectionChange"> Whether the Triangles colleciton has changed and should be </param>
		public void ReconstructVisualizationMesh()
		{
			Debug.Log($"{nameof(ReconstructVisualizationMesh)}()");

			/*
			Note: I used to have a lot of the loops in here bundled together for more efficiency, but it looked 
			awful, so I separated them into multiple loops to make them more debuggable. I think this added overhead 
			is okay because this method is not meant to be called during performance crticial moments. This is only
			for occasional, discrete, calls in the editor as needed. This is a slow method, and that's okay.
			*/

			_Mesh = new Mesh();

			bool listIsStillKosher = true;
			bool mainIndicesAreUnbroken = true;
			bool vertPositionsAreConsistentWithMeshVertPositions = true;

			List<int> mesh_triangles = new List<int>();

			#region ASSEMBLE THE UNIQUE VERTICES LIST ----------------------------------------------
			List<Vector3> uniqueVerts = new List<Vector3>();
			int greatestVertMeshIndex = 0; //We'll keep track of the greatest vertMeshIndex while we're at it...

			for ( int i_Triangles = 0; i_Triangles < Triangles.Length; i_Triangles++ )
			{
				for ( int i_Verts = 0; i_Verts < 3; i_Verts++ )
				{
					bool foundVertInUniqueList = false;
					for ( int i_uniqueVrts = 0; i_uniqueVrts < uniqueVerts.Count; i_uniqueVrts++ )
					{
						if ( Triangles[i_Triangles].Verts[i_Verts].Position == uniqueVerts[i_uniqueVrts] )
						{
							foundVertInUniqueList = true;
						}
					}

					if ( !foundVertInUniqueList )
					{
						uniqueVerts.Add( Triangles[i_Triangles].Verts[i_Verts].Position );
					}

					if ( Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices > greatestVertMeshIndex )
					{
						//Debug.Log($"Found new greatest vertmeshindex of '{Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices}' " +
							//$"at vert '[{i_Triangles}][{i_Verts}]'");
						greatestVertMeshIndex = Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices;
					}
				}
			}
			#endregion

			if ( greatestVertMeshIndex != (uniqueVerts.Count - 1) )
			{
				Debug.Log($"{nameof(greatestVertMeshIndex)} ({greatestVertMeshIndex}) was not the same as the count of " +
					$"'{nameof(uniqueVerts)}' ({uniqueVerts.Count}) minus one. Decided was not kosher...");
				listIsStillKosher = false;
			}

			#region CHECK THAT THE TRIANGLE INDICES ARE UNBROKEN ---------------------------------
			Debug.Log("checking that all verts' visualization mesh indices are continuous/unbroken all the way to the largest index...");
			for ( int i_Triangles = 0; i_Triangles < Triangles.Length; i_Triangles++ )
			{
				if( mainIndicesAreUnbroken && Triangles[i_Triangles].Index_inCollection != i_Triangles )
				{
					listIsStillKosher = false;
					mainIndicesAreUnbroken = false;
					Debug.Log($"Found that triangle{i_Triangles}'s main index property does NOT align with it's position in the collection. list is not kosher...");
					break;
				}
			}
			#endregion

			#region CHECK THAT ALL VERT POSITIONS CORRESPOND TO THEIR VISMESH VERT POSITION -----------------------
			for ( int i_Triangles = 0; i_Triangles < Triangles.Length; i_Triangles++ )
			{
				for ( int i_Verts = 0; i_Verts < 3; i_Verts++ )
				{
					if (Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices < 0 || 
						Triangles[i_Triangles].Verts[i_Verts].Position != uniqueVerts[Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices] ) //todo: this is where the unit test is failing
					{
						listIsStillKosher = false;
						vertPositionsAreConsistentWithMeshVertPositions = false;
						Debug.Log($"vert[{i_Triangles}],[{i_Verts}]'s position ({Triangles[i_Triangles].Verts[i_Verts].Position}) did NOT match unique " +
							$"vert{Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices}'s position. Decided list was NOT kosher...");

						break;
					}
				}

				if( !vertPositionsAreConsistentWithMeshVertPositions ) //This way it doesn't loop through any more triangles...
				{
					break;
				}
			}
			#endregion

			if ( !listIsStillKosher )
			{
				Debug.Log($"Collection didn't pass 'isKosher' check. Attempting fix...");

				#region FIX BROKEN INDICES ------------------------------------------------------------------------------
				if ( !mainIndicesAreUnbroken )
				{
					Debug.Log("Main Triangle cached indices had issues. Attempting to fix main indices...");

					for ( int i_Triangles = 0; i_Triangles < Triangles.Length; i_Triangles++ )
					{
						if ( Triangles[i_Triangles].Index_inCollection != i_Triangles )
						{
							Debug.Log($"Triangle '{i_Triangles}' had cached index of: '{Triangles[i_Triangles].Index_inCollection}'. Fixing...");
							Triangles[i_Triangles].ChangeIndex_action( i_Triangles );
						}
					}

				}
				#endregion

				#region FIX INCONSISTENT VERT INDICES ---------------------------------------------------------
				if ( !vertPositionsAreConsistentWithMeshVertPositions )
				{
					for ( int i_Triangles = 0; i_Triangles < Triangles.Length; i_Triangles++ )
					{
						for ( int i_Verts = 0; i_Verts < 3; i_Verts++ )
						{
							if (Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices < 0 || 
								Triangles[i_Triangles].Verts[i_Verts].Position != uniqueVerts[Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices] ) //this is going out of range...
							{
								bool foundUniqueVertMatch = false;
								for( int i_uniqueVerts = 0; i_uniqueVerts < uniqueVerts.Count; i_uniqueVerts++ )
								{
									if ( Triangles[i_Triangles].Verts[i_Verts].Position == uniqueVerts[i_uniqueVerts] )
									{
										Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices = i_uniqueVerts;
										foundUniqueVertMatch = true;
										break;
									}
								}

								if ( !foundUniqueVertMatch )
								{
									Debug.LogError($"Vert [{i_Triangles}][{i_Verts}] couldn't find a match in the unique vert list."); //I don't think it's possible this will ever happen 
									//because I think the assembly of the uniqueverts list should catch all vert positions.
								}
							}
						}
					}
				}
				#endregion

				#region ASSEMBLE MESH TRIANGLES COLLECTION -----------------------------------------------------
				mesh_triangles = new List<int>(); //now we can't trust what we logged earlier to this list...
				for ( int i_Triangles = 0; i_Triangles < Triangles.Length; i_Triangles++ )
				{
					for ( int i_Verts = 0; i_Verts < 3; i_Verts++ )
					{
						mesh_triangles.Add(Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices );
					}
				}
				#endregion
			}
			else
			{
				Debug.Log("Considered navmesh to be kosher. constructing vismesh lists...");

				for ( int i_Triangles = 0; i_Triangles < Triangles.Length; i_Triangles++ )
				{
					for( int i_Verts = 0; i_Verts < 3; i_Verts++ )
					{
						mesh_triangles.Add( Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices );
					}
				}
			}

			#region CREATE VISUALIZATION MESH ---------------------------------
			_Mesh.vertices = uniqueVerts.ToArray();

			Vector3[] nrmls = new Vector3[uniqueVerts.Count];
			for (int i = 0; i < nrmls.Length; i++)
			{
				nrmls[i] = Vector3.up; //todo: What should I actually do here?
			}
			_Mesh.normals = nrmls;

			_Mesh.triangles = mesh_triangles.ToArray(); //apparently this MUST come AFTER setting the vertices or will throw error

			dbgMesh_triangles = _Mesh.triangles;
			dbgMesh_vertices = _Mesh.vertices;
			dbgMesh_normals = _Mesh.normals;
			#endregion

			Debug.Log($"end of {nameof(ReconstructVisualizationMesh)}()");
		}

		#endregion

		#region MODIFICATION-----------------------------------------------------------
		/// <summary>
		/// Checks to see if any madifications exist on this LNX_NavMesh. Warning: Relatively slow operation. 
		/// Not as cheap as checking a boolean flag.
		/// </summary>
		/// <returns></returns>
		public bool HaveModifications()
		{
			string methodReport = $"{nameof(HaveModifications)}()\n";

			if( deletedTriangles != null && deletedTriangles.Count > 0 )
			{
				methodReport += $"Found DO have modifications. {nameof(deletedTriangles)}, count: '{deletedTriangles.Count}'\n";
				Debug.Log( methodReport );
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
						methodReport += $"Found DO have movement modifications at tri {i}. ";
						Debug.Log( methodReport );

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

			if( vert.Index_VisMesh_Vertices > -1 && _Mesh != null && _Mesh.vertices != null && _Mesh.vertices.Length > 0 )
			{
				//Debug.Log($"moving vert '{vert.MyCoordinate.ToString()}' with {nameof(vert.Index_VisMesh_Vertices)}: '{vert.Index_VisMesh_Vertices}'");

				Vector3[] tmpVrts = _Mesh.vertices; //note: I can't get it to update the mesh if I only change the relevant vertex within the mesh object, It seems like I MUST create and assign a whole new array.
				tmpVrts[vert.Index_VisMesh_Vertices] = vert.Position;
				_Mesh.vertices = tmpVrts; //apparently you have to assign to the mesh in this manner in order to make this update (apparently I can't just change one of the existing vertices elements)...
			}
		}

		public void ClearModifications()
		{
			Debug.Log($"{nameof(ClearModifications)}()");
			List<LNX_Triangle> newTrianglesList = new List<LNX_Triangle>();

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				if( !Triangles[i].WasAddedViaMod )
				{
					Triangles[i].ClearModifications();
					newTrianglesList.Add( Triangles[i] );
				}
			}

			deletedTriangles = new List<LNX_Triangle>();
			//addedTriangles = new List<int>(); //todo: dws

			UnityEditor.EditorUtility.SetDirty(this);
		}
		#endregion

		#region DELETING ---------------------------------------------------------------------------------
		public void DeleteTriangles( params LNX_Triangle[] trisToDelete )
		{
			if ( Triangles.Length <= 0 )
			{
				Debug.LogError("LNX ERROR! You tried to delete a triangle with either an invalid index, or when there were no triangles to delete. Returning early...");
				return;
			}

			List<int> mesh_triangles = new List<int>();
			List<Vector3> mesh_vertices = new List<Vector3>();

			List<LNX_Triangle> newTriangles = new List<LNX_Triangle>();
			int runningTriIndx = 0;

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				bool foundDeletion = false;

				for ( int j = 0; j < trisToDelete.Length; j++ )
				{
					if ( Triangles[i].ValueEquals(trisToDelete[j]) )
					{
						foundDeletion = true;
						deletedTriangles.Add(Triangles[i]);
						break;
					}
				}

				if( !foundDeletion ) //...then we need to add it's stuff to the collections...
				{
					if(Triangles[i].Index_inCollection != runningTriIndx)
					{
						//Debug.Log($"CHANGIN DA INDEX AT: '{runningTriIndx}'...");
						Triangles[i].ChangeIndex_action( runningTriIndx );
					}

					runningTriIndx++;
					newTriangles.Add( Triangles[i] );
				}
			}

			Triangles = newTriangles.ToArray();

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				Triangles[i].CreateRelationships( Triangles );
			}

			CalculateBounds();
		}

		public bool ContainsDeletion( LNX_Triangle tri )
		{
			if( deletedTriangles != null && deletedTriangles.Count > 0 )
			{
				for ( int i = 0; i < deletedTriangles.Count; i++ )
				{
					if( deletedTriangles[i].PositionallyMatches(tri) )
					{
						return true;
					}
				}
			}

			return false;
		}

		public bool ContainsDeletion( NavMeshTriangulation nmTriangulation, int areaIndex )
		{
			string methodReport = $"{nameof(ContainsDeletion)}(). checking vert in list starting at the vert at index: '{nmTriangulation.indices[areaIndex * 3]}', position: '{nmTriangulation.vertices[nmTriangulation.indices[areaIndex * 3]]}'...";

			if ( deletedTriangles != null && deletedTriangles.Count > 0 )
			{
				for ( int i = 0; i < deletedTriangles.Count; i++ )
				{
					if ( deletedTriangles[i].GetVertIndextAtOriginalPosition(nmTriangulation.vertices[nmTriangulation.indices[areaIndex * 3]]) != -1 )
					{
						methodReport += $"found DO contain deletion. passed index: '{areaIndex}'...";
						Debug.LogWarning( methodReport );
						return true;
					}
				}
			}

			return false;
		}
		#endregion

		#region ADDING ---------------------------------------------------------------
		public void AddTriangles( params LNX_Triangle[] tris )
		{
			Debug.Log($"{nameof(AddTriangles)}(). Was passed '{tris.Length}' tris...");

			List<LNX_Triangle> constructedLnxTriangles = Triangles.ToList();

			for ( int i = 0; i < tris.Length; i++ )
			{
				Debug.Log($"i: '{i}'...");
				//LNX_Triangle tri = new LNX_Triangle( tris[i], constructedLnxTriangles.Count );
				//tri.WasAddedViaMod = true;
				tris[i].WasAddedViaMod = true;
				constructedLnxTriangles.Add( tris[i] );
			}

			Triangles = constructedLnxTriangles.ToArray();

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				Triangles[i].CreateRelationships(Triangles);
			}

			CalculateBounds();

			ReconstructVisualizationMesh();
		}
		#endregion

		#region CALCULATION ---------------------------------------------------------

		/// <summary>
		/// Re-calculates the derived info and re-creates relationships for all triangles. Also 
		/// re-calculates the bounds. Call this after an edit has been made to the navmesh.
		/// </summary>
		[ContextMenu("z call RefreshAfterMove()")]
		public void RefreshAfterMove()
		{
			for (int i = 0; i < Triangles.Length; i++)
			{
				Triangles[i].RefreshTriangle(this, false);
			}

			CalculateBounds();
		}

		public void RecalculateRelational()
		{
			for (int i = 0; i < Triangles.Length; i++)
			{
				Triangles[i].CreateRelationships(Triangles);
			}

			CalculateBounds();
		}
		#endregion

		#region MAIN API METHODS----------------------------------------------------------------
		/*[SerializeField]*/
		private string dbgCalculatePath;
		public bool CalculatePath( Vector3 startPos_passed, Vector3 endPos_passed, float maxSampleDistance, out LNX_Path path )
		{
			LNX_ProjectionHit lnxHit = new LNX_ProjectionHit();

			if( SamplePosition(startPos_passed, out lnxHit, maxSampleDistance) )
			{
				startPos_passed = lnxHit.HitPosition;
				dbgCalculatePath += $"SamplePosition() hit startpos\n";
			}
			else
			{
				dbgCalculatePath += $"SamplePosition() did NOT hit startpos.\n";
				path = null;
				return false; //todo: returning a boolean is newly added. Make sure this return boolean is being properly used...
			}

			if ( SamplePosition(endPos_passed, out lnxHit, maxSampleDistance) )
			{
				endPos_passed = lnxHit.HitPosition;
				dbgCalculatePath += $"SamplePosition() hit endpos\n";
			}
			else
			{
				dbgCalculatePath += $"SamplePosition() did NOT hit endpos.\n";
				path = null;
				return false; //todo: returning a boolean is newly added. Make sure this return boolean is being properly used...
			}


			path = null;
			return true;
		}

		/// <summary>
		/// Returns true if the supplied position is within the projection of any triangle on the navmesh, 
		/// projected along it's normal.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="projectedPoint">Closest point to the supplied position on the surface of the Navmesh</param>
		/// <returns></returns>
		public int AmWithinNavMeshProjection( Vector3 pos, out Vector3 projectedPoint )
        {
			DbgSamplePosition = $"Searching through '{Triangles.Length}' tris...\n";
			int rtrnIndx = -1;
			float runningClosestDist = float.MaxValue;

			Vector3 currentPt = Vector3.zero;
			projectedPoint = Vector3.zero;

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
					    projectedPoint = currentPt;
					    runningClosestDist = Vector3.Distance( pos, projectedPoint );
					    rtrnIndx = i;
				    }
				}
			}

			DbgSamplePosition += $"finished. returning: '{rtrnIndx}' with pt: '{projectedPoint}'\n";
			return rtrnIndx;
		}

        [SerializeField, HideInInspector] private string DbgSamplePosition;
		/// <summary>
		/// Gets a point on the projection of the navmesh using the supplied position. If the supplied position is not on the 
		/// projection of the navmesh, it calculates the closest point on the surface of the navmesh.
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
			hit = LNX_ProjectionHit.None;

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
					hit.Index_hitTriangle = i;
				}
            }

            DbgSamplePosition += $"finished. returning: '{hit.Index_hitTriangle}' with pt: '{hit.HitPosition}'\n";

            if( runningClosestDist <= maxDistance )
			{
				return true;
			}
			else
			{
				return false;
			}
        }

		public string DBGRaycast;
		
		/// <summary>
		/// Traces a line between two points on a navmesh.
		/// </summary>
		/// <returns>True if the ray is terminated before reaching target position. Otherwise returns false.</returns>
		public bool Raycast( Vector3 sourcePosition, Vector3 targetPosition, float maxSampleDistance )
		{
			DBGRaycast = "";

			#region GET START AND END POINTS------------------------------------------
			LNX_ProjectionHit lnxStartHit = LNX_ProjectionHit.None;
			LNX_ProjectionHit lnxEndHit = LNX_ProjectionHit.None;

			if ( !SamplePosition(sourcePosition, out lnxStartHit, maxSampleDistance) )
			{
				return true;
			}

			if ( !SamplePosition(targetPosition, out lnxEndHit, maxSampleDistance) )
			{
				return true;
			}
			#endregion

			if ( lnxStartHit.Index_hitTriangle == lnxEndHit.Index_hitTriangle )
			{
				return false;
			}
			DBGRaycast += $"Sampled start: '{lnxStartHit.HitPosition}', end: '{lnxEndHit.HitPosition}'\n";

			#region PROJECT THROUGH TO TARGET POSITION -------------------------------------------------
			Vector3 projectionDir = targetPosition - sourcePosition;
			DBGRaycast += $"project direction: '{projectionDir}'\n\n";
			bool amStillProjecting = true;
			LNX_Triangle currentTri = Triangles[lnxStartHit.Index_hitTriangle];
			Vector3 currentStartPos = lnxStartHit.HitPosition;

			DBGRaycast += "looping through mesh triangles...\n";
			while ( amStillProjecting )
			{
				DBGRaycast += $"currentTri: '{currentTri.Index_inCollection}', startPt: '{currentStartPos}'\n";
				LNX_Edge hitEdge = null;
				currentStartPos = currentTri.ProjectThroughToPerimeter( currentStartPos, lnxEndHit.HitPosition, out hitEdge, ProjectionDirection );
				DBGRaycast += $"projecting...\n" +
					$"{currentTri.dbgPerim}\n";

				DBGRaycast += $"projected to edge: '{hitEdge.MyCoordinate}'\n";

				if( hitEdge.AmTerminal )
				{
					DBGRaycast += $"edge is terminal...\n";
					amStillProjecting = false;
				}
				else
				{
					currentTri = Triangles[hitEdge.SharedEdge.TrianglesIndex];
					DBGRaycast += $"edge is NOT terminal, set current tri to: '{currentTri.Index_inCollection}'...\n";

					if ( currentTri.Index_inCollection == lnxEndHit.Index_hitTriangle )
					{
						amStillProjecting = false;
						DBGRaycast += $"currentTri has same index as end hit triangle. Stopping...\n";
					}
				}
			}

			DBGRaycast += $"finally returning: '{currentTri.Index_inCollection != lnxEndHit.Index_hitTriangle}'";

			return currentTri.Index_inCollection != lnxEndHit.Index_hitTriangle;

			#endregion
		}
		#endregion
	}
}