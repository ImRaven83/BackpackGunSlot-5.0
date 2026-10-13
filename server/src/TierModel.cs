using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BackpackGunSlot;

public record TierModel
{
	[JsonPropertyName("maxCells")]
	public int MaxCells { get; set; }

	[JsonPropertyName("categories")]
	public List<string> Categories { get; set; } = new List<string>();
}
