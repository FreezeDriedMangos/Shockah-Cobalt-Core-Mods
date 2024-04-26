using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shockah.CatDiscordBotDataExport
{
	internal sealed class ApiImplementation : ICatApi
	{
		public void RegisterCardPosterTooltips(Deck deck, List<Tooltip> tooltips)
		{
			CardRenderer.CardPosterTooltips[deck] = tooltips;
		}
	}
}
