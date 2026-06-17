using MagicTiles.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;
using MoreMountains.Feedbacks;

namespace MagicTiles.Gameplay
{
    public class StartNoteTile : NoteTile
    {
        [field: SerializeField] public MMF_Player HitFeedback { get; private set; }

        public override void Tick(float deltaTime) { }

    }
}