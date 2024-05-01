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
using UnityEngine;
using UnityEngine.UI;
using WeightBar.Utils;

namespace WeightBar
{
    public class WeightBarComponent : UIElement
    {
        private static string _progressTexturePath = Path.Combine(Plugin.PluginFolder, "progressBarFill.png"); // 1x9
        private static FieldInfo _healthParametersPanelHealthController = AccessTools.Field(typeof(HealthParametersPanel), "ihealthController_0");

        private static Inventory _inventory => ClientAppUtils.GetMainApp().GetClientBackEndSession().Profile.Inventory;
        private static SkillManager _skills => ClientAppUtils.GetMainApp().GetClientBackEndSession().Profile.Skills;

        private static Vector2 _barSize = new(550, 9);
        private static Vector2 _tickSize = new(1, 9);
        private static Vector2 _position = new(0, -25);
        private float _tweenLength = 0.25f; // seconds

        private IHealthController _healthController;
        private Image _progressImage;
        private Image _backgroundImage;
        private Vector2 _baseOverweightLimits;
        private Vector2 _walkOverweightLimits;
        private Image _overweightTickMark;
        private Image _walkingDrainsTickMark;
        private Color _unencumberedColor = new Color(0.6431f, 0.7725f, 0.6627f, 1f);
        private Color _overweightColor = new Color(0.9176f, 0.7098f, 0.1961f, 1f);
        private Color _completelyOverweightColor = new Color(0.7686f, 0f, 0f, 1f);
        private Color _walkingDrainsColor;

        public static WeightBarComponent AttachToHealthParametersPanel(HealthParametersPanel healthParametersPanel)
        {
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

        private void Awake()
        {
            _walkingDrainsColor = Color.Lerp(_overweightColor, _completelyOverweightColor, 0.5f);

            // get health controller from parent
            var healthParametersPanel = transform.parent.gameObject.GetComponent<HealthParametersPanel>();
            _healthController = _healthParametersPanelHealthController.GetValue(healthParametersPanel) as IHealthController;

            // setup background
            _backgroundImage = CreateProgressImage("Background");
            _backgroundImage.color = Color.black;

            // setup current weight 
            _progressImage = CreateProgressImage("CurrentWeight");

            // setup tick marks
            _overweightTickMark = CreateTickMark("Overweight Tick");
            _walkingDrainsTickMark = CreateTickMark("Walking Drains Tick");
        }

        private void OnEnable()
        {
            OnUpdateWeightLimits();

            UI.BindEvent(_inventory.OnWeightUpdated, OnUpdateWeight);
            UI.BindEvent(_skills.Strength.OnLevelUp, OnUpdateWeightLimits);
        }

        private Image CreateTickMark(string name)
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

        private void MoveTickMark(Image tickMark, float relPos)
        {
            var pos = Mathf.Lerp(-_barSize.x/2, _barSize.x / 2, relPos);
            tickMark.RectTransform().anchoredPosition = new Vector2(pos, 0);
        }

        private Image CreateProgressImage(string name)
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

            MoveTickMark(_overweightTickMark, _baseOverweightLimits.x / _baseOverweightLimits.y);
            MoveTickMark(_walkingDrainsTickMark, _walkOverweightLimits.x / _baseOverweightLimits.y);

            UpdateWeight(false);
        }

        private void UpdateWeight(bool shouldTween = true)
        {
            var weight = _skills.StrengthBuffElite ? _inventory.TotalWeightEliteSkill : _inventory.TotalWeight;
            var overweight = _baseOverweightLimits.x;
            var walkingDrainsWeight = _walkOverweightLimits.x;
            var maxWeight = _baseOverweightLimits.y;

            // setup color
            if (weight < overweight)
            {
                _progressImage.color = _unencumberedColor;
                _overweightTickMark.color = _overweightColor;
                _walkingDrainsTickMark.color = _walkingDrainsColor;
            }
            else if (weight > overweight && weight < walkingDrainsWeight)
            {
                _progressImage.color = _overweightColor;
                _overweightTickMark.color = Color.black;
                _walkingDrainsTickMark.color = _walkingDrainsColor;
            }
            else if (weight > walkingDrainsWeight && weight < maxWeight)
            {
                _progressImage.color = _walkingDrainsColor;
                _overweightTickMark.color = Color.black;
                _walkingDrainsTickMark.color = Color.black;
            }
            else if (weight > maxWeight)
            {
                _progressImage.color = Color.red;
                _overweightTickMark.color = Color.black;
                _walkingDrainsTickMark.color = Color.black;
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