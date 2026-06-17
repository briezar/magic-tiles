using UnityEngine;

namespace MagicTiles.Data
{
    [CreateAssetMenu(menuName = "MagicTiles/Song Info")]
    public class SongInfoSO : ScriptableObject
    {
        [Header("Metadata")]
        public string SongTitle = "Unknown";
        public string ArtistName = "Unknown";
        public AudioClip MusicClip;
        public Sprite AlbumArt;
    }
}