using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_CalculatePath : TDG_base
	{
		[Header("REFERENCE")]
		public LNX_ComponentGrabber Grabber_StartPos;
		public LNX_ComponentGrabber Grabber_EndPos;

		public LNX_Triangle StartTriangle => Grabber_StartPos.CurrentlyGrabbedTriangle;
		public LNX_Triangle EndTriangle => Grabber_EndPos.CurrentlyGrabbedTriangle;

		public LNX_Vertex StartVert => Grabber_StartPos.CurrentlyGrabbedVert;
		public LNX_Vertex EndVert => Grabber_EndPos?.CurrentlyGrabbedVert;

		public TextAsset MyTextAsset;

		[Header("DATA")]
		public bool CurrentOperationResult;
		public LNX_Path CurrentResultPath;
		public LNX_NavMeshData _data;

		[Header("DEBUG PATH")]
		public Color Color_PathPoints;
		[Range(0f, 0.05f)] public float Size_PathPoints;
		[Range(0f, 0.25f)] public float Height_PathPtLabels;

		[Header("DEBUG")]
		public bool AllowEffiencyLoading;
		public Color Color_IfTrue;
		public Color Color_IfFalse;

		#region HELPERS ================================================
		[ContextMenu("z call LoadDataObjectFromDisk()")]
		public void LoadDataObjectFromDisk()
		{
			_data = new LNX_NavMeshData();
			
			if( _navmesh.TryGetEfficiencyData(out _data) )
			{
				Debug.Log($"Succesfully got efficiency data");
			}
			else
			{
				Debug.LogError($"apparently couldn't get efficiency data");
			}
		}
		#endregion

		[ContextMenu("z call TryIt()")]
		public void TryIt()
		{
			//List<LNX_ComponentCoordinate> backstopverts = new List<LNX_ComponentCoordinate>();
			List<LNX_ComponentCoordinate> backstopverts = null;

			//List<LNX_ComponentCoordinate> fwdBackstopVerts = new List<LNX_ComponentCoordinate>();
			List<LNX_ComponentCoordinate> fwdBackstopVerts = new List<LNX_ComponentCoordinate>(backstopverts); //todo: can I do this instead of the following loop?

			/*
			if (backstopverts != null && backstopverts.Count > 0)
			{
				for (int i = 0; i < backstopverts.Count; i++)
				{
					fwdBackstopVerts.Add(backstopverts[i]);
				}
			}
			*/

			Debug.Log($"fwdbstpvrts null: '{fwdBackstopVerts == null}'");
		}

		protected override void OnDrawGizmos()
		{
			if 
			( 
				AmInUnitTest || 
				!SelectionIsOneOfTheFollowing(
					gameObject, 
					Grabber_StartPos.gameObject,
					Grabber_EndPos.gameObject
				)
			)
			{
				return;
			}

			base.OnDrawGizmos();

			DrawStandardFocusTriGizmos(StartTriangle, 0.01f, "", Color.magenta, true, 0.01f, false, false);
			DrawStandardFocusTriGizmos(EndTriangle, 0.01f, "", Color.magenta, true, 0.01f, false, false);

			//DBG_Operation += $"Commencing operation...\n";

			if( 
				Grabber_StartPos.RecalculatedLastFrame ||
				Grabber_EndPos.RecalculatedLastFrame
			)
			{
				Debug.Log("Recalculating...");

				DBG_Operation = "";
				DBG_Method = "";
				CurrentOperationResult = false;

				if( AllowEffiencyLoading )
				{
					DBG_Operation += $"am allowing efficiency loading. attempting loading...\n";
					DateTime dt_efficiencyLoadStart = DateTime.Now;
					if( !_data.MatchesNavmesh(_navmesh) )
					{
						Debug.LogError($"LNX ERROR! {nameof(AllowEffiencyLoading)} is turned on, but saved navmesh data seems to be invalid. Returning early...");
						return;
					}
					else
					{
						_navmesh.TryLoadEfficiencyData(_data);
					}
					DBG_Operation += $"efficiency load took '{DateTime.Now.Subtract(dt_efficiencyLoadStart).TotalSeconds}' seconds...\n";
				}

				DateTime dt_opStart = DateTime.Now;
				int mode = 0;
				if( mode == 0 )
				{
					DBG_Operation += $"Mode0, using startHit: '{Grabber_StartPos.CurrentHit}', and endHit: '{Grabber_EndPos.CurrentHit}'...\n" +
						$"Commencing operation...\n";
					CurrentOperationResult = _navmesh.CalculatePath(
						Grabber_StartPos.CurrentHit, Grabber_EndPos.CurrentHit,
						out CurrentResultPath, ref DBG_Method
					);
				}
				else if( mode == 1 )
				{
					DBG_Operation += $"Mode1, using start pos: '{Grabber_StartPos.transform.position}', and end pos: '{Grabber_EndPos.transform.position}'...\n" +
						$"Commencing operation...\n";
					CurrentOperationResult = _navmesh.CalculatePath(
						Grabber_StartPos.transform.position, Grabber_EndPos.transform.position, 0.3f, 
						out CurrentResultPath, ref DBG_Method
					);
				}
				else if (mode == 2 )
				{
					DBG_Operation += $"Mode2, using StartVert: '{StartVert}', and EndVert: '{EndVert}'...\n" +
						$"Commencing operation...\n";
					CurrentOperationResult = _navmesh.CalculatePath(
						StartVert, EndVert,
						out CurrentResultPath, ref DBG_Method
					);
				}

				DBG_Operation += $"calculatepath took '{DateTime.Now.Subtract(dt_opStart).TotalSeconds}' seconds...\n" +
					$"Result: '{CurrentOperationResult}'\n";
			}


			if (CurrentOperationResult)
			{
				Gizmos.color = Color_IfTrue;
			}
			else
			{
				Gizmos.color = Color_IfFalse;
			}

			#region Draw Basic Gizmo Objects --------------------------------------------------------------------
			Grabber_StartPos.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_EndPos.DrawMyGizmos(Radius_ObjectDebugSpheres);
			//Debug.Log(System.DateTime.Now);
			#endregion

			#region Draw Path --------------------------------------------------
			Color oldclr = Gizmos.color;
			Color oldHandlesColor = Handles.color;
			Gizmos.color = Color_PathPoints;
			Handles.color = Color_PathPoints;
			CurrentResultPath.DrawMyGizmos(Size_PathPoints, Height_PathPtLabels);

			Gizmos.color = oldclr;
			Handles.color = oldHandlesColor;
			#endregion
		}
	}
}
