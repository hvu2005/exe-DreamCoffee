using System.Collections.Generic;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.SystemControl.Navigation
{
    /// <summary>
    /// Hợp đồng tìm đường cho mọi thứ biết đi trong quán. Tách thành interface + <see cref="IService"/>
    /// để đúng với cách phần còn lại của dự án làm việc: nơi dùng chỉ phụ thuộc vào abstraction, còn
    /// cách tìm đường (A* trên lưới ô hôm nay, NavMesh hay flow field sau này) là chi tiết thay được
    /// mà không phải đụng vào AI khách.
    /// </summary>
    public interface IPathfindingService : IService
    {
        /// <summary>
        /// Tìm đường đi giữa hai điểm. <paramref name="waypoints"/> là danh sách điểm cần bám theo,
        /// gồm cả điểm đích. Trả về false nếu bít đường.
        /// </summary>
        bool TryFindPath(Vector3 from, Vector3 to, List<Vector3> waypoints);

        /// <summary>Điểm này có đứng/đi được không.</summary>
        bool IsWalkable(Vector3 world);

        /// <summary>Kéo một điểm về chỗ đi được gần nhất (điểm đích bị đồ đạc đè lên chẳng hạn).</summary>
        bool TrySnapToWalkable(Vector3 world, out Vector3 result);
    }
}
