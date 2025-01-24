using Project.Scripts.Utils;
using Runtime.Character;
using UnityEngine;

namespace Runtime.Gameplay
{
    public class WallCreation: CreationEntityBase
    {
        protected override void CheckDetectionRange()
        {
            if (!m_isInitialized)
            {
                return;
            }

            //ToDo: change all these to Non-Alloc version for performance
            m_hitAmount = Physics.OverlapBoxNonAlloc(transform.position + m_creationAbilityData.wallCenter,
                m_creationAbilityData.wallHalfExtents, m_hitColliders,Quaternion.Euler(m_savedAimDirection),m_detectableLayers);

            if (m_hitAmount == 0)
            {
                return;
            }

            for (int i = 0; i < m_hitAmount; i++)
            {
                m_hitColliders[i].TryGetComponent(out BaseCharacter _character);
                
                if (_character == m_owner)
                {
                    continue;
                }

                if (m_creationAbilityData.applicableStatusesOnHit.Count > 0)
                {
                    foreach (var _statusData in m_creationAbilityData.applicableStatusesOnHit)
                    {
                        if(_character.IsNull() || _character.ContainsStatus(_statusData)){
                            continue;   
                        }

                        _character.ApplyStatus(_statusData);
                    }
                }
                
                //first check if ball -> hit ball
                HitCollider(m_hitColliders[i]);
            }
        }
    }
}