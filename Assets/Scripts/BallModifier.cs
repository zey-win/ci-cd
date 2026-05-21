using UnityEngine;

[System.Serializable]
public class BallModifier
{
    public float scale = 1f;
    public float mass = 1f;
    public float gravityScale = 1f;

    public Color color = Color.white;

    public PlinkoBall PrepareBall(PlinkoBall ball)
    {
        var skin = BallSkinsManager.Instance != null ? BallSkinsManager.Instance.GetActiveBall() : null;
        if (skin != null && skin.Preview != null)
            ball.Sprite.sprite = skin.Preview;

        // ball.transform.localScale = Vector3.one * scale;
        ball.Rb.mass = mass;
        ball.Rb.gravityScale = gravityScale;
        // ball.Sprite.color = color;

        if (skin != null)
            ball.SetColliderType(skin.ColliderType);
        else
            ball.SetColliderType(BallColliderType.Circle);

        return ball;
    }
}