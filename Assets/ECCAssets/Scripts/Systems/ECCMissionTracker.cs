using UnityEngine;
using EndlessCarChase.Modes;

namespace EndlessCarChase
{
    /// <summary>
    /// Listens to the game events during a run and reports them to the daily missions.
    /// Added to the game controller when the game starts ( once per run, the scene reloads between runs ).
    /// Every event is only counted while the chase is running, so nothing is counted twice or after game over.
    /// </summary>
    public class ECCMissionTracker : MonoBehaviour
    {
        ECCGameController gameController;

        float runTime = 0;
        int reportedSeconds = 0;
        int reportedWantedLevel = 0;
        int reportedScore = 0;
        int copsThisRun = 0;
        bool finalReported = false;

        void Start()
        {
            gameController = GetComponent<ECCGameController>();

            ECCCar.CarDestroyed += OnCarDestroyed;
            ECCItem.Collected += OnItemCollected;
            ECCComboSystem.NearMiss += OnNearMiss;
            ECCPowerUpManager.PowerUpUsed += OnPowerUpUsed;

            // The tracker is added when a chase starts
            ECCMissions.Report(ECCMissionType.PlayRuns, 1);
        }

        void OnDestroy()
        {
            ECCCar.CarDestroyed -= OnCarDestroyed;
            ECCItem.Collected -= OnItemCollected;
            ECCComboSystem.NearMiss -= OnNearMiss;
            ECCPowerUpManager.PowerUpUsed -= OnPowerUpUsed;
        }

        bool IsPlaying
        {
            get { return gameController && gameController.gameStarted && gameController.isGameOver == false && gameController.startDelay <= 0; }
        }

        void Update()
        {
            if (gameController == null) return;

            // When the run ends, report its final money once
            if (gameController.isGameOver)
            {
                if (finalReported == false)
                {
                    finalReported = true;

                    ECCMissions.Report(ECCMissionType.EarnInOneRun, gameController.score);
                }

                return;
            }

            if (IsPlaying == false) return;

            runTime += Time.deltaTime;

            // Report the survival time every 10 seconds, so the save isn't written every frame
            int seconds = Mathf.FloorToInt(runTime / 10) * 10;

            if (seconds > reportedSeconds)
            {
                reportedSeconds = seconds;

                ECCMissions.Report(ECCMissionType.SurviveSeconds, seconds);
            }

            // The run's money, in steps of 50
            if (gameController.score >= reportedScore + 50)
            {
                reportedScore = gameController.score - gameController.score % 50;

                ECCMissions.Report(ECCMissionType.EarnInOneRun, gameController.score);
            }

            ECCEndlessMode endlessMode = gameController.activeMode as ECCEndlessMode;

            if (endlessMode && endlessMode.WantedLevel > reportedWantedLevel)
            {
                reportedWantedLevel = endlessMode.WantedLevel;

                ECCMissions.Report(ECCMissionType.ReachWantedLevel, reportedWantedLevel);
            }
        }

        void OnCarDestroyed(ECCCar car)
        {
            if (IsPlaying == false || car.IsPlayer) return;

            copsThisRun++;

            ECCMissions.Report(ECCMissionType.DestroyCops, 1);
            ECCMissions.Report(ECCMissionType.DestroyCopsInOneRun, copsThisRun);
        }

        void OnItemCollected(ECCItem item)
        {
            if (IsPlaying && item.CashValue > 0) ECCMissions.Report(ECCMissionType.CollectCash, item.CashValue);
        }

        void OnNearMiss()
        {
            if (IsPlaying) ECCMissions.Report(ECCMissionType.NearMisses, 1);
        }

        void OnPowerUpUsed(ECCPowerUpType type)
        {
            if (IsPlaying) ECCMissions.Report(ECCMissionType.UsePowerUps, 1);
        }
    }
}
