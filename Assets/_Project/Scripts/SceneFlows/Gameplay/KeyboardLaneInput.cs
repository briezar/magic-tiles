using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

namespace MagicTiles.Gameplay
{
    public class KeyboardLaneInput : MonoBehaviour
    {
        [SerializeField] private TileController _tileController;

        private InputSystem_Actions _input;
        private readonly Action<CallbackContext>[] _pressHandlers = new Action<CallbackContext>[Constants.MaxLane];
        private readonly Action<CallbackContext>[] _releaseHandlers = new Action<CallbackContext>[Constants.MaxLane];

        private void Awake()
        {
            _input = new InputSystem_Actions();

            for (var i = 0; i < Constants.MaxLane; i++)
            {
                var lane = i;
                _pressHandlers[lane] = _ => _tileController.TapLowestInLane(lane);
                _releaseHandlers[lane] = _ => _tileController.ReleaseLowestInLane(lane);
            }
        }

        private void OnEnable()
        {
            _input.Gameplay.Enable();
            for (var i = 0; i < Constants.MaxLane; i++)
            {
                BindLane(i, true);
            }
        }

        private void OnDisable()
        {
            for (var i = 0; i < Constants.MaxLane; i++)
            {
                BindLane(i, false);
            }
            _input.Gameplay.Disable();
        }

        private void BindLane(int lane, bool bind)
        {
            var action = lane switch
            {
                0 => _input.Gameplay.TapLane0,
                1 => _input.Gameplay.TapLane1,
                2 => _input.Gameplay.TapLane2,
                3 => _input.Gameplay.TapLane3,
                _ => null,
            };
            if (action == null) { return; }

            if (bind)
            {
                action.started += _pressHandlers[lane];
                action.canceled += _releaseHandlers[lane];
            }
            else
            {
                action.started -= _pressHandlers[lane];
                action.canceled -= _releaseHandlers[lane];
            }
        }
    }
}