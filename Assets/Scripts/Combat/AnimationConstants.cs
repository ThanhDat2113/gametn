/// <summary>
/// Chứa tên các trạng thái (states) trong Animator dưới dạng hằng số để tránh lỗi chính tả.
/// QUAN TRỌNG: Các giá trị này PHẢI khớp chính xác với tên State trong Animator Controller của bạn.
/// </summary>
public static class AnimationConstants
{
    public const string Idle = "Idle";
    public const string Rush = "Rush";
    public const string Hurt = "Hurt";
    public const string Knockback = "Knockback"; // Giữ lại nếu bạn có dùng
    public const string Die = "Die";

    // Thêm tên các animation tấn công của bạn ở đây
    // Ví dụ:
    public const string Attack = "Attack";
    public const string Skill1 = "Skill1";
    // ...
}