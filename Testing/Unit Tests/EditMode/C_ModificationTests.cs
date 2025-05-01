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
        LNX_NavMesh _sceneGeneratedNavmesh;

		LNX_MeshManipulator _lnx_meshManipulator;


		#region A - Setup --------------------------------------------------------------------------------
		[Test]
		public void a1_SetupObjects()
		{
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(a1_SetupObjects)) );
			GameObject go = GameObject.Find( LNX_UnitTestUtilities.Name_GeneratedNavmeshGameobject );

			_sceneGeneratedNavmesh = go.GetComponent<LNX_NavMesh>();

			_lnx_meshManipulator = go.AddComponent<LNX_MeshManipulator>();
			_lnx_meshManipulator._LNX_NavMesh = _sceneGeneratedNavmesh;

			Assert.NotNull(_sceneGeneratedNavmesh);
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

			Debug.Log($"Making sure modifications are clear and navmesh is freshly calculated. currently showing " +
				$"'{_sceneGeneratedNavmesh.HaveModifications()}'. now calling {nameof(_sceneGeneratedNavmesh.ClearModifications)}()...");
			_sceneGeneratedNavmesh.ClearModifications(); //need to clear mods to be sure...

			Debug.Log($"calling {nameof(_sceneGeneratedNavmesh.CalculateTriangulation)}() in order to ensure a fresh creation free of mods...");
			_sceneGeneratedNavmesh.CalculateTriangulation();

			Assert.False( _sceneGeneratedNavmesh.HaveModifications() );

			Debug.Log($"calling {nameof(_lnx_meshManipulator.ClearSelection)}()...");
			_lnx_meshManipulator.ClearSelection();

			Debug.Log($"changing selectmode to vertices...");
			_lnx_meshManipulator.ChangeSelectMode( LNX_SelectMode.Vertices );
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestSectionEndString, "setup") );
			//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

			Debug.Log($"calculating and caching test values...");

			vertToMove = new LNX_ComponentCoordinate() { TrianglesIndex = 0, ComponentIndex = 0 }; //just arbitrarily picking the first tri and vert for this...

			moveVert = _sceneGeneratedNavmesh.GetVertexAtCoordinate( vertToMove );
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

					Assert.True( _sceneGeneratedNavmesh.HaveModifications() );
				}
			}
		}

		[Test]
		public void b2_RecalculateTriangulationAfterMove()
		{
			Debug.Log( string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b2_RecalculateTriangulationAfterMove)) );

			Debug.Log($"Recalculating triangulation....");
			_sceneGeneratedNavmesh.CalculateTriangulation();

			Debug.Log($"Checking if navmesh still considers itself to be modified after recalculating triangulation....");
			Assert.True( _sceneGeneratedNavmesh.HaveModifications() );

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

		[Test]
		public void b3_checkThatGreatestVisMeshIndexIsSameAfterMovingVert()
		{
			Debug.Log(string.Format(LNX_UnitTestUtilities.UnitTestMethodBeginString, nameof(b3_checkThatGreatestVisMeshIndexIsSameAfterMovingVert)));

			int lrgst = 0;
			Debug.Log($"Finding largest mesh vis index for meshes...");
			for (int i_triangles = 0; i_triangles < _sceneGeneratedNavmesh.Triangles.Length; i_triangles++)
			{
				for (int i_verts = 0; i_verts < 3; i_verts++)
				{
					if ( _sceneGeneratedNavmesh.Triangles[i_triangles].Verts[i_verts].Index_VisMesh_Vertices > lrgst )
					{
						lrgst = _sceneGeneratedNavmesh.Triangles[i_triangles].Verts[i_verts].Index_VisMesh_Vertices;
					}
				}
			}

			Debug.Log($"End of search. largest vis mesh index was: '{lrgst}'. Trying assert...");

			Assert.AreEqual( A_SetUpTests.largestMeshVisIndex_sceneGenerated, lrgst );
		}
		#endregion
	}
}

