using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.CatDiscordBotDataExport;

internal sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	
	internal readonly ICatApi Api = new ApiImplementation();
	internal readonly IMoreDifficultiesApi? MoreDifficultiesApi;

	private readonly Queue<Action<G>> QueuedTasks = new();
	internal readonly CardRenderer CardRenderer = new();
	internal readonly TooltipRenderer TooltipRenderer = new();

	public override object? GetApi(IModManifest requestingMod)
	{
		return Api;
	}

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;

		var harmony = new Harmony(package.Manifest.UniqueName);
		EditorPatches.Apply(harmony);
		GPatches.Apply(harmony);

		MoreDifficultiesApi = helper.ModRegistry.GetApi<IMoreDifficultiesApi>("TheJazMaster.MoreDifficulties");

		Api.RegisterTooltips(Deck.dizzy, new()
		{
			(new AStatus { targetPlayer = true, status = Status.maxShield, statusAmount = 1 }).GetTooltips(DB.fakeState).First(),
			(new AStatus { targetPlayer = true, status = Status.stunCharge, statusAmount = 1 }).GetTooltips(DB.fakeState).First(),
			(new AStatus { targetPlayer = true, status = Status.stunSource, statusAmount = 1 }).GetTooltips(DB.fakeState).First(),
			(new AStatus { targetPlayer = true, status = Status.corrode, statusAmount = 1 }).GetTooltips(DB.fakeState).First(),
			(new AStatus { targetPlayer = true, status = Status.mitosis, statusAmount = 1 }).GetTooltips(DB.fakeState).First(),
			(new AStatus { targetPlayer = true, status = Status.payback, statusAmount = 1 }).GetTooltips(DB.fakeState).First(),
			(new AStunShip()).GetTooltips(DB.fakeState).First(),
			(new AEndTurn()).GetTooltips(DB.fakeState).First(),
		});
	}

	internal void QueueTask(Action<G> task)
		=> QueuedTasks.Enqueue(task);

	internal void RunNextTask(G g)
	{
		if (!QueuedTasks.TryDequeue(out var task))
			return;
		task(g);

		if (QueuedTasks.Count == 0)
			Logger!.LogInformation("Finished all tasks.");
		else if (QueuedTasks.Count % 25 == 0)
			Logger!.LogInformation("Tasks left in the queue: {TaskCount}", QueuedTasks.Count);
	}

	internal void AllCardExportTask(G g, bool withScreenFilter, bool individualImages = true)
	{
		var modloaderFolder = AppDomain.CurrentDomain.BaseDirectory;

		static string GetUpgradePathAffix(Upgrade upgrade)
			=> upgrade switch
			{
				Upgrade.A => "A",
				Upgrade.B => "B",
				_ => ""
			};

		List<Upgrade> noUpgrades = [Upgrade.None];

		var groupedCards = DB.cards
			.Select(kvp => (Key: kvp.Key, Type: kvp.Value, Meta: DB.cardMetas.GetValueOrDefault(kvp.Key)))
			.Where(e => e.Meta is not null)
			.Select(e => (Key: e.Key, Type: e.Type, Meta: e.Meta!))
			.Where(e => DB.currentLocale.strings.ContainsKey($"card.{e.Key}.name"))
			.GroupBy(e => e.Meta.deck)
			.Select(g => (Deck: g.Key, HasUnreleased: g.Any(e => e.Meta.unreleased), Entries: g))
			.ToList();

		var exportableData = groupedCards
			.Select(group => new ExportDeckData(
				group.Deck.Key(),
				Loc.T($"char.{group.Deck.Key()}"),
				group.Entries
					.Select(e => new ExportCardData(
						e.Key,
						Loc.T($"card.{e.Key}.name"),
						e.Meta.unreleased,
						e.Meta.rarity,
						noUpgrades.Concat(e.Meta.upgradesTo).ToHashSet(),
						(Activator.CreateInstance(e.Type) as Card)?.GetData(g.state).description
					)).ToList()
			)).ToList();

		var exportableDataPath = Path.Combine(modloaderFolder, "CatDiscordBotDataExport", "cards");
		Directory.CreateDirectory(exportableDataPath);
		File.WriteAllText(Path.Combine(exportableDataPath, "data.json"), JsonConvert.SerializeObject(exportableData, new JsonSerializerSettings
		{
			Formatting = Formatting.Indented
		}));

		foreach (var group in groupedCards)
		{
			var fileSafeDeckKey = group.Deck.Key();
			foreach (var unsafeChar in Path.GetInvalidFileNameChars())
				fileSafeDeckKey = fileSafeDeckKey.Replace(unsafeChar, '_');

			var deckExportPath = Path.Combine(modloaderFolder, "CatDiscordBotDataExport", "cards", fileSafeDeckKey);
			var unreleasedCardsExportPath = Path.Combine(deckExportPath, "unreleased");

			Directory.CreateDirectory(deckExportPath);
			if (group.HasUnreleased)
				Directory.CreateDirectory(unreleasedCardsExportPath);

			if (individualImages)
			{
				foreach (var entry in group.Entries)
				{
					var fileSafeCardKey = entry.Key;
					foreach (var unsafeChar in Path.GetInvalidFileNameChars())
						fileSafeCardKey = fileSafeCardKey.Replace(unsafeChar, '_');

					List<Upgrade> upgrades = [Upgrade.None];
					upgrades.AddRange(entry.Meta.upgradesTo);

					foreach (var upgrade in upgrades)
					{
						var exportPath = Path.Combine(entry.Meta.unreleased ? unreleasedCardsExportPath : deckExportPath, $"{fileSafeCardKey}{GetUpgradePathAffix(upgrade)}.png");
						var card = (Card)Activator.CreateInstance(entry.Type)!;
						card.upgrade = upgrade;
						QueueTask(g => CardExportTask(g, withScreenFilter, card, exportPath));
					}
				}
			} 
			else
			{
				var backgrounds = new Dictionary<Type, Color>();

				HashSet<Type> starterCards = new();
				if (StarterDeck.starterSets.TryGetValue(group.Deck, out StarterDeck? value))
				{
					starterCards = new(value.cards.Select(c => c.GetType()));
					foreach (var card in value.cards) backgrounds[card.GetType()] = Colors.buttonEmphasis;
				}

				StarterDeck? altStarterDeck = MoreDifficultiesApi?.GetAltStarters(group.Deck);
				HashSet<Type> altStarters = new();
				if (altStarterDeck != null)
				{
					altStarters = new(altStarterDeck.cards.Select(c => c.GetType()));
					foreach (var card in altStarterDeck.cards) backgrounds[card.GetType()] = new Color("9c33ff");
				}

				string GetSortOrder(Card card)
				{
					if (starterCards.Contains(card.GetType())) return "0";
					if (altStarters.Contains(card.GetType())) return "1";
					return "2";
				}

				var rows = group
					.Entries
					.Where(e => !e.Meta.unreleased)
					.GroupBy(e => e.Meta.dontOffer ? 99 : (int)e.Meta.rarity)
					.OrderBy(group => group.First().Meta.dontOffer ? 99 : (int)group.First().Meta.rarity)
					.Select(group => group
						.Select(entry => (Card)Activator.CreateInstance(entry.Type)!)
						.OrderBy(card =>
							GetSortOrder(card) + 
							card.GetFullDisplayName()
						)
						.ToList()
					)
					.ToList();


				if (rows.Count <= 0) continue;

				QueueTask(g => CardCollectionExportTask(
					g, 
					withScreenFilter, 
					rows, 
					Path.Combine(deckExportPath, "cardPoster.png"), 
					backgrounds
				));
			}
		}
	}


	internal void AllTooltipsExportTask(G g, bool withScreenFilter)
	{
		var modloaderFolder = AppDomain.CurrentDomain.BaseDirectory;

		var groupedTooltips = CardRenderer.CardPosterTooltips
			.Select(kvp => (Deck: kvp.Key, Entries: kvp.Value))
			.ToList();

		var exportableDataPath = Path.Combine(modloaderFolder, "CatDiscordBotDataExport", "tooltips");
		Directory.CreateDirectory(exportableDataPath);

		foreach (var group in groupedTooltips)
		{
			var fileSafeDeckKey = group.Deck.Key();
			foreach (var unsafeChar in Path.GetInvalidFileNameChars())
				fileSafeDeckKey = fileSafeDeckKey.Replace(unsafeChar, '_');

			var tooltipsExportPath = Path.Combine(modloaderFolder, "CatDiscordBotDataExport", "tooltips", fileSafeDeckKey);
			Directory.CreateDirectory(tooltipsExportPath);

			for (int i = 0; i < group.Entries.Count; i++)
			{
				var entry = group.Entries[i];

				var fileSafeCardKey = i;

				var exportPath = Path.Combine(tooltipsExportPath, $"{fileSafeCardKey}.png");
				QueueTask(g => TooltipExportTask(g, withScreenFilter, entry, exportPath));
			}
		}
	}

	private void CardExportTask(G g, bool withScreenFilter, Card card, string path)
	{
		using var stream = new FileStream(path, FileMode.Create);
		CardRenderer.Render(g, withScreenFilter, card, stream);
	}

	private void CardCollectionExportTask(G g, bool withScreenFilter, List<List<Card>> rows, string path, Dictionary<Type, Color>? backgrounds = null)
	{
		using var stream = new FileStream(path, FileMode.Create);
		CardRenderer.RenderCollection(g, withScreenFilter, rows, stream, backgrounds, new());
	}

	private void TooltipExportTask(G g, bool withScreenFilter, Tooltip tooltip, string path)
	{
		using var stream = new FileStream(path, FileMode.Create);
		TooltipRenderer.Render(g, withScreenFilter, tooltip, stream);
	}
}
