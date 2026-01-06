using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts
{
	public class UIController : MonoBehaviour
	{
		public static event Action<Vector2> GlobalLeftClick;

		[SerializeField] private GameController _gameController;
		[System.Serializable]
		private class UICardTypePrefab
		{
			[SerializeField] private CardType _category;
			[SerializeField] private GameObject _prefab;

			public CardType Category => _category;
			public GameObject Prefab => _prefab;
		}

		[SerializeField] private GameObject _uiCardPrefab;
		[SerializeField] private UICardTypePrefab[] _uiPrefabsByCategory;

		private Cemetery _localCemetery;
		private Cemetery _opponentCemetery;

		private void Awake()
		{
			_gameController ??= FindFirstObjectByType<GameController>();

			var local = _gameController?.LocalCemeteryScrollRect;
			var opponent = _gameController?.OpponentCemeteryScrollRect;

			if (local != null)
			{
				local.enabled = true;
				local.gameObject.SetActive(false);
			}
			if (opponent != null)
			{
				opponent.enabled = true;
				opponent.gameObject.SetActive(false);
			}
		}

		private void Start()
		{
			TryBindCemeteries();
			RebuildAllCemeteryLists();
		}

		private void Update()
		{
			if (_localCemetery == null || _opponentCemetery == null)
				TryBindCemeteries();

			if (!Input.GetMouseButtonDown(0))
				return;

			GlobalLeftClick?.Invoke(Input.mousePosition);

			if (IsPointerOverCemeteryScrollRect())
				return;

			if (IsPointerOverCemeteryViewCollider())
				return;

			CemeteryController.DeselectAll();
		}

		private void TryBindCemeteries()
		{
			var gc = _gameController;
			if (gc == null)
				return;

			var board1 = gc.PlayerBoard1;
			var board2 = gc.PlayerBoard2;
			if (board1 == null || board2 == null)
				return;

			var p1 = board1.Player;
			var p2 = board2.Player;
			if (p1 == null || p2 == null)
				return;

			Cemetery local = p1.IsLocalPlayer ? p1.Cemetery : p2.Cemetery;
			Cemetery opponent = p1.IsLocalPlayer ? p2.Cemetery : p1.Cemetery;
			if (local == null || opponent == null)
				return;

			if (_localCemetery == local && _opponentCemetery == opponent)
				return;

			UnsubscribeCemeteryEvents();
			_localCemetery = local;
			_opponentCemetery = opponent;
			SubscribeCemeteryEvents();
		}

		private void SubscribeCemeteryEvents()
		{
			if (_localCemetery != null)
			{
				_localCemetery.CardAdded += OnAnyCemeteryChanged;
				_localCemetery.CardRemoved += OnAnyCemeteryChanged;
			}
			if (_opponentCemetery != null)
			{
				_opponentCemetery.CardAdded += OnAnyCemeteryChanged;
				_opponentCemetery.CardRemoved += OnAnyCemeteryChanged;
			}
		}

		private void UnsubscribeCemeteryEvents()
		{
			if (_localCemetery != null)
			{
				_localCemetery.CardAdded -= OnAnyCemeteryChanged;
				_localCemetery.CardRemoved -= OnAnyCemeteryChanged;
			}
			if (_opponentCemetery != null)
			{
				_opponentCemetery.CardAdded -= OnAnyCemeteryChanged;
				_opponentCemetery.CardRemoved -= OnAnyCemeteryChanged;
			}
		}

		private void OnDestroy()
		{
			UnsubscribeCemeteryEvents();
		}

		private void OnAnyCemeteryChanged(Card _)
		{
			RebuildAllCemeteryLists();
		}

		private void RebuildAllCemeteryLists()
		{
			var gc = _gameController;
			if (gc == null)
				return;

			RebuildCemeteryList(gc.LocalCemeteryScrollRect, _localCemetery);
			RebuildCemeteryList(gc.OpponentCemeteryScrollRect, _opponentCemetery);
		}

		private void RebuildCemeteryList(ScrollRect scrollRect, Cemetery cemetery)
		{
			if (scrollRect == null || cemetery == null)
				return;

			if (!TryGetContent(scrollRect, out var content))
				return;

			ClearContent(content);
			WarnIfLayoutMissing(content);
			CreateRows(content, cemetery);
			FinalizeLayout(scrollRect, content);
		}

		private static bool TryGetContent(ScrollRect scrollRect, out RectTransform content)
		{
			content = scrollRect != null ? scrollRect.content : null;
			if (content != null)
				return true;

			Debug.LogWarning("UIController: ScrollRect has no Content assigned.");
			return false;
		}

		private static void ClearContent(RectTransform content)
		{
			for (int i = content.childCount - 1; i >= 0; i--)
				Destroy(content.GetChild(i).gameObject);
		}

		private static void WarnIfLayoutMissing(RectTransform content)
		{
			// Layout is expected to be configured in Unity:
			// - Content should have VerticalLayoutGroup + ContentSizeFitter (vertical = PreferredSize)
			// - Item prefab should define its own size (LayoutElement) and aspect (AspectRatioFitter)
			if (content.GetComponent<VerticalLayoutGroup>() == null)
				Debug.LogWarning("UIController: ScrollRect Content has no VerticalLayoutGroup. Configure it in Unity for one-per-row layout.");
			if (content.GetComponent<ContentSizeFitter>() == null)
				Debug.LogWarning("UIController: ScrollRect Content has no ContentSizeFitter. Configure it in Unity so Content expands with rows.");
		}

		private void CreateRows(RectTransform content, Cemetery cemetery)
		{
			foreach (Card card in cemetery.Cards)
			{
				var prefab = GetUIPrefabFor(card);
				if (prefab == null)
					continue;

				var item = Instantiate(prefab, content);
				item.name = $"CemeteryCard_{(card != null ? card.Name : "null")}";
				item.SetActive(true);
				BindRow(item, card);
			}
		}

		private static void BindRow(GameObject item, Card card)
		{
			var uiView = item != null ? item.GetComponent<UICardView>() : null;
			if (uiView != null)
			{
				uiView.Bind(card);
				return;
			}

			// Fallback: if the prefab still uses legacy UI.Text.
			var label = item != null ? item.GetComponentInChildren<Text>(includeInactive: true) : null;
			if (label != null)
				label.text = card != null ? card.Name : "<null>";
		}

		private static void FinalizeLayout(ScrollRect scrollRect, RectTransform content)
		{
			Canvas.ForceUpdateCanvases();
			LayoutRebuilder.ForceRebuildLayoutImmediate(content);
			scrollRect.verticalNormalizedPosition = 1f;
		}

		private GameObject GetUIPrefabFor(Card card)
		{
			if (card == null)
				return _uiCardPrefab;

			if (_uiPrefabsByCategory != null)
			{
				for (int i = 0; i < _uiPrefabsByCategory.Length; i++)
				{
					var entry = _uiPrefabsByCategory[i];
					if (entry != null && entry.Category == card.Category && entry.Prefab != null)
						return entry.Prefab;
				}
			}

			return _uiCardPrefab;
		}

		public GameObject GetUIPrefabForCard(Card card)
		{
			return GetUIPrefabFor(card);
		}

		private bool IsPointerOverCemeteryScrollRect()
		{
			if (EventSystem.current == null)
				return false;

			var local = _gameController != null ? _gameController.LocalCemeteryScrollRect : null;
			var opponent = _gameController != null ? _gameController.OpponentCemeteryScrollRect : null;

			if (local == null && opponent == null)
				return false;

			var eventData = new PointerEventData(EventSystem.current)
			{
				position = Input.mousePosition
			};

			var results = new List<RaycastResult>();
			EventSystem.current.RaycastAll(eventData, results);

			foreach (var result in results)
			{
				Transform t = result.gameObject?.transform;
				if (t == null)
					continue;

				if (local != null && t.IsChildOf(local.transform))
					return true;
				if (opponent != null && t.IsChildOf(opponent.transform))
					return true;
			}

			return false;
		}

		private static bool IsPointerOverCemeteryViewCollider()
		{
			var cam = Camera.main;
			if (cam == null)
				return false;

			Ray ray = cam.ScreenPointToRay(Input.mousePosition);
			if (!Physics.Raycast(ray, out RaycastHit hit, 200f))
				return false;

			return hit.transform != null && hit.transform.GetComponentInParent<CemeteryView>() != null;
		}
	}
}
