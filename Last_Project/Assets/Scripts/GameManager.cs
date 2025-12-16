using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameFlowState
    {
        NotStarted,
        Playing,
        GameOver
    }

    public enum GameOverReason
    {
        None,
        PlayerDied,
        AllEnemiesDefeated
    }

    [Header("Refs")]
    public Damageable player;
    public PlayerInput playerInput;

    [Header("Enemy Root")]
    public Transform enemiesRoot;
    public List<Damageable> enemies = new List<Damageable>();

    [Header("Enemy HP Bar (World)")]
    public HpBarView enemyHpBarPrefab;
    public Transform worldUiRoot;

    [Header("Player HP Bar (HUD)")]
    public PlayerHpHudView playerHpHudPrefab;
    public Transform hudRoot;

    [Header("Start UI")]
    public GameObject startCanvas;
    public bool startAsNotStarted = true;

    [Header("GameOver UI")]
    public GameObject gameOverCanvas;
    public UnityEngine.UI.Text gameOverMessageText;
    public UnityEngine.UI.Button restartButton;

    private GameOverReason gameOverReason = GameOverReason.None;
    public GameFlowState FlowState { get; private set; }

    public bool IsGameStarted => FlowState == GameFlowState.Playing;
    public bool IsGameOver => FlowState == GameFlowState.GameOver;

    [Header("Camera")]
    public ThirdPersonCamera thirdPersonCamera;

    private readonly Dictionary<Damageable, HpBarView> enemyHpBars = new Dictionary<Damageable, HpBarView>();
    private PlayerHpHudView playerHudInstance;

    private readonly Dictionary<Damageable, Vector3> spawnPos = new Dictionary<Damageable, Vector3>();
    private readonly Dictionary<Damageable, Quaternion> spawnRot = new Dictionary<Damageable, Quaternion>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        if (player != null)
            RegisterPlayer(player);

        if (playerInput == null && player != null)
            playerInput = player.GetComponent<PlayerInput>();

        if (thirdPersonCamera == null)
            thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();

        if (thirdPersonCamera != null)
            thirdPersonCamera.CacheInitialView();

        RefreshEnemiesFromRoot();
        CacheAllSpawnsFromCurrentScene();

        SetFlowState(startAsNotStarted ? GameFlowState.NotStarted : GameFlowState.Playing);

        if (IsGameStarted)
        {
            if (player != null)
                EnsurePlayerHud(player);

            for (int i = 0; i < enemies.Count; ++i)
            {
                var e = enemies[i];
                if (e != null)
                    EnsureEnemyHpBar(e);
            }

            gameOverReason = GameOverReason.None;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshEnemiesFromRoot();

        spawnPos.Clear();
        spawnRot.Clear();
        CacheAllSpawnsFromCurrentScene();

        ApplyUiState();
    }

    public void StartGame()
    {
        StartCoroutine(CoStartGame());
    }

    IEnumerator CoStartGame()
    {
        yield return null;

        gameOverReason = GameOverReason.None;
        SetFlowState(GameFlowState.Playing);

        if (player != null)
            EnsurePlayerHud(player);

        for (int i = 0; i < enemies.Count; ++i)
        {
            var e = enemies[i];
            if (e != null)
                EnsureEnemyHpBar(e);
        }
    }

    public void GameOver(GameOverReason reason)
    {
        if (FlowState != GameFlowState.Playing) return;

        gameOverReason = reason;
        SetFlowState(GameFlowState.GameOver);
    }

    public void RestartFromGameOver()
    {
        if (FlowState != GameFlowState.GameOver) return;

        gameOverReason = GameOverReason.None;

        ResetAllToSceneSpawn_Internal();

        // 플레이어 리바이브 호출
        if (player != null)
        {
            var playerCC = player.GetComponent<CC_Control>();
            if (playerCC != null)
                playerCC.Revive(); // 플레이어 리바이브
        }

        SetFlowState(GameFlowState.Playing);

        if (player != null)
            EnsurePlayerHud(player);

        for (int i = 0; i < enemies.Count; ++i)
        {
            var e = enemies[i];
            if (e != null)
                EnsureEnemyHpBar(e);
        }
    }

    public void ResetAllToSceneSpawn()
    {
        gameOverReason = GameOverReason.None;

        ResetAllToSceneSpawn_Internal();
        SetFlowState(GameFlowState.NotStarted);
    }

    void ResetAllToSceneSpawn_Internal()
    {
        if (player != null)
            ResetOneToSpawn(player);

        for (int i = 0; i < enemies.Count; ++i)
        {
            var e = enemies[i];
            if (e != null)
                ResetOneToSpawn(e);
        }

        if (thirdPersonCamera != null)
            thirdPersonCamera.ResetViewToInitial();
    }

    void SetFlowState(GameFlowState state)
    {
        FlowState = state;
        ApplyUiState();
    }

    void ApplyUiState()
    {
        bool playing = FlowState == GameFlowState.Playing;
        bool notStarted = FlowState == GameFlowState.NotStarted;
        bool gameOver = FlowState == GameFlowState.GameOver;

        if (startCanvas != null)
            startCanvas.SetActive(notStarted);

        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(gameOver);

        if (restartButton != null)
            restartButton.gameObject.SetActive(gameOver);

        if (gameOver && gameOverMessageText != null)
        {
            if (gameOverReason == GameOverReason.PlayerDied)
                gameOverMessageText.text = "You Died...";
            else if (gameOverReason == GameOverReason.AllEnemiesDefeated)
                gameOverMessageText.text = "Victory!";
            else
                gameOverMessageText.text = "Game Over";
        }

        if (hudRoot != null)
            hudRoot.gameObject.SetActive(playing);

        if (worldUiRoot != null)
            worldUiRoot.gameObject.SetActive(playing);

        if (playerInput != null)
            playerInput.enabled = playing;

        Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !playing;
    }

    public void RegisterPlayer(Damageable d)
    {
        if (d == null) return;

        player = d;
        CacheSpawnIfNeeded(d);

        d.OnDied -= HandlePlayerDied;
        d.OnDied += HandlePlayerDied;

        if (IsGameStarted)
            EnsurePlayerHud(d);
    }

    void HandlePlayerDied(Damageable d)
    {
        if (FlowState != GameFlowState.Playing) return;

        if (AreAllEnemiesDead())
        {
            // 모든 적이 죽었다면, 게임을 승리로 처리
            GameOver(GameOverReason.AllEnemiesDefeated);
            SetPlayerToDanceState();
        }
        else
        {
            // 적이 살아있다면, 게임 오버로 처리하고 살아있는 적들을 Dance로 전환
            GameOver(GameOverReason.PlayerDied);
            SetEnemiesToDanceState();
        }
    }

    void SetEnemiesToDanceState()
    {
        foreach (var enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead) // 살아있는 적들만
            {
                var animator = enemy.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetTrigger("Dance");  // "Dance" 애니메이션 트리거
                }
            }
        }
    }

    public void RefreshEnemiesFromRoot()
    {
        if (enemiesRoot == null) return;

        var found = enemiesRoot.GetComponentsInChildren<Damageable>(true);

        for (int i = enemies.Count - 1; i >= 0; --i)
        {
            var e = enemies[i];
            if (e == null)
            {
                enemies.RemoveAt(i);
                continue;
            }

            bool stillExistsUnderRoot = false;
            for (int j = 0; j < found.Length; ++j)
            {
                if (found[j] == e) { stillExistsUnderRoot = true; break; }
            }

            if (!stillExistsUnderRoot)
                UnregisterEnemy(e);
        }

        for (int i = 0; i < found.Length; ++i)
        {
            var e = found[i];
            if (e == null) continue;
            if (e == player) continue;

            RegisterEnemy(e);
        }
    }

    void RegisterEnemy(Damageable d)
    {
        if (d == null) return;
        if (enemies.Contains(d)) return;

        enemies.Add(d);
        CacheSpawnIfNeeded(d);

        if (IsGameStarted)
            EnsureEnemyHpBar(d);

        d.OnDied -= HandleEnemyDied;
        d.OnDied += HandleEnemyDied;
    }

    void UnregisterEnemy(Damageable d)
    {
        if (d == null) return;

        enemies.Remove(d);
        d.OnDied -= HandleEnemyDied;

        RemoveEnemyHpBar(d);
    }

    void HandleEnemyDied(Damageable d)
    {
        if (FlowState != GameFlowState.Playing) return;

        if (AreAllEnemiesDead())
        {
            GameOver(GameOverReason.AllEnemiesDefeated);
            SetPlayerToDanceState();
        }
    }

    bool AreAllEnemiesDead()
    {
        for (int i = 0; i < enemies.Count; ++i)
        {
            var e = enemies[i];
            if (e == null) continue;
            if (!e.IsDead) return false;
        }
        return true;
    }
    void SetPlayerToDanceState()
    {
        if (player != null && player.GetComponent<Animator>() != null)
        {
            var animator = player.GetComponent<Animator>();
            animator.SetTrigger("Dance");  // Assuming the "Dance" animation trigger is set up in the Animator
        }
    }

    void EnsureEnemyHpBar(Damageable d)
    {
        if (!IsGameStarted) return;
        if (d == null || enemyHpBarPrefab == null) return;

        if (enemyHpBars.TryGetValue(d, out var existing))
        {
            if (existing != null) return;
            enemyHpBars.Remove(d);
        }

        Transform root = worldUiRoot != null ? worldUiRoot : null;

        HpBarView bar = Instantiate(enemyHpBarPrefab, root);
        bar.Bind(d, d.transform);

        enemyHpBars.Add(d, bar);
    }

    void RemoveEnemyHpBar(Damageable d)
    {
        if (!enemyHpBars.TryGetValue(d, out var bar)) return;
        enemyHpBars.Remove(d);

        if (bar != null)
            Destroy(bar.gameObject);
    }

    void EnsurePlayerHud(Damageable d)
    {
        if (!IsGameStarted) return;
        if (d == null || playerHpHudPrefab == null) return;

        if (playerHudInstance == null)
        {
            Transform root = hudRoot != null ? hudRoot : null;
            playerHudInstance = Instantiate(playerHpHudPrefab, root);
        }

        playerHudInstance.Bind(d);
    }

    void CacheSpawnIfNeeded(Damageable d)
    {
        if (d == null) return;

        if (!spawnPos.ContainsKey(d))
        {
            spawnPos[d] = d.transform.position;
            spawnRot[d] = d.transform.rotation;
        }
    }

    void CacheAllSpawnsFromCurrentScene()
    {
        if (player != null)
            CacheSpawnIfNeeded(player);

        for (int i = 0; i < enemies.Count; ++i)
        {
            if (enemies[i] != null)
                CacheSpawnIfNeeded(enemies[i]);
        }
    }

    void ResetOneToSpawn(Damageable d)
    {
        if (d == null) return;

        var cc = d.GetComponent<CharacterController>();
        bool ccWasEnabled = false;

        if (cc != null && cc.enabled)
        {
            ccWasEnabled = true;
            cc.enabled = false;
        }

        if (spawnPos.TryGetValue(d, out var p))
            d.transform.position = p;
        if (spawnRot.TryGetValue(d, out var r))
            d.transform.rotation = r;

        if (ccWasEnabled)
            cc.enabled = true;

        d.ResetHp();

        // 플레이어 리바이브 호출 (플레이어에 대해서만)
        if (d == player)
        {
            var playerCC = d.GetComponent<CC_Control>();
            if (playerCC != null)
                playerCC.Revive(); // 플레이어 리바이브
        }

        var navAi = d.GetComponent<CC_NavAI>();
        if (navAi != null)
            navAi.Revive();
    }
}
