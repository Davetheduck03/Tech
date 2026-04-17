using System;
using UnityEngine;

namespace TowerDefenseTK
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        [Header("RoundState")]
        public RoundState_Base currentState;
        public RoundState_Start Start = new RoundState_Start();
        public RoundState_Win Win = new RoundState_Win();
        public RoundState_InGame InGame = new RoundState_InGame();
        public RoundState_Lose Lose = new RoundState_Lose();

        private void Awake()
        {
            Instance = this;
        }

        public void Init() => SwitchState(Start);

        public RoundState_Base GetCurrentState() => currentState;

        public void SwitchState(RoundState_Base state)
        {
            currentState?.ExitState(this);
            currentState = state;
            state.EnterState(this);
        }

        private void Update()
        {
            if (currentState == null) return;
            currentState.UpdateState(this);
        }
    }
}
