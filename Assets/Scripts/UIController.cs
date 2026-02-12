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

		[Header("Target Cursor")]
		[SerializeField] private Canvas _uiCanvas;
		[SerializeField] private GameObject _targetCursorPrefab;
		private RectTransform _targetCursorInstance;

		[Header("Target Line")]
		[SerializeField] private Material _targetLineMaterial;
		[SerializeField] private Color _targetLineColor = Color.black;
		[SerializeField] private float _targetLineWidth = 0.05f;
		[SerializeField] private float _targetLineTextureTiling = 0.5f;
		[SerializeField] private float _targetLineDepthOffset = 0.02f;
		[SerializeField] private int _targetLineDotPixels = 4;
		[SerializeField] private int _targetLineGapPixels = 2;
		[SerializeField] private int _targetLineTextureHeightPixels = 16;
		private LineRenderer _targetLine;
		private Material _targetLineMaterialInstance;
		private Texture2D _targetLineTextureInstance;

		private Cemetery _localCemetery;
		private Cemetery _opponentCemetery;
		private ExtraDeck _localExtraDeck;
		private ExtraDeck _opponentExtraDeck;

		private void Awake()
		{
			_gameController ??= FindFirstObjectByType<GameController>();
			if (_uiCanvas == null)
				_uiCanvas = FindFirstObjectByType<Canvas>();

			var local = _gameController?.LocalCemeteryScrollRect;
			var opponent = _gameController?.OpponentCemeteryScrollRect;
			var localExtra = _gameController?.LocalExtraDeckScrollRect;
			var opponentExtra = _gameController?.OpponentExtraDeckScrollRect;

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
			if (localExtra != null)
			{
				localExtra.enabled = true;
				localExtra.gameObject.SetActive(false);
			}
			if (opponentExtra != null)
			{
				opponentExtra.enabled = true;
				opponentExtra.gameObject.SetActive(false);
			}
		}

		private void Start()
		{
			TryBindCemeteries();
			TryBindExtraDecks();
			RebuildAllCemeteryLists();
			RebuildAllExtraDeckLists();
		}

		private void Update()
		{
			if (_localCemetery == null || _opponentCemetery == null)
				TryBindCemeteries();
			if (_localExtraDeck == null || _opponentExtraDeck == null)
				TryBindExtraDecks();

			if (IsTargeting() && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
			{
				_gameController?.GameState?.Targeting?.Cancel("Cancelled by player.");
				return;
			}

			UpdateTargetCursor();
			UpdateTargetLine();

			if (!Input.GetMouseButtonDown(0))
				return;

			GlobalLeftClick?.Invoke(Input.mousePosition);

			if (IsPointerOverZoneScrollRect())
				return;

			if (IsPointerOverZoneViewCollider())
				return;

			CemeteryController.DeselectAll();
			ExtraDeckController.DeselectAll();
		}

		private void UpdateTargetCursor()
		{
			if (!IsTargeting())
			{
				DestroyTargetCursorInstance();
				return;
			}

			if (!EnsureTargetCursorInstance())
				return;

			UpdateTargetCursorPosition();
		}

		private void UpdateTargetLine()
		{
			if (!IsTargeting())
			{
				DestroyTargetLineInstance();
				return;
			}

			if (!EnsureTargetLineInstance())
				return;

			if (!TryGetTargetLineEndpoints(out var startWorld, out var endWorld))
			{
				_targetLine.enabled = false;
				return;
			}

			_targetLine.enabled = true;
			_targetLine.positionCount = 2;
			_targetLine.SetPosition(0, startWorld);
			_targetLine.SetPosition(1, endWorld);

			UpdateTargetLineTiling(startWorld, endWorld);
		}

		private void DestroyTargetLineInstance()
		{
			if (_targetLine == null)
				return;

			var go = _targetLine.gameObject;
			_targetLine = null;

			if (_targetLineMaterialInstance != null)
			{
				Destroy(_targetLineMaterialInstance);
				_targetLineMaterialInstance = null;
			}

			if (_targetLineTextureInstance != null)
			{
				Destroy(_targetLineTextureInstance);
				_targetLineTextureInstance = null;
			}

			if (go != null)
				Destroy(go);
		}

		private bool EnsureTargetLineInstance()
		{
			if (_targetLine != null)
				return true;

			var parent = _gameController != null && _gameController.GameplayRoot != null
				? _gameController.GameplayRoot
				: transform;

			var go = new GameObject("TargetLine");
			go.transform.SetParent(parent, worldPositionStays: true);

			_targetLine = go.AddComponent<LineRenderer>();
			_targetLine.useWorldSpace = true;
			_targetLine.alignment = LineAlignment.View;
			_targetLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			_targetLine.receiveShadows = false;
			_targetLine.numCapVertices = 0;
			_targetLine.numCornerVertices = 2;
			_targetLine.textureMode = LineTextureMode.Tile;
			_targetLine.widthMultiplier = Mathf.Max(0.001f, _targetLineWidth);
			_targetLine.startColor = _targetLineColor;
			_targetLine.endColor = _targetLineColor;
			_targetLine.enabled = false;

			if (_targetLineMaterial != null)
			{
				_targetLineMaterialInstance = new Material(_targetLineMaterial);
				_targetLine.material = _targetLineMaterialInstance;
			}
			else
			{
				// Auto-dotted: create a tiny repeating texture (dot + gap).
				_targetLineTextureInstance = CreateDottedLineTexture(
					dotPixels: Mathf.Max(1, _targetLineDotPixels),
					gapPixels: Mathf.Max(0, _targetLineGapPixels),
					heightPixels: Mathf.Clamp(_targetLineTextureHeightPixels, 1, 16));

				var shader = Shader.Find("Sprites/Default");
				_targetLineMaterialInstance = new Material(shader);
				_targetLineMaterialInstance.mainTexture = _targetLineTextureInstance;
				_targetLine.material = _targetLineMaterialInstance;
			}

			return _targetLine != null;
		}

		private static Texture2D CreateDottedLineTexture(int dotPixels, int gapPixels, int heightPixels)
		{
			int width = Mathf.Max(1, dotPixels + gapPixels);
			int height = Mathf.Max(1, heightPixels);

			var tex = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false)
			{
				wrapMode = TextureWrapMode.Repeat,
				filterMode = FilterMode.Point
			};

			Color32 on = new Color32(255, 255, 255, 255);
			Color32 off = new Color32(255, 255, 255, 0);

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					tex.SetPixel(x, y, x < dotPixels ? on : off);
				}
			}

			tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
			return tex;
		}

		private bool TryGetTargetLineEndpoints(out Vector3 startWorld, out Vector3 endWorld)
		{
			startWorld = default;
			endWorld = default;

			var gc = _gameController;
			var targeting = gc != null && gc.GameState != null ? gc.GameState.Targeting : null;
			if (targeting == null || !targeting.IsActive)
				return false;

			var sourceCard = targeting.SourceCard;
			if (sourceCard == null)
				return false;

			var registry = gc != null ? gc.CardViewRegistry : null;
			var sourceView = registry != null ? registry.GetOrNull(sourceCard) : null;
			if (sourceView == null)
				return false;

			var cam = Camera.main;
			if (cam == null)
				return false;

			startWorld = sourceView.transform.position;

			var startScreen = cam.WorldToScreenPoint(startWorld);
			if (startScreen.z <= 0f)
				return false;

			endWorld = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, startScreen.z));

			if (_targetLineDepthOffset > 0f)
			{
				var toCam = (cam.transform.position - startWorld);
				var dir = toCam.sqrMagnitude > 0.0001f ? toCam.normalized : (-cam.transform.forward);
				startWorld += dir * _targetLineDepthOffset;
				endWorld += dir * _targetLineDepthOffset;
			}

			return true;
		}

		private void UpdateTargetLineTiling(Vector3 startWorld, Vector3 endWorld)
		{
			if (_targetLine == null)
				return;

			var mat = _targetLine.material;
			if (mat == null || mat.mainTexture == null)
				return;

			float dist = Vector3.Distance(startWorld, endWorld);
			float tiling = Mathf.Max(0.01f, _targetLineTextureTiling);
			mat.mainTextureScale = new Vector2(dist * tiling, 1f);
		}

		private bool IsTargeting()
		{
			return _gameController != null
				&& _gameController.GameState != null
				&& _gameController.GameState.Targeting != null
				&& _gameController.GameState.Targeting.IsActive;
		}

		private void DestroyTargetCursorInstance()
		{
			if (_targetCursorInstance == null)
				return;

			Destroy(_targetCursorInstance.gameObject);
			_targetCursorInstance = null;
		}

		private bool EnsureTargetCursorInstance()
		{
			if (_targetCursorInstance != null)
				return true;

			if (_uiCanvas == null || _targetCursorPrefab == null)
				return false;

			var go = Instantiate(_targetCursorPrefab, _uiCanvas.transform);
			_targetCursorInstance = go != null ? go.GetComponent<RectTransform>() : null;
			if (_targetCursorInstance != null)
				return true;

			if (go != null)
				Destroy(go);
			return false;
		}

		private void UpdateTargetCursorPosition()
		{
			if (_uiCanvas == null || _targetCursorInstance == null)
				return;

			var canvasRect = _uiCanvas.transform as RectTransform;
			if (canvasRect == null)
				return;

			Camera cam = null;
			if (_uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
				cam = _uiCanvas.worldCamera != null ? _uiCanvas.worldCamera : Camera.main;

			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, cam, out var localPoint))
				_targetCursorInstance.anchoredPosition = localPoint;
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

		private void TryBindExtraDecks()
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

			ExtraDeck local = p1.IsLocalPlayer ? p1.ExtraDeck : p2.ExtraDeck;
			ExtraDeck opponent = p1.IsLocalPlayer ? p2.ExtraDeck : p1.ExtraDeck;
			if (local == null || opponent == null)
				return;

			if (_localExtraDeck == local && _opponentExtraDeck == opponent)
				return;

			UnsubscribeExtraDeckEvents();
			_localExtraDeck = local;
			_opponentExtraDeck = opponent;
			SubscribeExtraDeckEvents();
			RebuildAllExtraDeckLists();
		}

		private void SubscribeExtraDeckEvents()
		{
			if (_localExtraDeck != null)
			{
				_localExtraDeck.CardAdded += OnAnyExtraDeckChanged;
				_localExtraDeck.CardRemoved += OnAnyExtraDeckChanged;
			}
			if (_opponentExtraDeck != null)
			{
				_opponentExtraDeck.CardAdded += OnAnyExtraDeckChanged;
				_opponentExtraDeck.CardRemoved += OnAnyExtraDeckChanged;
			}
		}

		private void UnsubscribeExtraDeckEvents()
		{
			if (_localExtraDeck != null)
			{
				_localExtraDeck.CardAdded -= OnAnyExtraDeckChanged;
				_localExtraDeck.CardRemoved -= OnAnyExtraDeckChanged;
			}
			if (_opponentExtraDeck != null)
			{
				_opponentExtraDeck.CardAdded -= OnAnyExtraDeckChanged;
				_opponentExtraDeck.CardRemoved -= OnAnyExtraDeckChanged;
			}
		}

		private void OnDestroy()
		{
			UnsubscribeCemeteryEvents();
			UnsubscribeExtraDeckEvents();
		}

		private void OnAnyCemeteryChanged(Card changedCard)
		{
			_ = changedCard;
			RebuildAllCemeteryLists();
		}

		private void OnAnyExtraDeckChanged(Card changedCard)
		{
			_ = changedCard;
			RebuildAllExtraDeckLists();
		}

		private void RebuildAllCemeteryLists()
		{
			var gc = _gameController;
			if (gc == null)
				return;

			RebuildCemeteryList(gc.LocalCemeteryScrollRect, _localCemetery);
			RebuildCemeteryList(gc.OpponentCemeteryScrollRect, _opponentCemetery);
		}

		private void RebuildAllExtraDeckLists()
		{
			var gc = _gameController;
			if (gc == null)
				return;

			RebuildExtraDeckList(gc.LocalExtraDeckScrollRect, _localExtraDeck);
			RebuildExtraDeckList(gc.OpponentExtraDeckScrollRect, _opponentExtraDeck);
		}

		private void RebuildExtraDeckList(ScrollRect scrollRect, ExtraDeck extraDeck)
		{
			// Touch an instance field so analyzers don't suggest making this method static.
			_ = _uiCardPrefab;

			if (scrollRect == null || extraDeck == null)
				return;

			if (!TryGetContent(scrollRect, out var content))
				return;

			ClearContent(content);

			var cards = extraDeck.Cards;
			for (int i = 0; i < cards.Count; i++)			
			{
				var card = cards[i];
				var prefab = GetUIPrefabFor(card);
				if (prefab == null)
					continue;

				var item = Instantiate(prefab, content);
				item.name = $"ExtraDeckCard_{(card != null ? card.Name : "null")}";
				item.SetActive(true);
				BindRow(item, card);
			}

			FinalizeLayout(scrollRect, content);
		}

		private void RebuildCemeteryList(ScrollRect scrollRect, Cemetery cemetery)
		{
			if (scrollRect == null || cemetery == null)
				return;

			if (!TryGetContent(scrollRect, out var content))
				return;

			ClearContent(content);
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
			}
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

		private bool IsPointerOverZoneScrollRect()
		{
			if (EventSystem.current == null)
				return false;

			var local = _gameController != null ? _gameController.LocalCemeteryScrollRect : null;
			var opponent = _gameController != null ? _gameController.OpponentCemeteryScrollRect : null;
			var localExtra = _gameController != null ? _gameController.LocalExtraDeckScrollRect : null;
			var opponentExtra = _gameController != null ? _gameController.OpponentExtraDeckScrollRect : null;
			ScrollRect[] scrollRects = { local, opponent, localExtra, opponentExtra };

			if (!HasAnyScrollRect(scrollRects))
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
				if (IsChildOfAnyScrollRect(t, scrollRects))
					return true;
			}

			return false;
		}

		private static bool HasAnyScrollRect(ScrollRect[] scrollRects)
		{
			if (scrollRects == null)
				return false;
			for (int i = 0; i < scrollRects.Length; i++)
			{
				if (scrollRects[i] != null)
					return true;
			}
			return false;
		}

		private static bool IsChildOfAnyScrollRect(Transform t, ScrollRect[] scrollRects)
		{
			if (t == null || scrollRects == null)
				return false;
			for (int i = 0; i < scrollRects.Length; i++)
			{
				var sr = scrollRects[i];
				if (sr != null && t.IsChildOf(sr.transform))
					return true;
			}
			return false;
		}

		private static bool IsPointerOverZoneViewCollider()
		{
			var cam = Camera.main;
			if (cam == null)
				return false;

			Ray ray = cam.ScreenPointToRay(Input.mousePosition);
			if (!Physics.Raycast(ray, out RaycastHit hit, 200f))
				return false;

			if (hit.transform == null)
				return false;

			return hit.transform.GetComponentInParent<CemeteryView>() != null
				|| hit.transform.GetComponentInParent<ExtraDeckView>() != null;
		}
	}
}
