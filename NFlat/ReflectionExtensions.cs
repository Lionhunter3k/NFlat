using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NFlat
{
    public static class ReflectionExtensions
    {
        public static Func<T, K> GetValueGetter<T, K>(this PropertyInfo propertyInfo)
        {
            var instance = Expression.Parameter(typeof(T), "i");
            var convertInstance = Expression.TypeAs(instance, propertyInfo.DeclaringType);
            var property = Expression.Property(convertInstance, propertyInfo);
            if (property.Type.IsClass || property.Type.IsInterface)
            {
                var convertProperty = Expression.TypeAs(property, typeof(K));
                return Expression.Lambda<Func<T, K>>(convertProperty, instance).Compile();
            }
            else
            {
                var convertProperty = Expression.Convert(property, typeof(K));
                return Expression.Lambda<Func<T, K>>(convertProperty, instance).Compile();
            }
        }

        public static Action<T, K> GetValueSetter<T, K>(this PropertyInfo propertyInfo)
        {
            var sourceObjectParam = Expression.Parameter(typeof(T));
            ParameterExpression propertyValueParam;
            Expression valueExpression;
            if (propertyInfo.PropertyType == typeof(K))
            {
                propertyValueParam = Expression.Parameter(propertyInfo.PropertyType);
                valueExpression = propertyValueParam;
            }
            else
            {
                propertyValueParam = Expression.Parameter(typeof(K));
                valueExpression = Expression.Convert(propertyValueParam, propertyInfo.PropertyType);
            }
            return Expression.Lambda<Action<T, K>>(Expression.Call(Expression.Convert(sourceObjectParam, propertyInfo.DeclaringType), propertyInfo.SetMethod, valueExpression), sourceObjectParam, propertyValueParam).Compile();
        }


        public static PropertyInfo GetPropertyInfo<TSource, TProperty>(this Expression<Func<TSource, TProperty>> expression)
        {
            MemberExpression memberExpression = null;

            if (expression.Body.NodeType == ExpressionType.Convert)
            {
                memberExpression = ((UnaryExpression)expression.Body).Operand as MemberExpression;
            }
            else if (expression.Body.NodeType == ExpressionType.MemberAccess)
            {
                memberExpression = expression.Body as MemberExpression;
            }

            if (memberExpression == null)
            {
                throw new ArgumentException("Not a member access", "expression");
            }

            return memberExpression.Member as PropertyInfo;
        }
    }
}
