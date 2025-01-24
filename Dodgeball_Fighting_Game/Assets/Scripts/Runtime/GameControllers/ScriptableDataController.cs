using Project.Scripts.Utils;
using UnityEngine;

namespace Runtime.GameControllers
{
    public class ScriptableDataController: GameControllerBase
    {
        
        #region Static

        public static ScriptableDataController Instance { get; private set; }

        #endregion

        #region Serialized Fields
        
        [SerializeField] private int m_fenceDamage = 10;

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

        public int GetFenceDamage()
        {
            return m_fenceDamage;
        }

        #endregion
        
    }
}