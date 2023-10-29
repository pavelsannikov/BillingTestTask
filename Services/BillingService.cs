using Billing;
using Billing.Entities;
using Billing.Repositories;
using Google.Protobuf;
using Grpc.Core;
using System.Linq;

namespace Billing.Services
{

    public class BillingService : Billing.BillingBase
    {
        private readonly UserRepository _userRepository;
        private readonly CoinRepository _coinRepository;
        private readonly ILogger<BillingService> _logger;
        public BillingService(ILogger<BillingService> logger, UserRepository userRepository, CoinRepository coinRepository)
        {
            _logger = logger;
            _userRepository = userRepository;
            _coinRepository = coinRepository;
        }

        public override async Task ListUsers(None request,
        IServerStreamWriter<UserProfile> responseStream,
        ServerCallContext context)
        {
            foreach (var user in _userRepository.UserList)
            {
                await responseStream.WriteAsync(new UserProfile
                {
                    Name = user.Name,
                    Amount = _coinRepository.CoinList.Count(c => c.History.LastOrDefault() == user),
                });
            }
        }
        public override Task<Response> CoinsEmission(EmissionAmount emission, ServerCallContext context)
        {
            int userAmount = _userRepository.UserList.Count;
            long coinAmount = emission.Amount;

            //проверяем, что каждый пользователь получит хотя бы одну монету, иначе ошибка
            if (userAmount > coinAmount)
            {
                return Task.FromResult(new Response
                {
                    Status = Response.Types.Status.Failed,
                    Comment = "Need more gold"
                });
            }
            // Выдадим каждому пользователю по монете
            foreach (var user in _userRepository.UserList)
            {
                _coinRepository.NewCoin(user);
            }
            coinAmount -= userAmount;

            //Выдадим оставшиеся монеты случайным образом, где вероятность выдачи равна ( рейтингПользователя / суммарныйРейтинг  )
            //Конечно, такой способ даёт несправедливый результат при малой суммарной эмиссии
            long ratingAmount = _userRepository.UserList.Sum(p => p.Rating);
            Random rnd = new Random();
            long ratingIndex = rnd.NextInt64(ratingAmount);
            for (int i = 0; i < coinAmount; i++)
            {
                int probablyOwnerIndex = 0;
                UserEntity probablyOwner = _userRepository.UserList[probablyOwnerIndex];
                long ratingUpperIndex = probablyOwner.Rating;
                while (ratingIndex >= ratingUpperIndex)
                {
                    probablyOwnerIndex++;
                    probablyOwner = _userRepository.UserList[probablyOwnerIndex];
                    ratingUpperIndex += probablyOwner.Rating;
                }
                _coinRepository.NewCoin(probablyOwner);
                ratingIndex = rnd.NextInt64(ratingAmount);
            }


            return Task.FromResult(new Response
            {
                Status = Response.Types.Status.Ok,
            });
        }
        public override Task<Response> MoveCoins(MoveCoinsTransaction transaction, ServerCallContext context)
        {
            UserEntity sender = _userRepository.UserList.Find(u => u.Name == transaction.SrcUser);
            UserEntity recipient = _userRepository.UserList.Find(u => u.Name == transaction.DstUser);
            if (sender == null || recipient == null)
            {
                return Task.FromResult(new Response
                {
                    Status = Response.Types.Status.Failed,
                    Comment = "user (or users) not found"
                });
            }
            // у CLR есть проблема со списками размером больше чем int.MaxValue элементов

            int senderCoinAmount = (int)_coinRepository.CoinList.LongCount(c => c.History.LastOrDefault() == sender);
            int coinAmount = (int)transaction.Amount;
            if (senderCoinAmount < coinAmount)
            {
                return Task.FromResult(new Response
                {
                    Status = Response.Types.Status.Failed,
                    Comment = "sender haven't enough coins"
                });
            }
            var coins = _coinRepository.CoinList.Where(c => c.History.LastOrDefault() == sender).Take(coinAmount);
            foreach (var coin in coins)
            {
                coin.History.Add(recipient);
            }
            return Task.FromResult(new Response
            {
                Status = Response.Types.Status.Ok,
            });
        }
        public override Task<Coin> LongestHistoryCoin(None request, ServerCallContext context)
        {
            CoinEntity coin = _coinRepository.CoinList.OrderByDescending(c => c.History.Count).FirstOrDefault();
            if(coin == null)
            {
                return Task.FromResult(new Coin
                {
                    Id = -1,
                    History = string.Empty
                });
            }
            return Task.FromResult(new Coin
            {
                Id = coin.Id,
                History = string.Join(", ",coin.History.Select(user=> user.Name).ToList()),
            });

        }
    }
}