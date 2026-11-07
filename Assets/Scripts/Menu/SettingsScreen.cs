using Pathless.Audio;
using Pathless.Settings;
using UnityEngine;

namespace Pathless.Menu
{
    /// <summary>
    /// The Settings screen: master, music and effects volume (bars changed with Left / Right in steps of 10%) and the
    /// dialogue text speed (Slow / Normal / Fast). Every change is applied and saved at once (GameSettings.Save).
    /// </summary>
    public class SettingsScreen : MenuScreen
    {
        /// <summary>Number of options (rows): master, music, effects, text speed.</summary>
        private const int RowCount = 4;

        /// <summary>How much a volume changes with one press.</summary>
        private const float VolumeStep = 0.1f;

        /// <summary>Number of blocks drawn in a volume bar.</summary>
        private const int BarBlocks = 10;

        /// <summary>The four rows, in order: master, music, effects, text speed.</summary>
        [SerializeField] private SettingsRow[] rows;

        /// <summary>Name (in the AudioLibrary) of the short sound played as a preview when a volume changes.</summary>
        private const string PreviewSound = "Select";

        /// <summary>Index of the selected row.</summary>
        private int selected;

        /// <summary>Plays the preview sound. Found in the scene when the screen opens (null if the scene has none).</summary>
        private SfxPlayer sfxPlayer;

        /// <summary>Names the rows and shows the current values each time the screen opens.</summary>
        protected override void OnOpening()
        {
            selected = 0;
            sfxPlayer = FindFirstObjectByType<SfxPlayer>();
            rows[0].SetName("Master volume");
            rows[1].SetName("Music volume");
            rows[2].SetName("Effects volume");
            rows[3].SetName("Text speed");
            Refresh();
        }

        /// <summary>Selects the row above.</summary>
        public override void OnUp()
        {
            selected = (selected + RowCount - 1) % RowCount;
            Refresh();
        }

        /// <summary>Selects the row below.</summary>
        public override void OnDown()
        {
            selected = (selected + 1) % RowCount;
            Refresh();
        }

        /// <summary>Lowers the volume or picks the previous text speed.</summary>
        public override void OnLeft()
        {
            Change(-1);
        }

        /// <summary>Raises the volume or picks the next text speed.</summary>
        public override void OnRight()
        {
            Change(1);
        }

        /// <summary>Changes the selected option by one step, saves, and redraws.</summary>
        /// <param name="direction">-1 for less, +1 for more.</param>
        private void Change(int direction)
        {
            switch (selected)
            {
                case 0:
                    GameVolume.Master = Step(GameVolume.Master, direction);
                    break;
                case 1:
                    GameVolume.Music = Step(GameVolume.Music, direction);
                    break;
                case 2:
                    GameVolume.Sfx = Step(GameVolume.Sfx, direction);
                    break;
                default:
                    GameSettings.TextSpeed += direction;
                    break;
            }

            GameSettings.Save();
            Refresh();

            // Volume rows play a short sound so the player hears the new level (music already changes live).
            if (selected <= 2 && sfxPlayer != null)
            {
                sfxPlayer.Play(PreviewSound);
            }
        }

        /// <summary>Moves a volume by one step, rounded to a multiple of the step (avoids 0.30000001 drift).</summary>
        private static float Step(float volume, int direction)
        {
            float steps = Mathf.Round(volume / VolumeStep) + direction;
            return Mathf.Clamp01(steps * VolumeStep);
        }

        /// <summary>Redraws every row (value text and highlight).</summary>
        private void Refresh()
        {
            rows[0].SetValue(VolumeText(GameVolume.Master));
            rows[1].SetValue(VolumeText(GameVolume.Music));
            rows[2].SetValue(VolumeText(GameVolume.Sfx));
            rows[3].SetValue("< " + GameSettings.TextSpeedNames[GameSettings.TextSpeed] + " >");

            for (int i = 0; i < RowCount; i++)
            {
                rows[i].SetSelected(i == selected);
            }
        }

        /// <summary>Builds the text of a volume bar, for example "[#######---]  70%".</summary>
        private static string VolumeText(float volume)
        {
            int filled = Mathf.RoundToInt(volume * BarBlocks);
            return "[" + new string('#', filled) + new string('-', BarBlocks - filled) + "]  " + Mathf.RoundToInt(volume * 100f) + "%";
        }
    }
}
