using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The personal record of the endless mode, on the road: the distance driven in the run is counted, and when the
    /// player gets close to the longest run so far, a checkered «رکورد قبلی» line is laid across the road ahead, right
    /// where that run ended. Driving past it is a new record. A small readout under the wanted level shows the distance.
    /// The best distance is saved when the run ends.
    /// </summary>
    public class ECCRecordLine : MonoBehaviour
    {
        [Tooltip("How far ahead of the player the line is laid ( a little more than what the camera shows ahead )")]
        public float lookAhead = 32;

        [Tooltip("Records shorter than this don't get a line")]
        public int minRecord = 80;

        ECCGameController gameController;

        float distance;
        Vector3 lastPosition;
        bool hasLast;
        int best;
        bool linePlaced;
        bool recordBroken;
        int savedMeters = 0;

        Transform line;
        Canvas canvas;
        Text readout;
        Text lineLabel;

        public float Distance
        {
            get { return distance; }
        }

        public static void StartIfEndless(ECCGameController gameController)
        {
            if (gameController == null || gameController.activeMode is Modes.ECCEndlessMode == false) return;

            if (gameController.GetComponent<ECCRecordLine>() == null) gameController.gameObject.AddComponent<ECCRecordLine>();
        }

        void Start()
        {
            gameController = GetComponent<ECCGameController>();

            best = ECCSave.Data.endlessBestDistance;

            canvas = ECCRuntimeUI.CreateCanvas("CanvasRecordLine", 49);

            readout = ECCRuntimeUI.CreateText("Distance", canvas.transform, string.Empty, 22, new Vector2(0.5f, 1), new Vector2(0, -215), new Vector2(420, 30), Color.white);

            lineLabel = ECCRuntimeUI.CreateText("LineLabel", canvas.transform, string.Empty, 26, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 40), ECCRuntimeUI.StarColor);
            lineLabel.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (canvas) Destroy(canvas.gameObject);
        }

        void Update()
        {
            if (gameController == null) return;

            bool visible = gameController.gameStarted && gameController.isGameOver == false && (gameController.gameCanvas == null || gameController.gameCanvas.gameObject.activeInHierarchy);

            if (canvas.enabled != visible) canvas.enabled = visible;

            if (gameController.isGameOver)
            {
                SaveRecord();
                return;
            }

            ECCCar player = gameController.playerObject;

            if (gameController.gameStarted == false || player == null || player.health <= 0 || gameController.startDelay > 0) return;

            Vector3 position = player.transform.position;

            if (hasLast) distance += ECCRuntimeVisuals.FlatDistance(position, lastPosition);

            lastPosition = position;
            hasLast = true;

            UpdateReadout();

            if (best < minRecord) return;

            if (linePlaced == false && distance >= best - lookAhead) PlaceLine(player.transform);

            if (recordBroken == false && distance > best)
            {
                recordBroken = true;

                ECCHintBanner.Show("Icons/trophy", ECCRuntimeUI.StarColor, "رکورد جدید!", "از رکورد قبلی‌ات جلو زدی", 2.2f);
                ECCHaptics.Vibrate(ECCHaptics.Kind.Reward);
            }

            UpdateLineLabel();
        }

        void UpdateReadout()
        {
            string text = ECCUIKit.PersianDigits(Mathf.FloorToInt(distance)) + " متر";

            if (best >= minRecord) text += "  |  رکورد: " + ECCUIKit.PersianDigits(Mathf.Max(best, Mathf.FloorToInt(distance))) + " متر";

            ECCRuntimeUI.SetText(readout, text);

            readout.color = recordBroken ? ECCRuntimeUI.StarColor : Color.white;
        }

        /// <summary>
        /// A checkered line across the road where the record run ended, with a post on each side
        /// </summary>
        void PlaceLine(Transform player)
        {
            linePlaced = true;

            Vector3 forward = player.forward;
            forward.y = 0;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;

            Vector3 center = ECCRuntimeVisuals.SnapToGround(player.position + forward * Mathf.Max(5, best - distance), gameController.groundLayer);

            line = new GameObject("RecordLine").transform;
            line.position = center;
            line.rotation = Quaternion.LookRotation(forward);

            const float width = 16;
            const int squares = 16;
            float square = width / squares;

            Material white = ECCRuntimeVisuals.CreateMaterial(Color.white, 0.5f);
            Material black = ECCRuntimeVisuals.CreateMaterial(new Color(0.1f, 0.1f, 0.1f), 0);
            Material gold = ECCRuntimeVisuals.CreateMaterial(new Color(1, 0.78f, 0.15f), 1);

            for (int row = 0; row < 2; row++)
            {
                for (int index = 0; index < squares; index++)
                {
                    float x = -width * 0.5f + square * (index + 0.5f);
                    float z = (row - 0.5f) * square;

                    ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, line, new Vector3(x, 0.06f, z), new Vector3(square, 0.05f, square), (index + row) % 2 == 0 ? white : black);
                }
            }

            ECCGlow.CreateFlat("Glow", line, new Vector3(0, 0.04f, 0), new Vector2(width + 4, 5), ECCGlow.RadialMaterial(new Color(1, 0.8f, 0.2f, 0.6f)));

            for (int side = -1; side <= 1; side += 2)
            {
                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, line, new Vector3(side * (width * 0.5f + 0.4f), 2, 0), new Vector3(0.3f, 2, 0.3f), gold);
                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, line, new Vector3(side * (width * 0.5f - 0.6f), 3.6f, 0), new Vector3(2, 0.9f, 0.08f), (side < 0) ? white : black);
            }

            line.gameObject.AddComponent<ECCDespawnWhenFar>();
        }

        /// <summary>
        /// The «رکورد قبلی» label floats over the line on screen
        /// </summary>
        void UpdateLineLabel()
        {
            Camera view = Camera.main;

            bool show = line && view && canvas.enabled;

            Vector3 screen = show ? view.WorldToScreenPoint(line.position + Vector3.up * 5) : Vector3.zero;

            show = show && screen.z > 0;

            if (lineLabel.gameObject.activeSelf != show) lineLabel.gameObject.SetActive(show);

            if (show == false) return;

            ECCRuntimeUI.SetText(lineLabel, recordBroken ? "رکورد شکسته شد!" : "رکورد قبلی");

            lineLabel.transform.position = screen;
        }

        void SaveRecord()
        {
            int meters = Mathf.FloorToInt(distance);

            // Once per stretch of driving: a continue after the game over keeps counting, and is saved again at its end
            if (meters <= savedMeters) return;

            savedMeters = meters;

            ECCAchievements.Report(ECCStat.EndlessDistance, meters);

            if (meters > ECCSave.Data.endlessBestDistance)
            {
                ECCSave.Data.endlessBestDistance = meters;
                ECCSave.Save();
            }
        }
    }
}
