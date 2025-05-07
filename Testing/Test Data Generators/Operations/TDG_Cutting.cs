using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_Cutting : MonoBehaviour
    {
		public string LastWriteTime;

		public LNX_MeshManipulator _Lnx_MeshManipulator;

		public Ray _Ray;


		public List<Vector3> TestMousePositions_edge;
		public List<Vector3> TestMouseDirections_edge;

		public List<Vector3> GrabbedMidPositions_edge;

		[ContextMenu("z call ClearCollections()")]
		public void ClearCollections()
		{
			TestMousePositions_edge = new List<Vector3>();
			TestMouseDirections_edge = new List<Vector3>();
			GrabbedMidPositions_edge = new List<Vector3>();
		}

		public void CaptureMouseInfo( Vector3 pos, Vector3 dir )
		{
			Debug.Log($"capturing pos '{pos}', and dir: '{dir}'. mode: '{_Lnx_MeshManipulator.SelectMode}'...");

			if ( _Lnx_MeshManipulator.SelectMode == LNX_SelectMode.Edges )
			{
				if ( _Lnx_MeshManipulator.Edge_CurrentlyPointingAt == null )
				{
					Debug.LogError("captured null...");
				}
				else
				{
					TestMousePositions_edge.Add( pos );
					TestMouseDirections_edge.Add( dir );

					_Lnx_MeshManipulator.TryGrab();
					GrabbedMidPositions_edge.Add( _Lnx_MeshManipulator.Edge_LastSelected.MidPosition );

					Debug.Log($"Grabbed: '{_Lnx_MeshManipulator.Edge_LastSelected.MyCoordinate}', with total selected: " +
						$"'{_Lnx_MeshManipulator.Edges_currentlySelected.Count}'. Midpoint: '{_Lnx_MeshManipulator.Edge_LastSelected.MidPosition}'");

				}
			}
			else
			{
				Debug.LogError($"Error! change mesh manipulator select mode to edge. Returning early...");
				return;
			}
		}

		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			bool rslt = TDG_Manager.WriteTestObjectToJson(TDG_Manager.filePath_testData_cuttingTests, this);

			if (rslt)
			{
				LastWriteTime = System.DateTime.Now.ToString();
				return true;

			}

			return false;
		}

		[ContextMenu("z call RecreateMeFromJson()")]
		public void RecreateMeFromJson()
		{
			if ( !File.Exists(TDG_Manager.filePath_testData_cuttingTests) )
			{
				Debug.LogError($"path '{TDG_Manager.filePath_testData_cuttingTests}' didn't exist. returning early...");
				return;
			}

			string myJsonString = File.ReadAllText(TDG_Manager.filePath_testData_cuttingTests);

			JsonUtility.FromJsonOverwrite(myJsonString, this);

			EditorUtility.SetDirty(this);
		}
	}
    
}
