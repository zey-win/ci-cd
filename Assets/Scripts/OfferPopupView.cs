using UnityEngine;
using UnityEngine.UI;

public class OfferPopupView : MonoBehaviour
{
    [Header("Target image (assign ONE of them)")]
    [SerializeField] private RawImage rawImageTarget;
    [SerializeField] private Image imageTarget;

    [Header("Optional")]
    [SerializeField] private AspectRatioFitter aspectFitter; // если хочешь поддерживать аспект внутри

    public void SetImage(Texture2D texture)
    {
        if (texture == null) return;

        float aspect = (float)texture.width / texture.height; // W/H для AspectRatioFitter

        if (rawImageTarget != null)
        {
            rawImageTarget.texture = texture;
            if (aspectFitter != null) aspectFitter.aspectRatio = aspect;
            return;
        }

        if (imageTarget != null)
        {
            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            imageTarget.sprite = sprite;
            if (aspectFitter != null) aspectFitter.aspectRatio = aspect;
            return;
        }

        Debug.LogWarning($"{name}: No RawImage/Image target assigned in OfferPopupView.");
    }
}
