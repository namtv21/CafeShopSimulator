using UnityEngine;

namespace CafeShopSimulator
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TownRouteActor : MonoBehaviour
    {
        public Vector2[] route;
        public Sprite[] left, right, up, down;
        public float speed = 2;
        public float startOffset;
        public bool loop;
        [Min(0)] public float initialDelay;
        public Vector2 respawnDelay;
        public Vector2 speedRange;
        [Min(0)] public float animationFps = 9;
        [Tooltip("Route waypoint indices that are building entrances. The actor waits inside, then exits at the same point.")]
        public int[] indoorWaypoints;
        public Transform[] indoorDoorways;
        public Vector2 indoorStay = new Vector2(5, 12);
        public bool IsInsideBuilding { get; private set; }
        private SpriteRenderer body;
        private float distance;
        private float length;
        private float waitRemaining;
        private float travelSpeed;
        private float animationTime;
        private float[] waypointDistances;

        private void Awake()
        {
            body = GetComponent<SpriteRenderer>();
            if (route == null || route.Length < 2) return;
            if (indoorWaypoints != null && indoorDoorways != null)
                for (int i = 0; i < Mathf.Min(indoorWaypoints.Length, indoorDoorways.Length); i++)
                    if (indoorDoorways[i] != null && indoorWaypoints[i] >= 0 && indoorWaypoints[i] < route.Length)
                        route[indoorWaypoints[i]] = indoorDoorways[i].position;
            waypointDistances = new float[route.Length];
            for (int i = 1; i < route.Length; i++)
            {
                length += Vector2.Distance(route[i - 1], route[i]);
                waypointDistances[i] = length;
            }
            if (loop && route.Length > 1) length += Vector2.Distance(route[route.Length - 1], route[0]);
            if (length <= 0) return;
            distance = Mathf.Repeat(startOffset, length);
            waitRemaining = Mathf.Max(0, initialDelay);
            PickSpeed();
            RenderPosition();
            body.enabled = waitRemaining <= 0;
        }

        private void Update()
        {
            if (length <= 0 || (CafeApp.Instance != null && CafeApp.Instance.IsPaused)) return;
            Advance(Time.deltaTime);
        }

        private void PickSpeed()
        {
            travelSpeed = speedRange.x > 0 && speedRange.y >= speedRange.x
                ? Random.Range(speedRange.x, speedRange.y) : Mathf.Max(0, speed);
        }

        private void Advance(float deltaTime)
        {
            if (waitRemaining > 0)
            {
                waitRemaining = Mathf.Max(0, waitRemaining - deltaTime);
                if (waitRemaining > 0) return;
                IsInsideBuilding = false;
                body.enabled = true;
                // Resume at the off-map entrance without a large first movement step.
                RenderPosition();
                return;
            }
            float nextDistance = distance + travelSpeed * deltaTime;
            if (TryEnterBuilding(nextDistance)) return;
            distance = nextDistance;
            animationTime += deltaTime;
            if (distance >= length)
            {
                if (loop) distance = Mathf.Repeat(distance, length);
                else
                {
                    distance = 0;
                    waitRemaining = Random.Range(Mathf.Max(0, respawnDelay.x), Mathf.Max(respawnDelay.x, respawnDelay.y));
                    PickSpeed();
                    body.enabled = waitRemaining <= 0;
                }
            }
            RenderPosition();
        }

        private bool TryEnterBuilding(float nextDistance)
        {
            if (indoorWaypoints == null || indoorWaypoints.Length == 0) return false;
            float nearest = float.PositiveInfinity;
            int nearestWaypoint = -1;
            foreach (int waypoint in indoorWaypoints)
            {
                if (waypoint < 0 || waypoint >= waypointDistances.Length) continue;
                float stop = waypointDistances[waypoint];
                // A strict comparison prevents immediately re-entering the same doorway on exit.
                if (stop <= distance)
                {
                    if (!loop) continue;
                    stop += length;
                }
                if (stop <= nextDistance && stop < nearest)
                {
                    nearest = stop;
                    nearestWaypoint = waypoint;
                }
            }
            if (float.IsPositiveInfinity(nearest)) return false;
            // Keep the exact stored distance, including after wrapping a lap. An epsilon
            // can skip an entrance at high frame rates; modulo rounding can re-enter it.
            distance = waypointDistances[nearestWaypoint];
            RenderPosition();
            float minimum = Mathf.Max(.1f, indoorStay.x);
            waitRemaining = Random.Range(minimum, Mathf.Max(minimum, indoorStay.y));
            IsInsideBuilding = true;
            body.enabled = false;
            return true;
        }

        private void RenderPosition()
        {
            float remaining = distance;
            int count = loop ? route.Length : route.Length - 1;
            for (int i = 0; i < count; i++)
            {
                Vector2 start = route[i], end = route[(i + 1) % route.Length];
                float segment = Vector2.Distance(start, end);
                if (segment <= 0 || remaining > segment) { remaining -= segment; continue; }
                transform.position = Vector2.Lerp(start, end, remaining / segment);
                Vector2 direction = end - start;
                var frames = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
                    ? (direction.x > 0 ? right : left) : (direction.y > 0 ? up : down);
                if (frames != null && frames.Length > 0)
                    body.sprite = frames[(int)(animationTime * animationFps) % frames.Length];
                body.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y * 10);
                return;
            }
        }
    }
}
