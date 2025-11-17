using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    public class EnemyManager : MonoBehaviour
    {
        public static EnemyManager Instance { get; private set; }

        private readonly List<BaseEnemy> activeEnemies = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RegisterEnemy(BaseEnemy e)
        {
            if (!activeEnemies.Contains(e))
                activeEnemies.Add(e);
        }

        public void UnregisterEnemy(BaseEnemy e)
        {
            activeEnemies.Remove(e);
        }

        public BaseEnemy GetTarget(Vector3 towerPos, float range,
                                   TargetType type,
                                   TargetGroup group)
        {
            var candidates = GetEnemiesInRange(towerPos, range, group);
            if (candidates.Count == 0) return null;

            return type switch
            {
                TargetType.First => GetFirst(candidates),
                TargetType.Last => GetLast(candidates),
                TargetType.Closest => GetClosest(towerPos, candidates),
                TargetType.Strongest => GetStrongest(candidates),
                TargetType.Weakest => GetWeakest(candidates),
                _ => null
            };
        }

        private List<BaseEnemy> GetEnemiesInRange(Vector3 pos, float range, TargetGroup group)
        {
            var list = new List<BaseEnemy>(activeEnemies.Count);
            foreach (var e in activeEnemies)
            {
                if (!e.gameObject.activeInHierarchy) continue;

                if (group == TargetGroup.Ground && e.isFlying) continue;
                if (group == TargetGroup.Air && !e.isFlying) continue;

                if (Vector3.Distance(pos, e.transform.position) <= range)
                    list.Add(e);
            }
            return list;
        }

        private BaseEnemy GetFirst(List<BaseEnemy> enemies)
        {
            BaseEnemy best = null;
            float bestProg = -1f;
            foreach (var e in enemies)
            {
                float prog = e.totalPathNodes > 0 ? (float)e.nodesPassed / e.totalPathNodes : 0f;
                if (prog > bestProg)
                {
                    bestProg = prog;
                    best = e;
                }
            }
            return best;
        }

        private BaseEnemy GetLast(List<BaseEnemy> enemies)
        {
            BaseEnemy best = null;
            float bestProg = float.MaxValue;
            foreach (var e in enemies)
            {
                float prog = e.totalPathNodes > 0 ? (float)e.nodesPassed / e.totalPathNodes : 0f;
                if (prog < bestProg)
                {
                    bestProg = prog;
                    best = e;
                }
            }
            return best ?? enemies[0];
        }

        private BaseEnemy GetClosest(Vector3 towerPos, List<BaseEnemy> enemies)
        {
            BaseEnemy best = null;
            float bestDist = float.MaxValue;
            foreach (var e in enemies)
            {
                float d = Vector3.SqrMagnitude(towerPos - e.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = e;
                }
            }
            return best;
        }

        private BaseEnemy GetStrongest(List<BaseEnemy> enemies)
        {
            BaseEnemy best = null;
            float bestHp = -1f;
            foreach (var e in enemies)
            {
                var hp = e.GetComponent<HealthComponent>();
                if (hp != null && hp.currentHealth > bestHp)
                {
                    bestHp = hp.currentHealth;
                    best = e;
                }
            }
            return best;
        }

        private BaseEnemy GetWeakest(List<BaseEnemy> enemies)
        {
            BaseEnemy best = null;
            float bestHp = float.MaxValue;
            foreach (var e in enemies)
            {
                var hp = e.GetComponent<HealthComponent>();
                if (hp != null && hp.currentHealth < bestHp)
                {
                    bestHp = hp.currentHealth;
                    best = e;
                }
            }
            return best ?? enemies[0];
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = Color.white;
            foreach (var e in activeEnemies)
            {
                if (!e.gameObject.activeInHierarchy) continue;
                float prog = e.totalPathNodes > 0 ? (float)e.nodesPassed / e.totalPathNodes : 0f;
                Gizmos.color = Color.Lerp(Color.red, Color.green, prog);
                Gizmos.DrawWireSphere(e.transform.position, 0.3f);
            }
        }
    }
}