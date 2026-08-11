using Game.Simulation;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// Maps simulation X/Y coordinates into the X/Z presentation plane. The
    /// simulation remains engine-independent while Presentation gains real
    /// height, tilted camera parallax, ground decals, and deterministic depth.
    /// </summary>
    public static class PresentationSpace
    {
        public const float GroundHeight = 0f;
        public const float GroundDecalHeight = 0.015f;
        public const float ActorPivotHeight = 0.72f;
        public const float ProjectileHeight = 0.82f;
        public const float PickupHeight = 0.28f;

        public static readonly Quaternion GroundRotation = Quaternion.Euler(90f, 0f, 0f);

        public static Vector3 ToGround(float simulationX, float simulationY, float height = GroundHeight) =>
            new Vector3(simulationX, height, simulationY);

        public static Vector3 ToEntity(EntityKind kind, float simulationX, float simulationY)
        {
            switch (kind)
            {
                case EntityKind.Area:
                    return ToGround(simulationX, simulationY, GroundDecalHeight);
                case EntityKind.Projectile:
                    return ToGround(simulationX, simulationY, ProjectileHeight);
                case EntityKind.Pickup:
                    return ToGround(simulationX, simulationY, PickupHeight);
                default:
                    return ToGround(simulationX, simulationY, ActorPivotHeight);
            }
        }

        public static Vector2 ToSimulation(Vector3 worldPosition) =>
            new Vector2(worldPosition.x, worldPosition.z);

        public static int DepthOffset(float simulationY) =>
            Mathf.Clamp(-Mathf.RoundToInt(simulationY * 8f), -480, 480);

        public static int PriorityBand(PresentationPriority priority)
        {
            switch (priority)
            {
                case PresentationPriority.CriticalDanger: return 4000;
                case PresentationPriority.Mechanic: return 3000;
                case PresentationPriority.Combat: return 2000;
                default: return 1000;
            }
        }
    }
}
