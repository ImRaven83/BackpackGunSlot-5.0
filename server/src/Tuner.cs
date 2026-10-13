using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;

namespace BackpackGunSlot;

[Injectable(InjectionType.Transient, int.MaxValue, TypePriority = 1000001)]
public class Tuner(TemplateTable templates, ItemHelper itemHelper, JsonUtil jsonUtil, ModHelper modHelper, ISptLogger<Tuner> logger) : IOnLoad
{
	private const string BackpackBaseClass = "5448e53e4bdc2d60728b4567";

	private static readonly Dictionary<string, string> Categories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["Pistol"] = "5447b5cf4bdc2d65278b4567",
		["SMG"] = "5447b5e04bdc2d62278b4567",
		["AssaultRifle"] = "5447b5f14bdc2d61278b4567",
		["AssaultCarbine"] = "5447b5fc4bdc2d87278b4567",
		["Shotgun"] = "5447b6094bdc2dc3278b4567",
		["MarksmanRifle"] = "5447b6194bdc2d67278b4567",
		["SniperRifle"] = "5447b6254bdc2dc3278b4568",
		["MachineGun"] = "5447bed64bdc2d97278b4568",
		["GrenadeLauncher"] = "5447bedf4bdc2d87278b4568",
		["SpecialWeapon"] = "5447bee84bdc2dc3278b4569"
	};

	private readonly ConfigModel _config = jsonUtil.DeserializeFromFile<ConfigModel>(System.IO.Path.Join(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), "config", "config.json")) ?? new ConfigModel();

	public Task OnLoadAsync(CancellationToken cancellationToken)
	{
		if (!_config.Enabled)
		{
			logger.Info("[BackpackGunSlot] disabled by config");
			return Task.CompletedTask;
		}
		if (string.IsNullOrWhiteSpace(_config.SlotName))
		{
			logger.Error("[BackpackGunSlot] slotName cannot be empty; no templates changed");
			return Task.CompletedTask;
		}
		_config.SlotName = _config.SlotName.Trim();
		List<TierModel> tiers = (from t in _config.Tiers ?? new List<TierModel>()
			where t != null && t.MaxCells > 0
			orderby t.MaxCells
			select t).ToList();
		int patched = 0;
		int skipped = 0;
		foreach (var (tpl, template) in templates.Items)
		{
			if (!itemHelper.IsOfBaseclass(tpl, BackpackBaseClass))
			{
				continue;
			}
			TemplateItemProperties properties = template.Properties;
			if (properties == null)
			{
				continue;
			}
			List<Slot> slots = properties.Slots?.ToList() ?? new List<Slot>();
			if (!slots.Any(s => s.Name == _config.SlotName))
			{
				Dictionary<string, string> categories = ResolveCategories(tpl, CountCells(properties), tiers, out string tierLabel);
				if (categories.Count == 0)
				{
					skipped++;
					continue;
				}
				slots.Add(BuildWeaponSlot(tpl, categories));
				properties.Slots = slots;
				patched++;
				logger.Info($"[BackpackGunSlot] {itemHelper.GetItemName(tpl)} ({tpl}) -> tier '{tierLabel}': {string.Join("/", categories.Keys)}");
			}
		}
		logger.Success($"[BackpackGunSlot] patched {patched} backpack(s), skipped {skipped}");
		return Task.CompletedTask;
	}

	private static int CountCells(TemplateItemProperties props)
	{
		int cells = 0;
		foreach (Grid grid in props.Grids ?? Array.Empty<Grid>())
		{
			GridProperties properties = grid.Properties;
			cells += (properties?.CellsH ?? 0) * (properties?.CellsV ?? 0);
		}
		return cells;
	}

	private Dictionary<string, string> ResolveCategories(MongoId tpl, int cells, List<TierModel> tiers, out string tierLabel)
	{
		Dictionary<string, string> overrides = _config.Overrides;
		if (overrides != null && overrides.TryGetValue(tpl.ToString(), out string value))
		{
			value = value?.Trim() ?? "none";
			tierLabel = "override:" + value;
			if (value.Equals("none", StringComparison.OrdinalIgnoreCase))
			{
				return new Dictionary<string, string>();
			}
			return MapCategories(value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
		}
		if (_config.AllowAnyGun)
		{
			tierLabel = "allowAnyGun";
			return MapCategories(Categories.Keys);
		}
		TierModel tierModel = tiers.FirstOrDefault(t => cells <= t.MaxCells) ?? tiers.LastOrDefault();
		tierLabel = tierModel == null ? "none" : $"<={tierModel.MaxCells}c";
		if (tierModel != null)
		{
			return MapCategories(tierModel.Categories ?? new List<string>());
		}
		return new Dictionary<string, string>();
	}

	private Dictionary<string, string> MapCategories(IEnumerable<string> names)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (string name in names)
		{
			string text = name?.Trim();
			if (!string.IsNullOrEmpty(text) && Categories.TryGetValue(text, out string value))
			{
				dictionary[text] = value;
			}
			else
			{
				logger.Warning("[BackpackGunSlot] unknown weapon category '" + name + "' in config, ignored");
			}
		}
		return dictionary;
	}

	private Slot BuildWeaponSlot(MongoId backpackTpl, Dictionary<string, string> categories)
	{
		return new Slot
		{
			Name = _config.SlotName,
			Id = new MongoId(),
			Parent = backpackTpl,
			Required = false,
			MergeSlotWithChildren = false,
			Properties = new SlotProperties
			{
				Filters = new List<SlotFilter>
				{
					new SlotFilter
					{
						Filter = new HashSet<MongoId>(categories.Values.Select(v => new MongoId(v)))
					}
				}
			}
		};
	}
}
