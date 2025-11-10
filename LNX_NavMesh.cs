
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;


namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_NavMesh : MonoBehaviour
	{
		public LNX_Direction SurfaceOrientation = LNX_Direction.PositiveY;
		[SerializeField] private Vector3 v_surfaceOrientation_cached;
		public Vector3 V_SurfaceOrientation => v_surfaceOrientation_cached;

		public string LayerMaskName;

		private int cachedLayerMask;
		public int CachedLayerMask => cachedLayerMask;

		/*[HideInInspector]*/ public LNX_Triangle[] Triangles;

		[HideInInspector] public Vector3[] Vertices;

		//[SerializeField] private List<LNX_Triangle> deletedTriangles;
		[SerializeField, HideInInspector] private List<LNX_AtomicTriangle> deletedTriangles;

		private int addedTrianglesStartIndex = -1; //todo: what is this? Need to comment what it is

		[HideInInspector] public Mesh _VisualizationMesh;

		[Header("BOUNDS")]
		/// <summary>Stores the largest/smallest X, Y, and Z value of the navmesh. Elements 0 and 1 are lowest and 
		/// hightest X, elements 2 and 3 are lowest and highest Y, and elements 4 and 5 are lowest and highest z.</summary>
		[HideInInspector] public float[] Bounds;

		/// <summary>Stores the largest/smallest points defining the bounds of a navmesh. Elements 0-3 form the lower horizontal square of the 
		/// box, while 4-6 form the higher horizontal square of the bounding box. These theoretical boxes each run clockwise. Element 0 
		/// will be the lowest/most-negative value point, and element 4 will be the most positive value point</summary>
		[HideInInspector] public Vector3[] V_Bounds;

		[HideInInspector] public Vector3 V_BoundsCenter;

		//TODO: should unit test all these properties
		public float Bounds_LowestX => Bounds[0];
		public float Bounds_HighestX => Bounds[1];

		public float Bounds_LowestY => Bounds[2];
		public float Bounds_HighestY => Bounds[3];

		public float Bounds_LowestZ => Bounds[4];
		public float Bounds_HighestZ => Bounds[5];

		[HideInInspector] public Vector3 V_BoundsSize
		{
			get
			{
				return new Vector3(
					Mathf.Abs(Bounds[0] - Bounds[1]),
					Mathf.Abs(Bounds[2] - Bounds[3]),
					Mathf.Abs(Bounds[4] - Bounds[5])
				);
			}
		}

		/// <summary>
		/// Longest distance from the bounds center to any corner on the bounding box. This is used as an efficiency value 
		/// in order to short-circuit (return early) from certain methods that don't need to run further logic based on the 
		/// value of this threshold..
		/// </summary>
		[HideInInspector] public float BoundsContainmentDistanceThreshold
		{
			get
			{
				return Mathf.Max
				(
					Vector3.Distance(V_BoundsCenter, V_Bounds[0]),
					Vector3.Distance(V_BoundsCenter, V_Bounds[4])
				);
			}
		}


		//[Header("flags")]

		[Header("VISUAL/DEBUG")]
		public Color color_mesh;

		private void OnEnable()
		{
			Debug.Log("lnx_navmesh.onenable()");
		}

		private void Start()
		{
			#if DOING_WORK
			//ASDF
			#else
				
			#endif
		}

		#region Triangle fetchers ------------------------------------------------------
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

		public LNX_Triangle GetClosestTriangleToPosition(Vector3 pos)
		{
			float runningClosestDist = float.MaxValue;
			int runningBestTriIndex = 0;

			for (int i = 0; i < Triangles.Length; i++)
			{
				if (Vector3.Distance(pos, Triangles[i].V_Center) < runningClosestDist)
				{
					runningClosestDist = Vector3.Distance(pos, Triangles[i].V_Center );
					runningBestTriIndex = i;
				}
			}

			return Triangles[runningBestTriIndex];
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

		public LNX_Vertex GetClosestVertexToPosition(Vector3 pos)
		{
			float runningClosestDist = float.MaxValue;
			int runningBestTriIndex = 0;
			int runningBestVertIndex = 0;

			for( int i = 0; i < Triangles.Length; i++ )
			{
				for ( int j = 0; j < 3; j++ )
				{
					if( Vector3.Distance(pos, Triangles[i].Verts[j].V_Position) < runningClosestDist )
					{
						runningClosestDist = Vector3.Distance(pos, Triangles[i].Verts[j].V_Position);
						runningBestTriIndex = i;
						runningBestVertIndex = j;
					}
				}
			}

			return Triangles[runningBestTriIndex].Verts[runningBestVertIndex];
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

		[TextArea(1,20)] public string DBG_GetTerminalEdges;
		//public LNX_Edge[] GottenEdges;

		public LNX_Edge[] GetTerminalEdges(bool includeBoundsEdges)
		{
			DBG_GetTerminalEdges = "";
			List<LNX_Edge> temp_terminalEdges = new List<LNX_Edge>();

			for ( int i_tris = 0; i_tris < Triangles.Length; i_tris++ )
			{
				for ( int i_edges = 0; i_edges < 3; i_edges++ )
				{
					if ( Triangles[i_tris].Edges[i_edges].AmTerminal )
					{
						if
						( 
							includeBoundsEdges ||
							!Triangles[i_tris].Edges[i_edges].AmBoundsEdge(this)
						)
						{
							temp_terminalEdges.Add( Triangles[i_tris].Edges[i_edges] );
							DBG_GetTerminalEdges += $"Added: '{temp_terminalEdges[temp_terminalEdges.Count-1]}'\n";
						}
					}
				}
			}

			DBG_GetTerminalEdges += $"\n End. Now have '{temp_terminalEdges.Count}' edges...";
			return temp_terminalEdges.ToArray();
		}
		#endregion

		#region CREATION/SETUP ---------------------------------------------------------
		[NonSerialized, HideInInspector] public string DBG_CalculateTriangulation;
		[ContextMenu("z - call CalculateTriangulation()")]
		public void CalculateTriangulation()
		{
			DateTime dt_methodStart = DateTime.Now;
			DBG_CalculateTriangulation = $"{nameof(CalculateTriangulation)}()";

			if ( string.IsNullOrEmpty(LayerMaskName) ) //todo: dws
			{
				Debug.LogError("LogansNavmeshExtender ERROR! You need to set an environmental layer mask in order to construct the navmesh.");
				return;
			}
			else
			{
				cachedLayerMask = LayerMask.GetMask( LayerMaskName );
			}

			CalculateSurfaceNormal();

			// Make lists-------------------------
			List<Vector3> constructedVertices_unique = new List<Vector3>(); //it doesn't look to me like I actually do anything with this...

			_VisualizationMesh = new Mesh();

			#region DEAL WITH TRIANGULATION -----------------------------------------------------------------------------
			NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation(); //This calculates and returns a "simple triangulation of the current navmesh..." - docs

			DBG_CalculateTriangulation += $"fetched scene triangulation has '{triangulation.areas.Length}' areas, '{triangulation.vertices.Length}' " +
				$"vertices, and '{triangulation.indices.Length}' indices.\n";
			Debug.Log($"fetched scene triangulation has '{triangulation.areas.Length}' areas, '{triangulation.vertices.Length}' " +
				$"vertices, and '{triangulation.indices.Length}' indices.\n");

			List<LNX_AtomicTriangle> constructedAtomicTris = new List<LNX_AtomicTriangle>();
			List<int> constructedAreaIndices = new List<int>();
			bool hvMods = HaveModifications();

			DBG_CalculateTriangulation += $"Now looping through fetched triangulation to create triangle collection...\n";
			for ( int i = 0; i < triangulation.areas.Length; i++ )
			{
				DBG_CalculateTriangulation += $"{i} --------------------------////////////////////////////////////\n";
				//Debug.Log($"{i} --------------------------////////////////////////////////////\n");

				if ( ContainsDeletion(triangulation, i) )
				{
					continue;
				}

				LNX_AtomicTriangle tri = new LNX_AtomicTriangle(
					triangulation.vertices[triangulation.indices[i * 3]],
					triangulation.vertices[triangulation.indices[(i * 3) + 1]],
					triangulation.vertices[triangulation.indices[(i * 3) + 2]]
				);

				constructedAreaIndices.Add( i );

				if( hvMods )
				{
					for ( int j = 0; j < Triangles.Length; j++ )
					{
						if ( Triangles[j].HasBeenModifiedAfterCreation && Triangles[j].OriginallyPositionallyMatches(tri) )
						{
							DBG_CalculateTriangulation += $"new tri '{i}' originally matches old tri '{j}'\n";
							//tri.AdoptModifiedValues(Triangles[i]); //I don't think this will work bc I don't think you can change structs...
							tri = new LNX_AtomicTriangle( Triangles[j] );
						}
					}
				}

				constructedAtomicTris.Add(tri);
			}
			#endregion

			DBG_CalculateTriangulation += $"Finished constructing '{constructedAtomicTris.Count}' atomic tris. Constructing real list...\n";
			Debug.Log($"Finished constructing '{constructedAtomicTris.Count}' atomic tris. Constructing real list...\n");
			Triangles = new LNX_Triangle[constructedAtomicTris.Count];
			for( int i = 0; i < constructedAtomicTris.Count; i++ )
			{
				DBG_CalculateTriangulation += $"{i} --------------------------////////////////////////////////////\n";
				//Debug.Log($"{i} --------------------------////////////////////////////////////\n");

				Triangles[i] = new LNX_Triangle( i, constructedAreaIndices[i], constructedAtomicTris, this ); //stack trace 5
			}

			Debug.Log($"Finished making list. method time: '{DateTime.Now.Subtract(dt_methodStart)}'");
			DBG_CalculateTriangulation += $"Finished making list. method time: '{DateTime.Now.Subtract(dt_methodStart)}'";

			RefreshMe( true );

			DBG_CalculateTriangulation += $"End of {nameof(CalculateTriangulation)}(). Created '{Triangles.Length}' triangles, " +
				$"and '{constructedVertices_unique.Count}' unique vertices for the mesh.\n";

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

			//deletedTriangles = new List<LNX_Triangle>(); todo: dws
			deletedTriangles = new List<LNX_AtomicTriangle>();

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

			List<LNX_Triangle> newTriangles = new List<LNX_Triangle>();
			int runningTriIndx = 0;

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				bool triShouldBeDeleted = false;

				for ( int j = 0; j < trisToDelete.Length; j++ )
				{
					if ( Triangles[i].ValueEquals(trisToDelete[j]) )
					{
						triShouldBeDeleted = true;
						//deletedTriangles.Add(Triangles[i]); //todo: dws
						deletedTriangles.Add( new LNX_AtomicTriangle(Triangles[i]) );
						break;
					}
				}

				if( !triShouldBeDeleted ) //...then we need to add it's stuff to the new collection...
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
				}
			}

			return false;
		}

		public bool ContainsDeletion( LNX_AtomicTriangle triTemplate ) //todo: definitely unit test this...
		{
			string methodReport = $"ContainsDeletion()";

			if ( deletedTriangles != null && deletedTriangles.Count > 0 )
			{
				for ( int i = 0; i < deletedTriangles.Count; i++ )
				{
					if ( deletedTriangles[i].OriginalPositionallyMatches(triTemplate) )
					{
						methodReport += $"found DO contain deletion'...";
						Debug.LogWarning(methodReport);
						return true;
					}
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

			CalculateBounds(); //This needs to happen now before the triangles refresh because the creation of the vert relationships relies on CalculatePath(), which relies on knowing the bounds in order to short-circuit

			int nmbrFnshdLoops = 0;

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				//DateTime dt_loopStart = DateTime.Now;
				//Debug.Log($"I: '{i}'...");
				Triangles[i].RefreshMe( this, meshContinuityHasChanged );
				//Debug.Log($"tri loop time: '{DateTime.Now.Subtract(dt_loopStart)}', tri refsh total: '{Triangles[i].TotalRefreshTime}' " +
					//$"ts_crntLoop: '{ts_total.ToString()}', ms: '{ts_total.TotalMilliseconds}'");
				//nmbrFnshdLoops++;
				/*
				totalTriTime += Triangles[i].TotalRefreshTime;
				totalVertsTime += (Triangles[i].TotalCreateRelationships_vert0Time +
					Triangles[i].TotalCreateRelationships_vert1Time + 
					Triangles[i].TotalCreateRelationships_vert2Time);
				*/

				if ( DateTime.Now.Subtract(dt_start).TotalSeconds > 40f )
				{
					Debug.LogError($"timespan went beyond limit. breaking early...");
					//Debug.Log( $"number of finished loops: '{nmbrFnshdLoops}'\n" );

					return;
				}
			}

			Debug.Log($"Refresh loop finished after '{DateTime.Now.Subtract(dt_start).TotalSeconds}' seconds. Now calculating bounds...");


			//dt_start = DateTime.Now;
			if( !Application.isPlaying && meshContinuityHasChanged )
			{
				ReconstructVisualizationMesh();
			}

			//TimeSpan ts = DateTime.Now.Subtract(dt_start);
			//Debug.Log($"{nameof(ReconstructVisualizationMesh)}() finished after timespan of '{ts}', ms: '{ts.Milliseconds}'...");
		}

		public void CalculateBounds()
		{
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

			V_BoundsCenter = 
			(
				V_Bounds[0] + V_Bounds[1] + V_Bounds[2] + V_Bounds[3] +
				V_Bounds[4] + V_Bounds[5] + V_Bounds[6] + V_Bounds[7]
			) / 8f;
		}

		/// <summary>
		/// Calculates and caches certain information that can make this navmesh's operations run much faster. Note: This method can be a very
		/// expensive call, and though it can drastically speed up operations like path finding, it isn't necessary for operation of the 
		/// LNX_NavMesh. Find an appropriate spot in your code to do it once, not continuously. If for some reason it's necessary to calculate 
		/// this after game load/start, you can optionally call this method in a thread so it doesn't hang up your game.
		/// </summary>
		public void CacheMeshEfficiencyInformation()
		{
			#region CALCULATE TRI KNOWN-VISIBILITY ----------------------------------
			LNX_Edge[] trmnlEdges = GetTerminalEdges(false);
			for (int i = 0; i < Triangles.Length; i++)
			{
				Triangles[i].CalculateCompletelyVisibleTris(this, trmnlEdges);
			}
			#endregion


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
			#endregion

			if (dbgMethod) Debug.Log($"end of ReconstructVisualizationMesh()");
		}
		#endregion

		#region MAIN API METHODS----------------------------------------------------------------
		/*[SerializeField]*/
		[NonSerialized] public string dbgCalculatePath;

		/// <summary>
		/// Calculates and caches a Vector3 variable on this objectrepresenting the surface normal dictated by the SurfaceOrientation 
		/// setting.
		/// </summary>
		/// <returns></returns>
		public void CalculateSurfaceNormal()
		{
			if (SurfaceOrientation == LNX_Direction.PositiveY)
			{
				v_surfaceOrientation_cached = Vector3.up;
			}
			if ( SurfaceOrientation == LNX_Direction.NegativeY )
			{
				v_surfaceOrientation_cached = Vector3.down;
			}
			else if ( SurfaceOrientation == LNX_Direction.PositiveX )
			{
				v_surfaceOrientation_cached = Vector3.right;
			}
			else if ( SurfaceOrientation == LNX_Direction.NegativeX )
			{
				v_surfaceOrientation_cached = Vector3.left;
			}
			else if ( SurfaceOrientation == LNX_Direction.PositiveZ )
			{
				v_surfaceOrientation_cached = Vector3.forward;
			}
			else if ( SurfaceOrientation == LNX_Direction.NegativeZ )
			{
				v_surfaceOrientation_cached = Vector3.back;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="startPos_passed"></param>
		/// <param name="endPos_passed"></param>
		/// <param name="maxSampleDistance"></param>
		/// <param name="outPath"></param>
		/// <param name="considerOffPerimeter"></param>
		/// <returns></returns>
		public bool CalculatePath( Vector3 startPos_passed, Vector3 endPos_passed, float maxSampleDistance, out LNX_Path outPath, bool considerOffPerimeter = true)
		{
			#region CALCULATE START AND END POINT -------------------------------------------
			LNX_ProjectionHit startHit = new LNX_ProjectionHit();
			LNX_ProjectionHit endHit = new LNX_ProjectionHit();
			outPath = LNX_Path.None;

			dbgCalculatePath = $"{nameof(CalculatePath)}(strt: '{startPos_passed}', end: '{endPos_passed}', smplDst: '{maxSampleDistance}' " +
				$"at {DateTime.Now.ToString()})\n";

				if (SamplePosition(startPos_passed, out startHit, maxSampleDistance, considerOffPerimeter))
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
			

			#endregion

			#region CONSTRUCT PATH -------------------------------------
			dbgCalculatePath += $"now trying to construct path...\n";

			if ( !Raycast(startPos_passed, endPos_passed, maxSampleDistance, out outPath, considerOffPerimeter) )
			{

				return true;
			}
			else
			{
				outPath.AddPoint( endHit, this ); //doing this for now so that it definitely has the final point, which it needs.

				if ( Triangles[startHit.Index_Hit].HasIndexInKnownVisibleList(endHit.Index_Hit) )
				{
					//Maybe the following logic can be short-circuited here
					//on second thought, this won't work bc this else statement wouldn't execute if the 
					//above raycast was true, but there still should be a way to short-circuit given 
					//known visible tris...
				}
				else
				{
					///////////////////////////////////// IDEA PSUEDO-CODE
					/*
					Step 1 - We know that the first (and last) point we need to go to is going to be a 'terminal' vertex, so 
					assemble a list of terminal verts that are visible from the start position.

					Step 2 - For all of these verts, we need to do a sort of 'pinging' operation where we catalogue the path 
					distance from each of them to the destination. For the ones that can already see the destination (IE: that 
					can raycast to it), we can get that distance right away. For the rest, they would each need to do a sort of 
					'pinging' operation until they finally were able to hit the end position

				 
					*/


					// END -------------
				}
			}
			#endregion

			return true;
		}

		[NonSerialized] public string DBG_SamplePosition;

		/// <summary>
		/// Gets a point on the projection of the navmesh using the supplied position. If the supplied position is not on the 
		/// projection of the navmesh, it calculates the closest point on the surface of the navmesh.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="hit"></param>
		/// <param name="maxDistance"></param>
		/// <returns></returns>
		public bool SamplePosition( Vector3 pos, out LNX_ProjectionHit hit, float maxDistance, 
			bool considerOffPerimeter = true, bool considerPossibilityOfOverlaps = true 
		)
        {
			//DBG_SamplePosition = $"SamplePosition('{pos}'). Searching through '{Triangles.Length}' tris...\n";
			hit = LNX_ProjectionHit.None;

			if( Vector3.Distance(V_BoundsCenter, pos) > (maxDistance + BoundsContainmentDistanceThreshold) )
			{
				//DBG_SamplePosition += $"distance threshold short circuit";
				return false;
			}

            float runningClosestDist = float.MaxValue;
			int runningBestIndex = -1;
			Vector3 runningBestPt = Vector3.zero;

            for ( int i = 0; i < Triangles.Length; i++ )
            {
				//DBG_SamplePosition += $"i: '{i}'....................\n";
				Vector3 currentPt = Vector3.zero;
				float currentDist = float.MaxValue;

				if ( Triangles[i].IsInShapeProject(pos, out currentPt) )
				{
					if( !considerPossibilityOfOverlaps )
					{
						hit = new LNX_ProjectionHit( i, currentPt );

						return true;
					}

					//DBG_SamplePosition += $"found AM in shape project at '{currentPt}'...\n";
					//note: The reason I'm not immediately returning this tri here is because concievably
					// you could have two navmesh polys "on top of each other", (IE: in line with
					// each other's normals), which would result in more than one tri considering
					// this point to be within it's bounds, and you need to decide which one is
					// the better option...
					currentDist = Vector3.Distance(pos, currentPt);
				}
                else
                {
					//DBG_SamplePosition += $"found am NOT in shape project...\n";

					if ( considerOffPerimeter )
					{
						currentPt = Triangles[i].ClosestPointOnPerimeter( pos );
						currentDist = Vector3.Distance(pos, currentPt);
					}
				}

				//DBG_SamplePosition += $"dist: '{currentDist}'\n...";

				if ( currentDist < runningClosestDist )
				{
					//DBG_SamplePosition += $"new closest point at: '{currentDist}'...\n";
					runningBestPt = currentPt;
					runningClosestDist = currentDist;
					runningBestIndex = i;
				}
            }

			hit = new LNX_ProjectionHit(runningBestIndex, runningBestPt, pos);

			//DBG_SamplePosition += $"finished. returning: '{hit.Index_Hit}' with pt: '{hit.HitPosition}'\n";

            if( runningClosestDist <= maxDistance )
			{
				return true;
			}
			else
			{
				return false;
			}
        }

		[NonSerialized] public string DBG_NavmeshProjection;
		/// <summary>
		/// Returns true if the supplied position is within the projection of any triangle on the navmesh, 
		/// projected along the navmesh's surface orientation.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="projectedPoint">Closest point to the supplied position on the surface of the Navmesh</param>
		/// <returns></returns>
		public bool AmWithinSurfaceProjection(Vector3 pos, out LNX_ProjectionHit hit) //todo: unit test this method
		{
			return SamplePosition( pos, out hit, 100f, false, false );
		}

		[NonSerialized] public string DBGRaycast;

		/// <summary>
		/// Traces a line between two points on a navmesh.
		/// </summary>
		/// <returns>True if the ray is terminated before reaching target position. Otherwise returns false.</returns>
		public bool Raycast( Vector3 sourcePosition, Vector3 targetPosition, float maxSampleDistance, 
			bool onlySampleWithinSurfaceProject = true ) //todo: Unit test!!!
		{
			//DBGRaycast = "";

			LNX_ProjectionHit lnxStartHit = LNX_ProjectionHit.None;
			LNX_ProjectionHit lnxEndHit = LNX_ProjectionHit.None;

			#region GET START AND END POINTS------------------------------------------
			if( onlySampleWithinSurfaceProject )
			{
				if ( !AmWithinSurfaceProjection(sourcePosition, out lnxStartHit) && lnxStartHit.DistanceAway <= maxSampleDistance )
				{
					//DBGRaycast += $"{nameof(AmWithinSurfaceProjection)}() for sourcePosition was NOT succesful\n";
					return true;
				}

				if ( !AmWithinSurfaceProjection(targetPosition, out lnxEndHit) && lnxEndHit.DistanceAway <= maxSampleDistance )
				{
					//DBGRaycast += $"{nameof(AmWithinSurfaceProjection)}() for endPosition was NOT succesful\n;
					return true;
				}
			}
			else
			{
				if ( !SamplePosition(sourcePosition, out lnxStartHit, maxSampleDistance) )
				{
					//DBGRaycast += $"tried samplePosition did NOT work. Returning early...\n";
					return true;
				}

				if ( !SamplePosition(targetPosition, out lnxEndHit, maxSampleDistance) )
				{
					//DBGRaycast += $"tried samplePosition did NOT work. Returning early...\n";
					return true;
				}
			}
			#endregion

			if ( Triangles[lnxStartHit.Index_Hit].HasIndexInKnownVisibleList(lnxEndHit.Index_Hit) ) //Short-circuit: If start and end hit are on same triangle...
			{
				//DBGRaycast += $"Short-circuiting. Start and end hits are on the same triangle surface...";
				return false;
			}

			#region PROJECT THROUGH TO TARGET POSITION -------------------------------------------------
			LNX_Triangle currentTri = Triangles[lnxStartHit.Index_Hit];
			Vector3 currentStartPos = lnxStartHit.HitPosition;
			int currentEdgeIndex = -1;

			int safetyTimeout = Triangles.Count();
			int runningWhileIterations = 0;

			//DBGRaycast += "\nlooping through mesh triangles...\n\n";
			bool amStillProjecting = true;
			while ( amStillProjecting )
			{
				/*DBGRaycast += $"\nwhile{runningWhileIterations}=============================================== " +
					$"\n(currentTri: '{currentTri.Index_inCollection}', startPt: '{LNX_UnitTestUtilities.LongVectorString(currentStartPos)}')\n" +
					$"projecting through triangle...\n";*/

				LNX_ProjectionHit perimHit = currentTri.ProjectThroughToPerimeter( currentStartPos, lnxEndHit.HitPosition, currentEdgeIndex );
				/*DBGRaycast += $"tri.prjctThrToPerim report----------------\n" +
					$"{currentTri.dbg_prjctThrhToPerim}" +
					$"end rprt------------\n";*/

				if ( perimHit.Index_Hit < 0 || perimHit.Index_Hit > 2 )
				{
					//DBGRaycast += $"after project, hit object has bad index of '{perimHit.Index_Hit}'. now returning...";
					return true;
				}

				LNX_Edge hitEdge = currentTri.Edges[perimHit.Index_Hit];
				currentEdgeIndex = perimHit.Index_Hit;
				currentStartPos = perimHit.HitPosition;

				//DBGRaycast += $"projected to edge: '{hitEdge.MyCoordinate}' at '{LNX_UnitTestUtilities.LongVectorString(perimHit.HitPosition)}'. " +
					//$"Shared edge is: '{hitEdge.SharedEdgeCoordinate}'\n";

				if ( hitEdge.AmTerminal )
				{
					//DBGRaycast += $"hit edge is terminal. Stoping loop...\n";
					amStillProjecting = false;
				}
				else
				{
					/*DBGRaycast += $"edge is NOT terminal. Checking to see if we're at the end...\n";
					DBGRaycast += $"test1, Triangles[{currentTri.Index_inCollection}].AmAdjacentToTri({lnxEndHit.Index_Hit}): " +
						$"'{currentTri.AmAdjacentToTri(lnxEndHit.Index_Hit)}'\n" +
						$"";*/

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
						//DBGRaycast += $"Decided AM at the end. Stopping...\n";
					}
					else
					{
						currentTri = Triangles[hitEdge.SharedEdgeCoordinate.TrianglesIndex];
						currentEdgeIndex = hitEdge.SharedEdgeCoordinate.ComponentIndex;
						currentStartPos = perimHit.HitPosition;

						//DBGRaycast += $"Decided NOT at the end. Set new current tri to: '{currentTri.Index_inCollection}'...\n";
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

			//DBGRaycast += $"finally returning: '{currentTri.Index_inCollection != lnxEndHit.Index_Hit}'\n";

			return currentTri.Index_inCollection != lnxEndHit.Index_Hit;
		}

		/// <summary>
		/// Traces a line between two points on a navmesh.
		/// </summary>
		/// <returns>True if the ray is terminated before reaching target position. Otherwise returns false.</returns>
		public bool Raycast(Vector3 sourcePosition, Vector3 targetPosition, float maxSampleDistance, out LNX_Path outPath,
			bool onlySampleWithinSurfaceProject = true) //todo: Unit test!!!
		{
			//DBGRaycast = "";
			outPath = new LNX_Path();

			#region GET START AND END POINTS------------------------------------------
			LNX_ProjectionHit lnxStartHit = LNX_ProjectionHit.None;
			LNX_ProjectionHit lnxEndHit = LNX_ProjectionHit.None;

			if (AmWithinSurfaceProjection(sourcePosition, out lnxStartHit) && lnxStartHit.DistanceAway <= maxSampleDistance)
			{
				//DBGRaycast += $"{nameof(AmWithinSurfaceProjection)}() for sourcePosition was succesful\n" +
					//$"start projection: '{lnxStartHit}'\n";
			}
			else
			{
				/*DBGRaycast += $"SourcePosition NOT within navmesh projection...\n\n" +
					$"{nameof(AmWithinSurfaceProjection)} report:\n" +
					$"{DBG_NavmeshProjection}\n";*/

				if (onlySampleWithinSurfaceProject)
				{
					//DBGRaycast += $"not instructed to try samplePosition. Returning early...";
					return true;
				}
				else
				{
					//DBGRaycast += $"trying samplePosition...\n";
					if (!SamplePosition(sourcePosition, out lnxStartHit, maxSampleDistance))
					{
						//DBGRaycast += $"tried samplePosition. Still didn't work. Returning early...\n";
						return true;
					}
				}
			}

			if (AmWithinSurfaceProjection(targetPosition, out lnxEndHit) && lnxEndHit.DistanceAway <= maxSampleDistance)
			{
				//DBGRaycast += $"{nameof(AmWithinSurfaceProjection)}() for endPosition was succesful\n" +
					//$"end projection: '{lnxEndHit}'\n";
			}
			else
			{
				/*DBGRaycast += $"targetPosition NOT within navmesh projection...\n\n" +
					$"{nameof(AmWithinSurfaceProjection)} report:\n" +
					$"{DBG_NavmeshProjection}\n";*/

				if (onlySampleWithinSurfaceProject)
				{
					//DBGRaycast += $"not instructed to try targetPosition. Returning early...";
					return true;
				}
				else
				{
					//DBGRaycast += $"trying samplePosition...\n";
					if (!SamplePosition(targetPosition, out lnxEndHit, maxSampleDistance))
					{
						//DBGRaycast += $"tried samplePosition. Still didn't work. Returning early...\n";
						return true;
					}
				}
			}
			#endregion

			outPath.AddPoint( lnxStartHit, this );

			if (lnxStartHit.Index_Hit == lnxEndHit.Index_Hit) //Short-circuit: If start and end hit are on same triangle...
			{
				//DBGRaycast += $"Short-circuiting. Start and end hits are on the same triangle surface...";
				outPath.AddPoint( lnxEndHit, this );
				return false;
			}

			#region PROJECT THROUGH TO TARGET POSITION -------------------------------------------------
			LNX_Triangle currentTri = Triangles[lnxStartHit.Index_Hit];
			Vector3 currentStartPos = lnxStartHit.HitPosition;
			int currentEdgeIndex = -1;

			int safetyTimeout = Triangles.Count();
			int runningWhileIterations = 0;

			//DBGRaycast += "\nlooping through mesh triangles...\n\n";
			bool amStillProjecting = true;
			while (amStillProjecting)
			{
				/*DBGRaycast += $"\nwhile{runningWhileIterations}=============================================== " +
					$"\n(currentTri: '{currentTri.Index_inCollection}', startPt: '{LNX_UnitTestUtilities.LongVectorString(currentStartPos)}')\n" +
					$"projecting through triangle...\n";*/

				LNX_ProjectionHit edgePerimHit = currentTri.ProjectThroughToPerimeter(currentStartPos, lnxEndHit.HitPosition, currentEdgeIndex);
				outPath.AddPoint( new LNX_ProjectionHit(currentTri.Index_inCollection, edgePerimHit.HitPosition), this );

				/*DBGRaycast += $"tri.prjctThrToPerim report----------------\n" +
					$"{currentTri.dbg_prjctThrhToPerim}" +
					$"end rprt------------\n";*/

				if (edgePerimHit.Index_Hit < 0 || edgePerimHit.Index_Hit > 2)
				{
					//DBGRaycast += $"after project, hit object has bad index of '{edgePerimHit.Index_Hit}'. now returning...";
					return true;
				}

				LNX_Edge hitEdge = currentTri.Edges[edgePerimHit.Index_Hit];
				currentEdgeIndex = edgePerimHit.Index_Hit;
				currentStartPos = edgePerimHit.HitPosition;

				//DBGRaycast += $"projected to edge: '{hitEdge.MyCoordinate}' at '{LNX_UnitTestUtilities.LongVectorString(edgePerimHit.HitPosition)}'. " +
					//$"Shared edge is: '{hitEdge.SharedEdgeCoordinate}'\n";

				if (hitEdge.AmTerminal)
				{
					//DBGRaycast += $"hit edge is terminal. Stoping loop...\n";
					amStillProjecting = false;
				}
				else
				{
					/*DBGRaycast += $"edge is NOT terminal. Checking to see if we're at the end...\n";
					DBGRaycast += $"test1, Triangles[{currentTri.Index_inCollection}].AmAdjacentToTri({lnxEndHit.Index_Hit}): " +
						$"'{currentTri.AmAdjacentToTri(lnxEndHit.Index_Hit)}'\n" +
						$"";*/

					if (
						hitEdge.SharedEdgeCoordinate.TrianglesIndex == lnxEndHit.Index_Hit ||
						(
							currentTri.AmAdjacentToTri(lnxEndHit.Index_Hit) &&
							currentTri.IsPositionOnAnyEdge(lnxEndHit.HitPosition)
						)
					)
					{
						currentTri = Triangles[lnxEndHit.Index_Hit];
						outPath.AddPoint( lnxEndHit, this );
						amStillProjecting = false;
						//DBGRaycast += $"Decided AM at the end. Stopping...\n";
					}
					else
					{
						currentTri = Triangles[hitEdge.SharedEdgeCoordinate.TrianglesIndex];
						currentEdgeIndex = hitEdge.SharedEdgeCoordinate.ComponentIndex;
						currentStartPos = edgePerimHit.HitPosition;

						//DBGRaycast += $"Decided NOT at the end. Set new current tri to: '{currentTri.Index_inCollection}'...\n";
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

			//DBGRaycast += $"finally returning: '{currentTri.Index_inCollection != lnxEndHit.Index_Hit}'\n";

			return currentTri.Index_inCollection != lnxEndHit.Index_Hit;

		}

		/// <summary>
		/// Whether a supplied position lies between the path of the two supplied edges projected to each other.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="edgeA"></param>
		/// <param name="edgeB"></param>
		/// <returns>True if the supplied position lies in the path that two edges make toward each other</returns>
		public bool IsBetweenEdgePath( Vector3 pos, LNX_ComponentCoordinate edgecoordA, LNX_ComponentCoordinate edgecoordB )
		{
			LNX_Edge edgeA = Triangles[edgecoordA.TrianglesIndex].Edges[edgecoordA.ComponentIndex];
			LNX_Edge edgeB = Triangles[edgecoordB.TrianglesIndex].Edges[edgecoordB.ComponentIndex];

			Vector3 v_startToStart = Vector3.Normalize(LNX_Utils.FlatVector(edgeB.StartPosition, v_surfaceOrientation_cached) - LNX_Utils.FlatVector(edgeA.StartPosition, v_surfaceOrientation_cached));
			Vector3 v_startToEnd = Vector3.Normalize(LNX_Utils.FlatVector(edgeB.EndPosition, v_surfaceOrientation_cached) - LNX_Utils.FlatVector(edgeA.StartPosition, v_surfaceOrientation_cached));
			Vector3 v_endToStart = Vector3.Normalize(LNX_Utils.FlatVector(edgeB.StartPosition, v_surfaceOrientation_cached) - LNX_Utils.FlatVector(edgeA.EndPosition, v_surfaceOrientation_cached));
			Vector3 v_endToEnd = Vector3.Normalize(LNX_Utils.FlatVector(edgeB.EndPosition, v_surfaceOrientation_cached) - LNX_Utils.FlatVector(edgeA.EndPosition, v_surfaceOrientation_cached));

			float alignmentA = Vector3.Dot(v_startToStart, v_endToEnd);
			float alignmentB = Vector3.Dot(v_startToEnd, v_endToStart);
			Vector3 v_startToPos = Vector3.Normalize( LNX_Utils.FlatVector(pos, v_surfaceOrientation_cached) - LNX_Utils.FlatVector(edgeA.StartPosition, v_surfaceOrientation_cached) );
			Vector3 v_endToPos = Vector3.Normalize( LNX_Utils.FlatVector(pos, v_surfaceOrientation_cached) - LNX_Utils.FlatVector(edgeA.EndPosition, v_surfaceOrientation_cached) );

			string s = "";
			//The following if-check determines which vector-set is in better alignment. This set will be the vector-set 
			//that will be on the "outside", and therefore the correct one to use...
			if (alignmentA > alignmentB) //this means the start and end positions of the edges "line up", so to speak.
			{
				if
				(
					LNX_Utils.AmBetweenConcurrentLines
					( 
						pos, edgeA.StartPosition, edgeB.StartPosition, edgeA.EndPosition, edgeB.EndPosition, v_surfaceOrientation_cached, ref s
					) &&
					LNX_Utils.AmBetweenConcurrentLines
					(
						pos, edgeA.StartPosition, edgeA.EndPosition, edgeB.StartPosition, edgeB.EndPosition, v_surfaceOrientation_cached, ref s
					)
				)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				if
				(
					LNX_Utils.AmBetweenConcurrentLines
					(
						pos, edgeA.StartPosition, edgeB.EndPosition, edgeA.EndPosition, edgeB.StartPosition, v_surfaceOrientation_cached, ref s
					) &&
					LNX_Utils.AmBetweenConcurrentLines
					(
						pos, edgeA.StartPosition, edgeA.EndPosition, edgeB.EndPosition, edgeB.StartPosition, v_surfaceOrientation_cached, ref s
					)
				)
				{
					return true;
				}
				else
				{
					return false;
				}
				
			}

			return false;
		}

		#endregion // (END) MAIN API METHODS---------------------

		public bool HaveKink()
		{
			if ( Triangles == null || Triangles.Length <= 0 )
			{
				return false;
			}

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				if(Triangles[i].AmKinked )
				{
					return true;
				}
			}

			return false;
		}

		#region HELPERS --------------------------------------------------
		[ContextMenu("z call SayCurrentInfo()")]
		public void SayCurrentInfo()
		{
			Debug.Log($"" +
				$"{nameof(SurfaceOrientation)}: '{SurfaceOrientation}'\n" +
				$"Bounds-----\n" +
				$"{nameof(Bounds_LowestX)}: '{Bounds_LowestX}, {nameof(Bounds_HighestX)}: '{Bounds_HighestX}'\n" +
				$"{nameof(Bounds_LowestY)}: '{Bounds_LowestY}, {nameof(Bounds_HighestY)}: '{Bounds_HighestY}'\n" +
				$"{nameof(Bounds_LowestZ)}: '{Bounds_LowestZ}, {nameof(Bounds_HighestZ)}: '{Bounds_HighestZ}'\n" +
				
				"");

			for (int i = 0; i < Triangles.Length; i++)
			{
				Triangles[i].SayCurrentInfo(this);
			}
		}

		[ContextMenu("z call ReportAbnormalities")]
		public void ReportAbnormalities()
		{
			StringBuilder sb_anomolies = new StringBuilder();
			bool anomolyFound = false;

			if( Bounds == null )
			{
				sb_anomolies.AppendLine($"Bounds collection currently null...");
				anomolyFound = true;
			}
			if ( Bounds.Length <= 0 )
			{
				sb_anomolies.AppendLine($"Bounds collection length less than or equal to 0...");
				anomolyFound = true;
			}

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				sb_anomolies.AppendLine( $"Triangle[{i}]---" );

				string s = Triangles[i].GetAnomolyString( this );

				if ( !string.IsNullOrWhiteSpace(s) )
				{
					anomolyFound = true;
					sb_anomolies.AppendLine( s );
				}
			}

			if ( anomolyFound )
			{
				Debug.LogWarning($"Anomoly found!");
			}
			else
			{
				Debug.Log("no anomolies found");
			}

			Debug.Log(sb_anomolies);
		}

		[ContextMenu("z call SayRelational()")]
		public void SayRelational()
		{

			for (int i = 0; i < Triangles.Length; i++)
			{
				Debug.Log( Triangles[i].GetRelationalString() );
			}
		}

		#endregion
	}
}