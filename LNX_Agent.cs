using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace LogansNavigationExtension.AI
{
    public class LNX_Agent : MonoBehaviour
    {
        [Header("REFERENCE (INTERNAL)")]
        [Tooltip("The 'visual' part of the entity's heirarchy. This will be the object that's realistically rotated")]
        public Transform VisualTransform;
		[HideInInspector, NonSerialized] public Rigidbody _RigidBody;
        [HideInInspector, NonSerialized] public Transform _FollowTrans;

		//[Header("REFERENCE (EXTERNAL)")]
        private LNX_Manager _manager;
        public LNX_Manager Manager => _manager;

		[Header("FOOTING")]
		public LayerMask FootingLayerMask;
		[SerializeField] private List<FootingSampler> FootingSamplers;

		private Vector3 averageFootSampleNormal;
		public Vector3 AverageFootSampleNormal => averageFootSampleNormal;
        private RaycastHit footSampleHit;
        public RaycastHit FootSampleHit => footSampleHit;
		/// <summary>Restricts to only sample underfoot if position has changed since last frame. More efficient </summary>
		public bool SampleFootingOnlyWhenPositionHasChanged = true;

		[Header("MOVEMENT SCHEMAS")]
        public MovementSchema[] MovementSchemas;
        private int index_currentMovementSchema = 0;
        public MovementSchema CurrentMovementSchema => MovementSchemas[index_currentMovementSchema];
        public float CurrentMoveSpeed => MovementSchemas[index_currentMovementSchema].MoveSpeed;
        public float CurrentRotationSpeed => MovementSchemas[index_currentMovementSchema].RotationSpeed;

        public int Index_CurrentSurface = -1;

		//[Header("PATHING")]
		private LNX_Path _currentPath;
        public LNX_Path CurrentPath => _currentPath;

        private int index_currentPathPt = -1;
        public int Index_CurrentPathPt => index_currentPathPt;
        public LNX_NavmeshHit _CurrentPathPt => _currentPath.PathPoints[index_currentPathPt];


        //[Header("STATE")]
        private bool movementIsPaused = false;
        public bool AmPaused => movementIsPaused;

       
		//[Header("SPATIAL")]
		/// <summary>
		/// The current hit that describes this agent's position on the navmesh
		/// </summary>
		private LNX_NavmeshHit _currentHit;
		/// <summary>
		/// The current hit that describes this agent's position on the navmesh
		/// </summary>
		public LNX_NavmeshHit CurrentHit => _currentHit;

        private Vector3 v_managedAgentPos;
        public Vector3 ManagedAgentPosition => v_managedAgentPos;

        private Vector3 v_toCurrentPathPt;
        public Vector3 V_toNextPathPt => v_toCurrentPathPt;

        private float distToCurrentPoint;
        public float DistanceToNextPt => distToCurrentPoint;

		[Header("FOLLOWING")]
        public float PathUpdateRefreshDuration = 0.2f;
		[Tooltip("Distance to follow followtrans to.")] 
		public float FollowDistance = 0.25f;
		[Tooltip("How far off the navmesh that the follow trans can be while still sampling a closest position")]
		public float Distance_SampleFollowTransOffMesh = 2f;
        private float cd_pathRefresh = 0f;
        public float CD_PathRefresnh => cd_pathRefresh;
        private LNX_NavmeshHit lastFollowTransHit;
		public UnityEvent Event_FollowTransformNoLongerSampled;


        //[Header("OTHER")]
		private Vector3 v_lastTransformPositionAtSample;
		private Vector3 v_lastFollowTransPosition_cached;


        [Header("DEBUG")]
        [SerializeField] private bool lockGizmos = false;
        public LNX_MethodDebugReport Rprt_Movement;

		private void Awake()
		{
			
		}

		void Start()
        {
            v_lastTransformPositionAtSample = transform.position + (Vector3.right); //this way it's guaranteed to not be the same and sample first time

			for ( int i = 0; i < MovementSchemas.Length; i++ )
            {
                MovementSchemas[i].CheckIfKosher();
            }
        }

		void FixedUpdate()
        {
            Rprt_Movement.StartReport();
            Rprt_Movement.StartMethod($"FixedUpdate()");
            Rprt_Movement.Log($"_currentHit: '{_currentHit}'" );

			#region SHORT-CIRCUITING ====================
			if (movementIsPaused)
			{
				Rprt_Movement.Log_And_End_Method($"movement paused. Returning early...");
				return;
			}
			if (_currentPath == null)
			{
				Rprt_Movement.Log_And_End_Method($"current path is null. Returning early...");
				return;
			}
			if ( index_currentPathPt < 0 )
			{
				Rprt_Movement.Log_And_End_Method($"index_currentPathPt is: '{index_currentPathPt}'. Returning early...");
				return;
			}
			#endregion

			#region SAMPLE UNDER-FOOT / CALCULATE EFFECTIVE ENTITY POSITION =================================
			bool underfootSampleSuccess = false;
			if ( !SampleFootingOnlyWhenPositionHasChanged || transform.position != v_lastTransformPositionAtSample)
			{
                if( !SampleUnderfoot(ref Rprt_Movement) && CurrentMovementSchema.OnlyMoveIfHaveFooting )
				{
					Rprt_Movement.Log_And_End_Method($"current movement scheme dictates underfoot sampling in order to move, but couldn't sample underfoot");
					return;
				}

				v_lastTransformPositionAtSample = transform.position;
			}
			#endregion

			Rprt_Movement.Log($"using v_agentPos: '{v_managedAgentPos}'");

			#region FOLLOW TRANSFORM LOGIC ============================================
			if ( _FollowTrans != null )
            {
                Rprt_Movement.Log($"following '{_FollowTrans}'...");
				if ( cd_pathRefresh > 0f )
                {
                    cd_pathRefresh -= Time.fixedDeltaTime;

					if ( cd_pathRefresh <= 0f )
                    {
                        if( _FollowTrans.position != v_lastFollowTransPosition_cached )
                        {
                            lastFollowTransHit = _manager.SampleClosestHit( _FollowTrans.position, Distance_SampleFollowTransOffMesh, true );
                            v_lastFollowTransPosition_cached = _FollowTrans.position;
                        }

						if ( lastFollowTransHit == LNX_NavmeshHit.None )
						{   //IF follow transform can't be sampled. Stop following and invoke event...
							
							StopFollowing();
							cd_pathRefresh = -1f;
							Event_FollowTransformNoLongerSampled.Invoke();
						}
						else
						{	//If follow transform was succesfully sampled, check if path needs to be refreshed...

							if (Vector3.Distance(transform.position, _FollowTrans.position) > FollowDistance)
							{
								SetDestination(lastFollowTransHit);
							}

							cd_pathRefresh = PathUpdateRefreshDuration;
						}
					}
                }
            }
			#endregion

            Rprt_Movement.Log($"Current path pt index: '{index_currentPathPt}'...",
                $"First generating next-point relational properties...");

			#region CALCULATE NEXT-POINT VALUES =================================================
			v_toCurrentPathPt = Vector3.Normalize( _CurrentPathPt.Position - v_managedAgentPos );
            Vector3 v_visTrnsUpTgt = Vector3.RotateTowards( VisualTransform.up, _CurrentPathPt.Normal, CurrentRotationSpeed * Time.fixedDeltaTime, 0.0f );
            float alignment_transUp_with_crntPthNrml = Vector3.Dot( VisualTransform.up, _CurrentPathPt.Normal );
            float alignment_transFwd_with_crntPthPos = Vector3.Dot( VisualTransform.forward, v_toCurrentPathPt);

            Quaternion q_finalRot = Quaternion.identity;
			#endregion

			#region GENERATE ROTATION VALUE ======================================================
			Rprt_Movement.EmptyLine();
			if (alignment_transFwd_with_crntPthPos < -0.98f && alignment_transUp_with_crntPthNrml > 0.95f )
			{
                Rprt_Movement.Log($"rotating q_finalRot indiscriminately...");
				q_finalRot = Quaternion.LookRotation
                (
					Vector3.RotateTowards(VisualTransform.forward, VisualTransform.right, CurrentRotationSpeed * Time.fixedDeltaTime, 0.0f), 
                    VisualTransform.up 
                );
			}
			else
			{
				Rprt_Movement.Log($"rotating q_finalRot discriminately...");
				q_finalRot = Quaternion.LookRotation
                (
                    Vector3.RotateTowards(VisualTransform.forward, v_toCurrentPathPt, CurrentRotationSpeed * Time.fixedDeltaTime, 0.0f), 
                    v_visTrnsUpTgt
                );
			}
			#endregion

			Rprt_Movement.EmptyLine();
			Rprt_Movement.Log($"USING v_toNextPathPt: '{v_toCurrentPathPt}', nxtPt: '{_CurrentPathPt.Position}'",
                $"v_visualTransUpOrientation: '{v_visTrnsUpTgt}'",
                $"alignment_transUp_with_crntPthNrml: '{alignment_transUp_with_crntPthNrml}'",
                $"alignment_transFwd_with_crntPthPos: '{alignment_transFwd_with_crntPthPos}'",
				$"q_finalRot: '{q_finalRot}'"
				);

			#region MOVE ENTITY =========================================================
			Rprt_Movement.EmptyLine();
			Rprt_Movement.Log($"now moving...");

			Vector3 v_posBeforeMove = transform.position;

			if ( CurrentMovementSchema._MovementMode == MovementMode.Transform_directionallyDriven )
            {
                Rprt_Movement.Log($"movement mode Transform_directionallyDriven...");
                VisualTransform.rotation = q_finalRot;

                transform.Translate(CurrentMoveSpeed * Time.fixedDeltaTime * v_toCurrentPathPt);
				Rprt_Movement.Log($"projected move amt: '{CurrentMoveSpeed * Time.fixedDeltaTime}'", 
					$"dist_prev to after move: '{Vector3.Distance(v_posBeforeMove, transform.position)}'");
			}
			else if ( CurrentMovementSchema._MovementMode == MovementMode.Transform_forwardDriven )
            {
				transform.rotation = q_finalRot;
				transform.Translate( VisualTransform.forward * CurrentMoveSpeed * Time.fixedDeltaTime );
            }
            else if (CurrentMovementSchema._MovementMode == MovementMode.Rigidbody_directionallyDriven)
            {
                _RigidBody.MoveRotation(q_finalRot);

                if (Vector3.Angle(VisualTransform.forward, v_toCurrentPathPt) < CurrentMovementSchema.RotationAlignmentThreshold)
                {
                    _RigidBody.MovePosition(_RigidBody.position + (CurrentMoveSpeed * Time.fixedDeltaTime * v_toCurrentPathPt));

                }
            }
			else if (CurrentMovementSchema._MovementMode == MovementMode.Rigidbody_forwardDriven)
			{
				_RigidBody.MoveRotation(q_finalRot);

				if (Vector3.Angle(VisualTransform.forward, v_toCurrentPathPt) < CurrentMovementSchema.RotationAlignmentThreshold)
				{
					_RigidBody.MovePosition(_RigidBody.position + (VisualTransform.forward * CurrentMoveSpeed * Time.fixedDeltaTime));

				}
			}
			#endregion

			Rprt_Movement.Log($"moved. Now adjusting currenthit and distance...");

			#region CHECK ADVANCEMENT =====================================================
			distToCurrentPoint = Vector3.Distance(transform.position, _CurrentPathPt.Position);
            Rprt_Movement.Log($"Checking if should advance. distToNextPt: '{distToCurrentPoint}' / '{CurrentMovementSchema.Dist_advancePathPoint}', " +
                $"index_currentPathPt: '{index_currentPathPt}'");
			
			if ( distToCurrentPoint <= MovementSchemas[index_currentMovementSchema].Dist_advancePathPoint )
			{
				Rprt_Movement.Log($"am within 'raycast check' distance. Advancing path point...");
				index_currentPathPt++;

				if ( index_currentPathPt > _currentPath.PointCount - 1 )
				{
					if (_FollowTrans == null && distToCurrentPoint <= MovementSchemas[index_currentMovementSchema].Dist_advancePathPoint)
					{
						UnsetPath();
					}
				}
			}
			else if 
			(
				Vector3.Distance(v_posBeforeMove, _CurrentPathPt.Position) < Vector3.Distance(v_posBeforeMove, transform.position)
			)
			{
				Rprt_Movement.Log($"distance suggests overshoot. Checking for overshoot...");
				Vector3 vNow = Vector3.Normalize(_CurrentPathPt.Position - transform.position);
				if ( Vector3.Dot(v_toCurrentPathPt, vNow) < -0.1f )
				{
					Rprt_Movement.Log($"overshoot happened!");
					Debug.LogWarning($"overshot happened! old: '{v_toCurrentPathPt}', new: '{vNow}'");
					Debug.Log($"dist before: '{Vector3.Distance(v_posBeforeMove, _CurrentPathPt.Position)}', " +
						$"after: '{Vector3.Distance(transform.position, _CurrentPathPt.Position)}'");
					Debug.Log($"pos before: '{v_posBeforeMove}', after: '{transform.position}'");
					index_currentPathPt++;

					if (index_currentPathPt >= _currentPath.PointCount - 1)
					{
						if (_FollowTrans == null && distToCurrentPoint <= MovementSchemas[index_currentMovementSchema].Dist_advancePathPoint)
						{
							UnsetPath();
						}
					}
				}
			}
			#endregion

			Rprt_Movement.EndMethod($"FixedUpdate()");

		}

		public void SetManager( LNX_Manager mgr )
        {
            _manager = mgr;
        }

		public void SetDestination(LNX_NavmeshHit endHit, float maxSampleDistance = 1f, bool considerClosesetOffPerimeter = true)
		{
			//Debug.Log($"SetDestination(endHit: '{endHit}', maxSampleDistance: '{maxSampleDistance}')");
			//string dbgMthd = $"SetDestination(endHit: '{endHit}', maxSampleDistance: '{maxSampleDistance}')\n";

			//dbgMthd += $"sampling underfoot...\n";
            SampleUnderfoot();
			//dbgMthd += $"sampling complete. _currentHit: '{_currentHit}', footSampleHit: '{footSampleHit}'\n";

			bool rslt = false;
			if (_currentHit != LNX_NavmeshHit.None)
			{
				//dbgMthd += $"_currentHit IS valid. Calling CalculatePath()...\n";
				rslt = _manager.CalculatePath(_currentHit, endHit, maxSampleDistance, out _currentPath);
				//dbgMthd += $"CalculatePath() finisned. Rslt: '{rslt}'\n";
			}
			else
			{
				//dbgMthd += $"_currentHit was none. This means the agent is NOT on the navmesh...\n";

				rslt = _manager.CalculatePath(
					transform.position, endHit.Position, maxSampleDistance, out _currentPath, considerClosesetOffPerimeter
				); //<<<<<<<<<<<<<<<<<<<<<<<<<<
				//dbgMthd += $"CalculatePath() finisned. Rslt: '{rslt}'\n";
			}

			if (rslt)
			{
				index_currentPathPt = 0;
			}
			else
			{
				index_currentPathPt = -1;
			}
			//dbgMthd += $"index_currentPathPt: '{index_currentPathPt}'\n";

			//Debug.Log($"rprt-----\n" +
			//	$"{dbgMthd}");
		}


		public void StartFollowing( Transform trans )
        {
			Debug.Log($"StartFollowing()");

			_FollowTrans = trans;
            cd_pathRefresh = PathUpdateRefreshDuration;

			v_lastFollowTransPosition_cached = _FollowTrans.position;
			lastFollowTransHit = _manager.SampleClosestHit(_FollowTrans.position, 10f, true);
			SetDestination( lastFollowTransHit );
		}

        public void StopFollowing()
        {
			Debug.Log($"StopFollowing()");

			_FollowTrans = null;
            cd_pathRefresh = -1f;

            UnsetPath();
        }

        public void UnsetPath()
        {
            Debug.Log($"UnsetPath()");

            index_currentPathPt = -1;
            _currentPath = null;
        }

		public bool SampleUnderfoot()
		{
			#region DETERMINE CURRENTHIT =========================================================
			_currentHit = LNX_NavmeshHit.None;

			if (_manager.Surfaces[Index_CurrentSurface].PositionIsInShapeProject(transform.position, out _currentHit))
			{
				v_managedAgentPos = _currentHit.Position;
			}
			#endregion

			if (FootingSamplers == null || FootingSamplers.Count <= 0)
			{
				footSampleHit = new RaycastHit();
				return false;
			}

			#region DETERMINE FOOTING =================================================
			averageFootSampleNormal = Vector3.zero;
			Vector3 addedNormals = Vector3.zero;
			int succesfulHits = 0;
			for (int i = 0; i < FootingSamplers.Count; i++)
			{
				if (FootingSamplers[i].Distance <= 0f)
				{
					Debug.LogWarning($"LNX WARNING! You called SampleUnderfoot(), but sampler {i} had an invalid distance of " +
						$"'{FootingSamplers[i].Distance}'...");
				}

				RaycastHit lineHit = new RaycastHit();
				if
				(
					Physics.Linecast(
						transform.position + (transform.rotation * FootingSamplers[i].StartPosition),
						transform.position + (transform.rotation * FootingSamplers[i].StartPosition) +
							(transform.rotation * Vector3.down * FootingSamplers[i].Distance),
						out lineHit,
						FootingLayerMask
					)
				)
				{
					addedNormals += lineHit.normal;
					succesfulHits++;
					if (i == 0)
					{
						footSampleHit = lineHit;
						if (_currentHit == LNX_NavmeshHit.None)
						{
							v_managedAgentPos = footSampleHit.point;
						}
					}
				}
			}

			averageFootSampleNormal = addedNormals / succesfulHits;
			#endregion

			return succesfulHits > 0;
		}
		public bool SampleUnderfoot( ref LNX_MethodDebugReport rprt)
        {
            rprt.StartMethod($"SampleUnderfoot()");

			#region DETERMINE CURRENTHIT =========================================================
			_currentHit = LNX_NavmeshHit.None;

			rprt.Log($"starting with PositionIsInShapeProject() in order to determine _currentHit...");
			if (_manager.Surfaces[Index_CurrentSurface].PositionIsInShapeProject(transform.position, out _currentHit))
			{
				rprt.Log($"PositionIsInShapeProject() returned true...");

				v_managedAgentPos = _currentHit.Position;
			}
			else
			{
				rprt.Log($"PositionIsInShapeProject() returned false!...");
			}
			#endregion

			if (FootingSamplers == null || FootingSamplers.Count <= 0)
			{
				rprt.Log_And_End_Method($"FootingSamplers not set. Returning early...");
				footSampleHit = new RaycastHit();
				return false;
			}

			#region DETERMINE FOOTING =================================================
			rprt.Log($"Determining footing...");

			averageFootSampleNormal = Vector3.zero;
            Vector3 addedNormals = Vector3.zero;
            int succesfulHits = 0;
            rprt.Log($"now checking '{FootingSamplers.Count}' FootingSamplers...");
            for ( int i = 0; i < FootingSamplers.Count; i++ )
            {
                if( FootingSamplers[i].Distance <= 0f )
                {
					Debug.LogWarning($"LNX WARNING! You called SampleUnderfoot(), but sampler {i} had an invalid distance of " +
                        $"'{FootingSamplers[i].Distance}'...");
					rprt.Log($"LNX WARNING! You called SampleUnderfoot(), but sampler {i} had an invalid distance of " +
	                    $"'{FootingSamplers[i].Distance}'...");
				}
				
                RaycastHit lineHit = new RaycastHit();
                if 
                ( 
                    Physics.Linecast(
                        transform.position + (transform.rotation * FootingSamplers[i].StartPosition),
					    transform.position + (transform.rotation * FootingSamplers[i].StartPosition) + 
                            (transform.rotation * Vector3.down * FootingSamplers[i].Distance),
                        out lineHit,
                        FootingLayerMask
				    )
                )
                {
                    rprt.Log($"succesful linecast. hit pt: '{lineHit.point}', hit nrml: '{lineHit.normal}'...");

                    addedNormals += lineHit.normal;
                    succesfulHits++;
                    if( i == 0 )
                    {
                        footSampleHit = lineHit;
						if ( _currentHit == LNX_NavmeshHit.None )
						{
							v_managedAgentPos = footSampleHit.point;
						}
                    }
                }
			}

            averageFootSampleNormal = addedNormals / succesfulHits;
			#endregion

			rprt.Log_And_End_Method($"end of method. v_managedAgentPos: '{v_managedAgentPos}', averageFootSampleNormal: '{averageFootSampleNormal}'");
            return succesfulHits > 0;
		}

		private void OnDrawGizmos()
		{
			if
            ( 
                !lockGizmos && 
                Selection.activeGameObject != gameObject                
            )
            {
                return;
            }

            for ( int i = 0; i < FootingSamplers.Count; ++i )
            {
                Gizmos.DrawLine(
                    transform.position + (transform.rotation * FootingSamplers[i].StartPosition),
					transform.position + (transform.rotation * FootingSamplers[i].StartPosition) + (transform.rotation * Vector3.down * FootingSamplers[i].Distance)
                );
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
		[Tooltip("Distance within this agent will consider itself 'close enough' and start towards the next point")] 
        public float Dist_advancePathPoint;


		[Header("ROTATION")]
		[SerializeField] private float rotationSpeed;
        public float RotationSpeed => rotationSpeed;
		[SerializeField] private float rotationAcceleration;
		[SerializeField] private bool rotationAccelerationIsSloped;

		[Range(0f, 360f), Tooltip("How closely the agent has to be facing the next path target in order to allow movment forward")]
		public float RotationAlignmentThreshold;

		[Header("OPTIONS")]
		public bool OnlyMoveIfHaveFooting;

		public bool CheckIfKosher()
        {
            if(moveSpeed <= 0 )
            {
                Debug.LogError($"LNX ERROR! Movement schema '{Name}' has a moveSpeed with a negative value.");
                return false;
            }
			if (Dist_advancePathPoint <= 0)
			{
				Debug.LogError($"LNX ERROR! Movement schema '{Name}' has a Dist_advancePathPoint with a negative value.");
				return false;
			}
			if (rotationSpeed <= 0)
			{
				Debug.LogError($"LNX ERROR! Movement schema '{Name}' has a rotationSpeed with a negative value.");
				return false;
			}
			if (RotationAlignmentThreshold <= 0)
			{
				Debug.LogError($"LNX ERROR! Movement schema '{Name}' has a RotationAlignmentThreshold with a negative value.");
				return false;
			}

            return true;
        }
    }

    [System.Serializable]
    public struct FootingSampler
    {
        public Vector3 StartPosition;
        public float Distance;
    }
}
