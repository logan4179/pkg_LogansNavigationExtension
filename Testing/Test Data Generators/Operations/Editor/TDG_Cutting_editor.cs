using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
	[CustomEditor(typeof(TDG_Cutting)), CanEditMultipleObjects]
	public class TDG_Cutting_editor : Editor
	{
		TDG_Cutting _targetScript;

		private void OnEnable()
		{
			_targetScript = (TDG_Cutting)target;
		}

		private void OnSceneGUI()
		{
			if (!Application.isPlaying && Event.current.type == EventType.MouseMove)
			{
				SceneView.RepaintAll(); //This is so that the refreshing is quick for OnDrawGizmos
										//SceneView.currentDrawingSceneView.Repaint();
			}

			Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

			_targetScript._Lnx_MeshManipulator.TryPointAtComponentViaDirection(
				SceneView.lastActiveSceneView.camera.transform.position, mouseRay.direction.normalized
			);


			if (Event.current.isKey && Event.current.type == EventType.KeyUp)
			{
				if (Event.current.keyCode == KeyCode.G)
				{
					_targetScript._Ray = mouseRay;

					_targetScript.CaptureMouseInfo(SceneView.lastActiveSceneView.camera.transform.position, mouseRay.direction.normalized);
				}
			}



		}
	}
}
