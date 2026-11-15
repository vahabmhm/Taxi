using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// A spot on a street where a passenger waits or gets out: the place on the sidewalk where the person stands, and
    /// the place in the lane next to it where the taxi must stop ( the pickup zone )
    /// </summary>
    public struct TaxiSpot
    {
        public Vector3 stand;
        public Vector3 zone;

        // Points from the sidewalk onto the street
        public Vector3 toStreet;

        // Along the street
        public Vector3 streetDirection;
    }

    /// <summary>
    /// Helpers for the street grid of the endless city ( ECCCityWorld ): street lines at multiples of the street spacing
    /// on X and Z, crossroads where they meet, right-hand lanes, sidewalks, and routes along the streets
    /// </summary>
    public static class TaxiCity
    {
        // From a street's center line: the right-hand lane, the sidewalk where people stand, half a crossroad
        public const float LaneOffset = 3;
        public const float SidewalkOffset = 6.0f;
        public const float CrossHalf = 7;

        // Spots are this far from a crossroad's edge at least
        const float CornerMargin = 4;

        public static bool IsActive
        {
            get { return ECCCityWorld.IsActive; }
        }

        public static float Spacing
        {
            get
            {
                float spacing;

                return ECCCityWorld.GetStreetSpacing(out spacing) && spacing > 1 ? spacing : 42;
            }
        }

        public static Vector2Int NearestCrossroad(Vector3 position)
        {
            float spacing = Spacing;

            return new Vector2Int(Mathf.RoundToInt(position.x / spacing), Mathf.RoundToInt(position.z / spacing));
        }

        public static Vector3 CrossroadCenter(Vector2Int crossroad)
        {
            float spacing = Spacing;

            return new Vector3(crossroad.x * spacing, 0, crossroad.y * spacing);
        }

        /// <summary>
        /// Is the point inside a crossroad ( where two streets meet )?
        /// </summary>
        public static bool InCrossroad(Vector3 position, out Vector2Int crossroad)
        {
            crossroad = NearestCrossroad(position);

            Vector3 center = CrossroadCenter(crossroad);

            return Mathf.Abs(position.x - center.x) < CrossHalf && Mathf.Abs(position.z - center.z) < CrossHalf;
        }

        /// <summary>
        /// The main direction of a heading: true when it runs mostly along Z
        /// </summary>
        public static bool IsAlongZ(Vector3 direction)
        {
            return Mathf.Abs(direction.z) >= Mathf.Abs(direction.x);
        }

        /// <summary>
        /// A heading snapped to the nearest street direction ( +X, -X, +Z or -Z )
        /// </summary>
        public static Vector3 Snap(Vector3 direction)
        {
            if (IsAlongZ(direction)) return new Vector3(0, 0, direction.z < 0 ? -1 : 1);

            return new Vector3(direction.x < 0 ? -1 : 1, 0, 0);
        }

        /// <summary>
        /// The right hand side of a heading on the ground
        /// </summary>
        public static Vector3 Right(Vector3 direction)
        {
            return new Vector3(direction.z, 0, -direction.x);
        }

        /// <summary>
        /// Distance from the point to the entry line of the next crossroad ahead along a street direction, and that
        /// crossroad. A point inside a crossroad already passed its entry line, so the next one is returned
        /// </summary>
        public static float DistanceToNextCrossroad(Vector3 position, Vector3 direction, out Vector2Int crossroad)
        {
            float spacing = Spacing;

            direction = Snap(direction);

            if (direction.z != 0)
            {
                float sign = direction.z;
                int row = sign > 0 ? Mathf.CeilToInt((position.z + CrossHalf - 0.5f) / spacing) : Mathf.FloorToInt((position.z - CrossHalf + 0.5f) / spacing);

                crossroad = new Vector2Int(Mathf.RoundToInt(position.x / spacing), row);

                return sign * (row * spacing - sign * CrossHalf - position.z);
            }
            else
            {
                float sign = direction.x;
                int column = sign > 0 ? Mathf.CeilToInt((position.x + CrossHalf - 0.5f) / spacing) : Mathf.FloorToInt((position.x - CrossHalf + 0.5f) / spacing);

                crossroad = new Vector2Int(column, Mathf.RoundToInt(position.z / spacing));

                return sign * (column * spacing - sign * CrossHalf - position.x);
            }
        }

        /// <summary>
        /// A random spot on a sidewalk between minDistance and maxDistance from the center ( never next to a crossroad )
        /// </summary>
        public static TaxiSpot RandomSpot(Vector3 center, float minDistance, float maxDistance)
        {
            TaxiSpot spot = new TaxiSpot();

            for (int attempt = 0; attempt < 12; attempt++)
            {
                spot = SpotNear(center + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(minDistance, maxDistance), Random.value < 0.5f);

                float distance = ECCRuntimeVisuals.FlatDistance(spot.zone, center);

                if (distance >= minDistance * 0.85f && distance <= maxDistance * 1.15f) return spot;
            }

            return spot;
        }

        /// <summary>
        /// The sidewalk spot on the street closest to a point
        /// </summary>
        public static TaxiSpot SpotNear(Vector3 point, bool rightSide)
        {
            float spacing = Spacing;

            float offX = Mathf.Abs(point.x - Mathf.Round(point.x / spacing) * spacing);
            float offZ = Mathf.Abs(point.z - Mathf.Round(point.z / spacing) * spacing);

            // The nearer street line
            bool streetAlongZ = offX <= offZ;

            float side = rightSide ? 1 : -1;
            float minAlong = CrossHalf + CornerMargin;

            TaxiSpot spot;

            if (streetAlongZ)
            {
                float lineX = Mathf.Round(point.x / spacing) * spacing;
                float along = ClampAlong(point.z, spacing, minAlong);

                spot.stand = new Vector3(lineX + side * SidewalkOffset, 0, along);
                spot.zone = new Vector3(lineX + side * LaneOffset, 0, along);
                spot.streetDirection = Vector3.forward;
            }
            else
            {
                float lineZ = Mathf.Round(point.z / spacing) * spacing;
                float along = ClampAlong(point.x, spacing, minAlong);

                spot.stand = new Vector3(along, 0, lineZ + side * SidewalkOffset);
                spot.zone = new Vector3(along, 0, lineZ + side * LaneOffset);
                spot.streetDirection = Vector3.right;
            }

            spot.toStreet = (spot.zone - spot.stand).normalized;

            return spot;
        }

        /// <summary>
        /// Keeps a coordinate along a street away from the crossroads ( at multiples of the spacing )
        /// </summary>
        static float ClampAlong(float value, float spacing, float minAlong)
        {
            float block = Mathf.Floor(value / spacing) * spacing;
            float local = value - block;

            return block + Mathf.Clamp(local, minAlong, spacing - minAlong);
        }

        /// <summary>
        /// The street distance ( along the grid ) between two points. Used for the fares
        /// </summary>
        public static float StreetDistance(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
        }

        /// <summary>
        /// The next point to drive to on the way from a position to a target, along the streets ( an L shaped route ):
        /// the target itself when it's on the same street, otherwise the crossroad where the car must turn. Straight
        /// stretches aim at the right-hand lane
        /// </summary>
        public static Vector3 NextWaypoint(Vector3 position, Vector3 target)
        {
            float spacing = Spacing;

            float lineX = Mathf.Round(position.x / spacing) * spacing;
            float lineZ = Mathf.Round(position.z / spacing) * spacing;

            bool onStreetAlongZ = Mathf.Abs(position.x - lineX) < CrossHalf;
            bool onStreetAlongX = Mathf.Abs(position.z - lineZ) < CrossHalf;

            float targetLineX = Mathf.Round(target.x / spacing) * spacing;
            float targetLineZ = Mathf.Round(target.z / spacing) * spacing;

            bool targetAlongZ = Mathf.Abs(target.x - targetLineX) < CrossHalf;
            bool targetAlongX = targetAlongZ == false || Mathf.Abs(target.z - targetLineZ) < CrossHalf;

            bool sameColumn = Mathf.Abs(lineX - targetLineX) < 1;
            bool sameRow = Mathf.Abs(lineZ - targetLineZ) < 1;

            // On the target's own street: straight to it
            if (onStreetAlongZ && targetAlongZ && sameColumn) return target;
            if (onStreetAlongX && targetAlongX && sameRow) return target;

            Vector3 corner;

            if (onStreetAlongZ && onStreetAlongX)
            {
                // In a crossroad: leave it along the street that leads to the target's street
                if (targetAlongZ) corner = sameColumn ? target : new Vector3(targetLineX, 0, lineZ);
                else corner = sameRow ? target : new Vector3(lineX, 0, targetLineZ);
            }
            else if (onStreetAlongZ)
            {
                corner = new Vector3(lineX, 0, targetAlongX ? targetLineZ : Mathf.Round(target.z / spacing) * spacing);
            }
            else
            {
                corner = new Vector3(targetAlongZ ? targetLineX : Mathf.Round(target.x / spacing) * spacing, 0, lineZ);
            }

            if (corner == target) return target;

            Vector3 direction = Snap(corner - position);

            return corner + Right(direction) * LaneOffset;
        }
    }
}
