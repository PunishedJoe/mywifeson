using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.Damage.Systems;
using Content.Server.Effects;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._Crescent;
using Content.Shared._Crescent.ShipShields;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Database;
using Content.Shared.Projectiles;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Projectiles;

public sealed class ProjectileSystem : SharedProjectileSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ColorFlashEffectSystem _color = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly GunSystem _guns = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ProjectileComponent, HullrotBulletHitEvent>(OnBulletHit);
        SubscribeLocalEvent<EmbeddableProjectileComponent, DamageExamineEvent>(OnDamageExamine, after: [typeof(DamageOtherOnHitSystem)]);
    }
    // preety much a copy paste of OnStartCollide.
    // Needed because new SS14 ENGINE update doesn't let you make StartCollideEvents anymore for some reason - SPCR 2025
    private void OnBulletHit(EntityUid uid, ProjectileComponent component, ref HullrotBulletHitEvent args)
    {
        if (component.DamagedEntity || component is { Weapon: null, OnlyCollideWhenShot: true, })
            return;
        if (!HasComp<ShipShieldComponent>(uid) && !args.targetFixture.Hard)
            return;
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;
        var target = args.hitEntity;
        // it's here so this check is only done once before possible hit
        var attemptEv = new ProjectileReflectAttemptEvent(uid, component, false);
        RaiseLocalEvent(target, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            SetShooter(uid, component, target);
            return;
        }

        var ev = new ProjectileHitEvent(component.Damage, target, component.Shooter);
        RaiseLocalEvent(uid, ref ev);

        if (component.gibsOnHit) //used for antimateriel rifles
        {
            _bodySystem.GibBody(target);
        }

        var otherName = ToPrettyString(target);
        var modifiedDamage = _damageableSystem.TryChangeDamage(target, ev.Damage, component.IgnoreResistances, origin: component.Shooter, armorPen: component.HullrotArmorPenetration, stopPower: component.stoppingPower);
        var deleted = Deleted(target);

        if (modifiedDamage is not null && EntityManager.EntityExists(component.Shooter))
        {
            if (modifiedDamage.AnyPositive() && !deleted)
                _color.RaiseEffect(Color.Red, [ target, ], Filter.Pvs(target, entityManager: EntityManager));

            _adminLogger.Add(
                LogType.BulletHit,
                HasComp<ActorComponent>(target) ? LogImpact.Extreme : LogImpact.High,
                $"Projectile {ToPrettyString(uid):projectile} shot by {ToPrettyString(component.Shooter!.Value):user} hit {otherName:target} and dealt {modifiedDamage.GetTotal():damage} damage");
        }

        if (!deleted)
        {
            _guns.PlayImpactSound(target, modifiedDamage, component.SoundHit, component.ForceSound);

            if (!physics.LinearVelocity.IsLengthZero())
                _sharedCameraRecoil.KickCamera(target, physics.LinearVelocity.Normalized());
        }

        // Goobstation start
        if (component.Penetrate)
            component.IgnoredEntities.Add(target);
        else
            component.DamagedEntity = true;
        // Goobstation end

        if (component.DeleteOnCollide)
            QueueDel(uid);

        if (component.ImpactEffect != null && TryComp(uid, out TransformComponent? xform))
            RaiseNetworkEvent(new ImpactEffectEvent(component.ImpactEffect, GetNetCoordinates(xform.Coordinates)), Filter.Pvs(xform.Coordinates, entityMan: EntityManager));
    }

    private void OnStartCollide(EntityUid uid, ProjectileComponent component, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != ProjectileFixture || component.DamagedEntity || component is { Weapon: null, OnlyCollideWhenShot: true, })
            return;
        if (!HasComp<ShipShieldComponent>(uid) && !args.OtherFixture.Hard)
            return;

        var target = args.OtherEntity;
        // it's here so this check is only done once before possible hit
        var attemptEv = new ProjectileReflectAttemptEvent(uid, component, false);
        RaiseLocalEvent(target, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            SetShooter(uid, component, target);
            return;
        }

        var ev = new ProjectileHitEvent(component.Damage, target, component.Shooter);
        RaiseLocalEvent(uid, ref ev);

        if (component.gibsOnHit) //used for antimateriel rifles
        {
            _bodySystem.GibBody(target);
        }

        var otherName = ToPrettyString(target);
        var modifiedDamage = _damageableSystem.TryChangeDamage(target, ev.Damage, component.IgnoreResistances, origin: component.Shooter, armorPen: component.HullrotArmorPenetration, stopPower: component.stoppingPower);
        var deleted = Deleted(target);

        if (modifiedDamage is not null && EntityManager.EntityExists(component.Shooter))
        {
            if (modifiedDamage.AnyPositive() && !deleted)
                _color.RaiseEffect(Color.Red, [ target, ], Filter.Pvs(target, entityManager: EntityManager));

            _adminLogger.Add(
                LogType.BulletHit,
                HasComp<ActorComponent>(target) ? LogImpact.Extreme : LogImpact.High,
                $"Projectile {ToPrettyString(uid):projectile} shot by {ToPrettyString(component.Shooter!.Value):user} hit {otherName:target} and dealt {modifiedDamage.GetTotal():damage} damage");
        }

        if (!deleted)
        {
            _guns.PlayImpactSound(target, modifiedDamage, component.SoundHit, component.ForceSound);

            if (!args.OurBody.LinearVelocity.IsLengthZero())
                _sharedCameraRecoil.KickCamera(target, args.OurBody.LinearVelocity.Normalized());
        }

        // Goobstation start
        if (component.Penetrate)
            component.IgnoredEntities.Add(target);
        else
            component.DamagedEntity = true;
        // Goobstation end

        if (component.DeleteOnCollide || (component.NoPenetrateMask & args.OtherFixture.CollisionLayer) != 0) // Goobstation - Make x-ray arrows not penetrate blob
            QueueDel(uid);

        if (component.ImpactEffect != null && TryComp(uid, out TransformComponent? xform))
            RaiseNetworkEvent(new ImpactEffectEvent(component.ImpactEffect, GetNetCoordinates(xform.Coordinates)), Filter.Pvs(xform.Coordinates, entityMan: EntityManager));
    }

    private void OnDamageExamine(EntityUid uid, EmbeddableProjectileComponent component, ref DamageExamineEvent args)
    {
        if (!component.EmbedOnThrow)
            return;

        if (!args.Message.IsEmpty)
            args.Message.PushNewline();

        var isHarmful = TryComp<EmbedPassiveDamageComponent>(uid, out var passiveDamage) && passiveDamage.Damage.AnyPositive();
        var loc = isHarmful
            ? "damage-examine-embeddable-harmful"
            : "damage-examine-embeddable";

        var staminaCostMarkup = FormattedMessage.FromMarkupOrThrow(Loc.GetString(loc));
        args.Message.AddMessage(staminaCostMarkup);
    }
}
