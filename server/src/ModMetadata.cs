using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SemanticVersioning;

namespace BackpackGunSlot;

public record ModMetadata : IModMetadata
{
	public string ModGuid { get; init; } = "com.20fpsguy.backpackgunslot";

	public string Name { get; init; } = "BackpackGunSlot";

	public string Author { get; init; } = "20fpsguy";

	public List<string>? Contributors { get; init; }

	public Version Version { get; init; } = new Version(typeof(ModMetadata).Assembly.GetName().Version.ToString(3));

	public Range SptVersion { get; init; } = new Range("~5.0.0");

	public List<string>? Incompatibilities { get; init; }

	public Dictionary<string, Range>? ModDependencies { get; init; }

	public string? Url { get; init; } = "";

	public string License { get; init; } = "MIT";

	public bool HasPrepatcher { get; init; }
}
