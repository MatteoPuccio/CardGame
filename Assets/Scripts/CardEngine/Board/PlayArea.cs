using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Board
{
    /// <summary>
    /// Represents the full play area. Handles creation of zones and provides utility methods for spawn positions.
    /// </summary>
    public class PlayArea : MonoBehaviour
    {
        [Min(1)] public int zoneCount = 7;
        [Range(0f, 0.45f)] public float marginPercent = 0.05f;
        [Range(0f, 0.5f)] public float gapPercent = 0f; // fraction of usable width
        public float boardWidth = 7f; // fallback local width
        public float topOffset = 0.01f;
        public GameObject zonePrefab; // prefab must have PlayAreaZoneView

        /// <summary> List of model zones </summary>
        public List<PlayAreaZone> Zones { get; } = new();
        /// <summary> Mapping from model zone to its view </summary>
        public Dictionary<PlayAreaZone, PlayAreaZoneView> ZoneViews { get; } = new();

        private void Start()
        {
            ClearZones();
            CreateZones();
        }

        private void ClearZones()
        {
            Zones.Clear();
            ZoneViews.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (!child.name.StartsWith("Zone")) continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private void CreateZones()
        {
            if (zonePrefab == null || zoneCount <= 0) return;

            float width = GetBoardWidthLocal();
            float margin = width * marginPercent;
            float usable = width - margin * 2f;
            float totalGap = usable * gapPercent;
            float gap = zoneCount > 1 ? totalGap / (zoneCount - 1) : 0f;
            float zoneWidth = (usable - totalGap) / zoneCount;

            float startX = -width / 2f + margin + zoneWidth / 2f;
            float y = GetTopLocalY() + topOffset;

            for (int i = 0; i < zoneCount; i++)
            {
                // Instantiate zone prefab
                var zoneGO = Instantiate(zonePrefab, transform, false);
                zoneGO.name = $"Zone {i}";

                // Set local transform for layout
                float x = startX + i * (zoneWidth + gap);
                zoneGO.transform.localPosition = new Vector3(x, y, zoneGO.transform.localPosition.z);
                zoneGO.transform.localRotation = Quaternion.identity;
                zoneGO.transform.localScale = new Vector3(zoneWidth, 1f, 1f);

                // Create pure model
                var zoneModel = new PlayAreaZone { ZoneIndex = i };
                Zones.Add(zoneModel);

                // Get view
                var zoneView = zoneGO.GetComponent<PlayAreaZoneView>();
                if (zoneView == null)
                {
                    zoneView = zoneGO.AddComponent<PlayAreaZoneView>();
                }
                zoneView.ZoneIndex = i;

                ZoneViews.Add(zoneModel, zoneView);
            }
        }

        private float GetBoardWidthLocal()
        {
            var rend = GetComponent<Renderer>();
            if (rend != null)
                return rend.bounds.size.x / transform.lossyScale.x;

            return boardWidth;
        }

        private float GetTopLocalY()
        {
            var rend = GetComponent<Renderer>();
            if (rend != null)
                return transform.InverseTransformPoint(rend.bounds.max).y;

            return 0f;
        }

        // Returns a world-space spawn position above the given zone index
        public Vector3 GetZoneSpawnPosition(int zoneIndex, float heightOffset = 0.5f)
        {
            if (zoneIndex < 0 || zoneIndex >= Zones.Count)
                return transform.position + Vector3.up * heightOffset;

            var zoneView = ZoneViews[Zones[zoneIndex]];
            return zoneView.transform.position + Vector3.up * heightOffset;
        }

        // Finds closest zone to a world position
        public Vector3 GetClosestZoneSpawnPosition(Vector3 worldPosition, float heightOffset = 0.5f)
        {
            if (Zones.Count == 0)
                return transform.position + Vector3.up * heightOffset;

            PlayAreaZone closest = null;
            float bestDist = float.MaxValue;
            foreach (var z in Zones)
            {
                float d = Vector3.SqrMagnitude(ZoneViews[z].transform.position - worldPosition);
                if (d < bestDist)
                {
                    bestDist = d;
                    closest = z;
                }
            }

            if (closest == null)
                return transform.position + Vector3.up * heightOffset;

            return ZoneViews[closest].transform.position + Vector3.up * heightOffset;
        }
    }


}
