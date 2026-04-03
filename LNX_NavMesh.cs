
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;


namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_NavMesh : MonoBehaviour
	{
		//[Header("OPTIONS")]
		public LNX_Direction SurfaceOrientation = LNX_Direction.PositiveY;

		public LayerMask MyLayerMask;

		/*
		public string LayerMaskName;

		private int cachedLayerMask;
		public int CachedLayerMask => cachedLayerMask;
		*/

		/*[HideInInspector]*/ public LNX_Triangle[] Triangles;

		[HideInInspector] public Vector3[] Vertices;

		//[SerializeField] private List<LNX_Triangle> deletedTriangles;
		[SerializeField, HideInInspector] private List<LNX_AtomicTriangle> deletedTriangles;

		[HideInInspector] public Mesh _VisualizationMesh;

		//[Header("BOUNDS")]
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

		#region DATA ======================================================
		/// <summary>
		/// String that caches all string segments involved in data serialization in comma-separated format.<para/>
		/// [0] = GUID,<br/>
		/// [1] = File Name<br/>
		/// </summary>
		[SerializeField, HideInInspector] private string serializedDataString; //right now, I'm just using this to store the guid, but will eventually use it to also store the resource path

		public string cachedGUID => serializedDataString; /*string.IsNullOrEmpty(serializedDataString) ? "" : serializedDataString.Split(',')[0];*/


		#endregion

		#region EFFICIENCY ================================================
		LNX_ComponentCoordinate[] boundsVerts;
		public LNX_ComponentCoordinate[] BoundsVerts => boundsVerts;
		LNX_ComponentCoordinate[] boundsEdges;
		public LNX_ComponentCoordinate[] BoundsEdges => boundsEdges;

		#endregion


		[Header("VISUAL/DEBUG")]
		[SerializeField, Tooltip("Whether to draw the mesh visual")] private bool drawVisualizationMesh;
		[SerializeField] private Color color_visualMesh;

		private void OnEnable()
		{
			Debug.Log("lnx_navmesh.onenable()");


		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		void CheckForProblem()
		{
			Debug.Log("First scene loading: Before Awake is called.");

			for( int i = 0; i < Triangles.Count(); i++ )
			{
				for (int j = 0; j < 3; j++)
				{
					if( Triangles[i].Verts[j].Relationships == null )
					{
						//Debug.Log()
					}
				}
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		static void OnAfterSceneLoad()
		{
			Debug.Log("First scene loaded: After Awake is called.");
		}

		[ContextMenu("z call TrySomething()")]
		public void TrySomething()
		{
			//Curious about scene path...
			/*
			string scnPthString = SceneManager.GetActiveScene().path;
			Debug.Log($"scene path string from scenemanager: '{scnPthString}'..."); //Packages/com.loganland.logansnavigationextension/Testing/LNXTestingScene.unity
			string[] lines = scnPthString.Split("/");
			Debug.Log($"split lines via character to '{lines.Length}' entries...");
			string dirPthScnFldrString = "";
			for (int i = 0; i < lines.Length; i++)
			{
				dirPthScnFldrString = Path.Combine(dirPthScnFldrString, lines[i]);
			}

			Debug.Log($"reassembled string: '{dirPthScnFldrString}'");				//Packages\com.loganland.logansnavigationextension\Testing\LNXTestingScene.unity
			*/

			//Debug.Log(serializedDataString);


			#region Finding assets and retrieving guids ========================
			/*
			//string str = "scn_";
			//string str = "heyass"; //7442bb19a8eb4fd43a6274455f635654
			string str = "muhStankAss"; 

			string[] ids = AssetDatabase.FindAssets( str );
			Debug.Log($"string {str}, found '{ids.Length}' entries...");

			if (ids != null && ids.Length > 0 )
			{
				for (int i = 0; i < ids.Length; i++)
				{
					//Debug.Log($"{i}: {ids[i]}"); //this returns strings like ba914a2f030f0df459aaf2bcf4c8c702
					Debug.Log($"{i}: {ids[i]}\n" +
						$"path: '{AssetDatabase.GUIDToAssetPath(ids[i])}'");

					int idInt = -1;

					if( int.TryParse(ids[i], out idInt) )
					{
						Debug.Log($"succesful parse to '{idInt}'");
					}
					else
					{
						Debug.Log("parse was NOT succesful");
					}
				}
			}
			*/
			#endregion

			#region Finding with Resources class ================
			/*
			string str = "muhStankAss";

			TextAsset muhAsset = Resources.Load<TextAsset>(str);
			Debug.Log($"was null: '{muhAsset == null}'");

			if( muhAsset != null )
			{
				Debug.Log(muhAsset.text); //works!

				Debug.Log("now getting guid...");
			}
			*/
			#endregion

			string dirPthString = Path.Combine(LNX_Utils.MakePathFromString(SceneManager.GetActiveScene().path, "/", 1), "Resources"); //this will replace the forward slashes with back-slashes, and stop at the correct element
			Debug.Log(dirPthString);
			File.WriteAllText( Path.Combine(dirPthString, "asdf.json"), JsonUtility.ToJson(this, true));

			/*
			string path_foundViaGUID = AssetDatabase.GUIDToAssetPath(cachedGUID);
			Debug.Log ( $"{path_foundViaGUID}, empty or null: '{string.IsNullOrEmpty(path_foundViaGUID)}'" );
			*/
		}

		public Vector3 GetSurfaceNormalVector()
		{
			if (SurfaceOrientation == LNX_Direction.PositiveY)
			{
				return Vector3.up;
			}
			if (SurfaceOrientation == LNX_Direction.NegativeY)
			{
				return Vector3.down;
			}
			else if (SurfaceOrientation == LNX_Direction.PositiveX)
			{
				return Vector3.right;
			}
			else if (SurfaceOrientation == LNX_Direction.NegativeX)
			{
				return Vector3.left;
			}
			else if (SurfaceOrientation == LNX_Direction.PositiveZ)
			{
				return Vector3.forward;
			}
			else if (SurfaceOrientation == LNX_Direction.NegativeZ)
			{
				return Vector3.back;
			}

			return Vector3.zero;
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

		public List<LNX_Vertex> GetVerticesAtCoordinate( LNX_Vertex vert )
		{
			List<LNX_Vertex> returnList = new List<LNX_Vertex>();

			if 
			(
				Triangles == null || Triangles.Length <= 0 || vert.MyCoordinate.TrianglesIndex > Triangles.Length - 1 ||
				vert.MyCoordinate.ComponentIndex > 2 || vert.MyCoordinate.ComponentIndex < 0
			)
			{
				return null;
			}
			else
			{
				returnList.Add(vert);

				for (int i = 0; i < vert.SharedVertexCoordinates.Length; i++)
				{
					returnList.Add(Triangles[vert.SharedVertexCoordinates[i].TrianglesIndex].Verts[vert.SharedVertexCoordinates[i].TrianglesIndex]);
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

		#region COMPONENT FETCHERS ================================================
		public List<LNX_ComponentCoordinate> GetCoordinatesAtVertex( LNX_Vertex vert )
		{
			if
			(
				Triangles == null || Triangles.Length <= 0 || vert.MyCoordinate.TrianglesIndex > Triangles.Length - 1 ||
				vert.MyCoordinate.ComponentIndex > 2 || vert.MyCoordinate.ComponentIndex < 0
			)
			{
				return null;
			}

			List<LNX_ComponentCoordinate> returnList = new List<LNX_ComponentCoordinate>() { vert.MyCoordinate };
			returnList.AddRange( vert.SharedVertexCoordinates );

			return returnList;
		}
		#endregion

		#region CREATION/SETUP ---------------------------------------------------------
		[NonSerialized, HideInInspector] public string DBG_CalculateTriangulation;
		[ContextMenu("z - call CalculateTriangulation()")]
		public void CalculateTriangulation()
		{
			DateTime dt_methodStart = DateTime.Now;
			DBG_CalculateTriangulation = $"{nameof(CalculateTriangulation)}()";

			Debug.Log($"{nameof(MyLayerMask)}: '{MyLayerMask.value}'");
			if( MyLayerMask.value == 0 )
			{
				Debug.LogError($"LNX ERROR! You must specify an environmental mask.");
				return;
			}

			// Make lists-------------------------
			List<Vector3> constructedVertices_unique = new List<Vector3>(); //it doesn't look to me like I actually do anything with this...

			_VisualizationMesh = new Mesh();

			#region DEAL WITH TRIANGULATION -----------------------------------------------------------------------------
			NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation(); //Docs: This calculates and returns a "simple triangulation of the current navmesh..."

			DBG_CalculateTriangulation += $"fetched scene triangulation has '{triangulation.areas.Length}' areas, '{triangulation.vertices.Length}' " +
				$"vertices, and '{triangulation.indices.Length}' indices.\n";
			Debug.Log($"fetched scene triangulation has '{triangulation.areas.Length}' areas, '{triangulation.vertices.Length}' " +
				$"vertices, and '{triangulation.indices.Length}' indices.\n");

			List<LNX_AtomicTriangle> constructedAtomicTris = new List<LNX_AtomicTriangle>();
			List<int> constructedAreaIndices = new List<int>();
			bool hvMods = HaveModifications();

			DBG_CalculateTriangulation += $"Now looping through fetched triangulation to create triangle collection...\n";
			for (int i = 0; i < triangulation.areas.Length; i++)
			{
				DBG_CalculateTriangulation += $"{i} --------------------------////////////////////////////////////\n";
				//Debug.Log($"{i} --------------------------////////////////////////////////////\n");

				if (ContainsDeletion(triangulation, i))
				{
					continue;
				}

				LNX_AtomicTriangle tri = new LNX_AtomicTriangle(
					triangulation.vertices[triangulation.indices[i * 3]],
					triangulation.vertices[triangulation.indices[(i * 3) + 1]],
					triangulation.vertices[triangulation.indices[(i * 3) + 2]]
				);

				constructedAreaIndices.Add(i);

				if (hvMods)
				{
					for (int j = 0; j < Triangles.Length; j++)
					{
						if (Triangles[j].HasBeenModifiedAfterCreation && Triangles[j].OriginallyPositionallyMatches(tri))
						{
							DBG_CalculateTriangulation += $"new tri '{i}' originally matches old tri '{j}'\n";
							//tri.AdoptModifiedValues(Triangles[i]); //I don't think this will work bc I don't think you can change structs...
							tri = new LNX_AtomicTriangle(Triangles[j]);
						}
					}
				}

				constructedAtomicTris.Add(tri);
			}
			#endregion

			DBG_CalculateTriangulation += $"Finished constructing '{constructedAtomicTris.Count}' atomic tris. Now constructing real list...\n";
			Debug.Log($"Finished constructing '{constructedAtomicTris.Count}' atomic tris. Now constructing real list...\n");
			Triangles = new LNX_Triangle[constructedAtomicTris.Count];
			for (int i = 0; i < constructedAtomicTris.Count; i++)
			{
				DBG_CalculateTriangulation += $"{i} --------------------------////////////////////////////////////\n";
				Debug.Log($"{i} --------------------------////////////////////////////////////\n");

				Triangles[i] = new LNX_Triangle(i, constructedAreaIndices[i], constructedAtomicTris, this); //stack trace 5
			}

			Debug.Log($"Finished making list. method time: '{DateTime.Now.Subtract(dt_methodStart)}'");
			DBG_CalculateTriangulation += $"Finished making list. method time: '{DateTime.Now.Subtract(dt_methodStart)}'";

			Refresh( true );

			DBG_CalculateTriangulation += $"End of {nameof(CalculateTriangulation)}(). Created '{Triangles.Length}' triangles, " +
				$"and '{constructedVertices_unique.Count}' unique vertices for the mesh.\n";

			Debug.Log(DBG_CalculateTriangulation);
			EditorUtility.SetDirty(this);
		}

		private void DeleteAllRelationships()
		{
			for (int i = 0; i < Triangles.Length; i++)
			{
				/*
				Triangles[i].Verts[0].Relationships = new LNX_VertexRelationship[Triangles.Length * 3];
				Triangles[i].Verts[1].Relationships = new LNX_VertexRelationship[Triangles.Length * 3];
				Triangles[i].Verts[2].Relationships = new LNX_VertexRelationship[Triangles.Length * 3];
				*/

				Triangles[i].Verts[0].Relationships = new LNX_VertexRelationship[0];
				Triangles[i].Verts[1].Relationships = new LNX_VertexRelationship[0];
				Triangles[i].Verts[2].Relationships = new LNX_VertexRelationship[0];
			}
		}

		[ContextMenu("z call ForceRefresh()")]
		public void ForceRefresh() //todo: dws
		{
			DeleteAllRelationships();

			Refresh(true);
		}

		public void Refresh( bool meshContinuityHasChanged ) //NEW
		{
			Debug.Log($"{nameof(Refresh)}()---------------------------");

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
#if UNITY_EDITOR
			if( !Application.isPlaying && meshContinuityHasChanged )
			{
				ReconstructVisualizationMesh();
			}
#endif

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
		#endregion -------------------------------------------------------

		#region MODIFICATION-----------------------------------------------------------
		/// <summary>
		/// Checks to see if any madifications exist on this LNX_NavMesh. Warning: Relatively slow operation. 
		/// Not as cheap as checking a boolean flag.
		/// </summary>
		/// <returns></returns>
		public bool HaveModifications() //Todo: remember to unit test this
		{
			string methodReport = $"{nameof(HaveModifications)}()\n";

			if( deletedTriangles != null && deletedTriangles.Count > 0 )
			{
				methodReport += $"Found DO have modifications. {nameof(deletedTriangles)}, count: '{deletedTriangles.Count}'\n";
				Debug.Log( methodReport );
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

		[ContextMenu("z call ClearGeometry()")]
		public void ClearGeometry()
		{
			Debug.Log("ClearGeometry()");

			Triangles = new LNX_Triangle[0];
			deletedTriangles = new List<LNX_AtomicTriangle>();
			_VisualizationMesh = new Mesh();
			Bounds = new float[0];
			V_Bounds = new Vector3[6];
			V_BoundsCenter = Vector3.zero;

			UnityEditor.EditorUtility.SetDirty(this);
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

			Refresh( true );
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

			Refresh( true );
		}
		#endregion

		#region MAIN API METHODS----------------------------------------------------------------
		[NonSerialized] public string DBG_SamplePosition;

		/// <summary>
		/// Gets a point on the projection of the navmesh using the supplied position. If the supplied position is not on the 
		/// projection of the navmesh, it calculates the closest point on the surface of the navmesh.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="hit"></param>
		/// <param name="maxDistance"></param>
		/// <returns></returns>
		public bool SamplePosition( Vector3 pos, out LNX_NavmeshHit hit, float maxDistance, 
			bool considerClosestOffPerimeter = true, bool considerPossibilityOfOverlaps = true 
		)
        {
			//DBG_SamplePosition = $"SamplePosition('{pos}'). Searching through '{Triangles.Length}' tris...\n";
			hit = LNX_NavmeshHit.None;

			#region SHORT-CIRCUITING ===========================================================
			if ( Vector3.Distance(V_BoundsCenter, pos) > (maxDistance + BoundsContainmentDistanceThreshold) )
			{
				//DBG_SamplePosition += $"distance threshold short circuit";
				return false;
			}
			#endregion

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
						hit = new LNX_NavmeshHit(Triangles[i], currentPt, pos );

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

					if ( considerClosestOffPerimeter )
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

			if( runningBestIndex < 0 )
			{
				return false;
			}

			hit = new LNX_NavmeshHit(Triangles[runningBestIndex], runningBestPt, pos);

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
		public bool AmWithinSurfaceProjection(Vector3 pos, out LNX_NavmeshHit hit) //todo: unit test this method
		{
			return SamplePosition( pos, out hit, 100f, false, false );
		}

		/// <summary>
		/// Traces a line between two points on a navmesh.
		/// </summary>
		/// <returns>True if the ray is terminated before reaching target position. Otherwise returns false.</returns>
		public bool Raycast( LNX_NavmeshHit lnxStartHit, LNX_NavmeshHit lnxEndHit ) //todo: Unit test!!!
		{
			//DBGRaycast = "";

			#region SHORT-CIRCUITING ==================================================
			if (lnxStartHit.TriIndex == lnxEndHit.TriIndex) //If start and end hit are on same triangle...
			{
				return false;
			}

			if
			(
				Triangles[lnxStartHit.TriIndex].HasIndexInKnownFullyVisibleList(lnxEndHit.TriIndex) ||
				Triangles[lnxEndHit.TriIndex].HasIndexInKnownFullyVisibleList(lnxStartHit.TriIndex)
			)
			{
				return false;
			}
			#endregion

			string s = "";
			return !LNX_Utils.TryProjectThrough( this, lnxStartHit, lnxEndHit, ref s );
		}

		/// <summary>
		/// Traces a line between two points on a navmesh.
		/// </summary>
		/// <returns>True if the ray is terminated before reaching target position. Otherwise returns false.</returns>
		public bool Raycast(LNX_NavmeshHit lnxStartHit, LNX_NavmeshHit lnxEndHit, out LNX_Path outPath)
		{
			#region SHORT-CIRCUITING ==================================================
			if (lnxStartHit.TriIndex == lnxEndHit.TriIndex) //If start and end hit are on same triangle...
			{
				outPath = new LNX_Path(this, lnxStartHit, lnxEndHit);
				return false;
			}
			#endregion

			//Debug.Log($"no short circuits. Now trying TryProjectPathThrough...");
			string s = "";

			bool rslt = !LNX_Utils.TryProjectPathThrough(this, lnxStartHit, lnxEndHit, out outPath, ref s);
			//Debug.Log(s);
			return rslt;
			//return !LNX_Utils.TryProjectPathThrough( this, lnxStartHit, lnxEndHit, out outPath, ref s );
		}
		public bool Raycast(LNX_NavmeshHit lnxStartHit, LNX_NavmeshHit lnxEndHit, out LNX_Path outPath, ref StringBuilder sb) // 4 <<<<<<<<<<<<<<<<<<<<<<<<<<<<<
		{
			sb.AppendLine($"Raycast( '{lnxStartHit}', '{lnxEndHit}' )");
			#region SHORT-CIRCUITING ==================================================
			if (lnxStartHit.TriIndex == lnxEndHit.TriIndex) //If start and end hit are on same triangle...
			{
				sb.AppendLine("tri indices of both hits are the same. Returning simple path...");
				outPath = new LNX_Path(this, lnxStartHit, lnxEndHit);
				return false;
			}
			#endregion
			sb.AppendLine("no short-circuiting. Trying TryProjectPathThrough()...");

			//Debug.Log($"no short circuits. Now trying TryProjectPathThrough...");
			string s = "";

			bool rslt = !LNX_Utils.TryProjectPathThrough(this, lnxStartHit, lnxEndHit, out outPath, ref s);
			sb.AppendLine($"\nreport==========\n" +
				$"{s}\n" +
				$"===================");
			//Debug.Log(s);
			return rslt;
			//return !LNX_Utils.TryProjectPathThrough( this, lnxStartHit, lnxEndHit, out outPath, ref s );
		}

		/// <summary>
		/// Traces a line between two points on a navmesh.
		/// </summary>
		/// <returns>True if the ray is terminated before reaching target position. Otherwise returns false.</returns>
		public bool Raycast( Vector3 sourcePosition, Vector3 targetPosition, float maxSampleDistance, bool considerOffPerimeter = false ) //todo: Unit test!!!
		{
			//DBGRaycast = "";

			LNX_NavmeshHit lnxStartHit = LNX_NavmeshHit.None;
			LNX_NavmeshHit lnxEndHit = LNX_NavmeshHit.None;

			#region GET START AND END POINTS------------------------------------------
			if (!SamplePosition(sourcePosition, out lnxStartHit, maxSampleDistance, considerOffPerimeter))
			{
				//DBGRaycast += $"tried samplePosition. Still didn't work. Returning early...\n";
				return true;
			}

			if (!SamplePosition(targetPosition, out lnxEndHit, maxSampleDistance, considerOffPerimeter))
			{
				//DBGRaycast += $"tried samplePosition. Still didn't work. Returning early...\n";
				return true;
			}
			#endregion

			#region SHORT-CIRCUITING ==================================================
			if (lnxStartHit.TriIndex == lnxEndHit.TriIndex) //If start and end hit are on same triangle...
			{
				return false;
			}

			if
			(
				Triangles[lnxStartHit.TriIndex].HasIndexInKnownFullyVisibleList(lnxEndHit.TriIndex) ||
				Triangles[lnxEndHit.TriIndex].HasIndexInKnownFullyVisibleList(lnxStartHit.TriIndex)
			) 
			{
				return false;
			}
			#endregion

			return Raycast( lnxStartHit, lnxEndHit );

		}

		/// <summary>
		/// Traces a line between two points on a navmesh.
		/// </summary>
		/// <returns>True if the ray is terminated before reaching target position. Otherwise returns false.</returns>
		public bool Raycast(Vector3 sourcePosition, Vector3 targetPosition, float maxSampleDistance, out LNX_Path outPath,
			bool considerOffPerimeter = false) //todo: Unit test!!!
		{
			string s = "";
			#region GET START AND END POINTS------------------------------------------
			LNX_NavmeshHit lnxStartHit = LNX_NavmeshHit.None;
			LNX_NavmeshHit lnxEndHit = LNX_NavmeshHit.None;

			if ( !SamplePosition(sourcePosition, out lnxStartHit, maxSampleDistance, considerOffPerimeter) )
			{
				//DBGRaycast += $"tried samplePosition. Still didn't work. Returning early...\n";
				outPath = LNX_Path.None;
				return true;
			}

			if ( !SamplePosition(targetPosition, out lnxEndHit, maxSampleDistance, considerOffPerimeter) )
			{
				//DBGRaycast += $"tried samplePosition. Still didn't work. Returning early...\n";
				outPath = LNX_Path.None;
				return true;
			}
			#endregion

			#region SHORT-CIRCUITING ==================================================
			if (lnxStartHit.TriIndex == lnxEndHit.TriIndex) //If start and end hit are on same triangle...
			{
				outPath = new LNX_Path(this, lnxStartHit, lnxEndHit);
				return false;
			}

			if
			(
				Triangles[lnxStartHit.TriIndex].HasIndexInKnownFullyVisibleList(lnxEndHit.TriIndex) ||
				Triangles[lnxEndHit.TriIndex].HasIndexInKnownFullyVisibleList(lnxStartHit.TriIndex)
			)
			{
				outPath = new LNX_Path( this, lnxStartHit, lnxEndHit );
				return false;
			}
			#endregion

			Debug.Log($"no short circuits. Now trying atomic raycast with starthit: '{lnxStartHit}', and endhit: '{lnxEndHit}'...");
			return Raycast( lnxStartHit, lnxEndHit, out outPath );
		}
		public bool Raycast_dbg(Vector3 sourcePosition, Vector3 targetPosition, float maxSampleDistance, out LNX_Path outPath, ref string dbgRprt,
			bool considerOffPerimeter = false) //todo: Unit test!!!
		{
			dbgRprt = "";
			#region GET START AND END POINTS------------------------------------------
			LNX_NavmeshHit lnxStartHit = LNX_NavmeshHit.None;
			LNX_NavmeshHit lnxEndHit = LNX_NavmeshHit.None;

			if (!SamplePosition(sourcePosition, out lnxStartHit, maxSampleDistance, considerOffPerimeter))
			{
				dbgRprt += $"Could NOT sample sourcePosition. Returning early...\n";
				outPath = LNX_Path.None;
				return true;
			}

			if (!SamplePosition(targetPosition, out lnxEndHit, maxSampleDistance, considerOffPerimeter))
			{
				dbgRprt += $"Could NOT sample targetPosition. Returning early...\n";
				outPath = LNX_Path.None;
				return true;
			}
			#endregion

			dbgRprt += $"sampled startHit: '{lnxStartHit}', and endHit: '{lnxEndHit}'...\n";

			#region SHORT-CIRCUITING ==================================================
			if (lnxStartHit.TriIndex == lnxEndHit.TriIndex) //If start and end hit are on same triangle...
			{
				dbgRprt += $"start hit tri index ('{lnxStartHit.TriIndex}') and end hit tri index ('{lnxEndHit.TriIndex}') " +
					$"were the same. Short-circuiting by returning simple, 2-point path...\n";
				outPath = new LNX_Path(this, lnxStartHit, lnxEndHit);
				return false;
			}
			#endregion

			dbgRprt += $"no short circuits. Now trying 'atomic' raycast method with starthit: '{lnxStartHit}', and endhit: '{lnxEndHit}'...";
			return Raycast(lnxStartHit, lnxEndHit, out outPath);
		}

		/// <summary>
		/// Traces a line between two points on a navmesh.
		/// </summary>
		/// <returns>True if the ray is terminated before reaching target position. Otherwise returns false.</returns>
		public bool Raycast( LNX_Vertex startVert, LNX_Vertex endVert) //todo: Unit test!!!
		{
			//TODO: implement the logic for checking if a relationship (which will have a pre-cached path)
			// already exists, and then I can simply return based on whether the path is straight

			#region SHORT-CIRCUITING ==================================================
			if( startVert == endVert )
			{
				return false;
			}
			if( startVert.TriangleIndex == endVert.TriangleIndex )
			{
				return false;
			}
			if  //If start and end hit are on same triangle...
			(
				Triangles[startVert.TriangleIndex].HasIndexInKnownFullyVisibleList(endVert.TriangleIndex) ||
				Triangles[endVert.TriangleIndex].HasIndexInKnownFullyVisibleList(startVert.TriangleIndex)
			)
			{
				//DBGRaycast += $"Short-circuiting. Start and end hits are on the same triangle surface...";
				return false;
			}
			#endregion

			string s = "";
			return !LNX_Utils.TryProjectThrough( this, new LNX_NavmeshHit(startVert), new LNX_NavmeshHit(endVert), ref s );
		}

		/// <summary>
		/// Traces a line between two points on a navmesh.
		/// </summary>
		/// <returns>True if the ray is terminated before reaching target position. Otherwise returns false.</returns>
		public bool Raycast(LNX_Vertex startVert, LNX_Vertex endVert, out LNX_Path outPath ) //todo: Unit test!!!
		{
			//TODO: implement the logic for checking if a relationship (which will have a pre-cached path)
			// already exists, and then I can simply return based on whether the path is straight and set 
			//outPath to the relationship path

			outPath = LNX_Path.None;

			#region SHORT-CIRCUITING ==================================================
			if (startVert == endVert)
			{
				return false;
			}
			if (startVert.TriangleIndex == endVert.TriangleIndex)
			{
				outPath = new LNX_Path(this, new LNX_NavmeshHit(startVert), new LNX_NavmeshHit(endVert));
				return false;
			}
			if  //If start and end hit are on same triangle...
			(
				Triangles[startVert.TriangleIndex].HasIndexInKnownFullyVisibleList(endVert.TriangleIndex) ||
				Triangles[endVert.TriangleIndex].HasIndexInKnownFullyVisibleList(startVert.TriangleIndex)
			)
			{
				outPath = new LNX_Path (this, new LNX_NavmeshHit(startVert), new LNX_NavmeshHit(endVert) );
				return false;
			}
			#endregion
			string s = "";
			return !LNX_Utils.TryProjectPathThrough(this, startVert, endVert, out outPath, ref s);
		}

		/// <summary>
		/// Calculates a path over this navmesh from the start to the end point.
		/// </summary>
		/// <param name="startPos_passed"></param>
		/// <param name="endPos_passed"></param>
		/// <param name="maxSampleDistance"></param>
		/// <param name="outPath"></param>
		/// <param name="considerOffPerimeter"></param>
		/// <returns></returns>
		public bool CalculatePath(LNX_NavmeshHit startHit, LNX_NavmeshHit endHit,
			out LNX_Path outPath, ref StringBuilder dbgCalculatePath)					 // 2 <<<<<<<<<<<<<<<<<<<<<<<<<
		{
			dbgCalculatePath.AppendLine($"CalculatePath(startHit: '{startHit}', endHit: '{endHit}'\n");

			dbgCalculatePath.AppendLine( $"first, attempting to raycast to the destination..." );

			if ( !Raycast(startHit, endHit, out outPath, ref dbgCalculatePath) )
			{
				dbgCalculatePath.AppendLine( $"Initial raycast was false, meaning that it did NOT hit an obstruction. " +
					$"outPath: '{outPath}'. Returning true...");
				return true;
			}
			else
			{
				dbgCalculatePath.AppendLine( $"Initial raycast returned true, meaning it DID hit an obstruction. Commencing " +
					$"with pathfind operation...");

				dbgCalculatePath.AppendLine( $"assembling list of boundary verts...");
				
				float runningClosestDistance = float.MaxValue;
				/*
				if ( 
					Triangles[startHit.TriIndex].Verts[0].RelationshipsCollectionIsValid && 
					Triangles[endHit.TriIndex].Verts[0].RelationshipsCollectionIsValid
				)
				{
					dbgCalculatePath += $"relationship collections were valid...\n";
					runningClosestDistance = Vector3.Distance(startHit.HitPosition, Triangles[startHit.TriIndex].Verts[0].V_Position) +
					Triangles[startHit.TriIndex].Verts[0].GetPathTo(Triangles[endHit.TriIndex].Verts[0]).TotalDistance +
					Vector3.Distance(Triangles[endHit.TriIndex].Verts[0].V_Position, endHit.HitPosition);
				}
				*/

				dbgCalculatePath.AppendLine($"pre-determined initial running closest dist to be: '{runningClosestDistance}'...\n" +
					$"Now checking for which verts are visible from start position...");

				string dbgvsvrtfrmPt = "";
				List<LNX_Path> visblVrtPths = new List<LNX_Path>();
				List<LNX_ComponentCoordinate> visibleVerts = GetVisibleVertsFromPoint( startHit, out visblVrtPths, ref dbgvsvrtfrmPt, false );

				//Debug.Log($"GetVisibleVerts returned '{visibleVerts.Count}' verts. Report...\n" +
					//$"{dbgvsvrtfrmPt}");

				dbgCalculatePath.AppendLine( $"Decided there are '{visibleVerts.Count}' visible verts. Pinging each visible vert...\n");

				#region CONSTRUCT PATHS -------------------------------------
				LNX_Path[] paths = new LNX_Path[visibleVerts.Count];
				int indx_runningBestPath = -1;
				float dist_runningBestPath = float.MaxValue;
				for( int i_visblVrts = 0; i_visblVrts < visibleVerts.Count; i_visblVrts++ )
				{
					dbgCalculatePath.AppendLine($"for {i_visblVrts}: '{visibleVerts[i_visblVrts]}' valid?: " +
						$"'{GetVertexAtCoordinate(visibleVerts[i_visblVrts]).IsRelationshipCollectionValid()}'---");
					bool realPing = false;

					if( realPing )
					{
						paths[i_visblVrts] = Triangles[visibleVerts[i_visblVrts].TrianglesIndex].Verts[visibleVerts[i_visblVrts].ComponentIndex].Ping(
						DateTime.Now, endHit, this, runningClosestDistance, visblVrtPths[i_visblVrts], visibleVerts
						);
						if ( paths[i_visblVrts].TotalDistance < dist_runningBestPath )
						{
							indx_runningBestPath = i_visblVrts;
							dist_runningBestPath = paths[i_visblVrts].TotalDistance;
						}
					}
					else
					{
						string s = "";
						Triangles[visibleVerts[i_visblVrts].TrianglesIndex].Verts[visibleVerts[i_visblVrts].ComponentIndex].DbgPing(
							ref s, endHit, this, runningClosestDistance, visblVrtPths[i_visblVrts], visibleVerts
						);
						dbgCalculatePath.AppendLine( $"{s}\n" );
					}
				}
				#endregion

				if( indx_runningBestPath > -1 )
				{
					outPath = paths[indx_runningBestPath];
				}
			}

			return true;
		}

		/// <summary>
		/// Calculates a path over this navmesh from the start to the end point.
		/// </summary>
		/// <param name="startPos_passed"></param>
		/// <param name="endPos_passed"></param>
		/// <param name="maxSampleDistance"></param>
		/// <param name="outPath"></param>
		/// <param name="considerOffPerimeter"></param>
		/// <returns></returns>
		public bool CalculatePath(Vector3 startPos_passed, Vector3 endPos_passed,
			float maxSampleDistance, out LNX_Path outPath, ref string dbgCalculatePath, bool considerOffPerimeter = true)
		{
			dbgCalculatePath = $"CalculatePath(strt: '{startPos_passed}', end: '{endPos_passed}', smplDst: '{maxSampleDistance}' " +
				$"at {DateTime.Now.ToString()})\n";

			#region SAMPLE START AND END POINT -------------------------------------------
			LNX_NavmeshHit startHit = new LNX_NavmeshHit();
			LNX_NavmeshHit endHit = new LNX_NavmeshHit();
			outPath = LNX_Path.None;

			if (SamplePosition(startPos_passed, out startHit, maxSampleDistance, considerOffPerimeter))
			{
				//startPos_passed = startHit.HitPosition; //do we really need this? dws
				dbgCalculatePath += $"SamplePosition() hit startpos on tri '{startHit.TriIndex}', at: '{startHit.Position}'\n";
			}
			else
			{
				dbgCalculatePath += $"SamplePosition() did NOT hit startpos.\n";
				return false; //todo: returning a boolean is newly added. Make sure this return boolean is being properly used...
			}

			if (SamplePosition(endPos_passed, out endHit, maxSampleDistance, considerOffPerimeter))
			{
				//endPos_passed = endHit.HitPosition; //do we really need this? dws
				dbgCalculatePath += $"SamplePosition() hit endpos on tri '{endHit.TriIndex}', at: '{endHit.Position}'\n";
			}
			else
			{
				dbgCalculatePath += $"SamplePosition() did NOT hit endpos.\n";
				return false; //todo: returning a boolean is newly added. Make sure this return boolean is being properly used...
			}
			#endregion

			dbgCalculatePath += $"finished sampling start and end point. Now using these hit points to calculate path...\n";

			StringBuilder sb = new StringBuilder();
			return CalculatePath(startHit, endHit, out outPath, ref sb );
		}

		public bool CalculatePath(LNX_Vertex startVert, LNX_Vertex endVert, out LNX_Path outPath, ref StringBuilder dbgCalculatePath) //1 <<<<<<<<<<<<<<<<
		{
			dbgCalculatePath.AppendLine( $"CalculatePath(startVert: '{startVert}', endVert: '{endVert}')\n" );

			#region SHORT-CIRCUITING ===================================
			if ( startVert.IsRelationshipCollectionValid() && endVert.IsRelationshipCollectionValid() )
			{
				dbgCalculatePath.AppendLine( 
					$"start rels length: '{startVert.Relationships.Length}', end rels length: '{endVert.Relationships.Length}' " +
					$"relationships valid. Getting already-existing cached relational path...");
				outPath = startVert.GetPathTo( endVert );
				return true;
			}
			#endregion

			dbgCalculatePath.AppendLine($"relationships for start and/or end vert not valid. Passing off to more atomic version of method...");

			return CalculatePath( new LNX_NavmeshHit(startVert), new LNX_NavmeshHit(endVert), out outPath, ref dbgCalculatePath );
		}
		
		public List<LNX_ComponentCoordinate> GetVisibleVertsFromPoint( 
			LNX_NavmeshHit hit, ref string dbgString, bool includeFringeVerts = false, List<LNX_ComponentCoordinate> excludeVerts = null 
		)
		{
			dbgString = $"GetVisibleVertsFromPoint({hit}, excludeverts: '{(excludeVerts == null ? "null" : excludeVerts.Count)}')\n";

			List<LNX_ComponentCoordinate> visibleVerts = new List<LNX_ComponentCoordinate>();

			dbgString += $"assembling list of visible verts from this hit position...\n";

			for ( int i_tris = 0; i_tris < Triangles.Length; i_tris++ )
			{
				//dbgString += $"for tri'{i_tris}'...\n";
				//Debug.Log($"for tri'{i_tris}'...\n");
				
				if ( i_tris == hit.TriIndex )
				{
					dbgString += $"same tri index. Continuing...\n";
					continue;
				}
				

				//dbgCalculatePath += $"for tri{i_tris}...\n";
				for (int i_vrts = 0; i_vrts < 3; i_vrts++)
				{
					//Debug.Log($"for vert'{i_vrts}'...\n");

					//dbgString += $"...for vrt{i_vrts}...\n";
					//dbgString += $"for tri{i_tris}.Vert[{i_vrts}]...\n";

					#region SHORT-CIRCUITING =======================================
					if ( excludeVerts != null && excludeVerts.Count > 0 ) 
					{
						//dbgString += $"checking exclude verts...\n";
						//Debug.Log($"checking exclude verts...");
						bool foundOne = false;
						for (int j = 0; j < excludeVerts.Count; j++ )
						{
							//Debug.Log($"for exclude vert'{j}'...\n");

							if (Triangles[i_tris].Verts[i_vrts].SharesVertSpace(Triangles[excludeVerts[j].TrianglesIndex].Verts[excludeVerts[j].ComponentIndex]) )
							{
								dbgString += $"found that vert[{i_tris}][{i_vrts}] shares space with exclude vert {j}...\n";
								//Debug.Log($"found that vert[{i_tris}][{i_vrts}] shares space with exclude vert {j}...");
								foundOne = true;
								break;
							}
						}

						if ( foundOne )
						{
							continue;
						}
					}

					if ( visibleVerts.Count > 0 )
					{
						//dbgString += $"checking already logged visible verts...\n";
						bool foundOneAtSamePos = false;
						for ( int i_growingList = 0; i_growingList < visibleVerts.Count; i_growingList++ )
						{
							if ( Triangles[i_tris].Verts[i_vrts].SharesVertSpace(Triangles[visibleVerts[i_growingList].TrianglesIndex].Verts[visibleVerts[i_growingList].ComponentIndex]) )
							{
								dbgString += $"There's a vert in growing list of visible already logged at the same position as this vert[{i_tris}][{i_vrts}]. Bypassing...\n";
								//Debug.Log($"Vert in growing list of visible already logged at the same position as this vert[{i_tris}][{i_vrts}]. Bypassing...");

								foundOneAtSamePos = true;
								continue;
							}
						}

						if (foundOneAtSamePos)
						{
							continue;
						}
					}

					if( !includeFringeVerts && IsBoundsVert(i_tris, i_vrts) )
					{
						dbgString += $"Found that visible vert '[{i_tris}][{i_vrts}]' was a fringe vert. Excluding from list...\n";
						continue;
					}
					#endregion---------------------------------
					//dbgString += $"none of the short-circuting worked for vert[{i_tris}][{i_vrts}]. Trying raycast...\n";

					if ( !Raycast(hit, new LNX_NavmeshHit(Triangles[i_tris].Verts[i_vrts])) )
					{
						dbgString += $"raycast to vert[{i_tris}][{i_vrts}] showed clear path. Adding vert to visible...\n";
						visibleVerts.Add(Triangles[i_tris].Verts[i_vrts].MyCoordinate);
					}
					else
					{
						dbgString += $"raycast to vert[{i_tris}][{i_vrts}] hit obstruction...\n";
					}
				}
			}

			return visibleVerts;
		}

		public List<LNX_ComponentCoordinate> GetVisibleVertsFromPoint( // 3  <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
			LNX_NavmeshHit hit, out List<LNX_Path> outPaths, ref string dbgString, 
			bool includeFringeVerts = false, List<LNX_ComponentCoordinate> excludeVerts = null
		)
		{
			dbgString = $"GetVisibleVertsFromPoint({hit}, excludeverts: '{(excludeVerts == null ? "null" : excludeVerts.Count)}')\n";

			List<LNX_ComponentCoordinate> visibleVerts = new List<LNX_ComponentCoordinate>();
			outPaths = new List<LNX_Path>();

			dbgString += $"assembling list of visible verts from this hit position...\n";

			for (int i_tris = 0; i_tris < Triangles.Length; i_tris++)
			{
				//dbgString += $"for tri'{i_tris}'...\n";
				//Debug.Log($"for tri'{i_tris}'...\n");

				if (i_tris == hit.TriIndex)
				{
					dbgString += $"same tri index. Continuing...\n";
					continue;
				}

				//dbgCalculatePath += $"for tri{i_tris}...\n";
				for (int i_vrts = 0; i_vrts < 3; i_vrts++)
				{
					//Debug.Log($"for vert'{i_vrts}'...\n");

					//dbgString += $"...for vrt{i_vrts}...\n";
					//dbgString += $"for tri{i_tris}.Vert[{i_vrts}]...\n";

					#region SHORT-CIRCUITING =======================================
					if (excludeVerts != null && excludeVerts.Count > 0)
					{
						//dbgString += $"checking exclude verts...\n";
						//Debug.Log($"checking exclude verts...");
						bool foundOne = false;
						for (int j = 0; j < excludeVerts.Count; j++)
						{
							//Debug.Log($"for exclude vert'{j}'...\n");

							//explanation for following if-check: It's not enough to simply check if the currently-iterated vert is in the list of excludeVerts, we also need to make sure it does not share space with any of the exclude verts because each vert shares space with multiple other verts
							if ( Triangles[i_tris].Verts[i_vrts].SharesVertSpace(Triangles[excludeVerts[j].TrianglesIndex].Verts[excludeVerts[j].ComponentIndex])) //todo: this check won't be necessary if we unify the verts
							{
								dbgString += $"found that vert[{i_tris}][{i_vrts}] shares space with exclude vert {j}...\n";
								//Debug.Log($"found that vert[{i_tris}][{i_vrts}] shares space with exclude vert {j}...");
								foundOne = true;
								break;
							}
						}

						if (foundOne)
						{
							continue;
						}
					}

					if ( visibleVerts.Count > 0 )
					{
						//dbgString += $"checking already logged visible verts...\n";
						bool foundOneAtSamePos = false;
						for ( int i_growingList = 0; i_growingList < visibleVerts.Count; i_growingList++) //todo: this check won't be necessary if we unify the verts
						{
							if ( Triangles[i_tris].Verts[i_vrts].SharesVertSpace(Triangles[visibleVerts[i_growingList].TrianglesIndex].Verts[visibleVerts[i_growingList].ComponentIndex]))
							{
								dbgString += $"There's a vert in growing list of visible already logged at the same position as this vert[{i_tris}][{i_vrts}]. Bypassing...\n";
								//Debug.Log($"Vert in growing list of visible already logged at the same position as this vert[{i_tris}][{i_vrts}]. Bypassing...");

								foundOneAtSamePos = true;
								continue;
							}
						}

						if (foundOneAtSamePos)
						{
							continue;
						}
					}

					if ( !includeFringeVerts && IsBoundsVert(i_tris, i_vrts) )
					{
						dbgString += $"Found that visible vert '[{i_tris}][{i_vrts}]' was a fringe vert. Excluding from list...\n";
						continue;
					}

					//todo: should I even do the following check? It might be okay to log the vertex that meets the following check to visible list
					if ( Triangles[i_tris].Verts[i_vrts].V_Position == hit.Position ) //we'll want to continue because we may be using a vertex as a perspective position...
					{
						dbgString += $"vert was at same position as perspective hit position. continuing...\n";
						continue;
					}
					#endregion---------------------------------
					//dbgString += $"none of the short-circuting worked for vert[{i_tris}][{i_vrts}]. Trying raycast...\n";
					LNX_Path path_startHit_toVert = LNX_Path.None;

					if ( !Raycast(hit, new LNX_NavmeshHit(Triangles[i_tris].Verts[i_vrts]), out path_startHit_toVert) )
					{
						dbgString += $"raycast to vert[{i_tris}][{i_vrts}] showed clear path. Adding vert to visible...\n";
						visibleVerts.Add(Triangles[i_tris].Verts[i_vrts].MyCoordinate);
						outPaths.Add( path_startHit_toVert );
					}
					else
					{
						dbgString += $"raycast to vert[{i_tris}][{i_vrts}] hit obstruction...\n";
					}
				}
			}

			return visibleVerts;
		}

		public List<LNX_ComponentCoordinate> GetVisibleVertsFromPoint( LNX_Vertex vert, out List<LNX_Path> outPaths, ref string dbgString, 
			bool includeFringeVerts = false, List<LNX_ComponentCoordinate> excludeVerts = null)
		{
			List<LNX_ComponentCoordinate> returnCoords = new List<LNX_ComponentCoordinate>();
			outPaths = new List<LNX_Path>();

			if ( vert.IsRelationshipCollectionValid() )
			{
				dbgString += $"relationships valid. Using existing relational information...\n";

				for ( int i = 0; i < vert.Relationships.Length; i++ )
				{
					if ( vert.Relationships[i].CanSee )
					{
						returnCoords.Add( vert.Relationships[i].RelatedVertCoordinate );
						outPaths.Add( vert.Relationships[i].PathTo );
					}
				}
			}
			else
			{
				returnCoords = GetVisibleVertsFromPoint( new LNX_NavmeshHit(vert), out outPaths, ref dbgString, includeFringeVerts );
			}

			return returnCoords;
		}

		#endregion // (END) MAIN API METHODS---------------------

		#region DATA ===============================================================
		/// <summary>
		/// Calculates and caches certain information that can drastically speed up operations like pathfinding.<para/>
		/// Note: This method can be a VERY
		/// expensive call, possibly taking many seconds, and even minutes if your navmesh is big enough. In typical applications, you shouldn't call this 
		/// method, but instead use SaveEfficiencyDataToDisk() in the editor, before runtime, via this script's contextmenu. This will pre-cache the efficiency 
		/// information to a JSON file so that you don't have to wait on calculating at runtime.<para/> 
		/// If you can't pre-cache this information in the editor, and need to call this at runtime, because of a 
		/// situation like runtime navmesh creation, find an appropriate spot in your code to call it once, and optionally in a thread so it doesn't 
		/// hang up your game.
		/// </summary>
		public void CalculateEfficiencyData()
		{
			#region CALCULATE TRI KNOWN-VISIBILITY ----------------------------------
			LNX_Edge[] trmnlEdges = GetTerminalEdges(false);

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				Triangles[i].ClearKnownVisible(); //These indices need to be all cleared before being calculated to 
				//prevent problems when using 2-way assignment...
			}

			float maxTime = 45f;
			DateTime dt_start = DateTime.Now;
			bool cmpltd = false;

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				Debug.Log(i);
				Triangles[i].CalculateCompletelyVisibleTris(this, trmnlEdges);
				if (DateTime.Now.Subtract(dt_start).TotalSeconds > maxTime)
				{
					Debug.LogWarning("took too long. breaking...");
					break;
				}

				if (i >= Triangles.Length - 1)
				{
					cmpltd = true;
				}
			}
			#endregion

			#region CALCULATE BOUNDS INFORMATION =========================
			List<LNX_ComponentCoordinate> boundsVerts_temp = new List<LNX_ComponentCoordinate>();
			List<LNX_ComponentCoordinate> boundsEdges_temp = new List<LNX_ComponentCoordinate>();
			for (int i_tris = 0; i_tris < Triangles.Length; i_tris++)
			{
				for (int j = 0; j < 3; j++)
				{
					if ( IsBoundsVert(i_tris, j) )
					{
						boundsVerts_temp.Add( new LNX_ComponentCoordinate(i_tris, j) );
					}
					if ( IsBoundsEdge(i_tris, j) )
					{
						boundsEdges_temp.Add(new LNX_ComponentCoordinate(i_tris, j));
					}
				}
			}
			boundsVerts = boundsVerts_temp.ToArray();
			boundsEdges = boundsEdges_temp.ToArray();

			#endregion
		}

		public void WriteEfficiencyData()
		{
			string fPath = /*GetEfficiencyDataFilepath_Managed();*/ "";

			LNX_NavMeshData data = new LNX_NavMeshData(this);
			File.WriteAllText( fPath, JsonUtility.ToJson(data, true) );

			Debug.Log($"Wrote data to json at: '{fPath}'");
		}

		/*
		/// <summary>
		/// This method can be called from the editor to calculate and pre-cache efficiency data in one 
		/// call. Otherwise, just call CalculateEfficiencyData() in the STart() if you're not saving the data.
		/// </summary>
		[ContextMenu("z call CalculateAndWriteEfficiencyData()")]
		public void CalculateAndWriteEfficiencyData()
		{
			CalculateEfficiencyData();
			WriteEfficiencyData();
		}
		*/

#if UNITY_EDITOR
		/// <summary>
		/// Managed method for saving efficiency data for this LNX_NavMesh to a json text file. Will overwrite 
		/// existing data if it can find the existing data file.<para/>
		/// Note: Only call this method in the Inspector via the context menu - NOT at runtime. This will 
		/// pre-cache efficiency data to be used at runtime.
		/// </summary>
		[ContextMenu("z call SaveEfficiencyDataToDisk()")]
		public void SaveEfficiencyDataToDisk()
		{
			Debug.Log($"SaveEfficiencyData() serializedstring: '{serializedDataString}'");
			StringBuilder sb_report = new StringBuilder("SaveEfficiencyDataToDisk() report:\n");

			try
			{
				sb_report.AppendLine("First, calculating efficiency data...");
				CalculateEfficiencyData();

				serializedDataString = ""; //todo: dws

				bool foundWarning = false;
				bool foundError = false;

				string path_foundViaGUID = AssetDatabase.GUIDToAssetPath(cachedGUID);
				sb_report.AppendLine($"path_foundViaGUID: '{path_foundViaGUID}'");

				if ( string.IsNullOrEmpty(cachedGUID) || string.IsNullOrEmpty(path_foundViaGUID) )
				{
					sb_report.AppendLine("writing file as new...");
					// 1. GENERATE DIRECTORY PATH===============================================
					string dirPthString = Path.Combine( LNX_Utils.MakePathFromString(SceneManager.GetActiveScene().path, "/", 1), "Resources"); //this will replace the forward slashes with back-slashes, and stop at the correct element
					sb_report.AppendLine($"made dirPthString: '{dirPthString}'");
					
					if( !Directory.Exists(path_foundViaGUID) )
					{
						sb_report.AppendLine($"dir path didn't exist. Creating directory...");
						Directory.CreateDirectory( dirPthString );
					}

					// 2. GENERATE FILE NAME AND FILE PATH ===============================================
					string filePthString = LNX_Utils.AppendDigitTilUniqueFileName
						($"LNX_{SceneManager.GetActiveScene().name}_{name}_efficiencyData", ".json", dirPthString
					);
					sb_report.AppendLine($"made filePthString: '{filePthString}'");

					if ( string.IsNullOrEmpty(filePthString) )
					{
						sb_report.AppendLine($"LNX ERROR! Tried to create a file with unique file path: 'LNXDATA_{SceneManager.GetActiveScene().name}_{name}.json' with 100 numeric appends, and all file names were taken...");
						foundError = true;
						return;
					}

					// 3. CREATE DATA OBJECT AND WRITE TO DISK ===============================================
					LNX_NavMeshData data = new LNX_NavMeshData(this);
					sb_report.AppendLine("a");
					File.WriteAllText(filePthString, JsonUtility.ToJson(data, true));
					sb_report.AppendLine("b");

					sb_report.AppendLine($"Wrote data to json at: '{filePthString}'\n");

					// 4. DEAL WITH THE GUID ===============================================


					AssetDatabase.ImportAsset(filePthString);
					sb_report.AppendLine("c");


					string guidString = AssetDatabase.AssetPathToGUID(filePthString);
					sb_report.AppendLine($"fetched guid string via assetdatabase: '{guidString}'");
					if ( string.IsNullOrEmpty(guidString) )
					{
						Debug.LogError($"LNX ERROR! Couldn't get guid for newly created file at '{filePthString}'...");
						Debug.Log( sb_report );
						return;
					}

					// 5. PUT TOGETHER SERIALIZED DATA STRING ===============================================
					serializedDataString = $"{guidString}";
				}
				else
				{
					sb_report.AppendLine("overwriting existing file...");

					// 1. CREATE DATA OBJECT AND WRITE TO DISK ===============================================
					LNX_NavMeshData data = new LNX_NavMeshData(this);
					File.WriteAllText( path_foundViaGUID, JsonUtility.ToJson(data, true) );

					sb_report.AppendLine($"Wrote data to json at: '{path_foundViaGUID}'\n");
				}
				
			}
			catch (Exception)
			{
				Debug.Log(sb_report.ToString());

				throw;


			}

			Debug.Log(sb_report.ToString());
		}
#endif

		/// <summary>
		/// Attempts to read in serialized navmesh data from assets. 
		/// </summary>
		/// <param name="_dataOut"></param>
		/// <returns>Whether the retrieval of the data was succesfull AND whether the retrieved data matches 
		/// and is valid for this LNX_NavMesh</returns>
		public bool TryLoadEfficiencyData( LNX_NavMeshData data )
		{
			if (!data.AmValidForUse())
			{
				Debug.LogError($"LNX ERROR! data read from JSON string was not valid! It may need to be re-calculated");
				return false;
			}

			if (!data.MatchesNavmesh(this))
			{
				Debug.LogError($"LNX ERROR! data read from JSON string does NOT match supplied LNX_NavMesh!");
				return false;
			}

			for (int i = 0; i < Triangles.Length; i++)
			{
				Triangles[i].LoadWithSerializedData(data.Triangles[i]);
			}

			boundsVerts = data.boundsVerts;
			boundsEdges = data.boundsEdges;

			return true;
		}

		/// <summary>
		/// Attempts to read in serialized navmesh data from assets. 
		/// </summary>
		/// <param name="_dataOut"></param>
		/// <returns>Whether the retrieval of the data was succesfull AND whether the retrieved data matches 
		/// and is valid for this LNX_NavMesh</returns>
		public bool TryLoadEfficiencyData()
		{
			string fPath = /*GetEfficiencyDataFilepath_Managed();*/ "";

			if ( !File.Exists(fPath) )
			{
				Debug.LogError($"LNX ERROR! File path '{fPath}' did NOT exist!");
				return false;
			}

			string jsonString = File.ReadAllText( fPath );

			LNX_NavMeshData data = JsonUtility.FromJson<LNX_NavMeshData>(jsonString);

			return TryLoadEfficiencyData(data);
		}

		#endregion ------------------

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

		private bool IsBoundsVert( int triIndx, int vertIndx )
		{
			if ( boundsVerts != null && boundsVerts.Length > 0 )
			{
				for ( int i = 0; i < boundsVerts.Length; i++ )
				{
					if( boundsVerts[i].TrianglesIndex == triIndx && boundsVerts[i].ComponentIndex == vertIndx )
					{
						return true;
					}
				}
			}

			if (SurfaceOrientation == LNX_Direction.PositiveY || SurfaceOrientation == LNX_Direction.NegativeY)
			{
				if
				(
					Triangles[triIndx].Verts[vertIndx].V_Position.x == Bounds_HighestX ||
					Triangles[triIndx].Verts[vertIndx].V_Position.x == Bounds_LowestX ||
					Triangles[triIndx].Verts[vertIndx].V_Position.z == Bounds_HighestZ ||
					Triangles[triIndx].Verts[vertIndx].V_Position.z == Bounds_LowestZ
				)
				{
					return true;
				}
			}

			if (SurfaceOrientation == LNX_Direction.PositiveX || SurfaceOrientation == LNX_Direction.NegativeX)
			{
				if
				(
					Triangles[triIndx].Verts[vertIndx].V_Position.y == Bounds_HighestY ||
					Triangles[triIndx].Verts[vertIndx].V_Position.y == Bounds_LowestY ||
					Triangles[triIndx].Verts[vertIndx].V_Position.z == Bounds_HighestZ ||
					Triangles[triIndx].Verts[vertIndx].V_Position.z == Bounds_LowestZ
				)
				{
					return true;
				}
			}

			if (SurfaceOrientation == LNX_Direction.PositiveZ || SurfaceOrientation == LNX_Direction.NegativeZ)
			{
				if
				(
					Triangles[triIndx].Verts[vertIndx].V_Position.y == Bounds_HighestY ||
					Triangles[triIndx].Verts[vertIndx].V_Position.y == Bounds_LowestY ||
					Triangles[triIndx].Verts[vertIndx].V_Position.x == Bounds_HighestX ||
					Triangles[triIndx].Verts[vertIndx].V_Position.x == Bounds_LowestX
				)
				{
					return true;
				}
			}

			return false;
		}

		public bool IsBoundsEdge(int triIndx, int edgeIndx)
		{
			//note: It's possible to have a navmesh that isn't mostly square shaped. This won't help for that...
			//Debug.Log($"{nameof(AmBoundsEdge)}(), {nm.SurfaceOrientation}");
			if (Triangles[triIndx].Edges[edgeIndx].SharedEdgeCoordinate != LNX_ComponentCoordinate.None)
			{
				return false;
			}

			if (SurfaceOrientation == LNX_Direction.PositiveY || SurfaceOrientation == LNX_Direction.NegativeY)
			{
				if
				(
					(Triangles[triIndx].Edges[edgeIndx].StartPosition.x == Bounds_HighestX && Triangles[triIndx].Edges[edgeIndx].EndPosition.x == Bounds_HighestX) ||
					(Triangles[triIndx].Edges[edgeIndx].StartPosition.x == Bounds_LowestX && Triangles[triIndx].Edges[edgeIndx].EndPosition.x == Bounds_LowestX) ||
					(Triangles[triIndx].Edges[edgeIndx].StartPosition.z == Bounds_HighestZ && Triangles[triIndx].Edges[edgeIndx].EndPosition.z == Bounds_HighestZ) ||
					(Triangles[triIndx].Edges[edgeIndx].StartPosition.z == Bounds_LowestZ && Triangles[triIndx].Edges[edgeIndx].EndPosition.z == Bounds_LowestZ)
				)
				{
					return true;
				}
			}

			if (SurfaceOrientation == LNX_Direction.PositiveX || SurfaceOrientation == LNX_Direction.NegativeX)
			{
				if
				(
					(Triangles[triIndx].Edges[edgeIndx].StartPosition.y == Bounds_HighestY && Triangles[triIndx].Edges[edgeIndx].EndPosition.y == Bounds_HighestY) ||
					(Triangles[triIndx].Edges[edgeIndx].StartPosition.y == Bounds_LowestY && Triangles[triIndx].Edges[edgeIndx].EndPosition.y == Bounds_LowestY) ||
					(Triangles[triIndx].Edges[edgeIndx].StartPosition.z == Bounds_HighestZ && Triangles[triIndx].Edges[edgeIndx].EndPosition.z == Bounds_HighestZ) ||
					(Triangles[triIndx].Edges[edgeIndx].StartPosition.z == Bounds_LowestZ && Triangles[triIndx].Edges[edgeIndx].EndPosition.z == Bounds_LowestZ)
				)
				{
					return true;
				}
			}

			if (SurfaceOrientation == LNX_Direction.PositiveZ || SurfaceOrientation == LNX_Direction.NegativeZ)
			{
				if
				(
					(Triangles[triIndx].Edges[edgeIndx].StartPosition.y == Bounds_HighestY && Triangles[triIndx].Edges[edgeIndx].EndPosition.y == Bounds_HighestY) ||
					(Triangles[triIndx].Edges[edgeIndx].StartPosition.y == Bounds_LowestY &&  Triangles[triIndx].Edges[edgeIndx].EndPosition.y == Bounds_LowestY) ||
					(Triangles[triIndx].Edges[edgeIndx].StartPosition.x == Bounds_HighestX && Triangles[triIndx].Edges[edgeIndx].EndPosition.x == Bounds_HighestX) ||
					(Triangles[triIndx].Edges[edgeIndx].StartPosition.x == Bounds_LowestX &&  Triangles[triIndx].Edges[edgeIndx].EndPosition.x == Bounds_LowestX)
				)
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
				$"{nameof(serializedDataString)}: '{serializedDataString}'\n" +
				$"{nameof(SurfaceOrientation)}: '{SurfaceOrientation}'\n" +
				$"Bounds-----\n" +
				$"{nameof(Bounds_LowestX)}: '{Bounds_LowestX}, {nameof(Bounds_HighestX)}: '{Bounds_HighestX}'\n" +
				$"{nameof(Bounds_LowestY)}: '{Bounds_LowestY}, {nameof(Bounds_HighestY)}: '{Bounds_HighestY}'\n" +
				$"{nameof(Bounds_LowestZ)}: '{Bounds_LowestZ}, {nameof(Bounds_HighestZ)}: '{Bounds_HighestZ}'\n");

			if( _VisualizationMesh == null )
			{
				Debug.Log($"Visualization mesh was null...");
			}
			else
			{
				Debug.Log("Visual Mesh====\n" +
					$"{nameof(_VisualizationMesh.vertices)} length: '{_VisualizationMesh.vertices.Length}'" +
					$"");
			}

			if( Triangles == null )
			{
				Debug.Log($"Triangle collection was null...");
			}
			else
			{
				Debug.Log($"{nameof(Triangles)} length: '{Triangles.Length}'");

				for (int i = 0; i < Triangles.Length; i++)
				{
					Triangles[i].SayCurrentInfo(this);
				}
			}
		}

		[ContextMenu("z call ReportAbnormalities")]
		public void ReportAbnormalities()
		{
			StringBuilder sb_anomolies = new StringBuilder();
			int anomolyCount = 0;

			if( Bounds == null )
			{
				sb_anomolies.AppendLine($"Bounds collection currently null...");
				anomolyCount++;
			}
			if ( Bounds.Length <= 0 )
			{
				sb_anomolies.AppendLine($"Bounds collection length less than or equal to 0...");
				anomolyCount++;
			}


			for ( int i = 0; i < Triangles.Length; i++ )
			{
				sb_anomolies.AppendLine( $"Triangle[{i}]---" );

				string s = Triangles[i].GetAnomolyString( this );

				if ( !string.IsNullOrWhiteSpace(s) )
				{
					anomolyCount++;
					sb_anomolies.AppendLine( s );
				}
			}

			if ( anomolyCount > 0 )
			{
				Debug.LogWarning($"{anomolyCount} Anomolies found!");
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
				Debug.Log($"iterator tri'{i}'...");
				Debug.Log( Triangles[i].GetRelationalString() );
			}
		}

		#endregion

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			if ( Application.isPlaying || Triangles == null)
			{
				return;
			}

			if( drawVisualizationMesh && _VisualizationMesh != null && _VisualizationMesh.vertices != null && 
				_VisualizationMesh.vertices.Length > 0 )
			{
				Gizmos.color = color_visualMesh;
				Gizmos.DrawMesh( _VisualizationMesh );
			}
		}
#endif

	}
}