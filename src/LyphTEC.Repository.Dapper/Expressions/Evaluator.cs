using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LyphTEC.Repository.Dapper.Expressions;

// from http://blogs.msdn.com/b/mattwar/archive/2007/08/01/linq-building-an-iqueryable-provider-part-iii.aspx
internal class Evaluator
{
    public static Expression PartialEval(Expression exp, Func<Expression, bool> canBeEval)
    {
        return new SubtreeEvaluator(new Nominator(canBeEval).Nominate(exp)).Eval(exp);
    }

    public static Expression PartialEval(Expression exp)
    {
        return PartialEval(exp, CanBeEvaluatedLocally);
    }

    private static bool CanBeEvaluatedLocally(Expression exp)
    {
        return exp.NodeType != ExpressionType.Parameter;
    }

    class SubtreeEvaluator : ExpressionVisitor
    {
        readonly HashSet<Expression> _candidates;

        internal SubtreeEvaluator(HashSet<Expression> candidates)
        {
            _candidates = candidates;
        }

        internal Expression Eval(Expression exp)
        {
            return this.Visit(exp);
        }

        public override Expression Visit(Expression exp)
        {
            if (exp == null)
                return null;

            if (_candidates.Contains(exp))
                return this.Evaluate(exp);

            return base.Visit(exp);
        }

        private Expression Evaluate(Expression exp)
        {
            if (exp.NodeType == ExpressionType.Constant)
                return exp;

            var lambda = Expression.Lambda(exp);
            var del = lambda.Compile();

            return Expression.Constant(del.DynamicInvoke(null), exp.Type);
        }
    }

    class Nominator : ExpressionVisitor
    {
        readonly Func<Expression, bool> _canBeEval;
        HashSet<Expression> _candidates;
        bool _cannotBeEval;

        internal Nominator(Func<Expression, bool> canBeEval)
        {
            _canBeEval = canBeEval;
        }

        internal HashSet<Expression> Nominate(Expression exp)
        {
            _candidates = new HashSet<Expression>();
            this.Visit(exp);
            return _candidates;
        }

        public override Expression Visit(Expression exp)
        {
            if (exp == null)
                return null;

            var saveCannotBeEval = _cannotBeEval;
            _cannotBeEval = false;

            base.Visit(exp);

            if (!_cannotBeEval)
            {
                if (_canBeEval(exp))
                    _candidates.Add(exp);
                else
                    _cannotBeEval = true;
            }

            _cannotBeEval |= saveCannotBeEval;

            return exp;
        }
    }
}
