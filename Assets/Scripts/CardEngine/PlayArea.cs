using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using UnityEngine;

public class PlayArea : MonoBehaviour
{
    public int zoneCount = 7;
    public float marginPercent = 0.05f; // 5% margin on each side
    public GameObject zonePrefab; // Assign a simple empty prefab with a collider in the inspector or create in code
    public List<PlayAreaZone> Zones { get; private set; } = new List<PlayAreaZone>();

    void Awake()
    {
        CreateZones();
    }

    private void CreateZones()
    {
        float totalWidth = 7f; // Default fallback width
        float y = 0, z = 0;
        // Try to get width from RectTransform (UI)
        var rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            totalWidth = rect.rect.width;
        }
        else
        {
            // Try to get width from Renderer (3D)
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                totalWidth = rend.bounds.size.x;
                y = rend.bounds.center.y;
                z = rend.bounds.center.z;
            }
        }
        float margin = totalWidth * marginPercent;
        float usableWidth = totalWidth - 2 * margin;
        float spacing = (zoneCount > 1) ? usableWidth / (zoneCount - 1) : 0;
        float startX = -usableWidth / 2f;
        float zoneWidth = (zoneCount > 0) ? usableWidth / zoneCount : usableWidth;
        for (int i = 0; i < zoneCount; i++)
        {
            GameObject zoneGO;
            if (zonePrefab != null)
                zoneGO = Instantiate(zonePrefab, transform);
            else
                zoneGO = new GameObject($"Zone{i}");
            zoneGO.transform.parent = transform;
            // Center zone on board's Z axis (local Z = 0)
            zoneGO.transform.localPosition = new Vector3(startX + i * spacing, y, 0f);
            zoneGO.transform.localRotation = Quaternion.identity;
            // Scale zone to fit board width
            zoneGO.transform.localScale = new Vector3(zoneWidth, 1, 1);
            var zone = zoneGO.GetComponent<PlayAreaZone>() ?? zoneGO.AddComponent<PlayAreaZone>();
            zone.ZoneIndex = i;
            Zones.Add(zone);
        }
    }
}

public class PlayAreaZone : MonoBehaviour
{
    public int ZoneIndex;
    public bool IsOccupied { get; private set; }
    public DraggableCard OccupyingCard { get; private set; }

    public bool TryOccupy(DraggableCard card)
    {
        if (IsOccupied) return false;
        IsOccupied = true;
        OccupyingCard = card;
        return true;
    }

    public void Vacate()
    {
        IsOccupied = false;
        OccupyingCard = null;
    }
}
