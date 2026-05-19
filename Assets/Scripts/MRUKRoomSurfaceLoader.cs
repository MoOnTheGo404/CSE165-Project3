using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;

public class MRUKRoomSurfaceLoader : MonoBehaviour
{
    [SerializeField] private string surfaceLayerName = "Surface";
    [SerializeField] private string obstacleLayerName = "Obstacle";
    [SerializeField] private bool assignFloorToSurfaceLayer = true;
    [SerializeField] private bool assignWallsToObstacleLayer = true;
    [SerializeField] private bool addCollidersIfMissing = true;
    [SerializeField] private bool logDetails = true;

    public bool IsRoomLoaded { get; private set; }
    public Transform FloorTransform { get; private set; }
    public IReadOnlyList<Transform> WallTransforms => wallTransforms;

    private const float PlaneColliderThickness = 0.05f;
    private readonly List<Transform> wallTransforms = new();
    private MRUK subscribedMruk;

    private void OnEnable()
    {
        if (!TrySubscribeToMRUK())
        {
            return;
        }

        RefreshRoomSurfaces();
    }

    private void OnDisable()
    {
        if (subscribedMruk != null)
        {
            subscribedMruk.SceneLoadedEvent.RemoveListener(OnSceneLoaded);
            subscribedMruk = null;
        }
    }

    private void Start()
    {
        if (subscribedMruk != null || !TrySubscribeToMRUK())
        {
            return;
        }

        RefreshRoomSurfaces();
    }

    private void OnSceneLoaded()
    {
        RefreshRoomSurfaces();
    }

    public void RefreshRoomSurfaces()
    {
        IsRoomLoaded = false;
        FloorTransform = null;
        wallTransforms.Clear();

        if (MRUK.Instance == null)
        {
            Debug.LogWarning("MRUKRoomSurfaceLoader could not process room because MRUK.Instance is null.", this);
            return;
        }

        MRUKRoom room = MRUK.Instance.GetCurrentRoom();
        if (room == null)
        {
            Debug.LogWarning("MRUKRoomSurfaceLoader could not process room because no MRUK room is loaded.", this);
            return;
        }

        int surfaceLayer = assignFloorToSurfaceLayer ? GetLayer(surfaceLayerName, "floor") : -1;
        int obstacleLayer = assignWallsToObstacleLayer ? GetLayer(obstacleLayerName, "walls") : -1;

#pragma warning disable 0618
        MRUKAnchor floorAnchor = room.FloorAnchor;
#pragma warning restore 0618
        ProcessFloorAnchor(floorAnchor, surfaceLayer);
        ProcessWallAnchors(room, obstacleLayer);

        IsRoomLoaded = true;
        Debug.Log($"MRUKRoomSurfaceLoader processed room with {wallTransforms.Count} wall anchors.", this);
    }

    private void ProcessWallAnchors(MRUKRoom room, int obstacleLayer)
    {
        foreach (MRUKAnchor wallAnchor in room.WallAnchors)
        {
            if (wallAnchor == null)
            {
                continue;
            }

            wallTransforms.Add(wallAnchor.transform);

            if (assignWallsToObstacleLayer)
            {
                SetLayerRecursivelyIfValid(wallAnchor.transform, obstacleLayer);
            }

            AddBoxColliderIfMissing(wallAnchor);
        }

        if (wallTransforms.Count == 0)
        {
            Debug.LogWarning("MRUKRoomSurfaceLoader found zero MRUK wall anchors.", this);
        }
    }

    private void ProcessFloorAnchor(MRUKAnchor floorAnchor, int surfaceLayer)
    {
        if (floorAnchor == null)
        {
            Debug.LogWarning("MRUKRoomSurfaceLoader found no MRUK floor anchor.", this);
            return;
        }

        FloorTransform = floorAnchor.transform;

        if (assignFloorToSurfaceLayer)
        {
            SetLayerRecursivelyIfValid(floorAnchor.transform, surfaceLayer);
        }

        AddBoxColliderIfMissing(floorAnchor);
    }

    private bool TrySubscribeToMRUK()
    {
        if (MRUK.Instance == null)
        {
            Debug.LogWarning(
                "MRUKRoomSurfaceLoader could not find MRUK.Instance. Add an MRUK component to the scene and load the saved Quest room setup.",
                this
            );
            return false;
        }

        if (subscribedMruk == MRUK.Instance)
        {
            return true;
        }

        if (subscribedMruk != null)
        {
            subscribedMruk.SceneLoadedEvent.RemoveListener(OnSceneLoaded);
        }

        subscribedMruk = MRUK.Instance;
        subscribedMruk.SceneLoadedEvent.RemoveListener(OnSceneLoaded);
        subscribedMruk.SceneLoadedEvent.AddListener(OnSceneLoaded);
        return true;
    }

    private int GetLayer(string layerName, string usage)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogWarning(
                $"MRUKRoomSurfaceLoader could not find layer '{layerName}' for {usage}. Create the layer or update this component's layer name.",
                this
            );
        }

        return layer;
    }

    private void SetLayerRecursivelyIfValid(Transform target, int layer)
    {
        if (target == null || layer < 0)
        {
            return;
        }

        target.gameObject.layer = layer;
        for (int i = 0; i < target.childCount; i++)
        {
            SetLayerRecursivelyIfValid(target.GetChild(i), layer);
        }
    }

    private void AddBoxColliderIfMissing(MRUKAnchor anchor)
    {
        if (!addCollidersIfMissing || anchor == null || HasColliderInAnchorHierarchy(anchor))
        {
            return;
        }

        BoxCollider boxCollider = anchor.gameObject.AddComponent<BoxCollider>();

        if (anchor.VolumeBounds.HasValue)
        {
            Bounds bounds = anchor.VolumeBounds.Value;
            boxCollider.center = bounds.center;
            boxCollider.size = bounds.size;
        }
        else if (anchor.PlaneRect.HasValue)
        {
            Rect planeRect = anchor.PlaneRect.Value;
            boxCollider.center = new Vector3(planeRect.center.x, planeRect.center.y, 0f);
            boxCollider.size = new Vector3(
                Mathf.Max(planeRect.width, PlaneColliderThickness),
                Mathf.Max(planeRect.height, PlaneColliderThickness),
                PlaneColliderThickness
            );
        }
        else
        {
            Debug.LogWarning(
                $"MRUKRoomSurfaceLoader added a default BoxCollider to '{anchor.name}' because the anchor has no PlaneRect or VolumeBounds.",
                this
            );
        }

        if (logDetails)
        {
            Debug.Log($"MRUKRoomSurfaceLoader added BoxCollider to '{anchor.name}'.", this);
        }
    }

    private bool HasColliderInAnchorHierarchy(MRUKAnchor anchor)
    {
        return anchor.GetComponentInChildren<Collider>(true) != null;
    }
}
