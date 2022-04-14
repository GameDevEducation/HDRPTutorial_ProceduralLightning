using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EBranchDirection
{
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest,

    NumValues
}

public class LightningCell
{
    public Vector3 Position;
    public bool IsTrunk;
    public float CellSize;
    public EBranchDirection Direction;
    public int BranchLifeRemaining;
}

public class LightningSlice
{
    public List<LightningCell> Cells = new List<LightningCell>();

    public void AddCell(LightningCell cell)
    {
        Cells.Add(cell);
    }

    EBranchDirection GetRandomDirection()
    {
        return (EBranchDirection)Random.Range(0, (int)EBranchDirection.NumValues);
    }

    EBranchDirection GetRandomAdjacentDirection(EBranchDirection direction)
    {
        int newDirection = (int)direction + Random.Range(-1, 2);
        newDirection = (newDirection + (int)EBranchDirection.NumValues) % (int)EBranchDirection.NumValues;

        return (EBranchDirection)newDirection;
    }

    Vector3 DirectionToVector(EBranchDirection direction)
    {
        switch(direction)
        {
            case EBranchDirection.North: return Vector3.forward;
            case EBranchDirection.NorthEast: return Vector3.forward + Vector3.right;
            case EBranchDirection.East: return Vector3.right;
            case EBranchDirection.SouthEast: return -Vector3.forward + Vector3.right;
            case EBranchDirection.South: return -Vector3.forward;
            case EBranchDirection.SouthWest: return -Vector3.forward - Vector3.right;
            case EBranchDirection.West: return -Vector3.right;
            case EBranchDirection.NorthWest: return Vector3.forward - Vector3.right;
        }

        return Vector3.zero;
    }

    public LightningSlice Grow(LightningConfig config, float trunkProgress, bool performBranch)
    {
        LightningSlice newSlice = null;

        // traverse all of the cells
        foreach(LightningCell cell in Cells)
        {
            float nextCellSize = Mathf.Lerp(config.StartCellSize, config.EndCellSize, trunkProgress);
            float offset = (cell.CellSize + nextCellSize) * (0.5f - config.CellOverlap);

            if (cell.IsTrunk)
            {
                Grow_Trunk(config, cell, offset, nextCellSize, performBranch, trunkProgress, ref newSlice);
            }
            else
            {
                Grow_Branch(config, cell, offset, nextCellSize, ref newSlice);
            }
        }

        return newSlice;
    }

    void Grow_Branch(LightningConfig config, LightningCell cell, float offset, float nextCellSize,
                     ref LightningSlice newSlice)
    {
        // out of life
        if (cell.BranchLifeRemaining == 0)
            return;

        // need a new slice?
        if (newSlice == null)
            newSlice = new LightningSlice();

        // determine the direction
        bool changeDirection = Random.Range(0f, 1f) < config.BranchDeviationChance;
        var newDirection = changeDirection ? GetRandomAdjacentDirection(cell.Direction) : cell.Direction;
        var directionVector = DirectionToVector(newDirection);

        // handle vertical deviation
        if (Random.Range(0f, 1f) < config.BranchVerticalChance)
            directionVector.y = Random.Range(-1, 2);

        Vector3 nextCellPosition = cell.Position + directionVector * offset;

        // add the new cell
        newSlice.AddCell(new LightningCell()
        {
            Position = nextCellPosition,
            IsTrunk = false,
            CellSize = nextCellSize,
            Direction = newDirection,
            BranchLifeRemaining = cell.BranchLifeRemaining - 1
        });
    }

    void Grow_Trunk(LightningConfig config, LightningCell cell, float offset, float nextCellSize,
                    bool performBranch, float trunkProgress, ref LightningSlice newSlice)
    {
        // calculate new position
        Vector3 nextCellPosition = cell.Position;
        nextCellPosition += offset * Vector3.down; // trunk is always lower
        nextCellPosition += offset * Vector3.forward * Random.Range(-1, 2);
        nextCellPosition += offset * Vector3.right * Random.Range(-1, 2);

        // is the new position out of range?
        if (nextCellPosition.y < 0)
            return;

        // need a new slice?
        if (newSlice == null)
            newSlice = new LightningSlice();

        // add the new trunk cell
        newSlice.AddCell(new LightningCell()
        {
            Position = nextCellPosition,
            IsTrunk = true,
            CellSize = nextCellSize
        });

        // should branch?
        if (performBranch)
        {
            var direction = GetRandomDirection();
            var directionVector = DirectionToVector(direction);

            nextCellPosition = cell.Position + directionVector * offset;

            if (nextCellPosition.y < config.MinHeightToBranch)
                return;

            // determine the branch length
            int branchLength = Random.Range(config.MinBranchLength, config.MaxBranchLength);
            branchLength = Mathf.RoundToInt(branchLength * config.BranchScaleWithTrunkProgress.Evaluate(trunkProgress));

            if (branchLength < config.BranchCullLength)
                return;

            // add the new cell
            newSlice.AddCell(new LightningCell()
            {
                Position = nextCellPosition,
                IsTrunk = false,
                CellSize = nextCellSize,
                Direction = direction,
                BranchLifeRemaining = branchLength
            });
        }
    }
}

public class LightningBolt : MonoBehaviour
{
    List<LightningElement> Elements = new List<LightningElement>();
    float ElapsedTime = 0f;

    public void Build(LightningConfig config, float height)
    {
        List<LightningSlice> slices = new List<LightningSlice>();
        Vector3 startPoint = Vector3.up * height;

        // setup the first slice
        var startingSlice = new LightningSlice();
        startingSlice.AddCell(new LightningCell() { Position = startPoint, 
                                                    IsTrunk = true, 
                                                    CellSize = config.StartCellSize });
        slices.Add(startingSlice);

        int maxTrunkSlices = Mathf.RoundToInt(height / config.EndCellSize);
        int branchCountdown = Random.Range(config.MinBranchInterval, config.MaxBranchInterval);

        // grow the lightning bolt
        bool boltGrew = true;
        while (boltGrew)
        {
            boltGrew = false;

            float trunkProgress = (float)slices.Count / maxTrunkSlices;

            // update branching
            --branchCountdown;
            bool performBranch = false;
            if (branchCountdown == 0)
            {
                performBranch = true;
                branchCountdown = Random.Range(config.MinBranchInterval, config.MaxBranchInterval);
            }

            var newSlice = slices[^1].Grow(config, trunkProgress, performBranch);

            // added a new slice?
            if (newSlice != null)
            {
                slices.Add(newSlice);
                boltGrew = true;
            }
        }

        // recentre the lightning bolt
        RecentreLightningBolt(slices);

        // spawn the lightning bolt
        SpawnLightningBolt(slices, config);
    }

    void RecentreLightningBolt(List<LightningSlice> slices)
    {
        // find the actual end point
        Vector3 actualEnd = Vector3.zero;
        bool endFound = false;
        for (int index = slices.Count - 1; index >= 0; index--)
        {
            var slice = slices[index];

            foreach(var cell in slice.Cells)
            {
                if (cell.IsTrunk)
                {
                    actualEnd = cell.Position;
                    endFound = true;
                    break;
                }
            }

            if (endFound)
                break;
        }

        // move all of the cells
        foreach(var slice in slices)
        {
            foreach(var cell in slice.Cells)
            {
                cell.Position -= actualEnd;
            }
        }
    }

    void SpawnLightningBolt(List<LightningSlice> slices, LightningConfig config)
    {
        // spawn and configure every element
        float startTime = 0f;
        float timePerSlice = config.LightningFlashTime / slices.Count;
        foreach (var slice in slices)
        {
            foreach (var cell in slice.Cells)
            {
                // spawn the game object
                var elementGO = Instantiate(config.LightningElementPrefab, transform);
                elementGO.transform.localPosition = cell.Position;
                elementGO.transform.localScale = Vector3.one * cell.CellSize;

                // configure the element
                var elementScript = elementGO.GetComponent<LightningElement>();
                Elements.Add(elementScript);
                elementScript.SetTimes(startTime, startTime + config.LightningPersistenceTime);
            }

            startTime += timePerSlice;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // update all elements
        bool allFinished = true;
        foreach (var element in Elements)
            allFinished &= element.SyncToTime(ElapsedTime);

        ElapsedTime += Time.deltaTime;

        // cleanup once done
        if (allFinished)
            Destroy(gameObject);
    }
}
