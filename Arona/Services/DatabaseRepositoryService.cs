using LiteDB;
using Arona.Models.DB;

namespace Arona.Services;

public interface IDatabaseRepositoryService<T> where T : IEntity, new()
{
    T GetOrCreate(string id, Action<T>? setup = null);
    void Update(T entity);
}

public class DatabaseRepositoryService<T>(LiteDatabase db, string collectionName) : IDatabaseRepositoryService<T> where T : IEntity, new()
{
    private readonly ILiteCollection<T> _collection = db.GetCollection<T>(collectionName);

    public T GetOrCreate(string id, Action<T>? setup = null)
    {
        var entity = _collection.FindById(id);
        if (entity != null) return entity;

        entity = new T { Id = id };
        setup?.Invoke(entity);

        _collection.Insert(entity);
        return entity;
    }

    public void Update(T entity) =>  _collection.Update(entity);
}