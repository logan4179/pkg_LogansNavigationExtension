
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.LightTransport;
using UnityEngine.SceneManagement;


namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_NavMeshSurface : MonoBehaviour
	{
		//[Header("OPTIONS")]
		public LNX_Direction SurfaceOrientation = LNX_Direction.PositiveY;

		public LayerMask MyLayerMask;

		/// <summary>Index corresponding to this surface in manager's Surfaces collection. Gets set automatically by manager singleton.</summary>
		public int MyCollectionIndex = -1;
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
		/// will be the lowest/most-negative value point, and element 3 will be the most positive value point</summary>
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

		[InitializeOnLoadMethod]
		private static void OnEditorLoad()
		{
			Debug.Log($"Unity Editor Loaded or Scripts Recompiled.");
		}

		private void OnEnable()
		{
			Debug.Log("lnx_navmesh.onenable()");

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

		/// <summary>
		/// This returns the direction that the navmesh, as a whole, should be considered "facing", for 
		/// projection purposes. IE: This should be the direction that the unity object 
		/// (which has the navmesh component)'s UP direction is facing.
		/// </summary>
		/// <returns></returns>
		public Vector3 GetSurfaceProjectionVector()
		{
			if (SurfaceOrientation == LNX_Direction.PositiveY)
			{
				return Vector3.up;
			}
			else if (SurfaceOrientation == LNX_Direction.NegativeY)
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

		public LNX_Triangle GetTriangle(LNX_AtomicTriangle tri )
		{
			for (int i = 0; i < Triangles.Length; i++)
			{
				if ( tri.CurrentlyPositionallyMatches(Triangles[i]) )
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

		#region FLAT VECTOR METHODS ===========================================
		public Vector3 FlatVector( Vector3 vector )
		{
			if (SurfaceOrientation == LNX_Direction.PositiveY || SurfaceOrientation == LNX_Direction.NegativeY )
			{
				if (vector.y != 0f)
				{
					return new Vector3(vector.x, 0f, vector.z);
				}
				else
				{
					return vector;
				}
			}
			else if ( SurfaceOrientation == LNX_Direction.PositiveX || SurfaceOrientation == LNX_Direction.NegativeX )
			{
				if (vector.x != 0f)
				{
					return new Vector3(0f, vector.y, vector.z);
				}
				else
				{
					return vector;
				}
			}
			else if ( SurfaceOrientation == LNX_Direction.PositiveZ || SurfaceOrientation == LNX_Direction.NegativeZ )
			{
				if (vector.z != 0f)
				{
					return new Vector3(vector.x, vector.y, 0f);
				}
				else
				{
					return vector;
				}
			}

			return Vector3.zero;
		}

		public Vector3 FlatHitPosition( LNX_NavmeshHit hit )
		{
			Vector3 nrml = Vector3.zero;
			if (SurfaceOrientation == LNX_Direction.PositiveY || SurfaceOrientation == LNX_Direction.NegativeY)
			{
				nrml = Vector3.up;
			}
			else if (SurfaceOrientation == LNX_Direction.PositiveX || SurfaceOrientation == LNX_Direction.NegativeX)
			{
				nrml = Vector3.right;
			}
			else if (SurfaceOrientation == LNX_Direction.PositiveZ || SurfaceOrientation == LNX_Direction.NegativeZ)
			{
				nrml = Vector3.forward;
			}
			return FlatVector( hit.Position );
		}
		#endregion

		#region CREATION/SETUP ---------------------------------------------------------
		[NonSerialized, HideInInspector] public string DBG_CalculateTriangulation;
		public void CreateFromTriangulation(NavMeshTriangulation triangulation)
		{
			DateTime dt_methodStart = DateTime.Now;
			DBG_CalculateTriangulation = $"{nameof(CreateFromTriangulation)}()";

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

			DBG_CalculateTriangulation += $"supplied triangulation has '{triangulation.areas.Length}' areas, '{triangulation.vertices.Length}' " +
				$"vertices, and '{triangulation.indices.Length}' indices.\n";
			Debug.Log($"supplied triangulation has '{triangulation.areas.Length}' areas, '{triangulation.vertices.Length}' " +
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
			//Debug.Log($"Finished constructing '{constructedAtomicTris.Count}' atomic tris. Now constructing real list...\n");
			Triangles = new LNX_Triangle[constructedAtomicTris.Count];
			for ( int i = 0; i < constructedAtomicTris.Count; i++ )
			{
				DBG_CalculateTriangulation += $"{i} --------------------------////////////////////////////////////\n";
				//Debug.Log($"{i} --------------------------////////////////////////////////////\n");

				Triangles[i] = new LNX_Triangle(i, constructedAreaIndices[i], constructedAtomicTris, this);
			}
			
			CalculateBounds(); //This needs to happen now before the triangles refresh because the creation of the vert relationships relies on CalculatePath(), which relies on knowing the bounds in order to short-circuit
			for ( int i = 0; i < Triangles.Length; i++ )
			{
				StringBuilder sb = new StringBuilder();
				Triangles[i].Verts[0].CreateRelationships( this, true, true, false, ref sb);
				Triangles[i].Verts[1].CreateRelationships(this, true, true, false, ref sb);
				Triangles[i].Verts[2].CreateRelationships(this, true, true, false, ref sb);

				Triangles[i].Edges[0].CreateRelationships(this);
				Triangles[i].Edges[1].CreateRelationships(this);
				Triangles[i].Edges[2].CreateRelationships(this);
			}

#if UNITY_EDITOR
			if ( !Application.isPlaying )
			{
				ReconstructVisualizationMesh();
			}
#endif

			//Debug.Log($"Finished making list. method time: '{DateTime.Now.Subtract(dt_methodStart)}'");
			DBG_CalculateTriangulation += $"Finished making list. method time: '{DateTime.Now.Subtract(dt_methodStart)}'";

			DBG_CalculateTriangulation += $"End of {nameof(CreateFromTriangulation)}(). Created '{Triangles.Length}' triangles, " +
				$"and '{constructedVertices_unique.Count}' unique vertices for the mesh.\n";

			//Debug.Log(DBG_CalculateTriangulation);
			EditorUtility.SetDirty(this);
		}

		[ContextMenu("z - call CreateFromSceneTriangulation()")]
		public void CreateFromSceneTriangulation()
		{
			NavMeshTriangulation tringltn = NavMesh.CalculateTriangulation(); //Docs: This calculates and returns a "simple triangulation of the current navmesh..."
			if (tringltn.vertices == null || tringltn.vertices.Length <= 0 )
			{
				Debug.LogError($"LNX ERROR! triangulation gathered from the scene had null or 0 vertices collection. " +
					$"Returning early...");
				return;
			}

			CreateFromTriangulation( tringltn );
		}

		public void Refresh( bool meshContinuityHasChanged ) //NEW
		{
			Debug.Log($"Refresh()---------------------------");

			//Debug.Log($"now looping through '{Triangles.Length}' triangles...");

			DateTime dt_start = DateTime.Now;

			CalculateBounds(); //This needs to happen now before the triangles refresh because the creation of the vert relationships relies on CalculatePath(), which relies on knowing the bounds in order to short-circuit

			//int nmbrFnshdLoops = 0;

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				//DateTime dt_loopStart = DateTime.Now;
				Debug.Log($"I: '{i}'...");
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

		[ContextMenu("z call ReconstructVisualizationMesh()")]
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
					//Debug.Log($"");
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

		[ContextMenu("z call ClearAllData()")]
		public void ClearAllData()
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
			string methodReport = $"ContainsDeletion(). checking vert in list starting at the vert at index: " +
				$"'{nmTriangulation.indices[areaIndex * 3]}', position: '{nmTriangulation.vertices[nmTriangulation.indices[areaIndex * 3]]}'...";

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
		public bool PositionIsInShapeProject(Vector3 pos, out LNX_NavmeshHit hit, bool considerPossibilityOfOverlaps = true)
		{
			hit = LNX_NavmeshHit.None;
			float runningClosestDist = float.MaxValue;

			for (int i = 0; i < Triangles.Length; i++)
			{
				float currentDist = float.MaxValue;
				LNX_NavmeshHit crntHit = LNX_NavmeshHit.None;

				if (Triangles[i].IsInShapeProject(pos, out crntHit))
				{
					if (!considerPossibilityOfOverlaps)
					{
						hit = crntHit;
						return true;
					}

					//note: The reason I'm not immediately returning this tri here is because concievably
					// you could have two navmesh polys "on top of each other", (IE: in line with
					// each other's normals), which would result in more than one tri considering
					// this point to be within it's bounds, and you need to decide which one is
					// the better option...
					currentDist = Vector3.Distance(pos, crntHit.Position);
				}

				if (currentDist < runningClosestDist)
				{
					hit = crntHit;
					runningClosestDist = currentDist;
				}
			}

			return hit != LNX_NavmeshHit.None;
			
		}

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
			hit = LNX_NavmeshHit.None;

			#region SHORT-CIRCUITING ===========================================================
			if ( Vector3.Distance(V_BoundsCenter, pos) > (maxDistance + BoundsContainmentDistanceThreshold) )
			{
				return false;
			}
			#endregion

			float runningClosestDist = float.MaxValue;
			int runningBestIndex = -1;
			LNX_NavmeshHit runningBestHit = LNX_NavmeshHit.None;

            for ( int i = 0; i < Triangles.Length; i++ )
            {
				float currentDist = float.MaxValue;
				LNX_NavmeshHit crntHit = LNX_NavmeshHit.None;

				if ( Triangles[i].IsInShapeProject(pos, out crntHit) )
				{
					if( !considerPossibilityOfOverlaps )
					{
						hit = crntHit;
						return true;
					}

					//note: The reason I'm not immediately returning this tri here is because concievably
					// you could have two navmesh polys "on top of each other", (IE: in line with
					// each other's normals), which would result in more than one tri considering
					// this point to be within it's bounds, and you need to decide which one is
					// the better option...
					currentDist = Vector3.Distance( pos, crntHit.Position );
				}
                else
                {
					if ( considerClosestOffPerimeter )
					{
						crntHit = Triangles[i].ClosestHitOnPerimeter( pos );
						currentDist = Vector3.Distance(pos, crntHit.Position );
					}
				}

				if ( currentDist < runningClosestDist )
				{
					runningBestHit = crntHit;
					runningClosestDist = currentDist;
					runningBestIndex = i;
				}
            }

			if( runningBestIndex < 0 )
			{
				return false;
			}

			hit = runningBestHit;

            if( runningClosestDist <= maxDistance )
			{
				return true;
			}
			else
			{
				return false;
			}
        }

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

		#region RAYCASTS ======================================================
		/// <summary>
		/// Attempts to project a line through the surfaces in the scene.<br></br>
		/// NOTE: This returns true/false opposite what the raycast method would.
		/// </summary>
		/// <param name="startHit"></param>
		/// <param name="endHit"></param>
		/// <param name="outPath"></param>
		/// <param name="allowedDistance"></param>
		/// <returns>'True' if the projection completes without hitting any obstructions. 'False' if it hits an obstruction before it's end.</returns>
		public bool TryProjectThrough(LNX_NavmeshHit startHit, LNX_NavmeshHit endHit, out LNX_Path outPath, 
			bool allowRelationships = false )
		{
			#region SHORT-CIRCUITING ==================================================
			if (startHit.TriangleIndex == endHit.TriangleIndex) //If start and end hit are on same triangle...
			{
				outPath = new LNX_Path(GetSurfaceProjectionVector(), startHit, endHit);
				return true;
			}
			if (startHit.Position == endHit.Position)
			{
				outPath = new LNX_Path(GetSurfaceProjectionVector(), endHit);
				return true;
			}
			if
			( 
				startHit.VertIndex > -1 && 
				VertTouchesTriangle(Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].MyCoordinate, endHit.TriangleIndex)
			)
			{
				outPath = new LNX_Path(GetSurfaceProjectionVector(), startHit, endHit);
				return true;
			}
			#endregion

			outPath = new LNX_Path(GetSurfaceProjectionVector(), startHit);

			//todo: instead of using FlatHitPosition(startHit) below, cache this value and efficiency test to see if it's worth it
			// todo: also, a little bit lower, there's a line saying [Vector3 vProject = FlatVector( endHit.Position - startHit.Position ).normalized;],
			// try pre-caching this as well and efficiency testing

			Vector3 vProject_fltnd = FlatVector(endHit.Position - startHit.Position).normalized;

			if (startHit.VertIndex > -1)
			{
				//TODO: could we add another short-circuit here that checks if both the start and end hits are on a vert, and if so, if these verts are shared by a common
				//triangle? It would effectively be similar to the first short-circuit check above in that we would treat the hits as though theyre both on the same tri

				if ( endHit.VertIndex > -1 & allowRelationships )
				{
					if (Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].IsRelationshipCollectionSuperficiallyValid(Triangles.Length))
					{
						LNX_VertexRelationship rel = Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].GetRelationship(
							endHit.TriangleIndex, endHit.VertIndex);

						if (rel != null && rel.AmValid)
						{
							outPath = new LNX_Path(rel.PathTo); //IMPORTANT! This needs to be a new (different) object so that the pathpoint list doesn't get inadvertently changed
							return outPath.AmStraight;
						}
					}
				}

				LNX_ComponentCoordinate sweepCoord = Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].GetVertCoord_viaProjectionSweep(
					vProject_fltnd, true);

				if (sweepCoord.TrianglesIndex != startHit.TriangleIndex || sweepCoord.ComponentIndex != startHit.VertIndex)
				{
					if (sweepCoord == LNX_ComponentCoordinate.None)
					{
						if (!VertIsOnTerminalEdge(startHit.TriangleIndex, startHit.VertIndex))
						{
							Debug.LogError($"LNX_ERROR! Raycast startHit: ('{startHit}') was on a non-terminal vert, but couldn't get adjusted vert coord via projection sweep. " +
								$"This shouldn't happen on a non-terminal vert. Maybe the relational/shared-vert information is incorrect or needs to be reloaded. Returning early...");
						}

						outPath = null;
						return false;
					}
					else
					{
						startHit = new LNX_NavmeshHit(
							startHit.Position, GetSurfaceProjectionVector(),
							sweepCoord.TrianglesIndex,
							sweepCoord.ComponentIndex,
							-1
						);
					}
				}
			}
			else if (startHit.EdgeIndex > -1)
			{
				if
				(
					Vector3.Dot
					(
						vProject_fltnd,
						Triangles[startHit.TriangleIndex].Edges[startHit.EdgeIndex].v_Cross_flat
					) < 0f
				) //"if projection points toward 'outside' direction of this edge"...
				{
					if (Triangles[startHit.TriangleIndex].Edges[startHit.EdgeIndex].AmTerminal)
					{
						outPath = null;
						return true;
					}
					else
					{
						LNX_ComponentCoordinate shrdEdgeCoord = Triangles[startHit.TriangleIndex].Edges[startHit.EdgeIndex].SharedEdgeCoordinate;
						startHit = new LNX_NavmeshHit(
							Triangles[shrdEdgeCoord.TrianglesIndex].Edges[shrdEdgeCoord.ComponentIndex],
							startHit.Position,
							GetSurfaceProjectionVector()
						);
					}
				}
			}

			#region PROJECT THROUGH TO END HIT ==================================
			LNX_NavmeshHit currentStartHit = startHit;
			int safetyTimeout = Triangles.Length;
			int runningWhileIterations = 0;

			bool amStillProjecting = true;

			while (amStillProjecting)
			{
				LNX_NavmeshHit triPerimHit = LNX_NavmeshHit.None;

				if 
				(
					!Triangles[currentStartHit.TriangleIndex].ProjectThroughToPerimeter(
					currentStartHit, endHit, out triPerimHit, true)
				)
				{
					return false;
				}

				if (triPerimHit.TriangleIndex == outPath.PathPoints[outPath.PathPoints.Count - 1].TriangleIndex)
				{
					outPath.AddPoint(triPerimHit);

					return true;
				}

				if
				(
					triPerimHit.Position == endHit.Position ||
					Vector3.Distance(triPerimHit.Position, endHit.Position) < 0.001f
				)
				{
					outPath.AddPoint(endHit);
					return false;
				}
				else
				{
					outPath.AddPoint(triPerimHit);
				}

				if (HitIsOnTriPerimeter_extrapolated(triPerimHit, Triangles[endHit.TriangleIndex]))
				{
					if (endHit.Position != triPerimHit.Position) //In case the end position is actually on the perimeter of the destination tri...
					{
						outPath.AddPoint(endHit);
					}

					return false;
				}

				currentStartHit = triPerimHit;

				runningWhileIterations++;
				if (runningWhileIterations > safetyTimeout)
				{
					Debug.LogError($"Raycast('{startHit}', '{endHit}') while loop went for more than '{safetyTimeout}' iterations. Breaking early...");
					amStillProjecting = false;
					return true;
				}
			}
			#endregion

			return false;

		}

		public bool Raycast(LNX_NavmeshHit startHit, Vector3 projectDir, out LNX_Path outPath, float castDistance,
			bool allowRelationships = false)
		{
			#region SHORT-CIRCUITING ==================================================
			if (projectDir == Vector3.zero)
			{
				Debug.LogWarning($"LNX WARNING! projectDir was passed into method as Vector3.zero. Was this intentional? Returning early.");

				outPath = null;
				return false;
			}
			if (castDistance == 0f)
			{
				Debug.LogWarning($"LNX WARNING! allowedDistance was passed into method as 0. Was this intentional? Returning early.");
				outPath = null;
				return false;
			}
			#endregion

			outPath = new LNX_Path(GetSurfaceProjectionVector(), startHit);

			//todo: instead of using FlatHitPosition(startHit) below, cache this value and efficiency test to see if it's worth it
			// todo: also, a little bit lower, there's a line saying [Vector3 vProject = FlatVector( endHit.Position - startHit.Position ).normalized;],
			// try pre-caching this as well and efficiency testing

			Vector3 vProject_fltnd = FlatVector(projectDir).normalized;

			#region CHECK IF START HIT NEEDS TO BE ADJUSTED, OR IS CAUSE TO SHORT-CIRCUIT ========================
			if (startHit.VertIndex > -1)
			{
				LNX_ComponentCoordinate sweepCoord = Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].GetVertCoord_viaProjectionSweep(
					vProject_fltnd, true);

				if (sweepCoord == LNX_ComponentCoordinate.None)
				{
					if (!VertIsOnTerminalEdge(startHit.TriangleIndex, startHit.VertIndex))
					{
						Debug.LogError($"LNX_ERROR! Raycast startHit: ('{startHit}') was on a non-terminal vert, but couldn't get adjusted " +
							$"vert coord via projection sweep. This shouldn't happen on a non-terminal vert. Maybe the " +
							$"relational/shared-vert information is incorrect or needs to be reloaded. Returning early...");
					}

					outPath = null;
					return false;
				}
				else if (sweepCoord.TrianglesIndex != startHit.TriangleIndex || sweepCoord.ComponentIndex != startHit.VertIndex)
				{
					startHit = new LNX_NavmeshHit(
						startHit.Position, GetSurfaceProjectionVector(),
						sweepCoord.TrianglesIndex,
						sweepCoord.ComponentIndex,
						-1
					);
				}
			}
			else if (startHit.EdgeIndex > -1)
			{
				if
				(
					Vector3.Dot
					(
						vProject_fltnd,
						Triangles[startHit.TriangleIndex].Edges[startHit.EdgeIndex].v_Cross_flat.normalized
					) < 0f
				) //"if projection points toward 'outside' direction of this edge"...
				{
					if (Triangles[startHit.TriangleIndex].Edges[startHit.EdgeIndex].AmTerminal)
					{
						outPath = null;
						return true;
					}
					else
					{
						LNX_ComponentCoordinate shrdEdgeCoord = Triangles[startHit.TriangleIndex].Edges[startHit.EdgeIndex].SharedEdgeCoordinate;
						startHit = new LNX_NavmeshHit(
							Triangles[shrdEdgeCoord.TrianglesIndex].Edges[shrdEdgeCoord.ComponentIndex],
							startHit.Position,
							GetSurfaceProjectionVector()
						);
					}
				}
			}
			#endregion

			#region PROJECT THROUGH TO END HIT ==================================
			LNX_NavmeshHit currentStartHit = startHit;
			int safetyTimeout = Triangles.Length;
			int runningWhileIterations = 0;

			bool amStillProjecting = true;

			while (amStillProjecting)
			{
				LNX_NavmeshHit triPerimHit = LNX_NavmeshHit.None;

				if
				(
					!Triangles[currentStartHit.TriangleIndex].ProjectThroughToPerimeter(
					currentStartHit, projectDir, out triPerimHit, true)
				)
				{
					Debug.LogError($"something went wrong with tri{currentStartHit.TriangleIndex}." +
						$"ProjectThroughToPerimeter(" +
						$"'{LNX_UnitTestUtilities.LongVectorString(currentStartHit.Position)}', projectDir: '{projectDir}'). " +
						$"It returned false, which usually shouldn't happen...");
					return false;
				}

				float lastDist = Vector3.Distance(triPerimHit.Position, currentStartHit.Position);
				float wouldBeDist = outPath.TotalDistance + lastDist;

				#region CHECK FOR PROBLEM IN CASE OF HIT LANDING ON SAME TRIANGLE AS LAST ===============================
				if (triPerimHit.TriangleIndex == currentStartHit.TriangleIndex) //this means we haven't moved to a new triangle. Maybe we've "doubled back"
				{
					if
					(
						(
							triPerimHit.EdgeIndex != -1 && 
							!Triangles[triPerimHit.TriangleIndex].Edges[triPerimHit.EdgeIndex].AmTerminal
						) ||
						(
							triPerimHit.VertIndex > -1 &&
							Triangles[triPerimHit.TriangleIndex].Verts[triPerimHit.VertIndex].GetVertCoord_viaProjectionSweep(
							projectDir, true) == LNX_ComponentCoordinate.None
						)
					)
					{
						Debug.LogError($"LNX ERROR! Triangle.ProjectThroughToPerimeter returned hit: '{triPerimHit}' on same tri " +
							$"as last one: '{currentStartHit}', but doesn't appear to be on a terminal edge/vert. This is NOT " +
							$"supposed to happen.");
						return false;
					}
				}
				#endregion

				#region HANDLE DISTANCE EXCEEDED ================================================================
				if (wouldBeDist > castDistance)
				{
					int edgIndx = -1;
					if (triPerimHit.VertIndex > -1 && currentStartHit.VertIndex > -1)
					{
						//rprt.Log($"start and end are on verts. This means projection lies on edge parallel. Need to figure out which edge...");
						// In this case, the result should be on an edge...
						/*
						if
						(
							projectDir.normalized ==
							Triangles[currentStartHit.TriangleIndex].Verts[currentStartHit.VertIndex].V_ToFirstSiblingVert.normalized
						)
						{
							//todo: this block
						}
						*/
					}

					triPerimHit = new LNX_NavmeshHit(
						outPath.PathPoints[outPath.PathPoints.Count - 1].Position + projectDir.normalized * (castDistance - outPath.TotalDistance),
						triPerimHit.Normal,
						currentStartHit.TriangleIndex,
						0,
						edgIndx
					);

					outPath.AddPoint(triPerimHit);
					return false;
				}
				#endregion

				outPath.AddPoint(triPerimHit);

				if (triPerimHit.TriangleIndex == currentStartHit.TriangleIndex) //can assume NOT terminal because of earlier check
				{
					outPath.AddPoint(triPerimHit);
					return true;
				}

				if (outPath.TotalDistance == castDistance)
				{
					return false;
				}

				currentStartHit = triPerimHit;

				runningWhileIterations++;
				if (runningWhileIterations > safetyTimeout)
				{
					amStillProjecting = false;
					return true;
				}
			}
			#endregion

			return false;

		}
		public bool Raycast_dbg(LNX_NavmeshHit startHit, Vector3 projectDir, out LNX_Path outPath, float castDistance, 
			ref LNX_MethodDebugReport rprt, bool allowRelationships = false)
		{
			rprt.StartMethod($"Raycast_dbg('{startHit}', projectDir: '{projectDir}', castDistance: '{castDistance}')");

			#region SHORT-CIRCUITING ==================================================
			if (projectDir == Vector3.zero)
			{
				Debug.LogWarning($"LNX WARNING! projectDir was passed into method as Vector3.zero. Was this intentional? Returning early.");

				outPath = null;
				return false;
			}
			if (castDistance == 0f)
			{
				Debug.LogWarning($"LNX WARNING! allowedDistance was passed into method as 0. Was this intentional? Returning early.");
				outPath = null;
				return false;
			}
			#endregion

			rprt.Log($"went past short-circuiting...");
			outPath = new LNX_Path(GetSurfaceProjectionVector(), startHit);

			//todo: instead of using FlatHitPosition(startHit) below, cache this value and efficiency test to see if it's worth it
			// todo: also, a little bit lower, there's a line saying [Vector3 vProject = FlatVector( endHit.Position - startHit.Position ).normalized;],
			// try pre-caching this as well and efficiency testing

			Vector3 vProject_fltnd = FlatVector(projectDir).normalized;

			#region CHECK IF START HIT NEEDS TO BE ADJUSTED, OR IS CAUSE TO SHORT-CIRCUIT ========================
			rprt.Log($"Now checking if startHit needs to be adjusted, or is cause to short-circuit..");
			if (startHit.VertIndex > -1)
			{
				LNX_ComponentCoordinate sweepCoord = Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].GetVertCoord_viaProjectionSweep(
					vProject_fltnd, true);

				rprt.Log($"startHit is on vert. Inspecting gathered sweepcoord: '{sweepCoord}'...");

				if ( sweepCoord == LNX_ComponentCoordinate.None )
				{
					rprt.Log($"sweep coord is None. Making sure hit vert is terminal...");

					if (!VertIsOnTerminalEdge(startHit.TriangleIndex, startHit.VertIndex))
					{
						rprt.Log($"LNX_ERROR! Raycast startHit: ('{startHit}') was on a non-terminal vert, but couldn't get adjusted " +
							$"vert coord via projection sweep. This shouldn't happen on a non-terminal vert. Maybe the " +
							$"relational/shared-vert information is incorrect or needs to be reloaded. Returning early...");

						Debug.LogError($"LNX_ERROR! Raycast startHit: ('{startHit}') was on a non-terminal vert, but couldn't get adjusted " +
							$"vert coord via projection sweep. This shouldn't happen on a non-terminal vert. Maybe the " +
							$"relational/shared-vert information is incorrect or needs to be reloaded. Returning early...");
					}

					rprt.Log_And_End_Method($"hit vert is terminal. Returning false and null path...");

					outPath = null;
					return false;
				}
				else if (sweepCoord.TrianglesIndex != startHit.TriangleIndex || sweepCoord.ComponentIndex != startHit.VertIndex)
				{
					rprt.Log($"sweep coord is different from starthit coordinates. Adjusting...");
					startHit = new LNX_NavmeshHit(
						startHit.Position, GetSurfaceProjectionVector(),
						sweepCoord.TrianglesIndex,
						sweepCoord.ComponentIndex,
						-1
					);
					rprt.Log($"adjusted startHit to: '{startHit}'...");
				}
			}
			else if (startHit.EdgeIndex > -1)
			{
				rprt.Log($"startHit is on edge. Inspecting edge to see if hit needs to be adjusted...");

				if
				(
					Vector3.Dot
					(
						vProject_fltnd,
						Triangles[startHit.TriangleIndex].Edges[startHit.EdgeIndex].v_Cross_flat.normalized
					) < 0f
				) //"if projection points toward 'outside' direction of this edge"...
				{
					rprt.Log($"found that the projection points towards outside of hit edge based on vcross: " +
						$"'{Triangles[startHit.TriangleIndex].Edges[startHit.EdgeIndex].v_Cross_flat.normalized}'...");

					if (Triangles[startHit.TriangleIndex].Edges[startHit.EdgeIndex].AmTerminal)
					{
						rprt.Log_And_End_Method($"found that edge is terminal. Assuming this method should end here. Returning true and null path");
						outPath = null;
						return true;
					}
					else
					{
						rprt.Log($"found that edge is NOT terminal. adjusting startHit...");
						LNX_ComponentCoordinate shrdEdgeCoord = Triangles[startHit.TriangleIndex].Edges[startHit.EdgeIndex].SharedEdgeCoordinate;
						startHit = new LNX_NavmeshHit(
							Triangles[shrdEdgeCoord.TrianglesIndex].Edges[shrdEdgeCoord.ComponentIndex],
							startHit.Position,
							GetSurfaceProjectionVector()
						);
						rprt.Log($"adjusted startHit to: '{startHit}'...");
					}
				}
			}
			#endregion

			rprt.EmptyLine();
			rprt.Log($"now projecting through...");
			#region PROJECT THROUGH TO END HIT ==================================
			LNX_NavmeshHit currentStartHit = startHit;
			int safetyTimeout = Triangles.Length;
			int runningWhileIterations = 0;

			bool amStillProjecting = true;

			while (amStillProjecting)
			{
				rprt.Log($"while{runningWhileIterations}...");

				LNX_NavmeshHit triPerimHit = LNX_NavmeshHit.None;

				rprt.Log($"projecting through to perimeter...");
				if
				(
					!Triangles[currentStartHit.TriangleIndex].ProjectThroughToPerimeter_dbg(
					currentStartHit, projectDir, out triPerimHit, ref rprt, true)
				)
				{
					rprt.Log_And_End_Method($"something went wrong with tri{currentStartHit.TriangleIndex}." +
						$"ProjectThroughToPerimeter('{currentStartHit}', projectDir: '{projectDir}'). " +
						$"It returned false, which usually shouldn't happen...");
					Debug.LogError($"something went wrong with tri{currentStartHit.TriangleIndex}." +
						$"ProjectThroughToPerimeter(" +
						$"'{LNX_UnitTestUtilities.LongVectorString(currentStartHit.Position)}', projectDir: '{projectDir}'). " +
						$"It returned false, which usually shouldn't happen...");
					return false;
				}
				rprt.Log($"got triPerimHit: '{triPerimHit}'...");

				float lastDist = Vector3.Distance(triPerimHit.Position, currentStartHit.Position);
				float wouldBeDist = outPath.TotalDistance + lastDist;

				#region CHECK FOR PROBLEM IN CASE OF HIT LANDING ON SAME TRIANGLE AS LAST ===============================
				rprt.Log($"chekcing if hit has a coordinate issue...");

				if (triPerimHit.TriangleIndex == currentStartHit.TriangleIndex) //this means we haven't moved to a new triangle. Maybe we've "doubled back"
				{
					rprt.Log($"new hit tri same as last...");
					if
					(
						(
							triPerimHit.EdgeIndex != -1 &&
							!Triangles[triPerimHit.TriangleIndex].Edges[triPerimHit.EdgeIndex].AmTerminal
						) ||
						(
							triPerimHit.VertIndex > -1 &&
							Triangles[triPerimHit.TriangleIndex].Verts[triPerimHit.VertIndex].GetVertCoord_viaProjectionSweep(
							projectDir, true) == LNX_ComponentCoordinate.None
						)
					)
					{
						rprt.Log_And_End_Method($"LNX ERROR! Triangle.ProjectThroughToPerimeter returned hit: '{triPerimHit}' on same tri " +
							$"as last one: '{currentStartHit}', but doesn't appear to be on a terminal edge/vert. This is NOT " +
							$"supposed to happen.");
						Debug.LogError($"LNX ERROR! Triangle.ProjectThroughToPerimeter returned hit: '{triPerimHit}' on same tri " +
							$"as last one: '{currentStartHit}', but doesn't appear to be on a terminal edge/vert. This is NOT " +
							$"supposed to happen.");
						return false;
					}
				}
				#endregion
				rprt.Log($"apparently no coordinate issue. Proceeding...");

				#region HANDLE DISTANCE EXCEEDED ================================================================
				rprt.Log($"checking dist. wouldBeDist: '{wouldBeDist}', castDistance: '{castDistance}'..");

				if (wouldBeDist > castDistance)
				{
					rprt.Log($"decided distance was exceeded.");

					int edgIndx = -1;
					if (triPerimHit.VertIndex > -1 && currentStartHit.VertIndex > -1)
					{
						rprt.Log($"start and end are on verts. This means projection lies on edge parallel. Need to figure out which edge...");
						// In this case, the result should be on an edge...
						if
						(
							projectDir.normalized ==
							Triangles[currentStartHit.TriangleIndex].Verts[currentStartHit.VertIndex].V_ToFirstSiblingVert.normalized
						)
						{
							rprt.Log($"found that projection runs along ");
						}
					}

					triPerimHit = new LNX_NavmeshHit(
						outPath.PathPoints[outPath.PathPoints.Count - 1].Position + projectDir.normalized * (castDistance - outPath.TotalDistance),
						triPerimHit.Normal,
						currentStartHit.TriangleIndex,
						0,
						edgIndx
					);
					rprt.Log_And_End_Method($"adjusted hit to: '{triPerimHit}'. Adding point and returning false...");

					outPath.AddPoint(triPerimHit);
					return false;
				}
				#endregion

				outPath.AddPoint(triPerimHit);

				if (triPerimHit.TriangleIndex == currentStartHit.TriangleIndex) //can assume terminal because of earlier check
				{
					rprt.Log_And_End_Method($"deciding hit is terminal. returning true...");
					outPath.AddPoint(triPerimHit);
					return true;
				}

				if (outPath.TotalDistance == castDistance)
				{
					rprt.Log_And_End_Method($"constructed path distance equal to castDistance. Returning false...");
					return false;
				}

				currentStartHit = triPerimHit;

				runningWhileIterations++;
				if (runningWhileIterations > safetyTimeout)
				{
					Debug.LogError($"Raycast('{startHit}', '{castDistance}') while loop went for more than '{safetyTimeout}' iterations. Breaking early...");
					amStillProjecting = false;
					return true;
				}
			}
			#endregion

			return false;

		}


		/// <summary>
		/// Traces a line between two points on a navmesh.
		/// </summary>
		/// <returns>True if the ray is terminated before reaching target position. Otherwise returns false.</returns>
		public bool Raycast(LNX_NavmeshHit startHit, LNX_NavmeshHit endHit, out LNX_Path outPath )
		{
			#region SHORT-CIRCUITING ==================================================
			if (startHit.TriangleIndex == endHit.TriangleIndex) //If start and end hit are on same triangle...
			{
				outPath = new LNX_Path(GetSurfaceProjectionVector(), startHit, endHit);
				return false;
			}

			if (startHit.Position == endHit.Position)
			{
				outPath = new LNX_Path(GetSurfaceProjectionVector(), endHit);
				return false;
			}
			#endregion

			outPath = new LNX_Path(GetSurfaceProjectionVector(), startHit);

			//todo: instead of using FlatHitPosition(startHit) below, cache this value and efficiency test to see if it's worth it
			// todo: also, a little bit lower, there's a line saying [Vector3 vProject = FlatVector( endHit.Position - startHit.Position ).normalized;],
			// try pre-caching this as well and efficiency testing

			Vector3 vProject_fltnd = FlatVector(endHit.Position - startHit.Position).normalized;

			if (startHit.VertIndex > -1)
			{
				//TODO: could we add another short-circuit here that checks if both the start and end hits are on a vert, and if so, if these verts are shared by a common
				//triangle? It would effectively be similar to the first short-circuit check above in that we would treat the hits as though theyre both on the same tri

				if (endHit.VertIndex > -1)
				{
					if (Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].IsRelationshipCollectionSuperficiallyValid(Triangles.Length))
					{
						LNX_VertexRelationship rel = Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].GetRelationship(
							endHit.TriangleIndex, endHit.VertIndex);

						if (rel != null && rel.AmValid)
						{
							outPath = new LNX_Path(rel.PathTo); //IMPORTANT! This needs to be a new (different) object so that the pathpoint list doesn't get inadvertently changed
							return !outPath.AmStraight;
						}
					}
				}

				if
				(
					VertTouchesTriangle(Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].MyCoordinate, endHit.TriangleIndex)
				) //note: this is a pretty rare case, but it does happen. Especially through ping operation
				{
					outPath = new LNX_Path(GetSurfaceProjectionVector(), startHit, endHit);
					return false;
				}

				LNX_ComponentCoordinate sweepCoord = Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].GetVertCoord_viaProjectionSweep(
					vProject_fltnd, true);

				if (sweepCoord.TrianglesIndex != startHit.TriangleIndex || sweepCoord.ComponentIndex != startHit.VertIndex)
				{
					if (sweepCoord == LNX_ComponentCoordinate.None)
					{
						if ( !VertIsOnTerminalEdge(startHit.TriangleIndex, startHit.VertIndex) )
						{
							Debug.LogError($"LNX_ERROR! Raycast startHit: ('{startHit}') was on a non-terminal vert, but couldn't get adjusted vert coord via projection sweep. " +
								$"This shouldn't happen on a non-terminal vert. Maybe the relational information is incorrect or needs to be reloaded. Returning early...");
						}

						outPath = null;
						return true;
					}
					else
					{
						startHit = new LNX_NavmeshHit(
							startHit.Position, GetSurfaceProjectionVector(),
							sweepCoord.TrianglesIndex,
							sweepCoord.ComponentIndex,
							-1
						);
					}
				}
			}
			else if (startHit.EdgeIndex > -1)
			{
				if
				(
					Vector3.Dot
					(
						vProject_fltnd,
						Triangles[startHit.TriangleIndex].Edges[startHit.EdgeIndex].v_Cross_flat.normalized
					) < 0f
				) //projection points toward "outside" direction of this edge...
				{
					if (Triangles[startHit.TriangleIndex].Edges[startHit.EdgeIndex].AmTerminal)
					{
						outPath = null;
						return true;
					}
					else
					{
						LNX_ComponentCoordinate shrdEdgeCoord = Triangles[startHit.TriangleIndex].Edges[startHit.EdgeIndex].SharedEdgeCoordinate;
						startHit = new LNX_NavmeshHit(
							Triangles[shrdEdgeCoord.TrianglesIndex].Edges[shrdEdgeCoord.ComponentIndex],
							startHit.Position,
							GetSurfaceProjectionVector()
						);
					}
				}
			}

			#region PROJECT THROUGH TO END HIT ==================================
			LNX_NavmeshHit currentStartHit = startHit;
			int safetyTimeout = Triangles.Length;
			int runningWhileIterations = 0;

			bool amStillProjecting = true;

			while (amStillProjecting)
			{
				LNX_NavmeshHit triPerimHit = LNX_NavmeshHit.None;

				if (
					!Triangles[currentStartHit.TriangleIndex].ProjectThroughToPerimeter(
					currentStartHit, endHit, out triPerimHit, true)
				)
				{
					return true;
				}

				if (triPerimHit.TriangleIndex == outPath.PathPoints[outPath.PathPoints.Count - 1].TriangleIndex)
				{
					outPath.AddPoint(triPerimHit);

					return true;
				}

				if
				(
					triPerimHit.Position == endHit.Position ||
					Vector3.Distance(triPerimHit.Position, endHit.Position) < 0.001f
				)
				{
					outPath.AddPoint(endHit);
					return false;
				}
				else
				{
					outPath.AddPoint(triPerimHit);
				}

				if (HitIsOnTriPerimeter_extrapolated(triPerimHit, Triangles[endHit.TriangleIndex]))
				{
					if (endHit.Position != triPerimHit.Position) //In case the end position is actually on the perimeter of the destination tri...
					{
						outPath.AddPoint(endHit);
					}

					return false;
				}

				currentStartHit = triPerimHit;

				runningWhileIterations++;
				if (runningWhileIterations > safetyTimeout)
				{
					Debug.LogError($"Raycast('{startHit}', '{endHit}') while loop went for more than '{safetyTimeout}' iterations. Breaking early...");
					amStillProjecting = false;
					return true;
				}
			}
			#endregion

			return true;
		}
		public bool Raycast_dbg(LNX_NavmeshHit startHit, LNX_NavmeshHit endHit, out LNX_Path outPath, ref LNX_MethodDebugReport rprt) 
		{
			rprt.StartMethod($"Raycast_dbg(startHit: '{startHit}', endHit: '{endHit}')");

			#region SHORT-CIRCUITING ==================================================
			if (startHit.TriangleIndex == endHit.TriangleIndex) //If start and end hit are on same triangle...
			{
				outPath = new LNX_Path(GetSurfaceProjectionVector(), startHit, endHit);
				rprt.Log_And_End_Method("startHit and endHit on same tri index. Short-circuiting early...");
				return false;
			}

			if( startHit.Position == endHit.Position )
			{
				outPath = new LNX_Path(GetSurfaceProjectionVector(), endHit);
				rprt.Log_And_End_Method("start and end hit are in same  position. Returning already-calculated relational paths...");
				return false;
			}
			#endregion

			outPath = new LNX_Path(GetSurfaceProjectionVector(), startHit);

			//todo: instead of using FlatHitPosition(startHit) below, cache this value and efficiency test to see if it's worth it
			// todo: also, a little bit lower, there's a line saying [Vector3 vProject = FlatVector( endHit.Position - startHit.Position ).normalized;],
			// try pre-caching this as well and efficiency testing

			rprt.Log($"no short-circuit. Proceding...");

			Vector3 vProject_fltnd = FlatVector(endHit.Position - startHit.Position).normalized;

			if ( startHit.VertIndex > -1 )
			{
				//TODO: could we add another short-circuit here that checks if both the start and end hits are on a vert, and if so, if these verts are shared by a common
				//triangle? It would effectively be similar to the first short-circuit check above in that we would treat the hits as though theyre both on the same tri
				rprt.Log($"start hit lies on vert: '{startHit.VertIndex}'...", 
					"Checking if start vert touches end tri...");

				if( endHit.VertIndex > -1 )
				{
					rprt.Log($"Endhit is also on vertex. Investigating if relational short-circuiting can be used...");
					if (Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].IsRelationshipCollectionSuperficiallyValid(Triangles.Length) )
					{
						rprt.Log($"Relationship collection IS superficially valid. Proceeding with relational check...");
						LNX_VertexRelationship rel = Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].GetRelationship(
							endHit.TriangleIndex, endHit.VertIndex);
						rprt.Log($"Got existing relationship: '{rel}'...");

						if ( rel != null && rel.AmValid )
						{
							outPath = new LNX_Path(rel.PathTo); //IMPORTANT! This needs to be a new (different) object so that the pathpoint list doesn't get inadvertently changed

							rprt.Log($"existing relationship IS valid. used it's path: '{outPath}'. pt count: '{outPath.PointCount}'");

							rprt.Log_And_End_Method($"path.AmStraight: '{outPath.AmStraight}'",
								$". Now returning '{!outPath.AmStraight}'...");
							return !outPath.AmStraight;
						}
					}
					else
					{
						rprt.Log($"relationship colleciton is NOT valid. Cannot use relational information...");
					}
				}

				if 
				( 
					VertTouchesTriangle(Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].MyCoordinate, endHit.TriangleIndex)
				) //note: this is a pretty rare case, but it does happen. Especially through ping operation
				{
					rprt.Log($"start vert DOES indeed lie on endtri. Path end point can be assumed...");
					outPath = new LNX_Path(GetSurfaceProjectionVector(), startHit, endHit);
					return false;
				}

				rprt.Log($"start hit lies on vert: '{startHit.VertIndex}'. Checking if hit needs to be adjusted based on start to end projection...");

				LNX_ComponentCoordinate sweepCoord = Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].GetVertCoord_viaProjectionSweep_dbg( 
					vProject_fltnd, true, ref rprt );

				if( sweepCoord.TrianglesIndex == startHit.TriangleIndex && sweepCoord.ComponentIndex == startHit.VertIndex )
				{
					rprt.Log($"sweep decided that projection WAS already on the correct vert...");
				}
				else
				{
					rprt.Log($"Sweep decided it needed to adjust startHIt. Checking which vert the starthit should be adjusted to...");

					if (sweepCoord == LNX_ComponentCoordinate.None)
					{
						rprt.Log($"Got 'None' relationship...");
						if( VertIsOnTerminalEdge(startHit.TriangleIndex, startHit.VertIndex))
						{
							rprt.Log($"This vert is on a terminal edge. Assuming raycast is projected toward outside into terminal space. Returning true...");
						}
						else
						{
							Debug.LogError($"LNX_ERROR! Raycast startHit: ('{startHit}') was on a non-terminal vert, but couldn't get adjusted vert coord via projection sweep. " +
								$"This shouldn't happen on a non-terminal vert. Maybe the relational information is incorrect or needs to be reloaded. Returning early...");
							rprt.Log_And_End_Method($"Problem! Got none relationship. Returning true...");
						}

						outPath = null;
						return true;
					}
					else
					{
						rprt.Log($"got rel: '{sweepCoord}' from projectionsweep...");

						startHit = new LNX_NavmeshHit(
							startHit.Position, GetSurfaceProjectionVector(),
							sweepCoord.TrianglesIndex,
							sweepCoord.ComponentIndex,
							-1
						);

						rprt.Log($"adjusted starthit to: '{startHit}'...");
					}
				}
			}
			else if( startHit.EdgeIndex > -1 )
			{
				rprt.Log($"startHit was on an edge. Now making sure it's the correct edge...");
				if 
				(
					Vector3.Dot
					(
						vProject_fltnd, 
						Triangles[startHit.TriangleIndex].Edges[startHit.EdgeIndex].v_Cross_flat.normalized
					) < 0f
				) //projection points toward "outside" direction of this edge...
				{
					rprt.Log($"found that projection points in 'outside' direction of this edge...", 
						"this will need further investigation...");
					if (Triangles[startHit.TriangleIndex].Edges[startHit.EdgeIndex].AmTerminal )
					{
						outPath = null;
						rprt.Log_And_End_Method($"this edge is terminal. Made outpath: '{outPath}', and returning true here...");
						return true;
					}
					else
					{
						rprt.Log($"this edge is NOT terminal. Switching startHit to be on adjacent edge...");
						LNX_ComponentCoordinate shrdEdgeCoord = Triangles[startHit.TriangleIndex].Edges[startHit.EdgeIndex].SharedEdgeCoordinate;
						startHit = new LNX_NavmeshHit(
							Triangles[shrdEdgeCoord.TrianglesIndex].Edges[shrdEdgeCoord.ComponentIndex],
							startHit.Position,
							GetSurfaceProjectionVector()
						);
						rprt.Log($"generated new startHit: '{startHit}'...");
					}
				}
			}

			#region PROJECT THROUGH TO END HIT ==================================
			rprt.Log($"initialized path and added startHIt: '{startHit}'. Path pt count: '{outPath.PointCount}'");

			LNX_NavmeshHit currentStartHit = startHit;
			int safetyTimeout = Triangles.Length;
			int runningWhileIterations = 0;

			bool amStillProjecting = true;

			rprt.Log($"Now trying to project through to end hit...");
			while ( amStillProjecting )
			{
				rprt.Log("=========================================================================");
				rprt.Log($"while{runningWhileIterations}...");
				LNX_NavmeshHit triPerimHit = LNX_NavmeshHit.None;

				if (
					!Triangles[currentStartHit.TriangleIndex].ProjectThroughToPerimeter_dbg(
					currentStartHit, endHit, out triPerimHit, ref rprt, true)
				)
				{
					rprt.Log($"LNX_Triangle.ProjectThroughToPerimeter() was unsuccesful. This means the chain has failed. Returning early...");
					rprt.EndMethod("Raycast_dbg()");
					return true;
				}
				rprt.Log($"LNX_Triangle.ProjectThroughToPerimeter() got perimeter hit: '{triPerimHit}'...",
					$"Inspecting perimHit tri index against last logged tri index: '{outPath.EndTriIndex}'..."
				);

				if( triPerimHit.TriangleIndex == outPath.PathPoints[outPath.PathPoints.Count-1].TriangleIndex )
				{
					rprt.Log($"tri perimeter hit index: '{triPerimHit.TriangleIndex}' is the same as previously logged path index. " +
						$"Need to check if there's a problem...");

					if( DirectionIsTerminalFromHit_extrapolated(vProject_fltnd, triPerimHit) )
					{
						rprt.Log_And_End_Method($"found that this hit/projection combination is directionally-terminal. " +
							$"It seems we've hit a wall. Returning true...");
					}
					else
					{
						rprt.Log_And_End_Method($"tri perimeter hit does NOT seem to be directionally terminal, which is unexpected. There seems to be a problem. Returning true early...");
						Debug.Log($"raycast appears to have 'doubled back'. Returning early...");
					}

					outPath.AddPoint( triPerimHit );

					return true;
				}

				rprt.Log($"LNX_Triangle.ProjectThroughToPerimeter() WAS succesful...");
				
				if
				( 
					triPerimHit.Position == endHit.Position ||
					Vector3.Distance(triPerimHit.Position, endHit.Position) < 0.001f
				)
				{
					rprt.Log($"triperimhit position is same as endhit position.");
					outPath.AddPoint( endHit );
					rprt.Log_And_End_Method($"added endhit to outPath. Returning false now...");
					return false;
				}
				else
				{
					rprt.Log($"Adding the perimeter hit: '{triPerimHit}' to outPath...");
					outPath.AddPoint(triPerimHit);
				}
				
				if ( HitIsOnTriPerimeter_extrapolated_dbg(triPerimHit, Triangles[endHit.TriangleIndex], ref rprt) )
				{
					rprt.Log($"found that triPerimHit was on same triangle as endHIt.",
						"This means the while-loop should end here.");
					if (endHit.Position != triPerimHit.Position) //In case the end position is actually on the perimeter of the destination tri...
					{
						rprt.Log($"endhit position and triperimhit position NOT the same. Adding endHit: '{endHit}' to path...");
						outPath.AddPoint(endHit);
					}

					rprt.Log($"Now path has: '{outPath.PathPoints.Count}' points. Returning false...");

					rprt.EndMethod("Raycast_dbg()");
					return false;
				}

				currentStartHit = triPerimHit;

				runningWhileIterations++;
				if (runningWhileIterations > safetyTimeout)
				{
					Debug.LogError($"Raycast('{startHit}', '{endHit}') while loop went for more than '{safetyTimeout}' iterations. Breaking early...");
					amStillProjecting = false;
					rprt.Log($"while loop went for more than '{safetyTimeout}' iterations. Breaking early...");
					rprt.EndMethod("Raycast_dbg()");
					return true;
				}
			}
			#endregion

			rprt.Log($"after while loop. Apparently Projecting through to perimeter didn't work. Returning true as default...");
			rprt.EndMethod("Raycast_dbg()");

			return true;
		}

		/// <summary>
		/// Traces a line between two points on a navmesh.
		/// </summary>
		/// <returns>True if the ray is terminated before reaching target position. Otherwise returns false.</returns>
		public bool Raycast(Vector3 sourcePosition, Vector3 targetPosition, float maxSampleDistance, out LNX_Path outPath,
			bool considerOffPerimeter = false) //todo: Unit test!!!
		{
			outPath = null;

			LNX_NavmeshHit lnxStartHit = LNX_NavmeshHit.None;
			LNX_NavmeshHit lnxEndHit = LNX_NavmeshHit.None;

			#region SHORT-CIRCUITING ==================================================
			if (!SamplePosition(sourcePosition, out lnxStartHit, maxSampleDistance, considerOffPerimeter))
			{
				return true;
			}

			if (!SamplePosition(targetPosition, out lnxEndHit, maxSampleDistance, considerOffPerimeter))
			{
				return true;
			}
			#endregion

			return Raycast( lnxStartHit, lnxEndHit, out outPath );
		}

		public bool Raycast_dbg(Vector3 sourcePosition, Vector3 targetPosition, float maxSampleDistance, out LNX_Path outPath, 
			ref LNX_MethodDebugReport rprt, bool considerOffPerimeter = false)
		{
			//rprt.Log($"tablvl: '{rprt.MethodLvl}'");
			rprt.StartMethod($"Raycast_dbg(sourcePosition: '{sourcePosition}', targetPosition: '{targetPosition}')");

			outPath = null;

			LNX_NavmeshHit lnxStartHit = LNX_NavmeshHit.None;
			LNX_NavmeshHit lnxEndHit = LNX_NavmeshHit.None;

			rprt.Log($"first, attempting to sample source position...");

			#region SHORT-CIRCUITING ==================================================
			if (!SamplePosition(sourcePosition, out lnxStartHit, maxSampleDistance, considerOffPerimeter))
			{
				rprt.Log_And_End_Method($"Could NOT sample sourcePosition. Returning early...");

				return true;
			}
			else
			{
				rprt.Log($"succesfully sampled sourcePosition at: '{lnxStartHit}'...");
			}

			rprt.Log($"now, attempting to sample target position...");

			if ( !SamplePosition(targetPosition, out lnxEndHit, maxSampleDistance, considerOffPerimeter) )
			{
				rprt.Log_And_End_Method($"Could NOT sample targetPosition. Returning early...");

				return true;
			}
			else
			{
				rprt.Log($"succesfully sampled targetPosition at: '{lnxEndHit}'...");
			}

			#endregion

			rprt.Log($"no short circuits. Now passing off to deeper overload...");
			bool rslt = Raycast_dbg( lnxStartHit, lnxEndHit, out outPath, ref rprt );

			//rprt.Log($"tablvl: '{rprt.MethodLvl}'");
			rprt.EndMethod("Raycast_dbg()");
			//rprt.Log($"tablvl: '{rprt.MethodLvl}'");

			return rslt;
		}

		#endregion

		#region CALCULATEPATHS ===================================================
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
			out LNX_Path outPath)
		{
			LNX_Path rcPath = new LNX_Path();
			bool rcHitSomething = Raycast(startHit, endHit, out rcPath);

			//rprt.Log($"end of initial raycast...");
			if (!rcHitSomething)
			{
				outPath = new LNX_Path(rcPath);
				return true;
			}
			else
			{
				outPath = null; //needs to be done bc of the raycast above, which will give this a junk value if the method gets this far....

				#region GET VISIBLE VERTS/PATHS =========================================================
				List<LNX_Path> visblVrtPths = new List<LNX_Path>();

				if (startHit.VertIndex != -1)
				{
					visblVrtPths = GetVisibleVertsFromVert(
						Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex], false);
				}
				else
				{
					visblVrtPths = GetVisibleVertsFromHit(startHit, false);
				}

				if (visblVrtPths == null || visblVrtPths.Count <= 0)
				{
					return false;
				}
				#endregion

				#region ASSEMBLE BACKSTOP VERTS ====================================
				List<LNX_ComponentCoordinate> vsblBckstpVerts = new List<LNX_ComponentCoordinate>();
				if (startHit.VertIndex > -1)
				{
					vsblBckstpVerts.Add(startHit.AsVertCoordinate());
				}
				for (int i = 0; i < visblVrtPths.Count; i++) //Note: this must come before the check for shared vert space...
				{
					vsblBckstpVerts.Add(visblVrtPths[i].EndCoordinate_vert);
				}
				#endregion

				#region CHECK FOR BEST START PING VERT =======================================================
				int indx_runningBestPath = -1;

				bool foundRelPrblm = false;
				float runningClosestDistance = -1f;

				for (int i_visblVrtPths = 0; i_visblVrtPths < visblVrtPths.Count; i_visblVrtPths++)
				{
					if (!Triangles[startHit.TriangleIndex].IsTriangleCompletelyRelationallyValid(endHit.TriangleIndex))
					{
						foundRelPrblm = true;
						break;
					}

					for (int i_vrts = 0; i_vrts < 3; i_vrts++)
					{
						float dist = visblVrtPths[i_visblVrtPths].TotalDistance +
						Triangles[visblVrtPths[i_visblVrtPths].EndTriIndex].Verts[visblVrtPths[i_visblVrtPths].EndHit.VertIndex].
						GetRelationship(endHit.TriangleIndex, i_vrts).PathDistance +
						Vector3.Distance(Triangles[endHit.TriangleIndex].Verts[i_vrts].V_Position, endHit.Position);

						if (runningClosestDistance == -1 || dist < runningClosestDistance)
						{
							runningClosestDistance = dist;
							indx_runningBestPath = i_visblVrtPths;
						}
					}
				}

				if (foundRelPrblm)
				{
					int runningBestAdjacency = -1;
					List<int> uniqueTris = new List<int>();
					for (int i_visblVrtPths = 0; i_visblVrtPths < visblVrtPths.Count; i_visblVrtPths++)
					{
						if (uniqueTris.Contains(visblVrtPths[i_visblVrtPths].EndTriIndex))
						{
							continue;
						}

						uniqueTris.Add(visblVrtPths[i_visblVrtPths].EndTriIndex);

						int adjcncy = GetAdjacencyDepthToTriangle(visblVrtPths[i_visblVrtPths].EndTriIndex, endHit.TriangleIndex);

						if (adjcncy < 0)
						{
							Debug.LogError($"apparently something went wrong. Continuing...");
							continue;
						}

						if (runningBestAdjacency == -1 || adjcncy < runningBestAdjacency)
						{
							runningBestAdjacency = adjcncy;
							indx_runningBestPath = i_visblVrtPths;
						}

						if (adjcncy == 1)
						{
							break;
						}
					}
				}
				#endregion

				#region CONSTRUCT PATHS -------------------------------------
				LNX_Path[] paths = new LNX_Path[visblVrtPths.Count];

				if (indx_runningBestPath > -1)
				{
					paths[indx_runningBestPath] = Triangles[visblVrtPths[indx_runningBestPath].EndTriIndex].
						Verts[visblVrtPths[indx_runningBestPath].EndHit.VertIndex].Ping(
						endHit, this, runningClosestDistance, visblVrtPths[indx_runningBestPath], vsblBckstpVerts
					);

					if
					(
						paths[indx_runningBestPath] != null && paths[indx_runningBestPath].AmValid &&
						(runningClosestDistance == -1 || paths[indx_runningBestPath].TotalDistance < runningClosestDistance)
					)
					{
						runningClosestDistance = paths[indx_runningBestPath].TotalDistance;
					}
				}

				for (int i_visblVrtPths = 0; i_visblVrtPths < visblVrtPths.Count; i_visblVrtPths++)
				{
					if (i_visblVrtPths == indx_runningBestPath)
					{
						continue;
					}

					if (runningClosestDistance > 0f && visblVrtPths[i_visblVrtPths].TotalDistance > runningClosestDistance) //I found out that this actually gets triggered quite a lot, and is a huge performance saver.
					{
						continue;
					}

					paths[i_visblVrtPths] = Triangles[visblVrtPths[i_visblVrtPths].EndTriIndex].
						Verts[visblVrtPths[i_visblVrtPths].EndHit.VertIndex].Ping(
						endHit, this, runningClosestDistance, visblVrtPths[i_visblVrtPths], vsblBckstpVerts
					);

					if
					(
						paths[i_visblVrtPths] != null && paths[i_visblVrtPths].AmValid &&
						(runningClosestDistance == -1 || paths[i_visblVrtPths].TotalDistance < runningClosestDistance)
					)
					{
						indx_runningBestPath = i_visblVrtPths;
						runningClosestDistance = paths[i_visblVrtPths].TotalDistance;
					}
				}
				#endregion

				if (indx_runningBestPath > -1)
				{
					outPath = new LNX_Path(paths[indx_runningBestPath]);
					return true;
				}
			}

			return false;
		}
		public bool CalculatePath_dbg(LNX_NavmeshHit startHit, LNX_NavmeshHit endHit,
			out LNX_Path outPath, ref LNX_MethodDebugReport rprt)
		{
			rprt.StartMethod($"CalculatePath_dbg(startHit: '{startHit}', endHit: '{endHit}'");

			rprt.Log($"first, attempting to raycast to the destination...");

			rprt.StartAbbreviatedMethod("Raycast_dbg() from cp");
			LNX_Path rcPath = new LNX_Path();
			bool rcHitSomething = Raycast_dbg(startHit, endHit, out rcPath, ref rprt);
			rprt.EndAbbreviatedMethod("Raycast_dbg() from cp");

			//rprt.Log($"end of initial raycast...");
			if ( !rcHitSomething)
			{
				outPath = new LNX_Path(rcPath);
				rprt.Log_And_End_Method($"Initial raycast was false, meaning that it did NOT hit an obstruction. " +
					$"outPath: '{outPath}'. Returning true...", $"CalculatePath(startHit: '{startHit}', endHit: '{endHit}'");
				return true;
			}
			else
			{
				rprt.Log($"Initial raycast returned true, meaning it DID hit an obstruction. Continuing...");
				outPath = null; //needs to be done bc of the raycast above, which will give this a junk value if the method gets this far....

				rprt.Log($"Now checking for which verts are visible from start position...");

				//rprt.Flag_suspendAll = true;

				#region GET VISIBLE VERTS/PATHS =========================================================
				List<LNX_Path> visblVrtPths = new List<LNX_Path>();

				if ( startHit.VertIndex != -1 )
				{
					rprt.Log($"decided starthit was on a vert. Calling GetVisibleVertsFromVert_dbg()...");

					rprt.StartAbbreviatedMethod($"GetVisibleVertsFromVert_dbg()");
					visblVrtPths = GetVisibleVertsFromVert_dbg(
						Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex], ref rprt, false);
					rprt.EndAbbreviatedMethod("GetVisibleVertsFromVert_dbg()");
				}
				else
				{
					rprt.Log($"starthit NOT on a vert. Calling GetVisibleVertsFromPoint_dbg()...");

					rprt.StartAbbreviatedMethod($"GetVisibleVertsFromPoint_dbg()");
					visblVrtPths = GetVisibleVertsFromHit_dbg(startHit, ref rprt, false);
					rprt.EndAbbreviatedMethod("GetVisibleVertsFromPoint_dbg()");
				}

				if ( visblVrtPths == null || visblVrtPths.Count <= 0 )
				{
					rprt.Log_And_End_Method($"Something went wrong. GetVisibleVertsFromPoint() returned 0 paths. Returning early...", 
						$"CalculatePath(startHit: '{startHit}', endHit: '{endHit}'");
					return false;
				}

				rprt.Log($"found '{visblVrtPths.Count}' visible verts...");
				#endregion

				#region ASSEMBLE BACKSTOP VERTS ====================================
				rprt.Log($"Now assembling backstop list...");
				List<LNX_ComponentCoordinate> vsblBckstpVerts = new List<LNX_ComponentCoordinate>();
				if( startHit.VertIndex > -1 )
				{
					vsblBckstpVerts.Add( startHit.AsVertCoordinate() );
				}
				for (int i = 0; i < visblVrtPths.Count; i++) //Note: this must come before the check for shared vert space...
				{
					rprt.Log($"adding visible vert: '{visblVrtPths[i].EndCoordinate_vert}'...");
					vsblBckstpVerts.Add(visblVrtPths[i].EndCoordinate_vert);
				}
				rprt.Log($"Assembled visible backstop list with '{visblVrtPths.Count}' verts...");
				#endregion

				#region CHECK FOR BEST START PING VERT =======================================================
				rprt.Log($"Checking each visible vert for best start ping...\n");

				int indx_runningBestPath = -1;

				bool foundRelPrblm = false;
				float runningClosestDistance = -1f;
				System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

				for (int i_visblVrtPths = 0; i_visblVrtPths < visblVrtPths.Count; i_visblVrtPths++)
				{
					rprt.Log($"for({i_visblVrtPths}), vert: '{visblVrtPths[i_visblVrtPths].EndCoordinate_vert}'...");

					if ( !Triangles[startHit.TriangleIndex].IsTriangleCompletelyRelationallyValid(endHit.TriangleIndex))
					{
						rprt.Log($"startHit tri '{startHit.TriangleIndex}' does NOT consider endHit tri to be relationally valid. breaking check...");
						foundRelPrblm = true;
						break;
					}

					for ( int i_vrts = 0; i_vrts < 3; i_vrts++ )
					{
						float dist = visblVrtPths[i_visblVrtPths].TotalDistance +
						Triangles[visblVrtPths[i_visblVrtPths].EndTriIndex].Verts[visblVrtPths[i_visblVrtPths].EndHit.VertIndex].
						GetRelationship(endHit.TriangleIndex, i_vrts).PathDistance +
						Vector3.Distance(Triangles[endHit.TriangleIndex].Verts[i_vrts].V_Position, endHit.Position);
						
						if( runningClosestDistance == -1 || dist < runningClosestDistance )
						{
							runningClosestDistance = dist;
							indx_runningBestPath = i_visblVrtPths;
							rprt.Log($"decided path: '{visblVrtPths[i_visblVrtPths]}' " +
								$" seems to be the new best.",
								$"runningClosestDistance: '{runningClosestDistance}', indx_runningBestPath: '{indx_runningBestPath}'");

						}
					}
				}
				sw.Stop();
				rprt.Log($"fastest ping check took: '{sw.ElapsedMilliseconds}' ms");
				rprt.Log($"end of check for best start ping. indx_runningBestPath: '{indx_runningBestPath}', runningClosestDistance: '{runningClosestDistance}'");
				
				if (foundRelPrblm)
				{
					rprt.Log($"found relational problem. Using adjacency to calculate best start ping...");
					int runningBestAdjacency = -1;

					sw = new System.Diagnostics.Stopwatch();
					sw.Start();
					List<int> uniqueTris = new List<int>();
					for (int i_visblVrtPths = 0; i_visblVrtPths < visblVrtPths.Count; i_visblVrtPths++)
					{
						//rprt.Log($"for{i_visblVrtPths}: '{visblVrtPths[i_visblVrtPths]}'...");
						if (uniqueTris.Contains(visblVrtPths[i_visblVrtPths].EndTriIndex))
						{
							//rprt.Log($"end tri: '{visblVrtPths[i_visblVrtPths].EndTriIndex}' already inspected.");
							continue;
						}

						//rprt.Log($"found new triangle. Inspecting...");
						uniqueTris.Add(visblVrtPths[i_visblVrtPths].EndTriIndex);

						rprt.Log($"now checking adjacency from tri: '{visblVrtPths[i_visblVrtPths].EndTriIndex}', " +
							$"to tri: '{endHit.TriangleIndex}'...");

						int adjcncy = GetAdjacencyDepthToTriangle(visblVrtPths[i_visblVrtPths].EndTriIndex, endHit.TriangleIndex);

						if (adjcncy < 0)
						{
							rprt.Log($"apparently something went wrong. Continuing...");
							Debug.LogError($"apparently something went wrong. Continuing...");
							continue;
						}

						if (runningBestAdjacency == -1 || adjcncy < runningBestAdjacency)
						{
							runningBestAdjacency = adjcncy;
							indx_runningBestPath = i_visblVrtPths;
							rprt.Log($"found new best index: '{indx_runningBestPath}' with adjacency: '{runningBestAdjacency}'...");
						}

						if (adjcncy == 1)
						{
							rprt.Log($"adjacency won't get better than this. Breaking out of this looop with best adjacency of 1...");
							break;
						}
					}
					sw.Stop();
					rprt.Log($"After adjacency check, runningBestAdjacency: '{runningBestAdjacency}', " +
						$"indx_runningBestPath: '{indx_runningBestPath}'",
						$"adjacency check took: '{sw.ElapsedMilliseconds}' ms");
				}
				#endregion

				#region CONSTRUCT PATHS -------------------------------------
				rprt.Log($"Now pinging each visible vert...");
				LNX_Path[] paths = new LNX_Path[visblVrtPths.Count];

				if ( indx_runningBestPath > -1 )
				{
					rprt.Log($"starting with found best adjacency, path: '{indx_runningBestPath}'...");

					rprt.StartAbbreviatedMethod("Ping_dbg");
					paths[indx_runningBestPath] = Triangles[visblVrtPths[indx_runningBestPath].EndTriIndex].
						Verts[visblVrtPths[indx_runningBestPath].EndHit.VertIndex].Ping_dbg(
						endHit, this, runningClosestDistance, visblVrtPths[indx_runningBestPath], ref rprt, vsblBckstpVerts
					);
					rprt.EndAbbreviatedMethod("Ping_dbg");
					//Debug.Log($"ping{i_visblVrts} took: '{DateTime.Now.Subtract(dt_Try).TotalMilliseconds}'ms...");
					rprt.Log($"got path: '{(paths[indx_runningBestPath] == null ? "null" : paths[indx_runningBestPath])}'");

					if 
					( 
						paths[indx_runningBestPath] != null && paths[indx_runningBestPath].AmValid && 
						(runningClosestDistance == -1 || paths[indx_runningBestPath].TotalDistance < runningClosestDistance) 
					)
					{
						runningClosestDistance = paths[indx_runningBestPath].TotalDistance;
						rprt.Log($"found new runningBestPath with dist: '{paths[indx_runningBestPath].TotalDistance}'. " +
							$"indx_runningBestPath now: '{indx_runningBestPath}'...");
					}
					else
					{
						rprt.Log($"Decided this is NOT the new best path...");
					}
				}

				for ( int i_visblVrtPths = 0; i_visblVrtPths < visblVrtPths.Count; i_visblVrtPths++ )
				{
					rprt.Log($"for {i_visblVrtPths}: '{visblVrtPths[i_visblVrtPths]}'=====================");

					if( i_visblVrtPths == indx_runningBestPath)
					{
						rprt.Log($"bypassing bc same as indx_foundSecondarilyAdjacent, so already pinged...");
						continue;
					}

					if ( runningClosestDistance > 0f && visblVrtPths[i_visblVrtPths].TotalDistance > runningClosestDistance ) //I found out that this actually gets triggered quite a lot, and is a huge performance saver.
					{
						rprt.Log($"before pinging, determined that vsblVrtPath dist: '{visblVrtPths[i_visblVrtPths].TotalDistance}' " +
							$"is already too far",
							"bypassing this ping...");
						/*Debug.LogWarning($"before pinging, determined that vsblVrtPath dist: '{visblVrtPths[i_visblVrts].TotalDistance}' " +
							$"is already too far. " +
							"bypassing this ping...");*/
						continue;
					}

					rprt.StartAbbreviatedMethod("Ping_dbg");
					paths[i_visblVrtPths] = Triangles[visblVrtPths[i_visblVrtPths].EndTriIndex].
						Verts[visblVrtPths[i_visblVrtPths].EndHit.VertIndex].Ping_dbg(
						endHit, this, runningClosestDistance, visblVrtPths[i_visblVrtPths], ref rprt, vsblBckstpVerts
					);
					rprt.EndAbbreviatedMethod("Ping_dbg");
					//Debug.Log($"ping{i_visblVrts} took: '{DateTime.Now.Subtract(dt_Try).TotalMilliseconds}'ms...");

					if(paths[i_visblVrtPths] == null )
					{
						rprt.Log($"got null path");
					}
					else
					{
						rprt.Log($"Got path: '{paths[i_visblVrtPths]}' with dist: '{paths[i_visblVrtPths].TotalDistance}'.",
							$"pts: '{(paths[i_visblVrtPths] == null ? "None" : paths[i_visblVrtPths].PointCount)}'",
							$"Checking against runningClosestDistance: '{runningClosestDistance}' to see if this is a new best path...");
					}

					if 
					(
						paths[i_visblVrtPths] != null && paths[i_visblVrtPths].AmValid && 
						(runningClosestDistance == -1 || paths[i_visblVrtPths].TotalDistance < runningClosestDistance) 
					)
					{
						indx_runningBestPath = i_visblVrtPths;
						runningClosestDistance = paths[i_visblVrtPths].TotalDistance;
						rprt.Log($"found new runningBestPath with dist: '{paths[i_visblVrtPths].TotalDistance}'. indx now: '{indx_runningBestPath}'...");
					}
					else
					{
						rprt.Log($"Decided this is NOT the new best path...");
					}
				}

				rprt.EmptyLine();
				rprt.Log($"end of for loop. indx_runningBestPath: '{indx_runningBestPath}'...");
				#endregion

				if (indx_runningBestPath > -1)
				{
					outPath = new LNX_Path(paths[indx_runningBestPath]);

					rprt.Log($"made outPath: '{outPath}'...");
					rprt.Log_And_End_Method($"returning true...", $"CalculatePath(startHit: '{startHit}', endHit: '{endHit}'");

					return true;
				}
			}

			rprt.Log_And_End_Method($"returning false...", $"CalculatePath(startHit: '{startHit}', endHit: '{endHit}'");
			return false;
		}


		public bool CalculatePath(LNX_Vertex startVert, LNX_Vertex endVert, out LNX_Path outPath)
		{
			#region SHORT-CIRCUITING ===================================
			if
			(
				startVert.IsRelationshipCollectionSuperficiallyValid(Triangles.Length) &&
				startVert.IsVertexRelationallyValid(endVert.TriangleIndex, endVert.ComponentIndex)
			)
			{
				outPath = new LNX_Path(startVert.GetPathTo(endVert));
				Debug.LogError($"IIIIIITTTTT happened! strt: '{startVert}', end: '{endVert}', rslt: '{outPath}'");

				return true;
			}
			#endregion

			return CalculatePath(
				new LNX_NavmeshHit(startVert, Triangles[startVert.TriangleIndex].V_PathingNormal),
				new LNX_NavmeshHit(endVert, Triangles[endVert.TriangleIndex].V_PathingNormal),
				out outPath);
		}
		public bool CalculatePath_dbg(LNX_Vertex startVert, LNX_Vertex endVert, out LNX_Path outPath, ref LNX_MethodDebugReport rprt)
		{
			rprt.StartMethod($"CalculatePath_dbg(startVert: '{startVert}', endVert: '{endVert}')");

			#region SHORT-CIRCUITING ===================================
			if 
			(
				startVert.IsRelationshipCollectionSuperficiallyValid(Triangles.Length) && 
				startVert.IsVertexRelationallyValid(endVert.TriangleIndex, endVert.ComponentIndex) 
			)
			{
				rprt.Log_And_End_Method(
					$"start rels length: '{startVert.Relationships.Length}', end rels length: '{endVert.Relationships.Length}' " +
					$"relationships valid. Getting already-existing cached relational path...");
				Debug.LogError("IIIIIITTTTT happened");
				outPath = new LNX_Path( startVert.GetPathTo(endVert) );
				return true;
			}
			rprt.Log($"relationships for start and/or end vert not valid...");
			
			#endregion

			rprt.Log($"No short-circuits. Now passing off to more atomic version of method...");
			
			bool rslt = CalculatePath_dbg(
				new LNX_NavmeshHit(startVert, Triangles[startVert.TriangleIndex].V_PathingNormal), 
				new LNX_NavmeshHit(endVert, Triangles[endVert.TriangleIndex].V_PathingNormal), 
				out outPath, ref rprt );
			

			rprt.Log_And_End_Method($"ending method with rslt: '{rslt}'...");

			return rslt;
		}
		#endregion

		#region GET VISIBLE VERTS ========================================
		public List<LNX_Path> GetVisibleVertsFromHit( 
			LNX_NavmeshHit hit, bool includeFringeVerts = false, 
			List<LNX_ComponentCoordinate> excludeVerts = null, float maxDist = -1f, bool includeHitVert = false
		)
		{
			#region INITIALIZE VISIBLE VERT PATHS COLLECTION ===============================
			List<LNX_Path> visibleVertPaths = new List<LNX_Path>();

			for (int i = 0; i < 3; i++)
			{
				if ( !includeHitVert && i == hit.VertIndex)
				{
					continue;
				}

				if (!includeFringeVerts && IsBoundsVert(hit.TriangleIndex, i))
				{
					continue;
				}

				if (!VertTouchesAnotherVertInList(Triangles[hit.TriangleIndex].Verts[i].MyCoordinate, excludeVerts))
				{
					visibleVertPaths.Add
					(
						new LNX_Path(GetSurfaceProjectionVector(), hit, new LNX_NavmeshHit(Triangles[hit.TriangleIndex].Verts[i]))
					);
				}
			}
			#endregion

			for (int i_tris = 0; i_tris < Triangles.Length; i_tris++)
			{
				if (i_tris == hit.TriangleIndex) //already accounted for...
				{
					continue;
				}

				for (int i_vrts = 0; i_vrts < 3; i_vrts++)
				{
					#region SHORT-CIRCUITING =======================================
					if ( !includeHitVert && hit.VertIndex > -1 &&
					Triangles[i_tris].Verts[i_vrts].V_Position == Triangles[hit.TriangleIndex].Verts[hit.VertIndex].V_Position)
					{
						continue;
					}

					if (excludeVerts != null && excludeVerts.Count > 0)
					{
						if (VertTouchesAnotherVertInList(Triangles[i_tris].Verts[i_vrts].MyCoordinate, excludeVerts))
						{
							continue;
						}
					}

					if (visibleVertPaths.Count > 0) //if we've already got at least one logged visible vert path...
					{
						if (VertTouchesAnotherVertInList(Triangles[i_tris].Verts[i_vrts].MyCoordinate, visibleVertPaths))
						{
							continue;
						}
					}

					if (maxDist > 0f)
					{
						if (Vector3.Distance(hit.Position, Triangles[i_tris].Verts[i_vrts].V_Position) > maxDist)
						{
							//Debug.Log($"distance from '{hit}' to '{Triangles[i_tris].Verts[i_vrts]}' beyond max: '{maxDist}'. Bypassing...");
							continue;
						}
					}

					if (!includeFringeVerts && IsBoundsVert(i_tris, i_vrts))
					{
						continue;
					}
					#endregion---------------------------------
					LNX_Path path;
					if 
					(
						!Raycast
						(
							hit, new LNX_NavmeshHit(Triangles[i_tris].Verts[i_vrts], Triangles[i_tris].V_PathingNormal),
								out path
						)
					)
					{
						visibleVertPaths.Add(path);
					}
				}
			}

			return visibleVertPaths;
		}

		public List<LNX_Path> GetVisibleVertsFromHit_dbg(
			LNX_NavmeshHit hit, ref LNX_MethodDebugReport rprt, bool includeFringeVerts = false, 
			List<LNX_ComponentCoordinate> excludeVerts = null, float maxDist = -1f, bool includeHitVert = false
		)
		{
			rprt.StartMethod( $"GetVisibleVertsFromPoint_dbg(hit: '{hit}', excldCount: " +
				$"'{(excludeVerts == null ? "null" : excludeVerts.Count)}')" );

			#region INITIALIZE VISIBLE VERT PATHS COLLECTION ===============================
			List<LNX_Path> visibleVertPaths = new List<LNX_Path>();

			rprt.Log($"Initialized visibleVertPaths. First attempting to add the verts that are a part of this triangle...");
			for ( int i = 0; i < 3; i++ )
			{
				rprt.Log($"for composing vert '{i}'...");

				if (!includeHitVert && i == hit.VertIndex)
				{
					rprt.Log($"Same position. Bypassing...");
					continue;
				}

				if ( !includeFringeVerts && IsBoundsVert(hit.TriangleIndex, i) )
				{
					rprt.Log($"Found that composing vert '[{hit.TriangleIndex}][{i}]' was a fringe vert. Excluding from list...");
					continue;
				}

				if (!VertTouchesAnotherVertInList(Triangles[hit.TriangleIndex].Verts[i].MyCoordinate, excludeVerts))
				{
					rprt.Log($"adding composing vert '{i}' to list...");
					visibleVertPaths.Add
					(
						new LNX_Path(GetSurfaceProjectionVector(), hit, new LNX_NavmeshHit(Triangles[hit.TriangleIndex].Verts[i]))
					);
				}
				else
				{
					rprt.Log($"composing vert '{i}' touched another vert in exclude list...");
				}
			}
			#endregion

			rprt.EmptyLine();
			rprt.Log("for-looping through all tris and verts...");
			for (int i_tris = 0; i_tris < Triangles.Length; i_tris++)
			{
				string triString = $"for tri{i_tris}";
				
				if (i_tris == hit.TriangleIndex)
				{
					rprt.Log( $"{triString}..." );
					rprt.Log($"same tri index. composite verts already accounted for. Continuing...");
					continue;
				}
				
				for (int i_vrts = 0; i_vrts < 3; i_vrts++)
				{
					rprt.Log(triString + $", vert{i_vrts}..." );

					#region SHORT-CIRCUITING =======================================
					if ( !includeHitVert && hit.VertIndex > -1 &&
						Triangles[i_tris].Verts[i_vrts].V_Position == Triangles[hit.TriangleIndex].Verts[hit.VertIndex].V_Position)
					{
						rprt.Log($"Same position. Bypassing...");
						continue;
					}

					if (excludeVerts != null && excludeVerts.Count > 0)
					{
						rprt.Log($"was passed exclude verts. Checking exclude vert collection...");

						if ( VertTouchesAnotherVertInList(Triangles[i_tris].Verts[i_vrts].MyCoordinate, excludeVerts) )
						{
							rprt.Log($"found that vert[{i_tris}][{i_vrts}] shares space with an exclude vert...");
							continue;
						}

						rprt.Log($"did not find match in exclude vert collection. Proceeding...");
					}

					if ( visibleVertPaths.Count > 0 ) //if we've already got at least one logged visible vert path...
					{
						rprt.Log($"vvp count: '{visibleVertPaths.Count}'. This means we need to check if already logged...");

						if (VertTouchesAnotherVertInList(Triangles[i_tris].Verts[i_vrts].MyCoordinate, visibleVertPaths) )
						{
							rprt.Log($"There's a vert in growing list of visible already logged at the same position as this " +
								$"vert[{i_tris}][{i_vrts}]. Bypassing..."); 
							continue;
						}
					}

					if (maxDist > 0f)
					{
						//Debug.Log($"incremented to: '{NumberOfTimes}'");

						if (Vector3.Distance(hit.Position, Triangles[i_tris].Verts[i_vrts].V_Position) > maxDist)
						{
							rprt.Log($"distance too far. Bypassing...");
							//Debug.Log($"distance from '{hit}' to '{Triangles[i_tris].Verts[i_vrts]}' beyond max: '{maxDist}'. Bypassing...");
							continue;
						}
					}

					if (!includeFringeVerts && IsBoundsVert(i_tris, i_vrts))
					{
						rprt.Log($"Found that visible vert '[{i_tris}][{i_vrts}]' was a fringe vert. Excluding from list...");
						continue;
					}
					#endregion---------------------------------
					rprt.Log("no short-circuits apply. Raycasting from hit point to current vert...");
					LNX_Path path;
					//LNX_MethodDebugReport rcRprt = new LNX_MethodDebugReport();
					//rcRprt.StartMethod("Raycast_dbg");

					//rprt.StartAbbreviatedMethod("Raycast_dbg()");
					if ( !Raycast_dbg( 
						hit, new LNX_NavmeshHit(Triangles[i_tris].Verts[i_vrts], Triangles[i_tris].V_PathingNormal), 
						out path, ref rprt) 
					)
					{
						//rprt.EndAbbreviatedMethod("");
						
						rprt.Log($"raycast to vert[{i_tris}][{i_vrts}] showed clear path. Adding path to vsblVrtPths. endhit: '{path.EndHit}'...");
						visibleVertPaths.Add( path );
					}
					else
					{
						//rprt.EndAbbreviatedMethod("");
						
						rprt.Log($"raycast from hit: '{hit}' to vert[{i_tris}][{i_vrts}] hit obstruction");
					}
					//rcRprt.EndMethod();
				}
			}

			rprt.EndMethod("GetVisibleVertsFromPoint_dbg()");
			return visibleVertPaths;
		}

		public List<LNX_Path> GetVisibleVertsFromVert(LNX_Vertex vert,
			bool includeFringeVerts = false, List<LNX_ComponentCoordinate> excludeVerts = null, float maxDist = -1f)
		{
			List<LNX_Path> relationalPaths = new List<LNX_Path>();

			bool foundAnInvalidRel = false;

			if (vert.IsRelationshipCollectionSuperficiallyValid(Triangles.Length))
			{
				for (int i = 0; i < vert.Relationships.Length; i++)
				{
					if (i == vert.Index_Relational)
					{
						continue;
					}

					if (vert.Relationships[i] == null || !vert.Relationships[i].AmValid )
					{
						foundAnInvalidRel = true;
						continue;
					}

					if (vert.Relationships[i].PathTo.PointCount <= 1)
					{
						continue;
					}

					if 
					( 
						VertTouchesAnotherVertInList(vert.Relationships[i].RelatedVertCoordinate, excludeVerts) ||
						VertTouchesAnotherVertInList(vert.Relationships[i].RelatedVertCoordinate, relationalPaths)
					)
					{
						continue;
					}

					if (!includeFringeVerts && IsBoundsVert(vert.Relationships[i].RelatedTriIndex, vert.Relationships[i].RelatedComponentIndex))
					{
						continue;
					}

					if 
					(
						vert.Relationships[i].CanSee && 
						(
							maxDist <= 0 || 
							vert.Relationships[i].PathDistance <= maxDist
						)
					)
					{
						relationalPaths.Add(new LNX_Path(vert.Relationships[i].PathTo));
					}
				}

				if (!foundAnInvalidRel)
				{
					return relationalPaths;
				}

			}

			#region ASSEMBLE FORWARD EXCLUDE LIST ===========================================
			List<LNX_ComponentCoordinate> fwdExcludeVerts = new List<LNX_ComponentCoordinate>();
			if (excludeVerts != null && excludeVerts.Count > 0)
			{
				for (int i = 0; i < excludeVerts.Count; i++)
				{
					fwdExcludeVerts.Add(excludeVerts[i]);
				}
			}

			if (relationalPaths.Count > 0)
			{
				for (int i = 0; i < relationalPaths.Count; i++)
				{
					fwdExcludeVerts.Add(relationalPaths[i].EndCoordinate_vert);
				}
			}
			#endregion

			List<LNX_Path> gvvfpPaths = GetVisibleVertsFromHit(
				new LNX_NavmeshHit(vert),
				includeFringeVerts, fwdExcludeVerts, maxDist
			);

			for (int i = 0; i < gvvfpPaths.Count; i++)
			{
				relationalPaths.Add(gvvfpPaths[i]);
			}

			return relationalPaths;
		}
		public List<LNX_Path> GetVisibleVertsFromVert_dbg(LNX_Vertex vert, ref LNX_MethodDebugReport rprt,
			bool includeFringeVerts = false, List<LNX_ComponentCoordinate> excludeVerts = null, float maxDist = -1f)
		{
			rprt.StartMethod($"GetVisibleVertsFromVert_dbg(vert: '{vert}' excludecount: " +
				$"'{(excludeVerts == null ? "null" : excludeVerts.Count)}')");

			rprt.Log($"Initializing path collection. First deciding if siblings should be immediately added to collection...");
			List<LNX_Path> relationalPaths = new List<LNX_Path>();

			bool foundAnInvalidRel = false;

			rprt.Log($"First, checking validity of existing relationships...");
			if ( vert.IsRelationshipCollectionSuperficiallyValid(Triangles.Length) )
			{
				rprt.Log($"Relationship collection is superficially valid. Now checking through all relationships...");
				for (int i = 0; i < vert.Relationships.Length; i++)
				{
					//rprt.Log($"for {i}, ({vert.Relationships[i].RelatedVertCoordinate})...");
					if ( i == vert.Index_Relational )
					{
						rprt.Log($"this should be self relationship. Bypassing...");
						continue;
					}

					//rprt.Log($"first, checking if this one is already accounted for in the excludeverts list...");
					if ( vert.Relationships[i] == null || !vert.Relationships[i].AmValid )
					{
						//rprt.Log($"relationship not valid. bypassing...");
						foundAnInvalidRel = true;
						continue;
					}

					if ( vert.Relationships[i].PathTo.PointCount <= 1 )
					{
						rprt.Log($"relational path point count is 1. Continuing...");
						continue;
					}

					//rprt.Log($"relationship valid. Inspecting further...");

					if 
					( 
						VertTouchesAnotherVertInList(vert.Relationships[i].RelatedVertCoordinate, excludeVerts) ||
						VertTouchesAnotherVertInList(vert.Relationships[i].RelatedVertCoordinate, relationalPaths)
					)
					{
						//rprt.Log($"ultimately decided that this relationship is already accounted for. Moving on to next relationship...");
						continue;
					}

					if (!includeFringeVerts && IsBoundsVert(vert.Relationships[i].RelatedTriIndex, vert.Relationships[i].RelatedComponentIndex))
					{
						//rprt.Log($"Found that relationship '{vert.Relationships[i]}' was to a fringe vert. Excluding from list...");
						continue;
					}

					//rprt.Log($"Testing straight-path vision using CanSee property: '{vert.Relationships[i].CanSee}'...");

					if (vert.Relationships[i].CanSee)
					{
						//rprt.Log($"found that this relationship '{vert.Relationships[i]}' has clear vision...");

						if( maxDist <= 0 || vert.Relationships[i].PathDistance <= maxDist )
						{
							//rprt.Log($"Distance check passed. Adding to out paths!!!!!!!!!!!!!!!!!!!!!!!!!!");

							relationalPaths.Add( new LNX_Path(vert.Relationships[i].PathTo) );
						}
						else
						{
							rprt.Log($"distance check did NOT pass. Bypassing...");
						}

						//rprt.Log($"relationalPaths count now: '{relationalPaths.Count}'...");
					}
					else
					{
						//rprt.Log("found that this relationship CANNOT see..");
					}

				}

				rprt.Log($"End of for loop. Got '{relationalPaths.Count}' relational paths.", 
					$"foundInvalidRel: '{foundAnInvalidRel}'...", 
					$"exclude verts count: '{(excludeVerts == null ? "null" : excludeVerts.Count)}'...");

				if (!foundAnInvalidRel)
				{
					rprt.Log_And_End_Method($"did NOT find invalid relationship. " +
						$"Now returning collection of '{relationalPaths.Count}' relationships...");

					return relationalPaths;
				}
			}

			rprt.Log($"There was an invalid relationship, so GetVisibleVertsFromPoint() will need to be checked...");

			#region ASSEMBLE FORWARD EXCLUDE LIST ===========================================
			rprt.Log($"First, creating forward exclude list. Passed param count: '{(excludeVerts == null ? "null" : excludeVerts.Count)}'...");
			List<LNX_ComponentCoordinate> fwdExcludeVerts = new List<LNX_ComponentCoordinate>();
			if ( excludeVerts != null && excludeVerts.Count > 0 )
			{
				for ( int i = 0; i < excludeVerts.Count; i++ )
				{
					fwdExcludeVerts.Add( excludeVerts[i] );
				}
			}
			rprt.Log($"pre-assembled fwd list with count: '{fwdExcludeVerts.Count}'...");

			if ( relationalPaths.Count > 0 )
			{
				rprt.Log($"now adding relational paths to forward list...");
				for ( int i = 0; i < relationalPaths.Count; i++ )
				{
					rprt.Log($"adding '{relationalPaths[i].EndCoordinate_vert}' to forward list...");
					fwdExcludeVerts.Add( relationalPaths[i].EndCoordinate_vert );
				}
			}
			rprt.Log($"finally, fwd list has count: '{fwdExcludeVerts.Count}'...");
			#endregion

			rprt.Log($"now calling GetVisibleVertsFromPoint_dbg()...");
			//rprt.StartAbbreviatedMethod($"GetVisibleVertsFromPoint_dbg()");
			List<LNX_Path> gvvfpPaths = GetVisibleVertsFromHit_dbg(
				new LNX_NavmeshHit(vert), ref rprt,
				includeFringeVerts, fwdExcludeVerts, maxDist
			);
			//rprt.EndAbbreviatedMethod("GetVisibleVertsFromPoint_dbg()");
			rprt.Log($"operation returned '{gvvfpPaths.Count}' paths. Adding these to return list...");

			for (int i = 0; i < gvvfpPaths.Count; i++)
			{
				rprt.Log($"adding '{gvvfpPaths[i].EndCoordinate_vert}' to return list...");
				relationalPaths.Add( gvvfpPaths[i] );

			}

			rprt.Log_And_End_Method($"end of gvvfv. returning '{relationalPaths.Count}' paths...");

			return relationalPaths;
		}
		#endregion

		#endregion // (END) MAIN API METHODS---------------------

		public int GetAdjacencyDepthToTriangle(int startTriIndx, int endTriIndx )
		{
			List<int> backstopTris = new List<int>() { startTriIndx };
			int runningDepth = 0;

			#region SHORT-CIRCUITING ============================================
			if (startTriIndx == endTriIndx)
			{
				return 0;
			}
			#endregion

			bool amFinished = false;
			while (!amFinished)
			{
				runningDepth++;
				if (runningDepth > Triangles.Length)
				{
					Debug.LogError($"runningDepth exceded triangles length!");
					return -1;
				}

				#region ASSEMBLE ADJACENT TRIANGLES LIST =================================
				List<int> adjacentTris = new List<int>();
				List<int> heldBackStopTris = new List<int>();
				for (int i = 0; i < backstopTris.Count; i++)
				{
					heldBackStopTris.Add(backstopTris[i]);
				}

				for (int i = 0; i < heldBackStopTris.Count; i++)
				{
					for (int i_vrts = 0; i_vrts < 3; i_vrts++)
					{
						for (int i_shrd = 0; i_shrd < Triangles[backstopTris[i]].Verts[i_vrts].SharedVertexCoordinates.Length; i_shrd++)
						{
							if (Triangles[heldBackStopTris[i]].Verts[i_vrts].SharedVertexCoordinates[i_shrd].TrianglesIndex == endTriIndx)
							{
								return runningDepth;
							}

							if
							(
								!adjacentTris.Contains(Triangles[heldBackStopTris[i]].Verts[i_vrts].SharedVertexCoordinates[i_shrd].TrianglesIndex) &&
								!backstopTris.Contains(Triangles[heldBackStopTris[i]].Verts[i_vrts].SharedVertexCoordinates[i_shrd].TrianglesIndex)
							)
							{
								adjacentTris.Add(Triangles[heldBackStopTris[i]].Verts[i_vrts].SharedVertexCoordinates[i_shrd].TrianglesIndex);
								backstopTris.Add(Triangles[heldBackStopTris[i]].Verts[i_vrts].SharedVertexCoordinates[i_shrd].TrianglesIndex);

							}
						}
					}
				}
			}
			#endregion

			return -1;
		}

		public int GetAdjacencyDepthToTriangle_dbg( int startTriIndx, int endTriIndx, ref LNX_MethodDebugReport rprt )
		{
			rprt.StartMethod($"GetAdjacencyDepthToTriangle_dbg(startTriIndx: '{startTriIndx}',  endTriIndx: '{endTriIndx}')");

			List<int> backstopTris = new List<int>() { startTriIndx };
			int runningDepth = 0;

			#region SHORT-CIRCUITING ============================================
			if( startTriIndx == endTriIndx )
			{
				rprt.Log_And_End_Method($"indices the same.");
				return 0;
			}

			if (DateTime.Now.Subtract(rprt.DT_Start).TotalSeconds > 12f)
			{
				Debug.LogError($"datetime timeout. Returning early for safety...");

				rprt.Log_And_End_Method($"datetime timeout. Returning early for safety...");
				return -1;
			}
			#endregion

			bool amFinished = false;
			while ( !amFinished )
			{
				runningDepth++;
				rprt.Log($"while() runningDepth: '{runningDepth}'...");

				if (runningDepth > Triangles.Length)
				{
					rprt.Log($"runningDepth exceded triangles length!");

					Debug.LogError($"runningDepth exceded triangles length!");
					return -1;
				}

				rprt.Log("=======================================================================");
				#region ASSEMBLE ADJACENT TRIANGLES LIST =================================
				rprt.Log($"assembling list of adjacent triangles");

				List<int> adjacentTris = new List<int>();
				List<int> heldBackStopTris = new List<int>();
				for (int i = 0; i < backstopTris.Count; i++)
				{
					heldBackStopTris.Add(backstopTris[i]);
				}

				for( int i = 0; i < heldBackStopTris.Count; i++ )
				{
					for (int i_vrts = 0; i_vrts < 3; i_vrts++)
					{
						for (int i_shrd = 0; i_shrd < Triangles[backstopTris[i]].Verts[i_vrts].SharedVertexCoordinates.Length; i_shrd++)
						{
							if (Triangles[heldBackStopTris[i]].Verts[i_vrts].SharedVertexCoordinates[i_shrd].TrianglesIndex == endTriIndx)
							{
								rprt.Log_And_End_Method($"found endtri in adjacent indices. Returning: '{runningDepth + 1}'");
								return runningDepth;
							}

							if
							(
								!adjacentTris.Contains(Triangles[heldBackStopTris[i]].Verts[i_vrts].SharedVertexCoordinates[i_shrd].TrianglesIndex) &&
								!backstopTris.Contains(Triangles[heldBackStopTris[i]].Verts[i_vrts].SharedVertexCoordinates[i_shrd].TrianglesIndex)
							)
							{
								rprt.Log($"adding tri: '{Triangles[heldBackStopTris[i]].Verts[i_vrts].SharedVertexCoordinates[i_shrd].TrianglesIndex}' to adjacent list from v{i_vrts}...");
								adjacentTris.Add(Triangles[heldBackStopTris[i]].Verts[i_vrts].SharedVertexCoordinates[i_shrd].TrianglesIndex);
								backstopTris.Add(Triangles[heldBackStopTris[i]].Verts[i_vrts].SharedVertexCoordinates[i_shrd].TrianglesIndex);

							}
						}
					}
				}

				rprt.Log($"finished assembling list of adjacent tris. List length: '{adjacentTris.Count}', backstop: '{backstopTris.Count}'...");
				rprt.Log("=======================================================================");
			}
			#endregion

			return -1;
		}

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
			StringBuilder sb = new StringBuilder();
			//First, clear collection and make proximal relationships so that 2-way assignment is guaranteed to work...
			for (int i = 0; i < Triangles.Length; i++)
			{
				Triangles[i].Verts[0].Relationships = null;
				Triangles[i].Verts[1].Relationships = null;
				Triangles[i].Verts[2].Relationships = null;

				Triangles[i].Verts[0].CreateRelationships(this, true, true, false, ref sb);
				Triangles[i].Verts[1].CreateRelationships(this, true, true, false, ref sb);
				Triangles[i].Verts[2].CreateRelationships(this, true, true, false, ref sb);
			}

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				Triangles[i].Verts[0].CreateRelationships(this, false, false, true, ref sb);
				Triangles[i].Verts[1].CreateRelationships(this, false, false, true, ref sb);
				Triangles[i].Verts[2].CreateRelationships(this, false, false, true, ref sb);
			}

			#region CALCULATE TRI KNOWN-VISIBILITY ----------------------------------
			LNX_Edge[] trmnlEdges = GetTerminalEdges(false);

			float maxTime = 45f;
			DateTime dt_start = DateTime.Now;
			bool cmpltd = false;

			for (int i = 0; i < Triangles.Length; i++)
			{
				//Note: The following is because these indices need to be all cleared before complete
				//visible is calculated to prevent problems when using 2-way assignment...
				Triangles[i].ClearKnownVisible(); 
			}

			for ( int i = 0; i < Triangles.Length; i++ )
			{
				//Debug.Log(i);
				Triangles[i].CalculateCompletelyVisibleTris( this, trmnlEdges );
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

		public void WriteEfficiencyData() //todo: is this doing anything? dws if not
		{
			string fPath = /*GetEfficiencyDataFilepath_Managed();*/ "";

			LNX_NavMeshData data = new LNX_NavMeshData(this);
			File.WriteAllText( fPath, JsonUtility.ToJson(data, true) );

			Debug.Log($"Wrote data to json at: '{fPath}'");
		}

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
						return;
					}

					// 3. CREATE DATA OBJECT AND WRITE TO DISK ===============================================
					LNX_NavMeshData data = new LNX_NavMeshData(this);
					File.WriteAllText(filePthString, JsonUtility.ToJson(data, true));

					sb_report.AppendLine($"Wrote data to json at: '{filePthString}'\n");

					// 4. DEAL WITH THE GUID ===============================================


					AssetDatabase.ImportAsset(filePthString);

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

		#region VERTEX OPERATIONS =====================================================
		public bool VertIsOnTerminalEdge(int triIndx, int vrtIndx)
		{
			if(
				Triangles[triIndx].Edges[Triangles[triIndx].Verts[vrtIndx].Index_FirstFormingEdge].AmTerminal ||
				Triangles[triIndx].Edges[Triangles[triIndx].Verts[vrtIndx].Index_SecondFormingEdge].AmTerminal 
			)
			{
				return true;
			}

			foreach( LNX_ComponentCoordinate coord in Triangles[triIndx].Verts[vrtIndx].SharedVertexCoordinates )
			{
				if 
				(
					Triangles[coord.TrianglesIndex].Edges
						[
							Triangles[coord.TrianglesIndex].Verts[coord.ComponentIndex].Index_FirstFormingEdge
						].AmTerminal ||
					Triangles[coord.TrianglesIndex].Edges
						[
							Triangles[coord.TrianglesIndex].Verts[coord.ComponentIndex].Index_SecondFormingEdge
						].AmTerminal
				)
				{
					return true;
				}
			}

			/*
			//todo: efficiency test how much faster this really is considering how horrible it looks...
			for ( int i = 0; i < Triangles[triIndx].Verts[vrtIndx].SharedVertexCoordinates.Length; i++ )
			{
				if
				(
					Triangles[Triangles[triIndx].Verts[vrtIndx].SharedVertexCoordinates[i].TrianglesIndex].
						Edges
						[
							Triangles[Triangles[triIndx].Verts[vrtIndx].SharedVertexCoordinates[i].TrianglesIndex].
								Verts[Triangles[triIndx].Verts[vrtIndx].SharedVertexCoordinates[i].ComponentIndex].Index_FirstFormingEdge
						].AmTerminal ||
					Triangles[Triangles[triIndx].Verts[vrtIndx].SharedVertexCoordinates[i].TrianglesIndex].
						Edges
						[
							Triangles[Triangles[triIndx].Verts[vrtIndx].SharedVertexCoordinates[i].TrianglesIndex].
								Verts[Triangles[triIndx].Verts[vrtIndx].SharedVertexCoordinates[i].ComponentIndex].Index_SecondFormingEdge
						].AmTerminal
				)
				{
					return true;
				}
			}
			*/

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

		public bool VertsShareSpace( LNX_ComponentCoordinate firstVertCoordinate, LNX_ComponentCoordinate secondVertCoordinate )
		{
			if 
			(
				firstVertCoordinate == secondVertCoordinate ||
				Triangles[firstVertCoordinate.TrianglesIndex].Verts[firstVertCoordinate.ComponentIndex].SharesVertSpace_ViaRelational
					(secondVertCoordinate.TrianglesIndex, secondVertCoordinate.ComponentIndex) ||
				Triangles[secondVertCoordinate.TrianglesIndex].Verts[secondVertCoordinate.ComponentIndex].SharesVertSpace_ViaRelational
					(firstVertCoordinate.TrianglesIndex, firstVertCoordinate.ComponentIndex)
			)
			{
				return true;
			}

			return Triangles[firstVertCoordinate.TrianglesIndex].Verts[firstVertCoordinate.ComponentIndex].V_Position ==
				Triangles[secondVertCoordinate.TrianglesIndex].Verts[secondVertCoordinate.ComponentIndex].V_Position;
		}

		public bool VertTouchesAnotherVertInList( LNX_ComponentCoordinate vert, List<LNX_ComponentCoordinate> vertList )
		{
			if ( vertList != null && vertList.Count > 0 )
			{
				for ( int i = 0; i < vertList.Count; i++ )
				{
					if( VertsShareSpace( vert, vertList[i]) )
					{
						return true;
					}
				}
			}

			return false;
		}

		public bool VertTouchesAnotherVertInList(LNX_ComponentCoordinate vert, List<LNX_Path> vertList)
		{
			if (vertList != null && vertList.Count > 0)
			{
				for (int i = 0; i < vertList.Count; i++)
				{
					if (vertList[i].EndHit.VertIndex != -1 && VertsShareSpace(vert, vertList[i].EndCoordinate_vert) )
					{
						return true;
					}
				}
			}

			return false;
		}

		public bool VertTouchesTriangle(LNX_ComponentCoordinate vertCoordinate, int triIndex ) //todo: unit test
		{
			if ( vertCoordinate.TrianglesIndex == triIndex )
			{
				return true;
			}

			if 
			(
				Triangles[vertCoordinate.TrianglesIndex].Verts[vertCoordinate.ComponentIndex].SharedVertexCoordinates != null &&
				Triangles[vertCoordinate.TrianglesIndex].Verts[vertCoordinate.ComponentIndex].SharedVertexCoordinates.Length > 0
			)
			{
				for 
				(
					int i = 0; 
					i < Triangles[vertCoordinate.TrianglesIndex].Verts[vertCoordinate.ComponentIndex].SharedVertexCoordinates.Length; 
					i++
				)
				{
					if (Triangles[vertCoordinate.TrianglesIndex].Verts[vertCoordinate.ComponentIndex].SharedVertexCoordinates[i].TrianglesIndex == triIndex)
					{
						return true;
					}
				}
			}
			else //fallback for when relational data isn't loaded...
			{
				if
				(
					Triangles[triIndex].Verts[0].V_Position == Triangles[vertCoordinate.TrianglesIndex].Verts[vertCoordinate.ComponentIndex].V_Position ||
					Triangles[triIndex].Verts[1].V_Position == Triangles[vertCoordinate.TrianglesIndex].Verts[vertCoordinate.ComponentIndex].V_Position ||
					Triangles[triIndex].Verts[2].V_Position == Triangles[vertCoordinate.TrianglesIndex].Verts[vertCoordinate.ComponentIndex].V_Position
				)
				{
					return true;
				}
			}

			return false;
		}

		public bool VertTouchesTriangle( LNX_Vertex vert, int triIndx ) //todo: unit test
		{
			return VertTouchesTriangle(vert.MyCoordinate, triIndx);
		}

		#endregion

		#region TRIANGLE OPERATIONS ==================================================
		/*
		public int GetAdjacencyDepthToTriangle_dbg(int otherTriIndx, LNX_NavMesh nm, out float runningDist, int runningDepth,
			List<int> backstopTriIndices, ref LNX_MethodDebugReport rprt)
		{
			rprt.StartMethod($"{this}.GetAdjacencyDepthToTriangle_dbg(otherTriIndx: '{otherTriIndx}'");

		}
		*/
		#endregion

			#region HIT OPERATIONS ===========================================================
			/// <summary>
			/// Tells whether the supplied projection is directionally-terimnal if starting from the supplied hit.
			/// <para>Note: This method is "extrapolated" meaning it draws it's conclusion from existing relational information present on 
			/// the hit object and the navmesh components rather than performing an expensive calculation.<br></br>
			/// This makes it must faster, but it means that the supplied hit and the navmesh must have correct relational information in order for this method to work right.</para>
			/// </summary>
			/// <param name="projection"></param>
			/// <param name="hit"></param>
			/// <returns></returns>
		public bool DirectionIsTerminalFromHit_extrapolated( Vector3 projection, LNX_NavmeshHit hit )
		{
			Vector3 fltPrjction = FlatVector(projection);

			if( hit.EdgeIndex > -1 )
			{
				if ( Triangles[hit.TriangleIndex].Edges[hit.EdgeIndex].AmTerminal )
				{
					return Vector3.Dot( Triangles[hit.TriangleIndex].Edges[hit.EdgeIndex].v_Cross_flat, fltPrjction ) < 0f;
				}
				else
				{
					return false;
				}
			}
			else if ( hit.VertIndex > -1 )
			{
				return Triangles[hit.TriangleIndex].Verts[hit.VertIndex].GetVertCoord_viaProjectionSweep(fltPrjction, true) == LNX_ComponentCoordinate.None;
			}

			return false; //because it assumes the projection is on the inside of a triangle so it could NOT be terminal...
		}

		public bool HitIsOnTriPerimeter_extrapolated( LNX_NavmeshHit hit, LNX_Triangle tri )
		{
			if (hit.EdgeIndex > -1)
			{
				if (hit.TriangleIndex == tri.Index_inCollection)
				{
					return true;
				}
				else if
				(
					(
						tri.Edges[0].SharedEdgeCoordinate.TrianglesIndex == hit.TriangleIndex &&
						tri.Edges[0].SharedEdgeCoordinate.ComponentIndex == hit.EdgeIndex
					) ||
					(
						tri.Edges[1].SharedEdgeCoordinate.TrianglesIndex == hit.TriangleIndex &&
						tri.Edges[1].SharedEdgeCoordinate.ComponentIndex == hit.EdgeIndex
					) ||
										(
						tri.Edges[2].SharedEdgeCoordinate.TrianglesIndex == hit.TriangleIndex &&
						tri.Edges[2].SharedEdgeCoordinate.ComponentIndex == hit.EdgeIndex
					)
				)
				{
					return true;
				}
			}
			
			if (hit.VertIndex > -1)
			{
				if (Triangles[hit.TriangleIndex].Verts[hit.VertIndex].HasSharedVertViaTriIndex(tri.Index_inCollection))
				{
					return true;
				}
			}
			return false;
		}

		public bool HitIsOnTriPerimeter_extrapolated_dbg(LNX_NavmeshHit hit, LNX_Triangle tri, ref LNX_MethodDebugReport rprt)
		{
			rprt.StartMethod($"HitIsOnTriPerimeter_extrapolated_dbg(hit: '{hit}', tri: '{tri}')");

			if (hit.TriangleIndex == tri.Index_inCollection)
			{
				rprt.Log_And_End_Method($"hit tri index is same as passed triangle's index. Returning true...");

				return true;
			}

			if (hit.EdgeIndex > -1)
			{
				rprt.Log($"is on edge...");

				if
				(
					(
						tri.Edges[0].SharedEdgeCoordinate.TrianglesIndex == hit.TriangleIndex &&
						tri.Edges[0].SharedEdgeCoordinate.ComponentIndex == hit.EdgeIndex
					) ||
					(
						tri.Edges[1].SharedEdgeCoordinate.TrianglesIndex == hit.TriangleIndex &&
						tri.Edges[1].SharedEdgeCoordinate.ComponentIndex == hit.EdgeIndex
					) ||
										(
						tri.Edges[2].SharedEdgeCoordinate.TrianglesIndex == hit.TriangleIndex &&
						tri.Edges[2].SharedEdgeCoordinate.ComponentIndex == hit.EdgeIndex
					)
				)
				{
					rprt.Log_And_End_Method($"returning true...");
					return true;
				}
			}
			else if (hit.VertIndex > -1)
			{
				rprt.Log($"hit is on a vertex...");

				if( Triangles[hit.TriangleIndex].Verts[hit.VertIndex].HasSharedVertViaTriIndex(tri.Index_inCollection) )
				{
					rprt.Log_And_End_Method($"returning true...");
					return true;
				}
			}

			rprt.Log_And_End_Method($"made it to end. Returning false...");
			return false;
		}
		#endregion

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
				Debug.Log( Triangles[i].GetRelationalString(this) );
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

			//Debug.Log($"triangles length: '{Triangles.Length}', drawVisualizationMesh: '{drawVisualizationMesh}' " +
				//$"vismesh null: '{_VisualizationMesh == null}'");

			if( drawVisualizationMesh && _VisualizationMesh != null && _VisualizationMesh.vertices != null && 
				_VisualizationMesh.vertices.Length > 0 )
			{
				//Debug.Log("got here");
				Gizmos.color = color_visualMesh;
				Gizmos.DrawMesh( _VisualizationMesh );
			}
		}
#endif

	}
}