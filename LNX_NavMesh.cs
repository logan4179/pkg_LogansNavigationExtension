
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

		public LNX_Direction SurfaceOrientation = LNX_Direction.PositiveY;

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

		[HideInInspector] public Mesh _VisualizationMesh;

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
		[HideInInspector] public Vector3[] dbgMesh_vertices;
		[HideInInspector] public Vector3[] dbgMesh_normals;
		[HideInInspector] public int[] dbgMesh_triangles;
		[Space(10)]

		[HideInInspector] public int[] dbgTriangulation_Areas;
		[HideInInspector] public Vector3[] dbgTriangulation_Vertices;
		[HideInInspector] public int[] dbgTriangulation_Indices;
		[TextArea(0,10), NonSerialized] public string DBG_Relational;
		[Space(10)]

		[HideInInspector] public List<int> dbgKosher_Triangles = new List<int>(); //length should be 3x
		[HideInInspector] public List<Vector3> dbgKosher_vertices = new List<Vector3>();

		//[Header("ORIGINAL TRIANGULATION")]
		//public Vector3[] ot_vertices;
		//public int[] ot_indices;
		//public int[] ot_areas;

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


		public LNX_Triangle GetTriangle( LNX_Vertex vert )
		{
			return Triangles[vert.MyCoordinate.TrianglesIndex];
		}

		public LNX_Triangle GetTriangle( Vector3 center )
		{
			for( int i = 0; i < Triangles.Length; i++ )
			{
				if( Triangles[i].V_Center == center )
				{
					return Triangles[i];
				}
			}

			return null;
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
		public LNX_Edge GetEdge( LNX_ComponentCoordinate coord )
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

		public LNX_Edge GetEdge( int triIndex, int componentIndex )
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

			_VisualizationMesh = new Mesh();

			#region DEAL WITH TRIANGULATION -----------------------------------------------------------------------------
			NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
			Debug.Log($"inital triangulation has '{triangulation.areas.Length}' areas, '{triangulation.vertices.Length}' " +
				$"vertices, and '{triangulation.indices.Length}' indices.\n");

			List<LNX_Triangle> newTriCollection = new List<LNX_Triangle>();

			bool hvMods = HaveModifications();

			Debug.Log($"newtri list null: '{newTriCollection == null}'.");

			Debug.Log($"Now looping through triangulation to create triangle collection...");
			for ( int i = 0; i < triangulation.areas.Length; i++ )
			{
				Debug.Log($"{i} --------------------------////////////////////////////////////");
				if ( !hvMods || !ContainsDeletion(triangulation, i) )
				{
					Debug.Log($"instantiating tri...");
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
								//Debug.Log($"new tri '{i}' originally matches old tri '{j}'");
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
										$"matches original position of tri: '{i_Triangles}'. Changing position to: '{Triangles[i_Triangles].Verts[i_verts].V_Position}'...");

									constructedVertices_unique[i_uniqueVrts] = Triangles[i_Triangles].Verts[i_verts].V_Position;
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

			//Debug.Log($"for visualization mesh, constructed '{_mesh.triangles.Length}' tris (indices), '{_mesh.vertices.Length}' vertices, and '{_mesh.normals.Length}' normals...");

			RefreshMe( true );

			Debug.Log($"End of {nameof(CalculateTriangulation)}(). Created '{Triangles.Length}' triangles, and '{constructedVertices_unique.Count}' unique vertices for the mesh.");

			UnityEditor.EditorUtility.SetDirty( this );
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
		
		//I'm replacing this method with the one after it because that one seems more efficient. I'm leaving it in 
		//because frustratingly, I haven't been able to quantify how much more efficient it is because of the datetime/timespan 
		//thing not working like I'm expecting...
		public void MoveVert_managed( LNX_Vertex vert, Vector3 pos ) 
		{
			GetTriangle(vert).MoveVert_managed( this, vert.MyCoordinate.ComponentIndex, pos );

			if( vert.Index_VisMesh_Vertices > -1 && _VisualizationMesh != null && _VisualizationMesh.vertices != null && _VisualizationMesh.vertices.Length > 0 )
			{
				//Debug.Log($"moving vert '{vert.MyCoordinate.ToString()}' with {nameof(vert.Index_VisMesh_Vertices)}: '{vert.Index_VisMesh_Vertices}'");

				Vector3[] tmpVrts = _VisualizationMesh.vertices; //note: I can't get it to update the mesh if I only change the relevant vertex within the mesh object, It seems like I MUST create and assign a whole new array.
				tmpVrts[vert.Index_VisMesh_Vertices] = vert.V_Position;
				_VisualizationMesh.vertices = tmpVrts; //apparently you have to assign to the mesh in this manner in order to make this update (apparently I can't just change one of the existing vertices elements)...
			}
		}

		public void MoveSelectedVerts( List<LNX_Vertex> verts, Vector3 endPos )
		{
			Vector3[] tmpVrts = _VisualizationMesh.vertices; //note: I can't get it to update the vis mesh if I only
															 //change the position of the relevant verts within the vis mesh object, It seems like I MUST create and
															 //assign a whole new array, so that's what I'm doing here...

			bool visMeshValid = _VisualizationMesh != null && _VisualizationMesh.vertices != null && _VisualizationMesh.vertices.Length > 0;

			for ( int i = 0; i < verts.Count; i++ ) 
			{
				Triangles[verts[i].MyCoordinate.TrianglesIndex].MoveVert_managed( this, verts[i].MyCoordinate.ComponentIndex, endPos);

				if ( verts[i].Index_VisMesh_Vertices > -1 && visMeshValid )
				{
					tmpVrts[verts[i].Index_VisMesh_Vertices] = verts[i].V_Position;
				}
			}

			_VisualizationMesh.vertices = tmpVrts; //apparently you have to assign to the mesh in this manner in
			//order to make this update (apparently I can't just change one of the existing vertices elements)...
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

			RefreshMe( true );
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

		public bool ContainsDeletion( NavMeshTriangulation nmTriangulation, int areaIndex ) //todo: definitely unit test this...
		{
			string methodReport = $"ContainsDeletion(). checking vert in list starting at the vert at index: '{nmTriangulation.indices[areaIndex * 3]}', position: '{nmTriangulation.vertices[nmTriangulation.indices[areaIndex * 3]]}'...";

			if ( deletedTriangles != null && deletedTriangles.Count > 0 )
			{
				for ( int i = 0; i < deletedTriangles.Count; i++ )
				{
					if(
						deletedTriangles[i].HasVertAtOriginalPosition(nmTriangulation.vertices[nmTriangulation.indices[areaIndex * 3]]) &&
						deletedTriangles[i].HasVertAtOriginalPosition(nmTriangulation.vertices[nmTriangulation.indices[(areaIndex*3) + 1]]) &&
						deletedTriangles[i].HasVertAtOriginalPosition(nmTriangulation.vertices[nmTriangulation.indices[(areaIndex*3) + 2]])
					)
					{
						methodReport += $"found DO contain deletion. passed index: '{areaIndex}'...";
						Debug.LogWarning(methodReport);
						return true;
					}

					//dws - this old way only checked a single vert...wtf?
					/*
					if ( deletedTriangles[i].GetVertIndextAtOriginalPosition(nmTriangulation.vertices[nmTriangulation.indices[areaIndex * 3]]) != -1 )
					{
						methodReport += $"found DO contain deletion. passed index: '{areaIndex}'...";
						Debug.LogWarning( methodReport );
						return true;
					}
					*/
				}
			}

			return false;
		}
		#endregion

		#region ADDING ---------------------------------------------------------------
		public void AddTriangles( params LNX_Triangle[] addedTris )
		{
			Debug.Log($"{nameof(AddTriangles)}(). Was passed '{addedTris.Length}' tris...");

			List<LNX_Triangle> constructedLnxTriangles = Triangles.ToList();

			for ( int i = 0; i < addedTris.Length; i++ )
			{
				//Debug.Log($"i: '{i}'...");
				addedTris[i].WasAddedViaMod = true;
				constructedLnxTriangles.Add( addedTris[i] );
			}

			Triangles = constructedLnxTriangles.ToArray();

			RefreshMe( true );
		}
		#endregion

		#region CALCULATION ---------------------------------------------------------
		public void RefreshMe( bool meshContinuityHasChanged ) //NEW
		{
			//Debug.Log($"{nameof(RefreshMe)}()---------------------------");

			//Debug.Log($"now looping through '{Triangles.Length}' triangles...");

			DateTime dt_start = DateTime.Now;

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				//Debug.Log($"I: '{i}'...");
				Triangles[i].RefreshMe( this, meshContinuityHasChanged );
				TimeSpan ts_total = DateTime.Now.Subtract(dt_start);
				//Debug.Log($"ts_crntLoop: '{ts_total.ToString()}', ms: '{ts_total.TotalMilliseconds}'");

				if ( ts_total.TotalSeconds > 10 )
				{
					Debug.LogError($"timespan went beyond limit. breaking early...");
					return;
				}
			}

			Debug.Log($"Refresh loop finished after '{DateTime.Now.Subtract(dt_start).TotalSeconds}' seconds. Now calculating bounds...");

			CalculateBounds();

			//dt_start = DateTime.Now;
			if( meshContinuityHasChanged )
			{
				ReconstructVisualizationMesh();
			}
			//TimeSpan ts = DateTime.Now.Subtract(dt_start);
			//Debug.Log($"{nameof(ReconstructVisualizationMesh)}() finished after timespan of '{ts}', ms: '{ts.Milliseconds}'...");
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
					if (Triangles[i].Verts[j].V_Position.x < Bounds[0])
					{
						Bounds[0] = Triangles[i].Verts[j].V_Position.x;
					}
					else if (Triangles[i].Verts[j].V_Position.x > Bounds[1])
					{
						Bounds[1] = Triangles[i].Verts[j].V_Position.x;
					}

					if (Triangles[i].Verts[j].V_Position.y < Bounds[2])
					{
						Bounds[2] = Triangles[i].Verts[j].V_Position.y;
					}
					else if (Triangles[i].Verts[j].V_Position.y > Bounds[3])
					{
						Bounds[3] = Triangles[i].Verts[j].V_Position.y;
					}

					if (Triangles[i].Verts[j].V_Position.z < Bounds[4])
					{
						Bounds[4] = Triangles[i].Verts[j].V_Position.z;
					}
					else if (Triangles[i].Verts[j].V_Position.z > Bounds[5])
					{
						Bounds[5] = Triangles[i].Verts[j].V_Position.z;
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

		/// <summary>
		/// Re-constructs the visualization mesh for the scene. Use this
		/// in cases where the Mesh needs to be re-made when the Triangle info can be assumed to be correct/un-changed. IE: When Unity is 
		/// closed and reopened, and the mesh information needs to be remade because it's not serialized.
		/// </summary>
		public void ReconstructVisualizationMesh()
		{
			Debug.Log($"ReconstructVisualizationMesh()");

			bool dbgMethod = false;

			/*
			Note: I used to have a lot of the loops in here bundled together for more efficiency, but it looked 
			awful, so I separated them into multiple loops to make them more debuggable. I think this added overhead 
			is okay because this method is not meant to be called during performance crticial moments. This is only
			for occasional, discrete, calls in the editor as needed. This is a slow method, and that's okay.
			Note: Added overhead doesn't seem to be a problem, because even with the debug logs, I'm clocking this method only taking about 33 ms normally.
			*/

			_VisualizationMesh = new Mesh();

			bool listIsStillKosher = true;
			bool mainIndicesAreUnbroken = true;
			bool vertPositionsAreConsistentWithMeshVertPositions = true;

			List<int> mesh_triangles = new List<int>();

			#region ASSEMBLE THE UNIQUE VERTICES LIST ----------------------------------------------
			if( dbgMethod ) Debug.Log($"First, looking through '{Triangles.Length}' triangles to assemble a list of unique vertices...");
			List<Vector3> uniqueVerts = new List<Vector3>();
			int greatestVertMeshIndex = 0; //We'll keep track of the greatest vertMeshIndex while we're at it...

			for (int i_Triangles = 0; i_Triangles < Triangles.Length; i_Triangles++)
			{
				for (int i_Verts = 0; i_Verts < 3; i_Verts++)
				{
					if (dbgMethod) Debug.Log($"inspecting vert '{i_Triangles},{i_Verts}' at position: '{Triangles[i_Triangles].Verts[i_Verts].V_Position}'. vismeshindx: '{Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices}'...");
					bool foundVertInUniqueList = false;
					for (int i_uniqueVrts = 0; i_uniqueVrts < uniqueVerts.Count; i_uniqueVrts++)
					{
						if (Triangles[i_Triangles].Verts[i_Verts].V_Position == uniqueVerts[i_uniqueVrts])
						{
							foundVertInUniqueList = true;
						}
					}

					if (!foundVertInUniqueList)
					{
						uniqueVerts.Add(Triangles[i_Triangles].Verts[i_Verts].V_Position);
					}

					if (Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices > greatestVertMeshIndex)
					{
						if (dbgMethod)
						{
							Debug.Log($"Found new greatest vertmeshindex of '{Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices}' " +
							$"at vert '[{i_Triangles}][{i_Verts}]'");
						}

						greatestVertMeshIndex = Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices;
					}
				}
			}

			if (dbgMethod) Debug.Log($"End of loop. uniqueVerts list is now '{uniqueVerts.Count}' long. greatestVerMeshIndex: '{greatestVertMeshIndex}'");
			#endregion

			if (greatestVertMeshIndex != (uniqueVerts.Count - 1))
			{
				if (dbgMethod)
				{
					Debug.Log($"{nameof(greatestVertMeshIndex)} ({greatestVertMeshIndex}) was not the same as the count of " +
					$"'{nameof(uniqueVerts)}' ({uniqueVerts.Count}) minus one. Decided was NOT kosher...");
				}

				listIsStillKosher = false;
			}

			#region CHECK THAT THE TRIANGLE INDICES ARE UNBROKEN ---------------------------------
			if (dbgMethod) Debug.Log("checking that all verts' visualization mesh indices are continuous/unbroken all the way to the largest index...");
			for (int i_Triangles = 0; i_Triangles < Triangles.Length; i_Triangles++)
			{
				if (mainIndicesAreUnbroken && Triangles[i_Triangles].Index_inCollection != i_Triangles)
				{
					listIsStillKosher = false;
					mainIndicesAreUnbroken = false;
					if (dbgMethod) Debug.Log($"Found that triangle{i_Triangles}'s main index property does NOT align with it's position in the collection. list is not kosher...");
					break;
				}
			}
			#endregion

			#region CHECK THAT ALL VERT POSITIONS CORRESPOND TO THEIR VISMESH VERT POSITION -----------------------
			if (dbgMethod) Debug.Log($"Checking vismesh vert indices...");
			for (int i_Triangles = 0; i_Triangles < Triangles.Length; i_Triangles++)
			{
				if (dbgMethod) Debug.Log($"i_Triangles: '{i_Triangles}'...");
				for (int i_Verts = 0; i_Verts < 3; i_Verts++)
				{
					if (dbgMethod) Debug.Log($"checking vert: '{i_Triangles},{i_Verts}'. vismeshindx: '{Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices}'...");

					if (Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices < 0 ||
						Triangles[i_Triangles].Verts[i_Verts].V_Position != uniqueVerts[Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices]) //todo: this is where the unit test is failing
					{
						listIsStillKosher = false;
						vertPositionsAreConsistentWithMeshVertPositions = false;

						if (dbgMethod)
						{
							Debug.Log($"vert[{i_Triangles}],[{i_Verts}]'s position ({Triangles[i_Triangles].Verts[i_Verts].V_Position}) did NOT match unique " +
							$"vert{Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices}'s position. Decided list was NOT kosher...");
						}

						break;
					}
				}

				if (!vertPositionsAreConsistentWithMeshVertPositions) //This way it doesn't loop through any more triangles...
				{
					break;
				}
			}
			#endregion

			if (!listIsStillKosher)
			{
				if (dbgMethod) Debug.Log($"Collection didn't pass 'isKosher' check. Attempting fix...");

				#region FIX BROKEN INDICES ------------------------------------------------------------------------------
				if (!mainIndicesAreUnbroken)
				{
					if (dbgMethod) Debug.Log("Main Triangle cached indices had issues. Attempting to fix main indices...");

					for (int i_Triangles = 0; i_Triangles < Triangles.Length; i_Triangles++)
					{
						if (Triangles[i_Triangles].Index_inCollection != i_Triangles)
						{
							if (dbgMethod) Debug.Log($"Triangle '{i_Triangles}' had cached index of: '{Triangles[i_Triangles].Index_inCollection}'. Fixing...");
							Triangles[i_Triangles].ChangeIndex_action(i_Triangles);
						}
					}

				}
				#endregion

				#region FIX INCONSISTENT VERT INDICES ---------------------------------------------------------
				if (!vertPositionsAreConsistentWithMeshVertPositions)
				{
					for (int i_Triangles = 0; i_Triangles < Triangles.Length; i_Triangles++)
					{
						for (int i_Verts = 0; i_Verts < 3; i_Verts++)
						{
							if (Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices < 0 ||
								Triangles[i_Triangles].Verts[i_Verts].V_Position != uniqueVerts[Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices]) //this is going out of range...
							{
								bool foundUniqueVertMatch = false;
								for (int i_uniqueVerts = 0; i_uniqueVerts < uniqueVerts.Count; i_uniqueVerts++)
								{
									if (Triangles[i_Triangles].Verts[i_Verts].V_Position == uniqueVerts[i_uniqueVerts])
									{
										Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices = i_uniqueVerts;
										foundUniqueVertMatch = true;
										break;
									}
								}

								if (!foundUniqueVertMatch)
								{
									if (dbgMethod) Debug.LogError($"Vert [{i_Triangles}][{i_Verts}] couldn't find a match in the unique vert list."); //I don't think it's possible this will ever happen 
																																	   //because I think the assembly of the uniqueverts list should catch all vert positions.
								}
							}
						}
					}
				}
				#endregion

				#region ASSEMBLE MESH TRIANGLES COLLECTION -----------------------------------------------------
				mesh_triangles = new List<int>(); //now we can't trust what we logged earlier to this list...
				for (int i_Triangles = 0; i_Triangles < Triangles.Length; i_Triangles++)
				{
					for (int i_Verts = 0; i_Verts < 3; i_Verts++)
					{
						mesh_triangles.Add(Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices);
					}
				}
				#endregion
			}
			else
			{
				if (dbgMethod) Debug.Log("Considered navmesh to be kosher. constructing vismesh lists...");

				for (int i_Triangles = 0; i_Triangles < Triangles.Length; i_Triangles++)
				{
					for (int i_Verts = 0; i_Verts < 3; i_Verts++)
					{
						mesh_triangles.Add(Triangles[i_Triangles].Verts[i_Verts].Index_VisMesh_Vertices);
					}
				}
			}

			#region CREATE VISUALIZATION MESH ---------------------------------
			_VisualizationMesh.vertices = uniqueVerts.ToArray();

			Vector3[] nrmls = new Vector3[uniqueVerts.Count];
			for (int i = 0; i < nrmls.Length; i++)
			{
				nrmls[i] = Vector3.up; //todo: What should I actually do here?
			}
			_VisualizationMesh.normals = nrmls;

			_VisualizationMesh.triangles = mesh_triangles.ToArray(); //apparently this MUST come AFTER setting the vertices or will throw error

			dbgMesh_triangles = _VisualizationMesh.triangles;
			dbgMesh_vertices = _VisualizationMesh.vertices;
			dbgMesh_normals = _VisualizationMesh.normals;
			#endregion

			if (dbgMethod) Debug.Log($"end of ReconstructVisualizationMesh()");
		}
		#endregion

		#region MAIN API METHODS----------------------------------------------------------------
		/*[SerializeField]*/
		[HideInInspector] public string dbgCalculatePath;

		/// <summary>
		/// Returns a Vector3 representing the surface normal dictated by the SurfaceOrientation variable.
		/// </summary>
		/// <returns></returns>
		public Vector3 GetSurfaceNormal()
		{
			if (SurfaceOrientation == LNX_Direction.PositiveY)
			{
				return Vector3.up;
			}
			if ( SurfaceOrientation == LNX_Direction.NegativeY )
			{
				return Vector3.down;
			}
			else if ( SurfaceOrientation == LNX_Direction.PositiveX )
			{
				return Vector3.right;
			}
			else if ( SurfaceOrientation == LNX_Direction.NegativeX )
			{
				return Vector3.left;
			}
			else if ( SurfaceOrientation == LNX_Direction.PositiveZ )
			{
				return Vector3.forward;
			}
			else if ( SurfaceOrientation == LNX_Direction.NegativeZ )
			{
				return Vector3.back;
			}

			Debug.LogError($"LNX ERROR! {nameof(SurfaceOrientation)} needs to be set in order to run this operation!");
			return Vector3.zero;
		}

		public bool CalculatePath( Vector3 startPos_passed, Vector3 endPos_passed, float maxSampleDistance, out LNX_Path outPath, bool considerOffPerimeter = true)
		{
			LNX_ProjectionHit startHit = new LNX_ProjectionHit();
			LNX_ProjectionHit endHit = new LNX_ProjectionHit();
			outPath = LNX_Path.None;

			dbgCalculatePath = $"{nameof(CalculatePath)}(strt: '{startPos_passed}', end: '{endPos_passed}', smplDst: '{maxSampleDistance}' " +
				$"at {DateTime.Now.ToString()})\n";

			if( SamplePosition(startPos_passed, out startHit, maxSampleDistance, considerOffPerimeter) )
			{
				//startPos_passed = startHit.HitPosition; //do we really need this? dws
				dbgCalculatePath += $"SamplePosition() hit startpos on tri '{startHit.Index_Hit}', at: '{startHit.HitPosition}'\n";
			}
			else
			{
				dbgCalculatePath += $"SamplePosition() did NOT hit startpos.\n";
				return false; //todo: returning a boolean is newly added. Make sure this return boolean is being properly used...
			}

			if ( SamplePosition(endPos_passed, out endHit, maxSampleDistance, considerOffPerimeter) )
			{
				//endPos_passed = endHit.HitPosition; //do we really need this? dws
				dbgCalculatePath += $"SamplePosition() hit endpos on tri '{endHit.Index_Hit}', at: '{endHit.HitPosition}'\n";
			}
			else
			{
				dbgCalculatePath += $"SamplePosition() did NOT hit endpos.\n";
				return false; //todo: returning a boolean is newly added. Make sure this return boolean is being properly used...
			}

			#region CONSTRUCT PATH -------------------------------------
			dbgCalculatePath += $"now trying to construct path...\n";
			LNX_Triangle currentTri = Triangles[startHit.Index_Hit];

			List<Vector3> pthPts = new List<Vector3>() { startHit.HitPosition };
			List<Vector3> pthNormals = new List<Vector3>() { currentTri.V_PathingNormal };

			int whileIterations = 0;

			bool finishedPath = false;
			while ( !finishedPath )
			{
				dbgCalculatePath += $"\nwhile...\n" +
					$"projecting from tri: '{currentTri.Index_inCollection}'...\n";
				LNX_ProjectionHit perimHit = currentTri.ProjectThroughToPerimeter(
					pthPts[pthPts.Count-1], endHit.HitPosition );

				if ( perimHit.Index_Hit > -1 && perimHit.Index_Hit < 3 )
				{
					dbgCalculatePath += $"perimHit was good on current tri on edge: '{perimHit.Index_Hit}'\n" +
						$"sharededgecoordinate: '{currentTri.Edges[perimHit.Index_Hit].SharedEdgeCoordinate}'...\n";
					pthPts.Add( perimHit.HitPosition );

					if(currentTri.Edges[perimHit.Index_Hit].SharedEdgeCoordinate == LNX_ComponentCoordinate.None )
					{
						Debug.LogWarning($"hit sharededgecoordinate was none...");
					}
					pthNormals.Add( GetTriangle(currentTri.Edges[perimHit.Index_Hit].SharedEdgeCoordinate).V_PathingNormal ); //I think I need an error check that makes sure the edge isn't terminal first...

					// Check if we're touching the last triangle...
					if ( /*currentTri.Edges[perimHit.Index_Hit].MyCoordinate.TrianglesIndex == endHit.Index_Hit ||*/
						currentTri.Edges[perimHit.Index_Hit].SharedEdgeCoordinate.TrianglesIndex == endHit.Index_Hit
					)
					{
						dbgCalculatePath += $"prjoect is now touching last tri...\n";
						pthPts.Add( endHit.HitPosition );
						pthNormals.Add( Triangles[endHit.Index_Hit].V_PathingNormal );

						outPath = new LNX_Path( pthPts, pthNormals );
						finishedPath = true;
					}
					else
					{
						currentTri = Triangles[currentTri.Edges[perimHit.Index_Hit].SharedEdgeCoordinate.TrianglesIndex];
					}
				}
				else
				{
					dbgCalculatePath += $"perimeter hit returned out of range index: '{perimHit.Index_Hit}'. Returning false...\n";
					return false;
				}

				whileIterations++;
				if( whileIterations > 16 )
				{
					dbgCalculatePath += $"while iterations went too long. Exiting early...\n";
					return false;
				}
			}
			#endregion

			return true;
		}

		[HideInInspector] public string DBG_NavmeshProjection;
		/// <summary>
		/// Returns true if the supplied position is within the projection of any triangle on the navmesh, 
		/// projected along it's normal.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="projectedPoint">Closest point to the supplied position on the surface of the Navmesh</param>
		/// <returns></returns>
		public bool AmWithinSurfaceProjection( Vector3 pos, out LNX_ProjectionHit hit, float maxDistance ) //todo: unit test this method
        {
			DBG_NavmeshProjection = $"Searching through '{Triangles.Length}' tris...\n";
			int rtrnIndx = -1;
			float runningClosestDist = maxDistance;

			hit = LNX_ProjectionHit.None;

			Vector3 currentPt = Vector3.zero;
			Vector3 runningBestPt = Vector3.zero;

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				DBG_NavmeshProjection += $"i: '{i}'....................\n";
				LNX_Triangle tri = Triangles[i];

				if ( tri.IsInShapeProject(pos, out currentPt) ) //ERRORTRACE 8
				{
					DBG_NavmeshProjection += $"found AM in shape project at '{currentPt}'...\n";
					//note: The reason I'm not immediately returning this tri here is because concievably
					// you could have two navmesh polys "on top of each other", (IE: in line with
					// each other's normals), which would result in more than one tri considering
					// this point to be within it's bounds, and I need to decide which one is
					// the better option...

				    if ( Vector3.Distance(pos, currentPt) < runningClosestDist )
				    {
						runningBestPt = currentPt;
					    runningClosestDist = Vector3.Distance( pos, runningBestPt);
					    rtrnIndx = i;
				    }
				}
			}

			if( rtrnIndx > -1 )
			{
				DBG_NavmeshProjection += $"finished. returning: '{rtrnIndx}' with pt: '{runningBestPt}'\n";

				hit = new LNX_ProjectionHit( rtrnIndx, runningBestPt );
				return true;
			}
			else
			{
				DBG_NavmeshProjection += $"finished. Didn't find projection position.\n";

				return false;
			}
		}

		[HideInInspector] public string DBG_SamplePosition;

		/// <summary>
		/// Gets a point on the projection of the navmesh using the supplied position. If the supplied position is not on the 
		/// projection of the navmesh, it calculates the closest point on the surface of the navmesh.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="hit"></param>
		/// <param name="maxDistance"></param>
		/// <returns></returns>
		public bool SamplePosition( Vector3 pos, out LNX_ProjectionHit hit, float maxDistance, bool considerOffPerimeter = true )
        {
			DBG_SamplePosition = $"SamplePosition('{pos}'). Searching through '{Triangles.Length}' tris...\n";

			if( Vector3.Distance(V_BoundsCenter, pos) > (maxDistance + BoundsContainmentDistanceThreshold) )
			{
				DBG_SamplePosition += $"distance threshold short circuit";
				hit = LNX_ProjectionHit.None;
				return false;
			}

            float runningClosestDist = float.MaxValue;
			float currentDist = float.MaxValue;

			Vector3 currentPt = Vector3.zero;
			hit = LNX_ProjectionHit.None;

            for ( int i = 0; i < Triangles.Length; i++ )
            {
				DBG_SamplePosition += $"i: '{i}'....................\n";
                LNX_Triangle tri = Triangles[i];

				if ( tri.IsInShapeProject(pos, out currentPt) )
				{
					DBG_SamplePosition += $"found AM in shape project at '{currentPt}'...\n";
					//note: The reason I'm not immediately returning this tri here is because concievably
					// you could have two navmesh polys "on top of each other", (IE: in line with
					// each other's normals), which would result in more than one tri considering
					// this point to be within it's bounds, and you need to decide which one is
					// the better option...
					currentDist = Vector3.Distance(pos, currentPt);
				}
                else
                {
					DBG_SamplePosition += $"found am NOT in shape project...\n";

					/*if( i == 3 ) //use this to specify a certain tri to report. This is so that I can restrict the length of this report
					{
						DBG_SamplePosition += $" Triangle.IsInShapeProject report:\n" +
							$"{tri.DBG_IsInShapeProjectAlongNormal}\n\n";
					}*/

					if ( considerOffPerimeter )
					{
						currentPt = tri.ClosestPointOnPerimeter( pos );
						currentDist = Vector3.Distance(pos, currentPt);
					}
					else
					{
						currentDist = float.MaxValue;
					}
				}

				DBG_SamplePosition += $"dist: '{currentDist}'\n...";

				if ( currentDist < runningClosestDist )
				{
					DBG_SamplePosition += $"new closest point at: '{currentDist}'...\n";
					hit.HitPosition = currentPt;
					runningClosestDist = currentDist;
					hit.Index_Hit = i;
				}
            }

			DBG_SamplePosition += $"finished. returning: '{hit.Index_Hit}' with pt: '{hit.HitPosition}'\n";

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
		public string DBG_timetaken;

		public Transform TryTrans;
		/// <summary>
		/// Traces a line between two points on a navmesh.
		/// </summary>
		/// <returns>True if the ray is terminated before reaching target position. Otherwise returns false.</returns>
		public bool Raycast( Vector3 sourcePosition, Vector3 targetPosition, float maxSampleDistance, 
			bool onlySampleWithinNormalProject = true )
		{
			DateTime dt_methodStart = DateTime.Now;
			DBGRaycast = "";
			DBG_timetaken = "";

			#region GET START AND END POINTS------------------------------------------
			LNX_ProjectionHit lnxStartHit = LNX_ProjectionHit.None;
			LNX_ProjectionHit lnxEndHit = LNX_ProjectionHit.None;

			if( !AmWithinSurfaceProjection(sourcePosition, out lnxStartHit, maxSampleDistance) )
			{
				DBGRaycast += $"SourcePosition NOT within navmesh projection...\n" +
					$"\nAmWithinNavMeshProjection report:\n" +
					$"{DBG_NavmeshProjection}\n";

				if ( onlySampleWithinNormalProject )
				{
					DBGRaycast += $"not instructed to try samplePosition. Returning early...";
					return true;
				}
				else
				{
					DBGRaycast += $"trying samplePosition...\n";
					if (!SamplePosition(sourcePosition, out lnxStartHit, maxSampleDistance))
					{
						DBGRaycast += $"tried samplePosition. Still didn't work. Returning early...\n";
						return true;
					}
				}
			}
			else
			{
				DBGRaycast += $"sourcePosition WAS within navmeshprojection!\n" +
					$"start projection: '{lnxStartHit}' ({LNX_UnitTestUtilities.LongVectorString(lnxStartHit.HitPosition)})\n";
			}

			if ( !AmWithinSurfaceProjection(targetPosition, out lnxEndHit, maxSampleDistance) )
			{
				DBGRaycast += $"targetPosition NOT within navmesh projection...\n";

				if (onlySampleWithinNormalProject)
				{
					DBGRaycast += $"not instructed to try samplePosition. Returning early...";
					return true;
				}
				else
				{
					DBGRaycast += $"trying samplePosition...\n";
					if (!SamplePosition(targetPosition, out lnxEndHit, maxSampleDistance))
					{
						DBGRaycast += $"tried samplePosition. Still didn't work. Returning early...\n";
						return true;
					}
				}
			}
			else
			{
				DBGRaycast += $"targetPosition WAS within navmeshprojection!\n" +
					$"end projection: '{lnxEndHit}' ({LNX_UnitTestUtilities.LongVectorString(lnxEndHit.HitPosition)})\n";
			}

			#endregion

			if (lnxStartHit.Index_Hit == lnxEndHit.Index_Hit) //Short-circuit: If start and end hit are on same triangle...
			{
				DBGRaycast += $"Short-circuiting. Start and end hits are on the same triangle surface...";
				return false;
			}

			#region PROJECT THROUGH TO TARGET POSITION -------------------------------------------------
			LNX_Triangle currentTri = Triangles[lnxStartHit.Index_Hit];
			Vector3 currentStartPos = lnxStartHit.HitPosition;
			int currentEdgeIndex = -1;

			int safetyTimeout = 20;
			int runningWhileIterations = 0;

			DBGRaycast += "\nlooping through mesh triangles...\n\n";
			bool amStillProjecting = true;
			while ( amStillProjecting )
			{
				DBGRaycast += $"\nwhile{runningWhileIterations}=============================================== " +
					$"\n(currentTri: '{currentTri.Index_inCollection}', startPt: '{LNX_UnitTestUtilities.LongVectorString(currentStartPos)}')\n" +
					$"projectingThroughToPerim...\n";

				LNX_ProjectionHit perimHit = currentTri.ProjectThroughToPerimeter( currentStartPos, lnxEndHit.HitPosition, currentEdgeIndex );
				DBGRaycast += $"tri.prjctThrToPerim report----------------\n" +
					$"{currentTri.dbg_prjctThrhToPerim}" +
					$"end rprt------------\n";

				if ( perimHit.Index_Hit < 0 || perimHit.Index_Hit > 2 )
				{
					DBGRaycast += $"after project, hit object has bad index of '{perimHit.Index_Hit}'. Dumping triangle report...\n" +
					$"{currentTri.dbg_prjctThrhToPerim}.\n" +
					$"now returning...";
					return true;
				}

				LNX_Edge hitEdge = currentTri.Edges[perimHit.Index_Hit];
				currentEdgeIndex = perimHit.Index_Hit;
				currentStartPos = perimHit.HitPosition;

				DBGRaycast += $"projected to edge: '{hitEdge.MyCoordinate}' at '{LNX_UnitTestUtilities.LongVectorString(perimHit.HitPosition)}'. " +
					$"Shared edge is: '{hitEdge.SharedEdgeCoordinate}'\n";

				if ( hitEdge.AmTerminal )
				{
					DBGRaycast += $"hit edge is terminal. Stoping loop...\n";
					amStillProjecting = false;
				}
				else
				{
					DBGRaycast += $"edge is NOT terminal. Checking to see if we're at the end...\n";
					DBGRaycast += $"test1, Triangles[{currentTri.Index_inCollection}].AmAdjacentToTri({lnxEndHit.Index_Hit}): " +
						$"'{currentTri.AmAdjacentToTri(lnxEndHit.Index_Hit)}'\n" +
						$"";

					if(
						hitEdge.SharedEdgeCoordinate.TrianglesIndex == lnxEndHit.Index_Hit ||
						(
							currentTri.AmAdjacentToTri(lnxEndHit.Index_Hit) &&
							currentTri.IsPositionOnAnyEdge(lnxEndHit.HitPosition)
						)
					)
					{
						currentTri = Triangles[lnxEndHit.Index_Hit];

						amStillProjecting = false;
						DBGRaycast += $"Decided AM at the end. Stopping...\n";
					}
					else
					{
						currentTri = Triangles[hitEdge.SharedEdgeCoordinate.TrianglesIndex];
						currentEdgeIndex = hitEdge.SharedEdgeCoordinate.ComponentIndex;
						currentStartPos = perimHit.HitPosition;

						DBGRaycast += $"Decided NOT at the end. Set new current tri to: '{currentTri.Index_inCollection}'...\n";
					}
				}


				runningWhileIterations++;
				if (runningWhileIterations > safetyTimeout)
				{
					Debug.LogError($"while loop went for more than '{safetyTimeout}' iterations. dbg string says: \n{DBGRaycast}\nBreaking early...");
					amStillProjecting = false;
					return true;
				}
			}
			#endregion

			DBGRaycast += $"finally returning: '{currentTri.Index_inCollection != lnxEndHit.Index_Hit}'\n";
			DBG_timetaken += $"time: '{DateTime.Now.Subtract(dt_methodStart)}'";

			return currentTri.Index_inCollection != lnxEndHit.Index_Hit;

		}
		#endregion // (END) MAIN API METHODS---------------------

		[ContextMenu("z call SayCurrentInfo()")]
		public void SayCurrentInfo()
		{
			for (int i = 0; i < Triangles.Length; i++)
			{
				Triangles[i].SayCurrentInfo();
			}
		}
	}
}