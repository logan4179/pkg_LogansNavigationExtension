using NUnit.Framework.Constraints;
using System;
using UnityEngine;

namespace LogansNavigationExtension.AI
{
    public class LNX_Agent : MonoBehaviour
    {
        [Header("REFERENCE")]
        [Tooltip("The 'visual' part of the entity's heirarchy. This will be the object that's actually rotated")]
        public Transform VisualTransform;
		[HideInInspector, NonSerialized] public Rigidbody _RigidBody;
        [HideInInspector, NonSerialized] public Transform _FollowTrans;

        [Space(5f)]
        public MovementSchema[] MovementSchemas;
        private int index_currentMovementSchema = 0;
        public MovementSchema CurrentMovementSchema => MovementSchemas[index_currentMovementSchema];
        public float CurrentMoveSpeed => MovementSchemas[index_currentMovementSchema].MoveSpeed;
        public float CurrentRotationSpeed => MovementSchemas[index_currentMovementSchema].RotationSpeed;


		//[Header("PATHING")]
		[HideInInspector, NonSerialized] public LNX_Path _CurrentPath;
        private int index_currentPathPt = -1;
        public LNX_NavmeshHit _CurrentPathPt => _CurrentPath.PathPoints[index_currentPathPt];

        /// <summary>
        /// The current hit that describes this agent's position on the navmesh
        /// </summary>
        private LNX_NavmeshHit _currentHit;
		/// <summary>
		/// The current hit that describes this agent's position on the navmesh
		/// </summary>
		public LNX_NavmeshHit CurrentHit => _currentHit;

		private void Awake()
		{
			_RigidBody = GetComponent<Rigidbody>();
		}

		void Start()
        {
            
        }

        void FixedUpdate()
        {
            if ( _CurrentPath != null )
            {
                Vector3 v_toNext = Vector3.Normalize( _CurrentPathPt.Position - transform.position );
                Vector3 v_transUpGoal = Vector3.RotateTowards( VisualTransform.up, _CurrentPathPt.Normal, CurrentRotationSpeed * Time.fixedDeltaTime, 0.0f );
                float alignment_transUp_with_crntPthNrml = Vector3.Dot( VisualTransform.up, _CurrentPathPt.Normal );
                float alignment_transFwd_with_crntPthPos = Vector3.Dot( VisualTransform.forward, v_toNext );
                Quaternion q_finalRot = Quaternion.identity;

				if (alignment_transFwd_with_crntPthPos < -0.98f && alignment_transUp_with_crntPthNrml > 0.95f )
				{
					Vector3 vRot = Vector3.RotateTowards(VisualTransform.forward, VisualTransform.right, CurrentRotationSpeed * Time.fixedDeltaTime, 0.0f);
					q_finalRot = Quaternion.LookRotation( vRot, VisualTransform.up );
				}
				else
				{
					//vRot = Vector3.RotateTowards(trans.forward, v_toGoal, rotSpeed_passed * Time.fixedDeltaTime, 0.0f);
					Vector3 vRot = Vector3.RotateTowards(VisualTransform.forward, v_toNext, CurrentRotationSpeed * Time.fixedDeltaTime, 0.0f);
					//q_finalRot = Quaternion.LookRotation( vRot, _CurrentPathPt.Normal );
					q_finalRot = Quaternion.LookRotation(vRot, v_transUpGoal);
				}

				if ( CurrentMovementSchema._MovementMode == MovementMode.Transform_directionallyDriven ) //todo: should I not use fixedupdate for this? If not, I'd have to make an update block just for this case...
                {
                    transform.rotation = q_finalRot;
                    transform.Translate(CurrentMoveSpeed * Time.fixedDeltaTime * v_toNext);
				}
                else if ( CurrentMovementSchema._MovementMode == MovementMode.Transform_forwardDriven )
                {
					transform.rotation = q_finalRot;
					transform.Translate( VisualTransform.forward * CurrentMoveSpeed * Time.fixedDeltaTime );
                }
                else if (CurrentMovementSchema._MovementMode == MovementMode.Rigidbody_directionallyDriven)
                {
                    _RigidBody.MoveRotation(q_finalRot);

                    if (Vector3.Angle(VisualTransform.forward, v_toNext) < CurrentMovementSchema.RotationAlignmentThreshold)
                    {
                        _RigidBody.MovePosition(_RigidBody.position + (CurrentMoveSpeed * Time.fixedDeltaTime * v_toNext));

                    }
                }
				else if (CurrentMovementSchema._MovementMode == MovementMode.Rigidbody_forwardDriven)
				{
					_RigidBody.MoveRotation(q_finalRot);

					if (Vector3.Angle(VisualTransform.forward, v_toNext) < CurrentMovementSchema.RotationAlignmentThreshold)
					{
						_RigidBody.MovePosition(_RigidBody.position + (VisualTransform.forward * CurrentMoveSpeed * Time.fixedDeltaTime));

					}
				}
			}
        }

	}

    public enum MovementMode
    {
        None = 0,
        Transform_directionallyDriven,
        Transform_forwardDriven,
        Rigidbody_directionallyDriven,
        Rigidbody_forwardDriven,
    }

    [System.Serializable]
    public struct MovementSchema
    {
        public string Name;
        [Space(5f)]

		public MovementMode _MovementMode;

        [Header("MOVMENT")]
        [SerializeField] private float moveSpeed;
        public float MoveSpeed => moveSpeed;
        [SerializeField] private float movementAcceleration;
        [SerializeField] private bool movementAccelerationIsSloped;

		[Header("PATHING")]
		[SerializeField, Tooltip("Distance within this agent will consider itself 'close enough' and start towards the next point")] 
        private float advanceToNextPointDistance;

		[Header("ROTATION")]
		[SerializeField] private float rotationSpeed;
        public float RotationSpeed => rotationSpeed;
		[SerializeField] private float rotationAcceleration;
		[SerializeField] private bool rotationAccelerationIsSloped;

		[Range(0f, 360f), Tooltip("How closely the agent has to be facing the next path target in order to allow movment forward")]
		public float RotationAlignmentThreshold;
	}
}
