using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MagicTiles.Gameplay
{
    /// <summary>
    /// The horizontal hit line near the bottom of the screen.
    /// </summary>
    public class HitLine : MonoBehaviour
    {
        [SerializeField] private Collider2D _collider;

        private static readonly Dictionary<Collider2D, HitLine> _colliderLookup = new();
        public static IReadOnlyDictionary<Collider2D, HitLine> ColliderLookup => _colliderLookup;

        private void OnEnable() => _colliderLookup[_collider] = this;
        private void OnDisable() => _colliderLookup.Remove(_collider);
    }
}
