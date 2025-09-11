using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_CalculatePath : TDG_base
	{
        [Header("REFERENCE")]
        public Transform StartTransform;

		[Header("DATA")]
		public bool CurrentOperationResult;
		public List<LNX_ProjectionHit> CurrentResultantRaycastHits;
		public LNX_Path CurrentResultPath;

		[Header("DEBUG PATH")]
		public Color Color_PathPoints;
		[Range(0f, 0.05f)] public float Size_PathPoints;
		[Range(0f, 0.25f)] public float Height_PathPtLabels;

		[Header("DEBUG")]
		public bool AmReCalculating;
		public Color Color_IfTrue;
		public Color Color_IfFalse;

		[TextArea(0, 10)] public string DBG_Class;
		[TextArea(0, 10)] public string DBG_CalculatePath;
		[TextArea(0, 10)] public string DBG_SamplePosition;


		//[Header("DEBUG - OTHER")]
		[HideInInspector] public string lastDateTime;
		[HideInInspector] public Vector3 CachedLastStartPos;
		[HideInInspector] public Vector3 CachedLastEndPos;

		protected override void OnDrawGizmos()
		{
			if ( Selection.activeGameObject != gameObject )
			{
				return;
			}

			base.OnDrawGizmos();

			#region CHECK IF UPDATE NECESSARY------------------------------
			if ( CachedLastEndPos != transform.position || CachedLastStartPos != StartTransform.position )
			{
				AmReCalculating = true;
				CurrentOperationResult = _navmesh.CalculatePath(
					StartTransform.position, transform.position, 0.3f, out CurrentResultPath 
				);

				DBG_CalculatePath = _navmesh.dbgCalculatePath;
				DBG_SamplePosition = _navmesh.DBG_SamplePosition;

				lastDateTime = System.DateTime.Now.ToString();
			}
			else
			{
				AmReCalculating = false;
			}
			#endregion

			DBG_Class = $"{lastDateTime}\n";
			if (CurrentOperationResult)
			{
				DBG_Class += $"LNX_Navmesh.CalculatedPath() succesfull!\n";

				Gizmos.color = Color_IfTrue;
			}
			else
			{
				DBG_Class += $"LNX_Navmesh.CalculatedPath() was NOT succesfull...\n";

				Gizmos.color = Color_IfFalse;
			}

			#region Draw Basic Gizmo Objects --------------------------------------------------------------------
			float height_objectLabels = 0.15f;
			Gizmos.DrawSphere(transform.position, Radius_ObjectDebugSpheres );
			Handles.Label(transform.position + (Vector3.up * height_objectLabels), "end");
			Gizmos.DrawLine(transform.position, transform.position + (Vector3.up * height_objectLabels));

			Gizmos.DrawSphere(StartTransform.position, Radius_ObjectDebugSpheres );
			Handles.Label(StartTransform.position + (Vector3.up * height_objectLabels), "start");
			Gizmos.DrawLine(StartTransform.position, StartTransform.position + (Vector3.up * height_objectLabels));
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

			CachedLastStartPos = StartTransform.position;
			CachedLastEndPos = transform.position;
		}
	}
}
