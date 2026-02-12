using UnityEngine;

namespace Assets.Scripts.CardEngine.Cards.Views
{
    public sealed class DeployCostStars3DView : MonoBehaviour
    {
        [SerializeField] private Renderer _oneStar;
        [SerializeField] private Renderer _twoStar;
        [SerializeField] private Renderer _threeStar;

        public void Apply(int deployCost, Material baseMaterial, Material filledMaterial)
        {
            ApplyStar(_oneStar, deployCost >= 1, baseMaterial, filledMaterial);
            ApplyStar(_twoStar, deployCost >= 2, baseMaterial, filledMaterial);
            ApplyStar(_threeStar, deployCost >= 3, baseMaterial, filledMaterial);
        }

        private static void ApplyStar(Renderer renderer, bool filled, Material baseMaterial, Material filledMaterial)
        {
            if (renderer == null)
                return;

            renderer.material = filled ? filledMaterial : baseMaterial;
        }
    }
}
