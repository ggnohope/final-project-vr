using UnityEngine;
using System.Collections.Generic;
using VRDrawing.Data;

namespace VRDrawing.Rendering
{
    public class MeshStrokeRenderer : StrokeRenderer
    {
        [Header("Mesh Settings")]
        [SerializeField] private Material strokeMaterial;
        [SerializeField] private bool smoothNormals = true;

        private DrawingSurface surface;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh combinedMesh;
        private bool isInitialized = false;

        public override void Initialize(DrawingSurface surface)
        {
            this.surface = surface;

            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            if (strokeMaterial == null)
            {
                strokeMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                strokeMaterial.color = Color.white;
                strokeMaterial.enableInstancing = true;
            }

            meshRenderer.material = strokeMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            
            int surfaceLayer = surface.gameObject.layer;
            gameObject.layer = surfaceLayer;
            
            if (transform.parent != surface.transform)
            {
                transform.SetParent(surface.transform, false);
            }
            
            Debug.Log($"[MeshStrokeRenderer] Initialize: GameObject={gameObject.name}, Layer={surfaceLayer} ({LayerMask.LayerToName(surfaceLayer)}), Parent={transform.parent?.name}");

            combinedMesh = new Mesh();
            combinedMesh.name = "DrawingMesh";
            meshFilter.mesh = combinedMesh;

            isInitialized = true;
        }

        public override void RebuildAllStrokes(DrawingData data)
        {
            Debug.Log($"[MeshStrokeRenderer] RebuildAllStrokes START: data.strokes.Count={data?.strokes.Count}");
            
            if (!isInitialized || data == null)
            {
                Debug.LogWarning($"[MeshStrokeRenderer] Cannot rebuild: initialized={isInitialized}, data={data != null}");
                return;
            }

            ClearAllStrokes();

            if (data.strokes.Count == 0)
            {
                Debug.LogWarning("[MeshStrokeRenderer] No strokes to rebuild!");
                return;
            }

            List<CombineInstance> combines = new List<CombineInstance>();

            foreach (var stroke in data.strokes)
            {
                Debug.Log($"[MeshStrokeRenderer] Processing stroke: points={stroke.points.Count}, isValid={stroke.IsValid()}, color={stroke.color}, width={stroke.width}");
                
                if (!stroke.IsValid()) continue;

                Mesh strokeMesh = GenerateStrokeMesh(stroke);
                if (strokeMesh != null && strokeMesh.vertexCount > 0)
                {
                    Debug.Log($"[MeshStrokeRenderer] Generated mesh: vertices={strokeMesh.vertexCount}, triangles={strokeMesh.triangles.Length}, bounds={strokeMesh.bounds}");
                    
                    CombineInstance ci = new CombineInstance();
                    ci.mesh = strokeMesh;
                    ci.transform = Matrix4x4.identity;
                    combines.Add(ci);
                }
                else
                {
                    Debug.LogWarning($"[MeshStrokeRenderer] Failed to generate mesh for stroke!");
                }
            }

            if (combines.Count > 0)
            {
                Debug.Log($"[MeshStrokeRenderer] Combining {combines.Count} meshes...");
                combinedMesh.CombineMeshes(combines.ToArray(), true, false);
                combinedMesh.RecalculateBounds();
                
                Debug.Log($"[MeshStrokeRenderer] Combined mesh: vertices={combinedMesh.vertexCount}, triangles={combinedMesh.triangles.Length}, bounds={combinedMesh.bounds}");

                if (smoothNormals)
                {
                    combinedMesh.RecalculateNormals();
                }
                
                // QUAN TRỌNG: Kiểm tra MeshFilter
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    Debug.Log($"[MeshStrokeRenderer] ✅ MeshFilter.sharedMesh assigned! vertices={meshFilter.sharedMesh.vertexCount}");
                }
                else
                {
                    Debug.LogError($"[MeshStrokeRenderer] ❌ MeshFilter or mesh is NULL!");
                }
                
                // QUAN TRỌNG: Kiểm tra MeshRenderer
                if (meshRenderer != null && meshRenderer.enabled)
                {
                    Debug.Log($"[MeshStrokeRenderer] ✅ MeshRenderer enabled! material={meshRenderer.sharedMaterial?.name}");
                }
                else
                {
                    Debug.LogError($"[MeshStrokeRenderer] ❌ MeshRenderer disabled or NULL!");
                }
            }
            else
            {
                Debug.LogWarning("[MeshStrokeRenderer] No valid meshes to combine!");
            }

            foreach (var ci in combines)
            {
                if (ci.mesh != null)
                {
                    Destroy(ci.mesh);
                }
            }
        }


        public override void UpdateStroke(Stroke stroke)
        {
            Debug.Log($"[MeshStrokeRenderer] UpdateStroke called! stroke.points.Count={stroke?.points.Count}");
            
            if (!isInitialized || surface == null || surface.Data == null)
            {
                Debug.LogWarning($"[MeshStrokeRenderer] Cannot update: initialized={isInitialized}, surface={surface != null}, data={surface?.Data != null}");
                return;
            }
            
            RebuildAllStrokes(surface.Data);
            Debug.Log($"[MeshStrokeRenderer] RebuildAllStrokes completed");
        }

        public override void ClearAllStrokes()
        {
            if (combinedMesh != null)
            {
                combinedMesh.Clear();
            }
        }

        private Mesh GenerateStrokeMesh(Stroke stroke)
        {
            if (stroke == null || stroke.points.Count < 2) return null;

            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Color> colors = new List<Color>();

            float halfWidth = stroke.width * 0.5f;

            for (int i = 0; i < stroke.points.Count; i++)
            {
                Vector3 worldPos = surface.SurfaceUVToWorld(stroke.points[i].uv);
                Vector3 localPos = transform.InverseTransformPoint(worldPos);

                Vector3 forward = Vector3.forward;
                if (i < stroke.points.Count - 1)
                {
                    Vector3 nextWorldPos = surface.SurfaceUVToWorld(stroke.points[i + 1].uv);
                    Vector3 nextLocalPos = transform.InverseTransformPoint(nextWorldPos);
                    forward = (nextLocalPos - localPos).normalized;
                    if (forward.sqrMagnitude < 0.001f)
                    {
                        forward = Vector3.forward;
                    }
                }
                else if (i > 0)
                {
                    Vector3 prevWorldPos = surface.SurfaceUVToWorld(stroke.points[i - 1].uv);
                    Vector3 prevLocalPos = transform.InverseTransformPoint(prevWorldPos);
                    forward = (localPos - prevLocalPos).normalized;
                    if (forward.sqrMagnitude < 0.001f)
                    {
                        forward = Vector3.forward;
                    }
                }

                Vector3 right = Vector3.Cross(Vector3.back, forward).normalized;
                if (right.sqrMagnitude < 0.001f)
                {
                    right = Vector3.right;
                }

                Vector3 left = localPos - right * halfWidth;
                Vector3 rightPos = localPos + right * halfWidth;

                vertices.Add(left);
                vertices.Add(rightPos);

                colors.Add(stroke.color);
                colors.Add(stroke.color);

                if (i < stroke.points.Count - 1)
                {
                    int baseIndex = i * 2;
                    
                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 1);

                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 3);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(colors);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private void OnDestroy()
        {
            if (combinedMesh != null)
            {
                Destroy(combinedMesh);
            }
        }
    }
}
