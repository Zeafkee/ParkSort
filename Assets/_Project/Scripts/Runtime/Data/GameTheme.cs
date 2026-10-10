using TMPro;
using UnityEngine;

namespace ParkSort.Data
{
    [CreateAssetMenu(menuName = "ParkSort/Game Theme", fileName = "GameTheme")]
    public sealed class GameTheme : ScriptableObject
    {
        [Header("Colors (index = color id)")]
        public Color[] colors = new Color[0];
        public Material[] paintMaterials = new Material[0];
        public Material hiddenMaterial;
        public Material neutralMaterial;
        public Material shadowMaterial;
        public TMP_FontAsset font;
        public Material fontMaterial;

        [Header("Meshes")]
        public Mesh car;
        public Mesh van;
        public Mesh question;
        public Mesh garage;
        public Mesh waitingSlot;
        public Mesh tunnelBase;
        public Mesh tunnelLight;
        public Mesh quad;
        public Mesh[] obstacles = new Mesh[0];
        public Mesh[] decor = new Mesh[0];
    }
}
