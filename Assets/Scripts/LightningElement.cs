using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningElement : MonoBehaviour
{
    [SerializeField] MeshRenderer LinkedMesh;

    float StartTime;
    float EndTime;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool SyncToTime(float currentTime)
    {
        if (currentTime < StartTime || currentTime >= EndTime)
            LinkedMesh.enabled = false;
        else
            LinkedMesh.enabled = true;

        return currentTime >= EndTime;
    }

    public void SetTimes(float startTime, float endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }
}
