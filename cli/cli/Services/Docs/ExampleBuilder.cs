using System.Linq.Expressions;

namespace cli.Services;


public class MethodInvocationVisitor : ExpressionVisitor
{
	public static IEnumerable<MethodCallExpression> FindMethodInvocations(Action methodGroup)
	{
		var method = methodGroup.Method;
		var target = methodGroup.Target;

		var instance = Expression.Constant(target);
		// var arguments = new[] { Expression.Parameter(typeof(T)) };

		var callExpression = Expression.Call(instance, method);

		var lambdaExpression = Expression.Lambda<Action>(callExpression);

		var visitor = new MethodInvocationVisitor();
		visitor.Visit(lambdaExpression);
		return visitor.MethodInvocations;
	}
	
	public List<MethodCallExpression> MethodInvocations { get; } = new List<MethodCallExpression>();

	protected override Expression VisitMethodCall(MethodCallExpression node)
	{
		if (node.Method.Name == nameof(DocExample.RunCLIExpression))
		{
			MethodInvocations.Add(node);
		}

		return base.VisitMethodCall(node);
	}

	protected override Expression VisitLambda<T>(Expression<T> node)
	{
		return Visit(node.Body);
	}
}
