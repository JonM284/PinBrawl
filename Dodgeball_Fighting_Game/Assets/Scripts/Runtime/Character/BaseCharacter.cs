using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data;
using Data.AbilityDatas;
using Data.PerkDatas;
using Data.StatusDatas;
using DG.Tweening;
using GameControllers;
using Project.Scripts.Utils;
using Rewired;
using Runtime.Abilities;
using Runtime.GameControllers;
using Runtime.Gameplay;
using Runtime.Gameplay.Sensors;
using Runtime.GameplayInterfaces;
using Runtime.Perks;
using Runtime.Statuses;
using Runtime.VFX;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

namespace Runtime.Character
{
    
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public abstract class BaseCharacter: MonoBehaviour, IDamagable, IKnockbackable
    {
        
        #region Read-Only
        
        //Shaders
        private static readonly int markerColorName = Shader.PropertyToID("_Tint");
        private static readonly int aimColorName = Shader.PropertyToID("_Color");

        //Timer Identifiers
        private readonly string ballHitTimerIdentifier = "Ball_Hit_Timer";
        private readonly string ballCooldownTimerIdentifier = "Ball_Cooldown";
        private readonly string knockbackTimerIdentifer = "Hit_Stun";
        private readonly string actionUseTimerIdentifier = "Using_Action";
        private readonly string shieldPopTimerIdentifier = "SHIELD_POP@Identifier";
        private readonly string evadeUseTimerIdentifier = "EVADE@Identifier";
        private readonly string parryTimerIdentifier = "PARRY@Identifier";

        //Input Actions
        private readonly string ballMeleeActionName = "Ball_Melee";
        private readonly string primaryAbilityActionName = "Primary_Ability";
        private readonly string secondaryAbilityActionName = "Secondary_Ability";
        private readonly string energyShieldActionName = "Energy_Shield";
        private readonly string dashAbilityActionName = "Select";
        
        #endregion

        #region Actions

        public static event Action<BaseCharacter> OnPlayerPreDeath;
        
        public static event Action<BaseCharacter, BaseCharacter, Vector3> OnPlayerDeath;
        
        public static event Action<BaseCharacter> OnPlayerRevived;

        public static event Action<BaseCharacter, float, BaseCharacter> OnArmorAmountChanged;

        public static event Action<BaseCharacter, float, BaseCharacter> OnDamagePercentageChanged; 

        public static event Action<BaseCharacter, float> OnMaxArmorChanged;

        public static event Action OnAbilitiesAssigned;
        
        public static event Action<BaseCharacter> OnWackUsed;

        public static event Action OnSingleAbilityAssigned;

        public static event Action<BaseCharacter, AbilityBase> OnAbilityUsed; 
        
        public static event Action<BaseCharacter, StatusData> OnStatusApplied;
        
        public static event Action<BaseCharacter, StatusData> OnStatusRemoved;
        
        public static event Action<BaseCharacter> OnEvadeUsed;

        #endregion

        #region Serialized Fields

        [SerializeField] private Transform m_aimIndicator;

        [SerializeField] private Transform m_sphereCheckLocation;

        [SerializeField] protected Transform m_characterModelHolder;
        
        [SerializeField] private Transform m_indicatorHolder;

        [SerializeField] private GameObject m_meleeIndicator;

        [SerializeField] private VFXPlayer m_wackVFX;
        [SerializeField] private VFXPlayer m_wackChargeVFX;
        [SerializeField] private VFXPlayer m_shieldPopVFX;
        [SerializeField] private VFXPlayer m_shieldPopStunVFX;
        [SerializeField] private VFXPlayer m_knockbackVFX;
        
        [SerializeField] private MeshRenderer m_marker;

        [SerializeField] private MeshRenderer m_aimMarker;
        
        [SerializeField] private Transform m_healthbarLoc;
        
        [SerializeField] private Transform m_statusHolder;

        [SerializeField] private AbilityData m_dashAbilityData;

        [SerializeField] private EnergyShieldBehaviour energyShieldBehaviour;

        [SerializeField] private List<AudioClip> m_wackSounds = new List<AudioClip>();

        [SerializeField] protected List<AudioClip> m_damagedSounds = new List<AudioClip>();
        
        #endregion

        #region Private Fields
        
        //float
        protected float m_originalSpeed, m_currentSpeed;
        protected float m_currentDamagedAmount, m_damageAmountThreshold = 100f;
        protected float m_damagePercentage, m_damagePercentageKnockbackMod, m_maxDamagePercentageKnockbackMod = 18f;
        protected float m_ballConnectBuildUpTimerCurrent, m_ballConnectBuildUpTimerMax = 1.1f, m_calcMaxBuildUpTimer, 
            m_ballBuildUpPercentage;
        protected float m_knockbackForce, m_knockbackTime;
        protected float m_ballMeleeChargeAmount, m_ballMeleeChargeAmountMax = 1f;
        protected float m_damageIntakeMod = 1f;
        protected float m_meleeChargeThreshold = 0.8f;
        protected float m_hitStunMaxTime = 0.1f, m_hitStunMaxFrequency = 0.1f, m_hitStunKnockbackThreshold = 150f;
        protected float m_currentHitStunTime, m_currentHitStunFrequency;
        protected float m_armoredKnockbackReductionRate = 15f, m_unarmoredKnockbackReductionRate = 8f;
        protected float m_speedModifier = 1f, m_armorModifier = 1f, m_wackSizeModifier = 1f;
        protected float m_wackRangeCurrent, m_wackTimeMax = 0.15f;
        protected float m_energyShieldPopStunDuration = 10f;
        
        //bool
        protected bool m_isAlive = true;
        protected bool m_isInitialized;
        protected bool m_canReadPlayerInput = true;
        protected bool m_isStunned, m_isKnockedBack, m_isWaiting, m_isShieldPopStunned;
        protected bool m_isInAction;
        protected bool m_playerHittingBall, m_canCheckBallInput = true, m_isCharging;
        protected bool m_hasPlayedVFX;
        protected bool canUseAbilities;
        
        protected int m_playerIndex;
        protected int m_hitStunMaxVirbrato = 25, m_currentVirbrato;
        protected int m_currentHeldAbility = -1, m_heldResetNum = -1;
        
        protected Vector3 m_characterMoveVector, m_knockbackMoveVector;
        protected Vector3 m_inputMoveDir, m_inputAimDir;
        protected Vector3 m_hitStunV3FrequencyCurrent;

        protected LayerMask m_groundMask, m_wallLayerMask;
        
        protected Player m_player;

        protected CharacterController m_characterController;

        protected AudioSource m_audioSource;
        
        protected List<CustomTimer> m_currentTimers = new List<CustomTimer>();

        protected Camera m_mainCamera;

        protected BaseCharacter m_lastAttackingPlayer;

        protected Collider[] m_hitColliders = new Collider[6];
        protected int m_hitAmount;
        
        //abilities
        private List<AbilityBase> m_assignedAbilities = new List<AbilityBase>();
        private List<AbilityBase> m_abilitiesOnCooldown = new List<AbilityBase>();
        private List<AbilityBase> m_cooldownRemovableAbilities = new List<AbilityBase>();

        private AbilityBase m_dashAbility;
        private AbilityData m_selectedAbility;

        //status
        private List<StatusEntityBase> m_currentStatuses = new List<StatusEntityBase>();
        private List<string> m_currentStatusGUIDs = new List<string>();
        private List<StatusEntityBase> m_removableStatuses = new List<StatusEntityBase>();

        private List<PerkEntityBase> m_currentPerks = new List<PerkEntityBase>();

        private int m_abilityUseCharges = 0;

        private CancellationTokenSource cts = new CancellationTokenSource();

        private SemaphoreSlim wackBallSemaphoreSlim = new SemaphoreSlim(1,1);

        #endregion

        #region Accessors

        public CharacterData characterData { get; protected set; }

        public CharacterController characterController => CommonUtils.GetRequiredComponent(ref m_characterController, () =>
        {
            TryGetComponent(out CharacterController cc);
            return cc;
        });
        
        public AudioSource aSource => CommonUtils.GetRequiredComponent(ref m_audioSource, () =>
        {
            TryGetComponent(out AudioSource cc);
            return cc;
        });

        public bool m_stopCooldownTimer => m_isStunned || m_isKnockedBack || m_isWaiting;

        public Camera mainCamera => CommonUtils.GetRequiredComponent(ref m_mainCamera, () =>
        {
            return CameraUtils.GetMainCamera();
        });
        
        protected bool m_isPastThreshold => m_ballMeleeChargeAmount >= m_meleeChargeThreshold;
        
        public float meleeChargeSpeed => m_currentSpeed / 1.5f;

        protected float m_currentApplySpeed => m_isCharging && m_isPastThreshold ? meleeChargeSpeed : m_currentSpeed;

        public Vector3 m_playerAimVector { get; private set; }

        public Vector3 playerRStickInput => m_inputAimDir;

        public Transform spawnerLocation => m_sphereCheckLocation;

        public Color playerColor { get; private set; }

        public Color playerMidColor { get; private set; }

        public Color playerDarkColor { get; private set; }

        public bool isShielding { get; private set; }

        public bool isParrying { get; private set; }

        public bool isEvading { get; private set; }

        public bool isDashing { get; private set; }

        public CustomTimer wackTimer { get; protected set; }

        public bool canUseWack => m_canCheckBallInput;

        #endregion

        #region Unity Events

        private void OnEnable()
        {
            MatchGameController.OnRoundStart += SetPlayerRoundStart;
        }

        private void OnDisable()
        {
            MatchGameController.OnRoundStart -= SetPlayerRoundStart;
        }

        

        private void OnDrawGizmos()
        {
            if (m_sphereCheckLocation.IsNull())
            {
                return;
            }
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(m_sphereCheckLocation.position, characterData.IsNull() ? 1 : m_wackRangeCurrent);
        }

        private void Update()
        {
            if (!m_isAlive || !m_isInitialized)
            {
                return;
            }
            
            ReadPlayerInputs();
            CheckLocalTimers();

            CheckAbilityCooldowns();
            CheckStatusCooldown();

            CheckCharacterRotation();
            UpdateAimIndicator();

            if (!m_isKnockedBack && !m_isStunned && !m_isShieldPopStunned && !isShielding)
            {
                MoveCharacter();
            }
            else if(m_isKnockedBack)
            {
                DoKnockback();
            }
        }

        #endregion
        
        #region Class Implementation

        public void AddAbilityCharge()
        {
            m_abilityUseCharges++;
        }

        public void ResetCharacter()
        {
            m_canReadPlayerInput = false;
            m_characterMoveVector = Vector3.zero;
            CancelWackCharge();
            OnRevive();
        }

        private void SetPlayerRoundStart()
        {
            m_canReadPlayerInput = true;
        }

        public virtual async UniTask InitializeCharacter(CharacterData _characterData, AbilityData chosenAbility, int _index, Player _player,
            LayerMask _groundMask, LayerMask _wallMask, CancellationToken token)
        {
            if (_characterData.IsNull())
            {
                return;
            }

            Debug.Log("Initialize Player");
            m_canReadPlayerInput = false;
            m_playerIndex = _index;

            m_groundMask = _groundMask;
            m_wallLayerMask = _wallMask;
            
            m_player = _player;

            characterData = _characterData;
            m_selectedAbility = chosenAbility;

            m_currentDamagedAmount = 0;
            m_damagePercentage = 0;
            m_damagePercentageKnockbackMod = 0.01f;

            m_originalSpeed = characterData.characterWalkSpeed;
            m_currentSpeed = m_originalSpeed;
            
            m_wackRangeCurrent = characterData.ballMeleeColliderRadius;
            m_meleeIndicator.transform.localScale = Vector3.one * (m_wackRangeCurrent * 2);
            m_wackVFX.transform.localScale = m_meleeIndicator.transform.localScale;

            playerColor = SettingsController.Instance.GetColorByPlayerIndex(m_playerIndex);
            playerMidColor = SettingsController.Instance.GetMidColorByPlayerIndex(m_playerIndex);
            playerDarkColor = SettingsController.Instance.GetDarkColorByPlayerIndex(m_playerIndex);
            
            m_wackVFX.ChangeAllStartColor(playerColor);
            m_wackChargeVFX.ChangeAllStartColor(playerColor);
            
            m_marker.materials[0].SetColor(markerColorName, playerColor);
            m_aimMarker.materials[0].SetColor(aimColorName, playerColor);

            m_healthbarLoc.position = transform.position + characterData.healthBarOffset;
            
            energyShieldBehaviour.Initialize(this);
            //await InitializeAssignedAbilities(token);

            wackTimer = new CustomTimer(ballCooldownTimerIdentifier, characterData.ballMeleeCooldownTimer, false , ResetBallInput);
            
            m_isInitialized = true;
            canUseAbilities = true;
        }

        private async UniTask InitializeChosenAbility(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var chosenAbilityPrefab = Instantiate(m_selectedAbility.abilityGameObject, m_aimIndicator);

            if (chosenAbilityPrefab.TryGetComponent(out AbilityBase _chosenAbility).IsNull())
            {
                Debug.LogError($"Ability Initialization Error:[Name:{m_selectedAbility.abilityName}] does not contain component of type AbilityBase");
                return;
            }
                
            m_assignedAbilities.Add(_chosenAbility);
            await _chosenAbility.InitializeAbilityAsync(this, m_selectedAbility, false, token);
        }

        /// <summary>
        /// Initialize chosen ability to slot 0.
        /// Initialize pre-assigned Character abilities to other slots.
        /// </summary>
        /// <param name="token"></param>
        public async UniTask InitializeAssignedAbilities(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (characterData.IsNull())
            {
                return;
            }

            await InitializeChosenAbility(token);
            
            //ToDo: PRELOAD all necessary abilities spawnables AND vfx
            foreach (var _abilityData in characterData.allCharacterAbilities)
            {
                var _currentAbilityPrefab = Instantiate(_abilityData.abilityGameObject,
                    m_aimIndicator);
                
                if (_currentAbilityPrefab.TryGetComponent(out AbilityBase _ability).IsNull())
                {
                    continue;
                }
                
                m_assignedAbilities.Add(_ability);
                await _ability.InitializeAbilityAsync(this, _abilityData, false, token);
            }

            await AssignDashAbilityAsync(token);
            
            OnAbilitiesAssigned?.Invoke();
        }

        /// <summary>
        /// Initialize Dash ability
        /// </summary>
        private async UniTask AssignDashAbilityAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var _dashAbilityPrefab = Instantiate(m_dashAbilityData.abilityGameObject,
                transform);
            
            if (_dashAbilityPrefab.TryGetComponent(out AbilityBase _dashAbilityComp).IsNull())
            {
                return;
            }

            m_dashAbility = _dashAbilityComp;
            await m_dashAbility.InitializeAbilityAsync(this, m_dashAbilityData, false, token);
        }
        
        /// <summary>
        /// Initialize Ultimate Ability
        /// </summary>
        public async UniTask T_AssignLargeAbility(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (characterData.IsNull() || characterData.allCharacterAbilities.Count == 0)
            {
                return;
            }
            
            var _currentAbilityPrefab = Instantiate(characterData.largeAbility.abilityGameObject);

            _currentAbilityPrefab.TryGetComponent(out AbilityBase _ability);

            if (_ability.IsNull())
            {
                return;
            }
            
            m_assignedAbilities.Add(_ability);
            await _ability.InitializeAbilityAsync(this, characterData.largeAbility, false, token);
            
            OnAbilitiesAssigned?.Invoke();
        }

        public async UniTask AssignSpecificNewAbility(AbilityData _newAbility, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_newAbility.IsNull() || m_assignedAbilities.Count >= 3)
            {
                //Don't add more that max 3 abilties [NOT including shield, wack, and bunt]
                return;
            }
            
            var _currentAbilityPrefab = Instantiate(_newAbility.abilityGameObject,
                m_aimIndicator);

            _currentAbilityPrefab.TryGetComponent(out AbilityBase _ability);

            if (_ability.IsNull())
            {
                return;
            }
                
            m_assignedAbilities.Add(_ability);
            await _ability.InitializeAbilityAsync(this, _newAbility, true, token);
            //TBD
            OnSingleAbilityAssigned?.Invoke();
        }

        public int AbilitiesAmount() => m_assignedAbilities.Count;

        public AbilityBase GetAbility(int _index) => m_assignedAbilities[_index];

        public List<AbilityBase> GetAllAbilities()
        {
            return m_assignedAbilities.ToList();
        }

        public float GetGroundY() => m_sphereCheckLocation.transform.position.y;
        
        public int GetPlayerIndex()
        {
            return m_playerIndex;
        }

        public Player GetPlayerController()
        {
            return m_player;
        }

        protected void CheckCharacterRotation()
        {
            if (m_characterModelHolder.IsNull() || m_characterMoveVector == Vector3.zero || !m_canReadPlayerInput)
            {
                return;
            }

            m_characterModelHolder.forward = m_characterMoveVector.FlattenVector3Y();
        }

        public Transform GetCharacterModelParent()
        {
            return m_characterModelHolder;
        }

        public Transform GetIndicatorParent()
        {
            return m_indicatorHolder;
        }

        public Transform GetHealthBarTransform()
        {
            return m_healthbarLoc;
        }

        public void CreateLocalTimer(string _identifier, float _maxTime, Action _endAction = null)
        {
            m_currentTimers.Add(new CustomTimer(_identifier, _maxTime, false , _endAction));
        }

        protected void CheckLocalTimers()
        {
            if (m_currentTimers.Count == 0)
            {
                return;
            }

            if (m_stopCooldownTimer)
            {
                return;
            }
            
            if (m_currentTimers.Any(_timer => _timer.isFinished))
            {
                for (int i = m_currentTimers.Count - 1; i > -1; i--)
                {
                    if (!m_currentTimers[i].isFinished)
                    {
                        continue;
                    }
                    
                    m_currentTimers.Remove(m_currentTimers[i]);
                }
            }

            foreach (var _timer in m_currentTimers.Where(_timer => !_timer.pauseCondition))
            {
                _timer.currentTime -= Time.deltaTime;
            }
            
            foreach (CustomTimer _timer in m_currentTimers.Where(_timer => _timer.currentTime <= 0))
            {
                _timer.OnFinishAction?.Invoke();
                _timer.isFinished = true;
            }
        }

        protected bool ContainsTimer(string _identifier)
        {
            return m_currentTimers.Count != 0 && m_currentTimers.TrueForAll(ct => ct.timerIdentifier != _identifier);
        }

        protected void DecreaseTimerMaxTime(string _identifier, float m_decreaseAmount)
        {
            if (m_currentTimers.Count == 0)
            {
                //No Active Timers
                return;
            }

            var _foundTimer = m_currentTimers.FirstOrDefault(ct => ct.timerIdentifier == _identifier);

            if (_foundTimer.IsNull())
            {
                //Didn't find Timer
                return;
            }

            _foundTimer.maxTime -= m_decreaseAmount;
        }

        protected void EarlyEndTimer(string _identifier)
        {
            if (m_currentTimers.Count == 0)
            {
                //No Active Timers
                return;
            }

            if (!ContainsTimer(_identifier))
            {
                return;
            }

            var _foundTimer = m_currentTimers.FirstOrDefault(ct => ct.timerIdentifier == _identifier);

            if (_foundTimer.IsNull())
            {
                //Didn't find Timer
                return;
            }

            _foundTimer.currentTime = 0;
        }

        protected void ReadPlayerInputs()
        {
            if (!m_canReadPlayerInput || m_isShieldPopStunned)
            {
                return;
            }

            m_inputMoveDir = new Vector3(m_player.GetAxisRaw("Move_Horizontal"), 0, 
                m_player.GetAxisRaw("Move_Vertical"));
            
            m_inputAimDir = new Vector3(m_player.GetAxis("Aim_Horizontal"), 0, 
                m_player.GetAxis("Aim_Vertical"));
            
            m_characterMoveVector = Vector3.Normalize(m_inputMoveDir).FlattenVector3Y();
            
            m_characterMoveVector *= m_currentApplySpeed * Time.deltaTime;
            
            m_playerAimVector = m_player.controllers.hasMouse ? GetMousePosition() - transform.position : 
                m_inputAimDir != Vector3.zero ? m_inputAimDir : m_inputMoveDir != Vector3.zero ? m_inputMoveDir
                : m_characterModelHolder.forward;

            if (m_player.GetButton(ballMeleeActionName) && !m_player.GetButton(energyShieldActionName))
            {
                ChargeBallMelee();
            }else if(m_player.GetButtonUp(ballMeleeActionName) && !m_player.GetButton(energyShieldActionName))
            {
                PerformBallMelee();
            }

            if (m_player.GetButtonDown(energyShieldActionName))
            {
                OnStartEnergyShield();
            }else if(m_player.GetButton(energyShieldActionName))
            {
                OnUseEnergyShield();
            }else if (m_player.GetButtonUp(energyShieldActionName))
            {
                OnEndEnergyShield();
            }

            if (m_player.GetButtonDown(dashAbilityActionName))
            {
                UseDashAbility();
            }


            if (m_player.GetButtonDown(primaryAbilityActionName) && !m_player.GetButtonDown(secondaryAbilityActionName))
            {
                OnAbilityButtonDown(0);
            } else if (!m_player.GetButtonDown(primaryAbilityActionName) && m_player.GetButtonDown(secondaryAbilityActionName))
            {
                OnAbilityButtonDown(1);
            }
            
            if (m_player.GetButton(primaryAbilityActionName) && !m_player.GetButton(secondaryAbilityActionName))
            {
                OnAbilityButtonHeld(0);
            } else if (!m_player.GetButton(primaryAbilityActionName) && m_player.GetButton(secondaryAbilityActionName))
            {
                OnAbilityButtonHeld(1);
            }else if (m_player.GetButton(primaryAbilityActionName) && m_player.GetButton(secondaryAbilityActionName))
            {
                OnAbilityButtonHeld(2);
            }
                
            if (m_player.GetButtonUp(primaryAbilityActionName) && m_currentHeldAbility == 0)
            {
                OnAbilityButtonReleased(0);
            } else if (m_player.GetButtonUp(secondaryAbilityActionName) && m_currentHeldAbility == 1)
            {
                OnAbilityButtonReleased(1);
            }else if (!m_player.GetButton(primaryAbilityActionName) 
                      && !m_player.GetButton(secondaryAbilityActionName) && m_currentHeldAbility == 2)
            {
                OnAbilityButtonReleased(2);
            }
            
        }

        protected Vector3 GetMousePosition()
        {
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            return Physics.Raycast(ray, out RaycastHit _hitInfo, Mathf.Infinity, m_groundMask) ? _hitInfo.point : m_characterMoveVector;
        }

        protected void UpdateAimIndicator()
        {
            if (m_aimIndicator.IsNull() || m_playerAimVector == Vector3.zero || isShielding)
            {
                return;
            }
            
            m_aimIndicator.forward = m_playerAimVector.FlattenVector3Y();
        }

        #region Update Character Values

        public void Pause_UnPause_Character(bool _isPaused)
        {
            if (_isPaused)
            {
                HaltCharacterMovement();
                CancelWackCharge();
            }
            else
            {
                ResetCharacterMovementSpeed();
            }

            m_canReadPlayerInput = !_isPaused;
        }

        public void ChangeCurrentCharacterMovementSpeed(float _modifier)
        {
            m_currentSpeed *= _modifier;
        }

        public void HaltCharacterMovement()
        {
            m_originalSpeed = m_currentSpeed;
            m_currentSpeed = 0;
        }

        public void ResetCharacterMovementSpeed()
        {
            m_currentSpeed = m_originalSpeed;
            Debug.Log($"Reset movement: {m_currentSpeed}");
        }

        public void ChangeDamageIntakeMod(float _newModValue)
        {
            m_damageIntakeMod = _newModValue;
        }

        public void ResetDamageIntakeMod()
        {
            m_damageIntakeMod = 1f;
        }

        #endregion

        #region Player Upgrades

        //0: speed, 1: health, 2: size
        public void ChangeCharacterStatsValues(params object[] _arguments)
        {
            m_speedModifier += (float)_arguments[0];
            //ToDo:Change what m_armorModifier is for
            m_armorModifier += (float)_arguments[1];
            m_wackSizeModifier += (float)_arguments[2];
            
            SetSpeed();
            //SetMaxArmor();
            SetSize();
        }

        private void SetSpeed()
        {
            m_originalSpeed = characterData.characterWalkSpeed * m_speedModifier;
            m_currentSpeed = m_originalSpeed;
        }

        private void SetMaxArmor()
        {
            //m_current = characterData.characterArmorAmount * m_armorModifier;
            //OnMaxArmorChanged?.Invoke(this, m_currentMaxArmor);
        }

        private void SetSize()
        {
            m_wackRangeCurrent = characterData.ballMeleeColliderRadius * m_wackSizeModifier;
            m_meleeIndicator.transform.localScale = Vector3.one * (m_wackRangeCurrent * 2);
            m_wackVFX.transform.localScale = m_meleeIndicator.transform.localScale;
        }

        #endregion

        #region Dash Ability

        private void UseDashAbility()
        {
            if (m_dashAbility.IsNull() || m_abilitiesOnCooldown.Contains(m_dashAbility))
            {
                return;
            }
            
            OnAbilityButtonDown(m_dashAbility);
        }

        #endregion

        #region Character Movement -----------------

        protected void MoveCharacter()
        {
            if (characterController.IsNull() || !characterController.enabled)
            {
                return;
            }
            
            characterController.Move(Vector3.ClampMagnitude(m_characterMoveVector, m_currentSpeed));
        }
        
        public void TeleportPlayer(Vector3 _newPosition)
        {
            EnableCharacterController(false);
            transform.position = _newPosition;
            EnableCharacterController(true);
        }

        public void EnableCharacterController(bool _enabled)
        {
            characterController.enabled = _enabled;
        }

        #endregion

        #region Ball Melee --------------


        protected void DisplayMeleeIndicator(bool _isActive)
        {
            if (m_meleeIndicator.IsNull())
            {
                return;
            }
            
            m_meleeIndicator.SetActive(_isActive);
        }

        protected void CancelWackCharge()
        {
            m_isCharging = false;
            DisplayMeleeIndicator(false);
            m_ballMeleeChargeAmount = 0;
        }

        protected void ChargeBallMelee()
        {
            if (!m_canCheckBallInput || m_isKnockedBack || m_isStunned || isShielding || isEvading)
            {
                return;
            }

            m_isCharging = true;

            if (!m_meleeIndicator.activeSelf)
            {
                DisplayMeleeIndicator(true);
            }

            if (!m_hasPlayedVFX && m_isPastThreshold)
            {
                m_wackChargeVFX.Play();
                m_hasPlayedVFX = true;
            }

            if (m_ballMeleeChargeAmount >= m_ballMeleeChargeAmountMax)
            {
                return;
            }

            m_ballMeleeChargeAmount += Time.deltaTime;
        }
        
        protected void PerformBallMelee()
        {
            if (!m_canCheckBallInput || m_isKnockedBack || m_isStunned || isShielding || isEvading || m_playerHittingBall)
            {
                return;
            }

            PlayRandomWackSound();
            
            m_isCharging = false;
            m_playerHittingBall = true;
            m_canCheckBallInput = false;
            m_hasPlayedVFX = false;

            m_isInAction = true;

            m_wackVFX.transform.forward = m_playerAimVector.FlattenVector3Y();
            m_wackVFX.Play();

            wackTimer.currentTime = wackTimer.maxTime;
            wackTimer.isFinished = false;
            
            OnWackUsed?.Invoke(this);
            m_currentTimers.Add(new CustomTimer(ballHitTimerIdentifier, m_wackTimeMax, false, StopCheckingBall));
            m_currentTimers.Add(wackTimer);
            
            CheckHitBallAsync().Forget();
        }

        protected void PlayRandomWackSound()
        {
            if (m_wackSounds.Count == 0)
            {
                return;
            }

            aSource.pitch = Random.Range(0.9f, 1.1f);
            aSource.PlayOneShot(m_wackSounds[Random.Range(0,m_wackSounds.Count)]);
        }

        private async UniTask CheckHitBallAsync()
        {
            if (m_isKnockedBack || m_isStunned)
            {
                return;
            }

            Debug.Log($"Check WACK");

            m_hitAmount = 0;
            
            await wackBallSemaphoreSlim.WaitAsync();
            
            try
            {

                while (m_playerHittingBall)
                {
                    m_hitAmount = Physics.OverlapSphereNonAlloc(m_sphereCheckLocation.position, m_wackRangeCurrent,
                        m_hitColliders, characterData.ballMeleeLayerMask);

                    Debug.Log($"Check In While loop");
                    
                    if (m_hitAmount > 0)
                    {
                        break;
                    }
                    
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
                
                Debug.Log($"Check Hit Ball Amount:{m_hitAmount}");

                bool hasHitBall = false;

                for (int i = 0; i < m_hitAmount; i++)
                {
                    Debug.Log("--- FOR LOOP START");
                    if (hasHitBall)
                    {
                        continue;
                    }

                    m_hitColliders[i].TryGetComponent(out BallBehavior _ball);

                    if (_ball.IsNull())
                    {
                        continue;
                    }

                    if (m_ballMeleeChargeAmount / m_ballMeleeChargeAmountMax >= m_meleeChargeThreshold)
                    {
                        OnHitBallConnect(_ball).Forget();
                    }
                    else
                    {
                        _ball.HitBall(m_playerAimVector, HitStrengthType.LIGHT, this);
                    }

                    EarlyEndTimer(ballHitTimerIdentifier);
                    hasHitBall = true;
                    Debug.Log("FOR LOOP END ---");
                }
            }
            finally
            {
                wackBallSemaphoreSlim.Release();
            }
        }

        protected async UniTask OnHitBallConnect(BallBehavior _ball)
        {
            HaltCharacterMovement();
            EarlyEndTimer(ballHitTimerIdentifier);
            
            JuiceGameController.Instance.DoCameraShake(0.1f, 0.1f, 10, 10);

            if (_ball.isFastBall)
            {
                JuiceGameController.Instance.CreateScreenRipple(new Vector2(transform.position.x, transform.position.z));
            }
            
            _ball.StopBall();

            m_ballConnectBuildUpTimerCurrent = 0f;

            m_calcMaxBuildUpTimer = m_ballConnectBuildUpTimerMax * _ball.speedPercentage;
            
            _ball.SetBuildUp(true, this);
            
            _ball.ForceChangeColorTo(playerColor);

            _ball.ChargedWackResizeBall();

            while (m_ballConnectBuildUpTimerCurrent < m_calcMaxBuildUpTimer)
            {
                if (m_ballConnectBuildUpTimerCurrent >= m_calcMaxBuildUpTimer)
                {
                    break;
                }
                
                m_ballConnectBuildUpTimerCurrent += Time.deltaTime;

                m_ballBuildUpPercentage = m_ballConnectBuildUpTimerCurrent / m_calcMaxBuildUpTimer;
                
                _ball.UpdateBallCharge(m_ballBuildUpPercentage, m_playerAimVector);
                
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            
            //ToDo: Ball Hit VFX when going fast?
            
            JuiceGameController.Instance.DoCameraShake(0.1f, 0.1f, 10, 10);
            
            _ball.SetBuildUp(false, this);
            
            _ball.HitBall(m_playerAimVector, HitStrengthType.MEDIUM,this);

            ResetCharacterMovementSpeed();
        }

        protected void StopCheckingBall()
        {
            //End the ball check
            m_playerHittingBall = false;
            DisplayMeleeIndicator(false);
            m_ballMeleeChargeAmount = 0;
            m_isInAction = false;
        }

        protected void ResetBallInput()
        {
            m_canCheckBallInput = true;
        }

        public void SilenceWack(bool canUseWack)
        {
            if (ContainsTimer(wackTimer.timerIdentifier))
            {
                EarlyEndTimer(wackTimer.timerIdentifier);
            }
            
            m_canCheckBallInput = canUseWack;
        }

        #endregion

        #region Knockback Related ----------

        protected void PlayDamageSFX()
        {
            if (m_damagedSounds.Count == 0)
            {
                return;
            }

            aSource.pitch = Random.Range(0.9f, 1.1f);
            aSource.PlayOneShot(m_damagedSounds[Random.Range(0,m_damagedSounds.Count)]);
        }

        protected void StartKnockBack()
        {
            if (m_isCharging)
            {
                m_isCharging = false;
            }
            
            m_isKnockedBack = true;
        }

        protected void DoKnockback()
        {
            if (HasHitArenaBorder())
            {
                EndKnockBack();
                
                //ToDo: Juice => slow stuff on screen for less than a second
                JuiceGameController.Instance.DoCameraShake(0.5f, 0.2f, 10, 0f);
                
                OnKillPlayer(transform.position, -m_knockbackMoveVector.normalized);
                return;
            }
            
            if (m_knockbackForce <= 0.1f && m_isKnockedBack)
            {
                EndKnockBack();
                return;
            }

            m_characterMoveVector = Vector3.Normalize(m_knockbackMoveVector).FlattenVector3Y();
            m_characterMoveVector *= m_knockbackForce * Time.deltaTime;
            characterController.Move(Vector3.ClampMagnitude(m_characterMoveVector, m_knockbackForce));

            m_knockbackForce *= Mathf.Exp((m_damagePercentageKnockbackMod > 1f ? -m_unarmoredKnockbackReductionRate : -m_armoredKnockbackReductionRate) * Time.deltaTime);
        }

        protected bool HasHitArenaBorder()
        {
            return Physics.Raycast(transform.position, m_knockbackMoveVector, 1f, m_wallLayerMask);
        }

        protected void EndKnockBack()
        {
            if (!m_knockbackVFX.IsNull())
            {
                m_knockbackVFX.Stop();
            }
            m_canReadPlayerInput = true;
            m_isKnockedBack = false;
            m_knockbackForce = 0;
            m_knockbackTime = 0;
        }

        #endregion

        #region Ability Related ---------------

        protected void CheckAbilityCooldowns()
        {
            if (m_abilitiesOnCooldown.Count == 0)
            {
                return;
            }

            foreach (var _ability in m_abilitiesOnCooldown)
            {
                _ability.abilityCooldownCurrent -= Time.deltaTime * _ability.cooldownReductionModifier;

                if (_ability.abilityCooldownCurrent > 0)
                {
                    continue;
                }
                
                _ability.ResetAbilityUse();
                m_cooldownRemovableAbilities.Add(_ability);
            }

            if (m_cooldownRemovableAbilities.Count <= 0)
            {
                return;
            }

            foreach (var _removableAbility in m_cooldownRemovableAbilities)
            {
                m_abilitiesOnCooldown.Remove(_removableAbility);
            }
            
            //If all the abilities are able to be removed, might as well clear
            m_cooldownRemovableAbilities.Clear();

        }

        //Might Not be Needed
        protected abstract void PokeAbility();

        public void StartAllAbilityCooldowns()
        {
            foreach (var ability in m_assignedAbilities)
            {
                ability.canUseAbility = false;
                AddAbilityCooldown(ability);
            }
        }
        
        protected void OnAbilityButtonDown(int _abilityID)
        {
            if (m_assignedAbilities.Count == 0)
            {
                return;
            }

            if (_abilityID >= m_assignedAbilities.Count || m_assignedAbilities[_abilityID].IsNull())
            {
                return;
            }

            if (!m_assignedAbilities[_abilityID].canUseAbility)
            {
                return;
            }

            if (isShielding || isEvading || !canUseAbilities)
            {
                return;
            }
            
            m_currentHeldAbility = _abilityID;
            OnAbilityButtonDown(m_assignedAbilities[_abilityID]);
        }
        
        protected void OnAbilityButtonDown(AbilityBase abilityBase)
        {
            if (m_assignedAbilities.Count == 0 || abilityBase.IsNull())
            {
                return;
            }

            if (isShielding || isEvading)
            {
                return;
            }
            
            CancelWackCharge();

            if (cts.IsNull())
            {
                cts = new CancellationTokenSource();
            }
            
            abilityBase.OnAbilityButtonPressed();

            if (abilityBase.abilityData == null || abilityBase.abilityData.activationType != ActivationType.OnPress)
            {
                return;
            }
            
            OnAbilityUsed?.Invoke(this, abilityBase);
            AddAbilityCooldown(abilityBase);
        }

        protected void OnAbilityButtonHeld(int _abilityID)
        {
            if (m_assignedAbilities.Count == 0)
            {
                return;
            }

            if (_abilityID >= m_assignedAbilities.Count || m_assignedAbilities[_abilityID].IsNull())
            {
                return;
            }

            if (!m_assignedAbilities[_abilityID].canUseAbility)
            {
                return;
            }

            if (isShielding || isEvading || !canUseAbilities)
            {
                return;
            }
            
            m_currentHeldAbility = _abilityID;
            OnAbilityButtonHeld(m_assignedAbilities[_abilityID]);
        }
        
        protected void OnAbilityButtonHeld(AbilityBase abilityBase)
        {
            if (m_assignedAbilities.Count == 0 || abilityBase.IsNull())
            {
                return;
            }

            if (isShielding || isEvading)
            {
                return;
            }
            
            CancelWackCharge();

            if (cts.IsNull())
            {
                cts = new CancellationTokenSource();
            }
            
            abilityBase.OnAbilityButtonHeld();
        }

        protected void OnAbilityButtonReleased(int _abilityID)
        {
            if (m_assignedAbilities.Count == 0)
            {
                return;
            }

            if (_abilityID >= m_assignedAbilities.Count || m_assignedAbilities[_abilityID].IsNull())
            {
                return;
            }

            if (!m_assignedAbilities[_abilityID].canUseAbility)
            {
                return;
            }

            if (isShielding || isEvading || !canUseAbilities)
            {
                return;
            }
            
            m_currentHeldAbility = _abilityID;
            OnAbilityButtonReleased(m_assignedAbilities[_abilityID]);
        }
        
        protected void OnAbilityButtonReleased(AbilityBase abilityBase)
        {
            if (m_assignedAbilities.Count == 0 || abilityBase.IsNull())
            {
                return;
            }

            if (isShielding || isEvading)
            {
                return;
            }
            
            CancelWackCharge();

            if (cts.IsNull())
            {
                cts = new CancellationTokenSource();
            }
            
            abilityBase.OnAbilityButtonReleased();
            
            if (abilityBase.abilityData == null || abilityBase.abilityData.activationType != ActivationType.OnRelease)
            {
                return;
            }
            
            OnAbilityUsed?.Invoke(this, abilityBase);
            AddAbilityCooldown(abilityBase);
        }

        public void SetSilenceStatus(bool _canUseAbilities)
        {
            canUseAbilities = _canUseAbilities;
        }

        public void AddAbilityCooldown(AbilityBase _ability)
        {
            var _matchingAbility =
                m_assignedAbilities.FirstOrDefault(ab =>
                    ab.abilityData.abilityName == _ability.abilityData.abilityName);

            if (_matchingAbility.IsNull())
            {
                Debug.LogError($"[Ability Error] AddAbilityCooldown: {_ability.abilityData.abilityName} Not Added");
                return;
            }
            
            OnAbilityUsed?.Invoke(this, _matchingAbility);
            m_abilitiesOnCooldown.Add(_matchingAbility);
        }

        #endregion

        #region Perk Related -------------

        public async UniTask AddPerk(PerkDataBase _perkData, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_perkData.IsNull())
            {
                return;
            }
            
            var _currentPerkPrefab = await ObjectPoolController.Instance.T_CreateParentedObject(_perkData.name,
                _perkData.perkGameObject, m_statusHolder, token);
            
            _currentPerkPrefab.TryGetComponent(out PerkEntityBase _perkEntity);

            if (_perkEntity.IsNull())
            {
                ObjectPoolController.Instance.ReturnToPool(_perkData.name, _currentPerkPrefab);
                return;
            }
                
            m_currentPerks.Add(_perkEntity);
            _perkEntity.OnApply(this, _perkData);
            
            //ToDo: Action?
        }

        #endregion

        #region Energy Shield

        private void OnStartEnergyShield()
        {
            if (m_isInAction || m_isShieldPopStunned || energyShieldBehaviour.CurrentEnergy <= 0 || m_isKnockedBack || m_isStunned)
            {
                return;
            }

            isShielding = true;
            isParrying = true;
            energyShieldBehaviour.EnableShield(true);
            energyShieldBehaviour.SetParrying(true);
            CreateLocalTimer(parryTimerIdentifier, SettingsController.Instance.GetParryTiming(), OnParryEnd);
        }

        private void OnUseEnergyShield()
        {
            if (m_isInAction || m_isShieldPopStunned || m_isKnockedBack || m_isStunned)
            {
                return;
            }
            
            energyShieldBehaviour.UseEnergyShield();
        }

        private void OnEndEnergyShield()
        {
            isShielding = false;
            energyShieldBehaviour.EnableShield(false);
            energyShieldBehaviour.EndEnergyShield();
            EarlyEndTimer(parryTimerIdentifier);
        }

        private void OnParryEnd()
        {
            isParrying = false;
            energyShieldBehaviour.SetParrying(false);
        }

        public void OnPopEnergyShield()
        {
            m_isShieldPopStunned = true;

            m_shieldPopVFX.Play();
            m_shieldPopStunVFX.Play();
            
            OnEndEnergyShield();
            m_currentTimers.Add(new CustomTimer(shieldPopTimerIdentifier, m_energyShieldPopStunDuration, false, EndShieldPopStun));
        }

        protected void EndShieldPopStun()
        {
            if (m_shieldPopStunVFX.is_playing)
            {
                m_shieldPopStunVFX.Stop();
            }
            
            m_isShieldPopStunned = false;
            energyShieldBehaviour.HalfRegenShieldEnergy();
        }
        
        #endregion
        
        #region Status Related --------------

        public async UniTask ApplyStatus(StatusData _statusData, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_statusData.IsNull())
            {
                return;
            }
            
            var _currentStatusPrefab = await ObjectPoolController.Instance.T_CreateParentedObject(_statusData.name,
                _statusData.statusGameObject, m_statusHolder, token);

            VFXController.Instance.PlayBuffDebuff(_statusData.isBuff, transform.position, Quaternion.identity);
            
            _currentStatusPrefab.TryGetComponent(out StatusEntityBase _status);

            if (_status.IsNull())
            {
                ObjectPoolController.Instance.ReturnToPool(_statusData.name, _currentStatusPrefab);
                return;
            }
                
            m_currentStatuses.Add(_status);
            m_currentStatusGUIDs.Add(_statusData.statusIdentifierGUID);
            _status.OnApply(this);
            OnStatusApplied?.Invoke(this, _statusData);
        }

        protected void RemoveAllStatuses()
        {
            if (m_currentStatuses.Count == 0)
            {
                return;
            }
            
            m_currentStatuses.ForEach(seb =>
            {
                seb.OnEnd();
                OnStatusRemoved?.Invoke(this, seb.GetStatusData());
                ObjectPoolController.Instance.ReturnToPool(seb.GetStatusData().name,
                    seb.gameObject);
            });
            
            m_currentStatusGUIDs.Clear();
            m_currentStatuses.Clear();
        }

        protected void CheckStatusCooldown()
        {
            if (m_currentStatuses.Count <= 0)
            {
                return;
            }

            foreach (var _status in m_currentStatuses)
            {
                if (!_status.isInitialized)
                {
                    continue;
                }
                
                _status.statusTimeCurrent -= Time.deltaTime;

                if (_status.statusTimeCurrent > 0)
                {
                    continue;
                }
                
                _status.OnEnd();
                m_removableStatuses.Add(_status);
            }

            if (m_removableStatuses.Count <= 0)
            {
                return;
            }

            foreach (var _removableAbility in m_removableStatuses)
            {
                m_currentStatusGUIDs.Remove(_removableAbility.GetGUID());
                m_currentStatuses.Remove(_removableAbility);
                OnStatusRemoved?.Invoke(this, _removableAbility.GetStatusData());
                ObjectPoolController.Instance.ReturnToPool(_removableAbility.GetStatusData().name,
                    _removableAbility.gameObject);
            }
            
            //If all the abilities are able to be removed, might as well clear
            m_removableStatuses.Clear();
        }

        public bool ContainsStatus(StatusData _statusData)
        {
            return m_currentStatusGUIDs.Count != 0 && m_currentStatusGUIDs.Contains(_statusData.statusIdentifierGUID);
        }

        #endregion

        #region Damage Related

        protected virtual void UpdateDamagePercentage()
        {
            //Round to nearest tenth => multiply by 10, round to int, divide by 10
            m_damagePercentage = Mathf.CeilToInt(m_currentDamagedAmount * 10f) / 10f;
            m_damagePercentageKnockbackMod = m_damagePercentage / 10f;
            OnDamagePercentageChanged?.Invoke(this, m_damagePercentage, null);
        }

        #endregion
        

        #endregion

        #region IDamagable Inherited Methods

        public virtual void OnRevive()
        {
            m_currentDamagedAmount = 0;
            m_damagePercentage = 0;
            m_damagePercentageKnockbackMod = 0.01f;
            m_isAlive = true;
            OnPlayerRevived?.Invoke(this);
            UpdateDamagePercentage();
        }

        public virtual void OnHeal(float _healAmount)
        {
            m_currentDamagedAmount = Mathf.Max(m_currentDamagedAmount - Mathf.RoundToInt(_healAmount), 0);
            UpdateDamagePercentage();
        }
        
        public virtual void OnDealDamage(Transform _attacker, float _damageAmount, BaseCharacter _attackingCharacter = null)
        {
            if (isEvading)
            {
                return;
            }
            
            if (_damageAmount <= 0)
            {
                return;
            }

            _damageAmount = Mathf.CeilToInt(_damageAmount);

            if (isShielding)
            {
                energyShieldBehaviour.RemoveEnergy(_damageAmount);
                return;
            }

            var _damageIntakeAmount = Mathf.CeilToInt(_damageAmount * m_damageIntakeMod);
            m_currentDamagedAmount += _damageIntakeAmount;
            
            JuiceGameController.Instance.CreateDamageText(_damageIntakeAmount, transform.position);
            
            UpdateDamagePercentage();
        }

        public virtual void OnKillPlayer(Vector3 _deathPosition, Vector3 _deathDirection)
        {
            OnPlayerPreDeath?.Invoke(this);

            RemoveAllStatuses();

            if (isShielding)
            {
                OnEndEnergyShield();
            }
            
            m_canReadPlayerInput = false;
            m_characterMoveVector = Vector3.zero;
            m_isKnockedBack = false;
            m_knockbackForce = 0;
            m_knockbackTime = 0;
            
            m_player.SetVibration(0, 0.6f, 0.25f);
            
            OnPlayerDeath?.Invoke(this, m_lastAttackingPlayer, transform.position);
        }
        
        #endregion

        #region IKnockbackable Inherited Methods

        protected virtual async UniTask T_OnKnockback()
        {
            Debug.Log($"Current Hit Stun Settings: Time= {m_currentHitStunTime} Frequency= {m_hitStunV3FrequencyCurrent}, Vibrato= {m_currentVirbrato}");
            
            await m_characterModelHolder.DOShakePosition( m_currentHitStunTime, m_hitStunV3FrequencyCurrent , m_currentVirbrato,
                0);
            
            m_characterModelHolder.localPosition = Vector3.zero;
            
            StartKnockBack();
        }

        public virtual void ApplyKnockback(Transform _attackerTransform, BaseCharacter _lastAttacker, 
            float _baseKnockbackAmount, Vector3 _forcedDirection, bool _isBallHit = false)
        {
            if (_baseKnockbackAmount <= 0 || (isShielding && _isBallHit) || isEvading)
            {
                return;
            }

            Debug.Log($"[Knockback debug] IsShielding:{isShielding} -> IsBallHit:{_isBallHit}");
            m_canReadPlayerInput = false;
            
            PlayDamageSFX();

            var knockbackMod = Mathf.Min(Mathf.Max(0.01f, m_damagePercentageKnockbackMod), m_maxDamagePercentageKnockbackMod);
            m_knockbackForce = _baseKnockbackAmount * (knockbackMod) *
                               (1 - characterData.characterNaturalKnockbackResistance);
            
            Debug.Log($"[Knockback debug] _baseKnockbackAmount:{_baseKnockbackAmount} ... m_damagePercentageKnockbackMod: {m_damagePercentageKnockbackMod} " +
                      $"... characterData.characterNaturalKnockbackResistance:{characterData.characterNaturalKnockbackResistance} ... KnockbackForce:{m_knockbackForce}");
            
            m_knockbackMoveVector = _forcedDirection == Vector3.zero ? transform.position - _attackerTransform.position : _forcedDirection.FlattenVector3Y();

            m_characterModelHolder.forward = m_knockbackMoveVector.normalized;
            
            m_lastAttackingPlayer = _lastAttacker;
            
            //ToDo: can use end point calculation to do a SmashBros last hit FX
            //var m_knockbackEndPos = transform.position + (m_knockbackMoveVector.normalized * _baseKnockbackAmount);
            
            float _percentage = Mathf.Clamp01(m_knockbackForce / m_hitStunKnockbackThreshold);

            m_currentHitStunTime = m_hitStunMaxTime * _percentage;
            m_currentHitStunFrequency = m_hitStunMaxFrequency * _percentage;
            m_currentVirbrato = Mathf.CeilToInt(m_hitStunMaxVirbrato * _percentage);
            m_hitStunV3FrequencyCurrent = m_knockbackMoveVector.normalized * m_currentHitStunFrequency;

            VFXController.Instance.PlayDamageVFX(transform.position, Quaternion.identity);
            
            if (!m_knockbackVFX.IsNull())
            {
                m_knockbackVFX.transform.forward = -m_knockbackMoveVector.normalized;
                m_knockbackVFX.ChangeAllStartColor(_lastAttacker.playerColor);
                m_knockbackVFX.Play();
            }
            
            //ToDo: Controller Vibration
            //m_player.SetVibration(0, 0.1f, 0.1f);
            T_OnKnockback().Forget();
        }

        #endregion
        
       
    }
}