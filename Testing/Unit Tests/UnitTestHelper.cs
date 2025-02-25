using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.VirtualTexturing;

namespace LogansNavigationExtension
{
    public class UnitTestHelper : MonoBehaviour
    {
		[Header("REFERENCE")]
		[SerializeField] private LNX_NavMesh _lnxNavmesh;

		[Header("FILE")]
		[SerializeField] string fileName_lnxNavMesh;


		[Header("NavMesh Triangulation")]
		public int[] areas;
		public int[] indices;
		public Vector3[] vertices;

		[Header("DEBUG")]
		public bool AmDebugging = true;

		void Start()
        {
			
        }

        void Update()
        {
        
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

		[ContextMenu("z call WiteLnxMeshToJson()")]
		public void WiteLnxMeshToJson()
		{
			string constructedFilePath = $"{Directory.GetCurrentDirectory()}\\Packages\\LogansNavigationExtension\\Testing\\Unit Tests\\Test Data";

			if ( !Directory.Exists(constructedFilePath) )
			{
				Debug.LogWarning($"directory: '{constructedFilePath}' wasn't found.");
				return;
			}

			constructedFilePath = Path.Combine(constructedFilePath, $"{fileName_lnxNavMesh}.json");

			if ( File.Exists(constructedFilePath) )
			{
				Debug.LogWarning($"overwriting existing file at: '{constructedFilePath}'");
			}
			else
			{
				Debug.Log($"writing new file at: '{constructedFilePath}'");

			}

			File.WriteAllText( constructedFilePath, JsonUtility.ToJson(_lnxNavmesh, true) );
		}
	}
}
