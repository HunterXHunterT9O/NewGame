using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SquadPreviewScene : MonoBehaviour
{
    [Header("Squad Setup")]
    [SerializeField] private List<GameObject> soldierPrefabs = new List<GameObject>();
    [SerializeField] private float soldierSpacing = 2f;
    [SerializeField] private float startDistance = 10f;
    [SerializeField] private float stopDistance = 3f;
    [SerializeField] private float walkSpeed = 2f;

    [Header("Camera")]
    [SerializeField] private Camera previewCamera;
    [SerializeField] private Vector3 cameraPosition = new Vector3(0, 1.5f, -2f);
    [SerializeField] private Vector3 cameraRotation = new Vector3(5, 0, 0);

    [Header("Animation Override (Optional)")]
    [SerializeField] private RuntimeAnimatorController overrideController;

    private List<GameObject> spawnedSoldiers = new List<GameObject>();
    private List<Animator> soldierAnimators = new List<Animator>();
    private bool isWalking = true;

    // Common animation state names to try
    private readonly string[] walkStateNames = { "Walk", "WalkForward", "Walking", "walk", "Locomotion" };
    private readonly string[] idleStateNames = { "Idle", "idle", "IdleUnarmed", "IdleBreathe" };

    // Common parameter names to try
    private readonly string[] walkBoolParams = { "IsWalking", "isWalking", "Walk", "walk", "Moving" };
    private readonly string[] speedFloatParams = { "Speed", "speed", "MoveSpeed", "Velocity" };

    void Start()
    {
        SetupCamera();
        SpawnSquad();
        StartCoroutine(SquadWalkSequence());
    }

    void SetupCamera()
    {
        if (previewCamera == null)
        {
            previewCamera = Camera.main;
            if (previewCamera == null)
            {
                previewCamera = FindFirstObjectByType<Camera>();
            }
        }

        if (previewCamera != null)
        {
            previewCamera.transform.position = cameraPosition;
            previewCamera.transform.rotation = Quaternion.Euler(cameraRotation);
            previewCamera.fieldOfView = 60f;
            previewCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
        }
    }

    void SpawnSquad()
    {
        int soldierCount = soldierPrefabs.Count;
        if (soldierCount == 0)
        {
            Debug.LogWarning("No soldier prefabs assigned! Assign prefabs in the inspector.");
            return;
        }

        // Calculate starting positions (centered line formation)
        float totalWidth = (soldierCount - 1) * soldierSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < soldierCount; i++)
        {
            if (soldierPrefabs[i] == null)
            {
                Debug.LogWarning($"Soldier prefab at index {i} is null, skipping");
                continue;
            }

            Vector3 spawnPos = new Vector3(
                startX + (i * soldierSpacing),
                0,
                startDistance
            );

            GameObject soldier = Instantiate(soldierPrefabs[i], spawnPos, Quaternion.Euler(0, 180, 0));
            soldier.name = $"Soldier_{i + 1}_{soldierPrefabs[i].name}";
            spawnedSoldiers.Add(soldier);

            // Get Animator
            Animator anim = soldier.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                // Override controller if specified
                if (overrideController != null)
                {
                    anim.runtimeAnimatorController = overrideController;
                }

                soldierAnimators.Add(anim);
                Debug.Log($"Found animator on {soldier.name} with controller: {anim.runtimeAnimatorController?.name ?? "None"}");
            }
            else
            {
                Debug.LogWarning($"No Animator found on {soldier.name}");
            }
        }

        Debug.Log($"Spawned {spawnedSoldiers.Count} soldiers with {soldierAnimators.Count} animators");
    }

    void TrySetWalkAnimation(Animator anim, bool walking)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return;

        // Try to set walk bool parameter
        foreach (string param in walkBoolParams)
        {
            if (HasParameter(anim, param, AnimatorControllerParameterType.Bool))
            {
                anim.SetBool(param, walking);
                break;
            }
        }

        // Try to set speed parameter
        foreach (string param in speedFloatParams)
        {
            if (HasParameter(anim, param, AnimatorControllerParameterType.Float))
            {
                anim.SetFloat(param, walking ? 1f : 0f);
                break;
            }
        }

        // Try to directly play state if parameters don't exist
        string[] statesToTry = walking ? walkStateNames : idleStateNames;
        foreach (string state in statesToTry)
        {
            if (HasState(anim, state))
            {
                anim.CrossFade(state, 0.25f);
                break;
            }
        }
    }

    bool HasParameter(Animator anim, string paramName, AnimatorControllerParameterType type)
    {
        foreach (var param in anim.parameters)
        {
            if (param.name == paramName && param.type == type)
                return true;
        }
        return false;
    }

    bool HasState(Animator anim, string stateName)
    {
        // Check layer 0 for the state
        return anim.HasState(0, Animator.StringToHash(stateName));
    }

    IEnumerator SquadWalkSequence()
    {
        // Give a small delay for initialization
        yield return new WaitForSeconds(0.1f);

        // Trigger walk animation
        foreach (var anim in soldierAnimators)
        {
            TrySetWalkAnimation(anim, true);
        }

        isWalking = true;
        Debug.Log("Squad walking started");

        // Move soldiers forward until they reach stop distance
        while (isWalking)
        {
            bool allArrived = true;

            foreach (var soldier in spawnedSoldiers)
            {
                if (soldier.transform.position.z > stopDistance)
                {
                    soldier.transform.position += Vector3.back * walkSpeed * Time.deltaTime;
                    allArrived = false;
                }
            }

            if (allArrived)
            {
                isWalking = false;
            }

            yield return null;
        }

        // Transition to idle
        foreach (var anim in soldierAnimators)
        {
            TrySetWalkAnimation(anim, false);
        }

        Debug.Log("Squad has arrived and is now idle");
    }

    // Called from UI to restart the sequence
    public void RestartSequence()
    {
        StopAllCoroutines();

        // Reset positions
        int soldierCount = spawnedSoldiers.Count;
        float totalWidth = (soldierCount - 1) * soldierSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < soldierCount; i++)
        {
            spawnedSoldiers[i].transform.position = new Vector3(
                startX + (i * soldierSpacing),
                0,
                startDistance
            );
        }

        StartCoroutine(SquadWalkSequence());
    }

    // Editor helper to list available states
    [ContextMenu("Debug: List Animator Info")]
    void DebugListAnimatorInfo()
    {
        foreach (var anim in soldierAnimators)
        {
            if (anim == null) continue;

            Debug.Log($"=== {anim.gameObject.name} ===");
            Debug.Log($"Controller: {anim.runtimeAnimatorController?.name ?? "None"}");

            if (anim.parameters != null)
            {
                Debug.Log("Parameters:");
                foreach (var p in anim.parameters)
                {
                    Debug.Log($"  - {p.name} ({p.type})");
                }
            }
        }
    }
}
