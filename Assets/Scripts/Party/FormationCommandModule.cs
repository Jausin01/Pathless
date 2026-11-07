using System.Collections;
using Pathless.Dialogue;

namespace Pathless.Party
{
    /// <summary>
    /// The formation rules of dialogue files:
    ///   @formation require id   the member must go in the party (cannot be unchecked on the formation screen)
    ///   @formation absent id    the member is not available now
    ///   @formation free id      removes both rules of the member (available and optional)
    ///   @formation reset        removes the rules of every member
    /// After a rule changes, the active party is corrected right away (absent members leave, required ones join).
    /// </summary>
    public class FormationCommandModule : CommandModule
    {
        /// <summary>Handles "formation".</summary>
        public override bool Handles(string verb)
        {
            return verb == "formation";
        }

        /// <summary>Runs one command (it finishes instantly).</summary>
        public override IEnumerator Run(string[] words, string command)
        {
            PartyRoster roster = PartyRegistry.Roster;
            if (roster != null)
            {
                RunFormation(words, command, roster);
            }

            yield break;
        }

        /// <summary>Applies the command and then fixes the active party so it follows the rules.</summary>
        private void RunFormation(string[] words, string command, PartyRoster roster)
        {
            if (words.Length < 2)
            {
                Warn(command, "use '@formation require id', 'absent id', 'free id' or 'reset'.");
                return;
            }

            string action = words[1].ToLowerInvariant();
            if (action == "reset")
            {
                FormationRules.ClearAll(roster);
            }
            else if (action == "require" || action == "absent" || action == "free")
            {
                if (words.Length != 3)
                {
                    Warn(command, $"use '@formation {action} id'.");
                    return;
                }

                PartyMember member = roster.FindById(words[2]);
                if (member == null)
                {
                    Warn(command, $"no party member with id '{words[2]}'.");
                    return;
                }

                if (action == "require")
                {
                    FormationRules.SetRequired(member.MemberId, true);
                }
                else if (action == "absent")
                {
                    if (member.MemberId == FormationRules.ProtagonistId)
                    {
                        Warn(command, "the protagonist cannot be absent.");
                        return;
                    }

                    FormationRules.SetAbsent(member.MemberId, true);
                }
                else
                {
                    FormationRules.SetRequired(member.MemberId, false);
                    FormationRules.SetAbsent(member.MemberId, false);
                }
            }
            else
            {
                Warn(command, "unknown action. Use require, absent, free or reset.");
                return;
            }

            PartyFormation.SetActive(roster, FormationRules.Apply(roster, PartyFormation.GetActive(roster)));
        }
    }
}
