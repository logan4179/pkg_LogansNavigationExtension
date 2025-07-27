using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_CalculatePath : TDG_base
	{
		[Space(10f)]

		[Tooltip("0 = sample position, 1 = designate verts")]
        public int TestMode = 0;
        public LNX_ComponentCoordinate StartVert;
		public LNX_ComponentCoordinate EndVert;

        [Header("REFERENCE")]
        public Transform StartTransform;

		[Header("OTHER")]
		public LNX_Path MyPath;

		[Header("DEBUG")]
		public Color Color_IfTrue;
		public Color Color_IfFalse;

		[Range(0f, 0.3f)] public float radius_objectSpheres;
		[TextArea(0, 10)] public string DBG_Class;
		[TextArea(0, 10)] public string DBG_CalculatePath;
		[TextArea(0, 10)] public string DBG_SamplePosition;


		//[Header("DEBUG - OTHER")]
		[HideInInspector] public Vector3 lastStartPos;
		[HideInInspector] public Vector3 lastEndPos;
		[HideInInspector] public bool LastResult = false;

		protected override void OnDrawGizmos()
		{
			if ( Selection.activeGameObject != gameObject )
			{
				return;
			}

			base.OnDrawGizmos();

			#region DRAW LABEL STUFF (needs to be up here before the following logic)------------
			if ( LastResult == true )
			{
				Gizmos.color = Color_IfTrue;
			}
			else
			{
				Gizmos.color = Color_IfFalse;
			}

			float height_objectLabels = 0.15f;
			Gizmos.DrawSphere(transform.position, radius_objectSpheres);
			Handles.Label(transform.position + (Vector3.up * height_objectLabels), "end");
			Gizmos.DrawLine(transform.position, transform.position + (Vector3.up * height_objectLabels));

			Gizmos.DrawSphere(StartTransform.position, radius_objectSpheres);
			Handles.Label(StartTransform.position + (Vector3.up * height_objectLabels), "start");
			Gizmos.DrawLine(StartTransform.position, StartTransform.position + (Vector3.up * height_objectLabels));
			#endregion

			#region CHECK IF UPDATE NECESSARY------------------------------
			if ( lastEndPos == transform.position && lastStartPos == StartTransform.position )
			{
				return;
			}
			#endregion

			DBG_Class = System.DateTime.Now.ToString() + "\n";

			try
			{
				if( _navmesh.CalculatePath(StartTransform.position, transform.position, 0.3f, out MyPath) )
				{
					DBG_Class += $"LNX_Navmesh.CalculatedPath() succesfull!\n";

					Gizmos.color = Color_IfTrue;
					LastResult = true;
				}
				else
				{
					DBG_Class += $"LNX_Navmesh.CalculatedPath() was NOT succesfull...\n";

					Gizmos.color = Color_IfFalse;
					LastResult = false;
				}
			}
			catch  ( System.Exception e )
			{
				Gizmos.color = Color_IfFalse;
				LastResult = false;

				DBG_Class += $"Exception caught. Exception says:\n" +
					$"{e.ToString()}\n";

				lastStartPos = StartTransform.position;
				lastEndPos = transform.position;

				throw;
			}

			DBG_CalculatePath = _navmesh.dbgCalculatePath;

			DBG_SamplePosition = _navmesh.DBG_SamplePosition;

			lastStartPos = StartTransform.position;
			lastEndPos = transform.position;
		}
	}
}
