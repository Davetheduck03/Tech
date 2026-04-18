using UnityEngine;

namespace TowerDefenseTK
{
    public class RoundState_Lose : RoundState_Base
    {
        public override void EnterState(GameManager round)
        {
            // Pause the game when entering the lose state so towers and enemies
            // stop moving while the Game Over panel is visible.
            if (TimeController.Instance != null)
                TimeController.Instance.Pause();

            Debug.Log("[GameManager] Entered Lose state — game paused.");
        }

        public override void ExitState(GameManager round)
        {
            // TimeScale is reset by GameOverScreen.RestartLevel() before the
            // scene reloads, so nothing extra is needed here.
        }

        public override void UpdateState(GameManager round) { }
    }
}
