using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
	public class TDG_DoesPositionLieOnEdge : TDG_base
	{

		public LNX_ComponentCoordinate EdgeCoordinate;


		[Header("DATA CAPTURE")]
		public List<Vector3> CapturedPositions = new List<Vector3>();
		public List<bool> CapturedResults = new List<bool>();
		public List<Vector3> CapturedTriCenters = new List<Vector3>();
		public List<Vector3> CapturedEdgeCenters = new List<Vector3>();

		[Header("RESULT OBJECTS")]
		public bool CurrentProjectionResult = false;
		[HideInInspector] LNX_Triangle CurrentTriangle;
		[HideInInspector] LNX_Edge CurrentEdge;


		[Header("OTHER")]
		[Range(-1f,2f)] public float PlaceOnEdgePercentage = 0.5f;

		[ContextMenu("z CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			CapturedPositions.Add( transform.position );
			CapturedResults.Add( CurrentProjectionResult );
			CapturedTriCenters.Add( CurrentTriangle.V_Center );
			CapturedEdgeCenters.Add( CurrentEdge.MidPosition );

			DrawDataPointCapture( CapturedPositions[CapturedPositions.Count - 1], CurrentProjectionResult ? Color.green : Color.red );

			Debug.Log( $"Captured '{CapturedResults[CapturedResults.Count-1]}' at '{CapturedPositions[CapturedPositions.Count-1]}'..." );
		}

		protected override void OnDrawGizmos()
		{
			DBG_Operation = "";

			if ( Selection.activeObject != gameObject && Selection.activeObject != transform.parent.gameObject )
			{
				return;
			}

			base.OnDrawGizmos();

			if (EdgeCoordinate.TrianglesIndex < 0 || EdgeCoordinate.ComponentIndex < 0)
			{
				DBG_Operation += $"OnDrawGizmos short-circuit. {nameof(EdgeCoordinate)}: '{EdgeCoordinate}'...";
				return;
			}

			CurrentTriangle = _navmesh.GetTriangle( EdgeCoordinate );
			CurrentEdge = _navmesh.GetEdge( EdgeCoordinate );

			DrawStandardFocusTriGizmos(_navmesh.Triangles[EdgeCoordinate.TrianglesIndex], 1f, $"tri{EdgeCoordinate.TrianglesIndex}");
			DrawStandardEdgeFocusGizmos(CurrentEdge, 0.1f, "", Color.magenta);

			DBG_Operation += $"Commencing edge operation...\n" +
				$"projection report says:\n" +
				$"{CurrentEdge.dbg_doesPositionLieOnEdge}" +
				$"---------------------------------\n";
			CurrentProjectionResult = _navmesh.GetEdge(EdgeCoordinate).DoesPositionLieOnEdge(transform.position, _navmesh.GetSurfaceNormal() );
			DBG_Operation += $"=============================\n";

			Gizmos.color = CurrentProjectionResult ? Color.green : Color.red;

			Gizmos.DrawSphere(transform.position, Radius_ObjectDebugSpheres);

		}

		#region HELPERS---------------------------------------------
		[ContextMenu("z SampleFocusTri()")]
		public void SampleFocusTri()
		{
			Debug.Log($"{nameof(SampleFocusTri)}()...");

			LNX_ProjectionHit hit = LNX_ProjectionHit.None;

			if ( _navmesh.SamplePosition(transform.position, out hit, 2f, false) )
			{
				EdgeCoordinate = new LNX_ComponentCoordinate(hit.Index_Hit, EdgeCoordinate.ComponentIndex);
				//SetDebuggerFocusToMine();
				Debug.Log($"Succesful sample! Set new edgecoordinate to: '{EdgeCoordinate.ToString()}'");
			}
			else
			{
				Debug.Log($"sample unsuccesful...");
			}
		}

		[ContextMenu("z SetDebuggerFocusToMine()")]
		public void SetDebuggerFocusToMine()
		{
			Debug.Log($"{nameof(SetDebuggerFocusToMine)}()...");

			_debugger.Index_TriFocus = EdgeCoordinate.TrianglesIndex;
		}

		[ContextMenu("z GoToProblem()")]
		public void GoToProblem()
		{
			transform.position = problemPositions[index_focusProblem];


			Debug.Log($"{nameof(GoToProblem)}()...");
		}

		[ContextMenu("z PlaceOnEdge()")]
		public void PlaceOnEdge()
		{
			CurrentEdge = _navmesh.GetEdge(EdgeCoordinate);

			if( PlaceOnEdgePercentage >= 0f )
			{
				transform.position = CurrentEdge.StartPosition + (CurrentEdge.V_StartToEnd * PlaceOnEdgePercentage * CurrentEdge.EdgeLength );
			}
			else
			{
				transform.position = CurrentEdge.StartPosition + (-CurrentEdge.V_StartToEnd * Mathf.Abs(PlaceOnEdgePercentage) * CurrentEdge.EdgeLength);
			}

			Debug.Log($"{nameof(PlaceOnEdge)}()...");
		}

		[ContextMenu("z call RefactorHelper()")]
		public void RefactorHelper()
		{

			#region save captured positions to problem positions------------------------------
			/*
			//problemPositions = CapturedPositions; //nope, this makes it so that they point to the same objects...
			problemPositions = new List<Vector3>();
			for (int i = 0; i < CapturedPositions.Count; i++)
			{
				problemPositions.Add( CapturedPositions[i] );
			}
			*/
			#endregion

			/*
			SampleFocusTri();
			for (int i = 0; i < CapturedPositions.Count; i++)
			{
				LNX_ProjectionHit hit = LNX_ProjectionHit.None;

				_navmesh.SamplePosition(CapturedPositions[i], out hit, 2f, false);

				LNX_Triangle tri = _navmesh.Triangles[hit.Index_Hit];
				if (tri.Edges[0].DoesPositionLieOnEdge(CapturedPositions[i], _navmesh.GetSurfaceNormal()))
				{

				}
			}
			*/

			/*
			CapturedTriCenters = new List<Vector3>();
			CapturedEdgeCenters = new List<Vector3>();

			for ( int i = 0; i < CapturedPositions.Count; i++ )
			{
				LNX_ProjectionHit hit = LNX_ProjectionHit.None;

				_navmesh.SamplePosition( CapturedPositions[i], out hit, 2f, false );

				LNX_Triangle tri = _navmesh.Triangles[hit.Index_Hit];
				if ( tri.Edges[0].DoesPositionLieOnEdge(CapturedPositions[i], _navmesh.GetSurfaceNormal()) )
				{

				}
			}
			*/

			/*
			public List<Vector3> CapturedPositions = new List<Vector3>();
			public List<bool> CapturedResults = new List<bool>();
			public List<Vector3> CapturedTriCenters = new List<Vector3>();
			public List<Vector3> CapturedEdgeCenters = new List<Vector3>();
			*/
			Debug.Log($"{nameof(RefactorHelper)}()...");
		}
		#endregion

		#region WRITING-------------------------------------
		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			bool rslt = TDG_Manager.WriteTestObjectToJson(TDG_Manager.filePath_testData_doesPositionLieOnEdge, this);

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
			if ( !File.Exists(TDG_Manager.filePath_testData_doesPositionLieOnEdge) )
			{
				Debug.LogError($"path '{TDG_Manager.filePath_testData_doesPositionLieOnEdge}' didn't exist. returning early...");
				return;
			}

			string myJsonString = File.ReadAllText(TDG_Manager.filePath_testData_doesPositionLieOnEdge);

			JsonUtility.FromJsonOverwrite(myJsonString, this);

			EditorUtility.SetDirty(this);
		}
		#endregion
	}
}
