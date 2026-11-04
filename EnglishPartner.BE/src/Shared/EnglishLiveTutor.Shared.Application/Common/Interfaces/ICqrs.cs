using MediatR;

namespace EnglishLiveTutor.Application.Common.Interfaces;

public interface ICommand<out TResult> : IRequest<TResult> { }

public interface ICommand : IRequest { }

public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default);

    Task<TResult> IRequestHandler<TCommand, TResult>.Handle(TCommand request, CancellationToken cancellationToken)
        => HandleAsync(request, cancellationToken);
}

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken ct = default);

    Task IRequestHandler<TCommand>.Handle(TCommand request, CancellationToken cancellationToken)
        => HandleAsync(request, cancellationToken);
}

public interface IQuery<out TResult> : IRequest<TResult> { }

public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);

    Task<TResult> IRequestHandler<TQuery, TResult>.Handle(TQuery request, CancellationToken cancellationToken)
        => HandleAsync(request, cancellationToken);
}
