using System.Threading;
using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;

namespace MagicTiles
{
    public static class FeelExtensions
    {
        public static async UniTask PlayFeedbacksAsync(this MMFeedbacks feedbacks, CancellationToken ct = default)
        {
            if (feedbacks == null) { return; }
            feedbacks.PlayFeedbacks();
            while (feedbacks != null && feedbacks.IsPlaying)
            {
                await UniTask.Yield(ct);
            }
        }

    }
}