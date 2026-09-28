using UnityEngine;
using NaughtyAttributes;

public class TownManager : MonoBehaviour
{

    [SerializeField] private Building_Town townBuilding;
    //WAVES
    [Header("Wave System")]
    [SerializeField] private int CurrentWave;
    [Tooltip("Seconds between waves. A new wave spawns even if the previous one is still alive.")]
    [SerializeField] private float nextWaveTime = 40f;
    [SerializeField] private float currentWaveTime = 0f;
    [Tooltip("Spawn wave 1 on Start instead of waiting for the first timer to expire.")]
    [SerializeField] private bool spawnFirstWaveOnStart = true;

    [Header("Wave Size")]
    [SerializeField] private int enemiesPerWave = 3;
    [Tooltip("Extra enemies added to each wave after the first.")]
    [SerializeField] private int extraEnemiesPerWave = 1;
    [Tooltip("Ceiling on how many enemies a single wave may request.")]
    [SerializeField] private int maxEnemiesPerWave = 10;

    //ENEMY POOL
    [Header("Enemy Pool")]
    [SerializeField] private EnemyPool enemyPool;

    //CURRENCY (PLACEHOLDER, MOVE TO A CURRENCY MANAGER LATER)
    [Header("Currency")]
    [SerializeField] private int startingCoins = 100;
    public static int Coins;
    public static event System.Action<int> OnCoinsChanged;

    private bool gameOver;


    /// <summary>
    /// LATER IMPLEMENT WAVE SYSTEM INTO A WAVE MANAGER, THIS IS ONLY FOR TOWN TESTING
    
    private void Awake()
    {
        //STATICS SURVIVE BETWEEN PLAY SESSIONS WHEN DOMAIN RELOAD IS OFF, SO RESET HERE
        Coins = startingCoins;
        OnCoinsChanged?.Invoke(Coins);
    }

    private void Start()
    {

        //SET THE CURRENT WAVE TO 0
        CurrentWave = 0;
        currentWaveTime = 0f;

        //FIND THE TOWN IF NOT SET
        if (townBuilding == null) 
        {
            townBuilding = Object.FindFirstObjectByType<Building_Town>();
        }

        //FIND THE POOL IF NOT SET
        if (enemyPool == null)
        {
            enemyPool = Object.FindFirstObjectByType<EnemyPool>();
        }

        //SUBSCRIBE TO EVENTS
        subscribeToEvents(true);

        if (spawnFirstWaveOnStart)
        {
            SpawnWave();
        }
    }

    private void OnDestroy()
    {
        subscribeToEvents(false);
    }

    private void Update()
    {
        if (gameOver || enemyPool == null) return;

        //WAVES RUN ON A TIMER, INDEPENDENT OF HOW MANY ENEMIES ARE STILL ALIVE
        currentWaveTime += Time.deltaTime;
        if (currentWaveTime >= nextWaveTime)
        {
            currentWaveTime = 0f;
            SpawnWave();
        }
    }

    [Button]
    private void SpawnWave()
    {
        if (enemyPool == null)
        {
            Debug.LogError("Town Manager: no EnemyPool assigned, cannot spawn a wave.", this);
            return;
        }

        CurrentWave++;

        int requested = Mathf.Min(enemiesPerWave + (CurrentWave - 1) * extraEnemiesPerWave, maxEnemiesPerWave);
        int spawned = enemyPool.SpawnMany(requested);

        if (spawned < requested)
        {
            Debug.LogWarning($"Town Manager: wave {CurrentWave} wanted {requested} enemies but only spawned {spawned} (pool max reached or no valid spawn point).", this);
        }
        else
        {
            Debug.Log($"Town Manager: wave {CurrentWave} spawned {spawned} enemies. Active: {enemyPool.ActiveCount}");
        }
    }



    public static bool TrySpendCoins(int amount)
    {
        if (amount <= 0 || Coins < amount) return false;

        Coins -= amount;
        OnCoinsChanged?.Invoke(Coins);
        return true;
    }

    public static void AddCoins(int amount)
    {
        if (amount <= 0) return;

        Coins += amount;
        OnCoinsChanged?.Invoke(Coins);
    }

    [Button("Add 100 Coins")]
    private void DebugAddCoins()
    {
        AddCoins(100);
    }

    private void subscribeToEvents(bool subscribe) 
    {
        if (townBuilding == null) return;

        if (subscribe)
        {
            townBuilding.onDamageTaken += OnDamageTaken;
            townBuilding.onDied += OnDied;
        }
        else
        {
            townBuilding.onDamageTaken -= OnDamageTaken;
            townBuilding.onDied -= OnDied;
        }
    }


    private void OnDamageTaken(float damage) 
    {
        Debug.LogWarning($"Town Manager: Town Building took {damage} damage");
    }

    private void OnDied() 
    {
        gameOver = true;
        Debug.Log("Town Manager: Town Building died, game over");
    }

    

}
