using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.AI;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{

    public static GameManager instance;
    public const float LERPSPEED = 15f;
    

    // callbacks


    /// <summary>
    /// points, lvl
    /// </summary>
    public static UnityAction<int, int> scoreCallback;

    public static UnityAction<float> gameTimeCallback;
    public static UnityAction<int> pushCallback;

    /// <summary>
    /// success, time
    /// </summary>
    public static UnityAction<bool, float> gameCompleteCallback;


    public static UnityAction onGameLoad;

    // vars


    [Header("Main")]
    [NonReorderable] [SerializeField] private PointsPerType[] pointsPerType; // lists are broken in this version of unity so have to use NonReorderable 
    [SerializeField] private Transform generalCollection; // the would be parent of all spawned objects

    [Header("Map Generation")]
    [SerializeField] private Vector2Int mapSize;
    [SerializeField] private float mapScale;
    [SerializeField] private Transform mapSection;
    [SerializeField] private Transform mapContainer;

    [Header("Cube Spawning")]
    [SerializeField] private MapPresence cube; // prefab
    [SerializeField] private Vector2 cubeSpawnDelayRange; // delay between spawns

    // internal vars

    private int score;
    private int pushCount;

    private float counter;
    private float gameTime;

    private bool hasStarted = false;

    private Vector2Int plrSpawn;
    private Interatable lastPushed;


    private Dictionary<ObjectTypes, int> pointsPerTypeCache = new Dictionary<ObjectTypes, int>();
    private MapPresence[,] activeMap;


    // getters / setters

    public int lvl => Mathf.FloorToInt(score / 100);

    // ----------- unity funcs



    private void Awake()
    {
        if (instance)
            Destroy(gameObject);

        instance = this;
    }

    public void StartGame()
    {
        if (SaveManager.ReadData(out SaveDataContainer dat))
        {
            mapSize = dat.mapSize;
            gameTime = dat.totalTime;
            score = dat.pointCount;
            pushCount = dat.pushCount;

            GenerateMap();

            // respawn objects in saved locations
            foreach(SavedData_SavedPresence p in dat.savedMap)
            {
                switch ((ObjectTypes)p.type)
                {
                    case ObjectTypes.Player:
                        plrSpawn = p.pos;
                        break;

                    case ObjectTypes.Cube:
                        if (p.hasPlayerOn) plrSpawn = p.pos;
                        SpawnCube(p.pos);
                        break;

                    default:
                        SpawnObject(GetDataForType((ObjectTypes)p.type), p.pos);
                        break;
                }
            }

            counter = Random.Range(cubeSpawnDelayRange.x, cubeSpawnDelayRange.y);
        }
        else
        {
            score = 0;
            gameTime = 0;
            pushCount = 0;

            GenerateMap();
            SpawnObjects();

            plrSpawn = GetRandomEmptySection();
        }

        // recall callbacks to reset ui

        scoreCallback?.Invoke(score, lvl);
        pushCallback?.Invoke(pushCount);
        gameTimeCallback?.Invoke(gameTime);


        // load rest of dependencies

        onGameLoad?.Invoke();
        onGameLoad = null;

        hasStarted = true;
    }


    private void Update()
    {
        if (!hasStarted) return;

        gameTime += Time.deltaTime;
        gameTimeCallback?.Invoke(gameTime);

        counter -= Time.deltaTime;

        if (counter <= 0)
        {
            counter = Random.Range(cubeSpawnDelayRange.x, cubeSpawnDelayRange.y);

            SpawnCube(GetRandomEmptySection());
            CheckForFail();
        }

        if (Input.GetKey(KeyCode.Space)) //////// ----------------------------------------------- press to save game
            SaveGame();
    }





    // ------------------------- my funcs

    private void GenerateMap()
    {
        activeMap = new MapPresence[mapSize.x, mapSize.y];

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.x; y++)
            {
                Transform obj = Instantiate(mapSection);
                obj.SetParent(mapContainer);
                obj.localPosition = GetLocalPosByIndex(x, y);

                activeMap[x, y] = null;
            }
        }
    }


    public Vector2Int GetPlayerSpawn(PlayerController plr)
    {
        if(activeMap[plrSpawn.x, plrSpawn.y])
        {
            activeMap[plrSpawn.x, plrSpawn.y].SetHarbouringPlayer(plr); // for when loading, registers the plr on that tile
        }
        else
            activeMap[plrSpawn.x, plrSpawn.y] = plr;

        return plrSpawn;
    }


    private void SpawnObjects(Vector2Int[] exclude = null)
    {
        foreach (PointsPerType obj in pointsPerType)
            SpawnObject(obj, GetRandomEmptySection(exclude, true));
    }


    private void SpawnObject(PointsPerType obj, Vector2Int spawnAt)
    {
        if (spawnAt != Vector2Int.one * -1)
        {
            MapPresence o = MapPresence.Instantiate(obj.obj);
            o.transform.SetParent(generalCollection);
            o.transform.position = GetWorldPosByLoc(spawnAt);

            o.MoveTo(spawnAt);
            activeMap[spawnAt.x, spawnAt.y] = o;
        }
        else
        {
            // map is saturated
            EndGame(false);
        }
    }


    private void SpawnCube(Vector2Int pos)
    {
        if (pos != Vector2.one * -1)
        {
            MapPresence obj = MapPresence.Instantiate(cube);
            obj.transform.SetParent(generalCollection);
            obj.transform.position = GetWorldPosByLoc(pos);

            activeMap[pos.x, pos.y] = obj;
            obj.MoveTo(pos);
        }
        else
        {
            // map is saturated
            EndGame(false);
        }
    }


    public Vector2Int GetRandomEmptySection(Vector2Int[] exclude = null, bool skipEdges = false)
    {
        List<Vector2Int> acceptableLocals = new List<Vector2Int>();

        int skipEdgesNum = skipEdges ? 1 : 0;

        for (int x = skipEdgesNum; x < activeMap.GetLength(0) - skipEdgesNum; x++)
        {
            for (int y = skipEdgesNum; y < activeMap.GetLength(1) - skipEdgesNum; y++)
            {
                // have to have this because when pushing into complete stack, the player isnt updated yet
                // so objects can spawn on new pos and break it
                if (exclude != null && exclude.Contains(new Vector2Int(x, y))) continue;

                if (activeMap[x, y] == null)
                    acceptableLocals.Add(new Vector2Int(x, y));
            }
        }

        return acceptableLocals.Count > 0 ? acceptableLocals[Random.Range(0, acceptableLocals.Count)] : Vector2Int.one * -1;
    }


    public Vector3 GetWorldPosByLoc(Vector2Int at) => mapContainer.TransformPoint(GetLocalPosByIndex(at.x, at.y));
    private Vector3 GetLocalPosByIndex(int x, int y) => new Vector3(x, 0, y) * mapScale;



    private PointsPerType GetDataForType(Interatable interatable) => GetDataForType(interatable.getType);
    private PointsPerType GetDataForType(ObjectTypes type)
    {
        // check if entry for this type exists 
        if (!pointsPerTypeCache.ContainsKey(type))
        {
            int toAdd = -1;

            for (int i = 0; i < pointsPerType.Length; i++)
            {
                if (pointsPerType[i].type == type)
                {
                    toAdd = i;
                    break;
                }
            }

            // if no data for the type exit this and add nothing points wise
            if (toAdd == -1)
            {
                Debug.Log($"No data for type {type} exists");
                return new PointsPerType();
            }

            // add to cache
            pointsPerTypeCache.Add(type, toAdd);
        }

        // add score based on the lvl
        return pointsPerType[pointsPerTypeCache[type]];
    }




    public bool TryMovePlayer(Vector2Int from, Vector2Int change)
    {
        if (!hasStarted) return false;

        Vector2Int to = from + change;

        if (IsInRange(to))
        {
            // after implementing the ability to pass through the cube i reread the document and still no sure if the player has the ability to do this
            // however removing replacing the comment from the if statement adds that ability

            if (activeMap[to.x, to.y] == null) 
            //if (activeMap[to.x, to.y] == null || activeMap[to.x, to.y] is not Interatable)
            {
                return true;
            }
            else if(activeMap[to.x, to.y] is Interatable)
            {
                Vector2Int pushTo = to + change;

                if (IsInRange(pushTo))
                {
                    MapPresence pushingInto = activeMap[pushTo.x, pushTo.y];

                    // if trying to push into cube dont allows
                    if (pushingInto && pushingInto is not Interatable)
                        return false;

                    // change order in map for new elements
                    Interatable temp = activeMap[to.x, to.y] as Interatable;
                    SwapPos(to, pushTo);
                    temp.MoveTo(pushTo);

                    // add score, register last touched as well
                    AddToScore(temp);

                    pushCount++;
                    pushCallback?.Invoke(pushCount);

                    // handle on touch with other obj
                    if(pushingInto is Interatable && (pushingInto as Interatable).getType != temp.getType)
                    {
                        // if pushed into other obj then clear both and reset 
                        activeMap[temp.getPos.x, temp.getPos.y] = null;
                        Destroy(temp.gameObject);

                        activeMap[pushingInto.getPos.x, pushingInto.getPos.y] = null;
                        Destroy(pushingInto.gameObject);

                        SpawnObjects(new Vector2Int[] { new Vector2Int(to.x, to.y) });
                    }

                    return true;
                }
            }
        }



        return false;
    }


    public void SwapPlayer(PlayerController plr, Vector2Int from, Vector2Int to)
    {
        MapPresence lastTile = activeMap[from.x, from.y];

        if (lastTile && lastTile is not PlayerController) // if last tile was not the player, make sure its not harbouring anyone
        {
            activeMap[from.x, from.y].SetHarbouringPlayer(null);
        }
        else // else clear tile
        {
            activeMap[from.x, from.y] = null;
        }


        // not update next section
        if (activeMap[to.x, to.y] && activeMap[to.x, to.y] is not Interatable) // is next a default tile
        {
            activeMap[to.x, to.y].SetHarbouringPlayer(plr);
            return;
        }


        // always set new pos as plrs
        activeMap[to.x, to.y] = plr;
    }


    private void SwapPos(Vector2Int from, Vector2Int to)
    {
        // swap position of objects
        MapPresence temp = activeMap[from.x, from.y];
        activeMap[to.x, to.y] = temp;
        activeMap[from.x, from.y] = null;
    }


    private bool IsInRange(Vector2Int pos)
    {
        if (pos.x >= 0 && pos.x < activeMap.GetLength(0))
        {
            if (pos.y >= 0 && pos.y < activeMap.GetLength(1))
            {
                return true;
            }
        }

        return false;
    }


    public void AddToScore(Interatable interatable)
    {
        int change = GetDataForType(interatable).pointsByLvl[Mathf.Min(2, lvl)];

        if (lastPushed && lastPushed == interatable)
        {
            // not sure what "one by one the score decreases on double value." means but just removing 1 point
            change -= 1;
        }

        lastPushed = interatable;
        score += change;

        // callback for ui
        scoreCallback?.Invoke(score, lvl);

        // completed
        if (score >= 400)
            EndGame(true);
    }


    private void CheckForFail()
    {
        // checks the surroundings of plr and objects, if no escape then its a loss

        for (int x = 0; x < activeMap.GetLength(0); x++)
        {
            for (int y = 0; y < activeMap.GetLength(1); y++)
            {
                if(activeMap[x, y])
                {
                    if (activeMap[x, y].getType == ObjectTypes.Cube && !activeMap[x, y].getHarbouringPlayer)
                        continue;

                    List<MapPresence> surr = GetSurroundings(new Vector2Int(x, y), out int stuckCounter);

                    foreach (MapPresence m in surr)
                        if (m && m is not Interatable)
                            stuckCounter++;

                    if (stuckCounter == 8)
                    {
                        EndGame(false);
                        break;
                    }
                }
            }
        }
    }


    private List<MapPresence> GetSurroundings(Vector2Int from, out int outOfRangeCount)
    {
        List<MapPresence> toReturn = new List<MapPresence>();
        outOfRangeCount = 0;

        // search in 3x3 around player to see if theyre stuck
        for (int w = -1; w <= 1; w++)
        {
            for (int r = -1; r <= 1; r++)
            {
                if (w == 0 && r == 0) continue; // dont check where player is

                if (IsInRange(new Vector2Int(from.x + w, from.y + r)))
                {
                    toReturn.Add(activeMap[from.x + w, from.y + r]);
                }
                else
                    outOfRangeCount++;
            }
        }

        return toReturn;
    }



    private void EndGame(bool isSuccess)
    {
        gameCompleteCallback?.Invoke(isSuccess, gameTime);
        hasStarted = false; // stops timer and spawning
    }

    private void SaveGame()
    {
        SaveManager.WriteSave(new SaveDataContainer(activeMap, gameTime, pushCount, score));
    }
}

[System.Serializable]
public struct PointsPerType
{
    [Header("General")]
    [SerializeField] private string inspectorIdentity;
    public ObjectTypes type;
    public MapPresence obj;

    [Header("Data")]
    public int[] pointsByLvl;
}


public enum ObjectTypes
{
    // universal types ( for loading only )

    Cube,
    Player,

    // specific types

    Sphere,
    Capsule,
}