using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace LogansNavigationExtension.CustomEditors
{
	[CustomEditor(typeof(LNX_NavMeshLink)), CanEditMultipleObjects]
	public class LNX_NavMeshLink_editor : Editor
    {
		LNX_NavMeshLink _targetScript;
		public VisualTreeAsset m_InspectorPrefab;
		SerializedObject _lnxNavMeshLink_so;


		[SerializeField] private Vector3 spanAStrtPos;
		[SerializeField] private Vector3 spanAEndPos;
		[SerializeField] private Vector3 spanBStrtPos;
		[SerializeField] private Vector3 spanBEndPos;

		//////////////////
		[SerializeField] private Vector3 lastScreenPos_cached;
		[SerializeField] private Vector3 lastMousePos_cached;

		private void OnEnable()
		{
			Debug.Log("link was onenabled");
			_targetScript = (LNX_NavMeshLink)target;

			spanAStrtPos = _targetScript.SpanAStartHit.Position;
			spanAEndPos = _targetScript.SpanAEndHit.Position;
			spanBStrtPos = _targetScript.SpanBStartHit.Position;
			spanBEndPos = _targetScript.SpanBEndHit.Position;
		}

		public void OnSceneGUI() //only fires when mouse is in scene view and moving
		{
			//Debug.Log("osg");

			Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

			#region UPDATE SPANS =======================================================================
			if ( _targetScript.SetSpans )
			{
			Vector3 newHandlePosition_startA = Handles.PositionHandle(_targetScript.HandlePos_spanAStart, Quaternion.identity);
			/*
			if ( _targetScript.HandlePos_spanAStart != newHandlePosition_startA )
			{
				//Debug.Log($"span A start moving. pos_spanAStart: '{_targetScript.pos_spanAStart}', newHandlePosition: '{newHandlePosition}'");
				//_targetScript.HandlePos_spanAStart = newHandlePosition_startA;

				LNX_Edge edge = _targetScript._Manager.GetEdge(_targetScript.Coord_StartEdgeA);
				_targetScript.SpanA_StartUpdated( edge.ClosestHitOnEdge(newHandlePosition_startA) );
			}
			*/

			Vector3 newHandlePosition_endA = Handles.PositionHandle(_targetScript.HandlePos_spanAEnd, Quaternion.identity);
			/*
			if (_targetScript.HandlePos_spanAEnd != newHandlePosition_endA)
			{
				//_targetScript.HandlePos_spanAEnd = newHandlePosition_endA;

				LNX_Edge edge = _targetScript._Manager.GetEdge(_targetScript.Coord_EndEdgeA);
				_targetScript.SpanA_EndUpdated(edge.ClosestHitOnEdge(newHandlePosition_endA));
			}
			*/
			Vector3 newHandlePosition_startB = Handles.PositionHandle(_targetScript.HandlePos_spanBStart, Quaternion.identity);
			/*
			if (_targetScript.HandlePos_spanBStart != newHandlePosition_startB)
			{
				//_targetScript.HandlePos_spanBStart = newHandlePosition_startB;

				LNX_Edge edge = _targetScript._Manager.GetEdge(_targetScript.Coord_StartEdgeB);
				_targetScript.SpanB_StartUpdated(edge.ClosestHitOnEdge(newHandlePosition_startB));
			}
			*/
			Vector3 newHandlePosition_endB = Handles.PositionHandle(_targetScript.HandlePos_spanBEnd, Quaternion.identity);
			/*
			if (_targetScript.HandlePos_spanBEnd != newHandlePosition_endB)
			{
				//_targetScript.HandlePos_spanBEnd = newHandlePosition_endB;

				LNX_Edge edge = _targetScript._Manager.GetEdge(_targetScript.Coord_EndEdgeB);
				_targetScript.SpanB_EndUpdated(edge.ClosestHitOnEdge(newHandlePosition_endB));
			}
			*/
			_targetScript.UpdateHandles(newHandlePosition_startA, newHandlePosition_endA, 
				newHandlePosition_startB, newHandlePosition_endB);
			}
			#endregion

			
			lastScreenPos_cached = SceneView.lastActiveSceneView.camera.transform.position;
			lastMousePos_cached = Event.current.mousePosition;
			//Debug.Log($"osg sp: '{lastScreenPos_cached}', mp: '{lastMousePos_cached}'");
		}
	}
}
