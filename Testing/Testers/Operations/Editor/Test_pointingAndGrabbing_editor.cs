using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LogansNavigationExtension
{
	[CustomEditor(typeof(Test_pointingAndGrabbing)), CanEditMultipleObjects]
	public class Test_pointingAndGrabbing_editor : Editor
    {
		Test_pointingAndGrabbing _targetScript;

		private void OnEnable()
		{
			_targetScript = (Test_pointingAndGrabbing)target;
		}

		private void OnSceneGUI()
		{
			if (!Application.isPlaying && Event.current.type == EventType.MouseMove)
			{
				SceneView.RepaintAll(); //This is so that the refreshing is quick for OnDrawGizmos
										//SceneView.currentDrawingSceneView.Repaint();

			}

			//DrawDefaultInspector();

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



		}
	}
}
