using WordBuddy.Content.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.Autofill.Commands.ApproveChildWord;

/// <summary>Approves an auto-filled word for children. <paramref name="LearnerId"/> set = supporter
/// approval for that child only (policy <c>CanSupportLearner</c>); <see langword="null"/> = admin
/// approval for every child (policy <c>AdminOnly</c>). <paramref name="ApproverId"/> comes from the JWT.</summary>
public sealed record ApproveChildWordCommand(Guid SenseId, Guid? LearnerId, Guid ApproverId) : ICommand;
