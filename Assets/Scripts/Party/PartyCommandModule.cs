using System.Collections;
using System.Collections.Generic;
using Pathless.Dialogue;
using UnityEngine;

namespace Pathless.Party
{
    /// <summary>
    /// The party commands of dialogue files:
    ///   @party set id id ...   the active party becomes exactly these members (up to 4, in order)
    ///   @party add id          adds a member to the active party (if there is room)
    ///   @party remove id       removes a member from the active party
    ///   @party reset           back to the default party (the first members of the roster)
    ///   @bond id id amount     changes the bond between two members (amount can be negative; never below 0)
    /// Ids are the "memberId" of each PartyMember (for example "alice"). Everything is stored in StoryFlags through
    /// PartyFormation and PartyBonds, so it is saved with the story.
    /// </summary>
    public class PartyCommandModule : CommandModule
    {
        /// <summary>Handles "party" and "bond".</summary>
        public override bool Handles(string verb)
        {
            return verb == "party" || verb == "bond";
        }

        /// <summary>Runs one command (it finishes instantly).</summary>
        public override IEnumerator Run(string[] words, string command)
        {
            PartyRoster roster = PartyRegistry.Roster;
            if (roster != null)
            {
                if (words[0].ToLowerInvariant() == "party")
                {
                    RunParty(words, command, roster);
                }
                else
                {
                    RunBond(words, command, roster);
                }
            }

            yield break;
        }

        /// <summary>Runs "@party set|add|remove|reset".</summary>
        private void RunParty(string[] words, string command, PartyRoster roster)
        {
            if (words.Length < 2)
            {
                Warn(command, "use '@party set id id', '@party add id', '@party remove id' or '@party reset'.");
                return;
            }

            string action = words[1].ToLowerInvariant();
            List<PartyMember> active = PartyFormation.GetActive(roster);

            switch (action)
            {
                case "reset":
                    PartyFormation.Clear();
                    return;

                case "set":
                    List<PartyMember> chosen = new List<PartyMember>();
                    for (int i = 2; i < words.Length; i++)
                    {
                        PartyMember member = Find(roster, words[i], command);
                        if (member == null)
                        {
                            return;
                        }

                        if (!chosen.Contains(member))
                        {
                            chosen.Add(member);
                        }
                    }

                    if (chosen.Count == 0 || chosen.Count > PartyRoster.MaxSlots)
                    {
                        Warn(command, $"the party needs between 1 and {PartyRoster.MaxSlots} members.");
                        return;
                    }

                    PartyFormation.SetActive(roster, chosen);
                    return;

                case "add":
                case "remove":
                    if (words.Length != 3)
                    {
                        Warn(command, $"use '@party {action} id'.");
                        return;
                    }

                    PartyMember target = Find(roster, words[2], command);
                    if (target == null)
                    {
                        return;
                    }

                    if (action == "add" && !active.Contains(target))
                    {
                        if (active.Count >= PartyRoster.MaxSlots)
                        {
                            Warn(command, $"the party is full ({PartyRoster.MaxSlots} members).");
                            return;
                        }

                        active.Add(target);
                    }
                    else if (action == "remove")
                    {
                        if (active.Count <= 1 && active.Contains(target))
                        {
                            Warn(command, "the party cannot be left empty.");
                            return;
                        }

                        active.Remove(target);
                    }

                    PartyFormation.SetActive(roster, active);
                    return;

                default:
                    Warn(command, "unknown action. Use set, add, remove or reset.");
                    return;
            }
        }

        /// <summary>Runs "@bond id id amount".</summary>
        private void RunBond(string[] words, string command, PartyRoster roster)
        {
            if (words.Length != 4 || !int.TryParse(words[3], out int amount))
            {
                Warn(command, "use '@bond id id amount' (for example '@bond alice gabe 2').");
                return;
            }

            PartyMember first = Find(roster, words[1], command);
            PartyMember second = Find(roster, words[2], command);
            if (first == null || second == null)
            {
                return;
            }

            if (first == second)
            {
                Warn(command, "a bond needs two different members.");
                return;
            }

            PartyBonds.Add(first, second, amount);
        }

        /// <summary>Finds a member by id and warns when it does not exist.</summary>
        private PartyMember Find(PartyRoster roster, string id, string command)
        {
            PartyMember member = roster.FindById(id);
            if (member == null)
            {
                Warn(command, $"no party member with id '{id}'.");
            }

            return member;
        }
    }
}
