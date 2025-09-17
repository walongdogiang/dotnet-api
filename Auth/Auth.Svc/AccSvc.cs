namespace Auth.Svc.AccsSvc
{
    public class AccItm
    {
        public required string Id { get; set; }
        public string? Username { get; set; }
        public required string? Password { get; set; }
        public bool Active { get; set; } = true;
    }

    public interface IAccsSvc
    {
        List<AccItm> GetAll();
        string Register(string username, string password);
        string ChangePassword(string username, string password);
        string DelByUsn(string username);
        List<AccItm> GetByKwd(string keyword);
        bool IsExist(string username);
        bool Login(string username, string keyword);
    }


    public class AccsSvc : IAccsSvc
    {
        public readonly List<AccItm> users = new List<AccItm>
        {
            new () { Id = "1", Username = "user1", Password = "111", Active = true },
            new () { Id = "2", Username = "user2", Password = "222", Active = true },
            new () { Id = "3", Username = "user3", Password = "333", Active = true }
        };

        public List<AccItm> GetAll()
        {
            return users;
        }

        public string Register(string username, string password)
        {
            if (IsExist(username))
            {
                return "User already exists"; // User already exists
            }
            users.Add(new AccItm { Id = Guid.NewGuid().ToString(), Username = username, Password = password });
            return $"Add user '{username}' successfully!"; // User created successfully
        }

        public string ChangePassword(string username, string password)
        {
            var user = FindByUsn(username);
            if (user != null)
            {
                user.Password = password;
                return $"Change password successfully!"; // Password changed successfully
            }
            return $"User with username {username} not found"; // User not found
        }

        public string DelByUsn(string username)
        {
            var user = FindByUsn(username);
            if (user == null)
                return "User not found"; // User not found
            users.Remove(user);
            return $"Remove user successfully!";
        }
        public AccItm? FindByUsn(string username)
        {
            return users.FirstOrDefault(x => x.Username.Equals(username));
        }
        public List<AccItm> GetByKwd(string keyword)
        {
            var matches = users
        .Where(x => x.Username.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        .ToList();
            return matches;
        }

        public bool IsExist(string username)
        {
            return users.Any(x => x.Username.Equals(username));
        }
        public bool Login(string username, string password)
        {
            return users.Any(x => x.Username.Equals(username) && x.Password.Equals(password));
        }
    }
}