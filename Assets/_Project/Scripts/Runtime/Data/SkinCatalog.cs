using System;
using UnityEngine;

namespace ParkSort.Data
{
    [Serializable]
    public sealed class Skin
    {
        public string title = "CLASSIC";
        public int price;
        public Color grass = Color.green;
        public Color grassAlternate = Color.green;
        public Color lot = Color.grey;
        public Color road = Color.grey;
        public Color line = Color.white;
        public Color curb = Color.white;
        public Color light = Color.white;
        public Color ambient = Color.grey;
    }

    public enum BoosterKind
    {
        Undo,
        Hint,
        ExtraSlot,
    }

    [Serializable]
    public struct BoosterPack
    {
        public BoosterKind kind;
        public int amount;
        public int price;
    }

    [CreateAssetMenu(menuName = "ParkSort/Shop Catalog", fileName = "ShopCatalog")]
    public sealed class SkinCatalog : ScriptableObject
    {
        public Skin[] skins = new Skin[0];
        public BoosterPack[] packs = new BoosterPack[0];
    }
}
