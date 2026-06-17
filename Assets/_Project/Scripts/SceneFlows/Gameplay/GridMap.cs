using System.Collections;
using System.Collections.Generic;
using MagicTiles.Gameplay;
using EditorAttributes;
using UnityEngine;

namespace MagicTiles
{
    public class GridMap : MonoBehaviour
    {
        [SerializeField] private Grid _grid;
        [SerializeField] private HitLine _hitLine;
        [SerializeField] private Transform[] _lanes;
        [SerializeField] private LaneGlow[] _laneGlows;
        [SerializeField] private Transform[] _dividers;

        [SerializeField] private Vector3 _cellSpawnOffset = new(0, 6);

        public HitLine HitLine => _hitLine;
        public IReadOnlyList<LaneGlow> LaneGlows => _laneGlows;

        public Grid Grid => _grid;
        public Vector3 CellSize => _grid.cellSize;

        [Button]
        private void SetupMap()
        {
            var cellSize = _grid.cellSize;
            var cellGap = _grid.cellGap;
            var stride = cellSize.x + cellGap.x;
            var laneCount = _lanes.Length;

            var totalWidth = laneCount * cellSize.x + (laneCount - 1) * cellGap.x;
            var centerOffset = totalWidth / 2f - cellSize.x / 2f;

            for (var i = 0; i < laneCount; i++)
            {
                var x = i * stride - centerOffset;
                _lanes[i].localPosition = new Vector3(x, 0, 0);
                _laneGlows[i].transform.localPosition = new Vector3(x, _hitLine.transform.position.y, 0);
            }

            for (var i = 0; i < _dividers.Length; i++)
            {
                // Divider i sits at the left edge of column i, shifted back half a gap
                var x = i * stride - centerOffset - cellGap.x / 2f - cellSize.x / 2f;
                _dividers[i].localPosition = new Vector3(x, 0f, 0f);
            }
        }

        public Vector3 GetSpawnPosition(int lane)
        {
            var stride = _grid.cellSize + _grid.cellGap;
            return _lanes[lane].position + Vector3.Scale(_cellSpawnOffset, stride);
        }

        public float GetSpawnDistanceFromHitLine() => Mathf.Abs(GetSpawnPosition(0).y - _hitLine.transform.position.y);
    }
}
