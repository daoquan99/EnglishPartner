using EnglishPartner.SharedKernel.Interfaces;

namespace EnglishPartner.Modules.Identity.Application.Abstractions;

/// <summary>
/// Module-scoped UnitOfWork for Identity.
/// Extends the shared IUnitOfWork contract.
/// Implemented by IdentityDbContext in Infrastructure.
/// </summary>
public interface IIdentityUnitOfWork : IUnitOfWork;
