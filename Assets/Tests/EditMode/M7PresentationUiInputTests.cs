using System;
using System.Linq;
using System.Reflection;
using Game.Application;
using Game.Core;
using Game.Presentation;
using Game.Simulation;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using NumericsVector2 = System.Numerics.Vector2;

namespace Game.Tests.EditMode
{
    public sealed class M7PresentationUiInputTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [Test]
        public void EntityViewBindsInterpolatesAndRejectsDifferentHandle()
        {
            root = new GameObject("M7ViewTest");
            var view = root.AddComponent<ActorView>();
            var entity = new SpatialEntity(EntityKind.Actor, new EntityHandle(2, 1));
            var other = new SpatialEntity(EntityKind.Actor, new EntityHandle(2, 2));
            view.Bind(entity);
            var entry = new RenderEntitySnapshot(
                entity,
                new NumericsVector2(0f, 0f),
                new NumericsVector2(10f, 4f),
                0f,
                Mathf.PI,
                SimulationStateFlags.Active,
                SimulationStateFlags.Active | SimulationStateFlags.Moving);

            Assert.That(view.Apply(entry, 0.5f, 7), Is.True);
            Assert.That(view.transform.position.x, Is.EqualTo(5f).Within(0.001f));
            Assert.That(view.transform.position.y, Is.EqualTo(PresentationSpace.ActorPivotHeight).Within(0.001f));
            Assert.That(view.transform.position.z, Is.EqualTo(2f).Within(0.001f));
            Assert.That(view.LastSnapshotTick, Is.EqualTo(7));
            var wrong = new RenderEntitySnapshot(other, NumericsVector2.Zero, NumericsVector2.One, 0f, 0f,
                SimulationStateFlags.Active, SimulationStateFlags.Active);
            Assert.That(view.Apply(wrong, 1f, 8), Is.False);
        }

        [Test]
        public void CoordinatorPoolsAllKindsAndRejectsStaleRelease()
        {
            root = new GameObject("M7CoordinatorTest");
            var canvas = new GameObject("Canvas").AddComponent<Canvas>();
            canvas.transform.SetParent(root.transform);
            var coordinator = root.AddComponent<PresentationCoordinator>();
            coordinator.Initialize(canvas, new AccessibilitySettings());
            var world = new SimulationWorld();
            var actor = world.CreateActor(SimulationEntityState.Create(NumericsVector2.Zero, NumericsVector2.Zero));
            world.CreateProjectile(SimulationEntityState.Create(NumericsVector2.One, NumericsVector2.Zero));
            world.CreateArea(SimulationEntityState.Create(NumericsVector2.UnitY, NumericsVector2.Zero));
            world.CreatePickup(SimulationEntityState.Create(new NumericsVector2(2f, 2f), NumericsVector2.Zero));
            var runner = new FixedTickRunner(world);
            runner.Advance(SimulationClock.TickDurationSeconds);

            coordinator.Sync(world.RenderSnapshot, 0.5f);

            Assert.That(coordinator.ActiveViewCount, Is.EqualTo(4));
            Assert.That(coordinator.MissingProfileFallbackCount, Is.EqualTo(4));
            Assert.That(coordinator.TryGetView(new SpatialEntity(EntityKind.Actor, actor), out var actorView), Is.True);
            Assert.That(actorView.GetComponent<SpriteRenderer>().sprite, Is.Not.Null, "missing profile must use fallback");
            Assert.That(coordinator.Release(new SpatialEntity(EntityKind.Actor, new EntityHandle(actor.Index, (ushort)(actor.Generation + 1)))), Is.False);
            Assert.That(coordinator.ActiveViewCount, Is.EqualTo(4));
            coordinator.Clear();
            Assert.That(coordinator.ActiveViewCount, Is.Zero);
        }

        [Test]
        public void InterpolationClampsAndUsesShortestFacingArc()
        {
            var entity = new SpatialEntity(EntityKind.Projectile, new EntityHandle(0, 1));
            var snapshot = new RenderEntitySnapshot(
                entity,
                NumericsVector2.Zero,
                new NumericsVector2(6f, -2f),
                170f * Mathf.Deg2Rad,
                -170f * Mathf.Deg2Rad,
                SimulationStateFlags.Active,
                SimulationStateFlags.Active);

            Assert.That(snapshot.InterpolatePosition(-1f), Is.EqualTo(NumericsVector2.Zero));
            Assert.That(snapshot.InterpolatePosition(2f), Is.EqualTo(new NumericsVector2(6f, -2f)));
            Assert.That(Mathf.Abs(snapshot.InterpolateFacing(0.5f)), Is.EqualTo(Mathf.PI).Within(0.001f));
        }

        [Test]
        public void DirectionalFacingUsesAllFourFormalAtlasRows()
        {
            Assert.That(DirectionalSpriteCatalog.FacingFromRadians(0f), Is.EqualTo(PresentationFacing.Right));
            Assert.That(DirectionalSpriteCatalog.FacingFromRadians(Mathf.PI), Is.EqualTo(PresentationFacing.Left));
            Assert.That(DirectionalSpriteCatalog.FacingFromRadians(Mathf.PI * 0.5f), Is.EqualTo(PresentationFacing.Up));
            Assert.That(DirectionalSpriteCatalog.FacingFromRadians(-Mathf.PI * 0.5f), Is.EqualTo(PresentationFacing.Down));
        }

        [Test]
        public void BossPhasesAndYufengSwordUseFormalStatefulPooledPresentation()
        {
            root = new GameObject("G42DActorPresentation");
            var texture = new Texture2D(4, 4);
            var baseSprites = new Sprite[DirectionalSpriteSet.FacingCount * DirectionalSpriteSet.PoseCount];
            var phaseSprites = new Sprite[DirectionalSpriteSet.FacingCount * DirectionalSpriteSet.BossPhaseCount];
            var libraryType = typeof(ActorView).Assembly.GetType("Game.Presentation.ProceduralVisualLibrary");
            Assert.That(libraryType, Is.Not.Null);
            var library = Activator.CreateInstance(libraryType);
            var trailMaterial = (Material)libraryType.GetProperty(
                "TrailMaterial",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(library);
            try
            {
                for (var index = 0; index < baseSprites.Length; index++)
                    baseSprites[index] = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f, 4f);
                for (var index = 0; index < phaseSprites.Length; index++)
                    phaseSprites[index] = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f, 4f);

                var bossObject = new GameObject("Boss");
                bossObject.transform.SetParent(root.transform, false);
                var boss = bossObject.AddComponent<ActorView>();
                var bossId = ContentId.Create("qinglan.enemy.boss.tingfeng").Value;
                InvokeEntityView(boss, "Configure",
                    new[] { typeof(Sprite), typeof(Color), typeof(Vector2) },
                    baseSprites[0], Color.white, Vector2.one);
                InvokeEntityView(boss, "SetStyleIdentity",
                    new[] { typeof(ContentId), typeof(bool) }, bossId, false);
                InvokeEntityView(boss, "ConfigureAnimation",
                    new[] { typeof(DirectionalSpriteSet) },
                    new DirectionalSpriteSet(bossId, baseSprites, phaseSprites));
                var bossEntity = new SpatialEntity(EntityKind.Actor, new EntityHandle(10, 1));
                boss.Bind(bossEntity);
                InvokeEntityView(boss, "SetBossPhase", new[] { typeof(int) }, 2);
                var bossSnapshot = new RenderEntitySnapshot(
                    bossEntity,
                    NumericsVector2.Zero,
                    NumericsVector2.Zero,
                    0f,
                    0f,
                    SimulationStateFlags.Active,
                    SimulationStateFlags.Active);
                Assert.That(boss.Apply(bossSnapshot, 1f, 1), Is.True);
                Assert.That(boss.BossPhaseFrameActive, Is.True);
                Assert.That(boss.AppliedBossPhase, Is.EqualTo(2));
                Assert.That(boss.GetComponent<SpriteRenderer>().sprite,
                    Is.SameAs(phaseSprites[((int)PresentationFacing.Right * 3) + 2]));

                var playerObject = new GameObject("Player");
                playerObject.transform.SetParent(root.transform, false);
                var player = playerObject.AddComponent<ActorView>();
                var playerId = ContentId.Create("qinglan.character.lu_qingye").Value;
                InvokeEntityView(player, "Configure",
                    new[] { typeof(Sprite), typeof(Color), typeof(Vector2) },
                    baseSprites[0], Color.white, Vector2.one);
                InvokeEntityView(player, "SetStyleIdentity",
                    new[] { typeof(ContentId), typeof(bool) }, playerId, true);
                InvokeEntityView(player, "ConfigureAnimation",
                    new[] { typeof(DirectionalSpriteSet) },
                    new DirectionalSpriteSet(playerId, baseSprites));
                InvokeEntityView(player, "ConfigureHeldWeapon",
                    new[] { typeof(Sprite), typeof(Material) }, baseSprites[0], trailMaterial);
                var playerEntity = new SpatialEntity(EntityKind.Actor, new EntityHandle(11, 1));
                player.Bind(playerEntity);
                var movingSnapshot = new RenderEntitySnapshot(
                    playerEntity,
                    NumericsVector2.Zero,
                    NumericsVector2.UnitX,
                    0f,
                    0f,
                    SimulationStateFlags.Active,
                    SimulationStateFlags.Active | SimulationStateFlags.Moving);
                Assert.That(player.Apply(movingSnapshot, 1f, 2), Is.True);
                Assert.That(player.HeldWeaponState, Is.EqualTo(HeldWeaponPresentationState.Move));
                Assert.That(player.HeldWeaponScale, Is.EqualTo(QinglanPresentationTheme.HeldWeaponMoveScale).Within(0.001f));
                Assert.That(player.transform.Find("WeaponSocket_YufengSword"), Is.Not.Null);
                Assert.That(player.transform.Find("HeldWeapon_YufengSword/YufengSwordTrailTip"), Is.Not.Null);

                InvokeEntityView(player, "PlayAttackReaction", new[] { typeof(float) }, 0.18f);
                player.Apply(movingSnapshot, 1f, 3);
                Assert.That(player.HeldWeaponState, Is.EqualTo(HeldWeaponPresentationState.Attack));
                Assert.That(player.HeldWeaponAttackTrailActive, Is.True);
                Assert.That(player.HeldWeaponScale, Is.EqualTo(QinglanPresentationTheme.HeldWeaponAttackScale).Within(0.001f));
                InvokeEntityView(player, "TickVisual", new[] { typeof(float) }, 0.18f);
                player.Apply(movingSnapshot, 1f, 4);
                Assert.That(player.HeldWeaponState, Is.EqualTo(HeldWeaponPresentationState.Recovery));
                Assert.That(player.HeldWeaponAttackTrailActive, Is.False);
                player.Unbind();
                Assert.That(player.HeldWeaponVisible, Is.False);
                Assert.That(player.HeldWeaponAttackTrailActive, Is.False);
            }
            finally
            {
                (library as IDisposable)?.Dispose();
                for (var index = 0; index < baseSprites.Length; index++)
                    if (baseSprites[index] != null) Object.DestroyImmediate(baseSprites[index]);
                for (var index = 0; index < phaseSprites.Length; index++)
                    if (phaseSprites[index] != null) Object.DestroyImmediate(phaseSprites[index]);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void InputMapsSwitchAndKeyboardGamepadBindingsExist()
        {
            root = new GameObject("M7InputTest");
            var router = root.AddComponent<M7InputRouter>();
            router.Initialize();

            Assert.That(router.Actions.actionMaps.Select(value => value.name),
                Is.EquivalentTo(new[] { "Gameplay", "UI", "Debug" }));
            Assert.That(router.Actions.FindAction("Gameplay/Move").bindings.Any(value => value.path.Contains("Keyboard")), Is.True);
            Assert.That(router.Actions.FindAction("Gameplay/Move").bindings.Any(value => value.path.Contains("Gamepad")), Is.True);
            Assert.That(router.Actions.FindAction("UI/Submit").bindings.Any(value => value.path.Contains("Gamepad")), Is.True);
            router.SetGameplayMode(true);
            Assert.That(router.GameplayMap.enabled, Is.True);
            Assert.That(router.UiMap.enabled, Is.False);
            router.SetGameplayMode(false);
            Assert.That(router.GameplayMap.enabled, Is.False);
            Assert.That(router.UiMap.enabled, Is.True);
            Assert.That(router.ApplyBindingOverride("UI/Submit", 0, "<Keyboard>/numpadEnter"), Is.True);
            Assert.That(router.Actions.FindAction("UI/Submit").bindings[0].overridePath, Is.EqualTo("<Keyboard>/numpadEnter"));
        }

        [Test]
        public void AccessibilitySettingsClampAndToggle()
        {
            var settings = new AccessibilitySettings();
            settings.SetStickDeadzone(2f);
            settings.SetVibrationIntensity(-1f);
            settings.SetFlashIntensity(0.4f);
            settings.SetScreenShakeEnabled(false);
            settings.SetDamageNumbersEnabled(false);
            settings.SetAutoAim(AutoAimStrategy.MovementDirection);

            Assert.That(settings.StickDeadzone, Is.EqualTo(0.95f));
            Assert.That(settings.VibrationIntensity, Is.Zero);
            Assert.That(settings.FlashIntensity, Is.EqualTo(0.4f));
            Assert.That(settings.ScreenShakeEnabled, Is.False);
            Assert.That(settings.DamageNumbersEnabled, Is.False);
            Assert.That(settings.AutoAim, Is.EqualTo(AutoAimStrategy.MovementDirection));
        }

        [Test]
        public void UiAssemblyAndViewsExposeNoSimulationStoreWrites()
        {
            var forbidden = new[] { typeof(SimulationWorld), typeof(ActorStore), typeof(ProjectileStore), typeof(AreaStore), typeof(PickupStore) };
            var uiTypes = typeof(GameFlowPresenter).Assembly.GetTypes();
            foreach (var type in uiTypes)
            foreach (var method in type.GetMethods())
                Assert.That(forbidden.Contains(method.ReturnType), Is.False, type.FullName + "." + method.Name);

            var viewMethods = typeof(EntityView).GetMethods();
            Assert.That(viewMethods.Any(value => value.Name.Contains("Damage", StringComparison.OrdinalIgnoreCase)), Is.False);
        }

        [Test]
        public void CameraBoundsAndEffectsToggleAreHonored()
        {
            root = new GameObject("M7CameraTest");
            var target = new GameObject("Target");
            target.transform.SetParent(root.transform);
            target.transform.position = new Vector3(20f, PresentationSpace.ActorPivotHeight, -20f);
            var cameraObject = new GameObject("Camera");
            cameraObject.transform.SetParent(root.transform);
            var camera = cameraObject.AddComponent<Camera>();
            var rig = cameraObject.AddComponent<PresentationCameraRig>();
            rig.ConfigureTiltedOrthographic(camera);
            rig.SetTarget(target.transform);
            rig.SetBounds(new Rect(-5f, -4f, 10f, 8f));
            rig.EffectsEnabled = false;
            rig.RequestShake(3f, 1f);
            rig.TickCamera(0.2f);

            Assert.That(rig.transform.position.x, Is.EqualTo(5f));
            Assert.That(rig.LastStablePosition.z, Is.EqualTo(-13.4f).Within(0.001f));
            Assert.That(rig.transform.position.y, Is.GreaterThan(12f));
            Assert.That(rig.UsesTiltedOrthographicProjection, Is.True);
            Assert.That(camera.orthographic, Is.True);

            target.transform.position = new Vector3(4f, PresentationSpace.ActorPivotHeight, -3f);
            rig.TickCamera(0.25f);
            Assert.That(rig.MotionLead.magnitude, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(rig.MotionLead.x, Is.LessThan(0f));
            Assert.That(rig.MotionLead.y, Is.GreaterThan(0f));
        }

        [Test]
        public void CombatEventsBecomePooledHitAndDeathRequests()
        {
            root = new GameObject("M7CombatPresentationTest");
            var canvas = new GameObject("Canvas").AddComponent<Canvas>();
            canvas.transform.SetParent(root.transform);
            var coordinator = root.AddComponent<PresentationCoordinator>();
            coordinator.Initialize(canvas, new AccessibilitySettings());
            var world = new SimulationWorld();
            var source = world.CreateActor(
                SimulationEntityState.Create(NumericsVector2.Zero, NumericsVector2.Zero),
                ActorCombatInitialization.CreateDefault(100f));
            var target = world.CreateActor(
                SimulationEntityState.Create(NumericsVector2.One, NumericsVector2.Zero),
                ActorCombatInitialization.CreateDefault(5f));
            var sourceId = ContentId.Create("test.presentation.hit").Value;
            world.QueueDamage(new DamagePacket(
                new SpatialEntity(EntityKind.Actor, source),
                new SpatialEntity(EntityKind.Actor, target),
                sourceId,
                DamageType.Physical,
                DamageTags.Direct,
                10f,
                false,
                1f,
                NumericsVector2.Zero,
                NumericsVector2.One,
                0));
            var runner = new FixedTickRunner(world);
            runner.Advance(SimulationClock.TickDurationSeconds);

            coordinator.ConsumeLatestEvents(world.RenderSnapshot.Tick, world.Events, world.CombatEvents);
            coordinator.Sync(world.RenderSnapshot, 1f);

            Assert.That(coordinator.LastHitRequestCount, Is.EqualTo(1));
            Assert.That(coordinator.LastDeathRequestCount, Is.EqualTo(1));
            Assert.That(coordinator.ActiveVfxCount, Is.EqualTo(2));
            Assert.That(coordinator.ActiveDamageNumberCount, Is.EqualTo(1));
            coordinator.TickEffects(1f);
            Assert.That(coordinator.ActiveVfxCount, Is.Zero);
            Assert.That(coordinator.ActiveDamageNumberCount, Is.Zero);
        }

        [Test]
        public void PresentationRequestBufferSupportsStatusRequestsWithoutSimulationMutation()
        {
            var buffer = new PresentationRequestBuffer(1);
            var target = new SpatialEntity(EntityKind.Actor, new EntityHandle(3, 1));
            var status = new PresentationRequest(
                PresentationRequestType.Status,
                target,
                new NumericsVector2(2f, 4f),
                2f,
                false,
                ContentId.Create("test.status.presentation").Value);

            buffer.Add(status);

            Assert.That(buffer.Count, Is.EqualTo(1));
            Assert.That(buffer.GetAt(0).Type, Is.EqualTo(PresentationRequestType.Status));
            Assert.That(buffer.GetAt(0).Target, Is.EqualTo(target));
            buffer.Clear();
            Assert.That(buffer.Count, Is.Zero);
        }

        private static object InvokeEntityView(
            EntityView target,
            string methodName,
            Type[] signature,
            params object[] arguments)
        {
            var method = typeof(EntityView).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                signature,
                null);
            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(target, arguments);
        }
    }
}
