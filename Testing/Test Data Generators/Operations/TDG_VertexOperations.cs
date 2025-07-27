using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace LogansNavigationExtension
{
    public class TDG_VertexOperations : TDG_base
	{
		[Tooltip("0: IsInNormalizedCenterSweep, ")]
		public int Index_OperationMode = 0;

		public int Index_FocusTriangle = 0;
		[HideInInspector] public Vector3 ProjectedPos;

		[Header("DATA")]
		public List<Vector3> Positions = new List<Vector3>();
        public List<bool> Results_IsInCenterSweep_Vert0 = new List<bool>();
		public List<bool> Results_IsInCenterSweep_Vert1 = new List<bool>();
		public List<bool> Results_IsInCenterSweep_Vert2 = new List<bool>();

		[Header("DEBUG")]
		[SerializeField] private string DBG_class;

		public Color Color_IfTrue = Color.white;
		public Color Color_IfFalse = Color.white;

		[Range(0f,0.5f)] public float Radius_TestObject = 0.2f;
		[Range(0f, 0.005f)] public float Radius_ProjectedPos = 0.05f;

		public Color Color_projectedPos;

		[SerializeField] private bool amDebuggingDataPoints = true;
		[SerializeField] float radius_dataPoints = 0.05f;
		[SerializeField] Color color_dataPoints = Color.white;
		public int Index_GoToDataPoint = 0;

		[ContextMenu("z call GoToDataPoint()")]
		public void GoToDataPoint()
		{
			if( Index_OperationMode == 0 )
			{
				transform.position = Positions[Index_GoToDataPoint];
			}
		}

		[ContextMenu("z call WriteMeToJson()")]
		public bool WriteMeToJson()
		{
			bool rslt = TDG_Manager.WriteTestObjectToJson(TDG_Manager.filePath_testData_projectingTests, this);

			if (rslt)
			{
				LastWriteTime = System.DateTime.Now.ToString();
				return true;
			}

			return false;
		}

		protected override void OnDrawGizmos()
		{
			if ( Selection.activeObject != gameObject )
			{
				return;
			}

			base.OnDrawGizmos();


			DBG_class = "";
			Color decidedMainColor = Color_IfTrue;

			LNX_Triangle focusTri = _navmesh.Triangles[Index_FocusTriangle];
			DrawStandardFocusTriGizmos(focusTri, 0.5f, $"tri{focusTri.Index_inCollection}({focusTri.V_Center})");

			Vector3 v_ctrToPos = transform.position - focusTri.V_Center;

			//ProjectedPos = focusTri.V_Center + Vector3.ProjectOnPlane( v_ctrToPos, focusTri.v_derivedNormal ); //dws
			//ProjectedPos = focusTri.V_Center + focusTri.GetFlattenedPosition( v_ctrToPos ); //dws
			ProjectedPos = focusTri.V_Center + LNX_Utils.FlatVector( v_ctrToPos, focusTri.v_projectionNormal );


			DBG_class += $"{nameof(ProjectedPos)}: '{ProjectedPos}'\n";

			Gizmos.color = Color_projectedPos;
			Gizmos.DrawLine(transform.position, ProjectedPos);
			Gizmos.DrawSphere( ProjectedPos, Radius_ProjectedPos );
			Handles.Label( ProjectedPos, $"projectedPos({ProjectedPos})" );

			#region PERFORM OPERATIONS-------------------------------------------------------
			//bool rslt = focusTri.Verts[0].IsInCenterSweep(ProjectedPos);
			bool rslt = focusTri.Verts[0].IsInFlatCenterSweep(ProjectedPos);

			DBG_class += $"vrt0... \n" +
				$"{focusTri.Verts[0].DBG_IsInCenterSweep}\n";

			if( rslt )
			{
				Gizmos.color = Color.green;
				DBG_class += $"vert0 center sweep succeeded\n";
			}
			else
			{
				Gizmos.color = Color.red;
				decidedMainColor = Color_IfFalse;
				DBG_class += $"vert0 center sweep failed...\n";
			}
			Gizmos.DrawLine(focusTri.Verts[0].V_Position, focusTri.Verts[0].V_Position + (Vector3.up * 1f));

			//rslt = focusTri.Verts[1].IsInCenterSweep(ProjectedPos);
			rslt = focusTri.Verts[1].IsInFlatCenterSweep(ProjectedPos);

			DBG_class += $"vrt1... \n" +
				$"{focusTri.Verts[1].DBG_IsInCenterSweep}\n";

			if (rslt)
			{
				Gizmos.color = Color.green;
				DBG_class += $"vert1 center sweep succeeded\n";
			}
			else
			{
				decidedMainColor = Color_IfFalse;
				Gizmos.color = Color.red;
				DBG_class += $"vert1 center sweep failed...\n";
			}
			Gizmos.DrawLine(focusTri.Verts[1].V_Position, focusTri.Verts[1].V_Position + (Vector3.up * 1f));

			//rslt = focusTri.Verts[2].IsInCenterSweep(ProjectedPos);
			rslt = focusTri.Verts[2].IsInFlatCenterSweep(ProjectedPos);

			DBG_class += $"vrt2...\n" +
				$"{focusTri.Verts[2].DBG_IsInCenterSweep}\n";

			if (rslt)
			{
				Gizmos.color = Color.green;
				DBG_class += $"vert2 center sweep succeeded\n";
			}
			else
			{
				decidedMainColor = Color_IfFalse;
				Gizmos.color = Color.red;
				DBG_class += $"vert2 center sweep failed...\n";
			}
			Gizmos.DrawLine(focusTri.Verts[2].V_Position, focusTri.Verts[2].V_Position + (Vector3.up * 1f));
			#endregion			

			#region DRAW MAIN OBJECT------------------------------------
			Gizmos.color = decidedMainColor;
			Gizmos.DrawSphere(transform.position, Radius_TestObject);
			#endregion

		}
	}
}
