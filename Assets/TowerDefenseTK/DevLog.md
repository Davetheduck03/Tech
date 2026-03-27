IGTP Dev Log

This week's work was focused on expanding the tower side of the toolkit with three new archetypes, a reworked projectile system, two new AOE sub-types, and improved designer tooling. Everything was built to be data-driven and configurable from the Inspector without touching code.

1. AOE Slow Turret
The existing status effect system already applied on-hit effects to everything a projectile hit, so the Slow Turret needed no new code — just a correctly configured TowerSO asset pointing at the Slow StatusEffectSO. It serves as a good demonstration that the toolkit's data-driven approach keeps new tower variants cheap to add.

2. Buff Tower
Added a full TowerBuffSO/TowerBuffComponent system for tower-to-tower buffing. The Buff Tower pulses a TowerBuffSO asset every 0.5s to all allied towers in range that carry a TowerBuffComponent, applying multiplicative fire rate, damage, and range multipliers with independent timers per buff source. Active buffs tint the tower mesh so the effect is visually clear at a glance.

3. Miner Tower
Added ResourceTick() to TowerWeapon for passive gold income. The timer subtracts 1s rather than resetting to zero so fractional ticks don't accumulate drift over long sessions. Gold per second is a single float on TowerSO, keeping the miner trivial to configure.

4. Predictive Targeting & Ballistic Arc
Rewrote BaseProjectile to support two flight modes. Fixed-target mode samples the enemy's position at launch time and flies a ballistic arc using sin(t × π) for Y offset, so the arc stays clean even if the target moves or dies mid-flight. Live-tracking mode keeps refreshing the target position for homing behaviour. Both modes share the same Impact() AOE detonation on arrival.

5. Cone AOE & Circle AOE
Added two instant-damage AOE sub-types alongside the existing Turret_AOE projectile mode. Cone fires a forward-facing arc at the tower's fire rate using an OverlapSphere filtered by angle against the weapon's forward direction. Circle pulses 360° damage at fire rate with no rotation needed. Both are selected via the AOEType enum on TowerSO.

6. ProjectileConfig
Introduced a small ProjectileConfig struct on TowerSO holding arcHeight and useFixedTarget. Keeping these in the data layer rather than hardcoded on the prefab means designers can tune arc height per tower type from the Inspector and mix arcing with homing freely.

7. Conditional TowerSOInspector
Wrote a custom [CustomEditor] for TowerSO that hides irrelevant fields based on the selected TowerType and AOEType. Projectile Config only appears for Turret_AOE towers, Cone Angle only for Cone, Tower Buff for Support, and Gold/s for Resource. Warning boxes flag misconfigured towers, keeping the asset workflow clean.

8. W_TowerDataEditor Additions
Extended the existing tower spreadsheet editor with two new sections: Projectile Settings (arc height and fixed-target toggle, shown only for Turret_AOE) and Specialisation Settings (gold/s for Resource, tower buff reference for Support). This lets designers tweak these values across multiple towers side-by-side without opening individual assets.


Reflection
Having a clear data-driven foundation made all three new tower types fast to implement — the Slow Turret needed zero new code and the Miner was a handful of lines. The trickiest part was the ballistic arc: fixed-target mode was needed to keep the arc stable, which required storing the target position at launch time rather than tracking live. The conditional inspector significantly reduces inspector noise and should prevent common misconfiguration mistakes.

Next Week Goals
* Add OnEnemyDied and placement/wave event hooks for toolkit extensibility.
* Implement TowerBehaviourSO open type system to let users define custom tower logic without modifying TowerWeapon.
* Add a Timeline view to the Wave Editor.
