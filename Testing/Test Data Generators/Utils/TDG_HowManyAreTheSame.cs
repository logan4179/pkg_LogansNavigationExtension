using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LogansNavigationExtension
{
    public class TDG_HowManyAreTheSame : TDG_base
    {
        public Vector3[] vectors;

		public int CurrentOperationResult = 0;

		//[Header("DEBUG")]

		protected override void OnDrawGizmos()
		{
			if
			(
				Selection.activeGameObject != gameObject
			)
			{
				DBG_Operation = $"OnDrawGizmos short-circuit. Valid object not selected";
				return;
			}

			DBG_Operation = "";
			CurrentOperationResult = 0;

			DBG_Operation += $"Commencing operation...\n";

			CurrentOperationResult = LNX_Utils.HowManyAreTheSame( vectors );

			DBG_Operation += $"\nEnd of operation. Result: '{CurrentOperationResult}'\n";
		}
	}
}
