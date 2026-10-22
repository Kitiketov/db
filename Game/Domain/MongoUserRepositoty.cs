using System;
using MongoDB.Driver;

namespace Game.Domain
{
    public class MongoUserRepository : IUserRepository
    {
        private readonly IMongoCollection<UserEntity> userCollection;
        public const string CollectionName = "users";

        public MongoUserRepository(IMongoDatabase database)
        {
            userCollection = database.GetCollection<UserEntity>(CollectionName);
            
            var indexKey = Builders<UserEntity>.IndexKeys.Ascending(x => x.Login);
            
            var indexOptions = new CreateIndexOptions { Unique = true };
            
            userCollection.Indexes.CreateOne(new CreateIndexModel<UserEntity>(indexKey, indexOptions));
        }

        public UserEntity Insert(UserEntity user)
        {
            userCollection.InsertOne(user);
            return user;
        }

        public UserEntity FindById(Guid id)
        {
            return userCollection.Find(x => x.Id == id).FirstOrDefault();
        }

        public UserEntity GetOrCreateByLogin(string login)
        {
            var user = userCollection.Find(x => x.Login == login).FirstOrDefault();

            if (user is not null)
                return user;

            user = new UserEntity(
                Guid.NewGuid(),
                login,
                string.Empty,
                string.Empty,
                0,
                null
            );
            
            try
            {
                userCollection.InsertOne(user);
                return user;
            }
            catch (MongoWriteException)
            {
                user = userCollection
                    .Find(x => x.Login == login)
                    .FirstOrDefault();

                if (user is not null)
                    return user;

                throw;
            }
        }

        public void Update(UserEntity user)
        {
            userCollection.ReplaceOne(existing => existing.Id == user.Id, user);
        }

        public void Delete(Guid id)
        {
            userCollection.DeleteOne(x => x.Id == id);
        }

        // Для вывода списка всех пользователей (упорядоченных по логину)
        // страницы нумеруются с единицы
        public PageList<UserEntity> GetPage(int pageNumber, int pageSize)
        {
            //TODO: Тебе понадобятся SortBy, Skip и Limit
            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber));
            
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize));
            
            var totalCount = (int)userCollection.CountDocuments(
                FilterDefinition<UserEntity>.Empty);
            
            var users = userCollection
                .Find(FilterDefinition<UserEntity>.Empty)
                .SortBy(user => user.Login)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            return new PageList<UserEntity>(users, totalCount, pageNumber, pageSize);
        }

        // Не нужно реализовывать этот метод
        public void UpdateOrInsert(UserEntity user, out bool isInserted)
        {
            throw new NotImplementedException();
        }
    }
}