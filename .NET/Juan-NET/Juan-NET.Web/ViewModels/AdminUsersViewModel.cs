namespace Juan_NET.Web.ViewModels
{
    public class AdminUsersViewModel
    {
        public string Search { get; set; } = string.Empty;

        public List<User> Users { get; set; } = [];
    }
}
