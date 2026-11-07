using System.Collections.Generic;
using System.IO;
using Pathless.Dialogue;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Makes Unity understand ".dialogue" text files. Saving a file re-imports it: the file becomes a
    /// DialogueScript asset with one DialogueSequence sub-asset per [id] block (named after the id).
    /// Drag a sub-asset (expand the file in the Project window) into a DialogueInteractable.
    /// Problems are reported in the Console with the file name and line number.
    /// </summary>
    [ScriptedImporter(2, "dialogue")]
    public class DialogueScriptImporter : ScriptedImporter
    {
        /// <summary>Reads the file and creates the assets.</summary>
        /// <param name="ctx">Import context given by Unity.</param>
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Re-import when the character list changes, so unknown-name warnings stay correct.
            ctx.DependsOnSourceAsset(DialogueCharacterLibraryBuilder.LibraryPath);

            DialogueScriptParser.ParseResult parsed = DialogueScriptParser.Parse(File.ReadAllText(ctx.assetPath));
            ReportMessages(ctx, parsed);

            DialogueCharacterLibrary library = AssetDatabase.LoadAssetAtPath<DialogueCharacterLibrary>(DialogueCharacterLibraryBuilder.LibraryPath);
            List<DialogueSequence> sequences = new List<DialogueSequence>();

            foreach (DialogueScriptParser.ParsedBlock block in parsed.Blocks)
            {
                DialogueSequence sequence = ScriptableObject.CreateInstance<DialogueSequence>();
                sequence.name = block.Id;
                sequence.Setup(
                    block.Forced ? Pathless.Interaction.InteractionMode.Forced : Pathless.Interaction.InteractionMode.Optional,
                    BuildLines(ctx, block, library));

                // The identifier keeps the asset reference stable when the file is edited and re-imported.
                ctx.AddObjectToAsset(block.Id, sequence);
                sequences.Add(sequence);
            }

            // Second pass: now that every conversation exists, "@goto" targets can be linked by id.
            Dictionary<string, DialogueSequence> byId = new Dictionary<string, DialogueSequence>();
            foreach (DialogueSequence created in sequences)
            {
                byId[created.name] = created;
            }

            foreach (DialogueSequence created in sequences)
            {
                foreach (DialogueLine createdLine in created.Lines)
                {
                    createdLine.ResolveGotos(id => byId.TryGetValue(id, out DialogueSequence target) ? target : null);
                }
            }

            DialogueScript script = ScriptableObject.CreateInstance<DialogueScript>();
            script.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            script.Setup(parsed.Area, sequences);
            ctx.AddObjectToAsset("main", script);
            ctx.SetMainObject(script);
        }

        /// <summary>Converts parsed lines to DialogueLine objects, warning about names missing from the library.</summary>
        private static List<DialogueLine> BuildLines(AssetImportContext ctx, DialogueScriptParser.ParsedBlock block, DialogueCharacterLibrary library)
        {
            List<DialogueLine> lines = new List<DialogueLine>();
            foreach (DialogueScriptParser.ParsedLine parsedLine in block.Lines)
            {
                switch (parsedLine.Kind)
                {
                    case DialogueScriptParser.LineKind.Command:
                        lines.Add(DialogueLine.FromCommand(parsedLine.Command));
                        continue;

                    case DialogueScriptParser.LineKind.Flag:
                        lines.Add(DialogueLine.FromFlag(parsedLine.Command));
                        continue;

                    case DialogueScriptParser.LineKind.JumpIfFalse:
                        lines.Add(DialogueLine.FromJumpIfFalse(parsedLine.Command, parsedLine.JumpTarget));
                        continue;

                    case DialogueScriptParser.LineKind.Jump:
                        lines.Add(DialogueLine.FromJump(parsedLine.JumpTarget));
                        continue;

                    case DialogueScriptParser.LineKind.Goto:
                        lines.Add(DialogueLine.FromGoto(parsedLine.Command));
                        continue;

                    case DialogueScriptParser.LineKind.Choice:
                        List<DialogueChoiceOption> options = new List<DialogueChoiceOption>();
                        foreach (DialogueScriptParser.ParsedOption parsedOption in parsedLine.Options)
                        {
                            options.Add(new DialogueChoiceOption(parsedOption.Label, parsedOption.Actions, parsedOption.GotoId));
                        }

                        lines.Add(DialogueLine.FromChoice(options));
                        continue;
                }

                if (library != null && parsedLine.SpeakerName.Length > 0 && library.Find(parsedLine.SpeakerName) == null)
                {
                    ctx.LogImportWarning($"{ctx.assetPath}: [{block.Id}] unknown character '{parsedLine.SpeakerName}'. Add the character and use Tools > Pathless > Dialogue > Refresh Character Library.");
                }

                lines.Add(new DialogueLine(parsedLine.SpeakerName, parsedLine.Expression, parsedLine.Text));
            }

            return lines;
        }

        /// <summary>Prints parser errors and warnings with file name and line number.</summary>
        private static void ReportMessages(AssetImportContext ctx, DialogueScriptParser.ParseResult parsed)
        {
            foreach (DialogueScriptParser.ParseMessage message in parsed.Messages)
            {
                string text = $"{ctx.assetPath}({message.Line}): {message.Text}";
                if (message.IsError)
                {
                    ctx.LogImportError(text);
                }
                else
                {
                    ctx.LogImportWarning(text);
                }
            }
        }
    }
}
