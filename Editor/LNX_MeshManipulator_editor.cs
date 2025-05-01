using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.PackageManager.UI;
using System.Runtime.InteropServices;

namespace LogansNavigationExtension
{
	[CustomEditor(typeof(LNX_MeshManipulator)), CanEditMultipleObjects]
	public class LNX_MeshManipulator_editor : Editor
	{
		LNX_MeshManipulator _targetScript;
		public VisualTreeAsset m_InspectorPrefab;
		SerializedObject _lnxMeshManipulator_so;

		#region INPUT----------------------
		bool amAttemptingGrab
		{
			get
			{
				return Event.current.isKey && Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.G;

			}
		}
		#endregion

		[SerializeField] bool flag_translateHandleChangedLastFrame;
		[SerializeField] bool flag_moveHandleIsDirty;
		bool flag_mouseDownThisFrame;
		bool flag_mouseUpThisFrame;


		//protected virtual void OnSceneGUI() //this was how Unity suggested I start one of these custom editors, but a google result said the reason I was getting an error was because 
		// it was marked virtual. The error was "... should not be used inside OnSceneGUI or OnPreviewGUI. Use the single target property instead...."

		private void OnEnable()
		{
			//Debug.Log("was onenabled");
			_targetScript = (LNX_MeshManipulator)target;

			_targetScript.InitState();
		}

		public override VisualElement CreateInspectorGUI()
		{
			_targetScript = (LNX_MeshManipulator)target;
			_targetScript.ClearSelection();
			_lnxMeshManipulator_so = new SerializedObject(_targetScript);
			_lnxMeshManipulator_so.Update();

			// Create a new VisualElement to be the root of our inspector UI
			VisualElement ve_root = new VisualElement();
			m_InspectorPrefab.CloneTree(ve_root);

			PropertyField pf_selectMode = ve_root.Q<PropertyField>("pf_selectMode");
			pf_selectMode.label = "";

			PropertyField pf_lnxNavmeshRef = ve_root.Q<PropertyField>("pf_LnxNavMeshRef");
			pf_lnxNavmeshRef.label = "";

			Button btn_FetchTriangulation = ve_root.Q<Button>("btn_FetchTriangulation");
			btn_FetchTriangulation.clicked += Btn_fetchTriangulation_action;

			Button btn_ClearModifications = ve_root.Q<Button>("btn_ClearModifications");
			btn_ClearModifications.clicked += btn_clearModifications_action;

			pf_selectMode.RegisterValueChangeCallback( selectModeChangedCallback );


			// Return the finished inspector UI
			return ve_root;
		}

		private void Btn_fetchTriangulation_action()
		{
			_targetScript._LNX_NavMesh.CalculateTriangulation();
			_targetScript.ClearSelection();
		}

		private void btn_clearModifications_action()
		{
			_targetScript._LNX_NavMesh.ClearModifications();
			_targetScript.manipulatorPos = _targetScript.Vert_LastSelected.Position;
			//_targetScript.ClearSelection(); //because if I don't do this, the vertices referenced in memory by the mesh manipulator will still exist and be selected...
		}

		void selectModeChangedCallback( SerializedPropertyChangeEvent evt ) //note: for some reason, this is being called when you first select this in the inspector...
		{
			//Debug.Log($"selectModeChangedCallback, selectmode: '{_targetScript.SelectMode}'");

			_targetScript.ChangeSelectMode( _targetScript.SelectMode );
		}

		void MyPointerDownCallback(PointerDownEvent evt) 
		{
			Debug.Log("pde");
		}
		void MyMouseDownCallback(MouseDownEvent evt)
		{
			Debug.Log("mde");
		}

		public void OnSceneGUI()
		{
			if( _targetScript._LNX_NavMesh == null )
			{
				return;
			}

			if ( !Application.isPlaying && Event.current.type == EventType.MouseMove )
			{
				SceneView.RepaintAll(); //This is so that the refreshing is quick for OnDrawGizmos
				//SceneView.currentDrawingSceneView.Repaint();

			}

			#region INPUT --------------------------
			if( Event.current.isMouse ) //fires continuously when the mouse is both in the scene and moving. Does not fire when mouse is in the scene and still.
			{
				flag_mouseDownThisFrame = false;
				flag_mouseUpThisFrame = false;

				if( Event.current.type == EventType.MouseDown )
				{
					//Debug.Log("is mousedown");
					flag_mouseDownThisFrame = true;
				}
				else if( Event.current.type == EventType.MouseUp )
				{
					//Debug.Log("is mouseup");
					flag_mouseUpThisFrame = true;
					if( flag_moveHandleIsDirty )
					{
						//Debug.LogWarning( "refreshing..." );
						flag_moveHandleIsDirty = false;
						_targetScript._LNX_NavMesh.RefreshAfterMove();
					}
				}
			}

			if ( Event.current.isKey && Event.current.type == EventType.KeyUp )
			{
				if ( Event.current.alt )
				{
					if ( Event.current.keyCode == KeyCode.Alpha1 ) // Vertex mode
					{
						_targetScript.ChangeSelectMode( LNX_SelectMode.Vertices );
						
					}
					else if ( Event.current.keyCode == KeyCode.Alpha2 ) // Edge Mode
					{
						_targetScript.ChangeSelectMode( LNX_SelectMode.Edges );
					}
					else if ( Event.current.keyCode == KeyCode.Alpha3 ) // Face Mode
					{
						_targetScript.ChangeSelectMode( LNX_SelectMode.Faces );
					}
					else if( Event.current.keyCode == KeyCode.L ) // Lock selection
					{
						_targetScript.FlipLocked();
						Debug.Log($"fliplocked to '{_targetScript.Flag_AmLocked}'");
					}
					else if( Event.current.keyCode == KeyCode.W )
					{
						_targetScript.OperationMode = LNX_OperationMode.Translating;
					}
					else if ( Event.current.keyCode == KeyCode.X )
					{
						_targetScript.TryInsertLoop();
					}
					else if ( Event.current.keyCode == KeyCode.Delete )
					{
						//Debug.Log("doin it");
						_targetScript.DeleteSelectedTriangles();
					}
				}
				else
				{
					if ( Event.current.keyCode == KeyCode.Escape )
					{
						_targetScript.ChangeSelectMode( LNX_SelectMode.None );
					}
				}
			}
			#endregion

			if ( _targetScript.SelectMode != LNX_SelectMode.None )
			{
				_targetScript.AmPointingATBounds = false; //todo: in the future, I need to make something that can detect if I'm pointing at the bounds first before detecting if pointing at component for efficiency...

				HandleUtility.AddDefaultControl( GUIUtility.GetControlID(FocusType.Passive) ); //This is necessary to put in onscenegui in order to prevent focus from being taken away when clicking in the scene if I want clicking controls...

				Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
				_targetScript.ray = mouseRay;

				//using this simple logic for now...
				_targetScript.TryPointAtComponentViaDirection( SceneView.lastActiveSceneView.camera.transform.position, mouseRay.direction.normalized );

				if ( amAttemptingGrab )
				{
					_targetScript.TryGrab( Event.current.shift || Event.current.control );
				}

				if( _targetScript.OperationMode == LNX_OperationMode.Translating && _targetScript.HaveVertsSelected  )
				{
					EditorGUI.BeginChangeCheck();
					Vector3 newTargetPosition = Handles.PositionHandle( _targetScript.manipulatorPos, Quaternion.identity );
			
					//Debug.Log($"{_targetScript.manipulatorPos}");

					if ( EditorGUI.EndChangeCheck() ) //happens continuously when manipulator is dragged
					{
						_targetScript.MoveSelectedVerts( newTargetPosition );
						Undo.RecordObject( _targetScript, "Change component Positions" );

						flag_moveHandleIsDirty = true;

						flag_translateHandleChangedLastFrame = true;
					}
					else //happens continuously when manipulator is not being dragged, but mouse is moving
					{
						if ( flag_translateHandleChangedLastFrame )
						{
							//Debug.Log("haasdfsaf");
							//_targetScript._LNX_NavMesh.RefeshMesh();

							//if( )
						}

						flag_translateHandleChangedLastFrame = false;
					}
				}
			}

			if ( GUI.changed )
			{
				//Debug.Log("Changed");
				EditorUtility.SetDirty( _targetScript );
			}

		}
	}
}