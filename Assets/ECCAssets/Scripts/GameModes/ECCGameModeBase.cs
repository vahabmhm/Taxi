using UnityEngine;

namespace EndlessCarChase.Modes
{
    /// <summary>
    /// Base class of all game modes. The game controller runs the shared gameplay ( driving, spawning, score ),
    /// and the active mode adds its own rules on top of it, like difficulty, goals and victory conditions.
    /// </summary>
    public abstract class ECCGameModeBase : MonoBehaviour
    {
        protected ECCGameController gameController;

        public abstract GameModeType ModeType { get; }

        /// <summary>
        /// Called when the mode is attached to the game controller, before the game starts
        /// </summary>
        public virtual void Initialize(ECCGameController controller)
        {
            gameController = controller;
        }

        /// <summary>
        /// Called once the player has picked a car and the chase begins
        /// </summary>
        public virtual void OnGameStarted() { }

        /// <summary>
        /// Called once when the player car is destroyed
        /// </summary>
        public virtual void OnGameOver() { }

        /// <summary>
        /// When true, the game controller doesn't show its game over canvas, and calls ShowResultScreen instead
        /// </summary>
        public virtual bool ShowsOwnResultScreen
        {
            get { return false; }
        }

        /// <summary>
        /// Shows the mode's own victory / failure screen
        /// </summary>
        public virtual void ShowResultScreen(bool victory) { }

        /// <summary>
        /// The PlayerPrefs key used to save this mode's high score
        /// </summary>
        public virtual string GetHighScoreKey(string sceneKey)
        {
            return sceneKey + ModeType;
        }

        /// <summary>
        /// True while the chase is running ( after the ready-go countdown, before game over )
        /// </summary>
        protected bool IsPlaying
        {
            get { return gameController && gameController.gameStarted && gameController.isGameOver == false && gameController.startDelay <= 0; }
        }
    }
}
