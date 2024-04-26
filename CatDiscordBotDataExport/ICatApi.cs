using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shockah.CatDiscordBotDataExport
{
	public interface ICatApi
	{
		void RegisterCardPosterTooltips(Deck deck, List<Tooltip> tooltips);
	}
}
