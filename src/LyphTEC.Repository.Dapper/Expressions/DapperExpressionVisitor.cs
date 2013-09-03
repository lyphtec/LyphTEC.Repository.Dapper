using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DapperExtensions;

namespace LyphTEC.Repository.Dapper.Expressions
{
    // See https://github.com/tmsmith/Dapper-Extensions/wiki/Predicates

    /// <summary>
    /// This class converts an Expression{Func{TEntity, bool}} into an IPredicate group that can be used with DapperExtension's predicate system
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    internal class DapperExpressionVisitor<TEntity> : ExpressionVisitor where TEntity : class, IEntity
    {
        private PredicateGroup _pg;
        private Expression _processedProperty;
        private bool _unarySpecified;

        public DapperExpressionVisitor()
        {
            this.Expressions = new HashSet<Expression>();
        }

        public IPredicate Process(Expression exp)
        {
            _pg = new PredicateGroup { Predicates = new List<IPredicate>() };
            this.Visit(Evaluator.PartialEval(exp));

            // the 1st expression determines root group operator
            if (this.Expressions.Any())
                _pg.Operator = (this.Expressions.First().NodeType == ExpressionType.OrElse) ? GroupOperator.Or : GroupOperator.And;

            return (_pg.Predicates.Count == 1) ? _pg.Predicates[0] : _pg;
        }

        /// <summary>
        /// Holds BinaryExpressions
        /// </summary>
        public HashSet<Expression> Expressions { get; private set; }

        private static PredicateGroup GetLastPredicateGroup(PredicateGroup grp)
        {
            var groups = grp.Predicates;

            if (!groups.Any())
                return grp;

            var last = groups.Last();

            if (last is PredicateGroup)
                return GetLastPredicateGroup(last as PredicateGroup);

            return grp;
        }

        private IFieldPredicate GetLastField()
        {
            var lastGrp = GetLastPredicateGroup(_pg);

            var last = lastGrp.Predicates.Last();

            if (last is IFieldPredicate)
                return last as IFieldPredicate;

            return null;
        }

        private static Operator DetermineOperator(Expression binaryExpression)
        {
            switch (binaryExpression.NodeType)
            {
                case ExpressionType.Equal:
                    return Operator.Eq;
                case ExpressionType.GreaterThan:
                    return Operator.Gt;
                case ExpressionType.GreaterThanOrEqual:
                    return Operator.Ge;
                case ExpressionType.LessThan:
                    return Operator.Lt;
                case ExpressionType.LessThanOrEqual:
                    return Operator.Le;
                default:
                    return Operator.Eq;
            }
        }

        private void AddField(MemberExpression exp, Operator op = Operator.Eq, object value = null, bool not = false)
        {
            var pg = GetLastPredicateGroup(_pg);

            // need convert from Expression<Func<T, bool>> to Expression<Func<T, object>> as this is what Predicates.Field() requires
            var fieldExp = Expression.Lambda<Func<TEntity, object>>(Expression.Convert(exp, typeof(object)), exp.Expression as ParameterExpression);

            var field = Predicates.Field(fieldExp, op, value, not);
            pg.Predicates.Add(field);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            this.Expressions.Add(node);

            var nt = node.NodeType;

            if (nt == ExpressionType.OrElse || nt == ExpressionType.AndAlso)
            {
                var pg = new PredicateGroup
                             {
                                 Predicates = new List<IPredicate>(),
                                 Operator = (nt == ExpressionType.OrElse) ? GroupOperator.Or : GroupOperator.And
                             };

                _pg.Predicates.Add(pg);
            }

            this.Visit(node.Left);

            if (node.Left is MemberExpression)
            {
                var field = GetLastField();
                field.Operator = DetermineOperator(node);

                if (nt == ExpressionType.NotEqual)
                    field.Not = true;
            }

            this.Visit(node.Right);

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.MemberType != MemberTypes.Property || node.Expression.Type != typeof(TEntity))
                throw new NotSupportedException(string.Format("The member '{0}' is not supported", node));

            // skip if prop is part of a VisitMethodCall
            if (_processedProperty != null && _processedProperty == node)
            {
                _processedProperty = null;
                return node;
            }

            AddField(node);

            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            var field = GetLastField();
            field.Value = node.Value;

            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Type == typeof(bool) && node.Method.DeclaringType == typeof(string))
            {
                var arg = ((ConstantExpression)node.Arguments[0]).Value;
                var op = Operator.Like;

                switch (node.Method.Name.ToLowerInvariant())
                {
                    case "startswith":
                        arg = arg + "%";
                        break;
                    case "endswith":
                        arg = "%" + arg;
                        break;
                    case "contains":
                        arg = "%" + arg + "%";
                        break;
                    case "equals":
                        op = Operator.Eq;
                        break;
                    default:
                        throw new NotSupportedException(string.Format("The method '{0}' is not supported", node));
                }

                _processedProperty = node.Object;	// this is a PropertyExpression but as it's internal, to use, we cast to the base MemberExpression instead (see http://social.msdn.microsoft.com/Forums/en-US/ab528f6a-a60e-4af6-bf31-d58e3f373356/resolving-propertyexpressions-and-fieldexpressions-in-a-custom-linq-provider)
                var me = _processedProperty as MemberExpression;

                AddField(me, op, arg, _unarySpecified);

                // reset if applicable
                _unarySpecified = false;

                return node;
            }

            throw new NotSupportedException(string.Format("The method '{0}' is not supported", node));
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType != ExpressionType.Not)
                throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", node.NodeType));

            _unarySpecified = true;

            return base.VisitUnary(node);	// returning base because we want to continue further processing - ie subsequent call to VisitMethodCall
        }

    }
}
