﻿using UnityEngine;

namespace Project.Scripts.Utils
{
    public static class TransformUtils
    {
        
        #region Read-Only

        private static readonly string poolTag = "Pool";

        #endregion
        
        #region Class Implementation

        public static Transform CreatePool(Transform parentObject, bool isActive)
        {
            if (parentObject == null)
            {
                var nt = new GameObject().transform;
                nt.position = Vector3.down * 3;
                nt.gameObject.SetActive(isActive);
                return nt;
            }

            var t = new GameObject(parentObject.name + poolTag).transform;
            t.ResetTransform(parentObject);
            t.gameObject.SetActive(isActive);
            return t;
        }

        public static void ResetTransform(this Transform newTransform, Transform parentTransform = null)
        {
            if (parentTransform != null)
            {
                newTransform.parent = parentTransform;
                newTransform.position = parentTransform.position;
                return;
            }
            
            newTransform.position = Vector3.zero;
            newTransform.rotation = Quaternion.identity;
        }

        public static void ResetRectTransform(this RectTransform newRectTransform, RectTransform parentTransform = null)
        {
            if (parentTransform != null)
            {
                newRectTransform.parent = parentTransform;
                newRectTransform.anchoredPosition = parentTransform.anchoredPosition;
                return;
            }
            
            newRectTransform.position = Vector3.zero;
            newRectTransform.rotation = Quaternion.identity;
        }

        public static void ResetPRS(this Transform changedTransform, Transform unchangedTransform)
        {
            changedTransform.position = unchangedTransform.position;
            changedTransform.rotation = unchangedTransform.rotation;
            changedTransform.localScale = unchangedTransform.localScale;
        }

        public static void RenameTransform(this Transform newTransform, string newName)
        {
            if (newTransform == null)
            {
                return;
            }

            newTransform.name = newName;
        }

        #endregion




    }
}