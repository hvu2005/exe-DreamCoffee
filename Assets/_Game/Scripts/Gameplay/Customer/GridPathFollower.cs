using System.Collections.Generic;
using DreamCafe.SystemControl.Navigation;
using UnityEngine;

namespace DreamCafe.Gameplay.Customer
{
    /// <summary>
    /// Di chuyển khách theo đường do <see cref="IPathfindingService"/> trả về, đi lần lượt qua từng
    /// điểm trên lộ trình. Bản thân nó không biết đường được tính bằng A* hay gì khác — service được
    /// tiêm vào từ ngoài, nên đổi cách tìm đường không phải sửa ở đây.
    ///
    /// Thay cho NavMeshAgent vì lưới isometric quá mịn so với NavMesh — hai ô kề nhau chỉ cách
    /// 0.25 đơn vị theo trục y, mọi vùng khoét quanh cái bàn đều nuốt luôn ô ghế bên cạnh nên khách
    /// không bao giờ đứng đúng chỗ ngồi. Đi theo ô thì khách dừng đúng tâm ghế, và né đồ là hệ quả
    /// tự nhiên của bản đồ ô chứ không phải của hình học vật cản.
    /// </summary>
    public sealed class GridPathFollower : MonoBehaviour
    {
        [SerializeField, Min(0.1f), Tooltip("Tốc độ đi (đơn vị thế giới / giây).")]
        private float _speed = 1.8f;

        [SerializeField, Min(0.001f), Tooltip("Tới gần tâm ô dưới khoảng này thì coi như đã qua ô đó.")]
        private float _waypointTolerance = 0.02f;

        [SerializeField, Tooltip("Vẽ đường đang đi trong Scene view.")]
        private bool _drawPath = true;

        private readonly List<Vector3> _waypoints = new();
        private IPathfindingService _pathfinding;
        private int _index;
        private Vector3 _finalTarget;
        private bool _hasTarget;

        /// <summary>Đã tới đích (hoặc chưa được giao đích nào).</summary>
        public bool HasArrived => !_hasTarget || _index >= _waypoints.Count;

        /// <summary>Lần giao đích gần nhất có tìm được đường hay không.</summary>
        public bool PathFound { get; private set; } = true;

        /// <summary>Đích cuối cùng đang nhắm tới.</summary>
        public Vector3 Destination => _finalTarget;

        /// <summary>Tạm dừng chân tại chỗ (vẫn giữ đường đi để đi tiếp nếu bỏ dừng).</summary>
        public bool IsStopped { get; set; }

        /// <summary>
        /// Tiêm dịch vụ tìm đường. CustomerController gọi lúc Bind; thiếu nó thì vật thể chỉ biết
        /// đi thẳng tới đích.
        /// </summary>
        public void SetPathfinding(IPathfindingService pathfinding) => _pathfinding = pathfinding;

        /// <summary>
        /// Giao đích mới. Trả về false nếu không có đường tới — lúc đó nơi gọi tự quyết định
        /// (đổi chỗ ngồi khác, hay bỏ về).
        /// </summary>
        public bool SetDestination(Vector3 worldTarget)
        {
            _finalTarget = worldTarget;
            _hasTarget = true;
            _index = 0;
            IsStopped = false;

            if (_pathfinding == null)
            {
                // Chưa được tiêm service (scene test chẳng hạn) — đi thẳng, còn hơn đứng im.
                _waypoints.Clear();
                _waypoints.Add(worldTarget);
                PathFound = true;
                return true;
            }

            PathFound = _pathfinding.TryFindPath(transform.position, worldTarget, _waypoints);

            if (!PathFound)
            {
                _waypoints.Clear();
                return false;
            }

            // Chặng cuối đi thẳng vào đúng điểm được giao (tâm ghế, chỗ đứng quầy) thay vì dừng ở
            // tâm ô, để khách ngồi khít vào ghế.
            if (_waypoints.Count > 0) _waypoints[_waypoints.Count - 1] = worldTarget;
            else _waypoints.Add(worldTarget);

            return true;
        }

        /// <summary>Bỏ đích, đứng yên.</summary>
        public void Stop()
        {
            _hasTarget = false;
            _waypoints.Clear();
            _index = 0;
        }

        /// <summary>Đặt khách đúng vào một điểm, không đi bộ (dùng lúc spawn ra từ pool).</summary>
        public void Teleport(Vector3 world)
        {
            Stop();
            transform.position = new Vector3(world.x, world.y, 0f);
        }

        private void Update()
        {
            if (IsStopped || HasArrived) return;

            Vector3 target = _waypoints[_index];
            Vector3 position = transform.position;
            float step = _speed * Time.deltaTime;

            if ((target - position).sqrMagnitude <= _waypointTolerance * _waypointTolerance)
            {
                transform.position = new Vector3(target.x, target.y, 0f);
                _index++;
                return;
            }

            Vector3 next = Vector3.MoveTowards(position, target, step);
            transform.position = new Vector3(next.x, next.y, 0f);
        }

        private void OnDrawGizmos()
        {
            if (!_drawPath || HasArrived) return;

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Vector3 from = transform.position;
            for (int i = _index; i < _waypoints.Count; i++)
            {
                Gizmos.DrawLine(from, _waypoints[i]);
                Gizmos.DrawWireSphere(_waypoints[i], 0.06f);
                from = _waypoints[i];
            }
        }
    }
}
