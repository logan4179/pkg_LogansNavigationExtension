
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.DeviceSimulation;
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
			for ( int i = 0; i < constructedAtomicTris.Count; i++ )
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

			DateTime dt = DateTime.Now;
			Refresh(true);
			Debug.Log($"operation took: '{DateTime.Now.Subtract(dt).TotalSeconds}' seconds...");
		}

		public void Refresh( bool meshContinuityHasChanged ) //NEW
		{
			Debug.Log($"{nameof(Refresh)}()---------------------------");

			//Debug.Log($"now looping through '{Triangles.Length}' triangles...");

			DateTime dt_start = DateTime.Now;

			CalculateBounds(); //This needs to happen now before the triangles refresh because the creation of the vert relationships relies on CalculatePath(), which relies on knowing the bounds in order to short-circuit

			//int nmbrFnshdLoops = 0;

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

		#region TRYPROJECTTHROUGHS =============================================
		private bool TryProjectThrough(LNX_NavmeshHit startHit, LNX_NavmeshHit endHit)
		{
			#region SHORT-CIRCUITING =======================================
			if (startHit.TriangleIndex == endHit.TriangleIndex)
			{
				return true;
			}
			#endregion

			LNX_Triangle currentTri = Triangles[startHit.TriangleIndex];
			LNX_NavmeshHit currentStartHit = startHit;
			int currentEdgeIndex = -1;

			int safetyTimeout = Triangles.Length;
			int runningWhileIterations = 0;

			bool amStillProjecting = true;
			while (amStillProjecting)
			{
				LNX_NavmeshHit edgePerimHit = LNX_NavmeshHit.None;
				if ( !currentTri.ProjectThroughToPerimeter(currentStartHit, endHit, out edgePerimHit) )
				{
					return false;
				}

				LNX_Edge hitEdge = currentTri.Edges[edgePerimHit.EdgeIndex];
				currentEdgeIndex = edgePerimHit.EdgeIndex;
				currentStartHit = edgePerimHit;

				if (hitEdge.AmTerminal)
				{
					amStillProjecting = false;
				}
				else
				{
					if
					(
						hitEdge.SharedEdgeCoordinate.TrianglesIndex == endHit.TriangleIndex ||
						(currentTri.AmAdjacentToTri(Triangles[endHit.TriangleIndex]) && currentTri.IsPositionOnAnyEdge(endHit.Position))
					)
					{
						currentTri = Triangles[endHit.TriangleIndex];

						amStillProjecting = false;
					}
					else
					{
						currentTri = Triangles[hitEdge.SharedEdgeCoordinate.TrianglesIndex];
						currentEdgeIndex = hitEdge.SharedEdgeCoordinate.ComponentIndex;
						currentStartHit = edgePerimHit;
					}
				}

				runningWhileIterations++;
				if (runningWhileIterations > safetyTimeout)
				{
					Debug.LogError($"while loop went for more than '{safetyTimeout}' iterations. Breaking early...");
					amStillProjecting = false;

					return false;
				}
			}

			return currentTri.Index_inCollection == endHit.TriangleIndex;
		}
		#endregion

		#region RAYCASTS ==========================================================================================
		/// <summary>
		/// Traces a line between two points on a navmesh.
		/// </summary>
		/// <returns>True if the ray is terminated before reaching target position. Otherwise returns false.</returns>
		public bool Raycast( LNX_NavmeshHit lnxStartHit, LNX_NavmeshHit lnxEndHit ) //todo: Unit test!!!
		{
			//DBGRaycast = "";

			#region SHORT-CIRCUITING ==================================================
			if (lnxStartHit.TriangleIndex == lnxEndHit.TriangleIndex) //If start and end hit are on same triangle...
			{
				return false;
			}

			if
			(
				Triangles[lnxStartHit.TriangleIndex].HasIndexInKnownFullyVisibleList(lnxEndHit.TriangleIndex) ||
				Triangles[lnxEndHit.TriangleIndex].HasIndexInKnownFullyVisibleList(lnxStartHit.TriangleIndex)
			)
			{
				return false;
			}
			#endregion

			string s = "";
			return !TryProjectThrough( lnxStartHit, lnxEndHit );
		}

		/// <summary>
		/// Traces a line between two points on a navmesh.
		/// </summary>
		/// <returns>True if the ray is terminated before reaching target position. Otherwise returns false.</returns>
		public bool Raycast(LNX_NavmeshHit startHit, LNX_NavmeshHit endHit, out LNX_Path outPath )
		{
			Debug.Log($"Raycast_dbg(startHit: '{startHit}', endHit: '{endHit}')");

			#region SHORT-CIRCUITING ==================================================
			if (startHit.TriangleIndex == endHit.TriangleIndex) //If start and end hit are on same triangle...
			{
				outPath = new LNX_Path(GetSurfaceProjectionVector(), startHit, endHit);
				return false;
			}

			if
			(
				Triangles[startHit.TriangleIndex].HasIndexInKnownFullyVisibleList(endHit.TriangleIndex) ||
				Triangles[endHit.TriangleIndex].HasIndexInKnownFullyVisibleList(startHit.TriangleIndex)
			)
			{
				outPath = new LNX_Path(GetSurfaceProjectionVector(), startHit, endHit);
				return false;
			}

			if (startHit.Position == endHit.Position)
			{
				outPath = new LNX_Path(GetSurfaceProjectionVector(), startHit, endHit);
				return false;
			}
			#endregion

			//todo: instead of using FlatHitPosition(startHit) below, cache this value and efficiency test to see if it's worth it
			// todo: also, a little bit lower, there's a line saying [Vector3 vProject = FlatVector( endHit.Position - startHit.Position ).normalized;],
			// try pre-caching this as well and efficiency testing

			if (startHit.VertIndex > -1)
			{
				//TODO: could we add another short-circuit here that checks if both the start and end hits are on a vert, and if so, if these verts are shared by a common
				//triangle? It would effectively be similar to the first short-circuit check above in that we would treat the hits as though theyre both on the same tri

				Vector3 vProject = FlatVector(endHit.Position - startHit.Position).normalized;

				LNX_ComponentCoordinate rel = Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].GetVertCoord_viaProjectionSweep(
					vProject, true );

				if (rel.TrianglesIndex != startHit.TriangleIndex || rel.ComponentIndex != startHit.VertIndex)
				{
					if (rel == LNX_ComponentCoordinate.None)
					{
						if (!VertIsOnTerminalEdge(startHit.TriangleIndex, startHit.VertIndex))
						{
							Debug.LogError($"LNX_ERROR! Raycast startHit was on a vert, but couldn't get adjusted vert coord via projection sweep. Returning early...");
						}

						outPath = LNX_Path.None;
						return true;
					}
					else
					{
						startHit = new LNX_NavmeshHit(
							startHit.Position, GetSurfaceProjectionVector(),
							rel.TrianglesIndex,
							rel.ComponentIndex,
							-1
						);
					}
				}
			}

			#region PROJECT THROUGH TO END HIT ==================================
			outPath = new LNX_Path(this);
			outPath.AddPoint(startHit);

			LNX_NavmeshHit currentStartHit = startHit;
			int safetyTimeout = Triangles.Length;
			int runningWhileIterations = 0;

			bool amStillProjecting = true;

			//rprt.StartMethod("While(amStillProjecting)...");
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

				if (triPerimHit.Position != outPath.StartHit.Position) //need this check, otherwise a raycast starting on a vert can create an unnecessary path point that is the same as the alread-logged starthit
				{
					outPath.AddPoint(triPerimHit);
				}

				if (triPerimHit.EdgeIndex > -1) //note: what about hits that are on a vert? They will have an edgeindex of -1, so this block won't get triggered
				{
					if (Triangles[triPerimHit.TriangleIndex].Edges[triPerimHit.EdgeIndex].AmTerminal) //if we've hit a wall...
					{
						return true;
					}
					else if (
						triPerimHit.TriangleIndex == endHit.TriangleIndex ||
						(
							Triangles[triPerimHit.TriangleIndex].AmAdjacentToTri(Triangles[endHit.TriangleIndex]) && //this is called first for short-circuiting efficiency
							Triangles[triPerimHit.TriangleIndex].IsPositionOnAnyEdge(endHit.Position)
						)
					)
					{
						if (endHit.Position != triPerimHit.Position) //In case the end position is actually on the perimeter of the destination tri...
						{
							outPath.AddPoint(endHit);
						}

						return false;
					}
				}

				if (Vector3.Distance(triPerimHit.Position, endHit.Position) < 0.001f) //if the projection is close enough...
				{
					return false;
				}

				currentStartHit = triPerimHit;

				runningWhileIterations++;
				if (runningWhileIterations > safetyTimeout)
				{
					Debug.LogError($"while loop went for more than '{safetyTimeout}' iterations. Breaking early...");
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

			if
			(
				Triangles[startHit.TriangleIndex].HasIndexInKnownFullyVisibleList(endHit.TriangleIndex) ||
				Triangles[endHit.TriangleIndex].HasIndexInKnownFullyVisibleList(startHit.TriangleIndex)
			)
			{
				outPath = new LNX_Path(GetSurfaceProjectionVector(), startHit, endHit);
				rprt.Log_And_End_Method("start and end hit triangles have each other in fullyknownvisible list. Returning already-calculated relational paths...");
				return false;
			}

			if( startHit.Position == endHit.Position )
			{
				outPath = new LNX_Path(GetSurfaceProjectionVector(), startHit, endHit);
				rprt.Log_And_End_Method("start and end hit are in same  position. Returning already-calculated relational paths...");
				return false;
			}
			#endregion

			//todo: instead of using FlatHitPosition(startHit) below, cache this value and efficiency test to see if it's worth it
			// todo: also, a little bit lower, there's a line saying [Vector3 vProject = FlatVector( endHit.Position - startHit.Position ).normalized;],
			// try pre-caching this as well and efficiency testing

			rprt.Log($"no short-circuit. Proceding...");

			if ( startHit.VertIndex > -1 )
			{
				//TODO: could we add another short-circuit here that checks if both the start and end hits are on a vert, and if so, if these verts are shared by a common
				//triangle? It would effectively be similar to the first short-circuit check above in that we would treat the hits as though theyre both on the same tri
				rprt.Log($"start hit lies on vert: '{startHit.VertIndex}'...", 
					"Checking if start vert touches end tri...");

				if 
				( 
					Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].SharesVertSpaceWithTri(
					Triangles[endHit.TriangleIndex]) 
				) //note: this is a pretty rare case, but it does happen. Especially through ping operation
				{
					rprt.Log($"start vert DOES indeed lie on endtri. Path end point can be assumed...");
					outPath = new LNX_Path(GetSurfaceProjectionVector(), startHit, endHit);
					return false;
				}

				Vector3 vProject = FlatVector(endHit.Position - startHit.Position).normalized;

				rprt.Log($"start hit lies on vert: '{startHit.VertIndex}'. Checking if hit needs to be adjusted based on start to end projection...");

				LNX_ComponentCoordinate rel = Triangles[startHit.TriangleIndex].Verts[startHit.VertIndex].GetVertCoord_viaProjectionSweep_dbg( 
					vProject, true, ref rprt );

				if( rel.TrianglesIndex == startHit.TriangleIndex && rel.ComponentIndex == startHit.VertIndex )
				{
					rprt.Log($"sweep decided that projection WAS already on the correct vert...");
				}
				else
				{
					rprt.Log($"Sweep decided it needed to adjust startHIt. Checking which vert the starthit should be adjusted to...");

					if (rel == LNX_ComponentCoordinate.None)
					{
						rprt.Log($"Got 'None' relationship...");
						if( VertIsOnTerminalEdge(startHit.TriangleIndex, startHit.VertIndex))
						{
							rprt.Log($"This vert is on a terminal edge. Assuming raycast is projected toward outside into terminal space. Returning true...");
						}
						else //<<<<<<<<<<<<<<<<<<<<<<<<<
						{
							Debug.LogError($"LNX_ERROR! Raycast startHit: ('{startHit}') was on a non-terminal vert, but couldn't get adjusted vert coord via projection sweep. " +
								$"This shouldn't happen on a non-terminal vert. Maybe the relational information is incorrect or needs to be reloaded. Returning early...");
							rprt.Log_And_End_Method($"Problem! Got none relationship. Returning true...");
						}

						outPath = LNX_Path.None;
						return true;
					}
					else
					{
						rprt.Log($"got rel: '{rel}' from projectionsweep...");

						startHit = new LNX_NavmeshHit(
							startHit.Position, GetSurfaceProjectionVector(),
							rel.TrianglesIndex,
							rel.ComponentIndex,
							-1
						);

						rprt.Log($"adjusted starthit to: '{startHit}'...");
					}
				}
			}

			#region PROJECT THROUGH TO END HIT ==================================
			outPath = new LNX_Path( this );
			outPath.AddPoint( startHit );
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
				rprt.Log($"LNX_Triangle.ProjectThroughToPerimeter() got perimeter hit: '{triPerimHit}'...");

				if ( triPerimHit.TriangleIndex == currentStartHit.TriangleIndex)
				{
					rprt.Log_And_End_Method($"perimeter hit was on the same triangle as currentStartHit. Chain must have failed. Returning early...");
					return true;
				}

				rprt.Log($"LNX_Triangle.ProjectThroughToPerimeter() WAS succesful. Adding the perimeter hit: '{triPerimHit}' to outPath...");

				if ( triPerimHit.Position != outPath.StartHit.Position ) //need this check, otherwise a raycast starting on a vert can create an unnecessary path point that is the same as the alread-logged starthit
				{
					rprt.Log($"adding triPerimHit: '{triPerimHit}' to path...");
					outPath.AddPoint( triPerimHit );
				}

				if( triPerimHit.EdgeIndex > -1) //note: what about hits that are on a vert? They will have an edgeindex of -1, so this block won't get triggered
				{
					if ( Triangles[triPerimHit.TriangleIndex].Edges[triPerimHit.EdgeIndex].AmTerminal ) //if we've hit a wall...
					{
						rprt.Log($"hitEdge was terminal. This means we hit a wall. Returning true...");
						rprt.EndMethod("Raycast_dbg()");
						return true;
					}
					else if (
						triPerimHit.TriangleIndex == endHit.TriangleIndex ||
						(
							Triangles[triPerimHit.TriangleIndex].AmAdjacentToTri(Triangles[endHit.TriangleIndex]) && //this is called first for short-circuiting efficiency
							Triangles[triPerimHit.TriangleIndex].IsPositionOnAnyEdge(endHit.Position)
						)
					)
					{
						rprt.Log($"found that hitEdge was on same triangle as endHIt.",
							"This means the while-loop should end here. Adding endhit to path...");

						if (endHit.Position != triPerimHit.Position) //In case the end position is actually on the perimeter of the destination tri...
						{
							rprt.Log($"adding endHit: '{endHit}' to path...");
							outPath.AddPoint(endHit);
						}
						rprt.Log($"Now path has: '{outPath.PathPoints.Count}' points. Returning false...");

						rprt.EndMethod("Raycast_dbg()");
						return false;
					}
				}

				if (Vector3.Distance(triPerimHit.Position, endHit.Position) < 0.001f) //if the projection is close enough...
				{
					rprt.Log($"edgePerimHit was very close to endHIt position. Returning false...");
					rprt.EndMethod("Raycast_dbg()");
					return false;
				}

				currentStartHit = triPerimHit;

				runningWhileIterations++;
				if (runningWhileIterations > safetyTimeout)
				{
					Debug.LogError($"while loop went for more than '{safetyTimeout}' iterations. Breaking early...");
					amStillProjecting = false;
					rprt.Log($"while loop went for more than '{safetyTimeout}' iterations. Breaking early...");
					rprt.EndMethod("Raycast_dbg()");
					return true;
				}
				//rprt.Log_Untabbed($"~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
			}
			//rprt.EndMethod("while(amstillprojecting)");
			#endregion

			rprt.Log($"after while loop. Apparently Projecting through to perimeter didn't work. Returning true as default...");
			rprt.EndMethod("Raycast_dbg()");

			return true;
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
			if (lnxStartHit.TriangleIndex == lnxEndHit.TriangleIndex) //If start and end hit are on same triangle...
			{
				return false;
			}

			if
			(
				Triangles[lnxStartHit.TriangleIndex].HasIndexInKnownFullyVisibleList(lnxEndHit.TriangleIndex) ||
				Triangles[lnxEndHit.TriangleIndex].HasIndexInKnownFullyVisibleList(lnxStartHit.TriangleIndex)
			) 
			{
				return false;
			}
			#endregion

			return Raycast(lnxStartHit, lnxEndHit);

		}

		/// <summary>
		/// Traces a line between two points on a navmesh.
		/// </summary>
		/// <returns>True if the ray is terminated before reaching target position. Otherwise returns false.</returns>
		public bool Raycast(Vector3 sourcePosition, Vector3 targetPosition, float maxSampleDistance, out LNX_Path outPath,
			bool considerOffPerimeter = false) //todo: Unit test!!!
		{
			string s = "";

			LNX_NavmeshHit lnxStartHit = LNX_NavmeshHit.None;
			LNX_NavmeshHit lnxEndHit = LNX_NavmeshHit.None;

			#region SHORT-CIRCUITING ==================================================
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

			Debug.Log($"no short circuits. Now trying atomic raycast with starthit: '{lnxStartHit}', and endhit: '{lnxEndHit}'...");
			return Raycast( lnxStartHit, lnxEndHit, out outPath );
		}

		public bool Raycast_dbg(Vector3 sourcePosition, Vector3 targetPosition, float maxSampleDistance, out LNX_Path outPath, 
			ref LNX_MethodDebugReport rprt, bool considerOffPerimeter = false)
		{
			//rprt.Log($"tablvl: '{rprt.MethodLvl}'");
			rprt.StartMethod($"Raycast_dbg(sourcePosition: '{sourcePosition}', targetPosition: '{targetPosition}')");

			outPath = LNX_Path.None;

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
			if ( !Raycast(startHit, endHit, out outPath) )
			{
				return true;
			}
			else
			{
				float runningClosestDistance = float.MaxValue;

				#region MAKE PLACEHOLDER runningClosestDistance VALUE THAT'S SHORTER THAN MAX VALUE (AND THEREFORE MORE EFFICIENT)=
				if ( 
					Triangles[startHit.TriangleIndex].Verts[0].IsRelationshipCollectionValid() && 
					Triangles[endHit.TriangleIndex].Verts[0].IsRelationshipCollectionValid()
				) 
				{ 
					//IE: If the relationships collections are set, we can cheaply pre-determine a max possible distance that's 
					// shorter than float.maxvalue. Doing this might possibly save some iterations when we do the Pinging operations
					// TODO: I should efficiency test this method with and without this pre-determination, and possibly with multiple 
					//different methods of making the pre-determination, like using Vector.Distance() between the start hit and the first vert
					runningClosestDistance = Triangles[startHit.TriangleIndex].LongestEdgeLength +
					Triangles[startHit.TriangleIndex].Verts[0].GetPathTo(Triangles[endHit.TriangleIndex].Verts[0]).TotalDistance +
					Triangles[endHit.TriangleIndex].LongestEdgeLength;
				}
				#endregion

				List<LNX_Path> visblVrtPths = GetVisibleVertsFromPoint(startHit, false);
				if (visblVrtPths == null || visblVrtPths.Count <= 0)
				{
					return false;
				}

				List<LNX_ComponentCoordinate> vsblBckstpVerts = new List<LNX_ComponentCoordinate>();
				for (int i = 0; i < visblVrtPths.Count; i++)
				{
					vsblBckstpVerts.Add(
						new LNX_ComponentCoordinate(visblVrtPths[i].EndHit.TriangleIndex,
						visblVrtPths[i].EndHit.VertIndex)
					);
				}

				#region CONSTRUCT PATHS -------------------------------------
				LNX_Path[] paths = new LNX_Path[visblVrtPths.Count]; //todo: instead of making a list of paths, try efficiency-testing using a single path object that only gets updated if the next ping returns a shorter path
				int indx_runningBestPath = -1;
				for (int i_visblVrts = 0; i_visblVrts < visblVrtPths.Count; i_visblVrts++)
				{
					paths[i_visblVrts] = Triangles[visblVrtPths[i_visblVrts].EndTriIndex].
						Verts[visblVrtPths[i_visblVrts].EndHit.VertIndex].
						Ping(
						endHit, this, runningClosestDistance, visblVrtPths[i_visblVrts], vsblBckstpVerts
						);
				}
				#endregion

				if (indx_runningBestPath > -1)
				{
					outPath = paths[indx_runningBestPath];
				}
			}

			return true;
		}
		public bool CalculatePath_dbg(LNX_NavmeshHit startHit, LNX_NavmeshHit endHit,
			out LNX_Path outPath, ref LNX_MethodDebugReport rprt)
		{
			rprt.StartMethod($"CalculatePath(startHit: '{startHit}', endHit: '{endHit}'");

			rprt.Log($"first, attempting to raycast to the destination...");
			rprt.StartAbbreviatedMethod("Raycast_dbg()");
			bool rcHitSomething = Raycast_dbg(startHit, endHit, out outPath, ref rprt);
			rprt.EndAbbreviatedMethod("Raycast_dbg()");

			if ( !rcHitSomething)
			{
				rprt.Log_And_End_Method($"Initial raycast was false, meaning that it did NOT hit an obstruction. " +
					$"outPath: '{outPath}'. Returning true...");
				return true;
			}
			else
			{
				rprt.Log($"Initial raycast returned true, meaning it DID hit an obstruction. Commencing " +
					$"with pathfind operation...", 
					$"First, assembling list of boundary verts...");

				float runningClosestDistance = -1;
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

				rprt.Log($"pre-determined initial running closest dist to be: '{runningClosestDistance}'...");
				rprt.Log($"Now checking for which verts are visible from start position...");

				rprt.StartAbbreviatedMethod($"GetVisibleVertsFromPoint_dbg({startHit})");
				List<LNX_Path> visblVrtPths = GetVisibleVertsFromPoint_dbg(startHit, ref rprt, false);
				rprt.EndAbbreviatedMethod($"GetVisibleVertsFromPoint_dbg({startHit})");

				if ( visblVrtPths == null || visblVrtPths.Count <= 0 )
				{
					rprt.Log_And_End_Method($"Something went wrong. GetVisibleVertsFromPoint() returned 0 paths. Returning early...");
					return false;
				}

				rprt.Log($"Decided there are '{visblVrtPths.Count}' visible verts from startHit. Assembling backstop list...\n");
				List<LNX_ComponentCoordinate> vsblBckstpVerts = new List<LNX_ComponentCoordinate>();
				for ( int i = 0; i < visblVrtPths.Count; i++ )
				{
					rprt.Log($"adding visible vert: '{visblVrtPths[i].EndCoordinate_vert}'...");
					vsblBckstpVerts.Add( visblVrtPths[i].EndCoordinate_vert );
				}

				rprt.Log($"Decided there are '{visblVrtPths.Count}' visible verts from startHit. Pinging each visible vert...\n");

				#region CONSTRUCT PATHS -------------------------------------
				LNX_Path[] paths = new LNX_Path[visblVrtPths.Count];
				int indx_runningBestPath = -1;
				for ( int i_visblVrts = 0; i_visblVrts < visblVrtPths.Count; i_visblVrts++ )
				{
					rprt.Log($"for {i_visblVrts}: '{visblVrtPths[i_visblVrts].EndCoordinate_vert}'====================="); 
					//rprt.Log($"rels valid?: " +
						//$"'{GetVertexAtCoordinate(visblVrtPths[i_visblVrts].EndHit.TriangleIndex, visblVrtPths[i_visblVrts].EndHit.VertIndex).IsRelationshipCollectionValid()}'---");
					
					if(visblVrtPths[i_visblVrts].TotalDistance > runningClosestDistance )
					{
						rprt.Log($"this visible vert path has dist: '{visblVrtPths[i_visblVrts].TotalDistance}', which is greather than the runningclosestdist: '{runningClosestDistance}'. Skipping ping...");
						continue;
					}

					
					//rprt.StartAbbreviatedMethod($"Ping_dbg(endPt: '{endHit}', maxdist: '{runningClosestDistance}')");
					
					paths[i_visblVrts] = Triangles[visblVrtPths[i_visblVrts].EndTriIndex].
						Verts[visblVrtPths[i_visblVrts].EndHit.VertIndex].Ping_dbg(
						endHit, this, runningClosestDistance, visblVrtPths[i_visblVrts], ref rprt, vsblBckstpVerts
					);

					//rprt.EndAbbreviatedMethod($"Ping_dbg(endPt: '{endHit}', maxdist: '{runningClosestDistance}')");
					
					rprt.Log($"Got path: '{paths[i_visblVrts]}' with dist: '{paths[i_visblVrts].TotalDistance}'.",
						$"pts: '{(paths[i_visblVrts] == LNX_Path.None ? "None" : paths[i_visblVrts].PointCount)}'",
						$"Checking against runningClosestDistance: '{runningClosestDistance}' to see if this is a new best path...");

					if 
					(
						paths[i_visblVrts] != LNX_Path.None && 
						(runningClosestDistance == -1 || paths[i_visblVrts].TotalDistance < runningClosestDistance) 
					)
					{
						indx_runningBestPath = i_visblVrts;
						runningClosestDistance = paths[i_visblVrts].TotalDistance;
						rprt.Log($"found new runningBestPath with dist: '{paths[i_visblVrts].TotalDistance}'. indx now: '{indx_runningBestPath}'...");
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
					outPath = paths[indx_runningBestPath];
					rprt.Log($"made outPath: '{outPath}'...");
					rprt.Log_And_End_Method($"returning true...");
					return true;
				}
			}

			rprt.Log_And_End_Method($"returning false...");
			return false;
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
			float maxSampleDistance, out LNX_Path outPath, bool considerOffPerimeter = true)
		{
			#region SAMPLE START AND END POINT -------------------------------------------
			LNX_NavmeshHit startHit = new LNX_NavmeshHit();
			LNX_NavmeshHit endHit = new LNX_NavmeshHit();
			outPath = LNX_Path.None;

			if ( !SamplePosition(startPos_passed, out startHit, maxSampleDistance, considerOffPerimeter) )
			{
				return false; //todo: returning a boolean is newly added. Make sure this return boolean is being properly used...
			}

			if ( !SamplePosition(endPos_passed, out endHit, maxSampleDistance, considerOffPerimeter) )
			{
				return false; //todo: returning a boolean is newly added. Make sure this return boolean is being properly used...
			}
			#endregion

			return CalculatePath( startHit, endHit, out outPath );
		}

		public bool CalculatePath(LNX_Vertex startVert, LNX_Vertex endVert, out LNX_Path outPath)
		{
			#region SHORT-CIRCUITING ===================================
			if ( startVert.IsRelationshipCollectionValid() && endVert.IsRelationshipCollectionValid() )
			{
				outPath = startVert.GetPathTo( endVert );
				return true;
			}
			#endregion

			return CalculatePath( 
				new LNX_NavmeshHit(startVert, Triangles[startVert.TriangleIndex].V_PathingNormal), 
				new LNX_NavmeshHit(endVert, Triangles[endVert.TriangleIndex].V_PathingNormal), 
				out outPath 
			);
		}
		public bool CalculatePath_dbg(LNX_Vertex startVert, LNX_Vertex endVert, out LNX_Path outPath, ref LNX_MethodDebugReport rprt) //1 <<<<<<<<<<<<<<<<
		{
			rprt.StartMethod($"CalculatePath(startVert: '{startVert}', endVert: '{endVert}')");

			#region SHORT-CIRCUITING ===================================
			/*
			if (startVert.IsRelationshipCollectionValid() && endVert.IsRelationshipCollectionValid())
			{
				rprt.Log_And_End_Method(
					$"start rels length: '{startVert.Relationships.Length}', end rels length: '{endVert.Relationships.Length}' " +
					$"relationships valid. Getting already-existing cached relational path...");
				outPath = startVert.GetPathTo(endVert);
				return true;
			}
			rprt.Log($"relationships for start and/or end vert not valid...");
			*/
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

		public List<LNX_Path> GetVisibleVertsFromPoint( 
			LNX_NavmeshHit hit, bool includeFringeVerts = false, 
			List<LNX_ComponentCoordinate> excludeVerts = null, float maxDist = -1f
		)
		{
			List<LNX_Path> visibleVertPaths = new List<LNX_Path>();

			for (int i_tris = 0; i_tris < Triangles.Length; i_tris++)
			{
				if (i_tris == hit.TriangleIndex)
				{
					continue;
				}

				for (int i_vrts = 0; i_vrts < 3; i_vrts++)
				{
					#region SHORT-CIRCUITING =======================================
					if (excludeVerts != null && excludeVerts.Count > 0)
					{
						bool foundOne = false;
						for (int j = 0; j < excludeVerts.Count; j++)
						{
							if (Triangles[i_tris].Verts[i_vrts].SharesVertSpace(Triangles[excludeVerts[j].TrianglesIndex].Verts[excludeVerts[j].ComponentIndex]))
							{
								foundOne = true;
								break;
							}
						}

						if (foundOne)
						{
							continue;
						}
					}

					if (visibleVertPaths.Count > 0) //if we've already got at least one logged visible vert path...
					{
						bool foundOneAtSamePos = false;

						for (int i_growingList = 0; i_growingList < visibleVertPaths.Count; i_growingList++)
						{
							//Debug.Log($"i_growingList: '{i_growingList}'. endhit: '{visibleVertPaths[i_growingList].EndHit}'");
							if
							(
								Triangles[i_tris].Verts[i_vrts].SharesVertSpace
								(
									Triangles[visibleVertPaths[i_growingList].EndTriIndex].
									Verts[visibleVertPaths[i_growingList].EndHit.VertIndex]
								)
							)
							{
								foundOneAtSamePos = true;
								break;
							}
						}

						if (foundOneAtSamePos)
						{
							continue;
						}
					}

					if (!includeFringeVerts && IsBoundsVert(i_tris, i_vrts))
					{
						continue;
					}
					#endregion---------------------------------
					LNX_Path path;
					if (!Raycast(
						hit, new LNX_NavmeshHit(Triangles[i_tris].Verts[i_vrts], Triangles[i_tris].V_PathingNormal),
						out path)
					)
					{
						visibleVertPaths.Add(path);
					}
				}
			}

			return visibleVertPaths;
		}
		public List<LNX_Path> GetVisibleVertsFromPoint_dbg(
			LNX_NavmeshHit hit, ref LNX_MethodDebugReport rprt, bool includeFringeVerts = false, 
			List<LNX_ComponentCoordinate> excludeVerts = null, float maxDist = -1f
		)
		{
			rprt.StartMethod( $"GetVisibleVertsFromPoint_dbg(hit: '{hit}', excldCount: " +
				$"'{(excludeVerts == null ? "null" : excludeVerts.Count)}')" );

			List<LNX_Path> visibleVertPaths = new List<LNX_Path>();

			rprt.Log("for-looping through all tris and verts...");
			for (int i_tris = 0; i_tris < Triangles.Length; i_tris++)
			{
				string triString = $"for tri{i_tris}";
				if (i_tris == hit.TriangleIndex)
				{
					rprt.Log( $"{triString}...", true );
					rprt.Log($"same tri index. Continuing...", true);
					continue;
				}

				for (int i_vrts = 0; i_vrts < 3; i_vrts++)
				{
					rprt.Log(triString + $", vert{i_vrts}...", true );

					#region SHORT-CIRCUITING =======================================
					if (excludeVerts != null && excludeVerts.Count > 0)
					{
						bool foundOne = false;
						for (int j = 0; j < excludeVerts.Count; j++)
						{
							if (Triangles[i_tris].Verts[i_vrts].SharesVertSpace(Triangles[excludeVerts[j].TrianglesIndex].Verts[excludeVerts[j].ComponentIndex]))
							{
								rprt.Log($"found that vert[{i_tris}][{i_vrts}] shares space with exclude vert {j}...", true);
								foundOne = true;
								break;
							}
						}

						if (foundOne)
						{
							continue;
						}
					}

					if ( visibleVertPaths.Count > 0 ) //if we've already got at least one logged visible vert path...
					{
						bool foundOneAtSamePos = false;
						rprt.Log($"vvp count: '{visibleVertPaths.Count}'. Checking if already logged...", true);

						for (int i_growingList = 0; i_growingList < visibleVertPaths.Count; i_growingList++)
						{
							//Debug.Log($"i_growingList: '{i_growingList}'. endhit: '{visibleVertPaths[i_growingList].EndHit}'");
							if 
							(
								Triangles[i_tris].Verts[i_vrts].SharesVertSpace
								(
									Triangles[visibleVertPaths[i_growingList].EndTriIndex].
									Verts[visibleVertPaths[i_growingList].EndHit.VertIndex]
								)
							)
							{
								rprt.Log($"There's a vert in growing list of visible already logged at the same position as this " +
									$"vert[{i_tris}][{i_vrts}]. Bypassing...");

								foundOneAtSamePos = true;
								break;
							}
						}

						if (foundOneAtSamePos)
						{
							continue;
						}
					}

					if (maxDist != -1f)
					{
						if (Vector3.Distance(hit.Position, Triangles[i_tris].Verts[i_vrts].V_Position) > maxDist)
						{
							rprt.Log($"distance too far. Bypassing...");
							Debug.Log($"distance from '{hit}' to '{Triangles[i_tris].Verts[i_vrts]}' beyond max: '{maxDist}'. Bypassing...");
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
					if ( !Raycast_dbg( 
						hit, new LNX_NavmeshHit(Triangles[i_tris].Verts[i_vrts], Triangles[i_tris].V_PathingNormal), 
						out path, ref rprt) 
					)
					{
						rprt.Log($"raycast to vert[{i_tris}][{i_vrts}] showed clear path. Adding path to vsblVrtPths. endhit: '{path.EndHit}'...", true);
						visibleVertPaths.Add( path );
					}
					else
					{
						rprt.Log($"raycast from hit: '{hit}' to vert[{i_tris}][{i_vrts}] hit obstruction", true);
						rprt.Log($"end of path: '{(path.PathPoints == null || path.PathPoints.Count <= 0 ? "null" : path.EndHit)}'...", true);

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
			List<LNX_Path> outPaths = new List<LNX_Path>();

			if ( vert.IsRelationshipCollectionValid() )
			{
				for (int i = 0; i < vert.Relationships.Length; i++)
				{
					if (vert.Relationships[i].CanSee)
					{
						outPaths.Add(vert.Relationships[i].PathTo);
					}
				}
			}
			else
			{
				outPaths = GetVisibleVertsFromPoint(
					new LNX_NavmeshHit(
						vert.V_Position, Triangles[vert.TriangleIndex].V_PathingNormal,
						vert.MyCoordinate.TrianglesIndex, vert.MyCoordinate.ComponentIndex, -1
					), includeFringeVerts,
					new List<LNX_ComponentCoordinate>() { vert.MyCoordinate }, maxDist
				);
			}

			return outPaths;
		}
		public List<LNX_Path> GetVisibleVertsFromVert_dbg(LNX_Vertex vert, ref LNX_MethodDebugReport rprt,
			bool includeFringeVerts = false, List<LNX_ComponentCoordinate> excludeVerts = null, float maxDist = -1f)
		{
			rprt.StartMethod($"GetVisibleVertsFromVert_dbg(vert: '{vert}' excludecount: " +
				$"'{(excludeVerts == null ? "null" : excludeVerts.Count)}')");
			List<LNX_Path> outPaths = new List<LNX_Path>();

			if (/* vert.IsRelationshipCollectionValid()*/false ) //todo: bring back this block when we know it works
			{
				rprt.Log( $"relationships valid. Using existing relational information..." );

				for (int i = 0; i < vert.Relationships.Length; i++)
				{
					if (vert.Relationships[i].CanSee)
					{
						outPaths.Add(vert.Relationships[i].PathTo);
					}
				}
			}
			else
			{
				rprt.Log($"relationships collection NOT valid. Now passing off to more atomic version to manually calculate visible verts...");
				outPaths = GetVisibleVertsFromPoint_dbg(
					new LNX_NavmeshHit(
						vert.V_Position, Triangles[vert.TriangleIndex].V_PathingNormal, 
						vert.MyCoordinate.TrianglesIndex, vert.MyCoordinate.ComponentIndex, -1
					), ref rprt, includeFringeVerts, 
					excludeVerts, maxDist
				);
			}

			rprt.EndMethod("GetVisibleVertsFromPoint_dbg");

			return outPaths;
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