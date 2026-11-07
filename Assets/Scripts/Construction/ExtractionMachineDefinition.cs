using Basilica.Crafting;
using UnityEngine;

[CreateAssetMenu(menuName="Vitruvio Garden/Extraction Machine")]
public sealed class ExtractionMachineDefinition : ScriptableObject
{
    public string displayName = "Macchina estrattrice di Vitruvio";
    public MaterialCost[] costs;
    [Min(1)] public int coinsPerCycle = 5;
    [Min(1)] public float cycleSeconds = 10;
    public bool IsValid
    {
        get
        {
            if(costs == null || costs.Length == 0 || coinsPerCycle <= 0 ||
                float.IsNaN(cycleSeconds) || float.IsInfinity(cycleSeconds) || cycleSeconds < 1) return false;
            var seen = new System.Collections.Generic.HashSet<MaterialDefinition>();
            foreach(var cost in costs)
                if(cost.material == null || cost.quantity <= 0 || !seen.Add(cost.material)) return false;
            return true;
        }
    }
}
