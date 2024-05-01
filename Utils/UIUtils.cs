using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WeightBar.Utils
{
    public static class UIUtils
    {
        public static Image CreateImage(string name, Transform parent, Vector2 imageSize, Texture2D texture)
        {
            var imageGO = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            imageGO.transform.SetParent(parent);
            imageGO.transform.localScale = Vector3.one;
            imageGO.RectTransform().sizeDelta = imageSize;
            imageGO.RectTransform().anchoredPosition = Vector2.zero;

            var image = imageGO.AddComponent<Image>();
            image.sprite = Sprite.Create(texture,
                                         new Rect(0f, 0f, texture.width, texture.height),
                                         new Vector2(texture.width / 2, texture.height / 2));
            image.type = Image.Type.Simple;

            return image;
        }

        public static Image CreateProgressImage(string name, Transform parent, Vector2 barSize, Texture2D texture)
        {
            var image = CreateImage(name, parent, barSize, texture);

            // setup progress bar background
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillAmount = 1;

            return image;
        }

        public static TMP_Text CreateText(GameObject template, string name, Transform parent, Vector2 totalSize, float textFontSize)
        {
            var textGO = GameObject.Instantiate(template);
            textGO.name = name;
            textGO.transform.SetParent(parent);
            textGO.transform.ResetTransform();
            textGO.RectTransform().sizeDelta = totalSize;
            textGO.RectTransform().anchorMin = new Vector2(0.5f, 0.5f);
            textGO.RectTransform().anchorMax = new Vector2(0.5f, 0.5f);
            textGO.RectTransform().pivot = new Vector2(0.5f, 0.5f);

            var text = textGO.GetComponent<TMP_Text>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSizeMin = textFontSize;
            text.fontSizeMax = textFontSize;
            text.fontSize = textFontSize;

            return text;
        }
    }
}