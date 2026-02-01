using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace EdgeOfUniverse.Editor
{
    /// <summary>
    /// Editor tool to spawn NavMesh obstacles for more interesting pathfinding.
    /// </summary>
    public class NavMeshObstacleSpawner : EditorWindow
    {
        private enum ObstaclePattern
        {
            Random,
            Grid,
            Perimeter,
            Corridors,
            Custom
        }

        private ObstaclePattern pattern = ObstaclePattern.Random;
        private int obstacleCount = 15;
        private float spawnRadius = 40f;
        private float minObstacleSize = 2f;
        private float maxObstacleSize = 6f;
        private float minHeight = 2f;
        private float maxHeight = 5f;
        private bool addNavMeshObstacle = true;
        private bool rebakeAfterSpawn = true;

        [MenuItem("Tools/Edge of Universe/Spawn Obstacles")]
        public static void ShowWindow()
        {
            GetWindow<NavMeshObstacleSpawner>("Obstacle Spawner");
        }

        private void OnGUI()
        {
            GUILayout.Label("NavMesh Obstacle Spawner", EditorStyles.boldLabel);
            GUILayout.Space(10);

            pattern = (ObstaclePattern)EditorGUILayout.EnumPopup("Pattern", pattern);
            obstacleCount = EditorGUILayout.IntSlider("Obstacle Count", obstacleCount, 5, 50);
            spawnRadius = EditorGUILayout.Slider("Spawn Radius", spawnRadius, 10f, 100f);

            GUILayout.Space(10);
            GUILayout.Label("Obstacle Size", EditorStyles.boldLabel);
            minObstacleSize = EditorGUILayout.FloatField("Min Size", minObstacleSize);
            maxObstacleSize = EditorGUILayout.FloatField("Max Size", maxObstacleSize);
            minHeight = EditorGUILayout.FloatField("Min Height", minHeight);
            maxHeight = EditorGUILayout.FloatField("Max Height", maxHeight);

            GUILayout.Space(10);
            addNavMeshObstacle = EditorGUILayout.Toggle("Add NavMeshObstacle", addNavMeshObstacle);
            rebakeAfterSpawn = EditorGUILayout.Toggle("Rebake NavMesh After", rebakeAfterSpawn);

            GUILayout.Space(20);

            if (GUILayout.Button("Spawn Obstacles", GUILayout.Height(35)))
            {
                SpawnObstacles();
            }

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Obstacles"))
            {
                ClearObstacles();
            }
            if (GUILayout.Button("Rebake NavMesh"))
            {
                RebakeNavMesh();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Patterns:\n" +
                "• Random: Scattered obstacles\n" +
                "• Grid: Aligned obstacles with gaps\n" +
                "• Perimeter: Obstacles around edges\n" +
                "• Corridors: Creates pathways\n" +
                "• Custom: Mixed variety",
                MessageType.Info);
        }

        private void SpawnObstacles()
        {
            // Create container
            GameObject container = GameObject.Find("Obstacles");
            if (container == null)
            {
                container = new GameObject("Obstacles");
                Undo.RegisterCreatedObjectUndo(container, "Create Obstacles Container");
            }

            switch (pattern)
            {
                case ObstaclePattern.Random:
                    SpawnRandomObstacles(container.transform);
                    break;
                case ObstaclePattern.Grid:
                    SpawnGridObstacles(container.transform);
                    break;
                case ObstaclePattern.Perimeter:
                    SpawnPerimeterObstacles(container.transform);
                    break;
                case ObstaclePattern.Corridors:
                    SpawnCorridorObstacles(container.transform);
                    break;
                case ObstaclePattern.Custom:
                    SpawnCustomObstacles(container.transform);
                    break;
            }

            if (rebakeAfterSpawn)
            {
                RebakeNavMesh();
            }

            Debug.Log($"[Obstacle Spawner] Spawned {obstacleCount} obstacles.");
        }

        private void SpawnRandomObstacles(Transform parent)
        {
            for (int i = 0; i < obstacleCount; i++)
            {
                Vector2 randomPos = Random.insideUnitCircle * spawnRadius;

                // Avoid center spawn area
                if (randomPos.magnitude < 8f)
                {
                    randomPos = randomPos.normalized * 8f;
                }

                Vector3 position = new Vector3(randomPos.x, 0, randomPos.y);
                CreateObstacle(parent, position, i);
            }
        }

        private void SpawnGridObstacles(Transform parent)
        {
            int gridSize = Mathf.CeilToInt(Mathf.Sqrt(obstacleCount));
            float spacing = (spawnRadius * 2f) / (gridSize + 1);
            int index = 0;

            for (int x = 0; x < gridSize && index < obstacleCount; x++)
            {
                for (int z = 0; z < gridSize && index < obstacleCount; z++)
                {
                    // Skip some cells for pathways
                    if (Random.value < 0.3f) continue;

                    // Skip center area
                    if (x == gridSize / 2 && z == gridSize / 2) continue;

                    float posX = -spawnRadius + spacing + x * spacing;
                    float posZ = -spawnRadius + spacing + z * spacing;

                    // Add some randomness
                    posX += Random.Range(-spacing * 0.2f, spacing * 0.2f);
                    posZ += Random.Range(-spacing * 0.2f, spacing * 0.2f);

                    CreateObstacle(parent, new Vector3(posX, 0, posZ), index);
                    index++;
                }
            }
        }

        private void SpawnPerimeterObstacles(Transform parent)
        {
            float innerRadius = spawnRadius * 0.3f;
            float outerRadius = spawnRadius * 0.9f;

            for (int i = 0; i < obstacleCount; i++)
            {
                float angle = (i / (float)obstacleCount) * Mathf.PI * 2f;
                float radius = Random.Range(innerRadius, outerRadius);

                // Cluster more towards outer edge
                if (Random.value > 0.4f)
                {
                    radius = Random.Range(outerRadius * 0.7f, outerRadius);
                }

                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );

                CreateObstacle(parent, position, i);
            }
        }

        private void SpawnCorridorObstacles(Transform parent)
        {
            // Create walls that form corridors
            int wallCount = obstacleCount / 3;
            int index = 0;

            // Horizontal walls
            for (int i = 0; i < wallCount && index < obstacleCount; i++)
            {
                float z = Random.Range(-spawnRadius * 0.7f, spawnRadius * 0.7f);
                float startX = Random.Range(-spawnRadius * 0.8f, 0);
                float length = Random.Range(spawnRadius * 0.3f, spawnRadius * 0.6f);

                // Create wall segments
                int segments = Random.Range(2, 5);
                for (int j = 0; j < segments && index < obstacleCount; j++)
                {
                    float x = startX + (j * length / segments);

                    // Gap for passage
                    if (j == segments / 2) continue;

                    CreateObstacle(parent, new Vector3(x, 0, z), index, true);
                    index++;
                }
            }

            // Vertical walls
            for (int i = 0; i < wallCount && index < obstacleCount; i++)
            {
                float x = Random.Range(-spawnRadius * 0.7f, spawnRadius * 0.7f);
                float startZ = Random.Range(-spawnRadius * 0.8f, 0);
                float length = Random.Range(spawnRadius * 0.3f, spawnRadius * 0.6f);

                int segments = Random.Range(2, 5);
                for (int j = 0; j < segments && index < obstacleCount; j++)
                {
                    float z = startZ + (j * length / segments);

                    if (j == segments / 2) continue;

                    CreateObstacle(parent, new Vector3(x, 0, z), index, false);
                    index++;
                }
            }

            // Fill remaining with random
            while (index < obstacleCount)
            {
                Vector2 pos = Random.insideUnitCircle * spawnRadius * 0.5f;
                CreateObstacle(parent, new Vector3(pos.x, 0, pos.y), index);
                index++;
            }
        }

        private void SpawnCustomObstacles(Transform parent)
        {
            int index = 0;

            // Large corner structures
            Vector3[] corners = {
                new Vector3(-spawnRadius * 0.7f, 0, -spawnRadius * 0.7f),
                new Vector3(spawnRadius * 0.7f, 0, -spawnRadius * 0.7f),
                new Vector3(-spawnRadius * 0.7f, 0, spawnRadius * 0.7f),
                new Vector3(spawnRadius * 0.7f, 0, spawnRadius * 0.7f)
            };

            foreach (var corner in corners)
            {
                if (index >= obstacleCount) break;
                CreateLargeObstacle(parent, corner, index);
                index++;
            }

            // Medium scattered obstacles
            int mediumCount = obstacleCount / 3;
            for (int i = 0; i < mediumCount && index < obstacleCount; i++)
            {
                Vector2 pos = Random.insideUnitCircle * spawnRadius * 0.6f;
                if (pos.magnitude < 10f) pos = pos.normalized * 10f;

                CreateObstacle(parent, new Vector3(pos.x, 0, pos.y), index);
                index++;
            }

            // Small cover objects
            while (index < obstacleCount)
            {
                Vector2 pos = Random.insideUnitCircle * spawnRadius * 0.8f;
                CreateSmallObstacle(parent, new Vector3(pos.x, 0, pos.y), index);
                index++;
            }
        }

        private void CreateObstacle(Transform parent, Vector3 position, int index, bool elongatedX = false)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = $"Obstacle_{index}";
            obstacle.transform.SetParent(parent);
            obstacle.isStatic = true;

            float sizeX = Random.Range(minObstacleSize, maxObstacleSize);
            float sizeZ = Random.Range(minObstacleSize, maxObstacleSize);
            float height = Random.Range(minHeight, maxHeight);

            if (elongatedX)
            {
                sizeX *= 2f;
                sizeZ *= 0.5f;
            }

            obstacle.transform.localScale = new Vector3(sizeX, height, sizeZ);
            obstacle.transform.position = position + Vector3.up * (height / 2f);
            obstacle.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            ApplyObstacleMaterial(obstacle);

            if (addNavMeshObstacle)
            {
                var navObstacle = obstacle.AddComponent<NavMeshObstacle>();
                navObstacle.carving = true;
                navObstacle.carvingMoveThreshold = 0.1f;
            }

            Undo.RegisterCreatedObjectUndo(obstacle, "Create Obstacle");
        }

        private void CreateLargeObstacle(Transform parent, Vector3 position, int index)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = $"LargeObstacle_{index}";
            obstacle.transform.SetParent(parent);
            obstacle.isStatic = true;

            float size = Random.Range(maxObstacleSize, maxObstacleSize * 1.5f);
            float height = Random.Range(maxHeight, maxHeight * 1.5f);

            obstacle.transform.localScale = new Vector3(size, height, size);
            obstacle.transform.position = position + Vector3.up * (height / 2f);
            obstacle.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 90f), 0);

            ApplyObstacleMaterial(obstacle, true);

            if (addNavMeshObstacle)
            {
                var navObstacle = obstacle.AddComponent<NavMeshObstacle>();
                navObstacle.carving = true;
            }

            Undo.RegisterCreatedObjectUndo(obstacle, "Create Large Obstacle");
        }

        private void CreateSmallObstacle(Transform parent, Vector3 position, int index)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = $"Cover_{index}";
            obstacle.transform.SetParent(parent);
            obstacle.isStatic = true;

            float size = Random.Range(minObstacleSize * 0.5f, minObstacleSize);
            float height = Random.Range(minHeight * 0.5f, minHeight);

            obstacle.transform.localScale = new Vector3(size, height, size);
            obstacle.transform.position = position + Vector3.up * (height / 2f);
            obstacle.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            ApplyObstacleMaterial(obstacle);

            if (addNavMeshObstacle)
            {
                var navObstacle = obstacle.AddComponent<NavMeshObstacle>();
                navObstacle.carving = true;
            }

            Undo.RegisterCreatedObjectUndo(obstacle, "Create Small Obstacle");
        }

        private void ApplyObstacleMaterial(GameObject obstacle, bool isLarge = false)
        {
            var renderer = obstacle.GetComponent<Renderer>();
            if (renderer == null) return;

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            if (isLarge)
            {
                // Darker, industrial look for large structures
                mat.color = new Color(
                    Random.Range(0.2f, 0.3f),
                    Random.Range(0.22f, 0.32f),
                    Random.Range(0.25f, 0.35f)
                );
            }
            else
            {
                // Varied metal/concrete colors
                float grey = Random.Range(0.3f, 0.5f);
                mat.color = new Color(grey, grey * 1.05f, grey * 1.1f);
            }

            renderer.material = mat;
        }

        private void ClearObstacles()
        {
            GameObject container = GameObject.Find("Obstacles");
            if (container != null)
            {
                Undo.DestroyObjectImmediate(container);
                Debug.Log("[Obstacle Spawner] Obstacles cleared.");

                if (rebakeAfterSpawn)
                {
                    RebakeNavMesh();
                }
            }
        }

        private void RebakeNavMesh()
        {
            NavMeshSurface surface = FindAnyObjectByType<NavMeshSurface>();
            if (surface != null)
            {
                surface.BuildNavMesh();
                Debug.Log("[Obstacle Spawner] NavMesh rebaked.");
            }
            else
            {
                Debug.LogWarning("[Obstacle Spawner] No NavMeshSurface found. Use Setup NavMesh first.");
            }
        }
    }
}
