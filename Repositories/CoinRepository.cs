using Billing.Entities;

namespace Billing.Repositories
{
    public class CoinRepository
    {
        public List<CoinEntity> CoinList;
        private long last_id;
        public CoinRepository()
        {
            CoinList = new();
            last_id = 0;
        }
        public void NewCoin(UserEntity owner)
        {
            CoinList.Add(new CoinEntity()
            {
                Id = last_id,
                History = new List<UserEntity>() { owner },
            });
            last_id++;
        }
    }
}
