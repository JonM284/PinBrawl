using System.Threading;
using Cysharp.Threading.Tasks;
using Project.Scripts.Utils;
using UnityEngine;

namespace Runtime.Character
{
    public class CharacterModelController: MonoBehaviour
    {

        private static readonly int outlineColorName = Shader.PropertyToID("_OutlineColor");
        private static readonly int outlineSizeName = Shader.PropertyToID("_OutlineSize");
        
        [SerializeField] private MeshRenderer MeshRenderer;
        [SerializeField] private SkinnedMeshRenderer characterMeshRenderer;
        [SerializeField] private Animator anim;
        [SerializeField] private float outlineBlinkingRescaleFactor = 1.5f;

        private bool playBlinkOutlineEffect;
        private Material outlineMaterial;
        private Color playerColor;
        private float originalOutlineScale;
        
        public async UniTask Initialize(Color _playerColor)
        {
            playerColor = _playerColor;
            outlineMaterial = !MeshRenderer.IsNull() ? MeshRenderer.materials[^1] : 
                !characterMeshRenderer.IsNull() ? characterMeshRenderer.materials[^1] : null;
            
            if(outlineMaterial.IsNull()) return;
            
            originalOutlineScale = outlineMaterial.GetFloat(outlineSizeName);
        }
        
        //ToDo: Damage Flash
        
        public async UniTask PlayFlashingOutlineEffect(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            
            var isBlinking = true;
            playBlinkOutlineEffect = true;

            try
            {
                ResizeOutlineScale(outlineBlinkingRescaleFactor);
                while (playBlinkOutlineEffect)
                {
                    if (!playBlinkOutlineEffect)
                    {
                        break;
                    }

                    var flashColor = isBlinking ? playerColor : Color.white;
                    isBlinking = !isBlinking;
                    ChangeOutlineColor(flashColor);
                    await UniTask.Delay(100, cancellationToken: token);
                }
            }
            finally
            {
                ResizeOutlineScale(1f);
                ChangeOutlineColor(Color.black);
            }
        }

        public void StopBlinkingOutlineEffect()
        {
            playBlinkOutlineEffect = false;
            ResizeOutlineScale(1f);
            ChangeOutlineColor(Color.black);
        }

        public void ChangeOutlineColor(Color color)
        {
            if (outlineMaterial.IsNull())
            {
                return;
            }
            
            outlineMaterial.SetColor(outlineColorName ,color);
        }

        private void ResizeOutlineScale(float _modifier)
        {
            if (outlineMaterial.IsNull())
            {
                return;
            }
            
            outlineMaterial.SetFloat(outlineSizeName, originalOutlineScale * _modifier);
        }
        
    }
}