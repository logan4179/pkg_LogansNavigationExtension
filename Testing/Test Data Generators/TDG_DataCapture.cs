using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace LogansNavigationExtension
{
    [System.Serializable]
    public class TDG_DataCapture
    {
		//[Header("VECTOR")]
		public bool AmDoingVectorCaptureLists;
		public List<TDG_VectorCaptureList> VectorCaptureLists;

		//[Header("BOOLEAN")]
		[Space(15f)]
		public bool AmDoingBooleanCaptureList;
		public TDG_BooleanCaptureList BooleanCaptureList;

		//[Header("COMPONENT COORDINATE")]
		/*[Space(15f)]
		public bool AmDoingCoordinateCaptureLists;
		public List<TDG_ComponentCoordinateCaptureList> CoordinateCaptureLists;*/

		[Header("DEBUG")]
		public int Index_FocusOn = -1;
		public List<int> Indices_ProblemPoints;
		public Color Color_DataPointCapture;

		public bool VectorCaptureListsHaveEqualCount()
		{
			if (VectorCaptureLists == null || VectorCaptureLists.Count <= 0)
			{
				Debug.LogWarning($"LNX WARNING! There are no VectorCaptureLists. You do know that right?");
				return true;
			}

			int firstCount = VectorCaptureLists[0].vectors.Count;
			for (int i = 0; i < VectorCaptureLists.Count; i++)
			{
				if(VectorCaptureLists[i].vectors.Count != firstCount )
				{
					Debug.LogWarning( $"Vector capture lists do NOT have the same vector counts..." );
					return false;
				}
			}

			return true;
		}

		#region CAPTURING ======================================================================
		public bool CaptureDataPoint( params Vector3[] vectors )
		{
			if( !AmDoingVectorCaptureLists )
			{
				Debug.LogError($"LNX ERROR! This dataCapture object has '{nameof(AmDoingVectorCaptureLists)}' marked false. " +
					$"Are you sure you want to capture vectors?");
				return false;
			}
			if( VectorCaptureLists == null || VectorCaptureLists.Count <= 0 )
			{
				Debug.LogError($"LNX ERROR! There are no VectorCaptureLists set up to capture vector data...");
				return false;
			}

			if( vectors.Length != VectorCaptureLists.Count )
			{
				Debug.LogError($"LNX ERROR! Passed vectors length '{vectors.Length}' is not the same as " +
					$"{nameof(VectorCaptureLists)}.Count: '{VectorCaptureLists.Count}'...");
				return false;
			}

			if( !VectorCaptureListsHaveEqualCount() )
			{
				Debug.LogError( $"LNX ERROR! Found that vector lists don't have the same count." );
				return false;
			}

			for( int i = 0; i < VectorCaptureLists.Count; i++ )
			{
				VectorCaptureLists[i].vectors.Add( vectors[i] );

				Debug.DrawRay( vectors[i], Vector3.up, Color_DataPointCapture, 2f );
			}

			return true;
		}

		public bool CaptureDataPoint(bool b, params Vector3[] vectors )
		{
			if( !AmDoingBooleanCaptureList )
			{
				Debug.LogError($"LNX ERROR! This dataCapture object has '{nameof(AmDoingBooleanCaptureList)}' marked false. " +
					$"Are you sure you want to capture booleans?");
				return false;
			}
			if ( BooleanCaptureList == null || BooleanCaptureList.booleans == null )
			{
				Debug.LogError($"LNX ERROR! There are no BooleanCaptureLists set up to capture boolean data...");
				return false;
			}

			if ( !CaptureDataPoint(vectors) )
			{
				return false;
			}

			BooleanCaptureList.booleans.Add(b);

			return true;
		}

		public void CaptureDataPoint(bool b, LNX_ComponentCoordinate coord, params Vector3[] vectors)
		{

		}

		public void CaptureDataPoint(LNX_ComponentCoordinate coord, params Vector3[] vectors)
		{

		}
		#endregion ------------------------------------------------

		public void SendTo( int indx )
		{
			if ( AmDoingVectorCaptureLists )
			{
				for (int i = 0; i < VectorCaptureLists.Count; i++)
				{
					VectorCaptureLists[i].Trans_SendToVector.position = VectorCaptureLists[i].vectors[indx];
				}
			}
		}
	}

	[System.Serializable]
	public class TDG_DataEntry //was thinking maybe this could somehow take the place of all the capture lists below...maybe not...
	{

	}

	[System.Serializable]
	public class TDG_VectorCaptureList
	{
		public string Name;

		[Tooltip("Optional reference that allows you to send a transform to a vector point for diagnosing")]
		public Transform Trans_SendToVector;

		public List<Vector3> vectors;
	}

	[System.Serializable]
	public class TDG_BooleanCaptureList
	{
		public string Name;
		public List<bool> booleans;
	}

	[System.Serializable]
	public class TDG_ComponentCoordinateCaptureList
	{
		public string Name;
		public List<LNX_ComponentCoordinate> coordinates;
	}
}

