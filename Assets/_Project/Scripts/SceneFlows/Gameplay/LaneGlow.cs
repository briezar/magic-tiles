using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace MagicTiles
{
    public class LaneGlow : MonoBehaviour
    {
        [field: SerializeField] public MMF_Player HitFeedback { get; private set; }
        [field: SerializeField] public int Lane { get; private set; }
    }
}
