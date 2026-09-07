// MiniPlatform -- a small stand-in for the parts of the ImmersiWorks platform this game uses.
//
// The real platform drives a curved projection wall, a floor, depth cameras and four player zones.
// None of that is needed to build and judge a game, and shipping it would mean shipping four camera
// SDKs and a projection rig with it. So this file provides the handful of types the game actually
// touches, and nothing else.
//
// What it gives you:
//   GameBase              a timed game with begin / tick / stop and an intro
//   GameEndReason         why a round ended
//   Listen<T>             subscribe to an input event
//   TouchStarted/Released, Swipe   the input events
//   PlatformSpatial       UV to world position, on a flat plane here
//
// In the real platform these come from a wall the player stands in front of, with the UV being a
// position on that curved surface. Here they are driven by the mouse, so the game is playable on a
// laptop. The shapes are the same, so nothing has to be rewritten to run on the real thing.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ImmersiWorks.Platform.Core
{
    /// <summary>Marker for anything that can be raised on the event bus.</summary>
    public interface IPlatformEvent { }

    /// <summary>Which surface a touch landed on. Only North is used here.</summary>
    public enum WallId { North, East, South, West, Floor }

    public enum SwipeDirection { Left, Right, Up, Down }

    /// <summary>Why a round ended.</summary>
    public enum GameEndReason
    {
        TimedOut,          // the clock reached zero
        ForcedByOperator,  // an operator ended it
        AllPlayersLeft,    // nobody left in the play area
        EarlyEnd,          // the game itself asked to finish, e.g. all roots cleared
    }

    public enum ScoreChangeReason { GameAction, Bonus, Penalty }

    public readonly struct TouchStarted : IPlatformEvent
    {
        public readonly int PlayerId;
        public readonly WallId Wall;
        public readonly Vector2 UV;
        public readonly bool IsRightHand;
        public TouchStarted(int playerId, WallId wall, Vector2 uv, bool isRightHand = true)
        { PlayerId = playerId; Wall = wall; UV = uv; IsRightHand = isRightHand; }
    }

    public readonly struct TouchReleased : IPlatformEvent
    {
        public readonly int PlayerId;
        public readonly WallId Wall;
        public readonly Vector2 UV;
        public readonly bool IsRightHand;
        public TouchReleased(int playerId, WallId wall, Vector2 uv, bool isRightHand = true)
        { PlayerId = playerId; Wall = wall; UV = uv; IsRightHand = isRightHand; }
    }

    public readonly struct Swipe : IPlatformEvent
    {
        public readonly int PlayerId;
        public readonly SwipeDirection Direction;
        public readonly bool IsRightHand;
        public readonly float SpeedUVs;
        public Swipe(int playerId, SwipeDirection direction, bool isRightHand, float speedUVs)
        { PlayerId = playerId; Direction = direction; IsRightHand = isRightHand; SpeedUVs = speedUVs; }
    }

    /// <summary>
    /// Minimal event bus. The real one is shared across every game and the sensors; this one only
    /// has to carry input from the mouse to whoever is listening.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> Handlers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler) where T : IPlatformEvent
        {
            Handlers.TryGetValue(typeof(T), out Delegate existing);
            Handlers[typeof(T)] = Delegate.Combine(existing, handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : IPlatformEvent
        {
            if (!Handlers.TryGetValue(typeof(T), out Delegate existing))
                return;

            Delegate remaining = Delegate.Remove(existing, handler);
            if (remaining == null) Handlers.Remove(typeof(T));
            else                   Handlers[typeof(T)] = remaining;
        }

        public static void Post<T>(T evt) where T : IPlatformEvent
        {
            if (Handlers.TryGetValue(typeof(T), out Delegate d) && d is Action<T> typed)
                typed(evt);
        }

        public static void Clear() => Handlers.Clear();
    }
}

namespace ImmersiWorks.Platform.Runtime
{
    using ImmersiWorks.Platform.Core;

    /// <summary>
    /// Keeps a score per player. The real platform shows this across four zones on the wall.
    /// </summary>
    public sealed class ScoreManager
    {
        private readonly Dictionary<int, int> _scores = new Dictionary<int, int>();

        public int Total { get; private set; }

        public void AddScore(int playerId, int points, ScoreChangeReason reason, string gameId)
        {
            _scores.TryGetValue(playerId, out int current);
            _scores[playerId] = current + points;
            Total += points;
        }

        public int GetScore(int playerId)
        {
            _scores.TryGetValue(playerId, out int value);
            return value;
        }

        public void Reset() { _scores.Clear(); Total = 0; }
    }

    /// <summary>The outcome of one round, as reported to the session.</summary>
    public readonly struct GameplayModuleResult
    {
        public readonly string GameId;
        public readonly bool   Succeeded;
        public readonly int    Score;
        public readonly float  Weight;
        public readonly float  ElapsedSeconds;
        public readonly string Headline;
        public readonly string Outcome;

        public GameplayModuleResult(string gameId, bool succeeded, int score, float weight,
                                    float elapsedSeconds, string headline, string outcome)
        {
            GameId         = gameId;
            Succeeded      = succeeded;
            Score          = score;
            Weight         = weight;
            ElapsedSeconds = elapsedSeconds;
            Headline       = headline;
            Outcome        = outcome;
        }
    }

    /// <summary>Collects round results. The installation carries these across a whole session.</summary>
    public sealed class ResultsSink
    {
        public List<GameplayModuleResult> All { get; } = new List<GameplayModuleResult>();

        public void Submit(GameplayModuleResult result)
        {
            All.Add(result);
            Debug.Log($"[Result] {result.GameId}: {result.Outcome}, score {result.Score}, " +
                      $"{result.ElapsedSeconds:0.0}s -- {result.Headline}");
        }
    }

    /// <summary>Session-level services a game can report into.</summary>
    public sealed class GameplayServices
    {
        public ResultsSink Results { get; } = new ResultsSink();
    }

    /// <summary>One person in the play area.</summary>
    public sealed class TrackedPlayer
    {
        public int     Id;
        public Vector2 FloorUV;
        public bool    IsPresent = true;
    }

    /// <summary>
    /// Who is in the play area.
    ///
    /// The installation resolves this from a depth camera and can see four people in four zones.
    /// Here there is a single player who is always present, which keeps code that waits for
    /// somebody to arrive working without a sensor.
    /// </summary>
    public sealed class PlayerTracker
    {
        public List<TrackedPlayer> Players { get; } =
            new List<TrackedPlayer> { new TrackedPlayer { Id = 0, FloorUV = new Vector2(0.5f, 0.5f) } };

        public int Count => Players.Count;

        public TrackedPlayer Get(int playerId) => Players.Find(p => p.Id == playerId);
    }

    /// <summary>
    /// Turns a position on a surface into a world position.
    ///
    /// On the real installation the wall is a 210 degree curve and this does the curved mapping.
    /// Here it is a flat plane in front of the camera, which is enough to build and play against.
    /// </summary>
    public static class PlatformSpatial
    {
        /// <summary>Width of the stand-in wall, in metres.</summary>
        public static float WallWidthM = 10f;

        /// <summary>Height of the stand-in wall, in metres.</summary>
        public static float WallHeightM = 5f;

        /// <summary>How far in front of the origin the stand-in wall sits.</summary>
        public static float WallDistanceM = 10f;

        /// <summary>Depth of the stand-in floor, in metres.</summary>
        public static float FloorDepthM = 8f;

        public static Vector3 WallUVToWorld(Vector2 uv, float surfaceOffsetM = 0f)
        {
            return new Vector3(
                (uv.x - 0.5f) * WallWidthM,
                uv.y * WallHeightM,
                WallDistanceM - surfaceOffsetM);
        }

        public static Vector3 FloorUVToWorld(Vector2 uv, float surfaceOffsetM = 0f)
        {
            return new Vector3(
                (uv.x - 0.5f) * WallWidthM,
                surfaceOffsetM,
                uv.y * FloorDepthM);
        }
    }

    /// <summary>
    /// Base class for a timed game.
    ///
    /// Override OnGameBegin, OnGameTick and OnGameStop. The round runs for
    /// NominalDurationSeconds and then ends, unless the game calls RequestEarlyEnd first.
    ///
    /// The real platform starts games from a playlist and hands them a shared event bus and score
    /// manager. Here the game simply starts itself, so pressing Play runs it.
    /// </summary>
    public abstract class GameBase : MonoBehaviour
    {
        public abstract float NominalDurationSeconds { get; }

        /// <summary>Identifier used when recording score. Override for a nicer name.</summary>
        public virtual string GameId => GetType().Name;

        /// <summary>Name shown to players on the wall between games.</summary>
        public virtual string DisplayName => GameId;

        public float TimeRemaining { get; private set; }
        public bool  IsRunning     { get; private set; }

        public float NormalizedTimeRemaining => NominalDurationSeconds > 0f
            ? Mathf.Clamp01(TimeRemaining / NominalDurationSeconds)
            : 0f;

        protected ScoreManager Score { get; } = new ScoreManager();

        private readonly List<Action> _unsubscribes = new List<Action>();
        private bool _earlyEndRequested;

        // The camera, light and mouse input this scene needs are created by SceneBootstrap at the
        // bottom of this file, not from Awake here.
        //
        // CurupiraGameController declares its own private Awake, which HIDES a base Awake rather
        // than overriding it, so setup written here would silently never run. Any game that writes
        // a plain Awake hits the same thing. SceneBootstrap runs after the scene loads regardless
        // of what any component declares, so it cannot be bypassed by accident.

        protected virtual void Start() => StartCoroutine(RunGame());

        private IEnumerator RunGame()
        {
            yield return OnIntroRoutine();

            TimeRemaining      = NominalDurationSeconds;
            _earlyEndRequested = false;
            IsRunning          = true;
            OnGameBegin();

            while (IsRunning && TimeRemaining > 0f && !_earlyEndRequested)
            {
                TimeRemaining -= Time.deltaTime;
                OnGameTick(Time.deltaTime);
                yield return null;
            }

            IsRunning = false;
            OnGameStop(_earlyEndRequested ? GameEndReason.EarlyEnd : GameEndReason.TimedOut);
            yield return OnOutroRoutine();
        }

        /// <summary>Finish the round now, e.g. every root cleared before the clock ran out.</summary>
        public void RequestEarlyEnd() => _earlyEndRequested = true;

        /// <summary>
        /// Flash a short message to the players. On the installation this goes to a HUD spanning
        /// the curved wall; here it goes to the Console and to <see cref="LastMessage"/>, so you
        /// can bind it to whatever UI you build.
        /// </summary>
        protected void ShowMessage(string text, float durationSeconds = 2f)
        {
            LastMessage        = text;
            LastMessageSeconds = durationSeconds;
            MessageShown?.Invoke(text, durationSeconds);
            Debug.Log($"[{GameId}] {text}");
        }

        /// <summary>Most recent message passed to <see cref="ShowMessage"/>.</summary>
        public string LastMessage { get; private set; } = string.Empty;

        /// <summary>How long the most recent message asked to be shown for.</summary>
        public float LastMessageSeconds { get; private set; }

        /// <summary>Raised whenever ShowMessage is called. Hook your own UI to this.</summary>
        public event Action<string, float> MessageShown;

        /// <summary>
        /// Who is currently playing. The installation tracks up to four people with a depth
        /// camera; here there is one player, present as soon as the game starts, so code that
        /// waits for somebody to step in still runs.
        /// </summary>
        protected PlayerTracker Tracker { get; } = new PlayerTracker();

        /// <summary>
        /// Where a game reports how the round went. On the installation the session uses these to
        /// build the end-of-session summary; here they are logged to the Console.
        /// </summary>
        protected GameplayServices Gameplay { get; } = new GameplayServices();

        /// <summary>
        /// Subscribe to an input event for as long as this game is alive. Unsubscribed
        /// automatically on destroy, so a reloaded scene never gets stale handlers.
        /// </summary>
        protected void Listen<T>(Action<T> handler) where T : IPlatformEvent
        {
            EventBus.Subscribe(handler);
            _unsubscribes.Add(() => EventBus.Unsubscribe(handler));
        }

        protected virtual void OnDestroy()
        {
            for (int i = 0; i < _unsubscribes.Count; i++)
                _unsubscribes[i]?.Invoke();
            _unsubscribes.Clear();
        }

        protected virtual IEnumerator OnIntroRoutine() { yield break; }
        protected virtual IEnumerator OnOutroRoutine() { yield break; }
        protected virtual void OnGameBegin() { }
        protected virtual void OnGameTick(float deltaTime) { }
        protected virtual void OnGameStop(GameEndReason reason) { }
    }
}

namespace SaveCurupira.Core
{
    using ImmersiWorks.Platform.Runtime;

    /// <summary>
    /// Gives the scene a camera, a light and mouse input if it has none.
    ///
    /// The real installation supplies all three: projection cameras aimed at a curved wall, room
    /// lighting, and depth sensors watching the players. This scene was authored for that, so on a
    /// laptop it would otherwise open black with nothing to play with.
    ///
    /// This runs after the scene loads rather than from Awake or Start on purpose. A game that
    /// declares its own private Awake hides the base one instead of overriding it, and the setup
    /// would silently never run. That is exactly what happened here the first time.
    ///
    /// Everything below is skipped when the scene already has one, so the moment you add your own
    /// camera, light or input, these stop being created and you are in full control.
    /// </summary>
    public static class SceneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Run()
        {
            EnsureCamera();
            EnsureLight();
            EnsureInput();
        }

        private static void EnsureCamera()
        {
            if (UnityEngine.Object.FindObjectOfType<Camera>() != null)
                return;

            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.SetPositionAndRotation(new Vector3(0f, 2.28f, 1.16f), Quaternion.identity);

            Camera camera = go.AddComponent<Camera>();
            camera.fieldOfView   = 55f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane  = 500f;
            camera.clearFlags    = CameraClearFlags.Skybox;
            go.AddComponent<AudioListener>();

            Debug.Log("[MiniPlatform] This scene has no camera, so one was created looking at the " +
                      "play area. Add a camera of your own and this one stops appearing.");
        }

        private static void EnsureLight()
        {
            if (UnityEngine.Object.FindObjectOfType<Light>() != null)
                return;

            var go = new GameObject("Directional Light");
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Light light = go.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows   = LightShadows.Soft;

            Debug.Log("[MiniPlatform] This scene has no light, so a directional light was created.");
        }

        private static void EnsureInput()
        {
            if (UnityEngine.Object.FindObjectOfType<MouseInputDriver>() != null)
                return;

            var go = new GameObject("Mouse Input");
            go.AddComponent<MouseInputDriver>();

            Debug.Log("[MiniPlatform] Mouse input is active: hold the left button and drag to swipe. " +
                      "It raises the same events the depth sensors raise on the real installation, " +
                      "so anything you write against them needs no changes to run there.");
        }
    }
}
