namespace Pathless.Interaction
{
    /// <summary>
    /// Describes HOW an interactable object gets triggered.
    /// Set it on the object (for example in TestInteractable) to choose between an optional chat and a forced one.
    /// </summary>
    public enum InteractionMode
    {
        /// <summary>
        /// The player chooses to interact by pressing the interact button (E, or X / A on a gamepad)
        /// while facing the object. Typical for town NPCs and signs.
        /// </summary>
        Optional = 0,

        /// <summary>
        /// The interaction starts by itself when the player enters a trigger area (see InteractionZone).
        /// Typical for story events and forced dialogues. The interact button is ignored for these objects.
        /// </summary>
        Forced = 1
    }
}
