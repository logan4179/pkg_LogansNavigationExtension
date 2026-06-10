using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace LogansNavigationExtension
{
	[System.Serializable]
	public class LNX_MethodDebugReport //must be class instead of struct, bc passing by reference
	{
		[SerializeField, TextArea(1, 30)] private string rprtString;
		public string ReportString => rprtString;

		private List<LNXMDR_MethodSignature> methodSignatures;

		public int MethodLvl => methodSignatures.Count;

		public LNXMDR_MethodSignature CurrentMethodSignature => methodSignatures[methodSignatures.Count - 1];
		public string CurrentMethodName => CurrentMethodSignature.MethodName;
		public string CurrentMethodTabText => methodSignatures.Count > 0 ? CurrentMethodSignature.MethodTabTxt : "";
		public string CurrentInnerTabText => methodSignatures.Count > 0 ? CurrentMethodSignature.InnerTabTxt : "";

		private bool flag_limitReached = false;

		public int MethodLevelVerbosityLimit = -1;

		private bool flag_amInAbbreviateMethod = false;

		public void Clear()
		{
			rprtString = string.Empty;
			methodSignatures = new List<LNXMDR_MethodSignature>();
			flag_limitReached = false;
			flag_amInAbbreviateMethod = false;
		}

		public void StartReport(string rprtName = "")
		{
			//rprtString = $"{rprtName}\n" +
			//$"{{\n";
			rprtString = string.Empty;
			flag_limitReached = false;
			flag_amInAbbreviateMethod = false;

			methodSignatures = new List<LNXMDR_MethodSignature>();
		}

		public void StartReport(string rprtName, int mthdLvlLmt)
		{
			MethodLevelVerbosityLimit = mthdLvlLmt;

			//rprtString = $"{rprtName}\n" +
			//$"{{\n";
			rprtString = string.Empty;
			flag_limitReached = false;
			methodSignatures = new List<LNXMDR_MethodSignature>();
		}

		public void StartMethod(string methodName, string tabStarter = "|")
		{
			if
			(
				flag_limitReached || flag_amInAbbreviateMethod ||
				(MethodLevelVerbosityLimit > -1 && MethodLvl > MethodLevelVerbosityLimit)
			)
			{
				return;
			}

			methodSignatures.Add( new LNXMDR_MethodSignature(methodName, tabStarter, CurrentInnerTabText) );

			rprtString += $"{CurrentMethodTabText}{methodName}\n" +
				$"{CurrentMethodTabText}{{\n";
		}

		public void StartAbbreviatedMethod(string methodName)
		{
			if
			(
				flag_limitReached || flag_amInAbbreviateMethod ||
				(MethodLevelVerbosityLimit > -1 && MethodLvl > MethodLevelVerbosityLimit)
			)
			{
				return;
			}

			StartMethod(methodName, "");

			rprtString += $"{CurrentInnerTabText}...[abbreviated]...\n";

			flag_amInAbbreviateMethod = true; //this needs to be last so that the other stuff can work...

		}

		public void EndAbbreviatedMethod(string methodName)
		{
			if (flag_limitReached)
			{
				return;
			}

			if( !flag_amInAbbreviateMethod )
			{
				Debug.LogWarning($"EndAbbreviatedMethod() was called, but wasn't in abbreviated method");
			}
			flag_amInAbbreviateMethod = false;

			EndMethod(methodName);
		}

		public void EndMethod(string methodName = "")
		{
			if (flag_amInAbbreviateMethod || flag_limitReached )
			{
				return;
			}

			rprtString += $"{CurrentMethodTabText}}}{(methodName == string.Empty ? "" : $" <-end of {methodName}")}\n";

			methodSignatures.RemoveAt(methodSignatures.Count - 1);
		}

		public void Log(string s, bool includeConsole = false, bool abbreviationOverride = false)
		{
			if
			(
				flag_limitReached ||
				(MethodLevelVerbosityLimit > -1 && MethodLvl > MethodLevelVerbosityLimit) ||
				(flag_amInAbbreviateMethod && abbreviationOverride == false)
			)
			{
				return;
			}

			if (rprtString.Length < 50000)
			{
				rprtString += $"{CurrentInnerTabText}{s}\n";
			}
			else if (!flag_limitReached)
			{
				Debug.LogWarning($"limit reached on report");
				rprtString += "LIMIT REACHED ON REPORT\n";
				flag_limitReached = true;
			}

			if (includeConsole)
			{
				Debug.Log(s);
			}
		}

		public void Log(params string[] logs)
		{
			if
			(
				flag_limitReached || flag_amInAbbreviateMethod ||
				(MethodLevelVerbosityLimit > -1 && MethodLvl > MethodLevelVerbosityLimit)
			)
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
			if
			(
				flag_limitReached || flag_amInAbbreviateMethod ||
				(MethodLevelVerbosityLimit > -1 && MethodLvl > MethodLevelVerbosityLimit)
			)
			{
				return;
			}

			rprtString += "\n";

		}

		public void Log_InnrTabbed(string s, int extraTabs)
		{
			if
			(
				flag_limitReached || flag_amInAbbreviateMethod ||
				(MethodLevelVerbosityLimit > -1 && MethodLvl > MethodLevelVerbosityLimit)
			)
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
			Log($"{tempTabTxt}{s}");

		}

		public void Log_Untabbed(string s)
		{
			if
			(
				flag_limitReached || flag_amInAbbreviateMethod ||
				(MethodLevelVerbosityLimit > -1 && MethodLvl > MethodLevelVerbosityLimit)
			)
			{
				return;
			}

			rprtString += $"{s}\n";
		}

		public void Log_And_End_Method(string s)
		{
			Log(s);
			EndMethod("");
		}

		public void Log_And_End_Method(string s, string methodName)
		{
			Log(s);
			EndMethod(methodName);
		}

		public void EndReport()
		{
			//rprtString += $"}}";
		}
	}

	public struct LNXMDR_MethodSignature
	{
		public string MethodName;
		public string tabStarter;
		public string MethodTabTxt;
		public string InnerTabTxt;

		public LNXMDR_MethodSignature(string mthdName, string tbStrtr, string crntTbTxt)
		{
			MethodName = mthdName;
			tabStarter = tbStrtr;
			MethodTabTxt = crntTbTxt;
			InnerTabTxt = MethodTabTxt + tbStrtr + "\t";

		}
	}
}
