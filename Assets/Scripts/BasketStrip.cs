using System.Collections.Generic;
using UnityEngine;

public class BasketStrip : MonoBehaviour
{
    public Color maxColor = Color.white;
    public Color minColor = Color.white;

    public IPlinkoBaskets Strip { get; private set; }

    private void Awake()
    {
        Strip = new LinearBasketStrip();
    }

    public void ColorizeBasket()
    {
        var baskets = new List<PlinkoBasket>(Strip.GetBaskets());

        var numSprites = baskets.Count;

        for (var i = 0; i < numSprites; i++)
        {
            var t = i < numSprites / 2
                ? Mathf.Lerp(1f, 0f, (float)i / (numSprites / 2 - 1))
                : Mathf.Lerp(0f, 1f, (i - numSprites / 2) / ((float)numSprites / 2));

            baskets[i].SpriteRenderer.color = Color.Lerp(minColor, maxColor, t);
        }
    }
}