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

        private int methodLvl = 0;
        public int MethodLvl => methodLvl;

        private string tabTxt = "";
        //private string tabTxt_inMethod => tabTxt + "\t";

        private string currentMethodName = string.Empty;
        public string CurrentMethodName => currentMethodName;

        private bool flag_limitReached = false;

        public int MethodLevelVerbosityLimit = -1;

        public void Clear()
        {
			methodLvl = -1;
			tabTxt = string.Empty;
			rprtString = string.Empty;
		}

		public void StartReport( string rprtName = "", int mthdLvlLmt = -1 )
        {
            methodLvl = -1;
            tabTxt = string.Empty;
            MethodLevelVerbosityLimit = mthdLvlLmt;

            //rprtString = $"{rprtName}\n" +
            //$"{{\n";
            rprtString = string.Empty;
            flag_limitReached = false;
        }

        public void StartMethod( string methodName, bool addNewLineSpace = false )
        {
            currentMethodName = methodName;

            methodLvl++;

            if (addNewLineSpace){ rprtString += "\n"; }

            rprtString += $"{tabTxt}{methodName}\n" +
                $"{tabTxt}{{\n";

            tabTxt += "\t";

            if( MethodLevelVerbosityLimit > -1 && methodLvl > MethodLevelVerbosityLimit )
            {
                rprtString += $"{tabTxt}...\n";
            }
        }

        public void Log( string s )
        {
			if (MethodLevelVerbosityLimit > -1 && methodLvl > MethodLevelVerbosityLimit)
			{
				return;
			}

			if ( rprtString.Length < 50000)
            {
                rprtString += $"{tabTxt}{s}\n";
            }
            else if( !flag_limitReached )
            {
                Debug.LogWarning($"limit reached on report");
                rprtString += "LIMIT REACHED ON REPORT\n";
                flag_limitReached = true;
            }
        }

        public void Log(params string[] logs)
        {
			if (MethodLevelVerbosityLimit > -1 && methodLvl > MethodLevelVerbosityLimit)
			{
				return;
			}

			for (int i = 0; i < logs.Length; i++)
            {
                Log(logs[i]);
            }
        }

		public void EmptyLine()
        {
            rprtString += "\n";

		}

        public void Log_InnrTabbed( string s, int extraTabs )
        {
			if (MethodLevelVerbosityLimit > -1 && methodLvl > MethodLevelVerbosityLimit)
			{
				return;
			}
			/*
            string tempTabTxt = tabTxt;
            for ( int i = 0; i < extraTabs; i++ )
            {
                tempTabTxt += "\t";
            }

            rprtString += $"{tempTabTxt}{s}\n";

            */

			//trying this out instead. Make sure it works...
			string tempTabTxt = "";
			for (int i = 0; i < extraTabs; i++)
			{
				tempTabTxt += "\t";
			}
			Log( $"{tempTabTxt}{s}" );

		}

		public void Log_Untabbed(string s)
		{
			if (MethodLevelVerbosityLimit > -1 && methodLvl > MethodLevelVerbosityLimit)
			{
				return;
			}

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

		public void Log_And_End_Method(string s, string methodName, bool addNewLineSpace = false)
		{
			Log(s);
			EndMethod(methodName, addNewLineSpace);
		}

		public void EndReport()
		{
			//rprtString += $"}}";
		}
	}
}
