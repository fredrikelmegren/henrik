using UnityEngine;
using System.Collections;

/// <summary>
/// Causes our demo units to "wander about" randomly.
/// Doesn't deal with any collision detection or actual pathing.
/// </summary>
public class DemoWander : MonoBehaviour
{
    private const int WanderPercentage = 70;

    private bool _isWandering;
    private int _moveDir;   // 0 = north, 1 = east, 2 = south, 3 = west

    private Vector3 _moveFrom;
    private Vector3 _moveTo;

    private float _actionTime = 1f;
    private float _moveTime = 1f;
    private float _idleTime = 1f;

    private int[] _xDirs = new int[] { 0, 1, 0, -1 };
    private int[] _zDirs = new int[] { 1, 0, -1, 0 };

    private Demo _demo;

    void Start()
    {
        _demo = (Demo)FindObjectOfType(typeof(Demo));

        // make each unit move time a little different just for giggles
        _actionTime = Random.Range(0.5f, 2.5f);
        _moveTime = Random.Range(0.9f, 1.3f);
        _idleTime = Random.Range(0.9f, 1.3f);
    }

    public void Update()
    {
        var t = Time.deltaTime;

        _actionTime -= t;
        if (_actionTime <= 0)
        {
            // we've completed the "action" so pick something new to do (move or idle)
            if (_isWandering)
            {
                var pos = transform.position;

                pos.x = _moveTo.x;
                pos.z = _moveTo.z;

                // note: y will be updated by the demo automatically to keep all units "on the ground"
                // pos.y = demo.GetHeightAt(pos.x, pos.z);

                transform.position = pos;
            }

            PickNewAction();
        }
        else
        {
            // we're still working on the current action (moving or idling)
            if (_isWandering)
            {
                var pos = transform.position;

                var moveTime = (_moveTime - _actionTime) / _moveTime;
                pos.x = Mathf.Lerp(_moveFrom.x, _moveTo.x, moveTime);
                pos.z = Mathf.Lerp(_moveFrom.z, _moveTo.z, moveTime);

                transform.position = pos;
            }
        }
    }

    private void PickNewAction()
    {
        _moveFrom = transform.position;
        _moveTo = _moveFrom;

        var r = Random.Range(0, 100);

        if (r <= WanderPercentage) // percent change to move
        {
            if (!CanMoveInDirection(_moveDir) || r < 20)
            {
                // pick a new direction to wander
                var turnDir = (Random.Range(0, 2) == 0) ? 1 : -1;

                var origMoveDir = _moveDir;

                var maxTries = 4;
                while (maxTries > 0)
                {
                    _moveDir += turnDir;

                    if (_moveDir == origMoveDir)
                    {
                        // can't move, so idle
                        _isWandering = false;
                        _actionTime += _idleTime;
                        break;
                    }

                    if (_moveDir < 0)
                        _moveDir = 3;
                    else if (_moveDir > 3)
                        _moveDir = 0;

                    if (CanMoveInDirection(_moveDir))
                    {
                        _actionTime += _moveTime;
                        break;
                    }

                    maxTries--;
                }
            }
            else
            {
                // wander in the same direction
                _isWandering = true;
                _actionTime += _moveTime;
            }

            _moveTo = _moveFrom;
            _moveTo.x += 4 * _xDirs[_moveDir];
            _moveTo.z += 4 * _zDirs[_moveDir];
        }
        else
        {
            // idle
            _isWandering = false;
            _actionTime += _idleTime * Random.Range(0.5f, 3f);
        }
    }

    private bool CanMoveInDirection(int moveDir)
    {
        int myTileX, myTileZ;
        _demo.WorldPositionToTile(transform.position, out myTileX, out myTileZ);

        var moveToTileX = myTileX + _xDirs[moveDir];
        var moveToTileZ = myTileZ + _zDirs[moveDir];

        if (moveToTileX < 0 || moveToTileZ < 0 || moveToTileX >= _demo.SizeX || moveToTileZ >= _demo.SizeZ)
            return false;

        return !_demo.TileIsOccupied(moveToTileX, moveToTileZ);
    }
}
