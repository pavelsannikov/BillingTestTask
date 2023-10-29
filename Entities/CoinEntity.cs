
namespace Billing.Entities
{
    public class CoinEntity
    {
        public long Id { get; set; }
        public List<UserEntity> History { get; set; } = new();
    }
}
