using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class Test_MoveComponents : MonoBehaviour
    {
        public LNX_MeshManipulator _Lnx_MeshManipulator;

		[SerializeField] string fileName;

		public Ray _Ray;

		public List<Vector3> TestMousePositions_vert;
		public List<Vector3> TestMouseDirections_vert;
		public List<int> CapturedNumberOfSharedVerts;
		public List<Vector3> GrabbedPositions_vert;
		//public List<Vector3> GrabbedManipulatorPos_vert;
		[Space(10f)]

		public List<Vector3> TestMousePositions_edge;
		public List<Vector3> TestMouseDirections_edge;
		public List<int> CapturedNumberOfSharedEdges;
		public List<int> CapturedNumberOfSharedVerts_edge;
		public List<Vector3> GrabbedMidPositions_edge;
		//public List<Vector3> GrabbedManipulatorPos_edge;
		[Space(10)]

		public List<Vector3> TestMousePositions_face;
		public List<Vector3> TestMouseDirections_face;
		public List<Vector3> GrabbedPositions_face;
		//public List<Vector3> GrabbedManipulatorPos_face;
		public List<int> CapturedNumberOfSharedVerts_face;

		[ContextMenu("z call ClearCollections()")]
		public void ClearCollections()
		{
			TestMousePositions_vert = new List<Vector3>();
			TestMouseDirections_vert = new List<Vector3>();
			CapturedNumberOfSharedVerts = new List<int>();
			GrabbedPositions_vert = new List<Vector3>();

			TestMousePositions_edge = new List<Vector3>();
			TestMouseDirections_edge = new List<Vector3>();
			CapturedNumberOfSharedEdges = new List<int>();
			CapturedNumberOfSharedVerts_edge = new List<int>();
			GrabbedMidPositions_edge = new List<Vector3>();

			TestMousePositions_face = new List<Vector3>();
			TestMouseDirections_face = new List<Vector3>();
			GrabbedPositions_face = new List<Vector3>();
			CapturedNumberOfSharedVerts_face = new List<int>();
		}

		public void CaptureMouseInfo( Vector3 pos, Vector3 dir )
		{
			Debug.Log($"capturing pos '{pos}', and dir: '{dir}'. mode: '{_Lnx_MeshManipulator.SelectMode}'...");

			if ( _Lnx_MeshManipulator.SelectMode == LNX_SelectMode.None )
			{
				Debug.LogError($"Error! change mesh manipulator select mode to something other than 'none'. Returning early...");
				return;
			}

			if ( _Lnx_MeshManipulator.SelectMode == LNX_SelectMode.Vertices )
			{
				if ( _Lnx_MeshManipulator.Vert_CurrentlyPointingAt == null )
				{
					Debug.LogError("captured null...");
				}
				else
				{
					TestMousePositions_vert.Add( pos );
					TestMouseDirections_vert.Add( dir );

					_Lnx_MeshManipulator.TryGrab();
					CapturedNumberOfSharedVerts.Add(_Lnx_MeshManipulator.Verts_currentlySelected.Count);
					GrabbedPositions_vert.Add(_Lnx_MeshManipulator.Vert_LastSelected.Position);
					Debug.Log($"{_Lnx_MeshManipulator.Verts_currentlySelected.Count}");
				}
			}
			else if ( _Lnx_MeshManipulator.SelectMode == LNX_SelectMode.Edges )
			{
				if ( _Lnx_MeshManipulator.Edge_CurrentlyPointingAt == null )
				{
					Debug.LogError("captured null...");
				}
				else
				{
					TestMousePositions_edge.Add(pos);
					TestMouseDirections_edge.Add(dir);

					_Lnx_MeshManipulator.TryGrab();
					CapturedNumberOfSharedEdges.Add(_Lnx_MeshManipulator.Edges_currentlySelected.Count);
					GrabbedMidPositions_edge.Add(_Lnx_MeshManipulator.Edge_LastSelected.MidPosition);
					CapturedNumberOfSharedVerts_edge.Add(_Lnx_MeshManipulator.Verts_currentlySelected.Count);

					Debug.Log($"Grabbed: '{_Lnx_MeshManipulator.Edge_LastSelected.MyCoordinate}', with total selected: " +
						$"'{_Lnx_MeshManipulator.Edges_currentlySelected.Count}'. Midpoint: '{_Lnx_MeshManipulator.Edge_LastSelected.MidPosition}'");

				}
			}
			else if (_Lnx_MeshManipulator.SelectMode == LNX_SelectMode.Faces)
			{
				TestMousePositions_face.Add(pos);
				TestMouseDirections_face.Add(dir);

				Debug.Log(_Lnx_MeshManipulator.Index_TriPointingAt);

				if (_Lnx_MeshManipulator.Index_TriPointingAt < 0)
				{
					GrabbedPositions_face.Add(Vector3.zero);
					CapturedNumberOfSharedVerts_face.Add(0);
					Debug.Log("captured null...");
				}
				else
				{
					_Lnx_MeshManipulator.TryGrab();


					GrabbedPositions_face.Add(
						_Lnx_MeshManipulator._LNX_NavMesh.Triangles[_Lnx_MeshManipulator.Index_TriLastSelected].V_center);

					CapturedNumberOfSharedVerts_face.Add(_Lnx_MeshManipulator.Verts_currentlySelected.Count);
				}
			}
		}

		[ContextMenu("z call WiteMeToJson()")]
		public void WiteMeToJson()
		{
			string filePath = $"{Directory.GetCurrentDirectory()}\\Packages\\LogansNavigationExtension\\Testing\\Unit Tests\\Test Data";

			if (!Directory.Exists(filePath))
			{
				Debug.LogWarning($"directory: '{filePath}' wasn't found.");
				return;
			}

			filePath = Path.Combine(filePath, $"{fileName}.json");

			if (File.Exists(filePath))
			{
				Debug.LogWarning($"overwriting existing file at: '{filePath}'");
			}
			else
			{
				Debug.Log($"writing new file at: '{filePath}'");

			}

			File.WriteAllText(filePath, JsonUtility.ToJson(this, true));
		}

		[ContextMenu("z call RecreateMeFromJson()")]
		public void RecreateMeFromJson()
		{
			string filePath = Path.Combine($"{Directory.GetCurrentDirectory()}\\Packages\\LogansNavigationExtension\\Testing\\Unit Tests\\Test Data",
				$"{fileName}.json");

			if (!File.Exists(filePath))
			{
				Debug.LogError($"path '{filePath}' didn't exist. returning early...");
				return;
			}

			string myJsonString = File.ReadAllText(filePath);

			JsonUtility.FromJsonOverwrite(myJsonString, this);

			EditorUtility.SetDirty(this);
		}
	}
}
