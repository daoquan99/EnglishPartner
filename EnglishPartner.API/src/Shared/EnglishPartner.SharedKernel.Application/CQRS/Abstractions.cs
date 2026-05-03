using EnglishPartner.SharedKernel.Results;
using MediatR;

namespace EnglishPartner.SharedKernel.CQRS;

// ── Marker Interfaces ──

public interface IBaseCommand;
public interface ICommand : IRequest<Result>, IBaseCommand;
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand;
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

// ── Handler Interfaces ──

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
