using TMPro;
using UnityEngine;

using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts
{
	public class CardPreviewController : MonoBehaviour
	{
		public static CardPreviewController Instance { get; private set; }

		[SerializeField] private RectTransform _previewParent;
		[SerializeField] private UIController _uiController;
		[SerializeField] private GameObject _fallbackPrefab;
		[SerializeField, Min(0.1f)] private float _previewScale = 3.0f;

		private GameObject _currentPreview;
		private int _lastShowFrame = -1;

		private void OnEnable()
		{
			UIController.GlobalLeftClick += OnGlobalLeftClick;
		}

		private void OnDisable()
		{
			UIController.GlobalLeftClick -= OnGlobalLeftClick;
		}

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
			_uiController ??= FindFirstObjectByType<UIController>();

			if (_previewParent == null)
			{
				Debug.LogError("CardPreviewController: _previewParent is not assigned.");
				enabled = false;
			}
		}

		private void OnGlobalLeftClick(Vector2 screenPos)
		{
			if (_currentPreview == null)
				return;
			if (Time.frameCount == _lastShowFrame)
				return;

			if (RectTransformUtility.RectangleContainsScreenPoint(_previewParent, screenPos, null))
				return;

			Clear();
		}

		private void OnDestroy()
		{
			if (Instance == this)
				Instance = null;
		}

		public static void Show(Card card)
		{
			Instance?.ShowInternal(card);
		}

		public static void ClearPreview()
		{
			Instance?.Clear();
		}

		private void Clear()
		{
			if (_currentPreview == null)
				return;
			Destroy(_currentPreview);
			_currentPreview = null;
		}

		private void ShowInternal(Card card)
		{
			if (card == null)
				return;

			var prefab = _uiController != null ? _uiController.GetUIPrefabForCard(card) : null;
			prefab ??= _fallbackPrefab;
			if (prefab == null)
				return;

			if (_currentPreview != null)
				Destroy(_currentPreview);

			_currentPreview = Instantiate(prefab, _previewParent);
			_currentPreview.name = $"CardPreview_{card.Name}";
			_currentPreview.SetActive(true);
			_lastShowFrame = Time.frameCount;

			var rt = _currentPreview.GetComponent<RectTransform>();
			if (rt != null)
			{
				rt.anchorMin = new Vector2(0.5f, 0.5f);
				rt.anchorMax = new Vector2(0.5f, 0.5f);
				rt.pivot = new Vector2(0.5f, 0.5f);
				rt.anchoredPosition = Vector2.zero;
				rt.localScale = Vector3.one * _previewScale;
			}

			var canvasGroup = _currentPreview.GetComponent<CanvasGroup>();
			if (canvasGroup == null)
				canvasGroup = _currentPreview.AddComponent<CanvasGroup>();
			canvasGroup.interactable = false;
			canvasGroup.blocksRaycasts = false;

			var view = _currentPreview.GetComponent<UICardView>();
			if (view != null)
				view.Bind(card);
		}

	}
}
