using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace ExpressionTreesExample
{
    class Program
    {
        static void Main()
        {
            var cities = new List<City>(Enumerable.Range(0, 30).Select(x => new City { ru_Name = $"ru{x}", en_Name = $"en{x * 2}" }))
                .AsQueryable();
            var exprVisitor = new ExprVisitor();
            var filter = "1";
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("ru");

            var query = cities
                .Where(x => x.Name.Contains(filter))
                .Visit(exprVisitor); // changing query here

            foreach (var city in query)
                Console.WriteLine(city.Name);

            Console.ReadLine();
        }
    }

    public class City
    {
        [ExpressionLocalizable]
        public string Name
        {
            get
            {
                switch (Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName)
                {
                    case "ru":
                        return ru_Name;
                    case "en":
                        return en_Name;
                    default:
                        throw new NotSupportedException("Language is not supported");
                }
            }
            set
            {
                switch (Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName)
                {
                    case "ru":
                        ru_Name = value;
                        break;
                    case "en":
                        en_Name = value;
                        break;
                    default:
                        throw new NotSupportedException("Language is not supported");
                }
            }
        }

        public string ru_Name { get; set; }

        public string en_Name { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ExpressionLocalizableAttribute : Attribute { }

    public class ExprVisitor : ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.GetCustomAttribute<ExpressionLocalizableAttribute>() == null)
                return base.VisitMember(node);

            var nodeType = node.Expression.Type;
            var localizedPropertyName = $"{Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName}_{node.Member.Name}";
            var property = nodeType.GetProperty(localizedPropertyName);

            if (property == null)
            {
                throw new NotSupportedException($"No such property '{localizedPropertyName}' in type '{nodeType.Name}'!");
            }

            return Expression.MakeMemberAccess(node.Expression, property);
        }
    }

    public static class QueryExtensions
    {
        public static IQueryable<TResult> Visit<TResult>(this IQueryable<TResult> query, ExpressionVisitor visitor)
        {
            var expr = visitor.Visit(query.Expression);
            return query.Provider.CreateQuery<TResult>(expr);
        }
    }
}
