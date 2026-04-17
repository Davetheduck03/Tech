using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Base class for fully custom enemy behaviours.
    ///
    /// Create a ScriptableObject subclass, override <see cref="Tick"/>, then
    /// assign the asset to an <see cref="EnemySO"/>'s <b>Custom Behavior</b>
    /// field.  When assigned it <em>completely replaces</em> the built-in
    /// movement-only loop, giving you full control over what the enemy does
    /// every frame.
    ///
    /// All enemy runtime state is accessed through the
    /// <see cref="EnemyBehaviorContext"/> argument, which exposes components,
    /// path progress, speed overrides, damage helpers, and a blackboard for
    /// per-instance state that survives the pool reuse cycle.
    ///
    /// <example><code>
    /// [CreateAssetMenu(menuName = "TD Toolkit/Behaviours/Enemies/Berserk")]
    /// public class BerserkBehavior : EnemyBehaviorSO
    /// {
    ///     [Range(0f, 1f)] public float enrageThreshold = 0.3f;
    ///     public float enrageSpeedMultiplier = 2f;
    ///
    ///     public override void Tick(EnemyBehaviorContext ctx)
    ///     {
    ///         if (ctx.HealthPercent <= enrageThreshold)
    ///             ctx.SetSpeedMultiplier(enrageSpeedMultiplier);
    ///     }
    /// }
    /// </code></example>
    /// </summary>
    public abstract class EnemyBehaviorSO : ScriptableObject
    {
        /// <summary>
        /// Called once when the enemy's context is first built (Awake).
        /// Override to set up any per-SO runtime state that is
        /// <em>shared</em> across all instances using this asset.
        /// For per-instance state use <see cref="EnemyBehaviorContext.Blackboard"/>.
        /// </summary>
        public virtual void OnInit(EnemyBehaviorContext ctx) { }

        /// <summary>
        /// Called every frame (Update) for the owning enemy.
        /// Implement your full custom logic here — movement tweaks,
        /// ability triggers, conditional despawning, etc.
        /// The default implementation does nothing, leaving standard
        /// movement untouched.
        /// </summary>
        public abstract void Tick(EnemyBehaviorContext ctx);

        /// <summary>
        /// Called each time this enemy is retrieved from the object pool
        /// (after <see cref="OnInit"/>).  Use this to reset any
        /// per-instance state stored in the Blackboard.
        /// </summary>
        public virtual void OnSpawned(EnemyBehaviorContext ctx) { }

        /// <summary>
        /// Called just before the enemy's <see cref="HealthComponent"/> triggers
        /// death and returns the object to the pool.
        /// Use for death-burst effects, minion spawning, etc.
        /// </summary>
        public virtual void OnDeath(EnemyBehaviorContext ctx) { }

        /// <summary>
        /// Called when the enemy collides with the exit zone trigger —
        /// before it deals damage to the player and despawns.
        /// Use to modify <see cref="EnemySO.damageToBase"/> at runtime,
        /// play a VFX, etc.
        /// </summary>
        public virtual void OnReachExit(EnemyBehaviorContext ctx) { }
    }
}
