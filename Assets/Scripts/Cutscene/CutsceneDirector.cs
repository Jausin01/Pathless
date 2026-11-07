using System.Collections;
using Pathless.Dialogue;
using Pathless.SceneFlow;
using UnityEngine;

namespace Pathless.Cutscene
{
    /// <summary>
    /// Runs the "@command" lines of .dialogue files. It only reads the command words and passes the work to the
    /// parts that do it (ScreenFader, CutsceneActor, CutsceneCamera). A command with wrong arguments logs a warning
    /// and is skipped, so a typo never freezes the game.
    ///
    /// Commands (numbers use "."; positions are world units, the same numbers as the Transform in the Inspector):
    ///   @fade out [seconds]            fade the screen to black (default 0.5 s)
    ///   @fade in [seconds]             fade back to the scene
    ///   @wait seconds                  pause
    ///   @move Actor x y [temp]         walk the actor to the position and wait until it arrives
    ///   @face Actor direction [temp]   turn the actor (north, south, east, west, north-east, ... or n, s, e, w, ne...)
    ///   By default the final position/direction is permanent (the actor stays there after scene changes);
    ///   add "temp" at the end and the actor goes back to its original place when the scene reloads.
    ///   @camera focus Actor [seconds]  pan the camera to an actor (default 0.8 s)
    ///   @camera focus x y [seconds]    pan the camera to a position
    ///   @camera follow Actor [seconds] same as focus; use "@camera follow Player" to give the camera back
    ///   @scene Scene [spawnId] [conversationId]  fade out, load another scene, put the player on the spawn point
    ///                                  and continue with that conversation (a block of the new scene's .dialogue file)
    /// The camera also returns to the player by itself when the dialogue ends.
    /// </summary>
    public class CutsceneDirector : DialogueCommandExecutor
    {
        /// <summary>Default fade time in seconds.</summary>
        private const float DefaultFadeSeconds = 0.5f;

        /// <summary>Default camera pan time in seconds.</summary>
        private const float DefaultPanSeconds = 0.8f;

        /// <summary>The screen fader.</summary>
        [SerializeField] private ScreenFader fader;

        /// <summary>The camera controller.</summary>
        [SerializeField] private CutsceneCamera cutsceneCamera;

        /// <summary>Changes scenes for the @scene command.</summary>
        [SerializeField] private SceneLoader sceneLoader;

        /// <summary>The dialogue runner; used to reset the camera when the dialogue ends. Assigned by the scene builder.</summary>
        /// <summary>Extra command groups (party, music...). Asked when the director does not know a command itself.</summary>
        [SerializeField] private CommandModule[] modules;

        [SerializeField] private DialogueRunner runner;

        /// <summary>Starts listening for the end of the dialogue.</summary>
        private void OnEnable()
        {
            runner.DialogueEnded += OnDialogueEnded;
        }

        /// <summary>Stops listening.</summary>
        private void OnDisable()
        {
            runner.DialogueEnded -= OnDialogueEnded;
        }

        /// <summary>Gives the camera back to the player.</summary>
        private void OnDialogueEnded()
        {
            cutsceneCamera.ResetToDefault();
        }

        /// <summary>Reads the command word and runs the matching command.</summary>
        /// <param name="command">The command text without the "@".</param>
        /// <returns>A coroutine that ends when the command is done.</returns>
        public override IEnumerator Execute(string command)
        {
            string[] words = command.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                yield break;
            }

            switch (words[0].ToLowerInvariant())
            {
                case "fade":
                    yield return StartCoroutine(RunFade(words, command));
                    break;
                case "wait":
                    yield return StartCoroutine(RunWait(words, command));
                    break;
                case "move":
                    yield return StartCoroutine(RunMove(words, command));
                    break;
                case "face":
                    RunFace(words, command);
                    break;
                case "camera":
                    yield return StartCoroutine(RunCamera(words, command));
                    break;
                case "scene":
                    yield return StartCoroutine(RunScene(words, command));
                    break;
                default:
                    CommandModule module = FindModule(words[0].ToLowerInvariant());
                    if (module != null)
                    {
                        yield return StartCoroutine(module.Run(words, command));
                    }
                    else
                    {
                        Warn(command, "unknown command.");
                    }

                    break;
            }
        }

        /// <summary>@fade out|in [seconds]</summary>
        private IEnumerator RunFade(string[] words, string command)
        {
            if (words.Length < 2 || (words[1] != "out" && words[1] != "in"))
            {
                Warn(command, "use '@fade out [seconds]' or '@fade in [seconds]'.");
                yield break;
            }

            float seconds = DefaultFadeSeconds;
            if (words.Length > 2 && !CutsceneParsing.TryFloat(words[2], out seconds))
            {
                Warn(command, "the fade time is not a number.");
                yield break;
            }

            yield return StartCoroutine(fader.Fade(words[1] == "out", seconds));
        }

        /// <summary>@wait seconds</summary>
        private IEnumerator RunWait(string[] words, string command)
        {
            if (words.Length < 2 || !CutsceneParsing.TryFloat(words[1], out float seconds))
            {
                Warn(command, "use '@wait seconds'.");
                yield break;
            }

            yield return new WaitForSeconds(seconds);
        }

        /// <summary>@move Actor x y</summary>
        private IEnumerator RunMove(string[] words, string command)
        {
            if (words.Length < 4 || !CutsceneParsing.TryFloat(words[2], out float x) || !CutsceneParsing.TryFloat(words[3], out float y))
            {
                Warn(command, "use '@move Actor x y'.");
                yield break;
            }

            CutsceneActor actor = CutsceneActor.Find(words[1]);
            if (actor == null)
            {
                Warn(command, $"no actor named '{words[1]}' (add a CutsceneActor to it).");
                yield break;
            }

            yield return StartCoroutine(actor.MoveTo(new Vector2(x, y), !IsTemp(words, 4)));
        }

        /// <summary>@face Actor direction</summary>
        private void RunFace(string[] words, string command)
        {
            if (words.Length < 3 || !CutsceneParsing.TryDirection(words[2], out Vector2 direction))
            {
                Warn(command, "use '@face Actor north|south|east|west|north-east|...'.");
                return;
            }

            CutsceneActor actor = CutsceneActor.Find(words[1]);
            if (actor == null)
            {
                Warn(command, $"no actor named '{words[1]}' (add a CutsceneActor to it).");
                return;
            }

            actor.Face(direction, !IsTemp(words, 3));
        }

        /// <summary>@scene Scene [spawnId] [conversationId]</summary>
        private IEnumerator RunScene(string[] words, string command)
        {
            if (words.Length < 2)
            {
                Warn(command, "use '@scene SceneName [spawnId] [conversationId]'.");
                yield break;
            }

            string spawnId = words.Length > 2 ? words[2] : "";
            string conversationId = words.Length > 3 ? words[3] : "";
            yield return StartCoroutine(sceneLoader.GoTo(words[1], spawnId, conversationId));
        }

        /// <summary>@camera focus|follow Actor [seconds]  or  @camera focus x y [seconds]</summary>
        private IEnumerator RunCamera(string[] words, string command)
        {
            if (words.Length < 3 || (words[1] != "focus" && words[1] != "follow"))
            {
                Warn(command, "use '@camera focus Actor [seconds]', '@camera focus x y [seconds]' or '@camera follow Actor'.");
                yield break;
            }

            // "x y" form: two numbers after the verb.
            if (words.Length >= 4 && CutsceneParsing.TryFloat(words[2], out float x) && CutsceneParsing.TryFloat(words[3], out float y))
            {
                float pointSeconds = DefaultPanSeconds;
                if (words.Length > 4 && !CutsceneParsing.TryFloat(words[4], out pointSeconds))
                {
                    Warn(command, "the pan time is not a number.");
                    yield break;
                }

                yield return StartCoroutine(cutsceneCamera.FocusOnPoint(new Vector2(x, y), pointSeconds));
                yield break;
            }

            CutsceneActor actor = CutsceneActor.Find(words[2]);
            if (actor == null)
            {
                Warn(command, $"no actor named '{words[2]}' (add a CutsceneActor to it).");
                yield break;
            }

            float seconds = DefaultPanSeconds;
            if (words.Length > 3 && !CutsceneParsing.TryFloat(words[3], out seconds))
            {
                Warn(command, "the pan time is not a number.");
                yield break;
            }

            yield return StartCoroutine(cutsceneCamera.FocusOn(actor.transform, seconds));
        }

        /// <summary>Checks whether the optional "temp" word is at a position of the command.</summary>
        /// <param name="words">The command split in words.</param>
        /// <param name="index">Where "temp" would be.</param>
        /// <returns>True if the command is temporary.</returns>
        /// <summary>Finds the module that handles a command word.</summary>
        /// <param name="verb">The command word in lower case.</param>
        /// <returns>The module, or null if none handles it.</returns>
        private CommandModule FindModule(string verb)
        {
            if (modules == null)
            {
                return null;
            }

            foreach (CommandModule module in modules)
            {
                if (module != null && module.Handles(verb))
                {
                    return module;
                }
            }

            return null;
        }

        private static bool IsTemp(string[] words, int index)
        {
            return words.Length > index && words[index].ToLowerInvariant() == "temp";
        }

        /// <summary>Logs a warning that names the command.</summary>
        /// <param name="command">The command text.</param>
        /// <param name="problem">What is wrong.</param>
        private void Warn(string command, string problem)
        {
            Debug.LogWarning($"CutsceneDirector: '@{command}' skipped, {problem}", this);
        }
    }
}
