using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_ProjectThroughToPerimeter : TDG_base
    {
		public LNX_ComponentGrabber Grabber_CurrentTri;
		public LNX_Triangle CurrentlyGrabbedTriangle => Grabber_CurrentTri.CurrentlyGrabbedTriangle;
		public LNX_ComponentGrabber Grabber_StartPos;

		public LNX_ComponentGrabber Grabber_EndPos;

		public LNX_Triangle CurrentTriangle => Grabber_CurrentTri.CurrentlyGrabbedTriangle;
		public LNX_Edge ProjectedEdge;
		public LNX_NavmeshHit perimHit;
		public LNX_NavmeshHit closestHitOnPerim;

		[Header("GO TO")]
		public TDG_TryProjectPathThrough _tdg_projectPathThrough;

		[ContextMenu("z call CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			_dataCapture.CaptureDataPoint(
				Grabber_CurrentTri.transform.position, Grabber_EndPos.transform.position,
				CurrentTriangle.V_Center, ProjectedEdge.MidPosition, perimHit.Position
			);
		}

		[ContextMenu("z call GoToTDG()")]
		public void GoToTDG()
		{
			Grabber_CurrentTri.transform.position = _tdg_projectPathThrough.StartVert.V_Position;
			Grabber_EndPos.transform.position = _tdg_projectPathThrough.EndVert.V_Position;
		}

		[ContextMenu("z call RunOperation()")]
		public void RunOperation()
		{
			perimHit = LNX_NavmeshHit.None;
			closestHitOnPerim = LNX_NavmeshHit.None;
			ProjectedEdge = null;
			mthdDbg_Report.Clear();

			DBG_Operation = $"Recalcluated: '{DateTime.Now}'...\n";

			if ( CurrentTriangle == null )
			{
				DBG_Operation += $"CurrentTriangle null. Returning early...\n";
				return;
			}

			DBG_Operation += $"using triangle '{CurrentTriangle.Index_inCollection}'...\n";

			closestHitOnPerim = CurrentlyGrabbedTriangle.ClosestHitOnPerimeter(Grabber_StartPos.transform.position);
			DBG_Operation += $"using closestHitOnPerim: '{closestHitOnPerim}'...\n";

			if( closestHitOnPerim == LNX_NavmeshHit.None )
			{
				DBG_Operation += $"closest hit none. Can't use this. Returning early...\n";
				return;
			}

			if ( !UseDebugVersion )
			{
				DBG_Operation += "using real version...\n" +
					"commencing operation...\n";
				if ( CurrentTriangle.ProjectThroughToPerimeter
					(
						//Grabber_StartPos.CurrentHit, //this will cause problems if not on the same tri...
						new LNX_NavmeshHit(
							CurrentlyGrabbedTriangle, 
							closestHitOnPerim.Position
							),
						Grabber_EndPos.CurrentHit, out perimHit
					)
				)
				{
					DBG_Operation += $"projection returned true. perimHitParam: '{perimHit}'...\n";
					ProjectedEdge = CurrentTriangle.Edges[perimHit.EdgeIndex];
				}
				else
				{
					DBG_Operation += $"CurrentTriangle.ProjectThroughToPerimeter() returned false...\n";
				}
			}
			else
			{
				DBG_Operation += "using debug version...\n" +
					"commencing operation...\n";

				mthdDbg_Report.StartReport("");

				if (CurrentTriangle.ProjectThroughToPerimeter_dbg
					(
						//Grabber_StartPos.CurrentHit, //this will cause problems if not on the same tri...
						new LNX_NavmeshHit(
							CurrentlyGrabbedTriangle,
							//closestHitOnPerim.Position
							Grabber_StartPos.CurrentHit.Position
						),
						Grabber_EndPos.CurrentHit, out perimHit, ref mthdDbg_Report
					)
				)
				{
					DBG_Operation += $"projection returned true. perimHitParam: '{perimHit}'...\n";
					ProjectedEdge = CurrentTriangle.Edges[perimHit.EdgeIndex];

				}
				else
				{
					DBG_Operation += $"CurrentTriangle.ProjectThroughToPerimeter() returned false...\n";
				}
				mthdDbg_Report.EndReport();

			}

			DBG_Operation += $"completed operation. {nameof(perimHit)} now: '{perimHit}'...\n";

		}

		protected override void OnDrawGizmos()
		{
			#region SHORT-CIRCUITING ===============================
			if
			( 
				AmInUnitTest || 
				(
				Selection.activeGameObject != gameObject && 
				Selection.activeGameObject != Grabber_CurrentTri.gameObject && 
				Selection.activeGameObject != Grabber_StartPos.gameObject &&
				Selection.activeGameObject != Grabber_EndPos.gameObject 
				)
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Something wrong with selection...";
				return;
			}
			base.OnDrawGizmos();

			if( AutoRun && 
				(
					Grabber_CurrentTri.RecalculatedLastFrame || 
					Grabber_EndPos.RecalculatedLastFrame || 
					Grabber_StartPos.RecalculatedLastFrame
				)
			)
			{
				RunOperation();
			}
			#endregion


			if ( CurrentTriangle != null )
			{
				//DrawStandardFocusTriGizmos( CurrentTriangle, 1f, $"", Color.magenta);
				LNX_DrawingUtils.DrawStandardFocusTriGizmos( CurrentTriangle, 0.5f, "", Color.magenta );
			}


			if (Grabber_StartPos.CurrentHit != LNX_NavmeshHit.None)
			{
				LNX_DrawingUtils.DrawLabeledPoint(
					Grabber_StartPos.CurrentHit.Position, Grabber_StartPos.CurrentHit.Position + (Vector3.up * 0.04f), 
					$"startHit\n{Grabber_StartPos.CurrentHit.Position}", Color.white
				);
			}

			if ( ProjectedEdge != null )
			{
				DrawStandardEdgeFocusGizmos(ProjectedEdge, 0.02f, $"edge{ProjectedEdge.MyCoordinate.ComponentIndex}", Color.green);
			}

			if( perimHit != LNX_NavmeshHit.None )
			{
				LNX_DrawingUtils.DrawLabeledPoint(
					perimHit.Position, perimHit.Position + (Vector3.up * 0.04f), $"perimHit\n{perimHit.Position}", Color.white
				);
			}

			if ( closestHitOnPerim != LNX_NavmeshHit.None )
			{
				/*
				Vector3 vRise = (Vector3.up * 0.2f);
				Gizmos.DrawLine(closestHitOnPerim.Position, closestHitOnPerim.Position + vRise);
				Handles.Label(closestHitOnPerim.Position + vRise, "closestHitOnPerim");
				*/
			}

			Gizmos.color = perimHit.Equals(LNX_NavmeshHit.None) ? Color.red : Color.green;

			//Grabber_CurrentTri.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Grabber_StartPos.DrawMyGizmos(Radius_ObjectDebugSpheres);

			Grabber_EndPos.DrawMyGizmos(Radius_ObjectDebugSpheres);
			Gizmos.DrawLine(Grabber_StartPos.transform.position, Grabber_EndPos.transform.position);

		}

		#region HELPERS ---------------------------------------
		[ContextMenu("z call GoToDataPoint")]
		public void GoToDataPoint()
		{
			Debug.LogError("not yet implemented");
		}
		#endregion

		#region WRITING-------------------------------------
		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			bool rslt = TDG_Manager.WriteTestObjectToJson(TDG_Manager.filePath_testData_projectThroughToPerimeter, this);

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
			if ( !File.Exists(TDG_Manager.filePath_testData_projectThroughToPerimeter) )
			{
				Debug.LogError($"path '{TDG_Manager.filePath_testData_projectThroughToPerimeter}' didn't exist. returning early...");
				return;
			}

			string myJsonString = File.ReadAllText(TDG_Manager.filePath_testData_projectThroughToPerimeter);

			JsonUtility.FromJsonOverwrite(myJsonString, this);

			EditorUtility.SetDirty(this);
		}
		#endregion
	}
}
