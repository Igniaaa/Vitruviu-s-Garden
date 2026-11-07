using UnityEngine;

namespace Basilica.Crafting
{
    [CreateAssetMenu(menuName = "Vitruvio Garden/Construction/Material")]
    public sealed class MaterialDefinition : ScriptableObject
    {
        public string displayName;
        [Min(1)] public int unitPrice = 1;
    }
}
