using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shockah.CatDiscordBotDataExport;

internal sealed class CardRenderer
{
	private static readonly Vec BaseCardSize = new(59, 82);
	private static readonly Vec OverborderCardSize = new(67, 90);
	private static readonly int HorizontalSpacing = 5;
	private static readonly int VerticalSpacing = 5;

	private RenderTarget2D? CurrentRenderTarget;

	// todo: replace HashSet starters with Dictionary<Type, Color> backgrounds
	public void RenderCollection(G g, bool withScreenFilter, List<List<Card>> rows, HashSet<Type> starters, Stream stream)
	{
		int maxWidth = 0;
		int maxHeight = 0;
		int maxRowSize = 0;
		foreach (var row in rows)
		{
			maxRowSize = Math.Max(maxRowSize, row.Count);
			foreach (var card in row)
			{
				var cardSize = GetImageSize(card);
				maxWidth = (int)Math.Max(maxWidth, cardSize.x);
				maxHeight = (int)Math.Max(maxHeight, cardSize.y);
			}
		}

		Vec imageSize = new(maxRowSize*maxWidth + (maxRowSize)*HorizontalSpacing, rows.Count*maxHeight + (rows.Count)*VerticalSpacing);
		RenderTarget2D target = new(g.mg.GraphicsDevice, (int)(imageSize.x * g.mg.PIX_SCALE), (int)(imageSize.y * g.mg.PIX_SCALE));

		var oldRenderTargets = g.mg.GraphicsDevice.GetRenderTargets();

		g.mg.GraphicsDevice.SetRenderTarget(target);

		g.mg.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
		Draw.StartAutoBatchFrame();


		int curX = HorizontalSpacing / 2;
		int curY = VerticalSpacing / 2;
		foreach (var row in rows)
		{
			foreach (var card in row)
			{
				try
				{
					//Vec cardOffset = new((imageSize.x - BaseCardSize.x) / 2 + 1, (imageSize.y - BaseCardSize.y) / 2 + 1);

					if (starters.Contains(card.GetType()))
					{
						var cardSize = GetImageSize(card);
						Draw.Rect(curX-HorizontalSpacing, curY-VerticalSpacing, (int)(cardSize.x)+HorizontalSpacing, (int)(cardSize.y)+VerticalSpacing, Colors.buttonEmphasis);
					}

					card.Render(g, posOverride: new Vec(curX, curY), fakeState: DB.fakeState, ignoreAnim: true, ignoreHover: true);
				}
				catch
				{
					ModEntry.Instance.Logger.LogError("There was an error exporting card {Card}.", card.Key());
				}

				curX += maxWidth + HorizontalSpacing;
			}

			curX = HorizontalSpacing / 2;
			curY += maxHeight + VerticalSpacing;
		}

		if (withScreenFilter)
			Draw.Rect(0, 0, (int)(imageSize.x * g.mg.PIX_SCALE), (int)(imageSize.y * g.mg.PIX_SCALE), Colors.screenOverlay, new BlendState
			{
				ColorBlendFunction = BlendFunction.Add,
				ColorSourceBlend = Blend.One,
				ColorDestinationBlend = Blend.InverseSourceColor,
				AlphaSourceBlend = Blend.DestinationAlpha,
				AlphaDestinationBlend = Blend.DestinationAlpha
			});
		Draw.EndAutoBatchFrame();

		g.mg.GraphicsDevice.SetRenderTargets(oldRenderTargets);

		target.SaveAsPng(stream, (int)(imageSize.x * g.mg.PIX_SCALE), (int)(imageSize.y * g.mg.PIX_SCALE));
	}

	public void Render(G g, bool withScreenFilter, Card card, Stream stream)
	{
		var imageSize = GetImageSize(card);
		if (CurrentRenderTarget is null || CurrentRenderTarget.Width != (int)(imageSize.x * g.mg.PIX_SCALE) || CurrentRenderTarget.Height != (int)(imageSize.y * g.mg.PIX_SCALE))
		{
			CurrentRenderTarget?.Dispose();
			CurrentRenderTarget = new(g.mg.GraphicsDevice, (int)(imageSize.x * g.mg.PIX_SCALE), (int)(imageSize.y * g.mg.PIX_SCALE));
		}

		var oldRenderTargets = g.mg.GraphicsDevice.GetRenderTargets();

		g.mg.GraphicsDevice.SetRenderTarget(CurrentRenderTarget);

		g.mg.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
		Draw.StartAutoBatchFrame();
		try
		{
			card.Render(g, posOverride: new((imageSize.x - BaseCardSize.x) / 2 + 1, (imageSize.y - BaseCardSize.y) / 2 + 1), fakeState: DB.fakeState, ignoreAnim: true, ignoreHover: true);
		}
		catch
		{
			ModEntry.Instance.Logger.LogError("There was an error exporting card {Card}.", card.Key());
		}
		if (withScreenFilter)
			Draw.Rect(0, 0, (int)(imageSize.x * g.mg.PIX_SCALE), (int)(imageSize.y * g.mg.PIX_SCALE), Colors.screenOverlay, new BlendState
			{
				ColorBlendFunction = BlendFunction.Add,
				ColorSourceBlend = Blend.One,
				ColorDestinationBlend = Blend.InverseSourceColor,
				AlphaSourceBlend = Blend.DestinationAlpha,
				AlphaDestinationBlend = Blend.DestinationAlpha
			});
		Draw.EndAutoBatchFrame();

		g.mg.GraphicsDevice.SetRenderTargets(oldRenderTargets);

		CurrentRenderTarget.SaveAsPng(stream, (int)(imageSize.x * g.mg.PIX_SCALE), (int)(imageSize.y * g.mg.PIX_SCALE));
	}

	private Vec GetImageSize(Card card)
	{
		var meta = card.GetMeta();
		if (meta.deck is Deck.corrupted or Deck.evilriggs)
			return OverborderCardSize;

		if (ModEntry.Instance.Helper.Content.Decks.LookupByDeck(meta.deck) is { } deckEntry && deckEntry.Configuration.OverBordersSprite is { } overBordersSprite)
		{
			var texture = SpriteLoader.Get(overBordersSprite);
			if (texture is not null)
				return new(texture.Width, texture.Height);
		}
		return BaseCardSize;
	}
}