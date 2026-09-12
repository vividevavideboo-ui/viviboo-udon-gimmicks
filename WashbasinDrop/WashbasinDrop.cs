using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;

namespace Vivi
{
    /// <summary>
    /// エリアに入った最初のプレイヤーの頭上へ、全員に見えるたらいを落とす。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [AddComponentMenu("Vivi/Washbasin Drop")]
    public class WashbasinDrop : UdonSharpBehaviour
    {
        [Header("たらい設定")]
        [SerializeField, Tooltip("シーンに1個だけ置くたらいの GameObject。通常は非表示にします。")]
        private GameObject washbasin;

        [SerializeField, Tooltip("たらいに付けた Rigidbody。落下の物理挙動に使います。")]
        private Rigidbody washbasinRigidbody;

        [SerializeField, Tooltip("たらいに付けた VRC Object Sync。落下位置を全員に同期します。")]
        private VRCObjectSync washbasinObjectSync;

        [SerializeField, Tooltip("たらいに当たったとき全員へ鳴らす AudioSource。")]
        private AudioSource impactAudioSource;

        [SerializeField, Tooltip("頭の位置からたらいを出現させる高さ（m）。")]
        private float dropHeight = 2.0f;

        [SerializeField, Tooltip("たらいを消して次の落下を可能にするまでの秒数。")]
        private float lifetimeSeconds = 2.0f;

        [Header("内部連携")]
        [SerializeField, Tooltip("たらい側の衝突検知コンポーネント。Editor の配線ボタンで設定します。")]
        private WashbasinDropImpact washbasinImpact;

        [UdonSynced] private bool isDropping;
        [UdonSynced] private bool hasHitPlayer;
        [UdonSynced] private Vector3 dropPosition;

        private Quaternion initialRotation;
        private bool isConfigured;

        private void Start()
        {
            isConfigured = washbasin != null && washbasinRigidbody != null && washbasinObjectSync != null && washbasinImpact != null;
            if (!isConfigured)
            {
                Debug.LogWarning("[WashbasinDrop] たらい、Rigidbody、VRC Object Sync、衝突検知コンポーネントを設定してください。", this);
                return;
            }

            initialRotation = washbasin.transform.rotation;
            washbasin.SetActive(false);
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!isConfigured || !player.isLocal || isDropping) return;

            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            if (localPlayer == null) return;

            // 同期値と物理オブジェクトの所有者をそろえ、同じ人だけが落下を決定する。
            Networking.SetOwner(localPlayer, gameObject);
            Networking.SetOwner(localPlayer, washbasin);

            dropPosition = GetHeadPosition(player) + Vector3.up * dropHeight;
            isDropping = true;
            hasHitPlayer = false;
            RequestSerialization();
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(BeginDrop));
            SendCustomEventDelayedSeconds(nameof(HideWashbasin), lifetimeSeconds);
        }

        public override void OnDeserialization()
        {
            if (!isConfigured) return;

            if (isDropping)
            {
                BeginDrop();
            }
            else
            {
                HideLocally();
            }
        }

        /// <summary>
        /// 同期した出現位置でたらいを有効化するための内部イベントです。
        /// </summary>
        public void BeginDrop()
        {
            if (!isConfigured || !isDropping) return;

            washbasin.SetActive(true);
            washbasinImpact.ResetImpact();

            if (!Networking.IsOwner(washbasin)) return;

            washbasinRigidbody.isKinematic = true;
            washbasin.transform.SetPositionAndRotation(dropPosition, initialRotation);
            washbasinRigidbody.velocity = Vector3.zero;
            washbasinRigidbody.angularVelocity = Vector3.zero;
            washbasinRigidbody.isKinematic = false;
        }

        /// <summary>
        /// プレイヤーに当たった所有者だけが、効果音を一度だけ全員へ送ります。
        /// </summary>
        public void NotifyPlayerHit()
        {
            if (!isConfigured || !isDropping || hasHitPlayer || !Networking.IsOwner(gameObject)) return;

            hasHitPlayer = true;
            RequestSerialization();
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayImpactSound));
        }

        /// <summary>
        /// 全員のクライアントで衝突音を鳴らすための内部イベントです。
        /// </summary>
        public void PlayImpactSound()
        {
            if (impactAudioSource != null) impactAudioSource.Play();
        }

        /// <summary>
        /// 所有者が2秒後にたらいを回収し、回収状態を全員へ同期します。
        /// </summary>
        public void HideWashbasin()
        {
            if (!isConfigured || !Networking.IsOwner(gameObject)) return;

            isDropping = false;
            RequestSerialization();
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(HideLocally));
        }

        /// <summary>
        /// たらいを非表示にするための内部イベントです。
        /// </summary>
        public void HideLocally()
        {
            if (!isConfigured) return;

            washbasinRigidbody.isKinematic = true;
            washbasin.SetActive(false);
        }

        private Vector3 GetHeadPosition(VRCPlayerApi player)
        {
            Vector3 headPosition = player.GetBonePosition(HumanBodyBones.Head);
            if (headPosition == Vector3.zero)
            {
                return player.GetPosition() + Vector3.up * 1.6f;
            }

            return headPosition;
        }
    }
}
