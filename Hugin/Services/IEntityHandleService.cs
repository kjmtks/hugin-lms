using Hugin.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hugin.Services
{

    public interface IEntityHandleService<T> where T : class, IEntity
    {
        public DbSet<T> Set { get; }
        public T AddNew(T model);
        public T Update(T model);
        public void Remove(T model);
    }

    public abstract class EntityHandleServiceBase<T> : IEntityHandleService<T> where T : class, IEntity
    {
        protected readonly DatabaseContext DatabaseContext;
        public EntityHandleServiceBase(DatabaseContext databaseContext)
        {
            DatabaseContext = databaseContext;
        }

        public abstract DbSet<T> Set { get; }

        public abstract IQueryable<T> DefaultQuery { get; }

        protected virtual bool BeforeAddNew(T model) => true;
        protected virtual void AfterAddNew(T model) { }
        protected virtual bool BeforeUpdate(T model) => true;
        protected virtual void AfterUpdate(T model) { }
        protected virtual bool BeforeRemove(T model) => true;
        protected virtual void AfterRemove(T model) { }

        public T AddNew(T model)
        {
            lock(DatabaseContext)
            {
                if(!BeforeAddNew(model))
                {
                    return null;
                }
                Set.Add(model);
                DatabaseContext.SaveChanges();
                var _model = DefaultQuery.Where(x => x.Id == model.Id).FirstOrDefault();
                AfterAddNew(_model);
                return model;
            }

        }

        public T Update(T model)
        {
            lock (DatabaseContext)
            {
                var _model = DefaultQuery.Where(x => x.Id == model.Id).FirstOrDefault();
                if(_model != null)
                {
                    var xs = model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite && p.GetAccessors().All(x => !x.IsVirtual));
                    var ys = _model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite && p.GetAccessors().All(x => !x.IsVirtual));
                    var properties = xs.Join(ys, p => new { p.Name, p.PropertyType }, p => new { p.Name, p.PropertyType }, (p1, p2) => new { p1, p2 });
                    foreach (var property in properties)
                    {
                        property.p2.SetValue(_model, property.p1.GetValue(model));
                    }
                    if(!BeforeUpdate(_model))
                    {
                        return null;
                    }
                    Set.Update(_model);
                    DatabaseContext.SaveChanges();
                    AfterUpdate(model);
                    return _model;
                }
                return null;
            }
        }

        public void Remove(T model)
        {
            lock (DatabaseContext)
            {
                var _model = DefaultQuery.Where(x => x.Id == model.Id).FirstOrDefault();
                if (_model != null)
                {
                    if(!BeforeRemove(_model))
                    {
                        return;
                    }
                    Set.Remove(_model);
                    DatabaseContext.SaveChanges();
                    AfterRemove(model);
                }
            }
        }
    }
}
