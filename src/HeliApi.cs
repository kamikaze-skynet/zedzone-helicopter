namespace ZedHelicopter;

// Small public entry point for other mods (Spawn Menu's helicopter kit uses it by reflection).
public static class HeliApi
{
	/// <summary>Calls a helicopter in front of the local player. Returns a status line;
	/// a line starting with '!' means it failed.</summary>
	public static string CallIn(bool ignoreLimit)
	{
		if (!HeliRunner.Active)
		{
			return "!Helicopter mod is off (disabled in its config, or Industrial Age is installed)";
		}
		GameController gc = GameController.instance;
		if (gc == null || gc.playerCharacter == null)
		{
			return "!Load into a world first";
		}
		return Heli.CallIn(gc.playerCharacter, ignoreLimit) ?? "!Already flying";
	}

	/// <summary>Gives the local player full cans of gasoline (the helicopter's fuel).</summary>
	public static string GiveFuelCans(int count)
	{
		return Heli.GiveFuelCans(count);
	}
}
