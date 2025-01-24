using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using Project.Scripts.Utils;
using Runtime.Gameplay;
using Runtime.UI.Items;
using Runtime.VFX;
using Utils;
using Random = UnityEngine.Random;

namespace Runtime.GameControllers
{
    public class JuiceGameController: GameControllerBase
    {

        #region Static

        public static JuiceGameController Instance { get; private set; }

        #endregion

        #region Serialized Fields

        [SerializeField] private float m_stageY;
        
        [SerializeField] private DamageTextUIItem m_damageTextPrefab;

        [SerializeField] private RangeIndicatorEntity m_rangeAttackPrefab;

        #endregion

        #region Private Fields

        private bool m_isShakingCamera;

        private Vector3 m_randomOffset;

        private List<DamageTextUIItem> m_cachedDamageTexts = new List<DamageTextUIItem>();

        private List<RangeIndicatorEntity> m_cachedLineRenderers = new List<RangeIndicatorEntity>();

        private Transform m_textPool;
        
        #endregion
        
        #region Accessors

        private Camera cameraRef => CameraUtils.GetMainCamera();
        
        private Transform textPool => CommonUtils.GetRequiredComponent(ref m_textPool, () => TransformUtils.CreatePool(this.transform, false));
        
        #endregion
        
        #region GameControllerBase Inherited Methods

        public override void Initialize()
        {
            if (!Instance.IsNull())
            {
                return;
            }
            
            Instance = this;
            base.Initialize();
        }

        #endregion
        
        #region Class Implementation

        public void DoCameraShake(float _duration, float _strength, int _amplitude, float randomness)
        {
            cameraRef.DOShakePosition(_duration, _strength, _amplitude, randomness);
        }

        public void CreateDamageText(float _amount, Vector3 _position)
        {
            CreateDamageText(Mathf.FloorToInt(_amount), _position);
        }
        
        public void CreateDamageText(int _amount, Vector3 _position)
        {
            m_randomOffset = Random.insideUnitSphere.FlattenVector3Y() * 2;
            
            if (m_cachedDamageTexts.Count > 0)
            {
                var foundText = m_cachedDamageTexts.FirstOrDefault();
                
                m_cachedDamageTexts.Remove(foundText);

                foundText.transform.parent = null;
                
                foundText.transform.position = _position + m_randomOffset;
                
                foundText.Initialize(_amount);
                return;
            }

            var _createdText = Instantiate(m_damageTextPrefab.gameObject, _position + m_randomOffset, Quaternion.identity);

            _createdText.TryGetComponent(out DamageTextUIItem damageTextUIItem);

            if (damageTextUIItem)
            {
                damageTextUIItem.Initialize(_amount);
            }
        }

        public void CacheDamageText(DamageTextUIItem _item)
        {
            if (_item.IsNull())
            {
                return;
            }
            
            m_cachedDamageTexts.Add(_item);
            _item.transform.parent = textPool;
        }

        public void CreateRangeIndicator(Vector3 _startPos, Vector3 _endPos, float _scale ,float _duration = 1f)
        {
            if (m_cachedLineRenderers.Count > 0)
            {
                var _foundLine = m_cachedLineRenderers.FirstOrDefault();
                
                m_cachedLineRenderers.Remove(_foundLine);

                _foundLine.transform.parent = null;
                
                _foundLine.Initialize(_startPos.FlattenVectorToY(m_stageY), _endPos.FlattenVectorToY(m_stageY), _scale ,_duration);
                return;
            }

            var _createdRangeIndicator = Instantiate(m_rangeAttackPrefab.gameObject, Vector3.zero, Quaternion.Euler(new Vector3(90,0,0)));

            _createdRangeIndicator.TryGetComponent(out RangeIndicatorEntity _rangeIndicatorEntity);

            if (_rangeIndicatorEntity)
            {
                _rangeIndicatorEntity.Initialize(_startPos, _endPos, _scale, _duration);
            }
        }

        public void CacheRangeIndicator(RangeIndicatorEntity _indicator)
        {
            if (_indicator.IsNull())
            {
                return;
            }
            
            m_cachedLineRenderers.Add(_indicator);
            _indicator.transform.parent = textPool;
        }

        private Vector3 FlattenToStage(Vector3 _inputPos)
        {
            return new Vector3(_inputPos.x, m_stageY , _inputPos.z);
        }

        #endregion



    }
}