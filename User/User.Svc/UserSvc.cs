namespace User.Svc;

public class Usr
{
    public required string Id { get; set; }
    public required string FullName { get; set; }
    public string? Address { get; set; }
    public DateTime? BirthDay { get; set; }
    public string? Description { get; set; }
    public bool Active { get; set; } = true;
}

public interface IUsersSvc
{
    List<Usr> GetAll();
    Usr? GetById(string id);
    string Create(Usr user);
    string DelById(string id);
    string Update(Usr user);
    List<Usr> GetByKwd(string keyword);
}


public class UsersSvc : IUsersSvc
{
    public readonly List<Usr> users = new List<Usr>
    {
        new() {
            Id = "1",
            FullName = "Nguyen Van A",
            Address = "123 Street, City",
            BirthDay = new DateTime(1990, 1, 1),
            Description = "Description for Nguyen Van A",
            Active = true
        },
        new() {
            Id = "2",
            FullName = "Tran Thi B",
            Address = "456 Avenue, City",
            BirthDay = new DateTime(1992, 2, 2),
            Description = "Description for Tran Thi B",
            Active = false
        },
        new() {
            Id = "3",
            FullName = "Le Van C",
            Address = "789 Boulevard, City",
            BirthDay = new DateTime(1988, 3, 3),
            Description = "Description for Le Van C",
            Active = true
        }
    };

    public List<Usr> GetAll()
    {
        return users;
    }

    public Usr? GetById(string id)
    {
        return users.FirstOrDefault(x => x.Id == id);
    }

    public string Create(Usr usr)
    {
        if (usr is null || string.IsNullOrWhiteSpace(usr.Id))
            return "Invalid user";
        if (users.Any(x => x.Id == usr.Id))
            return "User already exists";
        users.Add(usr);
        return null;
    }


    public string DelById(string id)
    {
        var user = users.FirstOrDefault(x => x.Id == id);
        if (user == null)
            return "User not found"; // User not found
        users.Remove(user);
        return null;
    }

    public string Update(Usr user)
    {
        var existingUser = users.FirstOrDefault(x => x.Id == user.Id);
        if (existingUser == null)
            return "User not found"; // User not found
        existingUser.FullName = user.FullName;
        existingUser.Address = user.Address;
        existingUser.BirthDay = user.BirthDay;
        existingUser.Description = user.Description;
        existingUser.Active = user.Active;
        return $"Update user '{user.FullName}' successfully!";
    }
    public List<Usr> GetByKwd(string keyword)
    {
        var matches = users
            .Where(x => x.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return matches;
    }
}