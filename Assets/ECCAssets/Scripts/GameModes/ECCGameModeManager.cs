using System;
using UnityEngine;

namespace EndlessCarChase.Modes
{
    /// <summary>
    /// The game modes the player can choose from
    /// </summary>
    public enum GameModeType
    {
        Endless = 0,
        Levels = 1,
        DailyChallenge = 2, // Only for the current session, started from the daily challenge popup. Never saved
    }

    /// <summary>
    /// Remembers which game mode the player selected ( across scene reloads and app restarts ),
    /// and attaches the matching mode component to the game controller.
    /// </summary>
    public static class ECCGameModeManager
    {
        const string ModePlayerPrefs = "SelectedGameMode";

        // When true, the next scene load starts the chase right away ( retry / next level )
        public static bool autoStartOnLoad = false;

        /// <summary>
        /// Called when the player switches to another game mode
        /// </summary>
        public static event Action<GameModeType> ModeChanged;

        /// <summary>
        /// The game mode the player selected. Falls back to the endless mode if the saved mode isn't available
        /// </summary>
        public static GameModeType CurrentMode
        {
            get
            {
                GameModeType savedMode = (GameModeType)PlayerPrefs.GetInt(ModePlayerPrefs, (int)GameModeType.Endless);

                return IsModeAvailable(savedMode) ? savedMode : GameModeType.Endless;
            }
            set
            {
                if (IsModeAvailable(value) == false || value == CurrentMode) return;

                PlayerPrefs.SetInt(ModePlayerPrefs, (int)value);
                PlayerPrefs.Save();

                if (ModeChanged != null) ModeChanged(value);
            }
        }

        /// <summary>
        /// Whether a mode can be played
        /// </summary>
        public static bool IsModeAvailable(GameModeType mode)
        {
            switch (mode)
            {
                case GameModeType.Endless:
                case GameModeType.Levels:
                    return true;
                default: return false;
            }
        }

        /// <summary>
        /// Enables the component of the current mode on the game controller object and disables the others.
        /// A mode component that was added in the Inspector is reused ( so its settings can be tweaked there ),
        /// otherwise one is added with default settings.
        /// </summary>
        public static ECCGameModeBase AttachMode(ECCGameController gameController)
        {
            GameModeType mode = ECCDailyChallenge.PlayRequested ? GameModeType.DailyChallenge : CurrentMode;

            ECCGameModeBase activeMode = null;

            foreach (ECCGameModeBase modeComponent in gameController.GetComponents<ECCGameModeBase>())
            {
                if (modeComponent.ModeType == mode && activeMode == null) activeMode = modeComponent;
                else modeComponent.enabled = false;
            }

            if (activeMode == null) activeMode = AddModeComponent(gameController.gameObject, mode);

            activeMode.enabled = true;
            activeMode.Initialize(gameController);

            return activeMode;
        }

        static ECCGameModeBase AddModeComponent(GameObject target, GameModeType mode)
        {
            switch (mode)
            {
                case GameModeType.Levels: return target.AddComponent<ECCLevelMode>();
                case GameModeType.DailyChallenge: return target.AddComponent<ECCDailyChallengeMode>();
                default: return target.AddComponent<ECCEndlessMode>();
            }
        }
    }
}
