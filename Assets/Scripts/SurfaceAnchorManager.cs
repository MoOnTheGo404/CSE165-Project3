using System.Collections.Generic;
using UnityEngine;

public class SurfaceAnchorManager : MonoBehaviour
{
    [System.Serializable]
    public class SurfaceEntry
    {
        public string surfaceName;
        public Transform surfaceTransform;
        public Collider surfaceCollider;
        public bool isObstacle;
    }

    [SerializeField] private List<SurfaceEntry> surfaces = new List<SurfaceEntry>();
    [SerializeField] private string obstacleLayerName = "Obstacle";

    private void Start()
    {
        RefreshSurfaceColliders();
        ValidateSurfaces();
    }

    public void RefreshSurfaceColliders()
    {
        if (surfaces == null)
        {
            return;
        }

        foreach (SurfaceEntry surface in surfaces)
        {
            if (surface == null || surface.surfaceTransform == null || surface.surfaceCollider != null)
            {
                continue;
            }

            surface.surfaceCollider = surface.surfaceTransform.GetComponent<Collider>();
        }
    }

    private void ValidateSurfaces()
    {
        if (surfaces == null)
        {
            return;
        }

        int obstacleLayer = LayerMask.NameToLayer(obstacleLayerName);

        foreach (SurfaceEntry surface in surfaces)
        {
            if (surface == null)
            {
                continue;
            }

            string displayName = string.IsNullOrWhiteSpace(surface.surfaceName)
                ? "Unnamed Surface"
                : surface.surfaceName;

            if (surface.surfaceTransform == null)
            {
                Debug.LogWarning($"SurfaceAnchorManager: '{displayName}' is missing a transform.", this);
            }

            if (surface.surfaceCollider == null)
            {
                Debug.LogWarning($"SurfaceAnchorManager: '{displayName}' is missing a collider.", this);
            }

            if (!surface.isObstacle)
            {
                continue;
            }

            if (obstacleLayer < 0)
            {
                Debug.LogWarning($"SurfaceAnchorManager: obstacle layer '{obstacleLayerName}' does not exist.", this);
                continue;
            }

            if (surface.surfaceTransform != null && surface.surfaceTransform.gameObject.layer != obstacleLayer)
            {
                Debug.LogWarning(
                    $"SurfaceAnchorManager: obstacle surface '{displayName}' is not on layer '{obstacleLayerName}'.",
                    surface.surfaceTransform
                );
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (surfaces == null)
        {
            return;
        }

        foreach (SurfaceEntry surface in surfaces)
        {
            if (surface == null || surface.surfaceTransform == null)
            {
                continue;
            }

            if (surface.isObstacle)
            {
                DrawObstacleGizmo(surface);
            }
            else
            {
                DrawSurfaceGizmo(surface);
            }
        }
    }

    private void DrawSurfaceGizmo(SurfaceEntry surface)
    {
        Gizmos.color = Color.green;

        Transform surfaceTransform = surface.surfaceTransform;
        Vector3 size = surface.surfaceCollider != null
            ? surface.surfaceCollider.bounds.size
            : surfaceTransform.lossyScale;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(
            surfaceTransform.position,
            surfaceTransform.rotation,
            Vector3.one
        );

        Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, 0.01f, size.z));
        Gizmos.matrix = previousMatrix;
    }

    private void DrawObstacleGizmo(SurfaceEntry surface)
    {
        Gizmos.color = Color.red;

        if (surface.surfaceCollider != null)
        {
            Bounds bounds = surface.surfaceCollider.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            return;
        }

        Transform surfaceTransform = surface.surfaceTransform;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(
            surfaceTransform.position,
            surfaceTransform.rotation,
            surfaceTransform.lossyScale
        );

        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = previousMatrix;
    }
}
