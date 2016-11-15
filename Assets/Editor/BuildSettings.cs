﻿using UnityEngine;
using UnityEditor;
using System.Diagnostics;

public class ScriptBatch : MonoBehaviour 
{
	[MenuItem("Build/Windows with resources")]
	public static void BuildSettings ()
	{
		// Get filename
		string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");

		string[] levels = new string[] {"Assets/MapEditor.unity"};

		BuildPipeline.BuildPlayer(levels, path + "/FAForeverMapEditor.exe", BuildTarget.StandaloneWindows, BuildOptions.None);

		// Copy structure files
		FileUtil.CopyFileOrDirectory("Structure", path + "/FAForeverMapEditor_Data/Structure");

		// Run the game (Process class from System.Diagnostics).
		//Process proc = new Process();
		//proc.StartInfo.FileName = path + "/FAForeverMapEditor.exe";
		//proc.Start();
	}

	[MenuItem("Build/Windows with resources (64bit)")]
	public static void BuildSettings64 ()
	{
		// Get filename
		string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");

		string[] levels = new string[] {"Assets/MapEditor.unity"};

		BuildPipeline.BuildPlayer(levels, path + "/FAForeverMapEditor.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);

		// Copy structure files
		FileUtil.CopyFileOrDirectory("Structure", path + "/FAForeverMapEditor_Data/Structure");

		// Run the game (Process class from System.Diagnostics).
		//Process proc = new Process();
		//proc.StartInfo.FileName = path + "/FAForeverMapEditor.exe";
		//proc.Start();
	}
}