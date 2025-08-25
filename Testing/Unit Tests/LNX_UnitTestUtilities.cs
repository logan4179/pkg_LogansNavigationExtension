using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LogansNavigationExtension
{
    public static class LNX_UnitTestUtilities
    {
		public static string Name_GeneratedNavmeshGameobject = "SceneGeneratedNavmesh";
		public static string Name_SerializedNavmeshGameobject = "SerializedNavmesh";
		public static string Name_ExistingSceneNavmeshGameobject = "[LNX_Navmesh]";

		[Header("Log formatting")]
		public static string UnitTestMethodBeginString = "//----[START OF TEST '{0}']------------------//////////////////////////////////////////";
		public static string UnitTestMethodEndString = "//----[END OF TEST '{0}']------------------//////////////////////////////////////////";
		public static string UnitTestSectionEndString = "//----[END OF section '{0}']-----------------------\n";

		public static void LogTestStart( string testName, string testDescription )
		{
			Debug.Log( 
				"/////////////////////////////////////////////////////////////////////////////\n" + 
				testName + "\n" + 
				testDescription +
				"/////////////////////////////////////////////////////////////////////////////\n\n"
			);
		}

		public static string FormattedVectorString( Vector3 vect )
        {
            return $"new Vector3({vect.x}f, {vect.y}f, {vect.z}f)";
        }

		public static string FormattedVectorString_textParse(Vector3 vect)
		{
			return $"{vect.x},{vect.y},{vect.z}";
		}

        public static Vector3[] ParseVectorArray_fromFormattedVectorString( string str )
        {
            string[] lines = str.Split($",\n");

			Vector3[] returnArray = new Vector3[lines.Length];


            return returnArray;
		}

		public static string LongVectorString( Vector3 vect )
		{
			return $"{vect.x}, {vect.y}, {vect.z}";
		}

	}

}
