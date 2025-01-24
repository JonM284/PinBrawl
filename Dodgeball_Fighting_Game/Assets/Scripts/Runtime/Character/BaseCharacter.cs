using System;
using System.Collections.Generic;
using System.Linq;
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

        //Input Actions
        private readonly string ballMeleeActionName = "Ball_Melee";
        private readonly string primaryAbilityActionName = "Primary_Ability";
        private readonly string secondaryAbilityActionName = "Secondary_Ability";
        private readonly string energyShieldActionName = "Energy_Shield";
        
        #endregion

        #region Actions

        public static event Action<BaseCharacter> OnPlayerPreDeath;
        
        public static event Action<BaseCharacter, BaseCharacter, Vector3> OnPlayerDeath;
        
        public static event Action<BaseCharacter> OnPlayerRevived;

        public static event Action<BaseCharacter, float, BaseCharacter> OnArmorAmountChanged;

        public static event Action<BaseCharacter, float> OnMaxArmorChanged;

        public static event Action OnAbilitiesAssigned;
        
        public static event Action<BaseCharacter> OnWackUsed;

        public static event Action OnSingleAbilityAssigned;

        public static event Action<BaseCharacter, AbilityBase> OnAbilityUsed; 
        
        public static event Action<BaseCharacter, StatusData> OnStatusApplied;
        
        public static event Action<BaseCharacter, StatusData> OnStatusRemoved;

        public static event Action<BaseCharacter, float> OnEnergyAmountChanged;

        public static event Action<BaseCharacter> OnEvadeUsed;

        #endregion

        #region Serialized Fields

        [SerializeField] private Transform m_aimIndicator;

        [SerializeField] private Transform m_sphereCheckLocation;

        [SerializeField] protected Transform m_characterModelHolder;
        
        [SerializeField] private Transform m_indicatorHolder;

        [SerializeField] private GameObject m_meleeIndicator;

        [SerializeField] private Transform m_energyShieldParent;
        [SerializeField] private GameObject m_energyShieldIndicator;

        [SerializeField] private VFXPlayer m_wackVFX;
        [SerializeField] private VFXPlayer m_wackChargeVFX;
        [SerializeField] private VFXPlayer m_shieldPopVFX;
        [SerializeField] private VFXPlayer m_shieldPopStunVFX;

        [SerializeField] private MeshRenderer m_marker;

        [SerializeField] private MeshRenderer m_aimMarker;
        
        [SerializeField] private Transform m_healthbarLoc;
        
        [SerializeField] private Transform m_statusHolder;

        [SerializeField] private AbilityData m_buntAbilityData;

        [SerializeField] private List<AudioClip> m_wackSounds = new List<AudioClip>();

        [SerializeField] protected List<AudioClip> m_damagedSounds = new List<AudioClip>();
        
        #endregion

        #region Private Fields
        
        //float
        protected float m_originalSpeed, m_currentSpeed;
        protected float m_currentArmor, m_currentMaxArmor;
        protected float m_currentEnergy, m_energyMax = 100, m_energyDepleteAmount = 9f, m_energyShieldPopStunDuration = 10f;
        protected float m_evadeDuration = 2f;
        protected float m_playerKillEnergyAddAmount, m_pvpDamageEnergyAddAmount; 
        protected float m_playerHorizontalInput, m_playerVerticalInput;
        protected float m_knockbackForce, m_knockbackTime;
        protected float m_ballMeleeChargeAmount, m_ballMeleeChargeAmountMax = 1f;
        protected float m_damageIntakeMod = 1f;
        protected float m_meleeChargeThreshold = 0.8f;
        protected float m_hitStunMaxTime = 0.3f, m_hitStunMaxFrequency = 0.8f, m_hitStunKnockbackThreshold = 150f;
        protected float m_currentHitStunTime, m_currentHitStunFrequency;
        protected float m_armoredKnockbackReductionRate = 15f, m_unarmoredKnockbackReductionRate = 8f;
        protected float m_speedModifier = 1f, m_armorModifier = 1f, m_wackSizeModifier = 1f;
        protected float m_wackRangeCurrent, m_wackTimeMax = 0.1f;
        
        //bool
        protected bool m_isAlive = true;
        protected bool m_isInitialized;
        protected bool m_canReadPlayerInput = true;
        protected bool m_isStunned, m_isKnockedBack, m_isWaiting, m_isShieldPopStunned;
        protected bool m_isInAction;
        protected bool m_playerHittingBall, m_canCheckBallInput = true, m_isCharging;
        protected bool m_hasPlayedVFX;
        
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

        private AbilityBase m_buntAbility;

        //status
        private List<StatusEntityBase> m_currentStatuses = new List<StatusEntityBase>();
        private List<string> m_currentStatusGUIDs = new List<string>();
        private List<StatusEntityBase> m_removableStatuses = new List<StatusEntityBase>();

        private List<PerkEntityBase> m_currentPerks = new List<PerkEntityBase>();

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

        protected Camera mainCamera => CommonUtils.GetRequiredComponent(ref m_mainCamera, () =>
        {
            return CameraUtils.GetMainCamera();
        });
        
        protected bool m_isPastThreshold => m_ballMeleeChargeAmount >= m_meleeChargeThreshold;
        
        public float meleeChargeSpeed => m_currentSpeed / 1.5f;

        protected float m_currentApplySpeed => m_isCharging && m_isPastThreshold ? meleeChargeSpeed : m_currentSpeed;

        protected bool m_isDepletedArmor => m_currentArmor <= 0;

        public float currentArmor => m_currentArmor;


        public Vector3 m_playerAimVector { get; private set; }

        public Vector3 playerRStickInput => m_inputAimDir;

        public Transform spawnerLocation => m_sphereCheckLocation;

        public Color playerColor { get; private set; }

        public Color playerMidColor { get; private set; }

        public Color playerDarkColor { get; private set; }

        public bool isShielding { get; private set; }

        public bool isEvading { get; private set; }

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
            
            if (m_playerHittingBall)
            {
                CheckHitBall();
            }
        }

        #endregion
        
        #region Class Implementation

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

        public virtual async UniTask InitializeCharacter(CharacterData _characterData, int _index, Player _player,
            LayerMask _groundMask, LayerMask _wallMask)
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

            m_currentMaxArmor = characterData.characterArmorAmount;
            m_currentArmor = characterData.characterArmorAmount;

            m_originalSpeed = characterData.characterWalkSpeed;
            m_currentSpeed = m_originalSpeed;
            
            m_currentEnergy = m_energyMax / 4f;
            
            OnEnergyAmountChanged?.Invoke(this, m_currentEnergy);

            m_wackRangeCurrent = characterData.ballMeleeColliderRadius;
            m_meleeIndicator.transform.localScale = Vector3.one * (m_wackRangeCurrent * 2);
            m_wackVFX.transform.localScale = m_meleeIndicator.transform.localScale;

            m_playerKillEnergyAddAmount = SettingsController.Instance.GetPlayerKillEnergyAmount();
            m_pvpDamageEnergyAddAmount = SettingsController.Instance.GetPvpDamageEnergyAmount();

            playerColor = SettingsController.Instance.GetColorByPlayerIndex(m_playerIndex);
            playerMidColor = SettingsController.Instance.GetMidColorByPlayerIndex(m_playerIndex);
            playerDarkColor = SettingsController.Instance.GetDarkColorByPlayerIndex(m_playerIndex);
            
            m_wackVFX.ChangeAllStartColor(playerColor);
            m_wackChargeVFX.ChangeAllStartColor(playerColor);
            
            m_marker.materials[0].SetColor(markerColorName, playerColor);
            m_aimMarker.materials[0].SetColor(aimColorName, playerColor);

            m_healthbarLoc.position = transform.position + characterData.healthBarOffset;
            
            //await InitializeAssignedAbilities();

            wackTimer = new CustomTimer(ballCooldownTimerIdentifier, characterData.ballMeleeCooldownTimer, false , ResetBallInput);

            m_isInitialized = true;
        }

        public async UniTask InitializeAssignedAbilities()
        {
            if (characterData.IsNull() || characterData.allCharacterAbilities.Count == 0)
            {
                return;
            }
            
            //ToDo: PRELOAD all necessary abilities spawnables AND vfx
            foreach (var _abilityData in characterData.allCharacterAbilities)
            {
                var _currentAbilityPrefab = Instantiate(_abilityData.abilityGameObject,
                    m_aimIndicator);

                _currentAbilityPrefab.TryGetComponent(out AbilityBase _ability);

                if (_ability.IsNull())
                {
                    continue;
                }
                
                m_assignedAbilities.Add(_ability);
                _ability.InitializeAbility(this, _abilityData);
            }
            
            
            var _buntAbilityPrefab = Instantiate(m_buntAbilityData.abilityGameObject,
                transform);

            _buntAbilityPrefab.TryGetComponent(out AbilityBase _buntAbilityScript);

            if (_buntAbilityScript.IsNull())
            {
                return;
            }

            m_buntAbility = _buntAbilityScript;
            m_buntAbility.InitializeAbility(this, m_buntAbilityData);
            
            OnAbilitiesAssigned?.Invoke();
        }

        public async UniTask AssignSpecificNewAbility(AbilityData _newAbility)
        {
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
            _ability.InitializeAbility(this, _newAbility);
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
            if (m_characterModelHolder.IsNull() || m_characterMoveVector == Vector3.zero)
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
            return !m_currentTimers.FirstOrDefault(ct => ct.timerIdentifier == _identifier).IsNull();
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
                UseEnergy();
            }
                
            if (m_player.GetButton(primaryAbilityActionName) && !m_player.GetButton(secondaryAbilityActionName))
            {
                ShowAbilityIndicator(0);
            } else if (!m_player.GetButton(primaryAbilityActionName) && m_player.GetButton(secondaryAbilityActionName))
            {
                ShowAbilityIndicator(1);
            }else if (m_player.GetButton(primaryAbilityActionName) && m_player.GetButton(secondaryAbilityActionName))
            {
                ShowAbilityIndicator(2);
            }
                
            if (m_player.GetButtonUp(primaryAbilityActionName) && m_currentHeldAbility == 0)
            {
                UseAbility(0);
            } else if (m_player.GetButtonUp(secondaryAbilityActionName) && m_currentHeldAbility == 1)
            {
                UseAbility(1);
            }else if (!m_player.GetButton(primaryAbilityActionName) 
                      && !m_player.GetButton(secondaryAbilityActionName) && m_currentHeldAbility == 2)
            {
                UseAbility(2);
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
            m_armorModifier += (float)_arguments[1];
            m_wackSizeModifier += (float)_arguments[2];
            
            SetSpeed();
            SetMaxArmor();
            SetSize();
        }

        private void SetSpeed()
        {
            m_originalSpeed = characterData.characterWalkSpeed * m_speedModifier;
            m_currentSpeed = m_originalSpeed;
        }

        private void SetMaxArmor()
        {
            m_currentMaxArmor = characterData.characterArmorAmount * m_armorModifier;
            OnMaxArmorChanged?.Invoke(this, m_currentMaxArmor);
        }

        private void SetSize()
        {
            m_wackRangeCurrent = characterData.ballMeleeColliderRadius * m_wackSizeModifier;
            m_meleeIndicator.transform.localScale = Vector3.one * (m_wackRangeCurrent * 2);
            m_wackVFX.transform.localScale = m_meleeIndicator.transform.localScale;
        }

        #endregion
        
        #region Energy Shield --------------

        private void UseEnergy()
        {
            if (m_isInAction || m_isShieldPopStunned || m_currentEnergy <= 0 || m_isKnockedBack || m_isStunned)
            {
                return;
            }

            if (isEvading)
            {
                return;
            }

            if (m_currentEnergy < m_energyMax)
            {
                UseEvade();
            }
            else
            {
                UseBunt();
            }
        }

        private void UseEvade()
        {
            if (m_currentEnergy < m_energyMax/4)
            {
                return;
            }
            
            isEvading = true;

            RemoveEnergy(m_energyMax/4);
            
            OnEvadeUsed?.Invoke(this);

            m_currentTimers.Add(new CustomTimer(evadeUseTimerIdentifier, m_evadeDuration, false, EndEvade));
        }

        private void EndEvade()
        {
            isEvading = false;
        }

        private void UseBunt()
        {
            if (m_currentEnergy < m_energyMax)
            {
                return;
            }

            m_buntAbility.DoAbility();
            
            //ToDo: pause player for animation
            
            m_currentEnergy = m_energyMax / 4f;
            OnEnergyAmountChanged?.Invoke(this, m_currentEnergy);
            UpdateShieldScale();
        }

        public void AddEnergy(float _amountToAdd)
        {
            m_currentEnergy = Mathf.Clamp(m_currentEnergy + _amountToAdd, 0, m_energyMax);
            OnEnergyAmountChanged?.Invoke(this, m_currentEnergy);
            
            UpdateShieldScale();
        }

        private void RemoveEnergy(float _amountToRemove)
        {
            m_currentEnergy = Mathf.Clamp(m_currentEnergy - _amountToRemove, 0, m_energyMax);
            OnEnergyAmountChanged?.Invoke(this, m_currentEnergy);
            
            UpdateShieldScale();
        }

        private void UpdateShieldScale()
        {
            m_energyShieldIndicator.transform.localScale = Vector3.one * ((m_currentEnergy / m_energyMax) + 0.2f);
        }

        protected void DisplayEnergyShield(bool _isActive)
        {
            if (m_energyShieldIndicator.IsNull())
            {
                return;
            }
            
            if (_isActive)
            {
                m_energyShieldParent.LookAt(mainCamera.transform);
            }
            
            m_energyShieldIndicator.SetActive(_isActive);
        }
        
        protected void UseEnergyShield()
        {
            if (m_isInAction || m_isShieldPopStunned || m_currentEnergy <= 0 || m_isKnockedBack || m_isStunned)
            {
                return;
            }

            isShielding = true;
            
            if (!m_energyShieldIndicator.activeSelf)
            {
                DisplayEnergyShield(true);
            }
            
            if (m_currentEnergy <= 0)
            {
                if (!m_isShieldPopStunned)
                {
                    PopEnergyShield();
                }
                return;
            }

            RemoveEnergy(Time.deltaTime * m_energyDepleteAmount);
        }

        protected void EndEnergyShield()
        {
            isShielding = false;
            DisplayEnergyShield(false);
        }

        protected void PopEnergyShield()
        {
            m_isShieldPopStunned = true;

            m_shieldPopVFX.Play();
            m_shieldPopStunVFX.Play();
            
            EndEnergyShield();
            m_currentTimers.Add(new CustomTimer(shieldPopTimerIdentifier, m_energyShieldPopStunDuration, false, EndShieldPopStun));
        }

        protected void EndShieldPopStun()
        {
            if (m_shieldPopStunVFX.is_playing)
            {
                m_shieldPopStunVFX.Stop();
            }
            
            m_isShieldPopStunned = false;
            HalfRegenShieldEnergy();
        }

        protected void HalfRegenShieldEnergy()
        {
            m_currentEnergy = m_energyMax / 2f;
            UpdateShieldScale();
            OnEnergyAmountChanged?.Invoke(this, m_currentEnergy);
        }

        #endregion

        #region Character Movement -----------------

        protected void MoveCharacter()
        {
            if (characterController.IsNull())
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
            if (!m_canCheckBallInput || m_isKnockedBack || m_isStunned || isShielding || isEvading)
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

        protected void CheckHitBall()
        {
            if (m_isKnockedBack || m_isStunned)
            {
                return;
            }
            
            m_hitAmount = Physics.OverlapSphereNonAlloc(m_sphereCheckLocation.position, m_wackRangeCurrent, 
                m_hitColliders, characterData.ballMeleeLayerMask);
            
            if (m_hitAmount == 0)
            {
                return;
            }

            for (int i = 0; i < m_hitAmount; i++)
            {
                m_hitColliders[i].TryGetComponent(out BallBehavior _ball);

                if (_ball.IsNull())
                {
                    continue;
                }

                _ball.HitBall(m_playerAimVector, m_ballMeleeChargeAmount / m_ballMeleeChargeAmountMax > m_meleeChargeThreshold ? HitStrength.MEDIUM : HitStrength.LIGHT ,this);
                EarlyEndTimer(ballHitTimerIdentifier);
            }
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
            
            m_canReadPlayerInput = false;
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

            m_knockbackForce *= Mathf.Exp((m_isDepletedArmor ? -m_unarmoredKnockbackReductionRate : -m_armoredKnockbackReductionRate) * Time.deltaTime);
        }

        protected bool HasHitArenaBorder()
        {
            return Physics.Raycast(transform.position, m_knockbackMoveVector, 1f, m_wallLayerMask);
        }

        protected void EndKnockBack()
        {
            m_canReadPlayerInput = true;
            m_isKnockedBack = false;
            m_knockbackForce = 0;
            m_knockbackTime = 0;
        }

        #endregion

        #region Ability Related ---------------

        protected void CheckAbilityCooldowns()
        {
            if (m_abilitiesOnCooldown.Count <= 0)
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

        protected void UseAbility(int _abilityID)
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

            if (isShielding || isEvading)
            {
                return;
            }

            m_currentHeldAbility = m_heldResetNum;
            m_assignedAbilities[_abilityID].ShowAttackIndicator(false);
            
            CancelWackCharge();
            
            m_assignedAbilities[_abilityID].DoAbility();

            if (m_assignedAbilities[_abilityID].canUseAbility)
            {
                return;
            }
            
            OnAbilityUsed?.Invoke(this, m_assignedAbilities[_abilityID]);
            m_abilitiesOnCooldown.Add(m_assignedAbilities[_abilityID]);
        }

        private void ShowAbilityIndicator(int _abilityID)
        {
            if (m_assignedAbilities.Count == 0)
            {
                return;
            }
            
            if (m_currentHeldAbility > m_heldResetNum && m_currentHeldAbility != _abilityID)
            {
                //player starts holding a different ability
                m_assignedAbilities[m_currentHeldAbility].ShowAttackIndicator(false);
            }

            if (_abilityID >= m_assignedAbilities.Count || m_assignedAbilities[_abilityID].IsNull())
            {
                return;
            }

            if (!m_assignedAbilities[_abilityID].canUseAbility)
            {
                return;
            }

            if (isShielding || isEvading)
            {
                return;
            }
            
            m_currentHeldAbility = _abilityID;
            m_assignedAbilities[_abilityID].ShowAttackIndicator(true);
        }

        public void AddAbilityCooldown(AbilityBase _ability)
        {
            var _matchingAbility =
                m_assignedAbilities.FirstOrDefault(ab =>
                    ab.abilityData.abilityName == _ability.abilityData.abilityName);

            if (_matchingAbility.IsNull())
            {
                return;
            }
            
            OnAbilityUsed?.Invoke(this, _matchingAbility);
            m_abilitiesOnCooldown.Add(_matchingAbility);
        }

        #endregion

        #region Perk Related -------------

        public async UniTask AddPerk(PerkDataBase _perkData)
        {
            if (_perkData.IsNull())
            {
                return;
            }
            
            var _currentPerkPrefab = await ObjectPoolController.Instance.T_CreateParentedObject(_perkData.name,
                _perkData.perkGameObject, m_statusHolder);
            
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

        #region Status Related --------------

        public async UniTask ApplyStatus(StatusData _statusData)
        {
            if (_statusData.IsNull())
            {
                return;
            }
            
            var _currentStatusPrefab = await ObjectPoolController.Instance.T_CreateParentedObject(_statusData.name,
                _statusData.statusGameObject, m_statusHolder);

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

        #endregion

        #region IDamagable Inherited Methods

        public virtual void OnRevive()
        {
            m_currentArmor = m_currentMaxArmor;
            m_isAlive = true;
            OnPlayerRevived?.Invoke(this);
            OnArmorAmountChanged?.Invoke(this, m_currentArmor, null);
        }

        public virtual void OnHeal(float _healAmount)
        {
            m_currentArmor = Mathf.Clamp(m_currentArmor + Mathf.RoundToInt(_healAmount), 0, m_currentMaxArmor);
            OnArmorAmountChanged?.Invoke(this, m_currentArmor, null);
        }

        public void OnDealDamage(Transform _attacker, float _damageAmount, BaseCharacter _attackingCharacter = null)
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
                //** Formula => AmountReduction = (_damageAmount / characterMaxArmor) * maxShieldEnergy
                //--- This will reduce the energyAmount by an amount equal to damage the player would have taken 
                
                RemoveEnergy((_damageAmount / m_currentMaxArmor) * m_energyMax);
                return;
            }

            var _damageIntakeAmount = Mathf.CeilToInt(_damageAmount * m_damageIntakeMod);
            m_currentArmor = Mathf.Clamp(m_currentArmor - _damageIntakeAmount, 0, m_currentMaxArmor);

            if (!_attackingCharacter.IsNull())
            {
                _attackingCharacter.AddEnergy(m_pvpDamageEnergyAddAmount);
            }
            
            JuiceGameController.Instance.CreateDamageText(_damageIntakeAmount, transform.position);
            OnArmorAmountChanged?.Invoke(this, m_currentArmor, _attackingCharacter);
        }

        public virtual void OnKillPlayer(Vector3 _deathPosition, Vector3 _deathDirection)
        {
            OnPlayerPreDeath?.Invoke(this);

            RemoveAllStatuses();
            
            m_canReadPlayerInput = false;
            m_characterMoveVector = Vector3.zero;
            m_isKnockedBack = false;
            m_knockbackForce = 0;
            m_knockbackTime = 0;
            m_lastAttackingPlayer.AddEnergy(m_playerKillEnergyAddAmount);
            
            m_player.SetVibration(0, 0.6f, 0.25f);
            
            OnPlayerDeath?.Invoke(this, m_lastAttackingPlayer, transform.position);
        }
        
        #endregion

        #region IKnockbackable Inherited Methods

        private async UniTask T_OnKnockback()
        {
            Debug.Log($"Current Hit Stun Settings: Time= {m_currentHitStunTime} Frequency= {m_hitStunV3FrequencyCurrent}, Vibrato= {m_currentVirbrato}");
            
            m_characterModelHolder.DOShakePosition( m_currentHitStunTime, m_hitStunV3FrequencyCurrent , m_currentVirbrato,
                0);

            await UniTask.WaitForSeconds(m_currentHitStunTime);

            m_characterModelHolder.localPosition = Vector3.zero;
            
            StartKnockBack();
        }

        public void ApplyKnockback(Transform _attackerTransform, BaseCharacter _lastAttacker, 
            float _baseKnockbackAmount, Vector3 _forcedDirection, bool _isBallHit = false)
        {
            if (_baseKnockbackAmount <= 0 || isShielding || isEvading)
            {
                return;
            }

            PlayDamageSFX();
            
            m_knockbackForce = _baseKnockbackAmount * (m_currentArmor > 0 ? 1 : _isBallHit ? 10 : 2.5f) *
                               (1 - characterData.characterNaturalKnockbackResistance);
            
            m_knockbackMoveVector = _forcedDirection == Vector3.zero ? transform.position - _attackerTransform.position : _forcedDirection.FlattenVector3Y();

            m_lastAttackingPlayer = _lastAttacker;
            
            //ToDo: can use end point calculation to do a SmashBros last hit FX
            //var m_knockbackEndPos = transform.position + (m_knockbackMoveVector.normalized * _baseKnockbackAmount);
            
            float _percentage = m_knockbackForce / m_hitStunKnockbackThreshold;

            m_currentHitStunTime = m_hitStunMaxTime * _percentage;
            m_currentHitStunFrequency = m_hitStunMaxFrequency * _percentage;
            m_currentVirbrato = Mathf.CeilToInt(m_hitStunMaxVirbrato * _percentage);
            m_hitStunV3FrequencyCurrent = m_knockbackMoveVector.normalized * m_currentHitStunFrequency;

            VFXController.Instance.PlayDamageVFX(transform.position, Quaternion.identity);
            
            //ToDo: Controller Vibration
            //m_player.SetVibration(0, 0.1f, 0.1f);
            T_OnKnockback();
        }

        #endregion
        
       
    }
}