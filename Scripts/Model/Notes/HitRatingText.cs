using UnityEngine;
using UnityEngine.UI;

public class HitRatingText : BaseVisualEffect
{
    private Text myText;
    private Color textColor;

    void Awake()
    {
        myText = GetComponent<Text>();
    }

    public void Setup(string text, Color color)
    {
        if (myText == null) myText = GetComponent<Text>();

        myText.text = text;
        myText.color = color;
        textColor = color;
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void ApplyFade()
    {
        textColor.a -= fadeSpeed * Time.deltaTime;
        if (myText != null) myText.color = textColor;

        if (textColor.a <= 0) Destroy(gameObject);
    }
}