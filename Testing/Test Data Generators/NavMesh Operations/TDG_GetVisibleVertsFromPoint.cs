using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LogansNavigationExtension
{
    public class TDG_GetVisibleVertsFromPoint : TDG_base
    {
		public LNX_ComponentGrabber Grabber_Hit;

		[Header("RESULTS")]
		public LNX_NavmeshHit ResultHit;
		public List<LNX_ComponentCoordinate> ResultCoordinates;
		public List<LNX_ComponentCoordinate> excludeCoords;

		[Header("DEBUG")]
		public Color Color_lines;


		#region HELPERS -------------------------------------
		[ContextMenu("z call GoToDataPoint")]
		public void GoToDataPoint()
		{

		}

		[ContextMenu("z call DoEet")]
		public void DoEet()
		{
			List<int> tryListA = new List<int>() { 0, 1, 2, 3, 4 };
			List<int> tryListB = tryListA.GetRange(0, tryListA.Count);
			tryListA[0] = 123;
			Debug.Log($"A: '{tryListA[0]}', B: '{tryListB[0]}' bcount: '{tryListB.Count}'");
		}
		#endregion

		protected override void OnDrawGizmos()
		{
			if 
			(
				AmInUnitTest || 
				(
					Selection.activeGameObject != gameObject && 
					Selection.activeGameObject != Grabber_Hit.gameObject
				)
			)
			{
				return;
			}

			base.OnDrawGizmos();

			Grabber_Hit.DrawMyGizmos( Radius_ObjectDebugSpheres );

			if ( Grabber_Hit.RecalculatedLastFrame )
			{
				ResultHit = LNX_NavmeshHit.None;
				ResultCoordinates = new List<LNX_ComponentCoordinate>();
				DBG_Operation = "";
				DBG_Method = "";
				DBG_Operation += "Attempting to sample hit on Navmesh surface...\n";
				if( !_navmesh.SamplePosition(Grabber_Hit.transform.position, out ResultHit, 3f, false, true) )
				{
					DBG_Operation += $"was NOT able to sample hit from this position. Returning early...\n";
					return;
				}

				DBG_Operation += $"\nsampled hit at: '{ResultHit.HitPosition}'. Commencing operation...\n";

				ResultCoordinates = _navmesh.GetVisibleVertsFromPoint(ResultHit, ref DBG_Method, false, excludeCoords);

				DBG_Operation += $"{nameof(ResultCoordinates)} count: '{ResultCoordinates.Count}'\n";
			}


			Gizmos.color = Color_lines;
			float height = 0.5f;
			for( int i = 0; i < ResultCoordinates.Count; i++ )
			{
				Gizmos.DrawLine(
					ResultHit.HitPosition,
					_navmesh.GetVertexAtCoordinate(ResultCoordinates[i]).V_Position
				);

				Gizmos.DrawLine( 
					_navmesh.GetVertexAtCoordinate(ResultCoordinates[i]).V_Position,
					_navmesh.GetVertexAtCoordinate(ResultCoordinates[i]).V_Position + (Vector3.up * height)
				);
			}
		}



		#region WRITING-------------------------------------
		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			bool rslt = TDG_Manager.WriteTestObjectToJson(TDG_Manager.filePath_testData_Raycasting, this);

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
			if (!System.IO.File.Exists(TDG_Manager.filePath_testData_Raycasting))
			{
				Debug.LogError($"path '{TDG_Manager.filePath_testData_Raycasting}' didn't exist. returning early...");
				return;
			}

			string myJsonString = System.IO.File.ReadAllText(TDG_Manager.filePath_testData_Raycasting);

			JsonUtility.FromJsonOverwrite(myJsonString, this);

			EditorUtility.SetDirty(this);
		}
		#endregion
	}
}
