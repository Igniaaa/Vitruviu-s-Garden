using System;
using System.Collections.Generic;
using UnityEngine;

namespace Basilica.Crafting
{
    [Serializable]
    public struct MaterialCost
    {
        public MaterialDefinition material;
        [Min(2)] public int quantity;
        public MaterialCost(MaterialDefinition material, int quantity)
        { this.material = material; this.quantity = quantity; }
    }

    [CreateAssetMenu(menuName = "Vitruvio Garden/Construction/Recipe")]
    public sealed class CraftingRecipe : ScriptableObject
    {
        public string displayName;
        [Min(0)] public int buildPhase;
        public GameObject piecePrefab;
        public MaterialCost[] costs = Array.Empty<MaterialCost>();
        [Range(2, 64)] public int noteCount = 8;
        [Min(1)] public int minimumHits = 6;
        [Range(40, 180)] public float bpm = 100;
        [Range(0.02f, 0.3f)] public float hitWindow = 0.15f;
        public bool IsValid(out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(displayName) || piecePrefab == null || buildPhase < 0)
                error = "Ricetta non valida: controlla nome, prefab e fase.";
            else if (noteCount < 2 || noteCount > 64 || minimumHits < 1 || minimumHits > noteCount ||
                     float.IsNaN(bpm) || bpm < 40 || bpm > 180 || float.IsNaN(hitWindow) ||
                     hitWindow < .02f || hitWindow > 15f / bpm + .00001f)
                error = "Ricetta non valida: controlla note, soglia, BPM e finestra ritmica.";
            else if (costs == null || costs.Length == 0)
                error = "La ricetta non contiene materiali.";
            else
            {
                var seen = new HashSet<MaterialDefinition>();
                foreach (var cost in costs)
                    if (cost.material == null || string.IsNullOrWhiteSpace(cost.material.displayName) ||
                        cost.material.unitPrice <= 0 || cost.quantity <= 0 || cost.quantity % 2 != 0 ||
                        !seen.Add(cost.material))
                    {
                        error = "Materiali non validi: quantità positive e pari, senza duplicati.";
                        break;
                    }
            }
            return error == null;
        }
    }
}
