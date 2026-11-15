using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// When the player car is wrecked, offers to continue once per run: watch a rewarded video ( through ECCAds: loading popup,
    /// NoAds panel, verified rewards only ) or pay coins. The run is frozen meanwhile.
    /// Reviving restores full health, clears the police around the player and gives a few seconds of protection.
    /// The daily challenge uses its own once-per-day second chance instead ( ECCDailyChallengeMode ).
    /// </summary>
    public class ECCRevive : MonoBehaviour
    {
        [Tooltip("Seconds to decide before the game ends")]
        public float decisionTime = 8;

        [Tooltip("The rewarded video placement name")]
        public string adPlacement = "revive";

        public float protectionTime = 3;
        public float clearRadius = 12;

        ECCGameController gameController;
        ECCCar player;
        bool usedThisRun = false;
        bool offerOpen = false;

        /// <summary>
        /// Called by the player car when it's wrecked. Returns true if the revive offer is shown ( the car must not die yet )
        /// </summary>
        public static bool TryOffer(ECCGameController gameController, ECCCar player)
        {
            // The daily challenge has its own once-per-day rewarded second chance
            EndlessCarChase.Modes.ECCDailyChallengeMode challenge = gameController.activeMode as EndlessCarChase.Modes.ECCDailyChallengeMode;

            if (challenge) return challenge.OfferWreckContinue(player);

            ECCRevive revive = gameController.GetComponent<ECCRevive>();

            if (revive == null) revive = gameController.gameObject.AddComponent<ECCRevive>();

            return revive.Offer(gameController, player);
        }

        bool Offer(ECCGameController controller, ECCCar car)
        {
            if (usedThisRun || offerOpen || controller.isGameOver) return false;

            usedThisRun = true;
            offerOpen = true;
            gameController = controller;
            player = car;

            Time.timeScale = 0;

            ECCContinueOfferUI.Show(new ECCContinueOfferUI.Options
            {
                title = "ماشینت نابود شد!",
                reason = "ادامه می‌دهی؟",
                videoLabel = "تماشای ویدئو و ادامه با ماشین تعمیرشده",
                videoTag = "ادامه",
                placement = adPlacement,
                coinPrice = Mathf.Max(0, ECCEconomyConfig.Instance.reviveCoinPrice),
                coinLabel = "ادامه با",
                decisionTime = decisionTime,
                onVideoRewarded = Revive,
                onPaid = Revive,
                onDeclined = Decline,
            });

            return true;
        }

        void Revive()
        {
            offerOpen = false;

            Time.timeScale = 1;

            if (player == null) return;

            RestoreCar(player, protectionTime, clearRadius);

            ECCHintBanner.Show("Icons/play", ECCRuntimeUI.GoodColor, "دوباره در جاده!", "چند ثانیه از آسیب در امانی", 2);
        }

        void Decline()
        {
            offerOpen = false;

            Time.timeScale = 1;

            if (player) player.FinishPlayerDeath();
        }

        /// <summary>
        /// Puts a wrecked player car back on the road: full health, a few seconds of protection, and the police right
        /// around it cleared so it isn't wrecked again instantly
        /// </summary>
        public static void RestoreCar(ECCCar player, float protection = 3, float radius = 12)
        {
            if (player == null) return;

            player.ChangeHealth(player.healthMax);
            player.invulnerableTime = protection;

            ECCPowerUpManager.EmpBlast(player.transform.position, radius);
        }
    }
}
