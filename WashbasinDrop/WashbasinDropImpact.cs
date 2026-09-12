using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Vivi
{
    /// <summary>
    /// たらいがプレイヤーに当たった事実だけを本体へ通知する補助コンポーネント。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [AddComponentMenu("Vivi/Washbasin Drop Impact")]
    public class WashbasinDropImpact : UdonSharpBehaviour
    {
        [SerializeField, Tooltip("落下の状態と効果音を管理する Washbasin Drop。")]
        private WashbasinDrop controller;

        private bool hasNotified;

        public override void OnPlayerCollisionEnter(VRCPlayerApi player)
        {
            if (hasNotified || controller == null || !Networking.IsOwner(gameObject)) return;

            hasNotified = true;
            controller.NotifyPlayerHit();
        }

        /// <summary>
        /// 次の落下で再び衝突を検知できるようにする内部イベントです。
        /// </summary>
        public void ResetImpact()
        {
            hasNotified = false;
        }
    }
}
