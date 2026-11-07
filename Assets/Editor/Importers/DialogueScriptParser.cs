using System.Collections.Generic;
using System.Text.RegularExpressions;
using Pathless.Story;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Reads the text of a .dialogue file and turns it into plain data. It knows nothing about Unity assets
    /// (DialogueScriptImporter builds the assets from this data), so the format rules live in one place.
    ///
    /// File format:
    ///   # Area name                      optional title, first line
    ///   // comment                       ignored
    ///   ========                         ignored, only a visual divider between blocks
    ///   [conversation_id | forced]       starts a conversation; the tag is "forced" or "optional" (default optional)
    ///   Name (expression): text          a spoken line; "(expression)" is optional (default neutral)
    ///   > text                           a narration line (no name, portrait or voice)
    ///   @command args                    a cutscene command: fade, wait, move, face, camera, scene (see CutsceneDirector)
    ///   @set flag [value] / @add flag [n]  change a story flag (see StoryFlags)
    ///   @quest start id / @quest complete id   start or finish a quest (see QuestLog)
    ///   @if condition / @else / @end     lines that only play when the condition holds (see StoryCondition)
    ///   @goto id                         continue in another conversation of the same file
    ///   @choice                          the lines below it that start with "- " are options:
    ///   - Option text => @set flag @goto id    the part after "=>" is optional
    ///   more text                        a line that is not "Name:" continues the previous text
    /// </summary>
    public static class DialogueScriptParser
    {
        /// <summary>What a parsed line is (mirrors DialogueLineKind).</summary>
        public enum LineKind
        {
            Speech,
            Command,
            Flag,
            JumpIfFalse,
            Jump,
            Goto,
            Choice
        }

        /// <summary>One option of a choice.</summary>
        public class ParsedOption
        {
            /// <summary>Text shown to the player.</summary>
            public string Label = "";

            /// <summary>Flag commands run when chosen (without "@").</summary>
            public List<string> Actions = new List<string>();

            /// <summary>Conversation id to jump to, or empty.</summary>
            public string GotoId = "";
        }

        /// <summary>One line: speech, or one of the "@" kinds.</summary>
        public class ParsedLine
        {
            /// <summary>What this line is.</summary>
            public LineKind Kind = LineKind.Speech;

            /// <summary>Line number in the file (for error messages).</summary>
            public int SourceLine;

            /// <summary>Index of the line to jump to (JumpIfFalse and Jump); -1 until resolved.</summary>
            public int JumpTarget = -1;

            /// <summary>Options of a Choice line.</summary>
            public List<ParsedOption> Options = new List<ParsedOption>();

            /// <summary>Speaker name; empty for narration.</summary>
            public string SpeakerName = "";

            /// <summary>Expression name; empty means neutral.</summary>
            public string Expression = "";

            /// <summary>The text of the line.</summary>
            public string Text = "";

            /// <summary>Command / flag command / condition / goto id (without "@"); empty for speech.</summary>
            public string Command = "";
        }

        /// <summary>An @if whose @end has not been found yet.</summary>
        public class OpenIf
        {
            /// <summary>Index of the JumpIfFalse line.</summary>
            public int ConditionIndex;

            /// <summary>Index of the Jump line created by @else, or -1 if there is no @else yet.</summary>
            public int ElseJumpIndex = -1;

            /// <summary>Line number of the @if in the file.</summary>
            public int SourceLine;
        }

        /// <summary>One conversation ("[id | tag]" and the lines under it).</summary>
        public class ParsedBlock
        {
            /// <summary>Conversation id (becomes the asset name).</summary>
            public string Id;

            /// <summary>True for the "forced" tag.</summary>
            public bool Forced;

            /// <summary>The lines in order.</summary>
            public List<ParsedLine> Lines = new List<ParsedLine>();

            /// <summary>Line number of the header in the file (for error messages).</summary>
            public int SourceLine;

            /// <summary>The @if blocks that are still open while reading.</summary>
            public Stack<OpenIf> OpenIfs = new Stack<OpenIf>();

            /// <summary>The choice line that is collecting "- " options, or null.</summary>
            public ParsedLine OpenChoice;
        }

        /// <summary>A problem found while reading the file.</summary>
        public class ParseMessage
        {
            /// <summary>1-based line number in the file.</summary>
            public int Line;

            /// <summary>What is wrong.</summary>
            public string Text;

            /// <summary>True for errors, false for warnings.</summary>
            public bool IsError;
        }

        /// <summary>Everything read from a file.</summary>
        public class ParseResult
        {
            /// <summary>Area name from the header (empty if none).</summary>
            public string Area = "";

            /// <summary>The conversations in file order.</summary>
            public List<ParsedBlock> Blocks = new List<ParsedBlock>();

            /// <summary>Errors and warnings.</summary>
            public List<ParseMessage> Messages = new List<ParseMessage>();
        }

        /// <summary>The command words the cutscene system understands (anything else is reported as an error).</summary>
        private static readonly HashSet<string> KnownCommands = new HashSet<string> { "fade", "wait", "move", "face", "camera", "scene" };

        /// <summary>Matches a divider line made of 3 or more "=".</summary>
        private static readonly Regex DividerRegex = new Regex(@"^={3,}$");

        /// <summary>Matches "[id]" or "[id | forced]" / "[id | optional]".</summary>
        private static readonly Regex HeaderRegex = new Regex(@"^\[\s*([A-Za-z0-9_\-]+)\s*(?:\|\s*(forced|optional)\s*)?\]$", RegexOptions.IgnoreCase);

        /// <summary>Matches "Name: text" and "Name (expression): text" (name up to 32 characters).</summary>
        private static readonly Regex SpeakerRegex = new Regex(@"^([^\s:()>\[][^:()]{0,31}?)\s*(?:\(([^()]*)\))?\s*:\s*(\S.*)$");

        /// <summary>Parses the whole file text.</summary>
        /// <param name="fileText">Content of the .dialogue file.</param>
        /// <returns>The conversations and any messages.</returns>
        public static ParseResult Parse(string fileText)
        {
            ParseResult result = new ParseResult();
            HashSet<string> usedIds = new HashSet<string>();
            ParsedBlock current = null;
            string[] rawLines = fileText.Replace("\r\n", "\n").Split('\n');

            for (int i = 0; i < rawLines.Length; i++)
            {
                int lineNumber = i + 1;
                string line = rawLines[i].Trim();

                if (line.Length == 0 || line.StartsWith("//") || DividerRegex.IsMatch(line))
                {
                    continue;
                }

                if (line.StartsWith("#") && current == null)
                {
                    result.Area = line.TrimStart('#').Trim();
                    continue;
                }

                if (line.StartsWith("["))
                {
                    FinishBlock(current, result);
                    current = StartBlock(line, lineNumber, result, usedIds);
                    continue;
                }

                if (current == null)
                {
                    AddMessage(result, lineNumber, "Text found before the first [id] header.", true);
                    continue;
                }

                ReadTextLine(line, lineNumber, current, result);
            }

            FinishBlock(current, result);

            HashSet<string> blockIds = new HashSet<string>();
            foreach (ParsedBlock block in result.Blocks)
            {
                blockIds.Add(block.Id);
            }

            foreach (ParsedBlock block in result.Blocks)
            {
                if (block.Lines.Count == 0)
                {
                    AddMessage(result, block.SourceLine, $"Conversation '{block.Id}' has no lines.", false);
                }

                CheckGotoTargets(block, blockIds, result);
            }

            return result;
        }

        /// <summary>Checks the end of a block: unclosed @if and a @choice without options.</summary>
        private static void FinishBlock(ParsedBlock block, ParseResult result)
        {
            if (block == null)
            {
                return;
            }

            CloseChoice(block, result);
            foreach (OpenIf open in block.OpenIfs)
            {
                AddMessage(result, open.SourceLine, "This @if has no @end.", true);
            }

            block.OpenIfs.Clear();
        }

        /// <summary>Ends the choice that is collecting options; it must have at least one.</summary>
        private static void CloseChoice(ParsedBlock block, ParseResult result)
        {
            if (block.OpenChoice != null && block.OpenChoice.Options.Count == 0)
            {
                AddMessage(result, block.OpenChoice.SourceLine, "@choice has no options (write lines like '- Option text' under it).", true);
            }

            block.OpenChoice = null;
        }

        /// <summary>Reports every "@goto" (and option goto) that names a conversation that is not in the file.</summary>
        private static void CheckGotoTargets(ParsedBlock block, HashSet<string> blockIds, ParseResult result)
        {
            foreach (ParsedLine line in block.Lines)
            {
                if (line.Kind == LineKind.Goto && !blockIds.Contains(line.Command))
                {
                    AddMessage(result, line.SourceLine, $"@goto '{line.Command}': no conversation with that id in this file.", true);
                }

                foreach (ParsedOption option in line.Options)
                {
                    if (option.GotoId.Length > 0 && !blockIds.Contains(option.GotoId))
                    {
                        AddMessage(result, line.SourceLine, $"@goto '{option.GotoId}' of option '{option.Label}': no conversation with that id in this file.", true);
                    }
                }
            }
        }

        /// <summary>Reads a "[id | tag]" header and starts a new block.</summary>
        /// <returns>The new block, or a throw-away block when the header is invalid (so its lines do not cause more errors).</returns>
        private static ParsedBlock StartBlock(string line, int lineNumber, ParseResult result, HashSet<string> usedIds)
        {
            Match match = HeaderRegex.Match(line);
            if (!match.Success)
            {
                AddMessage(result, lineNumber, $"Invalid header '{line}'. Use [id] or [id | forced] / [id | optional].", true);
                return new ParsedBlock { Id = "invalid", SourceLine = lineNumber };
            }

            string id = match.Groups[1].Value;
            if (!usedIds.Add(id))
            {
                AddMessage(result, lineNumber, $"Duplicate conversation id '{id}'.", true);
                return new ParsedBlock { Id = id, SourceLine = lineNumber };
            }

            ParsedBlock block = new ParsedBlock
            {
                Id = id,
                Forced = match.Groups[2].Success && match.Groups[2].Value.ToLowerInvariant() == "forced",
                SourceLine = lineNumber
            };
            result.Blocks.Add(block);
            return block;
        }

        /// <summary>Reads a narration line, a spoken line, or a continuation of the previous text.</summary>
        private static void ReadTextLine(string line, int lineNumber, ParsedBlock block, ParseResult result)
        {
            // Option lines belong to the open @choice; any other line ends the choice.
            if (line.StartsWith("- "))
            {
                ReadOption(line.Substring(2).Trim(), lineNumber, block, result);
                return;
            }

            CloseChoice(block, result);

            if (line.StartsWith("@"))
            {
                ReadAtLine(line.Substring(1).Trim(), lineNumber, block, result);
                return;
            }

            if (line.StartsWith(">"))
            {
                block.Lines.Add(new ParsedLine { Text = line.Substring(1).Trim(), SourceLine = lineNumber });
                return;
            }

            Match match = SpeakerRegex.Match(line);
            if (match.Success)
            {
                block.Lines.Add(new ParsedLine
                {
                    SpeakerName = match.Groups[1].Value.Trim(),
                    Expression = match.Groups[2].Value.Trim(),
                    Text = match.Groups[3].Value.Trim(),
                    SourceLine = lineNumber
                });
                return;
            }

            if (block.Lines.Count == 0)
            {
                AddMessage(result, lineNumber, "Text before the first 'Name: text' line of the conversation.", true);
                return;
            }

            // Not a "Name:" line: it continues the previous text on the same dialogue box.
            ParsedLine previous = block.Lines[block.Lines.Count - 1];
            if (previous.Kind != LineKind.Speech)
            {
                AddMessage(result, lineNumber, "Text after an @line must start with 'Name:' (or '>' for narration).", true);
                return;
            }

            previous.Text += " " + line;
        }

        /// <summary>Reads a line that starts with "@": a command, flag change, @if/@else/@end, @goto or @choice.</summary>
        /// <param name="text">The line without the "@".</param>
        private static void ReadAtLine(string text, int lineNumber, ParsedBlock block, ParseResult result)
        {
            string[] words = text.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            string verb = words.Length > 0 ? words[0].ToLowerInvariant() : "";
            string rest = words.Length > 1 ? text.Substring(words[0].Length).Trim() : "";
            string error;

            switch (verb)
            {
                case "set":
                case "add":
                case "quest":
                    if (!StoryFlags.Validate(text, out error))
                    {
                        AddMessage(result, lineNumber, $"@{verb}: {error}", true);
                        return;
                    }

                    block.Lines.Add(new ParsedLine { Kind = LineKind.Flag, Command = text, SourceLine = lineNumber });
                    return;

                case "if":
                    if (!StoryCondition.Validate(rest, out error))
                    {
                        AddMessage(result, lineNumber, $"@if: {error}", true);
                        return;
                    }

                    block.OpenIfs.Push(new OpenIf { ConditionIndex = block.Lines.Count, SourceLine = lineNumber });
                    block.Lines.Add(new ParsedLine { Kind = LineKind.JumpIfFalse, Command = rest, SourceLine = lineNumber });
                    return;

                case "else":
                    if (block.OpenIfs.Count == 0 || block.OpenIfs.Peek().ElseJumpIndex >= 0)
                    {
                        AddMessage(result, lineNumber, "@else without a matching @if.", true);
                        return;
                    }

                    // The part before @else ends with a jump over the else part; a false condition lands right after it.
                    OpenIf openIf = block.OpenIfs.Peek();
                    openIf.ElseJumpIndex = block.Lines.Count;
                    block.Lines.Add(new ParsedLine { Kind = LineKind.Jump, SourceLine = lineNumber });
                    block.Lines[openIf.ConditionIndex].JumpTarget = block.Lines.Count;
                    return;

                case "end":
                    if (block.OpenIfs.Count == 0)
                    {
                        AddMessage(result, lineNumber, "@end without a matching @if.", true);
                        return;
                    }

                    OpenIf closing = block.OpenIfs.Pop();
                    int endIndex = block.Lines.Count;
                    if (closing.ElseJumpIndex >= 0)
                    {
                        block.Lines[closing.ElseJumpIndex].JumpTarget = endIndex;
                    }
                    else
                    {
                        block.Lines[closing.ConditionIndex].JumpTarget = endIndex;
                    }

                    return;

                case "goto":
                    if (words.Length != 2)
                    {
                        AddMessage(result, lineNumber, "use '@goto conversation_id'.", true);
                        return;
                    }

                    block.Lines.Add(new ParsedLine { Kind = LineKind.Goto, Command = words[1], SourceLine = lineNumber });
                    return;

                case "choice":
                    if (words.Length != 1)
                    {
                        AddMessage(result, lineNumber, "'@choice' takes no arguments; write the options on the lines below it.", true);
                        return;
                    }

                    ParsedLine choice = new ParsedLine { Kind = LineKind.Choice, SourceLine = lineNumber };
                    block.Lines.Add(choice);
                    block.OpenChoice = choice;
                    return;
            }

            if (!KnownCommands.Contains(verb))
            {
                AddMessage(result, lineNumber, $"Unknown command '@{verb}'. Known: fade, wait, move, face, camera, scene, set, add, quest, if, else, end, goto, choice.", true);
                return;
            }

            block.Lines.Add(new ParsedLine { Kind = LineKind.Command, Command = text, SourceLine = lineNumber });
        }

        /// <summary>Reads one "- text => @set flag @goto id" option line.</summary>
        /// <param name="text">The line without the leading "- ".</param>
        private static void ReadOption(string text, int lineNumber, ParsedBlock block, ParseResult result)
        {
            if (block.OpenChoice == null)
            {
                AddMessage(result, lineNumber, "An option line ('- text') must come right after '@choice' (or another option).", true);
                return;
            }

            ParsedOption option = new ParsedOption();
            int arrow = text.LastIndexOf("=>", System.StringComparison.Ordinal);
            string actionText = "";
            if (arrow >= 0)
            {
                option.Label = text.Substring(0, arrow).Trim();
                actionText = text.Substring(arrow + 2).Trim();
            }
            else
            {
                option.Label = text;
            }

            if (option.Label.Length == 0)
            {
                AddMessage(result, lineNumber, "The option has no text.", true);
                return;
            }

            // Actions look like "@set a @add b 1 @goto id": split at each "@".
            foreach (string piece in actionText.Split('@'))
            {
                string action = piece.Trim();
                if (action.Length == 0)
                {
                    continue;
                }

                string[] words = action.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                string verb = words[0].ToLowerInvariant();
                if (verb == "goto")
                {
                    if (words.Length != 2 || option.GotoId.Length > 0)
                    {
                        AddMessage(result, lineNumber, "an option may have one '@goto conversation_id'.", true);
                        return;
                    }

                    option.GotoId = words[1];
                }
                else if ((verb == "set" || verb == "add" || verb == "quest") && StoryFlags.Validate(action, out string error))
                {
                    option.Actions.Add(action);
                }
                else
                {
                    AddMessage(result, lineNumber, $"option action '@{action}' is not valid (use @set, @add, @quest or @goto).", true);
                    return;
                }
            }

            block.OpenChoice.Options.Add(option);
        }

        /// <summary>Adds an error or warning to the result.</summary>
        private static void AddMessage(ParseResult result, int line, string text, bool isError)
        {
            result.Messages.Add(new ParseMessage { Line = line, Text = text, IsError = isError });
        }
    }
}
