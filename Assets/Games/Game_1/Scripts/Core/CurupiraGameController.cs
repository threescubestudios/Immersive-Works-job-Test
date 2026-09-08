using System.Collections;
using UnityEngine;
using ImmersiWorks.Platform.Core;
using ImmersiWorks.Platform.Runtime;
using SaveCurupira.UI;

namespace SaveCurupira.Core
{
    public class CurupiraGameController : GameBase
    {
        public static CurupiraGameController Instance { get; private set; }

        public override string GameId =>
            "game_save_curupira";

        public override string DisplayName =>
            "Save Curupira - Fruit Strike";

        public override float NominalDurationSeconds =>
            180f;

        public SaveCurupiraDescriptor gameDescriptor;

        [Header("Gameplay Roots")]
        public Gameplay.RootBehaviour[] gameplayRoots;

        [SerializeField] private int rootHealth = 3;
        public enum GameState
        {
            Intro,
            Tutorial,
            Playing,
            End
        }

        public GameState CurrentState { get; private set; }

        private const float GAME_DURATION = 180f;

        private float gameTimer;

        private int rootsDestroyed;
        private int totalRootsToDestroy = 5;

        private bool hasSubscribedSensors;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private IEnumerator TutorialRoutine()
        {
            CurrentState = GameState.Tutorial;

            SubscribeToSensors();

            Gameplay.RootBehaviour[] allRoots =
                FindObjectsOfType<Gameplay.RootBehaviour>(true);

            Gameplay.RootBehaviour tutorialRoot = null;

            float minX = float.MaxValue;

            foreach (var root in allRoots)
            {
                root.gameObject.SetActive(true);

                float absX =
                    Mathf.Abs(root.transform.position.x);

                if (absX < minX)
                {
                    minX = absX;
                    tutorialRoot = root;
                }
            }

            if (tutorialRoot != null)
            {
                tutorialRoot.gameObject.SetActive(true);
                tutorialRoot.Initialize(1);
            }

            ShowMessage(
                "Swipe the fruit into the root!"
            );

            FruitSpawnManager spawner =
                FindObjectOfType<FruitSpawnManager>();

            if (spawner != null)
                spawner.SpawnSingleFruit();

            while (tutorialRoot != null &&
                   tutorialRoot.gameObject.activeInHierarchy &&
                   tutorialRoot.health > 0)
            {
                yield return new WaitForSeconds(0.2f);

                if (CurrentState != GameState.Tutorial)
                    yield break;
            }

            ShowMessage("Great Job!");

            yield return new WaitForSeconds(2f);
        }

        protected override IEnumerator OnIntroRoutine()
        {
            CurrentState = GameState.Intro;

            float playerDetectedTime = 0f;
            float messageTimer = 0f;

            while (playerDetectedTime < 0.8f)
            {
                if (Tracker.Players.Count > 0)
                {
                    playerDetectedTime += Time.deltaTime;
                }
                else
                {
                    playerDetectedTime = 0f;

                    messageTimer -= Time.deltaTime;

                    if (messageTimer <= 0f)
                    {
                        ShowMessage(
                            "STEP ONTO THE FLOOR TO START!",
                            2f
                        );

                        messageTimer = 5f;
                    }
                }

                yield return null;
            }

            yield return TutorialRoutine();

            ShowMessage("Save Curupira!");

            yield return new WaitForSeconds(2f);
        }

        protected override void OnGameBegin()
        {
            Time.timeScale = 1f;

            CurrentState = GameState.Playing;

            SubscribeToSensors();

            gameTimer = GAME_DURATION;

            rootsDestroyed = 0;
            totalRootsToDestroy = 5;

            if (ComboManager.Instance != null)
                ComboManager.Instance.ResetStats();

            if (SwipeInputManager.Instance != null)
                ComboManager.Instance.ResetStats();

            if (UIManager.Instance != null)
                UIManager.Instance.HidePanels();

            // Reset root state.
            rootsDestroyed = 0;
            totalRootsToDestroy = 3;

            // Make sure we actually have 3 gameplay roots assigned.
            if (gameplayRoots == null || gameplayRoots.Length < 3)
            {
                Debug.LogError(
                    "[CurupiraGameController] " +
                    "You must assign at least 3 Gameplay Roots in the Inspector!"
                );

                return;
            }

            // First disable ALL five roots.
            for (int i = 0; i < gameplayRoots.Length; i++)
            {
                if (gameplayRoots[i] != null)
                {
                    gameplayRoots[i].gameObject.SetActive(false);
                }
            }

            // Now explicitly enable the first three assigned roots.
            for (int i = 0; i < 3; i++)
            {
                Gameplay.RootBehaviour root = gameplayRoots[i];

                if (root == null)
                {
                    Debug.LogError(
                        $"[CurupiraGameController] Gameplay Root slot {i + 1} is empty!"
                    );

                    continue;
                }

                root.gameObject.SetActive(true);
                root.Initialize(rootHealth);

                Debug.Log(
                    $"[CurupiraGameController] " +
                    $"GAME ROOT {i + 1}: {root.name} ACTIVE, " +
                    $"HP = {root.health}"
                );
            }

            FruitSpawnManager spawner =
                FindObjectOfType<FruitSpawnManager>();

            if (spawner != null)
            {
                spawner.ClearAllFruits();
            }

            ShowMessage("GO!");

            Debug.Log(
                "[CurupiraGameController] Game started."
            );
        }

        protected override void OnGameTick(float deltaTime)
        {
            if (CurrentState != GameState.Playing)
                return;

            gameTimer -= deltaTime;

            if (gameTimer <= 0f)
            {
                gameTimer = 0f;

                EndGame(false);
            }
        }

        protected override void OnGameStop(GameEndReason reason)
        {
            CurrentState = GameState.End;

            Time.timeScale = 1f;
        }

        public void ShowComboMessage(string msg)
        {
            ShowMessage(msg);
        }

        public void AddPoints(int points)
        {
            if (Score != null)
            {
                Score.AddScore(
                    0,
                    points,
                    ScoreChangeReason.GameAction,
                    GameId
                );
            }
        }

        public float GetTimeRemaining()
        {
            return Mathf.Max(0f, gameTimer);
        }

        public void OnRootDestroyed()
        {
            if (CurrentState != GameState.Playing)
                return;

            rootsDestroyed++;

            Debug.Log(
                $"[CurupiraGameController] " +
                $"Root destroyed! " +
                $"{rootsDestroyed}/{totalRootsToDestroy}"
            );

            if (rootsDestroyed >= totalRootsToDestroy)
            {
                EndGame(true);
            }
        }

        private void EndGame(bool success)
        {
            if (CurrentState == GameState.End)
                return;

            CurrentState = GameState.End;

            int finalScore =
                ComboManager.Instance != null
                    ? ComboManager.Instance.TotalScore
                    : 0;

            int totalSwipes =
                SwipeInputManager.Instance != null
                    ? SwipeInputManager.Instance.TotalSwipes
                    : 0;

            int totalHits =
                ComboManager.Instance != null
                    ? ComboManager.Instance.TotalHits
                    : 0;

            int missCount =
                ComboManager.Instance != null
                    ? ComboManager.Instance.GetMissCount()
                    : 0;

            float accuracy =
                totalSwipes > 0
                    ? ((float)totalHits / totalSwipes) * 100f
                    : 0f;

            float elapsedTime =
                GAME_DURATION - gameTimer;

            if (success)
            {
                ShowMessage("CURUPIRA SAVED!");

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowVictoryPanel(
                        finalScore,
                        accuracy
                    );
                }

                Gameplay?.Results?.Submit(
                    new GameplayModuleResult(
                        GameId,
                        true,
                        finalScore,
                        1f,
                        elapsedTime,
                        "Curupira Freed",
                        "won"
                    )
                );
            }
            else
            {
                ShowMessage("TIME'S UP!");

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowDefeatPanel(
                        finalScore,
                        missCount,
                        "TIME'S UP!"
                    );
                }

                Gameplay?.Results?.Submit(
                    new GameplayModuleResult(
                        GameId,
                        false,
                        finalScore,
                        0.5f,
                        elapsedTime,
                        "Ran out of time",
                        "lost"
                    )
                );
            }

            RequestEarlyEnd();
        }

        public void OnTooManyMisses()
        {
            if (CurrentState == GameState.End)
                return;

            CurrentState = GameState.End;

            int finalScore =
                ComboManager.Instance != null
                    ? ComboManager.Instance.TotalScore
                    : 0;

            int missCount =
                ComboManager.Instance != null
                    ? ComboManager.Instance.GetMissCount()
                    : 0;

            float elapsedTime =
                GAME_DURATION - gameTimer;

            ShowMessage("TOO MANY MISSES!");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowDefeatPanel(
                    finalScore,
                    missCount,
                    "TOO MANY MISSES!"
                );
            }

            Gameplay?.Results?.Submit(
                new GameplayModuleResult(
                    GameId,
                    false,
                    finalScore,
                    0.5f,
                    elapsedTime,
                    "Too many misses",
                    "lost"
                )
            );

            RequestEarlyEnd();
        }

        private void SubscribeToSensors()
        {
            if (hasSubscribedSensors)
                return;

            Listen<TouchStarted>(
                OnSensorTouchStarted
            );

            Listen<TouchReleased>(
                OnSensorTouchReleased
            );

            Listen<Swipe>(
                OnPlatformSwipe
            );

            hasSubscribedSensors = true;
        }

        private void OnSensorTouchStarted(
            TouchStarted e)
        {
            if (CurrentState != GameState.Playing &&
                CurrentState != GameState.Tutorial)
                return;

            if (SwipeInputManager.Instance != null)
            {
                SwipeInputManager.Instance
                    .ProcessSensorTouchStart(
                        e.PlayerId,
                        e.UV
                    );
            }
        }

        private void OnSensorTouchReleased(
            TouchReleased e)
        {
            if (CurrentState != GameState.Playing &&
                CurrentState != GameState.Tutorial)
                return;

            if (SwipeInputManager.Instance != null)
            {
                SwipeInputManager.Instance
                    .ProcessSensorTouchEnd(
                        e.PlayerId,
                        e.UV
                    );
            }
        }

        private void OnPlatformSwipe(Swipe e)
        {
            if (CurrentState != GameState.Playing &&
                CurrentState != GameState.Tutorial)
                return;

            if (SwipeInputManager.Instance != null)
            {
                SwipeInputManager.Instance
                    .ProcessPlatformSwipe(e);
            }
        }
    }
}