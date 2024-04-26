using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shockah.CatDiscordBotDataExport
{
	internal sealed class ApiImplementation : ICatApi
	{
		public void RegisterTooltips(Deck deck, List<Tooltip> tooltips)
		{
			CardRenderer.CardPosterTooltips[deck] = tooltips;
		}

		public List<Tooltip> GetTooltips(Deck deck)
		{
			if (!CardRenderer.CardPosterTooltips.ContainsKey(deck)) return new();

			return CardRenderer.CardPosterTooltips[deck];
		}
	}
}
