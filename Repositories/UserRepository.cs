using Billing.Entities;
namespace Billing.Repositories
{
    public class UserRepository
    {
        public List<UserEntity> UserList;
        public UserRepository()
        {
            UserList = new() {
            new UserEntity {
                Id=0,
                Name = "boris",
                Rating = 5000,
                },
            new UserEntity {
                Id=1,
                Name = "maria",
                Rating = 1000,
                },
            new UserEntity {
                Id=2,
                Name = "oleg",
                Rating = 800,
                },
            };
        }
    }
}
