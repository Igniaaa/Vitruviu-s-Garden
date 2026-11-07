using NUnit.Framework;
using UnityEngine;

namespace Basilica.Crafting.Tests
{
    public sealed class ExtractionEconomyTests
    {
        private MaterialDefinition wood, iron;
        [SetUp] public void Setup()
        {
            wood=ScriptableObject.CreateInstance<MaterialDefinition>(); wood.displayName="Legno"; wood.unitPrice=2;
            iron=ScriptableObject.CreateInstance<MaterialDefinition>(); iron.displayName="Ferro"; iron.unitPrice=4;
        }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(wood); Object.DestroyImmediate(iron); }
        [Test] public void MissingIronDoesNotConsumeWood()
        {
            var inventory=new MaterialInventory(100); inventory.TryBuy(wood,4,out _);
            Assert.That(inventory.TrySpend(new[]{new MaterialCost(wood,4),new MaterialCost(iron,2)},out _),Is.False);
            Assert.That(inventory.Count(wood),Is.EqualTo(4));
        }
        [Test] public void MachineCostsConsumeMaterialsOnceWithoutChargingCoinsAgain()
        {
            var inventory=new MaterialInventory(100); inventory.TryBuy(wood,4,out _); inventory.TryBuy(iron,2,out _);
            var costs=new[]{new MaterialCost(wood,4),new MaterialCost(iron,2)};
            Assert.That(inventory.TrySpend(costs,out _),Is.True);
            Assert.That(inventory.Coins,Is.EqualTo(84));
            Assert.That(inventory.TrySpend(costs,out _),Is.False);
        }
        [Test] public void CreditingProductionCannotOverflowWallet()
        {
            var inventory=new MaterialInventory(int.MaxValue-5);
            Assert.That(inventory.TryCredit(5),Is.True);
            Assert.That(inventory.TryCredit(1),Is.False);
            Assert.That(inventory.TryCredit(-1),Is.False);
            Assert.That(inventory.Coins,Is.EqualTo(int.MaxValue));
        }
        [Test] public void DuplicateCostsAreRejectedBeforeAnyConsumption()
        {
            var inventory=new MaterialInventory(100); inventory.TryBuy(wood,4,out _);
            Assert.That(inventory.TrySpend(new[]{new MaterialCost(wood,2),new MaterialCost(wood,2)},out _),Is.False);
            Assert.That(inventory.Count(wood),Is.EqualTo(4));
        }
    }
}
