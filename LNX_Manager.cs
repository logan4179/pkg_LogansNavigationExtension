using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using LogansNavigationExtension.AI;

namespace LogansNavigationExtension
{
    public class LNX_Manager : MonoBehaviour
    {
        public static LNX_Manager Instance;

        public List<LNX_NavMeshSurface> Surfaces;
        public List<LNX_Agent> Agents;

		private void Awake()
		{
            Instance = this;

            for (int i = 0; i < Surfaces.Count; i++)
            {
                
            }

            for (int i = 0; i < Agents.Count; i++)
            {
                Agents[i].SetManager(this);
            }
		}

		void Start()
        {
			for (int i = 0; i < Surfaces.Count; i++)
			{
                Surfaces[i].MyCollectionIndex = i;
			}

			for (int i = 0; i < Agents.Count; i++)
			{
				Agents[i].SetManager(this);
			}
		}

        public LNX_NavmeshHit SampleClosestHit( Vector3 pos, float maxSampleDistance, bool considerClosesetOffPerimeter)
        {
            LNX_NavmeshHit returnHit = LNX_NavmeshHit.None;

            Surfaces[0].SamplePosition(pos, out returnHit, maxSampleDistance, considerClosesetOffPerimeter );

            return returnHit;
        }

		public bool CalculatePath( LNX_NavmeshHit startHit, LNX_NavmeshHit endHit, float maxSampleDistance, out LNX_Path path )
		{
			path = new LNX_Path();

            return Surfaces[0].CalculatePath( startHit, endHit, out path );
		}
		public bool CalculatePath( Vector3 startPt, Vector3 endPt, float maxSampleDistance, out LNX_Path path, 
            bool considerClosesetOffPerimeter)
        {
            LNX_NavmeshHit strtHt = SampleClosestHit(startPt, maxSampleDistance, considerClosesetOffPerimeter);
            LNX_NavmeshHit endHt = SampleClosestHit(endPt, maxSampleDistance, considerClosesetOffPerimeter);
			path = new LNX_Path();

            if( strtHt != LNX_NavmeshHit.None )
            {
                Debug.LogError($"LNX ERROR! Couldn't sample start position. Returning early");
                return false;
            }
			if ( endHt != LNX_NavmeshHit.None )
			{
				Debug.LogError($"LNX ERROR! Couldn't sample end position. Returning early");
				return false;
			}

            return CalculatePath( strtHt, endHt, maxSampleDistance, out path );
		}

		public bool Raycast(LNX_NavmeshHit startHit, Vector3 projectDir, out LNX_Path outPath, float allowedDistance, bool allowRelationships = false)
        {
            return Surfaces[0].Raycast( startHit, projectDir, out outPath, allowedDistance, allowRelationships );
        }
		public bool Raycast_dbg(LNX_NavmeshHit startHit, Vector3 projectDir, out LNX_Path outPath, float allowedDistance, 
            ref LNX_MethodDebugReport rprt, bool allowRelationships = false)
		{

			return Surfaces[0].Raycast_dbg(startHit, projectDir, out outPath, allowedDistance, ref rprt, allowRelationships);
		}
	}
}
