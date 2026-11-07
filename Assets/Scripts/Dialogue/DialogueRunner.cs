using System;
using System.Collections;
using System.Collections.Generic;
using Pathless.Story;
using UnityEngine;

namespace Pathless.Dialogue
{
    /// <summary>
    /// Plays a DialogueSequence line by line. Controls the flow only:
    /// Space while typing completes the line, Space when complete goes to the next line (or ends the dialogue).
    /// Besides speech it understands the other line kinds of a .dialogue file:
    ///  - Command lines ("@fade", "@move"...) are handed to the DialogueCommandExecutor; the box hides while they run.
    ///  - Flag lines ("@set", "@add") change the story flags instantly.
    ///  - "@if / @else / @end" are compiled into jumps; the runner follows them using the current flags.
    ///  - "@goto" continues in another conversation.
    ///  - "@choice" shows the options (ChoiceView); up/down selects, Space confirms.
    /// Visuals are delegated to DialogueView / ChoiceView and input to DialogueInputReader.
    /// Usage: call StartDialogue(sequence) from anything (NPC, cutscene, trigger).
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        /// <summary>The possible states of the dialogue flow.</summary>
        private enum State
        {
            /// <summary>No dialogue is running.</summary>
            Idle,

            /// <summary>The current line is being typed.</summary>
            Typing,

            /// <summary>The line is complete; waiting for the player to advance.</summary>
            WaitingForInput,

            /// <summary>A cutscene command is running; the advance button is ignored.</summary>
            RunningCommand,

            /// <summary>The options of a choice are on screen; waiting for the player to pick one.</summary>
            Choosing
        }

        /// <summary>
        /// Safety limit: how many instant lines (flags, jumps, gotos) may run in a row without showing anything.
        /// Protects against endless loops such as two conversations that "@goto" each other.
        /// </summary>
        private const int MaxInstantSteps = 1000;

        /// <summary>The dialogue box visuals.</summary>
        [SerializeField] private DialogueView view;

        /// <summary>The advance and navigation button reader.</summary>
        [SerializeField] private DialogueInputReader input;

        /// <summary>Finds characters by name for lines that only have a speaker name (lines from .dialogue files).</summary>
        [SerializeField] private DialogueCharacterLibrary characterLibrary;

        /// <summary>Runs the "@command" lines. Optional: without it commands are skipped with a warning.</summary>
        [SerializeField] private DialogueCommandExecutor commandExecutor;

        /// <summary>Shows the options of a choice. Optional: without it the first option is picked with a warning.</summary>
        [SerializeField] private ChoiceView choiceView;

        /// <summary>Current flow state.</summary>
        private State state = State.Idle;

        /// <summary>The sequence being played (changes when a jump goes to another conversation).</summary>
        private DialogueSequence sequence;

        /// <summary>Index of the line being shown.</summary>
        private int lineIndex;

        /// <summary>True while the dialogue box is on screen (it hides during commands).</summary>
        private bool isBoxOpen;

        /// <summary>The running command coroutine, or null.</summary>
        private Coroutine commandRoutine;

        /// <summary>The choice line being answered (only while Choosing).</summary>
        private DialogueLine activeChoice;

        /// <summary>Index of the highlighted option (only while Choosing).</summary>
        private int choiceIndex;

        /// <summary>True while a dialogue is running.</summary>
        public bool IsRunning => state != State.Idle;

        /// <summary>Raised when a dialogue begins.</summary>
        public event Action DialogueStarted;

        /// <summary>Raised when the last line was dismissed.</summary>
        public event Action DialogueEnded;

        /// <summary>Subscribes to input and view events.</summary>
        private void Awake()
        {
            input.AdvancePressed += OnAdvancePressed;
            input.UpPressed += OnUpPressed;
            input.DownPressed += OnDownPressed;
            view.TypingFinished += OnTypingFinished;
        }

        /// <summary>Unsubscribes from events.</summary>
        private void OnDestroy()
        {
            if (input != null)
            {
                input.AdvancePressed -= OnAdvancePressed;
                input.UpPressed -= OnUpPressed;
                input.DownPressed -= OnDownPressed;
            }

            if (view != null)
            {
                view.TypingFinished -= OnTypingFinished;
            }
        }

        /// <summary>Starts a dialogue. Ignored if one is already running or the sequence is empty.</summary>
        /// <param name="newSequence">The conversation to play.</param>
        public void StartDialogue(DialogueSequence newSequence)
        {
            if (IsRunning || newSequence == null || newSequence.Lines.Count == 0)
            {
                return;
            }

            sequence = newSequence;
            lineIndex = 0;

            input.SetListening(true);
            DialogueStarted?.Invoke();

            ProcessCurrentLine();
        }

        /// <summary>
        /// Runs the line at lineIndex. Instant lines (flags, jumps, gotos) are followed in a loop until a line that
        /// needs the player or time is reached (speech, command, choice) or the dialogue ends.
        /// </summary>
        private void ProcessCurrentLine()
        {
            int instantSteps = 0;

            while (true)
            {
                if (lineIndex >= sequence.Lines.Count)
                {
                    EndDialogue();
                    return;
                }

                instantSteps++;
                if (instantSteps > MaxInstantSteps)
                {
                    Debug.LogError($"DialogueRunner: more than {MaxInstantSteps} instant lines in a row in '{sequence.name}' (a @goto loop?). Dialogue stopped.", this);
                    EndDialogue();
                    return;
                }

                DialogueLine line = sequence.Lines[lineIndex];
                switch (line.Kind)
                {
                    case DialogueLineKind.Flag:
                        ApplyFlagCommand(line.Command);
                        lineIndex++;
                        break;

                    case DialogueLineKind.JumpIfFalse:
                        lineIndex = StoryCondition.Evaluate(line.Command) ? lineIndex + 1 : line.JumpTarget;
                        break;

                    case DialogueLineKind.Jump:
                        lineIndex = line.JumpTarget;
                        break;

                    case DialogueLineKind.Goto:
                        if (line.GotoSequence == null)
                        {
                            Debug.LogWarning($"DialogueRunner: @goto '{line.Command}' not found in '{sequence.name}', skipped.", this);
                            lineIndex++;
                        }
                        else
                        {
                            sequence = line.GotoSequence;
                            lineIndex = 0;
                        }

                        break;

                    case DialogueLineKind.Command:
                        state = State.RunningCommand;
                        CloseBox();
                        commandRoutine = StartCoroutine(RunCommand(line.Command));
                        return;

                    case DialogueLineKind.Choice:
                        BeginChoice(line);
                        return;

                    default:
                        OpenBox();
                        state = State.Typing;
                        view.ShowLine(line, ResolveSpeaker(line));
                        return;
                }
            }
        }

        /// <summary>Runs a "set" / "add" command and logs a warning if it is invalid.</summary>
        /// <param name="command">The flag command without the "@".</param>
        private void ApplyFlagCommand(string command)
        {
            if (!StoryFlags.TryApply(command, out string error))
            {
                Debug.LogWarning($"DialogueRunner: '@{command}' skipped, {error}", this);
            }
        }

        /// <summary>Runs one command, then continues with the next line.</summary>
        /// <param name="command">The command text without the "@".</param>
        private IEnumerator RunCommand(string command)
        {
            if (commandExecutor == null)
            {
                Debug.LogWarning($"DialogueRunner: no command executor assigned, skipping '@{command}'.", this);
            }
            else
            {
                yield return StartCoroutine(commandExecutor.Execute(command));
            }

            commandRoutine = null;
            GoToNextLine();
        }

        /// <summary>Moves to the next line, or ends the dialogue after the last one.</summary>
        private void GoToNextLine()
        {
            lineIndex++;
            ProcessCurrentLine();
        }

        /// <summary>Shows the options of a choice and waits for the player.</summary>
        /// <param name="line">The choice line.</param>
        private void BeginChoice(DialogueLine line)
        {
            OpenBox();
            activeChoice = line;
            choiceIndex = 0;

            if (choiceView == null)
            {
                Debug.LogWarning("DialogueRunner: no ChoiceView assigned, the first option is picked automatically.", this);
                state = State.Choosing;
                Choose(0);
                return;
            }

            List<string> labels = new List<string>();
            foreach (DialogueChoiceOption option in line.Options)
            {
                labels.Add(option.Label);
            }

            state = State.Choosing;
            choiceView.Show(labels);
            choiceView.SetSelected(choiceIndex);
        }

        /// <summary>Applies the picked option (flags, optional jump) and continues the dialogue.</summary>
        /// <param name="index">Index of the picked option.</param>
        private void Choose(int index)
        {
            DialogueChoiceOption option = activeChoice.Options[index];
            activeChoice = null;

            if (choiceView != null)
            {
                choiceView.Hide();
            }

            foreach (string action in option.Actions)
            {
                ApplyFlagCommand(action);
            }

            if (option.GotoSequence != null)
            {
                sequence = option.GotoSequence;
                lineIndex = 0;
                ProcessCurrentLine();
                return;
            }

            if (!string.IsNullOrEmpty(option.GotoId))
            {
                Debug.LogWarning($"DialogueRunner: @goto '{option.GotoId}' of option '{option.Label}' not found, continuing.", this);
            }

            GoToNextLine();
        }

        /// <summary>Slides the box in if it is not on screen.</summary>
        private void OpenBox()
        {
            if (!isBoxOpen)
            {
                isBoxOpen = true;
                view.Open();
            }
        }

        /// <summary>Slides the box out if it is on screen.</summary>
        private void CloseBox()
        {
            if (isBoxOpen)
            {
                isBoxOpen = false;
                view.Close();
            }
        }

        /// <summary>Finds the character of a line: the direct reference first, then the name in the library.</summary>
        /// <param name="line">The line to resolve.</param>
        /// <returns>The character, or null for narration or an unknown name (a warning is logged).</returns>
        private DialogueCharacter ResolveSpeaker(DialogueLine line)
        {
            if (line.Speaker != null)
            {
                return line.Speaker;
            }

            if (string.IsNullOrEmpty(line.SpeakerName))
            {
                return null;
            }

            DialogueCharacter found = characterLibrary != null ? characterLibrary.Find(line.SpeakerName) : null;
            if (found == null)
            {
                Debug.LogWarning($"DialogueRunner: no character named '{line.SpeakerName}' in the character library.", this);
            }

            return found;
        }

        /// <summary>Handles the advance button according to the current state.</summary>
        private void OnAdvancePressed()
        {
            switch (state)
            {
                case State.Typing:
                    view.SkipTyping();
                    break;

                case State.WaitingForInput:
                    GoToNextLine();
                    break;

                case State.Choosing:
                    Choose(choiceIndex);
                    break;
            }
        }

        /// <summary>Moves the highlighted option up (wraps around).</summary>
        private void OnUpPressed()
        {
            MoveChoice(-1);
        }

        /// <summary>Moves the highlighted option down (wraps around).</summary>
        private void OnDownPressed()
        {
            MoveChoice(1);
        }

        /// <summary>Changes the highlighted option while choosing.</summary>
        /// <param name="direction">-1 for up, 1 for down.</param>
        private void MoveChoice(int direction)
        {
            if (state != State.Choosing || activeChoice == null || choiceView == null)
            {
                return;
            }

            int count = activeChoice.Options.Count;
            choiceIndex = (choiceIndex + direction + count) % count;
            choiceView.SetSelected(choiceIndex);
        }

        /// <summary>
        /// Called when the line is fully visible. If the next line is a choice the options appear right away
        /// (the question stays on screen); otherwise the arrow shows and the runner waits for the advance button.
        /// </summary>
        private void OnTypingFinished()
        {
            if (state != State.Typing)
            {
                return;
            }

            int next = lineIndex + 1;
            if (next < sequence.Lines.Count && sequence.Lines[next].Kind == DialogueLineKind.Choice)
            {
                lineIndex = next;
                BeginChoice(sequence.Lines[next]);
                return;
            }

            state = State.WaitingForInput;
            view.SetWaiting(true);
        }

        /// <summary>Closes the box and returns to idle.</summary>
        private void EndDialogue()
        {
            state = State.Idle;
            sequence = null;
            activeChoice = null;

            if (commandRoutine != null)
            {
                StopCoroutine(commandRoutine);
                commandRoutine = null;
            }

            if (choiceView != null)
            {
                choiceView.Hide();
            }

            input.SetListening(false);
            CloseBox();
            DialogueEnded?.Invoke();
        }
    }
}
