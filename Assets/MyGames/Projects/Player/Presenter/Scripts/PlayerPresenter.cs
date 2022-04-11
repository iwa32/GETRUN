using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Cysharp.Threading.Tasks;
using Zenject;
using PlayerModel;
using GameModel;
using PlayerView;
using StateView;
using TriggerView;
using StageObject;
using SpWeaponDataList;
using SoundManager;
using static StateType;
using static SEType;
using PlayerWeapon;

namespace PlayerPresenter
{
    public class PlayerPresenter : MonoBehaviour
    {
        #region//インスペクターから設定
        [SerializeField]
        [Header("プレイヤーの初期hpを設定")]
        int _initialHp = 3;

        [SerializeField]
        [Header("プレイヤーの武器の攻撃力を設定")]
        int _initialPower = 0;

        [SerializeField]
        [Header("プレイヤーの移動速度")]
        float _speed = 10.0f;

        [SerializeField]
        [Header("プレイヤーの点滅時間")]
        float _blinkTime = 3.0f;

        [SerializeField]
        [Header("ノックバック時の飛ぶ威力")]
        float _knockBackPower = 10.0f;

        [SerializeField]
        [Header("HPのUIを設定")]
        HpView _hpView;

        [SerializeField]
        [Header("SP武器表示用のUIを設定")]
        SpWeaponView _spWeaponView;

        [SerializeField]
        [Header("装備武器を設定")]
        PlayerSword _playerWeapon;

        #region//インスペクターから設定
        [SerializeField]
        [Header("SpWeaponのScritableObjectを設定")]
        SpWeaponDataList.SpWeaponDataList _spWeaponDataList;
        #endregion
        #endregion

        #region//フィールド
        ActionView _actionView;//プレイヤーのアクション用スクリプト
        WaitState _waitState;//待機状態のスクリプト
        RunState _runState;//移動状態のスクリプト
        DownState _downState;//ダウン状態のスクリプト
        DeadState _deadState;//デッド状態のスクリプト
        AttackState _attackState;//攻撃状態のスクリプト
        JoyState _joyState;//喜び状態のスクリプト
        TriggerView.TriggerView _triggerView;//接触判定スクリプト
        CollisionView _collisionView;//衝突判定スクリプト
        InputView _inputView;//プレイヤーの入力取得スクリプト
        Rigidbody _rigidBody;
        Animator _animator;
        ObservableStateMachineTrigger _animTrigger;
        SpWeaponData _currentSpWeaponData = new SpWeaponData();//現在取得している武器情報を保持
        IDirectionModel _directionModel;
        IWeaponModel _weaponModel;
        IHpModel _hpModel;
        IScoreModel _scoreModel;
        IPointModel _pointModel;
        ISoundManager _soundManager;
        bool _isBlink;//点滅状態か
        #endregion

        #region//プロパティ
        #endregion

        [Inject]
        public void Construct(
            IWeaponModel weapon,
            IHpModel hp,
            IScoreModel score,
            IPointModel point,
            IDirectionModel direction,
            ISoundManager soundManager
        )
        {
            _weaponModel = weapon;
            _hpModel = hp;
            _scoreModel = score;
            _pointModel = point;
            _directionModel = direction;
            _soundManager = soundManager;
        }

        /// <summary>
        /// プレハブのインスタンス直後の処理
        /// </summary>
        public void ManualAwake()
        {
            _actionView = GetComponent<ActionView>();
            _waitState = GetComponent<WaitState>();
            _runState = GetComponent<RunState>();
            _downState = GetComponent<DownState>();
            _deadState = GetComponent<DeadState>();
            _attackState = GetComponent<AttackState>();
            _joyState = GetComponent<JoyState>();
            _triggerView = GetComponent<TriggerView.TriggerView>();
            _collisionView = GetComponent<CollisionView>();
            _inputView = GetComponent<InputView>();
            _rigidBody = GetComponent<Rigidbody>();
            _animator = GetComponent<Animator>();
            _animTrigger = _animator.GetBehaviour<ObservableStateMachineTrigger>();
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        public void Initialize()
        {
            InitializeModel();
            InitializeView();
            Bind();
        }

        /// <summary>
        /// モデルの初期化を行います
        /// </summary>
        void InitializeModel()
        {
            _weaponModel.SetPower(_initialPower);
            _hpModel.SetHp(_initialHp);
        }

        /// <summary>
        /// ビューの初期化を行います
        /// </summary>
        void InitializeView()
        {
            _runState.DelAction = Run;
            _actionView.State.Value = _waitState;
            _playerWeapon.Initialize();
        }

        /// <summary>
        /// リセットします
        /// </summary>
        public void ResetData()
        {
            _actionView.State.Value = _waitState;
            InitializeModel();
        }

        /// <summary>
        /// modelとviewの監視、処理
        /// </summary>
        void Bind()
        {
            //modelの監視
            _hpModel.Hp.Subscribe(hp => _hpView.SetHpGauge(hp)).AddTo(this);

            //trigger, collisionの取得
            _triggerView.OnTriggerEnter()
                .Where(_ => _directionModel.CanGame())
                .Subscribe(collider => CheckTrigger(collider))
                .AddTo(this);

            _collisionView.OnCollisionEnter()
                .Where(_ => _directionModel.CanGame())
                .Subscribe(collision => CheckCollision(collision))
                .AddTo(this);

            //viewの監視
            //状態の監視
            _actionView.State
                .Where(x => x != null)
                .Subscribe(x =>
                {
                    _actionView.ChangeState(x.State);
                })
                .AddTo(this);

            //入力の監視
            _inputView.InputDirection
                .Where(_ => _directionModel.CanGame())
                .Subscribe(input =>
                {
                    //攻撃中に入力した場合攻撃モーションを終了する
                    if (_actionView.HasStateBy(ATTACK))
                        _playerWeapon.EndMotion();

                    ChangeStateByInput(input);
                }
                )
                .AddTo(this);

            //攻撃入力
            //剣での攻撃
            _inputView.IsFired
                .Where(x => (x == true)
                && _directionModel.CanGame()
                && IsControllableState())
                .Subscribe(_ => ChangeAttack())
                .AddTo(this);

            //SP武器での攻撃
            _inputView.IsSpAttack
                .Where(x => (x == true)
                && _directionModel.CanGame()
                && IsControllableState()
                && _currentSpWeaponData.SpWeapon != null
                )
                .Subscribe(_ => Debug.Log("spAttack"))
                .AddTo(this);

            //アニメーションの監視
            _animTrigger.OnStateExitAsObservable()
                .Where(s => s.StateInfo.IsName("Attack")
                || s.StateInfo.IsName("Attack2")
                || s.StateInfo.IsName("Attack3")
                )
                .Subscribe(_ =>
                {
                    _playerWeapon.EndMotion();
                    _animator.ResetTrigger("ContinuousAttack");

                    if (_actionView.HasStateBy(ATTACK))
                        _actionView.State.Value = _waitState;
                });

            _animTrigger.OnStateExitAsObservable()
                .Where(s => s.StateInfo.IsName("Down"))
                .Subscribe(_ =>
                {
                    _actionView.State.Value = _waitState;
                })
                .AddTo(this);
        }

        /// <summary>
        /// 攻撃状態に切り替えます
        /// </summary>
        void ChangeAttack()
        {
            _playerWeapon.StartMotion();

            //連続攻撃
            if (_actionView.HasStateBy(ATTACK))
            {
                _animator.SetTrigger("ContinuousAttack");
            }
            _actionView.State.Value = _attackState;

        }

        /// <summary>
        /// 操作可能な状態か
        /// </summary>
        bool IsControllableState()
        {
            return (_actionView.HasStateBy(RUN)
                || _actionView.HasStateBy(WAIT)
                || _actionView.HasStateBy(ATTACK));
        }

        /// <summary>
        /// fixedUpdate処理
        /// </summary>
        public void ManualFixedUpdate()
        {
            _actionView.Action();
        }

        /// <summary>
        /// 接触時に確認します
        /// </summary>
        /// <param name="collider"></param>
        void CheckTrigger(Collider collider)
        {
            GetPointItemBy(collider);
            GetSpWeaponItemBy(collider);

            ReceiveDamageBy(collider);
        }

        /// <summary>
        /// 衝突時に確認します
        /// </summary>
        void CheckCollision(Collision collision)
        {
            ReceiveDamageBy(collision.collider);
        }

        /// <summary>
        /// ポイントアイテムの取得を試みます
        /// </summary>
        void GetPointItemBy(Collider collider)
        {
            if (collider.TryGetComponent(out IPointItem pointItem))
            {
                _soundManager.PlaySE(POINT_GET);
                _pointModel.AddPoint(pointItem.Point);
                _scoreModel.AddScore(pointItem.Score);
                pointItem.Destroy();
            }
        }

        /// <summary>
        /// Sp武器の取得を試みます
        /// </summary>
        /// <param name="collider"></param>
        void GetSpWeaponItemBy(Collider collider)
        {
            //Sp武器ならスコアを獲得し、自身のアイテム欄にセット
            if (collider.TryGetComponent(out ISpWeaponItem spWeaponItem) == false) return;

            SpWeaponData spWeaponData
                = _spWeaponDataList.FindSpWeaponDataByType(spWeaponItem.Type);

            if (spWeaponData == null) return;

            //SE

            //武器が違う場合のみセットする
            if (_currentSpWeaponData.Type != spWeaponData.Type)
            {
                _spWeaponView.SetIcon(spWeaponData.UIIcon);
                _currentSpWeaponData = spWeaponData;
            }

            Debug.Log(spWeaponData.Type.ToString());

            _scoreModel.AddScore(spWeaponItem.Score);
            spWeaponItem.Destroy();
        }

        /// <summary>
        /// ダメージを受けるか確認します
        /// </summary>
        void ReceiveDamageBy(Collider collider)
        {
            if (_isBlink) return;
            if (_actionView.HasStateBy(ATTACK)) return;//攻撃中はダメージを受けない
            if (collider.TryGetComponent(out IDamager damager))
            {
                _soundManager.PlaySE(DAMAGED);
                _hpModel.ReduceHp(damager.Damage);
                ChangeStateByDamage();
                KnockBack(collider?.gameObject);
            }
        }

        /// <summary>
        /// hpを増やします
        /// </summary>
        /// <param name="hp"></param>
        public void AddHp(int hp)
        {
            //hpは初期値以上は増えないようにする
            if (_hpModel.Hp.Value >= _initialHp) return;
            _soundManager.PlaySE(HP_UP);
            _hpModel.AddHp(hp);
        }

        /// <summary>
        /// 入力の有無でプレイヤーの状態を切り替えます
        /// </summary>
        /// <param name="input"></param>
        void ChangeStateByInput(Vector2 input)
        {
            if (input.magnitude != 0)
                _actionView.State.Value = _runState;
            else
                _actionView.State.Value = _waitState;
        }

        /// <summary>
        /// ダメージによってプレイヤーの状態を切り替えます
        /// </summary>
        void ChangeStateByDamage()
        {
            if (_hpModel.Hp.Value > 0)
                ChangeDown();
            else ChangeDead();
        }

        void ChangeDown()
        {
            _actionView.State.Value = _downState;
            PlayerBlinks().Forget();//点滅処理
        }

        public void ChangeDead()
        {
            _actionView.State.Value = _deadState;
            _directionModel.SetIsGameOver(true);
        }

        public void ChangeJoy()
        {
            _actionView.State.Value = _joyState;
        }

        /// <summary>
        /// ノックバックします
        /// </summary>
        void KnockBack(GameObject target)
        {
            //ノックバック方向を取得
            Vector3 knockBackDirection = (transform.position - target.transform.position).normalized;

            //速度ベクトルをリセット
            _rigidBody.velocity = Vector3.zero;
            knockBackDirection.y = 0;//Y方向には飛ばないようにする
            _rigidBody.AddForce(knockBackDirection * _knockBackPower, ForceMode.VelocityChange);
        }

        /// <summary>
        /// プレイヤーの点滅
        /// </summary>
        async UniTask PlayerBlinks()
        {
            bool isActive = false;
            float elapsedBlinkTime = 0.0f;

            _isBlink = true;
            while (elapsedBlinkTime <= _blinkTime)
            {
                SetActiveToAllChild(isActive);
                isActive = !isActive;
                await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
                elapsedBlinkTime += 0.2f;
            }

            SetActiveToAllChild(true);
            _isBlink = false;
        }

        /// <summary>
        /// 子要素を全てアクティブ・非アクティブにする
        /// </summary>
        /// <param name="isActive"></param>
        void SetActiveToAllChild(bool isActive)
        {
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.SetActive(isActive);
            }
        }

        /// <summary>
        /// 走ります
        /// </summary>
        void Run()
        {
            if (_directionModel.CanGame() == false) return;

            Vector2 input = _inputView.InputDirection.Value;
            Move(input);
            Rotation(input);
        }

        /// <summary>
        /// 移動します
        /// </summary>
        /// <param name="input"></param>
        void Move(Vector2 input)
        {
            //入力があった場合
            if (input != Vector2.zero)
            {
                Vector3 movePos = new Vector3(input.x, 0, input.y);
                _rigidBody.velocity = movePos * _speed;
            }
        }

        /// <summary>
        /// 回転します
        /// </summary>
        /// <param name="input"></param>
        void Rotation(Vector2 input)
        {
            _rigidBody.rotation = Quaternion.LookRotation(new Vector3(input.x, 0, input.y));
        }
    }
}