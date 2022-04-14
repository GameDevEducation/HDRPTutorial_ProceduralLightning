using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LightningConfig
{
    public float StartCellSize = 0.5f;
    public float EndCellSize = 0.2f;
    public float CellOverlap = 0.1f;

    public int MinBranchInterval = 15;
    public int MaxBranchInterval = 30;

    public int MinBranchLength = 30;
    public int MaxBranchLength = 50;
    [Range(0f, 1f)] public float BranchVerticalChance = 0.1f;
    [Range(0f, 1f)] public float BranchDeviationChance = 0.5f;
    public float MinHeightToBranch = 20f;

    public AnimationCurve BranchScaleWithTrunkProgress;
    public int BranchCullLength = 10;

    public float LightningFlashTime = 0.5f;
    public float LightningPersistenceTime = 0.2f;

    public GameObject LightningElementPrefab;
}

public class LightningGenerator : MonoBehaviour
{
    [SerializeField] float LightningHeight = 75f;
    [SerializeField] GameObject LightningBoltPrefab;
    [SerializeField] LightningConfig Config;

    [SerializeField] bool DEBUG_GenerateLightning;
    [SerializeField] Transform DEBUG_LightningTarget;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (DEBUG_GenerateLightning)
        {
            DEBUG_GenerateLightning = false;
            BuildLightning(DEBUG_LightningTarget.position, LightningHeight);
        }
    }

    public void BuildLightning(Vector3 target, float height)
    {
        var lightningGO = Instantiate(LightningBoltPrefab, target, Quaternion.identity);
        var lightningScript = lightningGO.GetComponent<LightningBolt>();

        lightningScript.Build(Config, height);
    }
}
