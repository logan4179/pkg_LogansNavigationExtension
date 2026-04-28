using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;

namespace LogansNavigationExtension.CustomEditors
{
    [CustomEditor(typeof(LNX_NavMesh)), CanEditMultipleObjects]
    public class LNX_NavMesh_editor : Editor
    {
        LNX_NavMesh _targetScript;

		public VisualTreeAsset m_InspectorPrefab;
		SerializedObject _lnxNavMesh_so;

		//protected virtual void OnSceneGUI() //this was how Unity suggested I start one of these custom editors, but a google
		//result said the reason I was getting an error was because it was marked virtual. The error was
		//"... should not be used inside OnSceneGUI or OnPreviewGUI. Use the single target property instead...."

		private void OnEnable()
		{
			Debug.Log($"LNX_NavMesh was onenabled through the editor code."); //from what I can tell, this gets called when you select the object in the heirarcy, NOT when you deactivate and reactivate the object

			//Force target script reference (LNX_NavMesh) to do any initializing I might want it to do below here...
			_targetScript = (LNX_NavMesh)target;

			Debug.Log($"Mesh null: '{_targetScript._VisualizationMesh == null}'");

			if ( _targetScript._VisualizationMesh != null )
			{
				Debug.Log($"LNX_NavMesh was onenabled through the editor code. Mesh verts null: '{_targetScript._VisualizationMesh.vertices == null}'");

				if ( _targetScript._VisualizationMesh.vertices != null )
				{
					Debug.Log($"vis mesh vert count: '{_targetScript._VisualizationMesh.vertices.Length}'"); //I'm debugging this currently because I'm considering re-calculating the vis mesh here if the collection is null or 0 count
				}
			}

		}

		public override VisualElement CreateInspectorGUI()
		{			
			#region CACHE MAIN OBJECTS -----------------------------
			_targetScript = (LNX_NavMesh)target;

			_lnxNavMesh_so = new SerializedObject(_targetScript);
			_lnxNavMesh_so.Update();
			#endregion

			VisualElement ve_root = new VisualElement();

			if ( false ) // Make this true if you just want the default inspector...
			{
				InspectorElement.FillDefaultInspector(ve_root, _lnxNavMesh_so, this);
				//DrawDefaultInspector();
				//return null;
				return ve_root;
			}

			// Create a new VisualElement to be the root of our inspector UI
			m_InspectorPrefab.CloneTree(ve_root);

			PropertyField pf_surfaceOrientation = ve_root.Q<PropertyField>("pf_surfaceOrientation");
			pf_surfaceOrientation.RegisterValueChangeCallback( surfaceOrientationChanged_action );

			/*
			PropertyField pf_selectMode = ve_root.Q<PropertyField>("pf_selectMode");
			pf_selectMode.label = "";

			PropertyField pf_lnxNavmeshRef = ve_root.Q<PropertyField>("pf_LnxNavMeshRef");
			pf_lnxNavmeshRef.label = "";

			Button btn_FetchTriangulation = ve_root.Q<Button>("btn_FetchTriangulation");
			btn_FetchTriangulation.clicked += Btn_fetchTriangulation_action;

			Button btn_ClearModifications = ve_root.Q<Button>("btn_ClearModifications");
			btn_ClearModifications.clicked += btn_clearModifications_action;

			pf_selectMode.RegisterValueChangeCallback(selectModeChangedCallback);
			*/

			
			return ve_root; // Return the finished inspector UI
		}

		private void surfaceOrientationChanged_action( SerializedPropertyChangeEvent evt )
		{
			Debug.Log($"surfaceorientation callback");

			//todo: what should I do now to update all the objects???

			Debug.Log($"surface orientation changed to: '{_targetScript.GetSurfaceProjectionVector()}'..."); 
		}

		public void OnSceneGUI()
		{
			
		}
	}
}
