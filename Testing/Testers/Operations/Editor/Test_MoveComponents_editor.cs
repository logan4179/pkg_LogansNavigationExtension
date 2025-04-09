using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LogansNavigationExtension
{
	[CustomEditor(typeof(Test_MoveComponents)), CanEditMultipleObjects]
	public class Test_MoveComponents_editor : Editor
    {
		Test_MoveComponents _targetScript;

		[SerializeField] bool flag_translateHandleChangedLastFrame;
		[SerializeField] bool flag_moveHandleIsDirty;
		bool flag_mouseDownThisFrame;
		bool flag_mouseUpThisFrame;

		private void OnEnable()
		{
			_targetScript = (Test_MoveComponents)target;
		}

		private void OnSceneGUI()
		{
			if ( !Application.isPlaying && Event.current.type == EventType.MouseMove )
			{
				SceneView.RepaintAll(); //This is so that the refreshing is quick for OnDrawGizmos
										//SceneView.currentDrawingSceneView.Repaint();
			}

			if ( Event.current.isMouse )
			{
				flag_mouseDownThisFrame = false;
				flag_mouseUpThisFrame = false;

				if ( Event.current.type == EventType.MouseDown )
				{
					flag_mouseDownThisFrame = true;
				}
				else if (Event.current.type == EventType.MouseUp)
				{
					flag_mouseUpThisFrame = true;
					if (flag_moveHandleIsDirty)
					{
						Debug.Log("refreshing from test...");
						flag_moveHandleIsDirty = false;
						_targetScript._Lnx_MeshManipulator._LNX_NavMesh.RefeshMesh();
					}
				}
			}


			Ray mouseRay = HandleUtility.GUIPointToWorldRay( Event.current.mousePosition );
			_targetScript._Lnx_MeshManipulator.TryPointAtComponentViaDirection( SceneView.lastActiveSceneView.camera.transform.position, mouseRay.direction.normalized );


			if ( Event.current.isKey && Event.current.type == EventType.KeyUp )
			{
				if( Event.current.keyCode == KeyCode.G )
				{
					_targetScript._Ray = mouseRay;

					_targetScript.CaptureMouseInfo( SceneView.lastActiveSceneView.camera.transform.position, mouseRay.direction.normalized );
				}
			}

			if ( _targetScript._Lnx_MeshManipulator.OperationMode == LNX_OperationMode.Translating && _targetScript._Lnx_MeshManipulator.HaveVertsSelected )
			{
				EditorGUI.BeginChangeCheck();
				Vector3 newTargetPosition = Handles.PositionHandle( _targetScript._Lnx_MeshManipulator.manipulatorPos, Quaternion.identity );

				Debug.Log($"{_targetScript._Lnx_MeshManipulator.manipulatorPos}");

				if ( EditorGUI.EndChangeCheck() ) //happens continuously when manipulator is dragged
				{
					_targetScript._Lnx_MeshManipulator.MoveSelectedVerts( newTargetPosition );
					Undo.RecordObject(_targetScript, "Change component Positions");

					flag_moveHandleIsDirty = true;

					flag_translateHandleChangedLastFrame = true;
				}
				else //happens continuously when manipulator is not being dragged, but mouse is moving
				{
					if (flag_translateHandleChangedLastFrame)
					{
						//Debug.Log("haasdfsaf");
						//_targetScript._LNX_NavMesh.RefeshMesh();

						//if( )
					}

					flag_translateHandleChangedLastFrame = false;
				}
			}

		}
	}
}
