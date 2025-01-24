using UnityEngine;

namespace Runtime.Gameplay
{
    public class RangeDetector: MonoBehaviour
    {

        protected float range;

        protected LayerMask detectableLayers;

        public bool IsInRange()
        {
            return Physics.OverlapSphere(transform.position, range, detectableLayers).Length > 0;
        }
        
        
    }
}