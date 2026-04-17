using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseTK
{
    /// <summary>
    /// Central ScriptableObject registry.
    ///
    /// Setup (pick one):
    ///   Option A — Resources folder (automatic):
    ///     Place the SOManager asset at: Assets/Resources/Data/SOManager.asset
    ///     The Instance getter will load it automatically at runtime.
    ///
    ///   Option B — Scene reference (recommended for packages):
    ///     Call SOManager.SetInstance(yourSOManagerAsset) from any scene MonoBehaviour
    ///     before other systems access it (e.g. in Awake on a bootstrap GameObject).
    /// </summary>
    [CreateAssetMenu(fileName = "SOManager", menuName = "TowerDefenseTK/SOManager")]
    public class SOManager : ScriptableObject
    {
        private static SOManager instance;

        public static SOManager Instance
        {
            get
            {
                if (instance != null) return instance;

                // Try to load from Resources as a fallback
                instance = Resources.Load<SOManager>("Data/SOManager");

                if (instance == null)
                    Debug.LogWarning("[SOManager] No instance found. Either place the asset at Resources/Data/SOManager.asset " +
                                     "or call SOManager.SetInstance() during scene setup.");
                return instance;
            }
        }

        /// <summary>
        /// Manually assign the SOManager instance — call this in Awake on a scene bootstrap
        /// object when not using the Resources folder approach.
        /// </summary>
        public static void SetInstance(SOManager so) => instance = so;

        public DamageTable DamageTable;
        public List<TowerSO> towers;
    }
}
