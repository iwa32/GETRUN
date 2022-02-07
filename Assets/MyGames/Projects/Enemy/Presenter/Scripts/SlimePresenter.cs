using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UniRx;
using UniRx.Triggers;
using Zenject;
using StateView;
using TriggerView;
using EnemyView;
using EnemyModel;
using GameModel;
using StageObject;

namespace EnemyPresenter
{
    public class SlimePresenter : EnemyPresenter, IDamager
    {
        #region//インスペクターから設定
        [SerializeField]
        [Header("追跡エリアのコンポーネントを設定")]
        TrackingAreaView _trackingAreaView;

        [SerializeField]
        [Header("障害物チェックのコンポーネントを設定する")]
        ForwardObstacleCheckView _forwardObstacleCheckView;
        #endregion

        #region//フィールド
        //状態
        RunView _runView;//移動状態のスクリプト
        TrackView _trackView;//追跡状態のスクリプト
        #endregion

        #region//プロパティ
        public int Damage => _powerModel.Power.Value;
        #endregion

        // Start is called before the first frame update
        new void Awake()
        {
            base.Awake();
            _runView = GetComponent<RunView>();
            _trackView = GetComponent<TrackView>();
        }

        void Start()
        {
            Initialize();
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        public void Initialize()
        {
            _runView.DelAction = Run;
            _trackView.DelAction = Track;
            DefaultState();
            Bind();
        }

        void Bind()
        {
            //前方の衝突を監視
            _forwardObstacleCheckView.IsOn
                .Where(isOn => isOn == true
                && (_actionView.HasActionBy(StateType.TRACK) == false)
                )
                .Subscribe(_ => ChangeDirection())
                .AddTo(this);

            //プレイヤーの追跡
            _trackingAreaView.CanTrack
                .Subscribe(canTrack => CheckTracking(canTrack))
                .AddTo(this);
        }

        #region //overrideMethod
        /// <summary>
        /// 接触したコライダーを確認します
        /// </summary>
        /// <param name="collider"></param>
        public override void CheckCollider(Collider collider)
        {
            //武器に接触でダメージを受ける
            CheckPlayerWeaponBy(collider);
        }

        /// <summary>
        /// 衝突を確認します
        /// </summary>
        public override void CheckCollision(Collision collision)
        {

        }

        /// <summary>
        /// 初期時、通常時の状態を設定します
        /// </summary>
        public override void DefaultState()
        {
            _actionView.State.Value = _runView;
        }
        #endregion

        /// <summary>
        /// プレイヤーの武器に接触したか
        /// </summary>
        void CheckPlayerWeaponBy(Collider collider)
        {
            if (collider.TryGetComponent(out IPlayerWeapon playerWeapon))
            {
                //hpを減らす
                _hpModel.ReduceHp(playerWeapon.Power);
                ChangeStateByDamege();
            }
        }

        /// <summary>
        /// 追跡の確認をします
        /// </summary>
        /// <param name="canTrack"></param>
        void CheckTracking(bool canTrack)
        {
            //追跡もしくは前方を走ります
            if (canTrack)
                _actionView.State.Value = _trackView;
            else
                _actionView.State.Value = _runView;
        }

        /// <summary>
        /// 走ります
        /// </summary>
        void Run()
        {
            //前方を移動します
            _navMeshAgent.SetDestination(transform.position + transform.forward);
        }

        /// <summary>
        /// 追跡します
        /// </summary>
        void Track()
        {
            _navMeshAgent.SetDestination(_trackingAreaView.TargetPlayerPosition);
        }

        /// <summary>
        /// 進行方向を変えます
        /// </summary>
        void ChangeDirection()
        {
            ChangeDirectionForRandom();
            _forwardObstacleCheckView.SetIsOn(false);
        }

        /// <summary>
        /// ランダムに進行方向を変える
        /// </summary>
        void ChangeDirectionForRandom()
        {
            //進行方向はランダム
            int dice = RandomDice(1, 5);
            int dirAngle = 90;

            dirAngle *= dice;

            //すでに同じ方向を向いてたら処理を行わない
            if (transform.localEulerAngles.x == dirAngle) return;
            //オイラー値をQuaternionに変換する。引数はz, x, y
            transform.rotation = Quaternion.Euler(0, dirAngle, 0);
        }

        /// <summary>
        /// ランダムな数値を算出
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns>ランダムな数値を出力</returns>
        int RandomDice(int min, int max)
        {
            return UnityEngine.Random.Range(min, max);
        }
    }
}