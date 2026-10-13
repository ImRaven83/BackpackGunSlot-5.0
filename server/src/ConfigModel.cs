using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BackpackGunSlot;

public record ConfigModel
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("slotName")]
	public string SlotName { get; set; } = "mod_backpack_weapon";

	[JsonPropertyName("allowAnyGun")]
	public bool AllowAnyGun { get; set; }

	[JsonPropertyName("tiers")]
	public List<TierModel> Tiers { get; set; } = new List<TierModel>
	{
		new TierModel
		{
			MaxCells = 12,
			Categories = new List<string> { "Pistol" }
		},
		new TierModel
		{
			MaxCells = 18,
			Categories = new List<string> { "Pistol", "SMG" }
		},
		new TierModel
		{
			MaxCells = 24,
			Categories = new List<string> { "Pistol", "SMG", "Shotgun" }
		},
		new TierModel
		{
			MaxCells = 35,
			Categories = new List<string> { "Pistol", "SMG", "Shotgun", "AssaultRifle", "AssaultCarbine", "MarksmanRifle" }
		},
		new TierModel
		{
			MaxCells = 9999,
			Categories = new List<string> { "Pistol", "SMG", "Shotgun", "AssaultRifle", "AssaultCarbine", "MarksmanRifle", "SniperRifle" }
		}
	};

	[JsonPropertyName("overrides")]
	public Dictionary<string, string> Overrides { get; set; } = new Dictionary<string, string>();
}
