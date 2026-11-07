namespace Pathless.Npc
{
    /// <summary>The kinds of icon shown above a character or object that can be interacted with.</summary>
    public enum InteractionIconType
    {
        /// <summary>No icon.</summary>
        None,

        /// <summary>Speech bubble with three dots: this character has something to talk about.</summary>
        Talk,

        /// <summary>Speech bubble with a quest mark: this character can give (or has) a quest.</summary>
        Quest,

        /// <summary>Typewriter: a place where the game can be saved.</summary>
        Save
    }
}
