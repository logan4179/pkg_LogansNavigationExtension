using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_GetVertexRelationship : TDG_base
    {
		public LNX_ComponentGrabber PerspectiveVertGrabber;
		public LNX_Vertex PerspectiveVertex => PerspectiveVertGrabber.CurrentlyGrabbedVert;
		public LNX_ComponentGrabber EndVertGrabber;
		public LNX_Vertex EndVertex => EndVertGrabber.CurrentlyGrabbedVert;

		[Header("RESULTS")]
		//public List<LNX_ProjectionHit> RaycastHitResults;
		public LNX_VertexRelationship ResultRelationship;
		public LNX_Path ResultPath => ResultRelationship.PathTo;

		[Header("DEBUG")]
		public Color Color_PathPoints;
		[Range(0f, 0.05f)] public float Size_PathPoints;
		[Range(0f, 0.25f)] public float Height_PathPtLabels;


		[ContextMenu("z CaptureDataPoint()")]
		public void CaptureDataPoint()
		{
			/*
			CapturedStartPositions.Add( startTrans.position );
			CapturedEndPositions.Add( endTrans.position );
			CapturedRaycastResults.Add( RaycastResult );
			*/
			//Debug.Log($"Logged '{rslt_CurrentProjectedPtOnEdge}'...");


		}

		[ContextMenu("z CaptureProblemPosition (override)()")]
		public void CaptureProblemPosition_override()
		{
			Debug.Log("from override");

			//_dataCapture_problems.CaptureDataPoint( startTrans.position, endTrans.position );

			Debug.Log($"{nameof(CaptureProblemPosition_override)}()...");
		}

		[ContextMenu("z GoToProblem()")]
		public void GoToProblem()
		{
			//startTrans.position = problemPositions[Index_FocusProblem];
			//endTrans.position = ProblemEndPositions[Index_FocusProblem];

			//startTrans.position = CapturedStartPositions[index_focusProblem];
			//endTrans.position = CapturedEndPositions[index_focusProblem];

			Debug.Log($"{nameof(GoToProblem)}()...");
		}

		[ContextMenu("z call RunOperation()")]
		public void RunOperation()
		{
			mthdDbg_Report.Clear();
			ResultRelationship = LNX_VertexRelationship.None;

			DBG_Operation = $"{DateTime.Now}\n";

			if ( PerspectiveVertex == null )
			{
				DBG_Operation += $"PerspectiveVertex is null. Returning early...\n";
				Debug.LogWarning($"PerspectiveVertex is null. Returning early...\n");

				return;
			}

			if ( EndVertex == null )
			{
				DBG_Operation += $"End Vertex is null. Returning early...\n";
				Debug.LogWarning($"End Vertex is null. Returning early...\n");
				return;
			}

			if( PerspectiveVertex.Relationships == null )
			{
				DBG_Operation += $"Perspective vertex relationships collection is null. Returning early...\n";
				Debug.Log($"Perspective vertex relationships collection is null. Returning early...\n");
				return;
			}

			if ( PerspectiveVertex.Relationships.Length <= 0 )
			{
				DBG_Operation += $"Perspective vertex relationships collection length is " +
					$"'{PerspectiveVertex.Relationships.Length}'. Returning early...\n";
				Debug.Log($"Perspective vertex relationships collection length is " +
					$"'{PerspectiveVertex.Relationships.Length}'. Returning early...\n");
				return;
			}


			DBG_Operation += $"using PerspectiveVert: '{PerspectiveVertex}'\n" +
				$"EndVert: '{EndVertex}'...\n";

			ResultRelationship = PerspectiveVertex.Relationships[EndVertex.Index_Relational];

			DBG_Operation += $"result: '{(ResultRelationship == LNX_VertexRelationship.None ? "none" : ResultRelationship)}'\n";
		}

		protected override void OnDrawGizmos()
		{

			if (AmInUnitTest || Selection.activeObject != gameObject && Selection.activeObject != PerspectiveVertGrabber.gameObject)
			{
				return;
			}

			base.OnDrawGizmos();


			//RaycastResult = _navmesh.Raycast(startTrans.position, endTrans.position, 3f); //for without path

			if (AutoRun && (PerspectiveVertGrabber.RecalculatedLastFrame || EndVertGrabber.RecalculatedLastFrame)) //"IF something's changed..." this is to make it a little snappier in the editor...
			{
				RunOperation();
			}

			if ( ResultRelationship != null && ResultPath != LNX_Path.None)
			{
				Color oldClr = Gizmos.color;
				Gizmos.color = Color_PathPoints;
				Handles.color = Color_PathPoints;

				ResultPath.DrawMyGizmos(Size_PathPoints, Height_PathPtLabels);

				Gizmos.color = oldClr;
				Handles.color = oldClr;
			}

			Gizmos.color = ResultRelationship == null ? Color.red : Color.green;

			Gizmos.DrawLine(PerspectiveVertGrabber.transform.position, EndVertGrabber.transform.position);

			Gizmos.DrawSphere(PerspectiveVertGrabber.transform.position, Radius_ObjectDebugSpheres);
			//Handles.Label(startTrans.position, "strtTrans");
			Gizmos.DrawSphere(EndVertGrabber.transform.position, Radius_ObjectDebugSpheres);
			//Handles.Label(startTrans.position, "endTrans");
		}

		#region HELPERS -------------------------------------
		[ContextMenu("z call GoToDataPoint")]
		public void GoToDataPoint()
		{
			//startTrans.position = CapturedStartPositions[index_focusProblem];
			//endTrans.position = CapturedEndPositions[index_focusProblem];
		}


		[ContextMenu("z call DoEet")]
		public void DoEet()
		{

		}
		#endregion

		#region WRITING-------------------------------------
		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			bool rslt = TDG_Manager.WriteTestObjectToJson(TDG_Manager.filePath_testData_Raycasting, this);

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
			if (!File.Exists(TDG_Manager.filePath_testData_Raycasting))
			{
				Debug.LogError($"path '{TDG_Manager.filePath_testData_Raycasting}' didn't exist. returning early...");
				return;
			}

			string myJsonString = File.ReadAllText(TDG_Manager.filePath_testData_Raycasting);

			JsonUtility.FromJsonOverwrite(myJsonString, this);

			EditorUtility.SetDirty(this);
		}
		#endregion
	}
}
