using LiteDB;
using NetCord;

namespace Arona.Models.DB;

public class User
{
    [BsonId]
    public required string Id { get; set; }

    [BsonField("custom_leaderboard")]
    public CustomLeaderboard? CustomLeaderboard { get; set; }

    /// <summary>
    /// Checks if a user exists in database
    /// </summary>
    /// <remarks>Creates a new <see cref="User"/> object if not found</remarks>
    public static void Exists(string userId)
    {
        if (Collections.Users.Exists(g => g.Id == userId))
            return;

        Collections.Users.Insert(new User
        {
            Id = userId,
        });
    }

    /// <summary>
    /// Finds a user from database via user id
    /// </summary>
    /// <remarks>Creates a new <see cref="User"/> object if not found</remarks>
    /// <returns>a <see cref="User"/> object</returns>
    public static User Find(string userId)
    {
        var user = Collections.Users.FindById(userId);

        if (user != null)
            return user;

        user = new User
        {
            Id = userId
        };

        Collections.Users.Insert(user);
        return user;
    }
}

public class CustomLeaderboard
{
    [BsonField("region")]
    public required Region Region { get; set; }

    [BsonField("clans")]
    public required List<int> Clans { get; set; }
}