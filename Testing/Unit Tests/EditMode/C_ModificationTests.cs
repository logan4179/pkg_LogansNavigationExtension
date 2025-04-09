using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LogansNavigationExtension;
using UnityEngine.AI;
using JetBrains.Annotations;
using System.IO;

namespace LoganLand.LogansNavmeshExtension.Tests
{
    public class C_ModificationTests
    {
		/// <summary>
		/// The Navmesh that is created by calculating it off the scene geometry as opposed to 
		/// creating from a saved json object.
		/// </summary>
        LNX_NavMesh _generatedNavmesh;

		LNX_MeshManipulator _lnx_meshManipulator;


		#region A - Setup --------------------------------------------------------------------------------
		[Test]
		public void a1_SetupObjects()
		{
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(a1_SetupObjects)) );
			GameObject go = GameObject.Find("TestLNX_Navmesh");

			_generatedNavmesh = go.GetComponent<LNX_NavMesh>();

			_lnx_meshManipulator = go.AddComponent<LNX_MeshManipulator>();
			_lnx_meshManipulator._LNX_NavMesh = _generatedNavmesh;

			Assert.NotNull(_generatedNavmesh);
		}
		#endregion

		#region B - Modification ---------------------------------------------------------------------
		LNX_ComponentCoordinate vertToMove;
		Vector3 v_moveTo, v_origPos;
		LNX_Vertex moveVert;
		[Test]
		public void b1_MoveVertThenCheckThereAreModifications()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b1_MoveVertThenCheckThereAreModifications)));

			vertToMove = new LNX_ComponentCoordinate() { TrianglesIndex = 0, ComponentIndex = 0 };

			Debug.Log($"Making sure modifications are clear and navmesh is freshly calculated. currently showing '{_generatedNavmesh.HaveModifications()}'...");
			_generatedNavmesh.ClearModifications(); //need to clear mods to be sure...
			_generatedNavmesh.CalculateTriangulation(); //need to re-calculate to be sure...

			Assert.False(_generatedNavmesh.HaveModifications());

			_lnx_meshManipulator.ClearSelection();
			_lnx_meshManipulator.ChangeSelectMode( LNX_SelectMode.Vertices );
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestSectionEndString, "setup") );

			Debug.Log($"calculating and caching test values...");
			moveVert = _generatedNavmesh.GetVertexAtCoordinate( vertToMove );
			v_moveTo = moveVert.Position + (Vector3.up * 0.5f);
			v_origPos = moveVert.Position;

			Debug.Log($"making sure that currently the .Position and .OriginalPosition on this vert are the same...");
			UnityEngine.Assertions.Assert.AreApproximatelyEqual(
				moveVert.OriginalPosition.x, moveVert.Position.x
			);
			UnityEngine.Assertions.Assert.AreApproximatelyEqual(
				moveVert.OriginalPosition.y, moveVert.Position.y
			);
			UnityEngine.Assertions.Assert.AreApproximatelyEqual(
				moveVert.OriginalPosition.z, moveVert.Position.z
			);
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestSectionEndString, "calculating/caching"));


			Debug.Log( $"attempting point at vert '{vertToMove.ToString()}'..." );
			_lnx_meshManipulator.TryPointAtComponentViaDirection(
				moveVert.Position + (Vector3.up * 3f),
				Vector3.down
			);


			if (_lnx_meshManipulator.Vert_CurrentlyPointingAt == null)
			{
				Debug.LogError( "apparently pointing didn't work. Returning early..." );
				return;
			}
			else
			{
				Debug.Log("apparently pointing DID work. Proceding with grab attempt...");

				_lnx_meshManipulator.TryGrab();

				if( _lnx_meshManipulator.Verts_currentlySelected == null )
				{
					Debug.LogError("apparently grabbing didn't work. Returning early...");
					return;
				}
				else
				{
					Debug.Log($"moving vert. vert current pos: '{moveVert.Position}'");

					_lnx_meshManipulator.MoveSelectedVerts( v_moveTo );

					Debug.Log($"after move, vert current pos: '{moveVert.Position}'. Checking that there are modifications...");

					Assert.True( _generatedNavmesh.HaveModifications() );
				}
			}
		}

		[Test]
		public void b2_RecalculateTriangulationAfterMove()
		{
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b2_RecalculateTriangulationAfterMove)) );

			Debug.Log($"Recalculating triangulation....");
			_generatedNavmesh.CalculateTriangulation();

			Debug.Log($"Checking if navmesh still considers itself to be modified after recalculating triangulation....");
			Assert.True( _generatedNavmesh.HaveModifications() );

			Debug.Log($"checking that the vert is still in the same moved position...");
			UnityEngine.Assertions.Assert.AreApproximatelyEqual(
				v_moveTo.x, moveVert.Position.x
			);
			UnityEngine.Assertions.Assert.AreApproximatelyEqual(
				v_moveTo.y, moveVert.Position.y
			);
			UnityEngine.Assertions.Assert.AreApproximatelyEqual(
				v_moveTo.z, moveVert.Position.z
			);

			Debug.Log($"checking that the vert's originalPosition value is maintained...");
			UnityEngine.Assertions.Assert.AreApproximatelyEqual(
				v_origPos.x, moveVert.OriginalPosition.x
			);
			UnityEngine.Assertions.Assert.AreApproximatelyEqual(
				v_origPos.y, moveVert.OriginalPosition.y
			);
			UnityEngine.Assertions.Assert.AreApproximatelyEqual(
				v_origPos.z, moveVert.OriginalPosition.z
			);
		}
		#endregion
	}
}

