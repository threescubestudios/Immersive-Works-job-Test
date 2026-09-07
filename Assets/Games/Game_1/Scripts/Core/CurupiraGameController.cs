using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ImmersiWorks.Platform.Core;
using ImmersiWorks.Platform.Runtime;

namespace SaveCurupira.Core
{
    public class CurupiraGameController : GameBase
    {
        public static CurupiraGameController Instance { get; private set; }

        public override string GameId => "game_save_curupira";
        public override string DisplayName => "Save Curupira - Fruit Strike";
        public override float NominalDurationSeconds => 180f;

        public SaveCurupiraDescriptor gameDescriptor;
        
        public enum GameState { Intro, Tutorial, Playing, End }
        public GameState CurrentState { get; private set; }

        private float gameTimer;
        
        private int rootsDestroyed = 0;
        private int totalRootsToDestroy = 4; // defaults to easy mode root count

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        
        private IEnumerator TutorialRoutine()
        {
            CurrentState = GameState.Tutorial;
            
            SubscribeToSensors();

            Gameplay.RootBehaviour[] allRoots = FindObjectsOfType<Gameplay.RootBehaviour>(true);
            Gameplay.RootBehaviour tutorialRoot = null;
            
            float minX = float.MaxValue;
            foreach (var r in allRoots)
            {
                r.gameObject.SetActive(false);
                if (Mathf.Abs(r.transform.position.x) < minX)
                {
                    minX = Mathf.Abs(r.transform.position.x);
                    tutorialRoot = r;
                }
            }
            
            if (tutorialRoot != null)
            {
                tutorialRoot.Initialize(1);
            }
            
            ShowMessage("Swipe the fruit into the root!");
            
            FruitSpawnManager spawner = FindObjectOfType<FruitSpawnManager>();
            if (spawner != null) spawner.SpawnSingleFruit();
            
            while (tutorialRoot != null && tutorialRoot.gameObject.activeInHierarchy && tutorialRoot.health > 0)
            {
                if (FindObjectOfType<Gameplay.FruitBehaviour>() == null)
                {
                    if (spawner != null) spawner.SpawnSingleFruit();
                }
                yield return new WaitForSeconds(0.5f);
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
                    if (messageTimer <= 0) 
                    { 
                        ShowMessage("STEP ONTO THE FLOOR TO START!", 2.0f); 
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
            CurrentState = GameState.Playing;
            SubscribeToSensors();
            
            if (gameDescriptor != null)
            {
                gameTimer = 180f;
                DifficultySettings settings = gameDescriptor.GetCurrentSettings();
                totalRootsToDestroy = settings.rootCount;
                rootsDestroyed = 0;
                
                Gameplay.RootBehaviour[] allRoots = FindObjectsOfType<Gameplay.RootBehaviour>(true);
                
                float minZ = float.MaxValue;
                foreach (var r in allRoots) {
                    if (r.transform.position.z < minZ) minZ = r.transform.position.z;
                }
                
                List<Gameplay.RootBehaviour> frontRow = new List<Gameplay.RootBehaviour>();
                List<Gameplay.RootBehaviour> backRow = new List<Gameplay.RootBehaviour>();
                
                foreach (var r in allRoots) {
                    if (r.transform.position.z <= minZ + 1f) frontRow.Add(r);
                    else backRow.Add(r);
                }
                
                frontRow.Sort((a, b) => Mathf.Abs(a.transform.position.x).CompareTo(Mathf.Abs(b.transform.position.x)));
                
                for (int i = 0; i < backRow.Count; i++) {
                    Gameplay.RootBehaviour temp = backRow[i];
                    int randomIndex = Random.Range(i, backRow.Count);
                    backRow[i] = backRow[randomIndex];
                    backRow[randomIndex] = temp;
                }
                
                List<Gameplay.RootBehaviour> orderedRoots = new List<Gameplay.RootBehaviour>(frontRow);
                orderedRoots.AddRange(backRow);
                
                for (int i = 0; i < orderedRoots.Count; i++)
                {
                    if (i < settings.rootCount) {
                        orderedRoots[i].Initialize(settings.rootHealth);
                    } else {
                        orderedRoots[i].gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                gameTimer = 180f;
            }
            
            ShowMessage("GO!");
        }

        protected override void OnGameTick(float deltaTime)
        {
            if (CurrentState == GameState.Playing)
            {
                gameTimer -= deltaTime;
                if (gameTimer <= 0)
                {
                    EndGame(false);
                }
            }
        }
        
        protected override void OnGameStop(GameEndReason reason)
        {
            CurrentState = GameState.End;
        }
        
        public void ShowComboMessage(string msg)
        {
            ShowMessage(msg);
        }

        public void AddPoints(int points)
        {
            if (Score != null)
            {
                Score.AddScore(0, points, ScoreChangeReason.GameAction, GameId);
            }
        }

        public float GetTimeRemaining()
        {
            return gameTimer;
        }

        public void OnRootDestroyed()
        {
            rootsDestroyed++;
            if (rootsDestroyed >= totalRootsToDestroy)
            {
                EndGame(true);
            }
        }

        private void EndGame(bool success)
        {
            if (CurrentState == GameState.End) return;
            CurrentState = GameState.End;
            
            int finalScore = ComboManager.Instance != null ? ComboManager.Instance.TotalScore : 0;
            
            int totalSwipes = SwipeInputManager.Instance != null ? SwipeInputManager.Instance.TotalSwipes : 0;
            int totalHits = ComboManager.Instance != null ? ComboManager.Instance.TotalHits : 0;
            float accuracy = totalSwipes > 0 ? ((float)totalHits / totalSwipes) * 100f : 0f;
            
            if (success)
            {
                ShowMessage($"CURUPIRA SAVED!\nScore: {finalScore}\nAccuracy: {accuracy:F1}%");
                float elapsed = 180f - gameTimer;
                Gameplay?.Results?.Submit(new GameplayModuleResult(GameId, true, finalScore, 1f, elapsed, "Curupira Freed", "won"));
            }
            else
            {
                ShowMessage($"TIME'S UP!\nScore: {finalScore}\nAccuracy: {accuracy:F1}%");
                Gameplay?.Results?.Submit(new GameplayModuleResult(GameId, false, finalScore, 1f, 180f, "Ran out of time", "lost"));
            }
            
            RequestEarlyEnd();
        }

        private bool hasSubscribedSensors = false;
        
        private void SubscribeToSensors()
        {
            if (hasSubscribedSensors) return;
            Listen<TouchStarted>(OnSensorTouchStarted);
            Listen<TouchReleased>(OnSensorTouchReleased);
            Listen<Swipe>(OnPlatformSwipe);
            hasSubscribedSensors = true;
        }

        private void OnSensorTouchStarted(TouchStarted e)
        {
            if (SwipeInputManager.Instance != null) 
                SwipeInputManager.Instance.ProcessSensorTouchStart(e.PlayerId, e.UV);
        }

        private void OnSensorTouchReleased(TouchReleased e)
        {
            if (SwipeInputManager.Instance != null) 
                SwipeInputManager.Instance.ProcessSensorTouchEnd(e.PlayerId, e.UV);
        }

        private void OnPlatformSwipe(Swipe e)
        {
            if (SwipeInputManager.Instance != null)
                SwipeInputManager.Instance.ProcessPlatformSwipe(e);
        }
    }
}
