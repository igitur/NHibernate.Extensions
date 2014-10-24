﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NHibernate.Engine;
using NHibernate.Extensions;
using NHibernate.Extensions.Linq;
using NHibernate.Extensions.Lock;
using NHibernate.Linq.Visitors;
using NHibernate.Linq.Visitors.ResultOperatorProcessors;
using NHibernate.Param;
using NHibernate.SqlCommand;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.StreamedData;
using Remotion.Linq.Parsing.Structure;
using Remotion.Linq.Parsing.Structure.IntermediateModel;
using Remotion.Linq.Parsing.Structure.NodeTypeProviders;

namespace NHibernate.Linq
{
    public static class LinqExtensions
    {
        private static readonly FieldInfo QueryProvider;
        private static readonly PropertyInfo SessionPropertyInfo;

        static LinqExtensions()
        {
            QueryProvider = typeof (QueryableBase<>).GetField("_queryProvider",
                BindingFlags.NonPublic | BindingFlags.Instance);
            SessionPropertyInfo = typeof (DefaultQueryProvider).GetProperty("Session",
                BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static IQueryable<T> Lock<T>(this IQueryable<T> query, LockMode lockMode, string alias = null)
        {
            var method = ReflectionHelper.GetMethodDefinition(() => Lock<object>(null, lockMode, alias)).MakeGenericMethod(typeof(T));
            var callExpression = Expression.Call(method, query.Expression, Expression.Constant(lockMode), Expression.Constant(alias));
            return new NhQueryable<T>(query.Provider, callExpression);
        }

        public static IQueryable<T> Include<T>(this IQueryable<T> query, Expression<Func<T, object>> include) where T: class
        {
            var nhQuery = query as NhQueryable<T>;
            if(nhQuery == null)
                throw new Exception("Include function is supported only for Nhibernate queries");

            var providerField = typeof(QueryableBase<>).MakeGenericType(typeof(T))
                .GetField("_queryProvider", BindingFlags.NonPublic | BindingFlags.Instance);
            if(providerField == null)
                throw new NullReferenceException("providerField");
            var provider = providerField.GetValue(query) as IQueryProvider;
            var nhProvider = provider as IncludeQueryProvider<T>;
            if (nhProvider == null)
            {
                var session = SessionPropertyInfo.GetValue(provider, null) as ISessionImplementor; 
                nhProvider = new IncludeQueryProvider<T>(nhQuery, session);
                providerField.SetValue(nhQuery, nhProvider);
            }
            nhProvider.Include(include);
            return nhQuery;
        }

    }

}