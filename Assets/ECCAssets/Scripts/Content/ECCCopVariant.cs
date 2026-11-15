using UnityEngine;

namespace EndlessCarChase
{
    public enum ECCCopType
    {
        Normal = 0,
        Interceptor = 1, // Fast and fragile
        Rammer = 2,      // Charges at the player when lined up
        Heavy = 3,       // Slow, big and armored
        Boss = 4,        // The armored truck of the boss levels
    }

    /// <summary>
    /// Turns a regular police car into a variant with different stats, size, color and behavior.
    /// Apply it right after the car is created, before its Start runs, so the new health becomes its maximum health.
    /// </summary>
    public class ECCCopVariant : MonoBehaviour
    {
        public ECCCopType type = ECCCopType.Normal;

        [Header("Rammer Charge")]
        public float chargeDistance = 14;
        public float chargeAngle = 25;
        public float chargeSpeedMultiplier = 1.9f;
        public float chargeDuration = 1.2f;
        public float chargeCooldown = 4;

        ECCCar car;
        float baseSpeed = -1;
        float chargeTime = 0;
        float cooldownTime = 2;

        /// <summary>
        /// Picks a variant at random using the given chances, and applies it
        /// </summary>
        public static ECCCopType RollAndApply(ECCCar car, float interceptorChance, float rammerChance, float heavyChance)
        {
            float roll = Random.value;

            ECCCopType type = ECCCopType.Normal;

            if (roll < interceptorChance) type = ECCCopType.Interceptor;
            else if (roll < interceptorChance + rammerChance) type = ECCCopType.Rammer;
            else if (roll < interceptorChance + rammerChance + heavyChance) type = ECCCopType.Heavy;

            if (type != ECCCopType.Normal) Apply(car, type);

            return type;
        }

        /// <summary>
        /// Makes a new police car tough enough for the player's car: at least policeHealthPerPlayerDamage x the player's ram
        /// damage ( about two rams ), plus policeHealthPerChapter for each chapter after the first.
        /// Call it right after the car is created, before RollAndApply, so the variants scale from the toughened health
        /// </summary>
        public static void Toughen(ECCCar car, int chapterIndex)
        {
            ECCEconomyConfig config = ECCEconomyConfig.Instance;

            ECCGameController gameController = ECCGameController.Instance;
            ECCCar player = gameController ? gameController.playerObject : null;

            if (player) car.health = Mathf.Max(car.health, player.damage * config.policeHealthPerPlayerDamage);

            car.health *= 1 + Mathf.Clamp(Mathf.Max(0, chapterIndex) * config.policeHealthPerChapter, 0, Mathf.Max(0, config.policeHealthChapterCap));
        }

        /// <summary>
        /// The boss takes at least bossHitsOfPlayerDamage average rams of the player's car. Call it after Apply(Boss)
        /// </summary>
        public static void ToughenBoss(ECCCar boss)
        {
            ECCGameController gameController = ECCGameController.Instance;
            ECCCar player = gameController ? gameController.playerObject : null;

            if (player) boss.health = Mathf.Max(boss.health, player.damage * ECCEconomyConfig.Instance.bossHitsOfPlayerDamage);
        }

        public static ECCCopVariant Apply(ECCCar car, ECCCopType type)
        {
            ECCCopVariant variant = car.gameObject.AddComponent<ECCCopVariant>();
            variant.type = type;
            variant.car = car;

            switch (type)
            {
                case ECCCopType.Interceptor:
                    car.speed *= 1.3f;
                    car.health = Mathf.Max(1, car.health * 0.5f);
                    Resize(car, 0.9f);
                    Tint(car, new Color(1, 0.55f, 0.55f));
                    break;

                case ECCCopType.Rammer:
                    car.speed *= 1.05f;
                    Tint(car, new Color(1, 0.75f, 0.35f));
                    break;

                case ECCCopType.Heavy:
                    car.speed *= 0.85f;
                    car.health *= 2.5f;
                    car.damage *= 2;
                    Resize(car, 1.25f);
                    Tint(car, new Color(0.55f, 0.6f, 0.75f));
                    break;

                case ECCCopType.Boss:
                    car.speed *= 0.95f;
                    car.health = Mathf.Max(car.health * 10, 25);
                    car.damage *= 2;
                    car.chaseAngleRange = Vector2.zero;
                    // The bus is already big: a bit bigger still, in its own colors. The van grows a lot and is darkened
                    if (car.name.Contains("Bus"))
                    {
                        Resize(car, 1.25f);
                    }
                    else
                    {
                        Resize(car, 2.1f);
                        Tint(car, new Color(0.4f, 0.4f, 0.45f));
                    }
                    variant.chargeCooldown = 6;
                    variant.chargeSpeedMultiplier = 1.6f;
                    break;
            }

            return variant;
        }

        static void Resize(ECCCar car, float scale)
        {
            car.transform.localScale *= scale;
        }

        static void Tint(ECCCar car, Color tint)
        {
            if (car.hurtFlashMaterials == null) return;

            foreach (Material part in car.hurtFlashMaterials)
            {
                if (part.HasProperty("_Color")) part.color = part.color * tint;
            }
        }

        void Update()
        {
            if (type != ECCCopType.Rammer && type != ECCCopType.Boss) return;

            if (car == null || car.health <= 0 || car.isStationary) return;

            // Remember the speed after the car's own setup ( Start adds a random speed variation )
            if (baseSpeed < 0) baseSpeed = car.speed;

            if (chargeTime > 0)
            {
                chargeTime -= Time.deltaTime;

                if (chargeTime <= 0)
                {
                    car.speed = baseSpeed;
                    cooldownTime = chargeCooldown;
                }

                return;
            }

            if (cooldownTime > 0)
            {
                cooldownTime -= Time.deltaTime;
                return;
            }

            ECCGameController gameController = ECCGameController.Instance;

            if (gameController == null || gameController.playerObject == null) return;

            Vector3 toPlayer = gameController.playerObject.transform.position - transform.position;
            toPlayer.y = 0;

            // Charge when the player is close and right in front
            if (toPlayer.magnitude < chargeDistance && Vector3.Angle(transform.forward, toPlayer) < chargeAngle)
            {
                car.speed = baseSpeed * chargeSpeedMultiplier;
                chargeTime = chargeDuration;
            }
        }
    }
}
