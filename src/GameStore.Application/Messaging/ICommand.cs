using GameStore.Domain.Shared;

namespace GameStore.Application.Messaging;

public interface ICommand : ICommand<Result> { }

public interface ICommand<TResponse> { }

public interface ICommandHandler<in TCommand> : ICommandHandler<TCommand, Result>
    where TCommand : ICommand
{ }

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken = default);
}