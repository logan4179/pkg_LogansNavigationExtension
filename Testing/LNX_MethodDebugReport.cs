using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace LogansNavigationExtension
{
    [System.Serializable]
    public class LNX_MethodDebugReport //must be class instead of struct, bc passing by reference
    {
        [SerializeField, TextArea(1,30)] private string rprtString;
        public string ReportString => rprtString;

        [SerializeField, HideInInspector] private int methodLvl = 0;
        public int MethodLvl => methodLvl;

        [SerializeField, HideInInspector] private string tabTxt = "";
        //private string tabTxt_inMethod => tabTxt + "\t";

        private string currentMethodName = string.Empty;
        public string CurrentMethodName => currentMethodName;

		public void StartReport( string rprtName )
        {
            methodLvl = -1;
            tabTxt = string.Empty;

            //rprtString = $"{rprtName}\n" +
            //$"{{\n";
            rprtString = string.Empty;
        }

        public void StartMethod( string methodName, bool addNewLineSpace = false )
        {
            currentMethodName = methodName;

            methodLvl++;

            if (addNewLineSpace)
            {
                rprtString += "\n";
            }

            rprtString += $"{tabTxt}{methodName}\n" +
                $"{tabTxt}{{\n";

            tabTxt += "\t";
        }

        public void Log( string s )
        {
            rprtString += $"{tabTxt}{s}\n";
        }

		public void EmptyLine()
        {
            rprtString += "\n";

		}

        public void Log_InnrTabbed( string s, int extraTabs )
        {
            string tempTabTxt = tabTxt;
            for ( int i = 0; i < extraTabs; i++ )
            {
                tempTabTxt += "\t";
            }

			rprtString += $"{tempTabTxt}{s}\n";
		}

		public void Log_Untabbed(string s)
		{
			rprtString += $"{s}\n";
		}

		public void EndMethod( string methodName = "", bool addNewLineSpace = false)
        {
            tabTxt = "";

            if( methodLvl > 0 )
            {
                for (int i = 0; i < methodLvl; i++)
                {
                    tabTxt += "\t";
                }
            }

            rprtString += $"{tabTxt}}}{(methodName == string.Empty ? "" : $" <-end of {methodName}")}\n";
			//rprtString += "\n"; //in order to make a space to separate methods

			if (addNewLineSpace)
			{
				rprtString += "\n";
			}

			methodLvl--;
        }

		public void Log_And_End_Method(string s, bool addNewLineSpace = false)
		{
			Log(s);
			EndMethod( currentMethodName, addNewLineSpace );
		}

		public void EndReport()
		{
			//rprtString += $"}}";
		}
	}
}
