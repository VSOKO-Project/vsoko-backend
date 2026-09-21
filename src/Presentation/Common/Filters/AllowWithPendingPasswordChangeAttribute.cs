namespace Presentation.Common.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AllowWithPendingPasswordChangeAttribute : Attribute { }
