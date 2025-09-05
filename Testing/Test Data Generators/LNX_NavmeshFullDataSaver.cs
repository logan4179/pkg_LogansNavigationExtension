using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace LogansNavigationExtension
{
    /// <summary>
    /// Allows me to save things that aren't serialized.
    /// </summary>
    [System.Serializable]
    public class LNX_NavmeshFullDataSaver
    {
        public LNX_NavMesh _Lnx_Navmesh;

        public Vector3[] _Mesh_Vertices;
        public int[] _Mesh_Triangles;

		public Vector3[] _triangulation_Vertices;
        public int[] _triangulation_indices;
        public int[] _triangulation_areas;

		//[ContextMenu("z call CacheNonSerializedData()")] //can't do this because it's not a monobehavior
		public void CacheNonSerializedData()
        {
            _Mesh_Triangles = _Lnx_Navmesh._VisualizationMesh.triangles;
            _Mesh_Vertices = _Lnx_Navmesh._VisualizationMesh.vertices;

            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
			_triangulation_Vertices = triangulation.vertices;
            _triangulation_indices = triangulation.indices;
			_triangulation_areas = triangulation.areas;

            Debug.Log($"triangulation calculated '{_triangulation_Vertices.Length}' vertices, " +
                $"'{_triangulation_indices.Length}' indices, and '{_triangulation_areas.Length}' areas.");
		}
    }
}
