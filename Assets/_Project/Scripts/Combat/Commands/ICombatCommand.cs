using System.Collections;

namespace Game.Combat
{
    public interface ICombatCommand
    {
        // Trả về một Coroutine nếu lệnh cần thời gian để hoàn thành,
        // hoặc null nếu nó thực thi ngay lập tức.
        IEnumerator Execute();
    }
}