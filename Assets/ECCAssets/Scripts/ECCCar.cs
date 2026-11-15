using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace EndlessCarChase
{
    /// <summary>
    /// This script defines a car, which has health, speed, rotation speed, damage, and other attributes related to the car's behaviour. It also defines AI controls when the car is not player-controlled.
    /// </summary>
    public class ECCCar : MonoBehaviour
    {
        // Various varialbes for quicker access
        internal Transform thisTransform;
        static ECCGameController gameController;
        static Transform targetPlayer;
        internal Vector3 targetPosition;

        internal RaycastHit groundHitInfo;
        internal Vector3 groundPoint;
        internal Vector3 forwardPoint;
        internal float forwardAngle;
        internal float rightAngle;

        [Tooltip("The health of the player. If this reaches 0, the player dies")]
        public float health = 10;
        internal float healthMax;

        [Tooltip("When the car gets hit and hurt, there is a delay during which it cannot be hit again")]
        public float hurtDelay = 2;
        internal float hurtDelayCount = 0;

        [Tooltip("The color in which the car flashes when hurt")]
        public Color hurtFlashColor = new Color(0.5f, 0.5f, 0.5f, 1);

        [Tooltip("The speed of the player, how fast it moves player. The player moves forward constantly")]
        public float speed = 10;

        [Tooltip("How quickly the player car rotates, in both directions")]
        public float rotateSpeed = 200;
        internal float currentRotation = 0;

        [Tooltip("The damage this car causes when hitting other cars. Damage is reduced from Health.")]
        public int damage = 1;

        [Tooltip("The effect that appears when this car is hit by another car")]
        public Transform hitEffect;

        [Tooltip("The effect that appears when this car dies")]
        public Transform deathEffect;

        [Tooltip("The slight extra rotation that happens to the car as it turns, giving a drifting effect")]
        public float driftAngle = 50;

        [Tooltip("The slight side tilt that happens to the car chassis as the car turns, making it lean inwards or outwards from the center of rotation")]
        public float leanAngle = 10;

        [Tooltip("The chassis object of the car which leans when the car rotates")]
        public Transform chassis;

        [Tooltip("The wheels of the car which rotate based on the speed of the car. The front wheels also rotate in the direction the car is turning")]
        public Transform[] wheels;

        [Tooltip("The front wheels of the car also rotate in the direction the car is turning")]
        public int frontWheels = 2;

        internal int index;

        [Header("AI Car Attributes")]
        [Tooltip("A random value that is added to the base speed of the AI car, to make their movements more varied")]
        public float speedVariation = 2;

        // The angle range that AI cars try to chase the player at. So for example if 0 they will target the player exactly, while at 30 angle they stop rotating when they are at a 30 angle relative to the player
        internal float chaseAngle;

        [Tooltip("A random value that is to the chase angle to make the AI cars more varied in how to chase the player")]
        public Vector2 chaseAngleRange = new Vector2(0, 30);

        [Tooltip("Make AI cars try to avoid obstacles. Obstacle are objects that have the ECCObstacle component attached to them")]
        public bool avoidObstacles = true;

        [Tooltip("The width of the obstacle detection area for this AI car")]
        public float detectAngle = 2;

        [Tooltip("The forward distance of the obstacle detection area for this AI car")]
        public float detectDistance = 3;

        //internal float obstacleDetected = 0;

        public float moveHeight = 0;

        [Header("Collision Impact")]
        [Tooltip("How fast a medium hit knocks the other car away, before the weight and speed of both cars are counted")]
        public float impactPush = 5;

        [Tooltip("How fast a strong hit spins the other car around, in degrees per second")]
        public float impactSpin = 280;

        [Tooltip("How long the car body shakes after it gets hit")]
        public float shakeDuration = 0.4f;

        // A hit that takes at least this part of the victim's max health knocks it away and spins it, a weaker one only shakes it
        internal const float DeflectSeverity = 0.25f;

        // Counts down after this car hits another car, so one crash doesn't hit every frame. Separate from the hurt delay so both cars can hit each other
        internal float attackCooldown = 0;

        // The knock-back and spin left over from the last hit, they fade out quickly
        internal Vector3 knockVelocity;
        internal float spinVelocity;
        internal float shakeTime;

        // The time the player car may next be hurt by a tree or rock ( see ECCObstacle )
        internal float nextObstacleHitTime;

        internal Collider bodyCollider;
        Transform shakeTarget;
        Vector3 shakeRestPosition;

        // Extra speed added to every AI car when it spawns. Raised by the endless mode wanted level
        public static float aiSpeedBonus = 0;

        // How well every car holds the road: 1 is dry ( instant steering, no sliding ). Lower on the wet road of the
        // rain theme: steering builds up a little slower and the car keeps sliding the way it was going while it turns
        public static float grip = 1;

        Vector3 moveDirection;
        float steer;

        // All the cars that are currently in the scene ( player and AI )
        public static readonly List<ECCCar> ActiveCars = new List<ECCCar>();

        // Called once when an AI car is destroyed ( health reaches 0 )
        public static event System.Action<ECCCar> CarDestroyed;

        // Called every time the player's car loses health ( not when a shield blocks the hit ), with the health lost
        public static event System.Action<ECCCar, float> PlayerDamaged;

        // While above 0, the car can't be hurt ( used by the shield power-up and after a revive )
        internal float invulnerableTime = 0;

        // A stationary car doesn't move or chase ( used by roadblocks )
        internal bool isStationary = false;

        /// <summary>
        /// Is this the car the player drives?
        /// </summary>
        public bool IsPlayer
        {
            get { return gameController && gameController.playerObject == this; }
        }

        // Cached components, so we don't search for them every frame
        internal Transform baseTransform;
        internal Animator carAnimator;
        internal Material[] hurtFlashMaterials;

        private void Awake()
        {
            thisTransform = this.transform;
            baseTransform = thisTransform.Find("Base");
            carAnimator = GetComponent<Animator>();

            MeshRenderer flashRenderer = GetComponentInChildren<MeshRenderer>();
            if (flashRenderer) hurtFlashMaterials = flashRenderer.materials;

            // The trigger box on the car root is its body when two cars crash
            bodyCollider = GetComponent<Collider>();

            DropForeignParts();

            shakeTarget = chassis ? chassis : baseTransform;
            if (shakeTarget) shakeRestPosition = shakeTarget.localPosition;
        }

        /// <summary>
        /// Some cars ( the prison bus ) were set up with a chassis and wheels that belong to another car's prefab: turning
        /// them would lean and spin that prefab asset instead of this car ( and dirty it in the editor ). Only parts of
        /// this car's own hierarchy are kept; a car without them simply doesn't lean or spin its wheels
        /// </summary>
        void DropForeignParts()
        {
            if (chassis && chassis.IsChildOf(thisTransform) == false) chassis = null;

            if (wheels == null) return;

            int kept = 0;

            foreach (Transform wheel in wheels)
            {
                if (wheel && wheel.IsChildOf(thisTransform) && wheel.Find("WheelObject")) kept++;
            }

            if (kept == wheels.Length) return;

            Transform[] own = new Transform[kept];
            kept = 0;

            foreach (Transform wheel in wheels)
            {
                if (wheel && wheel.IsChildOf(thisTransform) && wheel.Find("WheelObject")) own[kept++] = wheel;
            }

            wheels = own;
        }

        /// <summary>
        /// How heavy the car is in a crash. Bigger and harder hitting cars push the others away and barely move themselves
        /// </summary>
        public float ImpactWeight
        {
            get
            {
                float size = thisTransform ? thisTransform.localScale.x : 1;

                return size * size * Mathf.Max(1, damage);
            }
        }

        private void OnEnable()
        {
            ActiveCars.Add(this);
        }

        private void OnDisable()
        {
            ActiveCars.Remove(this);
        }

        bool sizeApplied = false;

        /// <summary>
        /// In the chase every car is drawn gameController.carSize times bigger, its lowest wheel stands on the ground, and
        /// it gets a soft contact shadow. The root can't be scaled ( the Animator's spawn clip writes the root's scale ),
        /// so the visual part ( Base ) and the colliders grow. Done once per car
        /// </summary>
        void ApplyGameSize()
        {
            if (sizeApplied || baseTransform == null || gameController == null || gameController.gameStarted == false) return;

            sizeApplied = true;

            float size = gameController.carSize;

            if (size > 0 && Mathf.Approximately(size, 1) == false)
            {
                baseTransform.localScale *= size;

                foreach (Collider part in GetComponents<Collider>())
                {
                    BoxCollider box = part as BoxCollider;
                    SphereCollider sphere = part as SphereCollider;
                    CapsuleCollider capsule = part as CapsuleCollider;

                    if (box) { box.size *= size; box.center *= size; }
                    else if (sphere) { sphere.radius *= size; sphere.center *= size; }
                    else if (capsule) { capsule.radius *= size; capsule.height *= size; capsule.center *= size; }
                }
            }

            // Put the lowest point of the car ( its wheels ) on the ground, so it doesn't float
            float lowest = LowestPoint();

            if (lowest < float.MaxValue) baseTransform.localPosition -= Vector3.up * lowest;

            ECCCarShadow.Create(this);
        }

        /// <summary>
        /// The lowest point of the car's meshes, in the car's own space ( 0 = the car's position )
        /// </summary>
        float LowestPoint()
        {
            return MeshExtent(false);
        }

        /// <summary>
        /// The top of the car ( its roof ), in the car's own space, measured from its meshes, so it's right even while the
        /// spawn animation squashes the car. Used to place the health bar above the car
        /// </summary>
        public float TopHeight
        {
            get
            {
                float top = baseTransform ? MeshExtent(true) : float.MinValue;

                return top > float.MinValue ? top : 1;
            }
        }

        /// <summary>
        /// The lowest or highest point of the car's meshes, in the car's own space ( the root's animated scale left out )
        /// </summary>
        float MeshExtent(bool highest)
        {
            float lowest = float.MaxValue;
            float top = float.MinValue;

            foreach (MeshFilter filter in baseTransform.GetComponentsInChildren<MeshFilter>())
            {
                Renderer part = filter.GetComponent<Renderer>();

                if (filter.sharedMesh == null || part == null || part.enabled == false) continue;

                // The chain of local transforms up to the car's root ( the root's own animated scale is left out )
                Matrix4x4 matrix = Matrix4x4.identity;

                for (Transform step = filter.transform; step != null && step != thisTransform; step = step.parent)
                {
                    matrix = Matrix4x4.TRS(step.localPosition, step.localRotation, step.localScale) * matrix;
                }

                Bounds bounds = filter.sharedMesh.bounds;

                // A wheel spins: the corners of its box, turned with it, reach lower than the tyre ever does and would lift
                // the car off the ground. Its real reach is its center plus or minus its radius ( its largest extent )
                if (IsWheelPart(filter.transform))
                {
                    Vector3 center = matrix.MultiplyPoint3x4(bounds.center);
                    float radius = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z)) * matrix.MultiplyVector(Vector3.up).magnitude;

                    lowest = Mathf.Min(lowest, center.y - radius);
                    top = Mathf.Max(top, center.y + radius);

                    continue;
                }

                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = new Vector3((corner & 1) == 0 ? bounds.min.x : bounds.max.x, (corner & 2) == 0 ? bounds.min.y : bounds.max.y, (corner & 4) == 0 ? bounds.min.z : bounds.max.z);

                    float y = matrix.MultiplyPoint3x4(point).y;

                    lowest = Mathf.Min(lowest, y);
                    top = Mathf.Max(top, y);
                }
            }

            return highest ? top : lowest;
        }

        /// <summary>
        /// True for a wheel's mesh: one of the car's wheels or a part of one, or anything named like a wheel
        /// </summary>
        bool IsWheelPart(Transform part)
        {
            for (Transform step = part; step != null && step != thisTransform; step = step.parent)
            {
                if (step.name.IndexOf("wheel", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;

                if (wheels == null) continue;

                foreach (Transform wheel in wheels)
                {
                    if (wheel == step) return true;
                }
            }

            return false;
        }

        private void Start()
        {

            // Hold some variables for easier access
            if ( gameController == null )    gameController = GameObject.FindObjectOfType<ECCGameController>();

            ApplyGameSize();
            if ( targetPlayer == null && gameController.gameStarted == true && gameController.playerObject )    targetPlayer = gameController.playerObject.transform;


            RaycastHit hit;

            if (Physics.Raycast(thisTransform.position + Vector3.up * 5 + thisTransform.forward * 1.0f, -10 * Vector3.up, out hit, 100, gameController.groundLayer)) forwardPoint = hit.point;

            baseTransform.LookAt(forwardPoint);

            // If this is not the player, then it is an AI controlled car, so we set some attribute variations for the AI such as speed and chase angle variations
            if (gameController.playerObject != this)
            {
                // Set a random chase angle for the AI car
                chaseAngle = Random.Range(chaseAngleRange.x, chaseAngleRange.y);

                // Set a random speed variation based on the original speed of the AI car
                speed += Random.Range(0, speedVariation) + aiSpeedBonus;
            }

            // The old police-only health bar is replaced by the mana bar that every car gets, the player's included
            if ( thisTransform.Find("HealthBar") )    thisTransform.Find("HealthBar").gameObject.SetActive(false);

            // Set the max health of the car
            healthMax = health;

            ECCCarHealthBar.Attach(this);

            // Update the health value
            ChangeHealth(0);
        }

        // This function runs whenever we change a value in the component
        private void OnValidate()
        {
            // Limit the maximum number of front wheels to the actual front wheels we have
            frontWheels = Mathf.Clamp(frontWheels, 0, wheels.Length);
        }

        // Update is called once per frame
        void Update()
        {
            // If the game hasn't started yet, nothing happens
            if (gameController && gameController.gameStarted == false) return;

            if (invulnerableTime > 0) invulnerableTime -= Time.deltaTime;

            if (attackCooldown > 0) attackCooldown -= Time.deltaTime;

            UpdateImpactMotion();

            // Roadblock cars stay in place
            if (isStationary == true) return;

            // Move the car forward ( on a wet road it slides: it follows its heading with a delay )
            if (grip >= 0.999f)
            {
                thisTransform.Translate(Vector3.forward * Time.deltaTime * speed, Space.Self);
            }
            else
            {
                Vector3 heading = thisTransform.forward;

                if (moveDirection.sqrMagnitude < 0.01f) moveDirection = heading;

                moveDirection = Vector3.Slerp(moveDirection, heading, Time.deltaTime * Mathf.Lerp(2.5f, 12, grip)).normalized;

                thisTransform.position += moveDirection * Time.deltaTime * speed;
            }

            // Get the current position of the target player
            if ( health > 0 && targetPlayer )    targetPosition = targetPlayer.transform.position;

            // Make the AI controlled car rotate towards the player
            if ( gameController.playerObject != this )
            {
                // Shoot a ray at the position to see if we hit something
                //Ray ray = new Ray(thisTransform.position + Vector3.up * 0.2f + thisTransform.right * Mathf.Sin(Time.time * 20) * detectAngle, transform.TransformDirection(Vector3.forward) * detectDistance);

                // Cast two raycasts to either side of the AI car so that we can detect obstacles
                Ray rayRight = new Ray(thisTransform.position + Vector3.up * 0.2f + thisTransform.right * detectAngle * 0.5f + transform.right * detectAngle * 0.0f * Mathf.Sin(Time.time * 50), transform.TransformDirection(Vector3.forward) * detectDistance);
                Ray rayLeft = new Ray(thisTransform.position + Vector3.up * 0.2f + thisTransform.right * -detectAngle * 0.5f - transform.right * detectAngle * 0.0f * Mathf.Sin(Time.time * 50), transform.TransformDirection(Vector3.forward) * detectDistance);

                RaycastHit hit;
                
                // If we detect an obstacle on our right side, swerve to the left
                if ( avoidObstacles == true && Physics.Raycast(rayRight, out hit, detectDistance) && (hit.transform.GetComponent<ECCObstacle>() || (hit.transform.GetComponent<ECCCar>() && gameController.playerObject != this)) )
                {
                    // Change the emission color of the obstacle to indicate that the car detected it
                    //if (hit.transform.GetComponent<MeshRenderer>() ) hit.transform.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.red);

                    // Rotate left to avoid obstacle
                    Rotate(-1);

                    //obstacleDetected = 0.1f;
                }
                else if ( avoidObstacles == true && Physics.Raycast(rayLeft, out hit, detectDistance) && (hit.transform.GetComponent<ECCObstacle>() || (hit.transform.GetComponent<ECCCar>() && gameController.playerObject != this))) // Otherwise, if we detect an obstacle on our left side, swerve to the right
                {
                    // Change the emission color of the obstacle to indicate that the car detected it
                    //if (hit.transform.GetComponent<MeshRenderer>()) hit.transform.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.red);

                    // Rotate right to avoid obstacle
                    Rotate(1);

                    //obstacleDetected = 0.1f;
                }
                else// if (obstacleDetected <= 0) // Otherwise, if no obstacle is detected, keep chasing the player normally
                {
                    // Rotate the car until it reaches the desired chase angle from either side of the player
                    if (Vector3.Angle(thisTransform.forward, targetPosition - thisTransform.position) > chaseAngle)
                    {
                        Rotate(ChaseAngle(thisTransform.forward, targetPosition - thisTransform.position, Vector3.up));
                    }
                    else // Otherwise, stop rotating
                    {
                        Rotate(0);
                    }
                }
            }

            // If we have no ground object assigned, or it is turned off, then cars will use raycast to move along terrain surfaces
            if ( gameController.groundObject == null || gameController.groundObject.gameObject.activeSelf == false )    DetectGround();

            //if (obstacleDetected > 0) obstacleDetected -= Time.deltaTime;

            // Count down the hurt delay, during which the car can't be hurt again
            if (hurtDelayCount > 0 && health > 0)
            {
                hurtDelayCount -= Time.deltaTime;

                // Change the emission color of the car to indicate that the car is hurt
                if ( hurtFlashMaterials != null )
                {
                    // When the hurt delay ends, make sure the car isn't left in the flash color
                    bool flashOn = hurtDelayCount > 0 && Mathf.Round(hurtDelayCount * 10) % 2 != 0;

                    foreach ( Material part in hurtFlashMaterials )
                    {
                        part.SetColor("_EmissionColor", flashOn ? hurtFlashColor : Color.black);
                    }
                }

            }
        }
        

        /// <summary>
        /// Calculates the approach angle of an object towrads another object
        /// </summary>
        /// <param name="forward"></param>
        /// <param name="targetDirection"></param>
        /// <param name="up"></param>
        /// <returns></returns>
        public float ChaseAngle(Vector3 forward, Vector3 targetDirection, Vector3 up)
        {
            // Calculate the approach angle
            float approachAngle = Vector3.Dot(Vector3.Cross(up, forward), targetDirection);
            
            // If the angle is higher than 0, we approach from the left ( so we must rotate right )
            if (approachAngle > 0f)
            {
                return 1f;
            }
            else if (approachAngle < 0f) //Otherwise, if the angle is lower than 0, we approach from the right ( so we must rotate left )
            {
                return -1f;
            }
            else // Otherwise, we are within the angle range so we don't need to rotate
            {
                return 0f;
            }
        }


        /// <summary>
        /// Rotates the car either left or right, and applies the relevant lean and drift effects
        /// </summary>
        /// <param name="rotateDirection"></param>
        public void Rotate( float rotateDirection )
        {
            // On a wet road the steering builds up and lets go a little slower
            if (grip < 0.999f)
            {
                steer = Mathf.MoveTowards(steer, rotateDirection, Time.deltaTime * Mathf.Lerp(3, 20, grip));

                rotateDirection = Mathf.Abs(steer) < 0.02f ? 0 : steer;
            }

            //thisTransform.localEulerAngles = new Vector3(Quaternion.FromToRotation(Vector3.up, groundHitInfo.normal).eulerAngles.x, thisTransform.localEulerAngles.y, Quaternion.FromToRotation(Vector3.up, groundHitInfo.normal).eulerAngles.z);

            //thisTransform.rotation = Quaternion.FromToRotation(Vector3.up, groundHitInfo.normal);


            // If the car is rotating either left or right, make it drift and lean in the direction its rotating
            if ( rotateDirection != 0 )
            {
                //thisTransform.localEulerAngles = Quaternion.FromToRotation(Vector3.up, groundHitInfo.normal).eulerAngles + Vector3.up * currentRotation;

                // Rotate the car based on the control direction
                thisTransform.localEulerAngles += Vector3.up * rotateDirection * rotateSpeed * Time.deltaTime;

                thisTransform.eulerAngles = new Vector3(thisTransform.eulerAngles.x, thisTransform.eulerAngles.y, thisTransform.eulerAngles.z);

                //thisTransform.eulerAngles = new Vector3(rightAngle, thisTransform.eulerAngles.y, forwardAngle);

                currentRotation += rotateDirection * rotateSpeed * Time.deltaTime;

                if (currentRotation > 360) currentRotation -= 360;
                //print(forwardAngle);
                // Make the base of the car drift based on the rotation angle
                baseTransform.localEulerAngles = new Vector3(rightAngle, Mathf.LerpAngle(baseTransform.localEulerAngles.y, rotateDirection * driftAngle + Mathf.Sin(Time.time * 50) * hurtDelayCount * 50, Time.deltaTime), 0);//  Mathf.LerpAngle(baseTransform.localEulerAngles.y, rotateDirection * driftAngle, Time.deltaTime);

                // Make the chassis lean to the sides based on the rotation angle
                if (chassis) chassis.localEulerAngles = Vector3.forward * Mathf.LerpAngle(chassis.localEulerAngles.z, rotateDirection * leanAngle, Time.deltaTime);//  Mathf.LerpAngle(baseTransform.localEulerAngles.y, rotateDirection * driftAngle, Time.deltaTime);
                
                // Play the skidding animation. In this animation you can trigger all kinds of effects such as dust, skid marks, etc
                PlayAnimation("Skid");

                // Go through all the wheels making them spin, and make the front wheels turn sideways based on rotation
                for (index = 0; index < wheels.Length; index++)
                {
                    // Turn the front wheels sideways based on rotation
                    if (index < frontWheels) wheels[index].localEulerAngles = Vector3.up * Mathf.LerpAngle(wheels[index].localEulerAngles.y, rotateDirection * driftAngle, Time.deltaTime * 10);

                    // Spin the wheel
                    wheels[index].Find("WheelObject").Rotate(Vector3.right * Time.deltaTime * speed * 20, Space.Self);
                }
            }
            else // Otherwise, if we are no longer rotating, straighten up the car and front wheels
            {
                // Return the base of the car to its 0 angle
                baseTransform.localEulerAngles = Vector3.up * Mathf.LerpAngle(baseTransform.localEulerAngles.y, 0, Time.deltaTime * 5);

                // Return the chassis to its 0 angle
                if (chassis) chassis.localEulerAngles = Vector3.forward * Mathf.LerpAngle(chassis.localEulerAngles.z, 0, Time.deltaTime * 5);//  Mathf.LerpAngle(baseTransform.localEulerAngles.y, rotateDirection * driftAngle, Time.deltaTime);

                // Play the move animation. In this animation we stop any previously triggered effects such as dust, skid marks, etc
                PlayAnimation("Move");

                // Go through all the wheels making them spin faster than when turning, and return the front wheels to their 0 angle
                for (index = 0; index < wheels.Length; index++)
                {
                    // Return the front wheels to their 0 angle
                    if (index < frontWheels) wheels[index].localEulerAngles = Vector3.up * Mathf.LerpAngle(wheels[index].localEulerAngles.y, 0, Time.deltaTime * 5);

                    // Spin the wheel faster
                    wheels[index].Find("WheelObject").Rotate(Vector3.right * Time.deltaTime * speed * 30, Space.Self);
                }
            }
        }

        /// <summary>
        /// Is executed when this obstacle touches another object with a trigger collider
        /// </summary>
        /// <param name="other"><see cref="Collider"/></param>
        void OnTriggerStay(Collider other)
        {
            ECCCar otherCar = other.GetComponent<ECCCar>();

            if (otherCar == null || otherCar == this) return;

            // Wrecks that are flipping over don't block or hit anymore
            if (health <= 0 || otherCar.health <= 0) return;

            // Cars are solid: push this car out of the other one instead of driving through it
            SeparateFrom(otherCar);

            // Each car hits the other once per cooldown, so both feel the crash
            if (attackCooldown <= 0)
            {
                attackCooldown = hurtDelay;

                otherCar.TakeImpact(this);
            }
        }

        /// <summary>
        /// Moves this car out of a solid obstacle ( a tree or rock ), along the ground
        /// </summary>
        public void PushOutOf(Collider obstacle)
        {
            if (isStationary || bodyCollider == null || obstacle == null) return;

            Vector3 direction;
            float distance;

            if (Physics.ComputePenetration(bodyCollider, thisTransform.position, thisTransform.rotation, obstacle, obstacle.transform.position, obstacle.transform.rotation, out direction, out distance) == false) return;

            direction.y = 0;

            if (direction.sqrMagnitude < 0.0001f) return;

            thisTransform.position += direction.normalized * distance;
        }

        /// <summary>
        /// Moves this car out of the other car's body. The lighter car moves more
        /// </summary>
        void SeparateFrom(ECCCar otherCar)
        {
            if (isStationary || bodyCollider == null || otherCar.bodyCollider == null) return;

            Vector3 direction;
            float distance;

            if (Physics.ComputePenetration(bodyCollider, thisTransform.position, thisTransform.rotation, otherCar.bodyCollider, otherCar.thisTransform.position, otherCar.thisTransform.rotation, out direction, out distance) == false) return;

            // Only push along the ground
            direction.y = 0;

            if (direction.sqrMagnitude < 0.0001f) return;

            // A roadblock doesn't move, so this car takes the whole push. The other car pushes itself out in its own trigger call
            float share = otherCar.isStationary ? 1 : otherCar.ImpactWeight / (ImpactWeight + otherCar.ImpactWeight);

            thisTransform.position += direction.normalized * distance * share;
        }

        /// <summary>
        /// This car was rammed by another car. Based on how hard the striker hits ( its damage, weight and speed into this car )
        /// this car shakes, gets knocked away and spun, or explodes when its health runs out
        /// </summary>
        public void TakeImpact(ECCCar striker)
        {
            if (health <= 0) return;

            Vector3 away = thisTransform.position - striker.thisTransform.position;
            away.y = 0;
            away = away.sqrMagnitude > 0.0001f ? away.normalized : -thisTransform.forward;

            // How fast the striker drives into this car. A head-on ram hits harder than two cars brushing side by side
            float closingSpeed = Vector3.Dot(striker.thisTransform.forward * striker.speed - thisTransform.forward * (isStationary ? 0 : speed), away);
            float speedFactor = Mathf.Clamp(0.7f + Mathf.Max(0, closingSpeed) / 12, 0.7f, 1.6f);

            // A heavier striker throws this car further
            float weightRatio = Mathf.Clamp(striker.ImpactWeight / ImpactWeight, 0.4f, 2.5f);

            float power = striker.damage * speedFactor;
            float hitDamage = Mathf.Max(1, Mathf.Round(power));

            // A shield ( or a fresh revive ) protects the car from the damage and from being thrown around
            if (invulnerableTime > 0)
            {
                shakeTime = shakeDuration * 0.5f;
                return;
            }

            bool lethal = health - hitDamage <= 0;
            float severity = hitDamage / Mathf.Max(1, healthMax);

            // Every hit shakes the car body and flashes it
            shakeTime = shakeDuration;
            hurtDelayCount = hurtDelay;

            if (isStationary == false && (lethal || severity >= DeflectSeverity || weightRatio > 1.3f))
            {
                // A strong hit knocks the car away and spins it to the side it was hit from
                knockVelocity = away * striker.impactPush * speedFactor * weightRatio;

                float side = Vector3.Dot(thisTransform.right, -away) >= 0 ? 1 : -1;
                spinVelocity = side * striker.impactSpin * Mathf.Clamp(severity * 2 * weightRatio, 0.4f, 1.5f);
            }
            else if (isStationary == false)
            {
                // A light hit only nudges it
                knockVelocity = away * striker.impactPush * 0.3f * weightRatio;
            }

            // Damage the car. If its health runs out, it explodes
            ChangeHealth(-hitDamage);

            // If the car survived, create the hit effect
            if (health > 0 && hitEffect) Instantiate(hitEffect, thisTransform.position, thisTransform.rotation);
        }

        /// <summary>
        /// Plays out the knock-back, spin and body shake of the last hit
        /// </summary>
        void UpdateImpactMotion()
        {
            if (knockVelocity != Vector3.zero)
            {
                thisTransform.position += knockVelocity * Time.deltaTime;

                knockVelocity = Vector3.MoveTowards(knockVelocity, Vector3.zero, 14 * Time.deltaTime);
            }

            if (spinVelocity != 0)
            {
                thisTransform.Rotate(Vector3.up * spinVelocity * Time.deltaTime, Space.World);

                spinVelocity = Mathf.MoveTowards(spinVelocity, 0, 600 * Time.deltaTime);
            }

            if (shakeTarget && shakeTime > 0)
            {
                shakeTime -= Time.deltaTime;

                float amount = 0.08f * Mathf.Clamp01(shakeTime / Mathf.Max(0.01f, shakeDuration));

                shakeTarget.localPosition = shakeTime > 0 ? shakeRestPosition + new Vector3(Random.Range(-amount, amount), Random.Range(0, amount), Random.Range(-amount, amount)) : shakeRestPosition;
            }
        }

        /// <summary>
        /// Changes the lives of the player. If lives reach 0, it's game over
        /// </summary>
        /// <param name="changeValue"></param>
        /// <summary>
        /// Plays an animator state using the cached animator
        /// </summary>
        void PlayAnimation(string stateName)
        {
            if (carAnimator) carAnimator.Play(stateName);
        }

        [Tooltip("Seconds between the player car's explosion and the continue offer or the loss screen")]
        public float wreckDelay = 2;

        bool wrecking = false;
        bool exploded = false;
        bool stationaryBeforeWreck = false;

        /// <summary>
        /// The player car was wrecked: it explodes right away ( the wreck is hidden and stops, with a short slow motion ),
        /// then after wreckDelay seconds the continue offer is shown if there is one, otherwise the run ends
        /// </summary>
        IEnumerator WreckRoutine()
        {
            if (wrecking) yield break;

            wrecking = true;
            exploded = true;
            stationaryBeforeWreck = isStationary;

            if (deathEffect) Instantiate(deathEffect, transform.position, transform.rotation);

            SetWreckVisible(false);
            isStationary = true;

            Time.timeScale = 0.5f;

            yield return new WaitForSecondsRealtime(wreckDelay);

            wrecking = false;

            if (gameController == null || gameController.isGameOver || health > 0)
            {
                if (Time.timeScale == 0.5f) Time.timeScale = 1;
                yield break;
            }

            Time.timeScale = 1;

            // The revive offer ( or the daily challenge's second chance ) puts the car back with RecoverFromWreck
            if (ECCRevive.TryOffer(gameController, this)) yield break;

            FinishPlayerDeath();
        }

        /// <summary>
        /// A revived car is shown and drives again
        /// </summary>
        void RecoverFromWreck()
        {
            exploded = false;
            isStationary = stationaryBeforeWreck;

            SetWreckVisible(true);
        }

        void SetWreckVisible(bool visible)
        {
            foreach (Renderer part in GetComponentsInChildren<Renderer>(true))
            {
                // Particles ( dust ) keep their own state
                if (part is ParticleSystemRenderer) continue;

                part.enabled = visible;
            }
        }

        public void ChangeHealth(float changeValue)
        {
            // A car that is already destroyed can't be hurt again. This prevents dying twice ( and a double game over )
            if (changeValue < 0 && health <= 0) return;

            // A revive brings an exploded car back
            if (changeValue > 0 && health <= 0 && exploded) RecoverFromWreck();

            // A shielded car can't be hurt
            if (changeValue < 0 && invulnerableTime > 0) return;

            // Change the health value
            health += changeValue;

            if (changeValue < 0 && IsPlayer)
            {
                ECCHaptics.Vibrate(ECCHaptics.Kind.Heavy);

                if (PlayerDamaged != null) PlayerDamaged(this, -changeValue);
            }

            // Limit the value of the health to the maximum allowed value
            if (health > healthMax) health = healthMax;

            // If this is the player car, play the shake animation
            // A shake that is already playing isn't restarted: an obstacle hurts the car on every physics step while they
            // touch, and restarting the animation each time makes the picture jump
            if (changeValue < 0 && gameController.playerObject == this)
            {
                Transform cameraHolder = Camera.main ? Camera.main.transform.parent : null;
                Animation shake = cameraHolder ? cameraHolder.GetComponent<Animation>() : null;

                if (shake && shake.isPlaying == false) shake.Play();
            }

            // If health reaches 0, the car dies
            if (health <= 0)
            {
                if (gameController.playerObject && gameController.playerObject != this)
                {
                    DelayedDie();

                    if (CarDestroyed != null) CarDestroyed(this);
                }
                else if (IsPlayer && gameController.gameStarted)
                {
                    // The car explodes first; the continue offer or the loss screen comes a moment later ( see WreckRoutine )
                    StartCoroutine(WreckRoutine());
                }
                else
                {
                    FinishPlayerDeath();
                }
            }

            // Update the health bar 
            if ( gameController.playerObject && gameController.playerObject == this && gameController.healthCanvas)
            {
                // Update the health bar based on the health we have
                if (gameController.healthCanvas.Find("Full")) gameController.healthCanvas.Find("Full").GetComponent<Image>().fillAmount = health / healthMax;

                if (gameController.healthCanvas.Find("Text")) gameController.healthCanvas.Find("Text").GetComponent<Text>().text = health.ToString();

                // Play the animation of the health icon
                if (gameController.healthCanvas.GetComponent<Animation>()) gameController.healthCanvas.GetComponent<Animation>().Play();
            }
        }

        /// <summary>
        /// Kill the car and create a death effect
        /// </summary>
        /// <summary>
        /// Destroys the car, and ends the game if it's the player car
        /// </summary>
        public void FinishPlayerDeath()
        {
            bool isPlayer = IsPlayer;

            // After WreckRoutine the explosion was already shown: the loss screen comes right away
            bool alreadyExploded = exploded;

            if (alreadyExploded) Destroy(gameObject);
            else Die();

            // If this is the player car, trigger the GameOver event
            if (isPlayer && alreadyExploded)
            {
                gameController.GameOverNow();
            }
            else if (isPlayer)
            {
                gameController.SendMessage("GameOver", 1.2f);

                // Play a slowmotion effect
                Time.timeScale = 0.5f;
            }
        }

        public void Die()
        {
            // Create a death effect at the position of the player
            if (deathEffect) Instantiate(deathEffect, transform.position, transform.rotation);

            // Remove the player from the game
            Destroy(gameObject);
        }

        /// <summary>
        /// Make the car lose control for a second, and then kill it
        /// </summary>
        public void DelayedDie()
        {
            //targetPlayer = null;

            // Add all wheels as part of the chasis to make sure they flip over with it
            for (index = 0; index < wheels.Length; index++)
            {
                wheels[index].transform.SetParent(chassis);
            }

            targetPosition = thisTransform.forward * -10;

            leanAngle = Random.Range(100,300);

            driftAngle = Random.Range(100, 150); ;

            //rotateSpeed *= 2;

            Invoke("Die", Random.Range(0,0.8f));
        }

        /// <summary>
        /// Detects the terrain under the car and aligns it to it
        /// </summary>
        public void DetectGround()
        {
            // Cast a ray to the ground below
            Ray carToGround = new Ray(thisTransform.position + Vector3.up * 10, -Vector3.up * 20);

            // Detect terrain under the car
            if (Physics.Raycast(carToGround, out groundHitInfo, 20, gameController.groundLayer))
            {
                //transform.position = new Vector3(transform.position.x, groundHitInfo.point.y, transform.position.z);
            }
            
            // Set the position of the car along the terrain
            thisTransform.position = new Vector3(thisTransform.position.x, groundHitInfo.point.y + 0.1f, thisTransform.position.z);

            RaycastHit hit;

            // Detect a point along the terrain in front of the car, so that we can make the car rotate accordingly
            if (Physics.Raycast(thisTransform.position + Vector3.up * 5 + thisTransform.forward * 1.0f, -10 * Vector3.up, out hit, 100, gameController.groundLayer))
            {
                forwardPoint = hit.point;
            }
            else if ( gameController.groundObject && gameController.groundObject.gameObject.activeSelf == true )
            {
                forwardPoint = new Vector3(thisTransform.position.x, gameController.groundObject.position.y, thisTransform.position.z);
            }

            // Make the car look at the point in front of it along the terrain
            baseTransform.LookAt(forwardPoint);
        }


        public void OnDrawGizmos()
        {
            Gizmos.color = Color.red;

            Gizmos.DrawRay(transform.position + Vector3.up * 0.2f + transform.right * detectAngle * 0.5f + transform.right * detectAngle * 0.0f * Mathf.Sin(Time.time * 50), transform.TransformDirection(Vector3.forward) * detectDistance);
            Gizmos.DrawRay(transform.position + Vector3.up * 0.2f + transform.right * -detectAngle * 0.5f - transform.right * detectAngle * 0.0f * Mathf.Sin(Time.time * 50), transform.TransformDirection(Vector3.forward) * detectDistance);

            Gizmos.DrawSphere(forwardPoint, 0.5f);
        }
    }
}
