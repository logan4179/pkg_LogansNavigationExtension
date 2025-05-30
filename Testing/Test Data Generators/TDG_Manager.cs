using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.VirtualTexturing;

namespace LogansNavigationExtension
{
    public class TDG_Manager : MonoBehaviour
    {
		public string LastWriteTime;

		[Header("REFERENCE")]
		[SerializeField] private LNX_NavMesh _serializedNavmeshModel;
		[SerializeField] private LNX_NavMesh _sceneGeneratedNavmeshModel;

		public LNX_NavmeshFullDataSaver _sceneGeneratedNavmeshDataSaver;

		[Header("NavMesh Triangulation")]
		public int[] areas;
		public int[] indices;
		public Vector3[] vertices;

		//Dir paths....
		public static string dirPath_testingFolder = $"{Directory.GetCurrentDirectory()}\\Packages\\LogansNavigationExtension\\Testing";
		public static string dirPath_testDataFolder = Path.Combine( dirPath_testingFolder, "Unit Tests\\Test Data" );

		[Header("FILE")]
		public static string filePath_serializedLnxNavMesh = Path.Combine(dirPath_testDataFolder, "serializedNavmesh_A");

		public static string filePath_sceneGeneratedLnxNavMesh = Path.Combine(dirPath_testDataFolder, "sceneGeneratedNavmesh_A");
		public static string filePath_sceneGeneratedNavmeshDataModel = Path.Combine(dirPath_testDataFolder, "sceneGeneratedNavmeshData_A");


		[Header("TESTERS")]

		public TDG_SamplePosition _tdg_samplePosition;
		public static string filePath_testData_SamplePosition = Path.Combine( dirPath_testDataFolder, "tdg_samplePosition_data_B.json" );

		public TDG_Pathing _tdg_pathing;
		public static string filePath_testData_pathing = $"{dirPath_testDataFolder}\\tdg_pathing_data_A.json";

		public TDG_SampleClosestPtOnPerimeter _tdg_sampleClosestPtOnPerim;
		public static string filePath_testData_sampleClosestPtOnPerim = $"{dirPath_testDataFolder}\\tdg_sampleClosestOnPerimeter_data_A.json";

		public TDG_pointingAndGrabbing _tdg_pointingAndGrabbing;
		public static string filePath_testData_pointingAndGrabbing = $"{dirPath_testDataFolder}\\tdg_pointingAndGrabbing_data_A.json";

		public TDG_DeleteTests _tdg_deleteTests;
		public static string filePath_testData_deleteTests = $"{dirPath_testDataFolder}\\tdg_deleteTests_data_A.json";

		public TDG_Cutting _tdg_cuttingTests;
		public static string filePath_testData_cuttingTests = $"{dirPath_testDataFolder}\\tdg_cuttingTests_data_A.json";

		[Header("DEBUG")]
		public bool AmDebugging = true;

		private void OnEnable()
		{
			Debug.Log($"{nameof(TDG_Manager)} onenable");
			logWarning = true;
		}

		private void OnDrawGizmos()
		{
			if (!AmDebugging)
			{
				return;
			}

			for (int i = 0; i < vertices.Length; i++)
			{
				Gizmos.DrawSphere(vertices[i], 0.1f);
			}
		}

		[ContextMenu("z - FetchNavmeshTriangulation()")]
		public void FetchNavmeshTriangulation()
		{
			Debug.Log($"{nameof(FetchNavmeshTriangulation)}()...");

			NavMeshTriangulation tringltn = NavMesh.CalculateTriangulation();

			areas = tringltn.areas;
			indices = tringltn.indices;
			vertices = tringltn.vertices;
		}

		[ContextMenu("z call WriteSerializedLnxMeshToJson()")]
		public void WriteSerializedLnxMeshToJson()
		{
			if ( !Directory.Exists(dirPath_testDataFolder) )
			{
				Debug.LogWarning($"directory: '{dirPath_testDataFolder}' wasn't found.");
				return;
			}

			if ( _serializedNavmeshModel.HaveModifications() )
			{
				Debug.LogError($"ERROR! LNX_NavMesh has modifications. Right now I'm not planning on using modifications on the serialized json LNX_Navmesh.");
				return;
			}

			if ( File.Exists(filePath_serializedLnxNavMesh) )
			{
				Debug.LogWarning($"overwriting existing file at: '{filePath_serializedLnxNavMesh}'");
			}
			else
			{
				Debug.Log($"writing new file at: '{filePath_serializedLnxNavMesh}'");
			}

			File.WriteAllText( filePath_serializedLnxNavMesh, JsonUtility.ToJson(_serializedNavmeshModel, true) );

			LastWriteTime = System.DateTime.Now.ToString();
		}

		[ContextMenu("z call WriteSceneGeneratedNavmeshDataToJson()")]
		public void WriteSceneGeneratedMeshDataToJson()
		{
			if (!Directory.Exists(dirPath_testDataFolder))
			{
				Debug.LogWarning($"directory: '{dirPath_testDataFolder}' wasn't found.");
				return;
			}

			if ( _sceneGeneratedNavmeshDataSaver._Lnx_Navmesh.HaveModifications() )
			{
				Debug.LogError($"ERROR! LNX_NavMesh has modifications. Right now I'm not planning on using modifications on the serialized json LNX_Navmesh.");
				return;
			}

			_sceneGeneratedNavmeshDataSaver.CacheNonSerializedData();

			if( _sceneGeneratedNavmeshDataSaver._Lnx_Navmesh == null )
			{
				Debug.LogError($"ERROR! LNX_NavMesh was null. Returning early...");
				return;
			}

			if ( _sceneGeneratedNavmeshDataSaver._Mesh_Vertices == null || _sceneGeneratedNavmeshDataSaver._Mesh_Vertices.Length <= 0 )
			{
				Debug.LogError($"ERROR! Mesh vertices array not set. Returning early...");
				return;
			}

			if ( _sceneGeneratedNavmeshDataSaver._Mesh_Triangles == null || _sceneGeneratedNavmeshDataSaver._Mesh_Triangles.Length <= 0 )
			{
				Debug.LogError($"ERROR! Mesh triangles array not set. Returning early...");
				return;
			}

			#region write data saver--------------------------------------------------------------------------------
			if ( File.Exists(filePath_sceneGeneratedNavmeshDataModel) )
			{
				Debug.LogWarning($"overwriting existing file at: '{filePath_sceneGeneratedNavmeshDataModel}'");
			}
			else
			{
				Debug.Log($"writing new file at: '{filePath_sceneGeneratedNavmeshDataModel}'");
			}

			File.WriteAllText(filePath_sceneGeneratedNavmeshDataModel, JsonUtility.ToJson(_sceneGeneratedNavmeshDataSaver, true) );
			#endregion

			#region Write lnx navmesh ---------------------------------------------------------------
			if ( File.Exists(filePath_sceneGeneratedLnxNavMesh) )
			{
				Debug.LogWarning($"overwriting existing file at: '{filePath_sceneGeneratedLnxNavMesh}'");
			}
			else
			{
				Debug.Log($"writing new file at: '{filePath_sceneGeneratedLnxNavMesh}'");
			}

			File.WriteAllText(filePath_sceneGeneratedLnxNavMesh, JsonUtility.ToJson(_sceneGeneratedNavmeshDataSaver._Lnx_Navmesh, true));
			#endregion

			LastWriteTime = System.DateTime.Now.ToString();
		}

		[SerializeField, Tooltip("this flag is ideally set by the script and not through the editor. It triggers a warning prompt when you try to update/write all")] 
		bool logWarning = true;

		[ContextMenu("z call WriteAll()")]
		public void UpdateAndWriteAll()
		{
			if (logWarning) 
			{
				Debug.LogWarning($"Warning! this method is ideally meant for when you've changed the navmesh, or there's " +
					$"been a significant change to the methods that sample the navmesh, and you want to re-write all the data. " +
					$"If you really wish to proceed, call this method again...");
				logWarning = false;

				return;
			}
			else
			{
				if (_serializedNavmeshModel.HaveModifications())
				{
					Debug.LogError($"ERROR! LNX_NavMesh has modifications. Right now I'm not planning on using modifications on the serialized json LNX_Navmesh.");
					return;
				}

				WriteSerializedLnxMeshToJson();
				WriteSceneGeneratedMeshDataToJson();

				#region SAMPLE POSITION ---------------------------------------------------------------------
				_tdg_samplePosition.GenerateHItResultCollections();

				if( _tdg_samplePosition.hitPositions == null || _tdg_samplePosition.hitPositions.Length <= 0 || 
					_tdg_samplePosition.triCenters == null || _tdg_samplePosition.triCenters.Length <= 0 )
				{
					Debug.LogError($"ERROR! Apparently something failed in {nameof(_tdg_samplePosition)}. returning early...");
					return;
				}
				else
				{
					if( !_tdg_samplePosition.WriteMeToJson() )
					{
						Debug.LogError($"write to json didn't work on {nameof(_tdg_samplePosition)}. Returning early...");
						return;
					}
				}
				#endregion

				#region PATHING ---------------------------------------------------
				//todo: implement when this tdg works...
				#endregion

				#region CLOSEST POINT ON PERIMETER ---------------------------------------------------
				_tdg_sampleClosestPtOnPerim.GenerateHItResultCollections();

				if (_tdg_sampleClosestPtOnPerim.hitPositions == null || _tdg_sampleClosestPtOnPerim.hitPositions.Length <= 0 ||
					_tdg_sampleClosestPtOnPerim.triCenters == null || _tdg_sampleClosestPtOnPerim.triCenters.Length <= 0)
				{
					Debug.LogError($"ERROR! Apparently something failed in {nameof(_tdg_sampleClosestPtOnPerim)}. returning early...");
					return;
				}
				else
				{
					if (!_tdg_sampleClosestPtOnPerim.WriteMeToJson())
					{
						Debug.LogError($"write to json didn't work on {nameof(_tdg_sampleClosestPtOnPerim)}. Returning early...");
						return;
					}
				}
				#endregion

				#region POINTING AND GRABBING ----------------------------------------------------------
				if ( !_tdg_pointingAndGrabbing.WriteMeToJson() )
				{
					Debug.LogError( $"write to json didn't work on {nameof(_tdg_pointingAndGrabbing)}. Returning early..." );
					return;
				}
				#endregion

				#region DELETING ----------------------------------------------------------
				if ( !_tdg_deleteTests.WriteMeToJson() )
				{
					Debug.LogError($"write to json didn't work on {nameof(_tdg_deleteTests)}. Returning early...");
					return;
				}
				#endregion

				#region CUTTING ----------------------------------------------------------
				if ( !_tdg_cuttingTests.WriteMeToJson() )
				{
					Debug.LogError($"write to json didn't work on {nameof(_tdg_cuttingTests)}. Returning early...");
					return;
				}
				#endregion

				logWarning = true;
			}
		}

		public static bool WriteTestObjectToJson( string filePath, Object testObject )
		{
			if ( !Directory.Exists(dirPath_testDataFolder) )
			{
				Debug.LogWarning($"directory: '{dirPath_testDataFolder}' wasn't found.");
				return false;
			}

			if ( File.Exists(filePath) )
			{
				Debug.LogWarning($"overwriting existing file at: '{filePath}'");
			}
			else
			{
				Debug.Log($"writing new file at: '{filePath}'");
			}

			File.WriteAllText( filePath, JsonUtility.ToJson(testObject, true) );

			return true;
		}
	}
}
