using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathless.Dialogue
{
    /// <summary>What a DialogueLine does when the runner reaches it.</summary>
    public enum DialogueLineKind
    {
        /// <summary>Shows text in the dialogue box (the normal line).</summary>
        Speech,

        /// <summary>Runs a cutscene command (@fade, @move, @camera...).</summary>
        Command,

        /// <summary>Changes a story flag (@set, @add). Instant.</summary>
        Flag,

        /// <summary>The start of an @if: continues if the condition holds, otherwise jumps to JumpTarget. Instant.</summary>
        JumpIfFalse,

        /// <summary>Jumps to JumpTarget (the end of an @else part). Instant.</summary>
        Jump,

        /// <summary>Continues in another conversation (@goto). Instant.</summary>
        Goto,

        /// <summary>Shows the options of a @choice and waits for the player.</summary>
        Choice
    }

    /// <summary>
    /// One line of a dialogue. Most lines are speech: who speaks, with which expression, and the text.
    /// The other kinds (see DialogueLineKind) are the "@" lines of a .dialogue file: cutscene commands, flag changes,
    /// conditions, jumps and choices. Lines live inside a DialogueSequence.
    /// Speech text supports TextMeshPro rich text, for example &lt;color=#ff6a5a&gt;red words&lt;/color&gt; or &lt;b&gt;bold&lt;/b&gt;.
    /// </summary>
    [Serializable]
    public class DialogueLine
    {
        /// <summary>The character who speaks (name, voice and portraits). Leave empty for narration without name or portrait.</summary>
        [SerializeField] private DialogueCharacter speaker;

        /// <summary>
        /// Speaker written as a name (used by lines imported from .dialogue text files). The runner looks the name up
        /// in the DialogueCharacterLibrary. Ignored when the Speaker reference above is set.
        /// </summary>
        [SerializeField] private string speakerName;

        /// <summary>Expression name (for example "happy"). Empty or unknown names use the character's first portrait.</summary>
        [SerializeField] private string expression = "neutral";

        /// <summary>The text that is typed in the dialogue box.</summary>
        [SerializeField, TextArea(2, 6)] private string text;

        /// <summary>What this line does.</summary>
        [SerializeField] private DialogueLineKind kind = DialogueLineKind.Speech;

        /// <summary>
        /// Text of a non-speech line: the cutscene command ("fade out 0.5"), the flag command ("set key"),
        /// the @if condition ("elder_trust >= 2") or the id of the @goto target. Written without the "@".
        /// </summary>
        [SerializeField] private string command;

        /// <summary>Index of the line to jump to (JumpIfFalse and Jump). A value equal to the line count means "the end".</summary>
        [SerializeField] private int jumpTarget;

        /// <summary>The conversation found for a Goto line (assigned by the importer).</summary>
        [SerializeField] private DialogueSequence gotoSequence;

        /// <summary>The options of a Choice line.</summary>
        [SerializeField] private List<DialogueChoiceOption> options = new List<DialogueChoiceOption>();

        /// <summary>Creates an empty line (used by the Unity serializer and the inspector).</summary>
        public DialogueLine()
        {
        }

        /// <summary>Creates a speech line from text data. Used by the .dialogue importer.</summary>
        /// <param name="speakerName">Name of the speaker; empty for narration.</param>
        /// <param name="expression">Expression name; empty means "neutral".</param>
        /// <param name="text">The line text.</param>
        public DialogueLine(string speakerName, string expression, string text)
        {
            this.speakerName = speakerName;
            this.expression = string.IsNullOrEmpty(expression) ? "neutral" : expression;
            this.text = text;
        }

        /// <summary>Creates a cutscene command line.</summary>
        /// <param name="commandText">The command without the "@".</param>
        /// <returns>The line.</returns>
        public static DialogueLine FromCommand(string commandText)
        {
            return new DialogueLine { kind = DialogueLineKind.Command, command = commandText };
        }

        /// <summary>Creates a flag-change line ("set x" / "add x 1").</summary>
        /// <param name="commandText">The flag command without the "@".</param>
        /// <returns>The line.</returns>
        public static DialogueLine FromFlag(string commandText)
        {
            return new DialogueLine { kind = DialogueLineKind.Flag, command = commandText };
        }

        /// <summary>Creates the start of an @if.</summary>
        /// <param name="condition">The condition text.</param>
        /// <param name="target">Line index to jump to when the condition is false.</param>
        /// <returns>The line.</returns>
        public static DialogueLine FromJumpIfFalse(string condition, int target)
        {
            return new DialogueLine { kind = DialogueLineKind.JumpIfFalse, command = condition, jumpTarget = target };
        }

        /// <summary>Creates an unconditional jump.</summary>
        /// <param name="target">Line index to jump to.</param>
        /// <returns>The line.</returns>
        public static DialogueLine FromJump(int target)
        {
            return new DialogueLine { kind = DialogueLineKind.Jump, jumpTarget = target };
        }

        /// <summary>Creates a jump to another conversation.</summary>
        /// <param name="sequenceId">Id of the target conversation.</param>
        /// <returns>The line.</returns>
        public static DialogueLine FromGoto(string sequenceId)
        {
            return new DialogueLine { kind = DialogueLineKind.Goto, command = sequenceId };
        }

        /// <summary>Creates a choice line.</summary>
        /// <param name="choiceOptions">The options the player can pick.</param>
        /// <returns>The line.</returns>
        public static DialogueLine FromChoice(List<DialogueChoiceOption> choiceOptions)
        {
            return new DialogueLine { kind = DialogueLineKind.Choice, options = choiceOptions };
        }

        /// <summary>
        /// Finds the conversations named by Goto lines and choice options. Called by the importer once all
        /// conversations of the file exist.
        /// </summary>
        /// <param name="find">Returns the conversation with an id, or null.</param>
        public void ResolveGotos(Func<string, DialogueSequence> find)
        {
            if (kind == DialogueLineKind.Goto)
            {
                gotoSequence = find(command);
            }

            foreach (DialogueChoiceOption option in options)
            {
                option.ResolveGoto(find);
            }
        }

        /// <summary>What this line does.</summary>
        public DialogueLineKind Kind => kind;

        /// <summary>True when this line is a cutscene command.</summary>
        public bool IsCommand => kind == DialogueLineKind.Command;

        /// <summary>The command / flag command / condition / goto id (without the "@"), or empty for speech.</summary>
        public string Command => command;

        /// <summary>Line index to jump to (JumpIfFalse and Jump lines).</summary>
        public int JumpTarget => jumpTarget;

        /// <summary>The conversation a Goto line jumps to (null if not found).</summary>
        public DialogueSequence GotoSequence => gotoSequence;

        /// <summary>The options of a Choice line.</summary>
        public IReadOnlyList<DialogueChoiceOption> Options => options;

        /// <summary>The character who speaks this line (can be null).</summary>
        public DialogueCharacter Speaker => speaker;

        /// <summary>Speaker name written in a .dialogue file (can be empty).</summary>
        public string SpeakerName => speakerName;

        /// <summary>Expression name used to choose the portrait.</summary>
        public string Expression => expression;

        /// <summary>The line text, possibly with rich text tags.</summary>
        public string Text => text;
    }
}
