using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LogansNavigationExtension
{
    public static class LNX_UtilityObjects
    {

	}


	#region ENUMS-----------------------------------------
	[System.Serializable]
	public enum LNX_Component
	{
		None = 0,
		Vertex = 1,
		Edge = 2,
		Triangle = 3
	}

	[System.Serializable]
	public enum LNX_OperationMode
	{
		Pointing = 0,
		Translating = 1,
	}

	[System.Serializable]
	public enum LNX_Direction
	{
		PositiveY = 0,
		NegativeY = 1,
		PositiveX = 2,
		NegativeX = 3,
		PositiveZ = 4,
		NegativeZ = 5,
	}
	#endregion

	[System.Serializable]
	public struct LNX_ComponentCoordinate
	{
		public int TrianglesIndex;
		public int ComponentIndex;
		/*
		public int TriangulationAreasIndex;
		/// <summary>
		/// Keeps track of the index inside of the NavMesh.CalculateTriangulation().vertices array where this component
		/// originated. This value is only relevant if this coordinate is pointing to a vertex. If it's an edge, this 
		/// value should be -1.
		/// </summary>
		public int TriangulationVerticesIndex;
		*/
		public bool AmValid
		{
			get
			{
				return TrianglesIndex > -1 && ComponentIndex > -1;
			}
		}

		public int AsRelationalVertIndex => AmValid ? ((TrianglesIndex*3) + ComponentIndex) : -1;

		private static LNX_ComponentCoordinate none = new LNX_ComponentCoordinate()
		{
			TrianglesIndex = -1,
			ComponentIndex = -1,
		};

		public static LNX_ComponentCoordinate None
		{
			get
			{
				return none;
			}
		}

		public LNX_ComponentCoordinate(int triIndx, int cmptIndx)
		{
			TrianglesIndex = triIndx;
			ComponentIndex = cmptIndx;
		}

		public LNX_ComponentCoordinate(LNX_NavmeshHit hit)
		{
			TrianglesIndex = hit.TriangleIndex;
			ComponentIndex = -1;
			if (hit.VertIndex != -1)
			{
				ComponentIndex = hit.VertIndex;
			}
			else if (hit.EdgeIndex != -1)
			{
				ComponentIndex = hit.EdgeIndex;
			}
		}

		public static bool operator ==(LNX_ComponentCoordinate a, LNX_ComponentCoordinate b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(LNX_ComponentCoordinate a, LNX_ComponentCoordinate b)
		{
			return !a.Equals(b);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is LNX_ComponentCoordinate))
				return false;

			LNX_ComponentCoordinate coord = (LNX_ComponentCoordinate)obj;
			if (coord.TrianglesIndex != TrianglesIndex || coord.ComponentIndex != ComponentIndex)
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		public override int GetHashCode()
		{
			return HashCode.Combine( TrianglesIndex, ComponentIndex );
		}

		public override string ToString()
		{
			return this == LNX_ComponentCoordinate.none ? "None" : $"[{TrianglesIndex}][{ComponentIndex}]";
		}
	}

	/// <summary>
	/// A more basic representation of an LNX_Triangle. This is useful in situations where you need an 
	/// object that will have a much cheaper serialization cost than a full LNX_Triangle.
	/// </summary>
	[System.Serializable]
	public struct LNX_AtomicTriangle
	{
		public Vector3 VertPos0_current, VertPos0_orig;

		public Vector3 VertPos1_current, VertPos1_orig;

		public Vector3 VertPos2_current, VertPos2_orig;

		public Vector3 Center => (VertPos0_current + VertPos1_current + VertPos2_current) / 3f;

		public LNX_AtomicTriangle(LNX_Triangle tri)
		{
			VertPos0_current = tri.Verts[0].V_Position;
			VertPos0_orig = tri.Verts[0].OriginalPosition;

			VertPos1_current = tri.Verts[1].V_Position;
			VertPos1_orig = tri.Verts[1].OriginalPosition;

			VertPos2_current = tri.Verts[2].V_Position;
			VertPos2_orig = tri.Verts[2].OriginalPosition;

		}

		public LNX_AtomicTriangle(Vector3 v0pos, Vector3 v1pos, Vector3 v2pos)
		{
			VertPos0_current = v0pos;
			VertPos0_orig = v0pos;

			VertPos1_current = v1pos;
			VertPos1_orig = v1pos;

			VertPos2_current = v2pos;
			VertPos2_orig = v2pos;
		}

		/// <summary>
		/// Returns any vertices owned by this triangle that originally existed at the supplied position before 
		/// being modified.
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public int GetVertIndextAtOriginalPosition(Vector3 pos)
		{
			if (VertPos0_orig == pos)
			{
				return 0;
			}
			else if (VertPos1_orig == pos)
			{
				return 1;
			}
			else if (VertPos2_orig == pos)
			{
				return 2;
			}

			return -1;
		}

		public bool HasVertAtOriginalPosition(Vector3 pos)
		{
			if (GetVertIndextAtOriginalPosition(pos) > -1)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool OriginalPositionallyMatches(LNX_AtomicTriangle tri)
		{
			if
			(
				tri.VertPos0_orig == VertPos0_orig &&
				tri.VertPos1_orig == VertPos1_orig &&
				tri.VertPos2_orig == VertPos2_orig
			)
			{
				return true;
			}

			return false;
		}

		public bool OriginalPositionallyMatches(LNX_Triangle tri)
		{
			if
			(
				tri.Verts[0].OriginalPosition == VertPos0_orig &&
				tri.Verts[1].OriginalPosition == VertPos1_orig &&
				tri.Verts[2].OriginalPosition == VertPos2_orig
			)
			{
				return true;
			}

			return false;
		}

		public bool CurrentlyPositionallyMatches(LNX_Triangle tri)
		{
			if
			(
				tri.Verts[0].V_Position == VertPos0_current &&
				tri.Verts[1].V_Position == VertPos1_current &&
				tri.Verts[2].V_Position == VertPos2_current
			)
			{
				return true;
			}

			return false;
		}
	}

	[System.Serializable]
	public struct LNX_NavmeshHit
	{
		[SerializeField] private Vector3 hitPosition;
		/// <summary>Index of the Triangle or component that was hit, depending on the context.</summary>
		public Vector3 Position => hitPosition;
		//public Vector3 Position_flat => LNX_Utils.FlatVector( hitPosition, normal ); //taking this out now bc this is now wrong

		//private Vector3 startPosition;
		//public Vector3 StartPosition => startPosition;

		[SerializeField] private Vector3 normal;
		public Vector3 Normal => normal;


		[SerializeField] private int triangleIndex;
		public int TriangleIndex => triangleIndex;

		[SerializeField] private int edgeIndex;
		public int EdgeIndex => edgeIndex;

		[SerializeField] private int vertIndex;
		public int VertIndex => vertIndex;
		

		private static LNX_NavmeshHit none = new LNX_NavmeshHit(Vector3.zero, Vector3.zero, -1, -1, -1);

		#region CONSTRUCTORS =============================================================		
		/* //Unfortunately can't do the following bc default vectors aren't compile-time constants...
		public LNX_NavmeshHit( Vector3 pos, int triIndx, int cmpntIndx = -1, Vector3 strtPos = Vector3.zero, Vector3 nrml = Vector3.zero )
		{
			hitPosition = pos;
			startPosition = strtPos;
			normal = nrml;

			Coordinate = new LNX_ComponentCoordinate( triIndx, cmpntIndx );
		}
		*/

		public LNX_NavmeshHit(LNX_Triangle hitTriangle, Vector3 hitpos ) //todo: takw away startpos and maybe even get rid of this overload
		{
			hitPosition = hitpos;
			normal = hitTriangle.V_PathingNormal;

			triangleIndex = hitTriangle.Index_inCollection;
			edgeIndex = -1;
			vertIndex = -1;
		}

		public LNX_NavmeshHit ( LNX_Vertex vert )
		{
			hitPosition = vert.V_Position;
			normal = vert.CalculatePathingNormal();
			triangleIndex = vert.MyCoordinate.TrianglesIndex;
			edgeIndex = -1;
			vertIndex = vert.ComponentIndex;
		}

		public LNX_NavmeshHit(LNX_Vertex vert, Vector3 nrml )
		{
			hitPosition = vert.V_Position;
			normal = nrml;
			triangleIndex = vert.MyCoordinate.TrianglesIndex;
			edgeIndex = -1;
			vertIndex = vert.ComponentIndex;
		}

		public LNX_NavmeshHit(LNX_Edge edge, Vector3 pos, Vector3 nrml)
		{
			hitPosition = pos;
			normal = nrml;
			triangleIndex = edge.MyCoordinate.TrianglesIndex;
			edgeIndex = edge.MyCoordinate.ComponentIndex;
			vertIndex = -1;
		}

		public LNX_NavmeshHit(Vector3 pos, Vector3 nrml, int triIndx, int vertIndx, int edgeIndx)
		{
			hitPosition = pos;
			normal = nrml;

			triangleIndex = triIndx;
			edgeIndex = edgeIndx;
			vertIndex = vertIndx;
		}

		#endregion

		public LNX_ComponentCoordinate AsVertCoordinate()
		{
			return new LNX_ComponentCoordinate(triangleIndex, vertIndex);
		}

		public static LNX_NavmeshHit None
		{
			get
			{
				return none;
			}
		}

		#region OPERATORS ===============================================
		public override bool Equals(object obj)
		{
			if (!(obj is LNX_NavmeshHit))
				return false;

			LNX_NavmeshHit hit = (LNX_NavmeshHit)obj;
			if ( hit.triangleIndex != triangleIndex || hit.hitPosition != hitPosition ||
				hit.vertIndex != vertIndex || hit.edgeIndex != edgeIndex || hit.normal != normal
			)
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		public override int GetHashCode()
		{
			return HashCode.Combine( hitPosition, normal, triangleIndex, vertIndex, edgeIndex );
		}

		public static bool operator ==(LNX_NavmeshHit a, LNX_NavmeshHit b)
		{
			return a.Equals(b);
		}
		public static bool operator !=(LNX_NavmeshHit a, LNX_NavmeshHit b)
		{
			return !a.Equals(b);
		}
		#endregion

		public override string ToString()
		{
			return $"[t{triangleIndex}v{vertIndex}e{edgeIndex}_pos{Position}]";
		}

		//public static explicit operator LNX_ComponentCoordinate(LNX_NavmeshHit hit) =>
			//new LNX_ComponentCoordinate( hit.triangleIndex, hit.vertIndex != -1 ? hit.vertIndex : hit.edgeIndex);
	}

	[System.Serializable]
	public struct LNX_Quad
	{
		public Vector3 crnrA;
		public Vector3 crnrB;
		public Vector3 crnrC;
		public Vector3 crnrD;

		private static LNX_Quad none = new LNX_Quad()
		{
			crnrA = Vector3.zero,
			crnrB = Vector3.zero,
			crnrC = Vector3.zero,
			crnrD = Vector3.zero
		};

		public static LNX_Quad None
		{
			get
			{
				return none;
			}
		}

		public LNX_Quad( Vector3 cA, Vector3 cB, Vector3 cC, Vector3 cD )
		{
			crnrA = cA;
			crnrB = cB;
			crnrC = cC;
			crnrD = cD;
		}

		public override string ToString()
		{
			return $"{crnrA}, {crnrB}, {crnrC}, {crnrD}";
		}
	}
}
