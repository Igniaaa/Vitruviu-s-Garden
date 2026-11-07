using System;
using System.Collections.Generic;

namespace Basilica.Crafting
{
    public enum CraftOutcome { Failure, NormalSuccess, PerfectSuccess, TechnicalAbort }

    public sealed class MaterialInventory
    {
        public int Coins { get; private set; }
        private readonly Dictionary<MaterialDefinition, int> stock = new Dictionary<MaterialDefinition, int>();
        private readonly Dictionary<MaterialDefinition, int> reserved = new Dictionary<MaterialDefinition, int>();
        private bool busy;

        public MaterialInventory(int coins)
        {
            if (coins < 0) throw new ArgumentOutOfRangeException(nameof(coins));
            Coins = coins;
        }

        public int Count(MaterialDefinition material) => material != null && stock.TryGetValue(material, out var n) ? n : 0;

        public bool TrySpend(MaterialCost[] costs, out string error)
        {
            error = null;
            if (busy || costs == null || costs.Length == 0) { error = "Materiali non disponibili durante una lavorazione."; return false; }
            var seen = new HashSet<MaterialDefinition>();
            foreach (var cost in costs)
                if (cost.material == null || cost.quantity <= 0 || !seen.Add(cost.material) || Count(cost.material) < cost.quantity)
                { error = "Materiali insufficienti o costo non valido."; return false; }
            foreach (var cost in costs) stock[cost.material] = Count(cost.material) - cost.quantity;
            return true;
        }

        public bool TryCredit(int amount)
        {
            if (amount <= 0 || (long)Coins + amount > int.MaxValue) return false;
            Coins += amount;
            return true;
        }

        public bool TryBuy(MaterialDefinition material, int quantity, out string error)
        {
            error = null;
            if (material == null || string.IsNullOrWhiteSpace(material.displayName) || material.unitPrice <= 0 || quantity <= 0)
                error = "Acquisto non valido.";
            else
            {
                long price = (long)material.unitPrice * quantity;
                reserved.TryGetValue(material, out int held);
                if (price > Coins) error = "Monete insufficienti.";
                else if ((long)Count(material) + held + quantity > int.MaxValue) error = "Inventario pieno.";
                else
                {
                    Coins -= (int)price;
                    stock[material] = Count(material) + quantity;
                }
            }
            return error == null;
        }

        public bool TryReserve(CraftingRecipe recipe, out CraftingReservation reservation, out string error)
        {
            reservation = null;
            error = null;
            if (busy) error = "Una lavorazione è già in corso.";
            else if (recipe == null) error = "Scegli una ricetta.";
            else if (!recipe.IsValid(out error)) return false;
            else
            {
                foreach (var cost in recipe.costs)
                    if (Count(cost.material) < cost.quantity)
                    { error = "Materiali insufficienti: " + cost.material.displayName + "."; return false; }
                var snapshot = (MaterialCost[])recipe.costs.Clone();
                foreach (var cost in snapshot)
                {
                    stock[cost.material] = Count(cost.material) - cost.quantity;
                    reserved[cost.material] = cost.quantity;
                }
                busy = true;
                reservation = new CraftingReservation(this, snapshot);
            }
            return error == null;
        }

        internal void Release(MaterialCost[] costs, CraftOutcome outcome)
        {
            foreach (var cost in costs)
            {
                int refund = outcome == CraftOutcome.TechnicalAbort ? cost.quantity :
                    outcome == CraftOutcome.NormalSuccess ? 0 : cost.quantity / 2;
                stock[cost.material] = Count(cost.material) + refund;
                reserved.Remove(cost.material);
            }
            busy = false;
        }
    }

    public sealed class CraftingReservation
    {
        public bool IsSettled { get; private set; }
        private readonly MaterialInventory inventory;
        private readonly MaterialCost[] costs;
        internal CraftingReservation(MaterialInventory inventory, MaterialCost[] costs)
        { this.inventory = inventory; this.costs = costs; }

        public bool Settle(CraftOutcome outcome)
        {
            if (IsSettled) return false;
            if (!Enum.IsDefined(typeof(CraftOutcome), outcome)) throw new ArgumentOutOfRangeException(nameof(outcome));
            IsSettled = true;
            inventory.Release(costs, outcome);
            return true;
        }
    }
}
