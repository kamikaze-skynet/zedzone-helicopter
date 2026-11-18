using System;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Logging;

namespace ZedHelicopter;

internal static class Report
{
	private static readonly StringBuilder _notes = new StringBuilder();

	private static string FilePath => Path.Combine(Paths.BepInExRootPath, "Helicopter-report.txt");

	public static void Note(string s)
	{
		try
		{
			ManualLogSource l = HeliPlugin.L;
			if (l != null)
			{
				l.LogInfo((object)((s.Length > 300) ? s.Substring(0, 300) : s));
			}
		}
		catch
		{
		}
		if (HeliPlugin.Diagnostics == null || !HeliPlugin.Diagnostics.Value)
		{
			return;
		}
		lock (_notes)
		{
			_notes.AppendLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + s);
			try
			{
				File.WriteAllText(FilePath, "# Helicopter report\n\n" + _notes, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
			}
			catch
			{
			}
		}
	}
}
