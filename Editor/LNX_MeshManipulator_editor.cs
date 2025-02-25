using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.PackageManager.UI;

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
		[SerializeField] bool flag_moveHandleDirty;
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
			pf_selectMode.RegisterValueChangeCallback( selectModeChangedCallback );


			// Return the finished inspector UI
			return ve_root;
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

			#region HOTKEYS --------------------------
			if( Event.current.isMouse )
			{
				flag_mouseDownThisFrame = false;
				flag_mouseUpThisFrame = false;

				if( Event.current.type == EventType.MouseDown )
				{
					flag_mouseDownThisFrame = true;
				}
				else if( Event.current.type == EventType.MouseUp )
				{
					flag_mouseUpThisFrame = true;
					if( flag_moveHandleDirty )
					{
						Debug.Log( "refreshing..." );
						flag_moveHandleDirty = false;
						_targetScript._LNX_NavMesh.RefeshMesh();
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
						_targetScript.OperationMode = LNX_OperationMode.Moving;
					}
					else if ( Event.current.keyCode == KeyCode.X )
					{
						_targetScript.TryCut();
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
				_targetScript.RayOrigin = _targetScript.ray.origin;
				_targetScript.RayDirection = _targetScript.ray.direction;

				//using this simple logic for now...
				_targetScript.TryPointAtComponentViaDirection( SceneView.lastActiveSceneView.camera.transform.position, mouseRay.direction.normalized );

				if ( amAttemptingGrab )
				{
					_targetScript.TryGrab( Event.current.shift || Event.current.control );
				}

				if( _targetScript.OperationMode == LNX_OperationMode.Moving && _targetScript.HaveVertsSelected  )
				{
					EditorGUI.BeginChangeCheck();
					Vector3 newTargetPosition = Handles.PositionHandle( _targetScript.manipulatorPos, Quaternion.identity );
			
					//Debug.Log($"{_targetScript.manipulatorPos}");

					if ( EditorGUI.EndChangeCheck() ) //happens when manipulator is dragged
					{

						_targetScript.MoveSelectedVerts( newTargetPosition );
						Undo.RecordObject( _targetScript, "Change component Positions" );

						flag_moveHandleDirty = true;

						flag_translateHandleChangedLastFrame = true;
					}
					else //happens when the mouse moves, but not when manipulator is dragged
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