using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Base class for fully custom tower behaviours.
    ///
    /// Create a ScriptableObject subclass, override <see cref="Tick"/>, then
    /// assign the asset to a <see cref="TowerSO"/>'s <b>Custom Behaviour</b>
    /// field. When a custom behaviour is assigned it <em>completely replaces</em>
    /// the built-in Turret / AoE / Support / Resource enum dispatch for that
    /// tower, giving you full control over what the tower does every frame.
    ///
    /// <para>All tower runtime state is accessed through the
    /// <see cref="TowerBehaviourContext"/> argument, which exposes the tower's
    /// data, effective stats, current target, damage helpers, and projectile
    /// firing.</para>
    ///
    /// <example>
    /// <code>
    /// [CreateAssetMenu(menuName = "TD Toolkit/Behaviours/Burst Fire")]
    /// public class BurstFireBehaviour : TowerBehaviourSO
    /// {
    ///     [Min(1)] public int burstCount = 3;
    ///     [Min(0f)] public float burstCooldown = 2f;
    ///
    ///     public override void Tick(TowerBehaviourContext ctx)
    ///     {
    ///         ctx.FindTarget();
    ///         if (ctx.Target == null || !ctx.CanFire()) return;
    ///
    ///         for (int i = 0; i &lt; burstCount; i++)
    ///             ctx.DealDamageTo(ctx.Target.gameObject);
    ///
    ///         ctx.RegisterFire();
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public abstract class TowerBehaviourSO : ScriptableObject
    {
        /// <summary>
        /// Called once when the tower's <see cref="TowerWeapon"/> initialises.
        /// Override to set up any runtime state your behaviour needs.
        /// The default implementation does nothing.
        /// </summary>
        public virtual void OnInit(TowerBehaviourContext ctx) { }

        /// <summary>
        /// Called every frame in place of the built-in tower type switch.
        /// Implement your full tower logic here: targeting, damage, firing, etc.
        /// </summary>
        public abstract void Tick(TowerBehaviourContext ctx);
    }
}
