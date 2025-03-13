using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;


namespace LogansNavigationExtension
{
	public static class LNX_Utils
	{
		/// <summary>
		/// Casts multiple times in a cross formation around origin. If any of the casts finds a hit, it stops the operation immediately and returns true.
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="radius"></param>
		/// <param name="layerMask"></param>
		/// <returns></returns>
		public static bool CrossCast(Vector3 origin, float radius, out RaycastHit hitInfo, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
		{
			if (Physics.Linecast(origin + (Vector3.up * radius), origin + (Vector3.down * radius), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}
			else if (Physics.Linecast(origin + (Vector3.down * radius), origin + (Vector3.up * radius), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}

			if (Physics.Linecast(origin + (Vector3.right * radius), origin + (Vector3.left * radius), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}
			else if (Physics.Linecast(origin + (Vector3.left * radius), origin + (Vector3.right * radius), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}

			if (Physics.Linecast(origin + (Vector3.forward * radius), origin + (Vector3.back * radius), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}
			else if (Physics.Linecast(origin + (Vector3.back * radius), origin + (Vector3.forward * radius), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}
			return false;
		}

		public static bool CrossCast(Vector3 origin, Vector3 end, float extendCastDist, out RaycastHit hitInfo, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
		{
			Vector3 vExtend = Vector3.Normalize(end - origin) * extendCastDist;
			Debug.Log($"vExtend: '{vExtend}'");

			if (Physics.Linecast(origin - vExtend, end + vExtend, out hitInfo, layerMask, queryTriggerInteraction))
			{
				Debug.Log("crosscast immediately made hit");
				return true;
			}
			else if (Physics.Linecast(end + vExtend, origin - vExtend, out hitInfo, layerMask, queryTriggerInteraction))
			{
				Debug.Log("crosscast immediately made reverse hit");

				return true;
			}

			Debug.Log($"crosscast wasn't immediately succesful for pt: '{origin}'");


			float dist = Vector3.Distance(origin, end);

			if (Physics.Linecast(origin + (Vector3.up * dist), origin + (Vector3.down * dist), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}
			else if (Physics.Linecast(origin + (Vector3.down * dist), origin + (Vector3.up * dist), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}

			if (Physics.Linecast(origin + (Vector3.right * dist), origin + (Vector3.left * dist), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}
			else if (Physics.Linecast(origin + (Vector3.left * dist), origin + (Vector3.right * dist), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}

			if (Physics.Linecast(origin + (Vector3.forward * dist), origin + (Vector3.back * dist), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}
			else if (Physics.Linecast(origin + (Vector3.back * dist), origin + (Vector3.forward * dist), out hitInfo, layerMask, queryTriggerInteraction))
			{
				return true;
			}

			Debug.LogWarning($"crosscast ultimately failed for pt: '{origin}'");
			return false;
		}

		public static Vector3 CreateCornerPathPoint( LNX_PathPoint startPt, LNX_PathPoint endPt )
		{
			Vector3 resultPt = Vector3.zero;

			/*
			Vector3 v_starPtTToEndPt = (endPt.V_point - startPt.V_point);
			Vector3 v_endPtToStartPt = -v_starPtTToEndPt;

			Vector3 v_startPtToEndPt_onPtNormal = Vector3.ProjectOnPlane(v_starPtTToEndPt, startPt.V_normal);
			Vector3 v_endPtToStartPt_onEndPtNormal = Vector3.ProjectOnPlane(v_endPtToStartPt, endPt.V_normal);
			Vector3 v_startPtToEndPt_onEndPtNormal = Vector3.ProjectOnPlane(v_starPtTToEndPt, endPt.V_normal);

			//Vector3 v_ptToIntersectedPt_onPtNormal_cross = Vector3.Cross(pt.V_normal, v_ptToIntersectedPt_onPtNormal); //for if I want the cross vector...
			Vector3 v_try = (startPt.V_point + v_startPtToEndPt_onPtNormal) - endPt.V_point;

			Vector3 vectorParam = v_starPtTToEndPt;
			Vector3 normalParam = v_startPtToEndPt_onPtNormal;

			Vector3 v_projected = Vector3.ProjectOnPlane(vectorParam, normalParam);
			Debug.DrawLine(startPt.V_point, startPt.V_point + vectorParam, Color.red, 4f);
			Debug.DrawLine(startPt.V_point, startPt.V_point + normalParam, Color.green, 4f);
			Debug.DrawLine(endPt.V_point, endPt.V_point + v_try, Color.blue, 4f);

			float angleA = 90f - Vector3.Angle( startPt.V_normal, v_starPtTToEndPt );
			float angleB = 90f - Vector3.Angle( endPt.V_normal, v_endPtToStartPt );
			float unknownAngle = 180f - angleA - angleB;
			*/


			//https://math.libretexts.org/Bookshelves/Algebra/Algebra_and_Trigonometry_1e_(OpenStax)/10%3A_Further_Applications_of_Trigonometry/10.01%3A_Non-right_Triangles_-_Law_of_Sines

			Vector3 v_starPtTToEndPt = (endPt.V_point - startPt.V_point);
			Vector3 v_endPtToStartPt = -v_starPtTToEndPt;
			float dist_hypotenuse = v_endPtToStartPt.magnitude;
			float angleA = 90f - Vector3.Angle(startPt.V_normal, v_starPtTToEndPt.normalized);
			float angleB = 90f - Vector3.Angle(endPt.V_normal, v_endPtToStartPt.normalized);
			float angle_opposingHypotenuse = 180f - angleA - angleB;

			//note: need to convert to radians in the following, as opposed to degrees...
			float distA = Mathf.Sin(Mathf.Deg2Rad * angleB) * (dist_hypotenuse / Mathf.Sin(Mathf.Deg2Rad * angle_opposingHypotenuse)); //This is a re-ordered algebraic equation based on trigonometry

			resultPt = startPt.V_point + Vector3.ProjectOnPlane(v_starPtTToEndPt, startPt.V_normal).normalized * distA;



			/*
			Debug.Log($"CreateCornerPathPoint()----------\n" +
				$"{nameof(dist_hypotenuse)}: '{dist_hypotenuse}', {nameof(angleA)}: '{angleA}', {nameof(angleB)}, '{angleB}'\n" +
				$"{nameof(angle_opposingHypotenuse)}: '{angle_opposingHypotenuse}', {nameof(distA)}: '{distA}\n" +
				$"" +
				$"{nameof(resultPt)}: '{resultPt}'");



			Debug.Log($"math report...\n" + $"angleB: '{angleB}', sin(angleB): '{Mathf.Sin(angleB)}'\n" + 
				$"angle_opposingHypotenuse: '{angle_opposingHypotenuse}', sin(angle_opposingHypotenuse): '{angle_opposingHypotenuse}'");
			*/

			return resultPt;
		}

		public static Vector3 GetCenterVector( Vector3[] corners )
		{
			Vector3 vCenter = Vector3.zero;

			for ( int i = 0; i < corners.Length; i++ )
			{
				vCenter += corners[i];
			}

			return vCenter / corners.Length;
		}

		#region FOR COMPONENT SELECTION ("GRABBING")-------------------------
		//could put methods in here to shorten constructing the list of vertices grabbed by various components... idk if it's worth it...
		#endregion

		#region FOR MESH MANIPULATION-------------------------
		public static bool AmPointingAt( Vector3 vOrigin, Vector3 vProjection, Vector3 vCenter, Vector3[] corners )
		{
			Vector3 v_originToCenter = Vector3.Normalize( vCenter - vOrigin );
			float dot_projectionToCenter = Vector3.Dot( v_originToCenter, vProjection );

			for ( int i = 0; i < corners.Length; i++ )
			{
				Vector3 v_originToCorner = Vector3.Normalize( corners[i] - vOrigin );

				if( dot_projectionToCenter < Vector3.Dot(v_originToCorner, v_originToCenter) )
				{
					return false;
				}
			}

			return true;
		}
		/*
		public static bool AmPointingAt( Vector3 vOrigin, Vector3 vProjection, Vector3[] corners, out string dbgString )
		{
			dbgString = $"perspective : '{vOrigin}' vProj: '{vProjection}' \n";
			Vector3 v_center = GetCenterVector( corners );
			
			dbgString += $"ctr of mesh: '{v_center}' \n";

			Vector3 v_originToCenter = Vector3.Normalize( v_center - vOrigin );
			Vector3 v_originToProjection = Vector3.Normalize( (vOrigin + vProjection) - vOrigin );
			float dot_mouseProjAlignedWithCtr = Vector3.Dot( v_originToCenter, v_originToProjection );
			dbgString += $"v_orgnToCtr: '{v_originToCenter}' \n";
			dbgString += $"v_originToProjection: '{v_originToProjection}'\n";
			dbgString += $" dot_prjToCtr: '{dot_mouseProjAlignedWithCtr}'\n\n";

			for (int i = 0; i < corners.Length; i++)
			{
				Vector3 v_originToCrnr = Vector3.Normalize(corners[i] - vOrigin);
				dbgString += $"v_originToCrnr: '{v_originToCrnr}' dot '{Vector3.Dot(v_originToCrnr, v_originToCenter)}'\n";

				if (dot_mouseProjAlignedWithCtr < Vector3.Dot(v_originToCrnr, v_originToCenter) )
				{
					dbgString += $"failed at corner: '{i}'";
					return false;
				}
			}

			return true;
		}
		*/

		// The idea: every point on the triangle will have at least one other point where the dot product of the vToPtA is more aligned with vToProjection 
		// than vToPointB...
		public static bool AmPointingAt( Vector3 vOrigin, Vector3 vProjection, LNX_Triangle tri )
		{
			Vector3 v_toProjection = Vector3.Normalize((vOrigin + vProjection) - vOrigin);

			Vector3[] vToVerts = new Vector3[3] 
			{
				Vector3.Normalize(tri.Verts[0].Position - vOrigin),
				Vector3.Normalize(tri.Verts[1].Position - vOrigin),
				Vector3.Normalize(tri.Verts[2].Position - vOrigin)
			};

			float alignment_projWithV0 = Vector3.Dot(v_toProjection, vToVerts[0]);
			float alignment_projWithV1 = Vector3.Dot(v_toProjection, vToVerts[1]);
			float alignment_projWithV2 = Vector3.Dot(v_toProjection, vToVerts[2]);

			float[] alignments_projectionWithVerts = new float[3]
			{
				Vector3.Dot(v_toProjection, vToVerts[0]),
				Vector3.Dot(v_toProjection, vToVerts[1]),
				Vector3.Dot(v_toProjection, vToVerts[2])
			};

			float alignment_v0toV1 = Vector3.Dot(vToVerts[0], vToVerts[1]);
			float alignment_v0toV2 = Vector3.Dot(vToVerts[0], vToVerts[2]);
			float alignment_v1toV2 = Vector3.Dot(vToVerts[1], vToVerts[2]);


			//0...
			if ( alignment_projWithV1 < alignment_v0toV1 &&
				alignment_projWithV2 < alignment_v0toV2
			)
			{
				return false;
			}

			//1...
			if ( alignment_projWithV0 < alignment_v0toV1 &&
				alignment_projWithV2 < alignment_v1toV2
			)
			{
				return false;
			}

			//2...
			if ( alignment_projWithV0 < alignment_v0toV2 &&
				alignment_projWithV1 < alignment_v1toV2
			)
			{
				return false;
			}

			return true;
		}

		public static bool AmPointingAt(Vector3 vOrigin, Vector3 vProjection, Vector3[] corners, out string dbgString)
		{
			dbgString = $"perspective : '{vOrigin}' vProj: '{vProjection}' \n";
			Vector3 v_center = GetCenterVector(corners);
			Vector3 v_originToProjection = Vector3.Normalize( (vOrigin + vProjection) - vOrigin );

			Vector3[] originToCrnrVectors = new Vector3[corners.Length];
			int mostAlignedCrnr = 0;
			float runningClosestDot = -1f;
			for ( int i = 0; i < corners.Length; i++ )
			{
				originToCrnrVectors[i] = Vector3.Normalize( corners[i] - vOrigin );

				float d = Vector3.Dot( v_originToProjection, originToCrnrVectors[i] );
				if( d > runningClosestDot )
				{
					mostAlignedCrnr = i;
				}
			}

			/*
			Run through all corners and check that v_originToProjection has a closer dot product than...
			*/
			for ( int i = 0; i < corners.Length; i++ )
			{
				if( i != mostAlignedCrnr )
				{

				}
			}


			/*

				Vector3 v_originToCenter = Vector3.Normalize(v_center - vOrigin);
			float dot_mouseProjAlignedWithCtr = Vector3.Dot(v_originToCenter, v_originToProjection);

			for ( int i = 0; i < corners.Length; i++ )
			{
				Vector3 v_crnrToOrigin = Vector3.Normalize( vOrigin - corners[i] );
				Vector3 v_originToCorner = Vector3.Normalize(corners[i] - vOrigin);

				if (dot_mouseProjAlignedWithCtr < Vector3.Dot(v_originToCrnr, v_originToCenter))
				{
					return false;
				}
			}
			*/

			return true;
		}
		
		/// <summary>
		/// Gets the verts that should be moved during a cut to form the 
		/// </summary>
		/// <param name="nm"></param>
		/// <param name="primaryEdge"></param>
		/// <param name="secondaryEdge"></param>
		/// <param name="pt"></param>
		/// <returns></returns>
		public static List<LNX_Vertex> GetMoveVerts_forInsertLoop( LNX_NavMesh nm, LNX_Edge primaryEdge, LNX_Edge secondaryEdge )
		{
			List<LNX_Vertex> returnVerts = new List<LNX_Vertex>();

			Vector3 avgdMidPt = (primaryEdge.MidPosition + secondaryEdge.MidPosition) * 0.5f;
			//Debug.DrawLine(avgdMidPt, avgdMidPt + (Vector3.up * 3f), Color.red, 3f);

			LNX_Triangle primaryTri = nm.GetTriangle( primaryEdge );
			float runningfurthestdist = 0f;
			int edgIndx = -1;
			for ( int i = 0; i < 3; i++ ) //find the edge with the furthest away mid position
			{
				float dst = Vector3.Distance( primaryEdge.MidPosition, avgdMidPt );
				if ( primaryTri.Edges[i] != primaryEdge && dst > runningfurthestdist )
				{
					runningfurthestdist = dst;
					edgIndx = i;
				}
			}

			LNX_Edge moveEdge = primaryTri.Edges[edgIndx];

			//find verts...
			returnVerts.Add( primaryTri.Verts[moveEdge.StartVertCoordinate.ComponentIndex] );
			returnVerts.Add( primaryTri.Verts[moveEdge.EndVertCoordinate.ComponentIndex] );

			if ( nm.GetVertexAtCoordinate(secondaryEdge.StartVertCoordinate).Position == moveEdge.StartPosition ||
				nm.GetVertexAtCoordinate(secondaryEdge.StartVertCoordinate).Position == moveEdge.EndPosition
			)
			{
				returnVerts.Add( nm.GetVertexAtCoordinate(secondaryEdge.StartVertCoordinate) );
			}
			else if (nm.GetVertexAtCoordinate(secondaryEdge.EndVertCoordinate).Position == moveEdge.StartPosition ||
				nm.GetVertexAtCoordinate(secondaryEdge.EndVertCoordinate).Position == moveEdge.EndPosition
			)
			{
				returnVerts.Add( nm.GetVertexAtCoordinate(secondaryEdge.EndVertCoordinate) );
			}

			return returnVerts;
		}
		#endregion

	}

	#region ENUMS-----------------------------------------
	[System.Serializable]
	public enum LNX_SelectMode
	{ 
		None = 0,
		Vertices = 1,
		Edges = 2,
		Faces = 3
	}

	[System.Serializable]
	public enum LNX_OperationMode
	{
		Pointing = 0,
		Translating = 1,
	}
	#endregion

	/*
	[System.Serializable]
	public struct LNX_Selection
	{
		public int TriIndex;
		public int EdgeIndex;
		public int VertIndex;

		private static readonly LNX_Selection noSelection = new LNX_Selection( -1, -1, -1 );

		public static LNX_Selection None
		{
			get
			{
				return noSelection;
			}
		}

		public LNX_Selection(int tIndx, int edgIndx, int vrtIndx )
        {
            TriIndex = tIndx;
			EdgeIndex = edgIndx;
			VertIndex = vrtIndx;
        }

        public LNX_Selection( int tIndx )
        {
			TriIndex = tIndx;
			EdgeIndex = -1;
			VertIndex = -1;
		}
    }*/

	[System.Serializable]
	public struct LNX_ComponentCoordinate
	{
		public int TriIndex;
		public int ComponentIndex;

		private static LNX_ComponentCoordinate none = new LNX_ComponentCoordinate( -1, -1 );

		public static LNX_ComponentCoordinate None
		{
			get
			{
				return none;
			}
		}

        public LNX_ComponentCoordinate( int triIndx, int cmptIndx )
        {
            TriIndex = triIndx;
			ComponentIndex = cmptIndx;
        }

		public static bool operator ==( LNX_ComponentCoordinate a, LNX_ComponentCoordinate b )
		{
			return a.Equals( b );
		}

		public static bool operator !=( LNX_ComponentCoordinate a, LNX_ComponentCoordinate b )
		{
			return !a.Equals( b) ;
		}

		public override bool Equals(object obj)
		{
			if ( !(obj is LNX_ComponentCoordinate) )
				return false;

			LNX_ComponentCoordinate coord = (LNX_ComponentCoordinate)obj;
			if( coord.TriIndex != TriIndex || coord.ComponentIndex != ComponentIndex )
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		public override string ToString()
		{
			return $"[{TriIndex}][{ComponentIndex}]";
		}
	}

	[System.Serializable]
	public class LNX_TriangleModification
	{
		public LNX_Triangle OriginalTriangleState;

		/// <summary>
		/// Returns originalTri.Index_parallelWithParentArray
		/// </summary>
		public int OriginalStateIndex => OriginalTriangleState.Index_parallelWithParentArray;

		public LNX_TriangleModification( LNX_Triangle originalTri )
		{
			OriginalTriangleState = new LNX_Triangle( originalTri, originalTri.Index_parallelWithParentArray );
		}
	}

	public struct LNX_ProjectionHit
	{
		public int Index_intersectedTri;
		public Vector3 HitPosition;

        private static LNX_ProjectionHit none = new LNX_ProjectionHit( -1, Vector3.zero );

        public LNX_ProjectionHit( int indx, Vector3 pos )
        {
            Index_intersectedTri = indx;
			HitPosition = pos;
        }

		public static LNX_ProjectionHit None
		{
			get
			{
				return none;
			}
		}
	}

	#region RELATIONSHIPS------------------------------------------------------------------------
	[System.Serializable]
	public struct LNX_VertexRelationship_exp
	{
		public LNX_ComponentCoordinate PerspectiveVertCoordinate;
		public LNX_ComponentCoordinate RelatedVertCoordinate;

		public bool CanSee;
		public bool AmOverlapping;

		/// <summary>The shortest possible distance to the destination vertex</summary>
		public float Distance;

		public Vector3 v_to;

		public float Angle_centerToDestinationVertex;


		public LNX_VertexRelationship_exp(LNX_Vertex myVert, LNX_Vertex relatedVert)
		{
			PerspectiveVertCoordinate = myVert.MyCoordinate;
			RelatedVertCoordinate = relatedVert.MyCoordinate;

			CanSee = true;
			AmOverlapping = false;
			Distance = Vector3.Distance(myVert.Position, relatedVert.Position);
			v_to = Vector3.Normalize(relatedVert.Position - myVert.Position);

			Angle_centerToDestinationVertex = Vector3.Angle(myVert.v_toCenter, v_to);
		}
	}

	[System.Serializable]
	public struct LNX_TriangleRelationship_exp
	{
		public int Index_relatedTriangle;

		/// <summary>An array that maps the verts of the triangle this belongs to to the verts in 
		/// the related triangle that each vert shares space with. If it doesn't share space, with any 
		/// vert belonging to the related triangle at a certain index, the value will be -1, otherwise 
		/// it will be 0-3.</summary>
		public int[] IndexMap_OwnedVerts_toShared;
		
		/// <summary>Conveniency int so you can quickly determine whether the "related" triangle 
		/// shares vertices with the "perspective" triangle. Otherwise, you'd need to compare the 
		/// IndexMap_MyVerts_toShared array to see if any entries are not -1.</summary>
		public int NumberofSharedVerts;

		public bool HasSharedEdge;

		[HideInInspector] public string DbgStruct;

        public LNX_TriangleRelationship_exp( LNX_Triangle selfTri, LNX_Triangle relatedTri )
        {
			DbgStruct = string.Empty;
			HasSharedEdge = false;
			NumberofSharedVerts = 0;
			Index_relatedTriangle = relatedTri.Index_parallelWithParentArray;
			IndexMap_OwnedVerts_toShared = new int[3] { -1, -1, -1 };

			if( selfTri == relatedTri )
			{
				DbgStruct = "self";
			}
			else
			{
				DbgStruct += $"Index_RltdTri: '{Index_relatedTriangle}'\n" +
					$"relationships:\n";

				for ( int i = 0; i < 3; i++ )
				{
					for ( int j = 0; j < 3; j++ )
					{
						if ( selfTri.Verts[i].Position == relatedTri.Verts[j].Position )
						{
							IndexMap_OwnedVerts_toShared[i] = j;
							NumberofSharedVerts++;
						}
					}

					DbgStruct += $"tri{selfTri.Index_parallelWithParentArray}v[{i}] to tri{relatedTri.Index_parallelWithParentArray}v: '{IndexMap_OwnedVerts_toShared[i]}'\n";
				}

				if ( NumberofSharedVerts > 1 )
				{
					for ( int i = 0; i < 3; i++ )
					{
						for ( int j = 0; j < 3; j++ )
						{
							if (
								(selfTri.Edges[i].StartPosition == relatedTri.Edges[j].StartPosition || selfTri.Edges[i].StartPosition == relatedTri.Edges[j].EndPosition) &&
								(selfTri.Edges[i].EndPosition == relatedTri.Edges[j].StartPosition || selfTri.Edges[i].EndPosition == relatedTri.Edges[j].EndPosition)
							)
							{
								selfTri.Edges[i].SharedEdge = new LNX_ComponentCoordinate(relatedTri.Index_parallelWithParentArray, j);
								HasSharedEdge = true;
							}
						}

					}
				}
			}
        }
    }
	#endregion
}