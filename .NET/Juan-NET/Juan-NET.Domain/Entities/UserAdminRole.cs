namespace Juan_NET.Domain.Entities
{
    public class UserAdminRole
    {
        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public int AdminRoleId { get; set; }

        public AdminRole AdminRole { get; set; } = null!;
    }
}
