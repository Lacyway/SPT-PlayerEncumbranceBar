using System.IO;
using System.Reflection;
using Aki.Reflection.Utils;
using Comfort.Common;
using DG.Tweening;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Health;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WeightBar.Utils;

namespace WeightBar
{
    public class WeightBarComponent : UIElement
    {
        private static string _progressTexturePath = Path.Combine(Plugin.PluginFolder, "progressBarFill.png"); // 1x9
        private static FieldInfo _healthParametersPanelHealthControllerField = AccessTools.Field(typeof(HealthParametersPanel), "ihealthController_0");
        private static FieldInfo _healthParametersPanelWeightField = AccessTools.Field(typeof(HealthParametersPanel), "_weight");
        private static FieldInfo _healthParameterPanelCurrentValueField = AccessTools.Field(typeof(HealthParameterPanel), "_currentValue");

        private static Inventory _inventory => ClientAppUtils.GetMainApp().GetClientBackEndSession().Profile.Inventory;
        private static SkillManager _skills => ClientAppUtils.GetMainApp().GetClientBackEndSession().Profile.Skills;

        private static HealthParametersPanel _healthParametersPanel; // set by AttachToHealthParametersPanel
        private static GameObject _textTemplate;
        private static Color _unencumberedColor = new(0.6431f, 0.7725f, 0.6627f, 1f);
        private static Color _overweightColor = new(0.9176f, 0.7098f, 0.1961f, 1f);
        private static Color _completelyOverweightColor = new(0.7686f, 0f, 0f, 1f);
        private static Color _walkingDrainsColor; // set by lerp
        private static Vector2 _position = new(0, -25);
        private static Vector2 _barSize = new(550, 9);
        private static Vector2 _tickSize = new(1, 9);
        private static Vector2 _textSize = new(40, 20);
        private static float _textFontSize = 10;
        private static Vector2 _textPosition = new(0, -11);
        private static float _tweenLength = 0.25f; // seconds

        private IHealthController _healthController;
        private Image _progressImage;
        private Image _backgroundImage;
        private Vector2 _baseOverweightLimits;
        private Vector2 _walkOverweightLimits;
        private Image _overweightTickMark;
        private Image _walkingDrainsTickMark;
        private TMP_Text _overweightText;
        private TMP_Text _walkingDrainsText;

        public static WeightBarComponent AttachToHealthParametersPanel(HealthParametersPanel healthParametersPanel)
        {
            // setup static variables for later
            _healthParametersPanel = healthParametersPanel;
            _walkingDrainsColor = Color.Lerp(_overweightColor, _completelyOverweightColor, 0.5f);

            // get text template
            var weightPanel = _healthParametersPanelWeightField.GetValue(_healthParametersPanel) as HealthParameterPanel;
            _textTemplate = (_healthParameterPanelCurrentValueField.GetValue(weightPanel) as TMP_Text).gameObject;

            // setup container
            var containerGO = new GameObject("WeightBarContainer", typeof(RectTransform));
            containerGO.layer = healthParametersPanel.gameObject.layer;
            containerGO.transform.SetParent(healthParametersPanel.gameObject.transform);
            containerGO.transform.localScale = Vector3.one;
            containerGO.RectTransform().sizeDelta = _barSize;
            containerGO.RectTransform().anchoredPosition = _position;

            var layoutElement = containerGO.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            // HACK: move healthparameterspanel to make sure the bottom bar renders behind us
            healthParametersPanel.gameObject.transform.SetAsLastSibling();

            var component = containerGO.AddComponent<WeightBarComponent>();
            return component;
        }

        private static Image CreateProgressImage(string name, Transform transform)
        {
            // setup progress bar background
            var imageGO = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            imageGO.transform.SetParent(transform);
            imageGO.transform.localScale = Vector3.one;
            imageGO.RectTransform().sizeDelta = _barSize;
            imageGO.RectTransform().anchoredPosition = Vector2.zero;
            var image = imageGO.AddComponent<Image>();

            var texture = TextureUtils.LoadTexture2DFromPath(_progressTexturePath);
            image.sprite = Sprite.Create(texture,
                                         new Rect(0f, 0f, texture.width, texture.height),
                                         new Vector2(texture.width / 2, texture.height / 2));
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillAmount = 1;

            return image;
        }

        private static Image CreateTickMark(string name, Transform transform)
        {
            var tickMarkGO = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            tickMarkGO.transform.SetParent(transform);
            tickMarkGO.transform.localScale = Vector3.one;
            tickMarkGO.RectTransform().sizeDelta = _tickSize;
            tickMarkGO.RectTransform().anchoredPosition = Vector2.zero;
            var image = tickMarkGO.AddComponent<Image>();

            var texture = TextureUtils.LoadTexture2DFromPath(_progressTexturePath);
            image.sprite = Sprite.Create(texture,
                                         new Rect(0f, 0f, texture.width, texture.height),
                                         new Vector2(texture.width / 2, texture.height / 2));
            image.type = Image.Type.Simple;
            image.color = Color.black;

            return image;
        }

        private static TMP_Text CreateText(string name, Transform transform)
        {
            var textGO = Instantiate(_textTemplate);
            textGO.name = name;
            textGO.transform.SetParent(transform);
            textGO.transform.ResetTransform();
            textGO.RectTransform().sizeDelta = _textSize;
            textGO.RectTransform().anchorMin = new Vector2(0.5f, 0.5f);
            textGO.RectTransform().anchorMax = new Vector2(0.5f, 0.5f);
            textGO.RectTransform().pivot = new Vector2(0.5f, 0.5f);
            textGO.RectTransform().anchoredPosition = _textPosition;

            var text = textGO.GetComponent<TMP_Text>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSizeMin = _textFontSize;
            text.fontSizeMax = _textFontSize;
            text.fontSize = _textFontSize;

            return text;
        }

        private void Awake()
        {
            // get health controller from parent
            _healthController = _healthParametersPanelHealthControllerField.GetValue(_healthParametersPanel) as IHealthController;

            // setup background
            _backgroundImage = CreateProgressImage("Background", transform);
            _backgroundImage.color = Color.black;

            // setup current weight 
            _progressImage = CreateProgressImage("CurrentWeight", transform);

            // setup tick marks
            _overweightTickMark = CreateTickMark("Overweight Tick", transform);
            _walkingDrainsTickMark = CreateTickMark("Walking Drains Tick", transform);

            // setup texts
            _overweightText = CreateText("Overweight Text", transform);
            _walkingDrainsText = CreateText("Walking Drains Text", transform);
            _overweightText.color = Color.grey;
            _walkingDrainsText.color = Color.grey;
            ShowHideText();
        }

        private void OnEnable()
        {
            OnUpdateWeightLimits();

            UI.BindEvent(_inventory.OnWeightUpdated, OnUpdateWeight);
            UI.BindEvent(_skills.Strength.OnLevelUp, OnUpdateWeightLimits);
        }

        private void MoveRelativeToBar(Component component, float relPos)
        {
            var x = Mathf.Lerp(-_barSize.x/2, _barSize.x / 2, relPos);
            var y = component.RectTransform().anchoredPosition.y;
            component.RectTransform().anchoredPosition = new Vector2(x, y);
        }

        private void ShowHideText()
        {
            if (Settings.DisplayText.Value)
            {
                _overweightText.gameObject.SetActive(true);
                _walkingDrainsText.gameObject.SetActive(true);
            }
            else
            {
                _overweightText.gameObject.SetActive(false);
                _walkingDrainsText.gameObject.SetActive(false);
            }
        }

        internal void OnSettingChanged()
        {
            ShowHideText();
        }

        private void OnUpdateWeight()
        {
            UpdateWeight();
        }

        private void OnUpdateWeightLimits()
        {
            var stamina = Singleton<BackendConfigSettingsClass>.Instance.Stamina;
            var relativeModifier = _skills.CarryingWeightRelativeModifier * _healthController.CarryingWeightRelativeModifier;
            var absoluteModifier = _healthController.CarryingWeightAbsoluteModifier * Vector2.one;

            _baseOverweightLimits = stamina.BaseOverweightLimits * relativeModifier + absoluteModifier;
            _walkOverweightLimits = stamina.WalkOverweightLimits * relativeModifier + absoluteModifier;

            // update tick mark positions
            MoveRelativeToBar(_overweightTickMark, _baseOverweightLimits.x / _baseOverweightLimits.y);
            MoveRelativeToBar(_walkingDrainsTickMark, _walkOverweightLimits.x / _baseOverweightLimits.y);

            // update text values and positions
            MoveRelativeToBar(_overweightText, _baseOverweightLimits.x / _baseOverweightLimits.y);
            MoveRelativeToBar(_walkingDrainsText, _walkOverweightLimits.x / _baseOverweightLimits.y);
            _overweightText.text = $"{_baseOverweightLimits.x:f1}";
            _walkingDrainsText.text = $"{_walkOverweightLimits.x:f1}";

            UpdateWeight(false);
        }

        private void UpdateWeight(bool shouldTween = true)
        {
            var weight = _skills.StrengthBuffElite ? _inventory.TotalWeightEliteSkill : _inventory.TotalWeight;
            var overweight = _baseOverweightLimits.x;
            var walkingDrainsWeight = _walkOverweightLimits.x;
            var maxWeight = _baseOverweightLimits.y;

            // setup color
            _overweightTickMark.color = Color.black;
            _walkingDrainsTickMark.color = Color.black;
            if (weight < overweight)
            {
                _progressImage.color = _unencumberedColor;
                _overweightTickMark.color = _overweightColor;
                _walkingDrainsTickMark.color = _walkingDrainsColor;
            }
            else if (weight > overweight && weight < walkingDrainsWeight)
            {
                _progressImage.color = _overweightColor;
                _walkingDrainsTickMark.color = _walkingDrainsColor;
            }
            else if (weight > walkingDrainsWeight && weight < maxWeight)
            {
                _progressImage.color = _walkingDrainsColor;
            }
            else if (weight > maxWeight)
            {
                _progressImage.color = Color.red;
            }

            // tween to the proper length
            if (!shouldTween || _tweenLength == 0)
            {
                _progressImage.fillAmount = weight / maxWeight;
            }
            else
            {
                _progressImage.DOFillAmount(weight / maxWeight, _tweenLength);
            }
        }
    }
}
